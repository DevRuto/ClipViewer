<script setup>
import { onMounted, ref, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/services/api'
import VideoPlayer from '@/components/VideoPlayer.vue'
import VideoInfo from '@/components/VideoInfo.vue'
import { Card } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { AlertCircle, VideoOff } from '@lucide/vue'

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

async function loadVideo() {
  loading.value = true
  video.value = null
  error.value = ''
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }

  await fetchVideo()

  // Start polling if video is not processed
  if (video.value && !video.value.processed) {
    startPolling()
  }

  loading.value = false
}

onMounted(loadVideo)

// Vue Router reuses this component instance when navigating between two /clips/:videoId routes,
// so onMounted alone won't re-fire - reload explicitly when the param changes.
watch(
  () => route.params.videoId,
  (newId, oldId) => {
    if (newId !== oldId) loadVideo()
  },
)

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
    <div v-if="loading" class="max-w-5xl mx-auto">
      <Skeleton class="aspect-video w-full rounded-lg mb-4" />
      <Skeleton class="h-8 w-2/3 mb-2" />
      <Skeleton class="h-4 w-1/3" />
      <p class="sr-only">Loading video...</p>
    </div>

    <div v-else-if="video" :class="['mx-auto', { 'max-w-5xl': !isCinemaMode, 'max-w-7xl': isCinemaMode }]">
      <Alert v-if="error" variant="destructive" class="mb-4">
        <AlertDescription>{{ error }}</AlertDescription>
      </Alert>
      <Card class="overflow-hidden py-0 gap-0">
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
      </Card>
    </div>

    <div v-else-if="error" class="text-center py-12">
      <AlertCircle class="size-16 text-destructive mx-auto mb-4" />
      <p class="text-muted-foreground text-lg mb-2">Unable to load video</p>
      <p class="text-muted-foreground/70">{{ error }}</p>
      <Button variant="link" class="mt-4" @click="refreshVideo">Try again</Button>
    </div>

    <div v-else class="text-center py-12">
      <VideoOff class="size-16 text-muted-foreground mx-auto mb-4" />
      <p class="text-muted-foreground text-lg mb-2">Video not found</p>
      <p class="text-muted-foreground/70">The video you're looking for doesn't exist.</p>
      <RouterLink to="/browse" class="mt-4 inline-block text-primary hover:underline">
        ← Back to clips
      </RouterLink>
    </div>
  </div>
</template>
