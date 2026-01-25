<script setup>
import { ref } from 'vue'
import EditBar from './EditBar.vue'
import VideoPlayer from './VideoPlayer.vue'

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
  emit('timestamps-change', timestamps)
}

function clearVideoPreview() {
  emit('clear-preview')
}
</script>

<template>
  <div class="mt-6">
    <div class="flex items-center justify-between mb-3">
      <h3 class="text-lg font-medium text-gray-800 dark:text-white">
        {{ isEditingMode ? 'Clip Editor' : 'Video Preview' }}
      </h3>
      <button
        @click="clearVideoPreview"
        class="text-sm text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300 transition-colors"
      >
        Clear Preview
      </button>
    </div>

    <div class="relative rounded-lg overflow-hidden bg-black" :class="[isEditingMode ? 'max-w-7xl mx-auto' : '']">
      <VideoPlayer
        ref="videoPlayerRef"
        :src="props.videoUrl"
        @loaded="onVideoLoaded"
        class="w-full"
        :class="[isEditingMode ? 'max-h-[70vh]' : 'max-h-96']"
      />
    </div>

    <!-- Edit Bar with Toggle -->
    <div class="mt-4">
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center">
          <input
            type="checkbox"
            :checked="isEditingMode"
            @change="emit('toggle-edit-mode')"
            class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded cursor-pointer"
            id="editMode"
          />
          <label for="editMode" class="ml-2 text-sm font-medium text-gray-700 dark:text-gray-300 cursor-pointer">
            Edit Mode
          </label>
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

    <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
      {{ props.file?.name }} ({{ (props.file?.size / 1024 / 1024).toFixed(2) }} MB)
    </p>
  </div>
</template>
