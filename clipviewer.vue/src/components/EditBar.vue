<script setup>
import { ref, computed, onMounted } from 'vue'
import { formatDuration, parseTimeToSeconds } from '@/composables/useDuration.js'

const props = defineProps({
  videoDuration: {
    type: Number,
    default: 0
  },
  videoPlayerRef: {
    type: Object,
    default: null
  }
})

const emit = defineEmits(['timestamps-change'])

const startTime = ref('0:00')
const endTime = ref('')

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
  if (props.videoPlayerRef) {
    startTime.value = formatDuration(props.videoPlayerRef.currentTime)
    onTimeInput()
  }
}

function setEndTime() {
  if (props.videoPlayerRef) {
    endTime.value = formatDuration(props.videoPlayerRef.currentTime)
    onTimeInput()
  }
}

defineExpose({
  setStartTime,
  setEndTime
})

onMounted(() => {
  onTimeInput()
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
      <p>Video duration: {{ formatDuration(videoDuration) }}</p>
      <p v-if="startTime && endTime" :class="{ 'text-red-500': !timestampsValid }">
        Clip duration: {{ formatDuration(parseTimeToSeconds(endTime) - parseTimeToSeconds(startTime)) }}
      </p>
    </div>

    <div v-if="startTime && endTime && !timestampsValid" class="p-3 bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-600 rounded-md">
      <p class="text-red-700 dark:text-red-400 text-sm">
        Invalid timestamps. Make sure start time is before end time and within video duration.
      </p>
    </div>
  </div>
</template>
