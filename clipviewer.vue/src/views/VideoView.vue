<script setup>
import { onMounted, ref, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/services/api'
import VideoPlayer from '@/components/VideoPlayer.vue'
import VideoInfo from '@/components/VideoInfo.vue'

const route = useRoute()
const router = useRouter()
const video = ref(null)
const loading = ref(true)
const videoPlayer = ref(null)
const isCinemaMode = ref(false)
const pollingInterval = ref(null)
const error = ref('')

const videoSource = ref(null)

async function fetchVideo(isPolling = false) {
  try {
    const response = await api.get(`/api/videos/${route.params.videoId}`)
    if (response.status === 200) {
      video.value = response.data
      error.value = ''
      // Update document title with video name
      if (video.value.name) {
        document.title = `${video.value.name} - ClipViewer`
      }
      // Stop polling if video is processed
      if (response.data.processed && pollingInterval.value) {
        clearInterval(pollingInterval.value)
        pollingInterval.value = null
      }

      // Update video source
      if (!isPolling && response.data.processed) {
        videoSource.value = response.data.hlsPlaylistFile
      } else {
        videoSource.value = response.data.sourceVideoFile
      }
    }
  } catch (err) {
    console.error('Failed to fetch video:', err)
    // A 404 is a legitimate "not found" state, handled by the template - don't treat it as an error banner
    if (!isPolling && err.response?.status !== 404) {
      error.value = err.response?.data?.message || 'Failed to load video. Please try again.'
    }
  }
}

function startPolling() {
  if (pollingInterval.value) return

  pollingInterval.value = setInterval(() => {
    if (video.value && !video.value.processed) {
      fetchVideo(true) // Pass true to indicate this is a polling request
    }
  }, 5000) // Poll every 5 seconds
}

onMounted(async () => {
  await fetchVideo()

  // Start polling if video is not processed
  if (video.value && !video.value.processed) {
    startPolling()
  }

  loading.value = false
})

onUnmounted(() => {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }
})

function onVideoLoaded() {
  // Check if param time is set
  if (route.query.t) {
    videoPlayer.value.goToTime(route.query.t)
  }
}

async function updateVideo(updatedVideo) {
  try {
    const response = await api.put(`/api/videos/${route.params.videoId}`, {
      unlisted: updatedVideo.unlisted,
      name: updatedVideo.name,
      description: updatedVideo.description,
    })

    if (response.status === 200) {
      video.value = response.data
      error.value = ''
      // Update document title with new video name
      if (video.value.name) {
        document.title = `${video.value.name} - ClipViewer`
      }
    }
  } catch (err) {
    console.error('Failed to update video:', err)
    error.value = err.response?.data?.message || 'Failed to save changes. Please try again.'
  }
}

async function retryVideo() {
  try {
    const response = await api.post(`/api/videos/${route.params.videoId}/retry`)

    if (response.status === 200) {
      video.value = response.data
      error.value = ''
      startPolling()
    }
  } catch (err) {
    console.error('Failed to retry video:', err)
    error.value = err.response?.data?.message || 'Failed to retry the conversion. Please try again.'
  }
}

async function deleteVideo() {
  try {
    const response = await api.delete(`/api/videos/${route.params.videoId}`)

    if (response.status === 200) {
      router.push('/browse')
    }
  } catch (err) {
    console.error('Failed to delete video:', err)
    error.value = err.response?.data?.message || 'Failed to delete video. Please try again.'
  }
}

function onToggleCinemaMode(cinemaModeState) {
  isCinemaMode.value = cinemaModeState
}

async function refreshVideo() {
  loading.value = true
  await fetchVideo()
  loading.value = false
}
</script>

<template>
  <div :class="['mx-auto px-4 py-8 container']">
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      <p class="mt-4 text-gray-600 dark:text-gray-400">Loading video...</p>
    </div>

    <div
      v-else-if="video"
      :class="['mx-auto', { 'max-w-5xl': !isCinemaMode, 'max-w-7xl': isCinemaMode }]"
    >
      <div
        v-if="error"
        class="mb-4 p-3 bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-600 rounded-md"
      >
        <p class="text-red-700 dark:text-red-400 text-sm">{{ error }}</p>
      </div>
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden">
        <div class="aspect-video">
          <VideoPlayer
            ref="videoPlayer"
            :src="videoSource"
            :placeholder="video.thumbnail"
            @loaded="onVideoLoaded"
            @toggleCinemaMode="onToggleCinemaMode"
          />
        </div>
        <VideoInfo
          :video="video"
          :videoPlayer="videoPlayer"
          @update-video="updateVideo"
          @delete-video="deleteVideo"
          @refresh-video="refreshVideo"
          @retry-video="retryVideo"
        />
      </div>
    </div>

    <div v-else-if="error" class="text-center py-12">
      <svg
        class="w-16 h-16 text-red-400 dark:text-red-500 mx-auto mb-4"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        ></path>
      </svg>
      <p class="text-gray-500 dark:text-gray-400 text-lg mb-2">Unable to load video</p>
      <p class="text-gray-400 dark:text-gray-500">{{ error }}</p>
      <button
        @click="refreshVideo"
        class="mt-4 inline-block text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
      >
        Try again
      </button>
    </div>

    <div v-else class="text-center py-12">
      <svg
        class="w-16 h-16 text-gray-400 dark:text-gray-500 mx-auto mb-4"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
        ></path>
      </svg>
      <p class="text-gray-500 dark:text-gray-400 text-lg mb-2">Video not found</p>
      <p class="text-gray-400 dark:text-gray-500">The video you're looking for doesn't exist.</p>
      <RouterLink
        to="/browse"
        class="mt-4 inline-block text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
      >
        ← Back to clips
      </RouterLink>
    </div>
  </div>
</template>
