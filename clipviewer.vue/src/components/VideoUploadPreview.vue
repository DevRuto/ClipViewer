<script setup>
import { ref } from 'vue'
import EditBar from './EditBar.vue'
import VideoPlayer from './VideoPlayer.vue'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'

const props = defineProps({
  videoUrl: {
    type: String,
    required: true
  },
  file: {
    type: File,
    default: null
  },
  isEditingMode: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['clear-preview', 'timestamps-change', 'toggle-edit-mode'])

const videoDuration = ref(0)
const editBarRef = ref(null)
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
    <div class="flex items-center justify-between mb-3">
      <h3 class="text-lg font-medium">
        {{ isEditingMode ? 'Clip Editor' : 'Video Preview' }}
      </h3>
      <Button variant="link" size="sm" class="text-destructive px-0" @click="clearVideoPreview">
        Clear Preview
      </Button>
    </div>

    <div
      class="relative aspect-video overflow-hidden rounded-lg bg-black"
      :class="[isEditingMode ? 'max-h-[70vh] max-w-7xl mx-auto' : 'max-h-96']"
    >
      <VideoPlayer ref="videoPlayerRef" :src="props.videoUrl" @loaded="onVideoLoaded" />
    </div>

    <!-- Edit Bar with Toggle -->
    <div class="mt-4">
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-2">
          <Switch id="editMode" :model-value="isEditingMode" @update:model-value="emit('toggle-edit-mode')" />
          <Label for="editMode" class="cursor-pointer">Edit Mode</Label>
        </div>
      </div>

      <EditBar
        v-if="isEditingMode"
        ref="editBarRef"
        :video-duration="videoDuration"
        :video-player-ref="videoPlayerRef"
        @timestamps-change="onTimestampsChange"
      />
    </div>

    <p class="mt-2 text-sm text-muted-foreground">
      {{ props.file?.name }} ({{ (props.file?.size / 1024 / 1024).toFixed(2) }} MB)
    </p>
  </div>
</template>
