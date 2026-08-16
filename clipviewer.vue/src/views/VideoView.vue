<script setup>
import { onMounted, ref, onUnmounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/services/api'
import VideoPlayer from '@/components/VideoPlayer.vue'
import VideoInfo from '@/components/VideoInfo.vue'
import { Card } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { AlertCircle, VideoOff, GripVertical } from '@lucide/vue'

const route = useRoute()
const router = useRouter()
const video = ref(null)
const loading = ref(true)
const videoPlayer = ref(null)
const pollingInterval = ref(null)
const error = ref('')
const savingVideo = ref(false)
const updateError = ref('')

const videoSource = ref(null)

const PLAYER_WIDTH_STORAGE_KEY = 'clipviewer:player-width'
const MIN_PLAYER_WIDTH = 320
// Matches the px-4 (1rem each side) horizontal padding on the page's outer wrapper, which stays
// in place even when the max-width "container" class is dropped for a custom-width player.
const PAGE_EDGE_MARGIN = 32

// Wraps the Alert + Card. While playerWidth is set, this element's own max-w-5xl/7xl (and the
// page's outer max-width) are dropped so it can size up to the browser edge instead of just the
// normal centered column - see the container/containerRef bindings in the template.
const containerRef = ref(null)
const playerWidth = ref(null)
const isResizingPlayer = ref(false)

// Resizing is a desktop-only affordance - matches the `md` breakpoint used for the mobile nav
// in App.vue. On mobile the handle is hidden and any previously-saved desktop width is ignored
// so the player always falls back to the normal responsive layout.
const MOBILE_MEDIA_QUERY = '(max-width: 767px)'
// Read synchronously (rather than waiting for onMounted) so the mobile edge-to-edge layout is
// correct on first paint instead of flashing the desktop layout for a frame.
const isMobile = ref(typeof window.matchMedia === 'function' ? window.matchMedia(MOBILE_MEDIA_QUERY).matches : false)
let mobileMediaQuery = null

function updateIsMobile(event) {
  isMobile.value = event.matches
}

const effectivePlayerWidth = computed(() => (isMobile.value ? null : playerWidth.value))

function readStoredPlayerWidth() {
  const raw = Number(localStorage.getItem(PLAYER_WIDTH_STORAGE_KEY))
  return Number.isFinite(raw) && raw > 0 ? raw : null
}

playerWidth.value = readStoredPlayerWidth()

function onPlayerResizeStart(event) {
  if (event.button !== 0 || !containerRef.value || isMobile.value) return
  event.preventDefault()

  const startX = event.clientX
  const startWidth = containerRef.value.getBoundingClientRect().width
  const maxWidth = window.innerWidth - PAGE_EDGE_MARGIN

  isResizingPlayer.value = true
  document.body.style.cursor = 'ew-resize'
  document.body.style.userSelect = 'none'

  function onMove(moveEvent) {
    const next = startWidth + (moveEvent.clientX - startX)
    playerWidth.value = Math.min(maxWidth, Math.max(MIN_PLAYER_WIDTH, next))
  }

  function onUp() {
    isResizingPlayer.value = false
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
    window.removeEventListener('pointermove', onMove)
    window.removeEventListener('pointerup', onUp)
    if (playerWidth.value) {
      localStorage.setItem(PLAYER_WIDTH_STORAGE_KEY, String(Math.round(playerWidth.value)))
    }
  }

  window.addEventListener('pointermove', onMove)
  window.addEventListener('pointerup', onUp)
}

function resetPlayerWidth() {
  playerWidth.value = null
  localStorage.removeItem(PLAYER_WIDTH_STORAGE_KEY)
}

async function fetchVideo(isPolling = false) {
  try {
    const response = await api.get(`/api/videos/${route.params.videoId}`)
    if (response.status === 200) {
      video.value = response.data
      error.value = ''
      // Update document title with video name
      if (video.value.name) {
        document.title = `${video.value.name} - ClipViewer`
      }
      // Stop polling if video is processed
      if (response.data.processed && pollingInterval.value) {
        clearInterval(pollingInterval.value)
        pollingInterval.value = null
      }

      // Update video source
      if (!isPolling && response.data.processed) {
        videoSource.value = response.data.hlsPlaylistFile
      } else {
        videoSource.value = response.data.sourceVideoFile
      }
    }
  } catch (err) {
    console.error('Failed to fetch video:', err)
    // A 404 is a legitimate "not found" state, handled by the template - don't treat it as an error banner
    if (!isPolling && err.response?.status !== 404) {
      error.value = err.response?.data?.message || 'Failed to load video. Please try again.'
    }
  }
}

function startPolling() {
  if (pollingInterval.value) return

  pollingInterval.value = setInterval(() => {
    if (video.value && !video.value.processed) {
      fetchVideo(true) // Pass true to indicate this is a polling request
    }
  }, 5000) // Poll every 5 seconds
}

async function loadVideo() {
  loading.value = true
  video.value = null
  error.value = ''
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }

  await fetchVideo()

  // Start polling if video is not processed
  if (video.value && !video.value.processed) {
    startPolling()
  }

  loading.value = false
}

onMounted(() => {
  if (typeof window.matchMedia !== 'function') return
  mobileMediaQuery = window.matchMedia(MOBILE_MEDIA_QUERY)
  isMobile.value = mobileMediaQuery.matches
  mobileMediaQuery.addEventListener('change', updateIsMobile)
})

onMounted(loadVideo)

// Vue Router reuses this component instance when navigating between two /clips/:videoId routes,
// so onMounted alone won't re-fire - reload explicitly when the param changes.
watch(
  () => route.params.videoId,
  (newId, oldId) => {
    if (newId !== oldId) loadVideo()
  },
)

