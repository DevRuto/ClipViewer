<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  videoDuration: {
    type: Number,
    default: 0
  }
})

const emit = defineEmits(['timestamps-change'])

const startTime = ref('')
const endTime = ref('')

const formatTime = (seconds) => {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = Math.floor(seconds % 60)

  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
  }
  return `${minutes}:${secs.toString().padStart(2, '0')}`
}

const parseTimeToSeconds = (timeString) => {
  if (!timeString) return 0

  const parts = timeString.split(':').map(Number)
  if (parts.length === 2) {
    return parts[0] * 60 + parts[1]
  } else if (parts.length === 3) {
    return parts[0] * 3600 + parts[1] * 60 + parts[2]
  }
  return 0
}

const timestampsValid = computed(() => {
  const start = parseTimeToSeconds(startTime.value)
  const end = parseTimeToSeconds(endTime.value)

  return startTime.value && endTime.value &&
         start >= 0 && end > 0 &&
         start < end &&
         end <= props.videoDuration
})

function onTimeInput() {
  const start = parseTimeToSeconds(startTime.value)
  const end = parseTimeToSeconds(endTime.value)

  if (timestampsValid.value) {
    emit('timestamps-change', { startTime: start, endTime: end })
  } else {
    emit('timestamps-change', null)
  }
}

function setStartTime() {
  const video = document.querySelector('video')
  if (video) {
    startTime.value = formatTime(video.currentTime)
    onTimeInput()
  }
}

function setEndTime() {
  const video = document.querySelector('video')
  if (video) {
    endTime.value = formatTime(video.currentTime)
    onTimeInput()
  }
}

defineExpose({
  setStartTime,
  setEndTime
})
</script>

<template>
  <div class="space-y-4">
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Start Time
        </label>
        <div class="flex gap-2">
          <input
            v-model="startTime"
            type="text"
            placeholder="0:00"
            class="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
            @input="onTimeInput"
          />
          <button
            @click="setStartTime"
            class="px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition duration-200"
            title="Set to current time"
          >
            Set
          </button>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          End Time
        </label>
        <div class="flex gap-2">
          <input
            v-model="endTime"
            type="text"
            placeholder="1:00"
            class="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
            @input="onTimeInput"
          />
          <button
            @click="setEndTime"
            class="px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition duration-200"
            title="Set to current time"
          >
            Set
          </button>
        </div>
      </div>
    </div>

    <div class="text-sm text-gray-600 dark:text-gray-400">
      <p>Format: MM:SS or H:MM:SS</p>
      <p>Video duration: {{ formatTime(videoDuration) }}</p>
      <p v-if="startTime && endTime" :class="{ 'text-red-500': !timestampsValid }">
        Clip duration: {{ formatTime(parseTimeToSeconds(endTime) - parseTimeToSeconds(startTime)) }}
      </p>
    </div>

    <div v-if="startTime && endTime && !timestampsValid" class="p-3 bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-600 rounded-md">
      <p class="text-red-700 dark:text-red-400 text-sm">
        Invalid timestamps. Make sure start time is before end time and within video duration.
      </p>
    </div>
  </div>
</template>
