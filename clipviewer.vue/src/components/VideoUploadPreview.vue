<script setup>
import { ref } from 'vue'
import EditBar from './EditBar.vue'
import VideoPlayer from './VideoPlayer.vue'
import { Button } from '@/components/ui/button'

const props = defineProps({
  videoUrl: {
    type: String,
    required: true
  },
  file: {
    type: File,
    default: null
  }
})

const emit = defineEmits(['clear-preview', 'timestamps-change'])

const videoDuration = ref(0)
const videoPlayerRef = ref(null)

function onVideoLoaded() {
  if (videoPlayerRef.value && videoPlayerRef.value.refVideo) {
    videoDuration.value = videoPlayerRef.value.refVideo.duration
  }
}

function onTimestampsChange(timestamps) {
  emit('timestamps-change', timestamps ? { ...timestamps, videoDuration: videoDuration.value } : null)
}

function clearVideoPreview() {
  emit('clear-preview')
}
</script>

<template>
  <div class="mt-6">
    <div class="flex items-center justify-between gap-3 mb-3">
      <h3 class="text-lg font-medium truncate">
        {{ props.file?.name }}
        <span class="text-sm font-normal text-muted-foreground">
          ({{ (props.file?.size / 1024 / 1024).toFixed(2) }} MB)
        </span>
      </h3>
      <Button variant="link" size="sm" class="text-destructive px-0 shrink-0" @click="clearVideoPreview">
        Clear Preview
      </Button>
    </div>

    <div class="relative aspect-video max-h-[70vh] max-w-7xl mx-auto overflow-hidden rounded-lg bg-black">
      <VideoPlayer ref="videoPlayerRef" :src="props.videoUrl" @loaded="onVideoLoaded" />
    </div>

    <div class="mt-4">
      <EditBar
        :video-duration="videoDuration"
        :video-player-ref="videoPlayerRef"
        :video-url="props.videoUrl"
        @timestamps-change="onTimestampsChange"
      />
    </div>
  </div>
</template>
