<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { useFullscreen } from '@vueuse/core'
import 'hls-video-element'
import { Play, Pause, Maximize, Minimize, Loader2, SkipBack, SkipForward } from '@lucide/vue'
import { useVideoPlayer } from '@/composables/useVideoPlayer.js'
import { formatDuration } from '@/composables/useDuration.js'
import VideoSeekBar from './video-player/VideoSeekBar.vue'
import VideoVolumeControl from './video-player/VideoVolumeControl.vue'
import PlaybackRateMenu from './video-player/PlaybackRateMenu.vue'
import { playerButtonClass } from './video-player/playerButtonClass.js'

const CINEMA_MODE_STORAGE_KEY = 'cinema-mode'
const SEEK_STEP_SMALL = 5
const SEEK_STEP_LARGE = 10
const VOLUME_STEP = 0.05
const CONTROLS_HIDE_DELAY = 2500
const PREVENT_DEFAULT_KEYS = new Set([' ', 'arrowleft', 'arrowright', 'arrowup', 'arrowdown', 'home', 'end'])

const props = defineProps({
  src: {
    type: String,
    required: true,
  },
  placeholder: {
    type: String,
    default: null,
  },
})

const emit = defineEmits(['loaded', 'toggleCinemaMode'])

const playerRoot = ref(null)
const refVideo = ref(null)
const videoLoaded = ref(false)
const controlsVisible = ref(true)
const isCinemaMode = ref(localStorage.getItem(CINEMA_MODE_STORAGE_KEY) === 'true')

const isHLS = computed(() => props.src.toLowerCase().endsWith('.m3u8'))

const {
  isPlaying,
  isBuffering,
  currentTime,
  duration,
  bufferedEnd,
  volume,
  muted,
  playbackRate,
  togglePlay,
  seekTo,
  seekBy,
  setVolume,
  toggleMute,
  setPlaybackRate,
} = useVideoPlayer(refVideo, {
  onLoadedData: () => {
    videoLoaded.value = true
    emit('loaded')
  },
})

const { isFullscreen, toggle: toggleFullscreen } = useFullscreen(playerRoot)

defineExpose({
  goToTime,
  currentTime,
  refVideo,
})

function goToTime(time) {
  seekTo(Number(time))
}

let hideTimer = null

function scheduleHide() {
  clearTimeout(hideTimer)
  if (!isPlaying.value) return
  hideTimer = setTimeout(() => {
    controlsVisible.value = false
  }, CONTROLS_HIDE_DELAY)
}

function showControls() {
  controlsVisible.value = true
  scheduleHide()
}

function holdControls() {
  clearTimeout(hideTimer)
}

// Keep the bar visible whenever the video isn't actively playing (paused/ended/loading).
watch(isPlaying, (playing) => {
  if (playing) {
    scheduleHide()
  } else {
    clearTimeout(hideTimer)
    controlsVisible.value = true
  }
})

onMounted(() => {
  // Tell the parent the persisted cinema-mode state right away, since it otherwise only
  // learns about it from a user-driven toggle.
  emit('toggleCinemaMode', isCinemaMode.value)
})

onBeforeUnmount(() => clearTimeout(hideTimer))

function onVideoClick() {
  togglePlay()
  showControls()
}

function toggleCinemaMode() {
  isCinemaMode.value = !isCinemaMode.value
  localStorage.setItem(CINEMA_MODE_STORAGE_KEY, String(isCinemaMode.value))
  emit('toggleCinemaMode', isCinemaMode.value)
}

function onMouseLeaveRoot() {
  if (isPlaying.value) controlsVisible.value = false
}

function isTypingTarget(target) {
  return target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA' || target?.isContentEditable
}

function onKeydown(event) {
  if (isTypingTarget(event.target)) return

  const key = event.key.toLowerCase()
  let handled = true

  if (key === ' ' || key === 'k') togglePlay()
  else if (key === 'arrowleft') seekBy(-SEEK_STEP_SMALL)
  else if (key === 'arrowright') seekBy(SEEK_STEP_SMALL)
  else if (key === 'j') seekBy(-SEEK_STEP_LARGE)
  else if (key === 'l') seekBy(SEEK_STEP_LARGE)
  else if (key === 'arrowup') setVolume(Math.min(1, (muted.value ? 0 : volume.value) + VOLUME_STEP))
  else if (key === 'arrowdown') setVolume(Math.max(0, (muted.value ? 0 : volume.value) - VOLUME_STEP))
  else if (key === 'm') toggleMute()
  else if (key === 'f') toggleFullscreen()
  else if (key === 'c') toggleCinemaMode()
  else if (key === 'home') seekTo(0)
  else if (key === 'end') seekTo(duration.value)
  else if (/^[0-9]$/.test(event.key)) seekTo((Number(event.key) / 10) * duration.value)
  else handled = false

  if (!handled) return
  if (PREVENT_DEFAULT_KEYS.has(key)) event.preventDefault()
  showControls()
}
</script>

