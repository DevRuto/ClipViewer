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
  },
  videoUrl: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['timestamps-change'])

const startTime = ref('0:00')
const endTime = ref('')

// The minimum distance (in seconds) a drag on the trim bar is allowed to leave between the
// start and end handles, so they can't be dragged past/on top of each other.
const MIN_GAP_SECONDS = 0.5
const FILMSTRIP_TILE_COUNT = 20

const startSeconds = computed(() => parseTimeToSeconds(startTime.value))
const endSeconds = computed(() => parseTimeToSeconds(endTime.value))

const timestampsValid = computed(() => {
  const start = startSeconds.value
  const end = endSeconds.value

  return startTime.value && endTime.value &&
         start >= 0 && end > 0 &&
         start < end &&
         end <= props.videoDuration
})

function clampPercent(value) {
  return Math.min(100, Math.max(0, value))
}

const startPercent = computed(() =>
  props.videoDuration > 0 ? clampPercent((startSeconds.value / props.videoDuration) * 100) : 0
)
const endPercent = computed(() =>
  props.videoDuration > 0 ? clampPercent((endSeconds.value / props.videoDuration) * 100) : 100
)

function onTimeInput() {
  if (timestampsValid.value) {
    emit('timestamps-change', { startTime: startSeconds.value, endTime: endSeconds.value })
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

// ---- Trim bar dragging ----
// A single in-flight drag, captured via Pointer Events so the handle keeps tracking the
// pointer even once it leaves the bar's bounds (see setPointerCapture below).
const trackRef = ref(null)
let drag = null

function ratioFromClientX(clientX) {
  const rect = trackRef.value.getBoundingClientRect()
  if (rect.width === 0) return 0
  return Math.min(1, Math.max(0, (clientX - rect.left) / rect.width))
}

function seekPreview(seconds) {
  props.videoPlayerRef?.goToTime?.(seconds)
}

function startDrag(mode, event) {
  if (props.videoDuration <= 0 || event.button !== 0) return
  event.preventDefault()
  const target = event.currentTarget
  target.setPointerCapture?.(event.pointerId)
  drag =
    mode === 'range'
      ? {
          mode,
          target,
          pointerId: event.pointerId,
          width: endSeconds.value - startSeconds.value,
          grabOffset: ratioFromClientX(event.clientX) * props.videoDuration - startSeconds.value,
        }
      : { mode, target, pointerId: event.pointerId }
}

function onDragMove(event) {
  if (!drag) return
  const t = ratioFromClientX(event.clientX) * props.videoDuration

  if (drag.mode === 'start') {
    const clamped = Math.max(0, Math.min(t, endSeconds.value - MIN_GAP_SECONDS))
    startTime.value = formatDuration(clamped)
    seekPreview(clamped)
  } else if (drag.mode === 'end') {
    const clamped = Math.min(props.videoDuration, Math.max(t, startSeconds.value + MIN_GAP_SECONDS))
    endTime.value = formatDuration(clamped)
    seekPreview(clamped)
  } else if (drag.mode === 'range') {
    const clamped = Math.max(0, Math.min(props.videoDuration - drag.width, t - drag.grabOffset))
    startTime.value = formatDuration(clamped)
    endTime.value = formatDuration(clamped + drag.width)
    seekPreview(clamped)
  }
}

function endDrag() {
  if (!drag) return
  drag.target.releasePointerCapture?.(drag.pointerId)
  drag = null
}

// ---- Filmstrip preview ----
// Renders real frames from the dropped clip onto the trim bar (the same idea as the Worker's
// scrub-sprite on the video page) using a detached <video> + canvas so it never disturbs the
// visible preview player's own playback position.
const tiles = ref(Array(FILMSTRIP_TILE_COUNT).fill(null))
let filmstripToken = 0

async function generateFilmstrip() {
  const token = ++filmstripToken
  tiles.value = Array(FILMSTRIP_TILE_COUNT).fill(null)
  if (!props.videoUrl || !props.videoDuration) return

  try {
    const video = document.createElement('video')
    video.muted = true
    video.preload = 'auto'
    video.src = props.videoUrl
    await new Promise((resolve, reject) => {
      video.addEventListener('loadedmetadata', resolve, { once: true })
      video.addEventListener('error', reject, { once: true })
    })
    if (token !== filmstripToken) return

    const duration = video.duration || props.videoDuration
    const tileWidth = 96
    const tileHeight = Math.round(tileWidth * ((video.videoHeight || 9) / (video.videoWidth || 16)))
    const canvas = document.createElement('canvas')
    canvas.width = tileWidth
    canvas.height = tileHeight
    const ctx = canvas.getContext('2d')

    const captured = []
    for (let i = 0; i < FILMSTRIP_TILE_COUNT; i++) {
      const seekTime = Math.min(duration - 0.05, Math.max(0, ((i + 0.5) / FILMSTRIP_TILE_COUNT) * duration))
      await new Promise((resolve) => {
        video.addEventListener('seeked', resolve, { once: true })
        video.currentTime = seekTime
      })
      if (token !== filmstripToken) return
      ctx.drawImage(video, 0, 0, tileWidth, tileHeight)
      captured.push(canvas.toDataURL('image/jpeg', 0.6))
      tiles.value = [...captured, ...Array(FILMSTRIP_TILE_COUNT - captured.length).fill(null)]
    }
  } catch {
    // No filmstrip preview available (e.g. an unsupported codec) - the numeric start/end
    // fields and drag handles still work without it.
  }
}

watch(() => [props.videoUrl, props.videoDuration], generateFilmstrip, { immediate: true })
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-baseline justify-between">
      <Label>Trim clip</Label>
      <span class="text-sm text-muted-foreground">
        Selected <span class="font-medium text-foreground">{{ formatDuration(Math.max(0, endSeconds - startSeconds)) }}</span>
        of {{ formatDuration(videoDuration) }}
      </span>
    </div>

    <div
      ref="trackRef"
      class="relative h-14 w-full touch-none overflow-hidden rounded-md border border-border bg-black select-none"
    >
      <div class="absolute inset-0 flex">
        <div
          v-for="(tile, index) in tiles"
          :key="index"
          class="flex-1 border-r border-white/10 bg-cover bg-center last:border-r-0"
          :class="{ 'animate-pulse bg-white/10': !tile }"
          :style="tile ? { backgroundImage: `url(${tile})` } : undefined"
        />
      </div>

      <div class="pointer-events-none absolute inset-y-0 left-0 bg-black/60" :style="{ width: startPercent + '%' }" />
      <div class="pointer-events-none absolute inset-y-0 right-0 bg-black/60" :style="{ width: 100 - endPercent + '%' }" />

      <div
        class="absolute inset-y-0 cursor-grab border-y-2 border-primary bg-primary/20 active:cursor-grabbing"
        data-testid="trim-selection"
        :style="{ left: startPercent + '%', width: Math.max(0, endPercent - startPercent) + '%' }"
        @pointerdown="startDrag('range', $event)"
        @pointermove="onDragMove"
        @pointerup="endDrag"
        @pointercancel="endDrag"
      />

      <div
        class="absolute inset-y-0 z-10 -ml-[7px] flex w-3.5 cursor-ew-resize items-center justify-center rounded bg-primary"
        role="slider"
        aria-label="Start time"
        :aria-valuemin="0"
        :aria-valuemax="videoDuration"
        :aria-valuenow="startSeconds"
        :style="{ left: startPercent + '%' }"
        @pointerdown="startDrag('start', $event)"
        @pointermove="onDragMove"
        @pointerup="endDrag"
        @pointercancel="endDrag"
      >
        <span class="h-4 w-0.5 rounded-full bg-primary-foreground/80" />
      </div>
      <div
        class="absolute inset-y-0 z-10 -ml-[7px] flex w-3.5 cursor-ew-resize items-center justify-center rounded bg-primary"
        role="slider"
        aria-label="End time"
        :aria-valuemin="0"
        :aria-valuemax="videoDuration"
        :aria-valuenow="endSeconds"
        :style="{ left: endPercent + '%' }"
        @pointerdown="startDrag('end', $event)"
        @pointermove="onDragMove"
        @pointerup="endDrag"
        @pointercancel="endDrag"
      >
        <span class="h-4 w-0.5 rounded-full bg-primary-foreground/80" />
      </div>
    </div>

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
    </div>

    <Alert v-if="startTime && endTime && !timestampsValid" variant="destructive">
      <AlertDescription>
        Invalid timestamps. Make sure start time is before end time and within video duration.
      </AlertDescription>
    </Alert>
  </div>
</template>
