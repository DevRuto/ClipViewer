<script setup>
import { ref, computed, onBeforeUnmount, watch } from 'vue'
import { useFullscreen } from '@vueuse/core'
import 'hls-video-element'
import { Play, Pause, Maximize, Minimize, Loader2, SkipBack, SkipForward } from '@lucide/vue'
import { useVideoPlayer } from '@/composables/useVideoPlayer.js'
import { formatDuration } from '@/composables/useDuration.js'
import VideoSeekBar from './video-player/VideoSeekBar.vue'
import VideoVolumeControl from './video-player/VideoVolumeControl.vue'
import PlaybackRateMenu from './video-player/PlaybackRateMenu.vue'
import { playerButtonClass } from './video-player/playerButtonClass.js'

const SEEK_STEP_SMALL = 5
const SEEK_STEP_LARGE = 10
const VOLUME_STEP = 0.05
const CONTROLS_HIDE_DELAY = 2500
const PREVENT_DEFAULT_KEYS = new Set([' ', 'arrowleft', 'arrowright', 'arrowup', 'arrowdown', 'home', 'end'])

// Touch gesture tuning: tapping the outer 30% of the player is a candidate for a double-tap
// seek; a second tap in the same zone within the window seeks, otherwise it falls back to a
// normal toggle-play tap. TAP_MOVE_THRESHOLD tells a tap apart from a swipe/scroll started on
// the video so scrolling the page past the player isn't misread as a gesture.
const EDGE_ZONE_RATIO = 0.3
const DOUBLE_TAP_WINDOW = 300
const TAP_MOVE_THRESHOLD = 10
const TAP_MAX_DURATION = 500

