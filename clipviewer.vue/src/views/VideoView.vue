<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { api } from '../services/api'
import VideoPlayer from '../components/VideoPlayer.vue'
import { formatDuration } from '../composables/useDuration.js'

const route = useRoute()
const video = ref(null)
const loading = ref(true)

const videoSource = computed(() => {
  if (video.value?.hlsPlaylistFile) {
    return video.value.hlsPlaylistFile
  }
  return video.value?.sourceVideoFile
})

onMounted(async () => {
  try {
    const response = await api.get(`/api/videos/${route.params.videoId}`)
    if (response.status === 200) {
      video.value = response.data
    }
  } catch (error) {
    console.error('Failed to fetch video:', error)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="min-h-screen">
    <div class="container mx-auto px-4 py-8">
      <div v-if="loading" class="text-center py-12">
        <div
          class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"
        ></div>
        <p class="mt-4 text-gray-600 dark:text-gray-400">Loading video...</p>
      </div>

      <div v-else-if="video" class="max-w-4xl mx-auto">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden">
          <div class="aspect-video">
            <video-player :src="videoSource" :placeholder="video.thumbnail" />
          </div>

          <div class="p-6">
            <h1 class="text-2xl font-bold text-gray-800 dark:text-white mb-2">{{ video.name }}</h1>
            <div class="flex items-center justify-between mb-4">
              <div class="flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400">
                <div class="bg-blue-500 text-white text-xs px-2 py-1 rounded font-medium">
                  {{ video.author }}
                </div>
                <span>Duration: {{ formatDuration(video.duration) }}</span>
                <span>Uploaded: {{ new Date(video.createdAt).toLocaleDateString() }}</span>
              </div>
              <a
                :href="video.sourceVideoFile"
                :download="video.name"
                class="inline-flex items-center px-3 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-md transition-colors"
              >
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
                  ></path>
                </svg>
                Download
              </a>
            </div>
          </div>
        </div>
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
  </div>
</template>