onUnmounted(() => {
  if (pollingInterval.value) {
    clearInterval(pollingInterval.value)
    pollingInterval.value = null
  }
  mobileMediaQuery?.removeEventListener('change', updateIsMobile)
})

function onVideoLoaded() {
  // Check if param time is set
  if (route.query.t) {
    videoPlayer.value.goToTime(route.query.t)
  }
}

async function updateVideo(updatedVideo) {
  savingVideo.value = true
  updateError.value = ''
  try {
    const response = await api.put(`/api/videos/${route.params.videoId}`, {
      unlisted: updatedVideo.unlisted,
      name: updatedVideo.name,
      description: updatedVideo.description,
      tags: updatedVideo.tags,
    })

    if (response.status === 200) {
      video.value = response.data
      // Update document title with new video name
      if (video.value.name) {
        document.title = `${video.value.name} - ClipViewer`
      }
    }
  } catch (err) {
    console.error('Failed to update video:', err)
    updateError.value = err.response?.data?.message || 'Failed to save changes. Please try again.'
  } finally {
    savingVideo.value = false
  }
}

async function retryVideo() {
  try {
    const response = await api.post(`/api/videos/${route.params.videoId}/retry`)

    if (response.status === 200) {
      video.value = response.data
      error.value = ''
      startPolling()
    }
  } catch (err) {
    console.error('Failed to retry video:', err)
    error.value = err.response?.data?.message || 'Failed to retry the conversion. Please try again.'
  }
}

async function deleteVideo() {
  try {
    const response = await api.delete(`/api/videos/${route.params.videoId}`)

    if (response.status === 200) {
      router.push('/browse')
    }
  } catch (err) {
    console.error('Failed to delete video:', err)
    error.value = err.response?.data?.message || 'Failed to delete video. Please try again.'
  }
}

async function refreshVideo() {
  loading.value = true
  await fetchVideo()
  loading.value = false
}
</script>

<template>
  <div v-if="loading" :class="isMobile ? '' : 'container mx-auto px-4 py-8'">
    <div v-if="isMobile">
      <Skeleton class="aspect-video w-full mb-4" />
      <div class="px-4">
        <Skeleton class="h-8 w-2/3 mb-2" />
        <Skeleton class="h-4 w-1/3" />
      </div>
    </div>
    <div v-else class="max-w-5xl mx-auto">
      <Skeleton class="aspect-video w-full rounded-lg mb-4" />
      <Skeleton class="h-8 w-2/3 mb-2" />
      <Skeleton class="h-4 w-1/3" />
    </div>
    <p class="sr-only">Loading video...</p>
  </div>

  <div v-else-if="video" :class="isMobile ? '' : ['mx-auto px-4 py-8', { container: !effectivePlayerWidth }]">
    <div
      ref="containerRef"
      :class="[isMobile ? '' : 'mx-auto', !isMobile && !effectivePlayerWidth ? 'max-w-5xl' : '']"
      :style="effectivePlayerWidth ? { width: `${effectivePlayerWidth}px`, maxWidth: '100%' } : undefined"
    >
      <Alert v-if="error" variant="destructive" :class="isMobile ? 'mx-4 mt-4' : 'mb-4'">
        <AlertDescription>{{ error }}</AlertDescription>
      </Alert>
      <Card class="overflow-hidden py-0 gap-0" :class="isMobile ? 'rounded-none border-x-0 border-t-0 shadow-none' : ''">
        <div class="relative aspect-video w-full">
          <VideoPlayer
            ref="videoPlayer"
            :src="videoSource"
            :placeholder="video.thumbnail"
            :scrub-sprite="video.scrubSprite"
            @loaded="onVideoLoaded"
          />
          <div
            v-if="!isMobile"
            class="group absolute top-0 right-0 bottom-20 flex w-4 items-center justify-center cursor-ew-resize touch-none transition-colors hover:bg-white/10"
            title="Drag to resize the player (double-click to reset)"
            @pointerdown="onPlayerResizeStart"
            @dblclick="resetPlayerWidth"
          >
            <div
              class="flex h-12 w-5 items-center justify-center rounded-full bg-black/50 text-white/70 opacity-60 transition-opacity group-hover:opacity-100"
              :class="{ 'opacity-100! bg-black/70!': isResizingPlayer }"
            >
              <GripVertical class="size-3.5" />
            </div>
          </div>
        </div>
        <VideoInfo
          :video="video"
          :videoPlayer="videoPlayer"
          :saving="savingVideo"
          :save-error="updateError"
          @update-video="updateVideo"
          @delete-video="deleteVideo"
          @refresh-video="refreshVideo"
          @retry-video="retryVideo"
        />
      </Card>
    </div>
  </div>

  <div v-else-if="error" class="container mx-auto px-4 py-12 text-center">
    <AlertCircle class="size-16 text-destructive mx-auto mb-4" />
    <p class="text-muted-foreground text-lg mb-2">Unable to load video</p>
    <p class="text-muted-foreground/70">{{ error }}</p>
    <Button variant="link" class="mt-4" @click="refreshVideo">Try again</Button>
  </div>

  <div v-else class="container mx-auto px-4 py-12 text-center">
    <VideoOff class="size-16 text-muted-foreground mx-auto mb-4" />
    <p class="text-muted-foreground text-lg mb-2">Video not found</p>
    <p class="text-muted-foreground/70">The video you're looking for doesn't exist.</p>
    <RouterLink to="/browse" class="mt-4 inline-block text-primary hover:underline">
      ← Back to clips
    </RouterLink>
  </div>
</template>