const props = defineProps({
  src: {
    type: String,
    required: true,
  },
  placeholder: {
    type: String,
    default: null,
  },
  scrubSprite: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['loaded'])

const playerRoot = ref(null)
const refVideo = ref(null)
const videoLoaded = ref(false)
const controlsVisible = ref(true)

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

onBeforeUnmount(() => {
  clearTimeout(hideTimer)
  clearTapTimer()
  clearTimeout(seekFlashTimer)
})

// Toggles play/pause immediately and returns an undo that reverts just that toggle - used so an
// edge-zone tap/click can apply optimistically and still be cleanly reversed if a second one
// confirms it was actually a double-tap/double-click seek (see resolveZoneGesture).
function toggleSingleClick() {
  togglePlay()
  showControls()
  return () => togglePlay()
}

function onVideoClick() {
  toggleSingleClick()
}

// Mirrors the touch double-tap-edge-seek gesture below for mouse users: a click on the
// left/right edge applies its toggle immediately (see toggleSingleClick) but is reverted if a
// second click lands in the same zone within DOUBLE_TAP_WINDOW, so a double-click purely seeks
// without leaving playback state flipped. Unlike touch (resolveSingleTap), a lone desktop click
// always toggles play/pause unconditionally - mouse users don't have the mobile "big play button
// is the only thing that resumes playback" ambiguity, so there's no need to special-case paused.
function onVideoElementClick(event) {
  resolveZoneGesture(zoneForX(event.clientX, event.currentTarget.getBoundingClientRect()), toggleSingleClick)
}

// While playing, a tap that isn't a confirmed edge double-tap pauses (same as onVideoClick).
// While paused, the big play button is the only thing that resumes playback - a tap anywhere
// else on the player just toggles the controls overlay instead of also restarting playback.
// Returns an undo matching whichever branch it took, for the same optimistic-apply-then-revert
// reason as toggleSingleClick.
function resolveSingleTap() {
  if (isPlaying.value) {
    return toggleSingleClick()
  }
  const wasVisible = controlsVisible.value
  controlsVisible.value = !wasVisible
  return () => {
    controlsVisible.value = wasVisible
  }
}

// Touch gesture handling lives on touchstart/touchend rather than the click handler above so it
// can distinguish a tap from a swipe (a scroll started on the video shouldn't toggle play) and
// so a confirmed tap can suppress the browser's trailing synthetic click - see onVideoTouchEnd.
let touchStartX = 0
let touchStartY = 0
let touchStartTime = 0
let tapTimer = null
let pendingTapZone = null
let pendingUndo = null
const seekFlash = ref(null)
const seekFlashKey = ref(0)
let seekFlashTimer = null

// Clears the pending-second-tap window without undoing anything - the first tap's action (if
// edge-zone) was already applied optimistically and stays committed unless a confirmed double
// follows (see resolveZoneGesture).
function clearTapTimer() {
  clearTimeout(tapTimer)
  tapTimer = null
  pendingTapZone = null
  pendingUndo = null
}

function flashSeek(direction) {
  clearTimeout(seekFlashTimer)
  seekFlash.value = direction
  seekFlashKey.value++
  seekFlashTimer = setTimeout(() => {
    seekFlash.value = null
  }, 600)
}

function zoneForX(clientX, rect) {
  if (rect.width === 0) return 'center'
  const ratio = (clientX - rect.left) / rect.width
  if (ratio < EDGE_ZONE_RATIO) return 'left'
  if (ratio > 1 - EDGE_ZONE_RATIO) return 'right'
  return 'center'
}

// Shared by the touch and mouse gesture handlers: only the left/right edge zones have a
// double-tap/double-click gesture (10s seek). A center zone gesture has nothing to disambiguate,
// so it resolves immediately. An edge zone gesture also resolves immediately (optimistically) -
// `resolveSingle` applies its toggle right away and returns an undo - but stays pending for
// DOUBLE_TAP_WINDOW in case a second tap/click lands in the same zone, in which case the toggle
// is reverted and replaced with a seek. This way a plain click/tap never lags, and only a real
// double confirmation costs a brief (<300ms) play/pause flicker. `resolveSingle` differs between
// touch and mouse since they differ when paused (see callers).
function resolveZoneGesture(zone, resolveSingle) {
  if (zone === 'center') {
    clearTapTimer()
    resolveSingle()
    return
  }

  if (tapTimer && zone === pendingTapZone) {
    const undo = pendingUndo
    clearTapTimer()
    undo?.()
    seekBy(zone === 'left' ? -SEEK_STEP_LARGE : SEEK_STEP_LARGE)
    flashSeek(zone === 'left' ? 'back' : 'forward')
    showControls()
    return
  }

  clearTapTimer()
  pendingTapZone = zone
  pendingUndo = resolveSingle()
  tapTimer = setTimeout(() => {
    tapTimer = null
    pendingTapZone = null
    pendingUndo = null
  }, DOUBLE_TAP_WINDOW)
}

function onVideoTouchStart(event) {
  const touch = event.touches?.[0]
  if (!touch) return
  touchStartX = touch.clientX
  touchStartY = touch.clientY
  touchStartTime = Date.now()
}

function onVideoTouchEnd(event) {
  const touch = event.changedTouches?.[0]
  if (!touch) return

  const movedTooFar =
    Math.abs(touch.clientX - touchStartX) > TAP_MOVE_THRESHOLD ||
    Math.abs(touch.clientY - touchStartY) > TAP_MOVE_THRESHOLD
  if (movedTooFar || Date.now() - touchStartTime > TAP_MAX_DURATION) {
    // A real scroll/swipe - leave it to the browser and drop any pending double-tap window.
    clearTapTimer()
    return
  }

  // Confirmed tap: handle it here and suppress the trailing synthetic click so it isn't
  // double-handled by the @click listener below.
  event.preventDefault()
  resolveZoneGesture(zoneForX(touch.clientX, event.currentTarget.getBoundingClientRect()), resolveSingleTap)
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
      @click="onVideoElementClick"
      @touchstart="onVideoTouchStart"
      @touchend="onVideoTouchEnd"
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
      @click="onVideoElementClick"
      @touchstart="onVideoTouchStart"
      @touchend="onVideoTouchEnd"
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

      <!-- Big center play button while paused. Sized to just the button (not the full player)
           so a tap elsewhere only toggles the controls overlay instead of also resuming
           playback - see resolveSingleTap. -->
      <div
        v-else-if="!isPlaying"
        class="pointer-events-none absolute inset-0 flex items-center justify-center transition-opacity duration-200"
        :class="controlsVisible ? 'opacity-100' : 'opacity-0'"
      >
        <button
          type="button"
          class="pointer-events-auto flex size-16 items-center justify-center rounded-full bg-black/50 text-white transition-transform hover:scale-105"
          title="Play"
          @click="onVideoClick"
        >
          <Play class="size-8 translate-x-0.5" fill="currentColor" />
        </button>
      </div>

      <!-- Gesture-seek flash: brief +/-10s indicator after a double-tap/double-click on the left/right edge -->
      <div
        v-if="seekFlash"
        :key="seekFlashKey"
        class="seek-flash-indicator pointer-events-none absolute inset-y-0 flex w-2/5 items-center justify-center"
        :class="seekFlash === 'back' ? 'left-0' : 'right-0'"
      >
        <span class="flex flex-col items-center gap-1 rounded-full bg-black/60 px-4 py-3 text-white">
          <component :is="seekFlash === 'back' ? SkipBack : SkipForward" class="size-6" />
          <span class="text-xs font-medium">10s</span>
        </span>
      </div>

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
            :scrub-sprite="scrubSprite"
            @seek="seekTo"
            @scrub-start="holdControls"
            @scrub-end="scheduleHide"
          />
        </div>
        <div class="flex items-center gap-0.5 pointer-coarse:gap-1.5 px-1.5 pointer-coarse:px-2 pb-1.5 pointer-coarse:pb-2">
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

.seek-flash-indicator {
  animation: seek-flash-fade 600ms ease-out;
}

@keyframes seek-flash-fade {
  0% {
    opacity: 0;
    transform: scale(0.9);
  }
  20% {
    opacity: 1;
    transform: scale(1);
  }
  100% {
    opacity: 0;
    transform: scale(1);
  }
}

@container player (max-width: 260px) {
  .time-display {
    display: none;
  }
}
</style>
