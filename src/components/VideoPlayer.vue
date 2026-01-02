<script setup>
import { ref, computed, onMounted, watch } from 'vue'

const props = defineProps({
  videoId: String,
})

const emit = defineEmits(['video-loaded', 'timestamp-copied'])

const videoElement = ref(null)
const wrapperRef = ref(null)
const isPlaying = ref(false)
const currentTime = ref(0)
const duration = ref(0)
const showCopyFeedback = ref(false)
const videoWidth = ref(0)
const videoHeight = ref(0)
const fileSize = ref(0)
const attachTimestamp = ref(true)
const STORAGE_KEY = 'clipviewer.attachTimestamp'

const videoUrl = computed(() => {
  if (!props.videoId) return ''
  if (props.videoId.startsWith('http://') || props.videoId.startsWith('https://')) {
    return props.videoId
  }
  return props.videoId ? `https://share.ruto.sh/u/${props.videoId}.mp4` : ''
})

const formattedCurrentTime = computed(() => formatTime(currentTime.value))
const formattedDuration = computed(() => formatTime(duration.value))

const isFullscreen = ref(false)
const overlayVisible = ref(false)
const overlayTimer = ref(null)
const OVERLAY_TIMEOUT = 3000
const clickTimer = ref(null)
const SINGLE_CLICK_DELAY = 250
const pendingClickWasPlaying = ref(null)

const progressPercent = computed(() => {
  return duration.value ? (currentTime.value / duration.value) * 100 : 0
})

const formatTime = (seconds) => {
  if (!seconds || !isFinite(seconds)) return '0:00'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = Math.floor(seconds % 60)

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }
  return `${minutes}:${String(secs).padStart(2, '0')}`
}

const play = () => {
  videoElement.value?.play()
}

const pause = () => {
  videoElement.value?.pause()
}

const togglePlayPause = (e) => {
  // If called without an event (programmatic), act immediately
  if (!e) {
    isPlaying.value ? pause() : play()
    return
  }

  // If this is the second click of a double-click, cancel the pending
  // single-click action and do nothing (double-click handler will run).
  if (e.detail === 2) {
    if (clickTimer.value) {
      clearTimeout(clickTimer.value)
      clickTimer.value = null
    }
    return
  }

  pendingClickWasPlaying.value = isPlaying.value
  isPlaying.value ? pause() : play()

  if (clickTimer.value) {
    clearTimeout(clickTimer.value)
    clickTimer.value = null
  }

  clickTimer.value = setTimeout(() => {
    clickTimer.value = null
  }, SINGLE_CLICK_DELAY)
}

const seekTo = (time) => {
  if (videoElement.value) {
    const wasPlaying = !videoElement.value.paused
    videoElement.value.currentTime = time
    // Resume playback if it was playing before the seek
    if (wasPlaying) {
      videoElement.value.play().catch(() => {
        // Ignore autoplay errors
      })
    }
  }
}

const handleProgressClick = (e) => {
  const rect = e.currentTarget.getBoundingClientRect()
  const percent = (e.clientX - rect.left) / rect.width
  const wasPlaying = !videoElement.value.paused
  seekTo(percent * duration.value)
  // Ensure video continues playing if it was before
  if (wasPlaying) {
    videoElement.value.play().catch(() => {
      // Ignore autoplay errors
    })
  }
}

const copyTimestampLink = () => {
  const baseUrl = window.location.origin + window.location.pathname
  let url = baseUrl
  if (attachTimestamp.value) {
    url += `#t=${Math.floor(currentTime.value)}`
  }
  navigator.clipboard.writeText(url)

  showCopyFeedback.value = true
  setTimeout(() => {
    showCopyFeedback.value = false
  }, 2000)

  emit('timestamp-copied', currentTime.value)
}

const onLoadedMetadata = async () => {
  await fetchFileSize()
  duration.value = videoElement.value?.duration || 0
  videoWidth.value = videoElement.value?.videoWidth || 0
  videoHeight.value = videoElement.value?.videoHeight || 0
  emit('video-loaded', {
    duration: duration.value,
    width: videoWidth.value,
    height: videoHeight.value,
    fileSize: fileSize.value,
  })
}

const toggleFullscreen = async () => {
  const el = wrapperRef.value || videoElement.value || document.documentElement
  try {
    if (!isFullscreen.value) {
      if (el.requestFullscreen) await el.requestFullscreen()
      else if (el.webkitRequestFullscreen) el.webkitRequestFullscreen()
      else if (el.mozRequestFullScreen) el.mozRequestFullScreen()
    } else {
      if (document.exitFullscreen) await document.exitFullscreen()
      else if (document.webkitExitFullscreen) document.webkitExitFullscreen()
      else if (document.mozCancelFullScreen) document.mozCancelFullScreen()
    }
  } catch (e) {
    console.warn('Fullscreen toggle failed', e)
  }
}

