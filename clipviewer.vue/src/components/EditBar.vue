<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { formatDuration, parseTimeToSeconds } from '@/composables/useDuration.js'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'

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
  }
}

function setEndTime() {
  if (props.videoPlayerRef) {
    endTime.value = formatDuration(props.videoPlayerRef.currentTime)
  }
}

defineExpose({
  setStartTime,
  setEndTime
})

// The shadcn Input's v-model updates asynchronously (via VueUse's useVModel), so an @input
// listener on the Input itself would read startTime/endTime before they've actually changed.
// Watching the refs directly guarantees onTimeInput only runs once they hold the new value.
watch([startTime, endTime], onTimeInput)

onMounted(() => {
  endTime.value = formatDuration(props.videoDuration)
  onTimeInput()
})
</script>

<template>
  <div class="space-y-4">
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="space-y-2">
        <Label>Start Time</Label>
        <div class="flex gap-2">
          <Input v-model="startTime" type="text" placeholder="0:00" />
          <Button type="button" title="Set to current time" @click="setStartTime">Set</Button>
        </div>
      </div>

      <div class="space-y-2">
        <Label>End Time</Label>
        <div class="flex gap-2">
          <Input v-model="endTime" type="text" placeholder="1:00" />
          <Button type="button" title="Set to current time" @click="setEndTime">Set</Button>
        </div>
      </div>
    </div>

    <div class="text-sm text-muted-foreground">
      <p>Format: MM:SS or H:MM:SS</p>
      <p>Video duration: {{ formatDuration(videoDuration) }}</p>
      <p v-if="startTime && endTime" :class="{ 'text-destructive': !timestampsValid }">
        Clip duration: {{ formatDuration(parseTimeToSeconds(endTime) - parseTimeToSeconds(startTime)) }}
      </p>
    </div>

    <Alert v-if="startTime && endTime && !timestampsValid" variant="destructive">
      <AlertDescription>
        Invalid timestamps. Make sure start time is before end time and within video duration.
      </AlertDescription>
    </Alert>
  </div>
</template>
