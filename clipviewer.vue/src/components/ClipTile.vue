<script setup>
import { formatDuration } from '../composables/useDuration.js'

const { video } = defineProps(['video'])
</script>

<template>
  <div class="aspect-video relative overflow-hidden bg-gray-200 dark:bg-gray-700">
    <img
      v-if="video.thumbnail"
      :src="video.thumbnail"
      :alt="video.name"
      class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
    />
    <img
      v-else
      src=""
      :alt="video.name"
      class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
    />
    <div
      class="absolute bottom-2 right-2 bg-black bg-opacity-75 text-white text-xs px-2 py-1 rounded"
    >
      {{ formatDuration(video.duration) }}
    </div>
  </div>
  <div class="p-4">
    <h3 class="font-semibold text-gray-800 dark:text-white truncate mb-1">
      {{ video.name }}
    </h3>
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500 dark:text-gray-400">
        {{ new Date(video.createdAt).toLocaleDateString() }}
      </p>
      <div class="bg-blue-500 text-white text-xs px-2 py-1 rounded font-medium">
        {{ video.author }}
      </div>
    </div>
  </div>
</template>