const onFullScreenChange = () => {
  isFullscreen.value = !!(
    document.fullscreenElement ||
    document.webkitFullscreenElement ||
    document.mozFullScreenElement
  )
  if (isFullscreen.value) {
    // show overlay briefly when entering fullscreen
    overlayVisible.value = true
    startOverlayTimer()
  } else {
    overlayVisible.value = false
    clearOverlayTimer()
  }
}

const startOverlayTimer = () => {
  if (overlayTimer.value) clearTimeout(overlayTimer.value)
  overlayTimer.value = setTimeout(() => {
    overlayVisible.value = false
    overlayTimer.value = null
  }, OVERLAY_TIMEOUT)
}

const clearOverlayTimer = () => {
  if (overlayTimer.value) {
    clearTimeout(overlayTimer.value)
    overlayTimer.value = null
  }
}

const showOverlay = () => {
  overlayVisible.value = true
  startOverlayTimer()
}

const hideOverlay = () => {
  overlayVisible.value = false
  clearOverlayTimer()
}

const onMouseMove = () => {
  // when in fullscreen, any mouse movement should reveal overlay briefly
  if (isFullscreen.value) {
    overlayVisible.value = true
    startOverlayTimer()
    return
  }

  // when not fullscreen, reset hide timer if overlay is already visible
  if (overlayVisible.value) {
    startOverlayTimer()
  }
}

const fetchFileSize = async () => {
  console.log('Fetching file size for', videoUrl.value)
  try {
    const response = await fetch(videoUrl.value, { method: 'HEAD' })
    const size = response.headers.get('content-length')
    if (size) {
      fileSize.value = parseInt(size, 10)
    }
  } catch (error) {
    console.warn('Could not fetch video file size:', error)
  }
}

const onTimeUpdate = () => {
  currentTime.value = videoElement.value?.currentTime || 0
}

const onPlayStateChange = () => {
  isPlaying.value = !videoElement.value?.paused
}

const SEEK_STEP = 5 // seconds

const handleVideoDoubleClick = (e) => {
  if (!videoElement.value) return
  const wasPlaying = pendingClickWasPlaying.value
  const rect = videoElement.value.getBoundingClientRect()
  const x = e.clientX - rect.left
  const isLeftHalf = x < rect.width / 2

  if (isLeftHalf) {
    // Seek backward 10 seconds
    seekTo(Math.max(0, currentTime.value - SEEK_STEP))
  } else {
    // Seek forward 10 seconds
    seekTo(Math.min(duration.value, currentTime.value + SEEK_STEP))
  }
  // Resume playback if it was playing before the seek
  if (wasPlaying) {
    videoElement.value.play().catch(() => {
      // Ignore autoplay errors
    })
  }
}

// Handle URL hash for timestamp seeking
const handleHashChange = () => {
  const hash = window.location.hash
  const timeMatch = hash.match(/t=(\d+)/)
  if (timeMatch && videoElement.value) {
    const seconds = parseInt(timeMatch[1], 10)
    seekTo(seconds)
  }
}

onMounted(async () => {
  // initialize persisted checkbox state
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    attachTimestamp.value = stored === null ? true : stored === 'true'
  } catch {
    attachTimestamp.value = true
  }

  window.addEventListener('hashchange', handleHashChange)
  handleHashChange()
  // fullscreen change listener
  document.addEventListener('fullscreenchange', onFullScreenChange)
  document.addEventListener('webkitfullscreenchange', onFullScreenChange)
  document.addEventListener('mozfullscreenchange', onFullScreenChange)
  return () => {
    window.removeEventListener('hashchange', handleHashChange)
    document.removeEventListener('fullscreenchange', onFullScreenChange)
    document.removeEventListener('webkitfullscreenchange', onFullScreenChange)
    document.removeEventListener('mozfullscreenchange', onFullScreenChange)
    if (overlayTimer.value) {
      clearTimeout(overlayTimer.value)
      overlayTimer.value = null
    }
    if (clickTimer.value) {
      clearTimeout(clickTimer.value)
      clickTimer.value = null
    }
  }
})

watch(
  () => props.videoId,
  async () => {
    if (videoElement.value) {
      videoElement.value.load()
    }
  },
)

// persist attachTimestamp when it changes
watch(
  () => attachTimestamp.value,
  (val) => {
    try {
      localStorage.setItem(STORAGE_KEY, val ? 'true' : 'false')
    } catch {
      /* ignore */
    }
  },
)
</script>

