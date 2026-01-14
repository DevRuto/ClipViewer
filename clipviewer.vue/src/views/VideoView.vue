<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import VideoPlayer from '../components/VideoPlayer.vue'

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
    const response = await fetch(`/api/videos/${route.params.videoId}`)
    if (response.ok) {
      video.value = await response.json()
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
          <div class="aspect-video bg-black">
            <video-player :src="videoSource" :placeholder="video.thumbnail" />
          </div>

          <div class="p-6">
            <h1 class="text-2xl font-bold text-gray-800 dark:text-white mb-2">{{ video.name }}</h1>
            <div class="flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400 mb-4">
              <div class="bg-blue-500 text-white text-xs px-2 py-1 rounded font-medium">
                {{ video.author }}
              </div>
              <span>Duration: {{ video.duration }}</span>
              <span>Uploaded: {{ new Date(video.createdAt).toLocaleDateString() }}</span>
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
          to="/clips"
          class="mt-4 inline-block text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
        >
          ← Back to clips
        </RouterLink>
      </div>
    </div>
  </div>
</template>
