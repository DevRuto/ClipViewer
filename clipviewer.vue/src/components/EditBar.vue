<script setup>
import { ref, computed, watch } from 'vue'
import { formatDuration } from '@/composables/useDuration.js'

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
  },
  videoFileSize: {
    type: Number,
    default: 0
  }
})

const emit = defineEmits(['timestamps-change'])

// The minimum distance (in seconds) a drag on the trim bar is allowed to leave between the
// start and end handles, so they can't be dragged past/on top of each other. Whole seconds
// since that's also the backend's precision (UploadController takes int? start/endTime).
const MIN_GAP_SECONDS = 1
const FILMSTRIP_TILE_COUNT = 20

// The trim bar is the only way to set these - always a clamped, valid range by construction,
// so there's no invalid-timestamp state to guard against here.
const start = ref(0)
const end = ref(0)

function clampPercent(value) {
  return Math.min(100, Math.max(0, value))
}

const startPercent = computed(() =>
  props.videoDuration > 0 ? clampPercent((start.value / props.videoDuration) * 100) : 0
)
const endPercent = computed(() =>
  props.videoDuration > 0 ? clampPercent((end.value / props.videoDuration) * 100) : 100
)

function emitChange() {
  emit('timestamps-change', { startTime: start.value, endTime: end.value })
}

// videoDuration starts out at 0 and only becomes known once the preview video's metadata has
// loaded, which can happen after Edit Mode is already toggled on - so this can't just be an
// onMounted default, it has to react to the prop arriving (or changing) later too.
watch(
  () => props.videoDuration,
  (duration) => {
    start.value = 0
    end.value = Math.floor(duration)
    emitChange()
  },
  { immediate: true }
)

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
          width: end.value - start.value,
          grabOffset: Math.round(ratioFromClientX(event.clientX) * props.videoDuration) - start.value,
        }
      : { mode, target, pointerId: event.pointerId }
}

function onDragMove(event) {
  if (!drag) return
  const maxSeconds = Math.floor(props.videoDuration)
  const t = Math.round(ratioFromClientX(event.clientX) * props.videoDuration)

  if (drag.mode === 'start') {
    start.value = Math.max(0, Math.min(t, end.value - MIN_GAP_SECONDS))
    seekPreview(start.value)
  } else if (drag.mode === 'end') {
    end.value = Math.min(maxSeconds, Math.max(t, start.value + MIN_GAP_SECONDS))
    seekPreview(end.value)
  } else if (drag.mode === 'range') {
    const clamped = Math.max(0, Math.min(maxSeconds - drag.width, t - drag.grabOffset))
    start.value = clamped
    end.value = clamped + drag.width
    seekPreview(clamped)
  }
  emitChange()
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
// Above this, the per-tile seek-and-decode loop below gets slow and memory-hungry enough
// (a second full decoder pipeline on top of the visible preview player) that it's not worth
// taxing the browser for what's ultimately a cosmetic scrubbing aid - the drag handles work
// fine without it.
const FILMSTRIP_MAX_FILE_SIZE_BYTES = 500 * 1024 * 1024 // 500MB

const tiles = ref(Array(FILMSTRIP_TILE_COUNT).fill(null))
const filmstripProgress = ref(0)
const filmstripUnavailable = ref(false)
let filmstripToken = 0

async function generateFilmstrip() {
  const token = ++filmstripToken
  tiles.value = Array(FILMSTRIP_TILE_COUNT).fill(null)
  filmstripProgress.value = 0
  filmstripUnavailable.value = false
  if (!props.videoUrl || !props.videoDuration) return

  if (props.videoFileSize > FILMSTRIP_MAX_FILE_SIZE_BYTES) {
    filmstripProgress.value = 100
    filmstripUnavailable.value = true
    return
  }

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
      filmstripProgress.value = Math.round((captured.length / FILMSTRIP_TILE_COUNT) * 100)
    }
  } catch {
    // No filmstrip preview available (e.g. an unsupported codec) - the drag handles still
    // work without it. Count it as "done" so the loading overlay doesn't spin forever.
    if (token === filmstripToken) {
      filmstripProgress.value = 100
      filmstripUnavailable.value = true
    }
  }
}

watch(() => [props.videoUrl, props.videoDuration, props.videoFileSize], generateFilmstrip, { immediate: true })
</script>

<template>
  <div class="space-y-3">
    <div class="flex items-baseline justify-between text-sm">
      <span class="font-medium">Trim clip</span>
      <span class="text-muted-foreground">
        Selected
        <span class="font-medium text-foreground tabular-nums">{{ formatDuration(Math.max(0, end - start)) }}</span>
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
          :class="{ 'animate-pulse bg-white/10': !tile && !filmstripUnavailable }"
          :style="tile ? { backgroundImage: `url(${tile})` } : undefined"
        />
      </div>

      <div class="pointer-events-none absolute inset-y-0 left-0 bg-black/60" :style="{ width: startPercent + '%' }" />
      <div class="pointer-events-none absolute inset-y-0 right-0 bg-black/60" :style="{ width: 100 - endPercent + '%' }" />

      <div
        v-if="filmstripProgress < 100"
        class="pointer-events-none absolute inset-0 z-[5] flex items-center justify-center bg-black/50 text-xs font-medium tabular-nums text-white"
      >
        Loading preview {{ filmstripProgress }}%
      </div>
      <div
        v-else-if="filmstripUnavailable"
        class="pointer-events-none absolute inset-0 z-[5] flex items-center justify-center text-[11px] text-white/50"
      >
        Preview unavailable for this file
      </div>

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
        :aria-valuenow="start"
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
        :aria-valuenow="end"
        :style="{ left: endPercent + '%' }"
        @pointerdown="startDrag('end', $event)"
        @pointermove="onDragMove"
        @pointerup="endDrag"
        @pointercancel="endDrag"
      >
        <span class="h-4 w-0.5 rounded-full bg-primary-foreground/80" />
      </div>
    </div>

    <div class="flex items-center justify-between text-sm text-muted-foreground">
      <span>Start <span class="font-medium text-foreground tabular-nums">{{ formatDuration(start) }}</span></span>
      <span>End <span class="font-medium text-foreground tabular-nums">{{ formatDuration(end) }}</span></span>
    </div>
  </div>
</template>
