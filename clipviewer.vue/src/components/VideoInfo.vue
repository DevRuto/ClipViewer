<script setup>
import { formatDuration } from '../composables/useDuration.js'
const longTitleTest =
  'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'

defineProps(['video'])
</script>

<template>
  <div class="p-6">
    <h1
      class="text-2xl font-bold text-gray-800 dark:text-white mb-2 break-words line-clamp-2 sm:line-clamp-3"
    >
      {{ video.name }} {{ longTitleTest }}
    </h1>
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4">
      <div
        class="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-500 dark:text-gray-400"
      >
        <div class="bg-blue-500 text-white text-xs px-2 py-1 rounded font-medium">
          {{ video.author }}
        </div>
        <span>Duration: {{ formatDuration(video.duration) }}</span>
        <span>Uploaded: {{ new Date(video.createdAt).toLocaleDateString() }}</span>
      </div>
      <a
        :href="video.sourceVideoFile"
        :download="video.name"
        class="inline-flex items-center justify-center w-full sm:w-auto px-3 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-md transition-colors"
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
</template>
