<script setup>
import { ref, computed, watch } from 'vue'
import { formatDuration } from '@/composables/useDuration.js'

const props = defineProps({
  currentTime: {
    type: Number,
    default: 0,
  },
  duration: {
    type: Number,
    default: 0,
  },
  bufferedEnd: {
    type: Number,
    default: 0,
  },
  scrubSprite: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['seek', 'scrub-start', 'scrub-end'])

const track = ref(null)
const isDragging = ref(false)
const hoverRatio = ref(null)
const spriteManifest = ref(null)

const progressRatio = computed(() => (props.duration > 0 ? clampRatio(props.currentTime / props.duration) : 0))
const bufferedRatio = computed(() => (props.duration > 0 ? clampRatio(props.bufferedEnd / props.duration) : 0))
const hoverTime = computed(() => (hoverRatio.value === null ? null : hoverRatio.value * props.duration))

// Fetches the sprite's JSON manifest (see Worker's GenerateScrubSprite) once per video. A stale
// in-flight fetch for a previous scrubSprite URL is dropped if the prop changes again before it
// resolves, and any failure (including clips that don't have a sprite yet) just leaves the
// preview thumbnail off - the time-only tooltip below still works either way.
watch(
  () => props.scrubSprite,
  async (url) => {
    spriteManifest.value = null
    if (!url) return
    try {
      const response = await fetch(url)
      if (!response.ok) return
      const manifest = await response.json()
      if (props.scrubSprite === url) spriteManifest.value = manifest
    } catch {
      /* no scrubbing preview available for this clip */
    }
  },
  { immediate: true },
)

const previewTileStyle = computed(() => {
  const manifest = spriteManifest.value
  if (!manifest || hoverTime.value === null) return null

  const index = Math.min(manifest.count - 1, Math.max(0, Math.floor(hoverTime.value / manifest.interval)))
  const col = index % manifest.columns
  const row = Math.floor(index / manifest.columns)
  const rows = Math.ceil(manifest.count / manifest.columns)
  // The manifest's `image` is a bare filename, colocated with the manifest itself - resolve it
  // against the manifest's own URL rather than assuming any particular public-file prefix.
  const imageUrl = props.scrubSprite.slice(0, props.scrubSprite.lastIndexOf('/') + 1) + manifest.image

  return {
    width: `${manifest.tileWidth}px`,
    height: `${manifest.tileHeight}px`,
    backgroundImage: `url(${imageUrl})`,
    backgroundSize: `${manifest.columns * manifest.tileWidth}px ${rows * manifest.tileHeight}px`,
    backgroundPosition: `-${col * manifest.tileWidth}px -${row * manifest.tileHeight}px`,
  }
})

function clampRatio(ratio) {
  return Math.min(1, Math.max(0, ratio))
}

function ratioFromEvent(event) {
  const rect = track.value.getBoundingClientRect()
  if (rect.width === 0) return 0
  return clampRatio((event.clientX - rect.left) / rect.width)
}

function onPointerMove(event) {
  const ratio = ratioFromEvent(event)
  hoverRatio.value = ratio
  if (isDragging.value) emit('seek', ratio * props.duration)
}

function onPointerDown(event) {
  if (props.duration <= 0 || event.button !== 0) return
  isDragging.value = true
  track.value.setPointerCapture?.(event.pointerId)
  emit('scrub-start')
  onPointerMove(event)
}

function onPointerUp(event) {
  if (!isDragging.value) return
  isDragging.value = false
  track.value.releasePointerCapture?.(event.pointerId)
  emit('scrub-end')
}

function onPointerLeave() {
  if (!isDragging.value) hoverRatio.value = null
}
</script>

<template>
  <div
    ref="track"
    class="group/seek relative flex h-4 w-full cursor-pointer touch-none items-center select-none"
    role="slider"
    aria-label="Seek"
    :aria-valuemin="0"
    :aria-valuemax="duration"
    :aria-valuenow="currentTime"
    @pointerdown="onPointerDown"
    @pointermove="onPointerMove"
    @pointerup="onPointerUp"
    @pointercancel="onPointerUp"
    @pointerleave="onPointerLeave"
  >
    <div
      v-if="hoverTime !== null"
      class="pointer-events-none absolute bottom-full mb-2 flex -translate-x-1/2 flex-col items-center gap-1"
      :style="{ left: `${hoverRatio * 100}%` }"
    >
      <div
        v-if="previewTileStyle"
        class="overflow-hidden rounded border border-white/20 bg-black shadow-lg"
        :style="previewTileStyle"
      ></div>
      <div class="rounded bg-black/90 px-1.5 py-0.5 text-xs whitespace-nowrap text-white">
        {{ formatDuration(hoverTime) }}
      </div>
    </div>

    <div class="relative h-1 w-full rounded-full bg-white/25 transition-[height] group-hover/seek:h-1.5">
      <div class="absolute inset-y-0 left-0 rounded-full bg-white/40" :style="{ width: `${bufferedRatio * 100}%` }" />
      <div class="absolute inset-y-0 left-0 rounded-full bg-primary" :style="{ width: `${progressRatio * 100}%` }" />
      <div
        class="absolute top-1/2 size-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary opacity-0 shadow transition-opacity group-hover/seek:opacity-100"
        :class="{ 'opacity-100': isDragging }"
        :style="{ left: `${progressRatio * 100}%` }"
      />
    </div>
  </div>
</template>