<template>
  <div ref="wrapperRef" class="w-full bg-black rounded-lg overflow-hidden relative">
    <!-- Video Container -->
    <div
      class="relative bg-black aspect-video flex items-center justify-center"
      @mouseenter="showOverlay"
      @mouseleave="hideOverlay"
      @mousemove="onMouseMove"
    >
      <video
        ref="videoElement"
        class="w-full h-full touch-none cursor-pointer"
        @loadedmetadata="onLoadedMetadata"
        @timeupdate="onTimeUpdate"
        @play="onPlayStateChange"
        @pause="onPlayStateChange"
        @click="togglePlayPause"
        @dblclick="handleVideoDoubleClick"
      >
        <source v-if="videoUrl" :src="videoUrl" type="video/mp4" />
        Your browser does not support the video tag.
      </video>

      <!-- Mobile tap zones for seek prev/next (left/right sides) -->
      <div class="absolute inset-0 flex pointer-events-none md:hidden">
        <div
          class="w-1/3 cursor-pointer pointer-events-auto"
          @dblclick="seekTo(Math.max(0, currentTime - SEEK_STEP))"
        ></div>
        <div class="w-1/3 pointer-events-none"></div>
        <div
          class="w-1/3 cursor-pointer pointer-events-auto"
          @dblclick="seekTo(Math.min(duration, currentTime + SEEK_STEP))"
        ></div>
      </div>

      <!-- Fullscreen overlay (shows on hover) -->
      <div v-show="overlayVisible" class="absolute inset-0 pointer-events-none">
        <div class="absolute left-0 right-0 bottom-0 p-3 pointer-events-auto">
          <!-- Interactive controls: always show play/pause, progress, time on hover -->
          <div v-show="isFullscreen" class="w-full flex justify-center">
            <div
              class="bg-black/60 rounded-lg p-2 md:p-3 flex items-center gap-3 w-full max-w-2xl px-4"
            >
              <!-- Play/Pause -->
              <button
                @click="togglePlayPause()"
                class="p-2 rounded bg-black/30 text-white flex-shrink-0"
              >
                <svg v-if="isPlaying" class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M6 4h4v16H6V4zm8 0h4v16h-4V4z" />
                </svg>
                <svg v-else class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M8 5v14l11-7z" />
                </svg>
              </button>

              <!-- Progress bar (click to seek) -->
              <div class="flex-1 min-w-0">
                <div
                  class="h-2 bg-gray-700 rounded cursor-pointer"
                  @click.stop="handleProgressClick"
                >
                  <div
                    class="h-full bg-red-600 rounded"
                    :style="{ width: progressPercent + '%' }"
                  ></div>
                </div>
              </div>

              <!-- Time display -->
              <div class="text-white text-sm font-mono whitespace-nowrap flex-shrink-0">
                {{ formattedCurrentTime }} / {{ formattedDuration }}
              </div>
            </div>
          </div>

          <!-- Bottom-right fullscreen button (next to interactive controls) -->
          <div class="absolute right-3 bottom-3 pointer-events-auto">
            <button
              @click.prevent="toggleFullscreen"
              class="p-2 rounded bg-black/60 text-white hover:bg-black/70"
            >
              <svg class="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M8 3H5a2 2 0 00-2 2v3m0 8v3a2 2 0 002 2h3m8-16h3a2 2 0 012 2v3M16 21h3a2 2 0 002-2v-3"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Controls -->
    <div v-show="!isFullscreen" class="bg-gray-900 p-4 space-y-3">
      <!-- Progress Bar -->
      <div
        class="h-1 bg-gray-700 rounded cursor-pointer hover:h-2 transition-all group"
        @click="handleProgressClick"
      >
        <div class="h-full bg-red-600 rounded" :style="{ width: progressPercent + '%' }"></div>
      </div>

      <!-- Time Display and Controls -->
      <div class="flex flex-col md:flex-row md:items-center gap-2 md:gap-4">
        <!-- Play/Pause Button -->
        <button
          @click="togglePlayPause()"
          class="p-2 rounded hover:bg-gray-800 transition-colors flex-shrink-0 w-fit"
          :aria-label="isPlaying ? 'Pause' : 'Play'"
        >
          <svg v-if="isPlaying" class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
            <path d="M6 4h4v16H6V4zm8 0h4v16h-4V4z" />
          </svg>
          <svg v-else class="w-6 h-6 text-white" fill="currentColor" viewBox="0 0 24 24">
            <path d="M8 5v14l11-7z" />
          </svg>
        </button>

        <!-- Time Display -->
        <div class="text-white text-xs md:text-sm font-mono flex-shrink-0 md:flex-1">
          {{ formattedCurrentTime }} / {{ formattedDuration }}
        </div>

        <!-- Copy Timestamp Controls -->
        <div class="flex flex-col sm:flex-row items-start sm:items-center gap-2">
          <label class="flex items-center text-xs md:text-sm text-gray-300 gap-2 flex-shrink-0">
            <input
              type="checkbox"
              v-model="attachTimestamp"
              class="h-4 w-4 text-red-600 bg-gray-800 border-gray-700 rounded focus:ring-0"
            />
            <span class="select-none">Attach timestamp</span>
          </label>

          <button
            @click="copyTimestampLink"
            class="px-3 py-2 bg-red-600 hover:bg-red-700 text-white text-xs md:text-sm rounded transition-colors flex items-center gap-2 w-full sm:w-auto justify-center"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.658 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
              />
            </svg>
            {{ showCopyFeedback ? 'Copied!' : 'Copy Link' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