<template>
  <div
    ref="playerRoot"
    class="player-root relative h-full w-full overflow-hidden bg-black"
    :class="{ 'cursor-none': isPlaying && !controlsVisible }"
    tabindex="0"
    role="region"
    aria-label="Video player"
    @keydown="onKeydown"
    @mousemove="showControls"
    @mouseleave="onMouseLeaveRoot"
  >
    <!--
      The media element stays mounted (just hidden) while loading, rather than behind v-if,
      since it's the loadeddata event it fires that flips videoLoaded to true in the first place.
    -->
    <video
      v-if="!isHLS"
      v-show="videoLoaded"
      ref="refVideo"
      :src="props.src"
      :poster="placeholder"
      class="h-full w-full object-contain"
      playsinline
      @click="onVideoClick"
    ></video>
    <hls-video
      v-else
      v-show="videoLoaded"
      ref="refVideo"
      :src="props.src"
      :poster="placeholder"
      crossorigin
      playsinline
      class="h-full w-full object-contain"
      @click="onVideoClick"
    ></hls-video>

    <!-- Loading state -->
    <div
      v-if="!videoLoaded"
      class="absolute inset-0 flex items-center justify-center bg-muted bg-cover bg-center"
      :style="placeholder ? { backgroundImage: `url(${placeholder})` } : undefined"
    >
      <div class="absolute inset-0" :class="placeholder ? 'bg-black/40' : ''"></div>
      <div class="relative text-center">
        <div class="border-primary mb-4 inline-block h-8 w-8 animate-spin rounded-full border-b-2"></div>
        <p class="text-sm" :class="placeholder ? 'text-gray-100' : 'text-muted-foreground'">Loading video...</p>
      </div>
    </div>

    <template v-else>
      <!-- Buffering spinner -->
      <div v-if="isBuffering" class="pointer-events-none absolute inset-0 flex items-center justify-center">
        <Loader2 class="size-12 animate-spin text-white/90" />
      </div>

      <!-- Big center play button while paused -->
      <button
        v-else-if="!isPlaying"
        type="button"
        class="absolute inset-0 flex items-center justify-center"
        title="Play"
        @click="onVideoClick"
      >
        <span
          class="flex size-16 items-center justify-center rounded-full bg-black/50 text-white transition-transform hover:scale-105"
        >
          <Play class="size-8 translate-x-0.5" fill="currentColor" />
        </span>
      </button>

      <!-- Controls bar -->
      <div
        class="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/85 via-black/40 to-transparent pt-8 transition-opacity duration-200"
        :class="controlsVisible ? 'opacity-100' : 'pointer-events-none opacity-0'"
        @mouseenter="holdControls"
        @mouseleave="scheduleHide"
      >
        <div class="px-2">
          <VideoSeekBar
            :current-time="currentTime"
            :duration="duration"
            :buffered-end="bufferedEnd"
            @seek="seekTo"
            @scrub-start="holdControls"
            @scrub-end="scheduleHide"
          />
        </div>
        <div class="flex items-center gap-0.5 px-1.5 pb-1.5">
          <button type="button" :class="playerButtonClass" :title="isPlaying ? 'Pause' : 'Play'" @click="togglePlay">
            <Pause v-if="isPlaying" class="size-[18px]" fill="currentColor" />
            <Play v-else class="size-[18px]" fill="currentColor" />
          </button>
          <button
            type="button"
            :class="[playerButtonClass, 'skip-btn']"
            title="Back 10 seconds"
            @click="seekBy(-SEEK_STEP_LARGE)"
          >
            <SkipBack class="size-4" />
          </button>
          <button
            type="button"
            :class="[playerButtonClass, 'skip-btn']"
            title="Forward 10 seconds"
            @click="seekBy(SEEK_STEP_LARGE)"
          >
            <SkipForward class="size-4" />
          </button>

          <VideoVolumeControl :volume="volume" :muted="muted" @update:volume="setVolume" @toggle-mute="toggleMute" />

          <span class="time-display ml-1 text-xs tabular-nums text-white/90 select-none">
            {{ formatDuration(currentTime) }} / {{ formatDuration(duration) }}
          </span>

          <div class="flex-1"></div>

          <PlaybackRateMenu :rate="playbackRate" @update:rate="setPlaybackRate" />

          <button
            type="button"
            :class="[playerButtonClass, 'hidden sm:inline-flex']"
            :title="isCinemaMode ? 'Exit cinema mode' : 'Enter cinema mode'"
            @click="toggleCinemaMode"
          >
            <svg v-if="isCinemaMode" viewBox="0 0 24 24" class="size-[18px]" fill="currentColor" aria-hidden="true">
              <path d="M2 4v16h20V4H2zm18 14H4V6h16v12z" />
              <path d="M6 8h12v8H6z" />
            </svg>
            <svg v-else viewBox="0 0 24 24" class="size-[18px]" fill="currentColor" aria-hidden="true">
              <path d="M3 5v14h18V5H3zm16 12H5V7h14v10z" />
              <path d="M7 9h10v6H7z" />
            </svg>
          </button>

          <button
            type="button"
            :class="playerButtonClass"
            :title="isFullscreen ? 'Exit fullscreen' : 'Fullscreen'"
            @click="toggleFullscreen"
          >
            <Minimize v-if="isFullscreen" class="size-[18px]" />
            <Maximize v-else class="size-[18px]" />
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.player-root {
  container-type: inline-size;
  container-name: player;
}

@container player (max-width: 340px) {
  .skip-btn {
    display: none;
  }
}

@container player (max-width: 260px) {
  .time-display {
    display: none;
  }
}
</style>
