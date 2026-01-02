<script setup>
import { ref, computed, onMounted, watch } from 'vue'

const props = defineProps({
  videoId: String,
})

const emit = defineEmits(['video-loaded', 'timestamp-copied'])

const videoElement = ref(null)
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

const togglePlayPause = () => {
  isPlaying.value ? pause() : play()
}

const seekTo = (time) => {
  if (videoElement.value) {
    videoElement.value.currentTime = time
  }
}

const handleProgressClick = (e) => {
  const rect = e.currentTarget.getBoundingClientRect()
  const percent = (e.clientX - rect.left) / rect.width
  seekTo(percent * duration.value)
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

  return () => {
    window.removeEventListener('hashchange', handleHashChange)
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
  <div class="w-full bg-black rounded-lg overflow-hidden">
    <!-- Video Container -->
    <div class="relative bg-black aspect-video flex items-center justify-center">
      <video
        ref="videoElement"
        class="w-full h-full"
        @loadedmetadata="onLoadedMetadata"
        @timeupdate="onTimeUpdate"
        @play="onPlayStateChange"
        @pause="onPlayStateChange"
      >
        <source v-if="videoUrl" :src="videoUrl" type="video/mp4" />
        Your browser does not support the video tag.
      </video>
    </div>

    <!-- Controls -->
    <div class="bg-gray-900 p-4 space-y-3">
      <!-- Progress Bar -->
      <div
        class="h-1 bg-gray-700 rounded cursor-pointer hover:h-2 transition-all group"
        @click="handleProgressClick"
      >
        <div class="h-full bg-red-600 rounded" :style="{ width: progressPercent + '%' }"></div>
      </div>

      <!-- Time Display and Controls -->
      <div class="flex items-center gap-4">
        <!-- Play/Pause Button -->
        <button
          @click="togglePlayPause"
          class="p-2 rounded hover:bg-gray-800 transition-colors"
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
        <div class="flex-1 text-white text-sm font-mono">
          {{ formattedCurrentTime }} / {{ formattedDuration }}
        </div>

        <!-- Copy Timestamp Controls -->
        <div class="flex items-center gap-3">
          <label class="flex items-center text-sm text-gray-300 gap-2">
            <input
              type="checkbox"
              v-model="attachTimestamp"
              class="h-4 w-4 text-red-600 bg-gray-800 border-gray-700 rounded focus:ring-0"
            />
            <span class="select-none">Attach timestamp</span>
          </label>

          <button
            @click="copyTimestampLink"
            class="px-3 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded transition-colors flex items-center gap-2"
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
