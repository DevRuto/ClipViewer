<script setup>
import { ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import ClipTile from './ClipTile.vue'
import { api } from '../services/api'

const videos = ref([])

const props = defineProps({
  username: {
    type: String,
    default: null,
  },
})

async function fetchVideos() {
  const usernameParam = props.username ? `?user=${props.username}` : ''
  const res = await api.get(`/api/videos${usernameParam}`)
  videos.value = res.data
}

watch(
  () => props.username,
  () => {
    videos.value = []
    fetchVideos()
  },
)
</script>

<template>
  <div
    v-if="videos.length > 0"
    class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6"
  >
    <RouterLink
      v-for="video in videos"
      :key="video.id"
      :to="`/clips/${video.videoId}`"
      class="group block bg-white dark:bg-gray-800 rounded-lg overflow-hidden shadow-md hover:shadow-lg transition-shadow duration-200"
    >
      <ClipTile :video="video" />
    </RouterLink>
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
        d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"
      ></path>
    </svg>
    <p class="text-gray-500 dark:text-gray-400 text-lg mb-2">No clips uploaded yet</p>
    <p class="text-gray-400 dark:text-gray-500">Upload your first video to get started</p>
  </div>
</template>
