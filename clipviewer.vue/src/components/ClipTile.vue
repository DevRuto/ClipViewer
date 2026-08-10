<script setup>
import { formatDuration } from '@/composables/useDuration.js'
import { useAuthorColor } from '@/composables/useAuthorColor.js'

const { video } = defineProps(['video'])
const { stringToColor, getContrastColor } = useAuthorColor()

const authorColor = stringToColor(video.author)
const textColor = getContrastColor(authorColor)
</script>

<template>
  <div class="aspect-video relative overflow-hidden bg-muted">
    <img
      v-if="video.thumbnail"
      :src="video.thumbnail"
      :alt="video.name"
      class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
    />
    <div class="absolute bottom-2 right-2 bg-black/75 text-white text-xs px-2 py-1 rounded">
      {{ formatDuration(video.duration) }}
    </div>
  </div>
  <div class="p-4">
    <h3 class="font-semibold truncate mb-1">
      {{ video.name }}
    </h3>
    <div class="flex items-center justify-between">
      <p class="text-sm text-muted-foreground">
        {{ new Date(video.createdAt).toLocaleDateString() }}
      </p>
      <div class="text-xs px-2 py-1 rounded font-medium" :style="{ backgroundColor: authorColor, color: textColor }">
        {{ video.author }}
      </div>
    </div>
  </div>
</template>
