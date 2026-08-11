import { ref, watch, onBeforeUnmount } from 'vue'

const VOLUME_STORAGE_KEY = 'clipviewer:player-volume'
const MUTED_STORAGE_KEY = 'clipviewer:player-muted'

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value))
}

function readStoredVolume() {
  const raw = Number(localStorage.getItem(VOLUME_STORAGE_KEY))
  return Number.isFinite(raw) ? clamp(raw, 0, 1) : 1
}

// Drives playback state off a swappable HTMLMediaElement ref (VideoPlayer swaps between
// <video> and <hls-video> depending on source type, so listeners must follow the ref).
export function useVideoPlayer(mediaRef, { onLoadedData } = {}) {
  const isPlaying = ref(false)
  const isBuffering = ref(false)
  const currentTime = ref(0)
  const duration = ref(0)
  const bufferedEnd = ref(0)
  const volume = ref(readStoredVolume())
  const muted = ref(localStorage.getItem(MUTED_STORAGE_KEY) === 'true')
  const playbackRate = ref(1)

  function syncBuffered(el) {
    if (!el.buffered || el.buffered.length === 0) return
    // .end() throws if the index is stale by the time this runs - buffered ranges can
    // change between the length check and the read.
    try {
      bufferedEnd.value = el.buffered.end(el.buffered.length - 1)
    } catch {
      /* ignore */
    }
  }

  const handlers = {
    play: () => {
      isPlaying.value = true
    },
    pause: () => {
      isPlaying.value = false
    },
    ended: () => {
      isPlaying.value = false
    },
    waiting: () => {
      isBuffering.value = true
    },
    playing: () => {
      isBuffering.value = false
    },
    canplay: () => {
      isBuffering.value = false
    },
    timeupdate: (event) => {
      currentTime.value = event.target.currentTime
    },
    durationchange: (event) => {
      duration.value = Number.isFinite(event.target.duration) ? event.target.duration : 0
    },
    progress: (event) => syncBuffered(event.target),
    volumechange: (event) => {
      volume.value = event.target.volume
      muted.value = event.target.muted
    },
    ratechange: (event) => {
      playbackRate.value = event.target.playbackRate
    },
    loadeddata: () => onLoadedData?.(),
  }

  function attach(el) {
    if (!el) return
    el.volume = volume.value
    el.muted = muted.value
    el.playbackRate = playbackRate.value
    for (const [event, handler] of Object.entries(handlers)) {
      el.addEventListener(event, handler)
    }
  }

  function detach(el) {
    if (!el) return
    for (const [event, handler] of Object.entries(handlers)) {
      el.removeEventListener(event, handler)
    }
  }

  // sync flush: attach as soon as the template ref is set, rather than waiting for Vue's
  // default pre-render batching - a real <video> firing loadeddata is always async, but tests
  // that dispatch it synchronously right after mount would otherwise race the listener attach.
  watch(
    mediaRef,
    (el, previousEl) => {
      detach(previousEl)
      attach(el)
    },
    { flush: 'sync' },
  )

  onBeforeUnmount(() => detach(mediaRef.value))

  function togglePlay() {
    const el = mediaRef.value
    if (!el) return
    if (el.paused || el.ended) el.play()
    else el.pause()
  }

  function seekTo(time) {
    const el = mediaRef.value
    const target = Number(time)
    if (!el || !Number.isFinite(target)) return
    const max = duration.value > 0 ? duration.value : Infinity
    const clamped = clamp(target, 0, max)
    el.currentTime = clamped
    currentTime.value = clamped
  }

  function seekBy(delta) {
    seekTo((mediaRef.value?.currentTime ?? currentTime.value) + delta)
  }

  function setVolume(value) {
    const el = mediaRef.value
    const clamped = clamp(value, 0, 1)
    volume.value = clamped
    localStorage.setItem(VOLUME_STORAGE_KEY, String(clamped))
    if (!el) return
    el.volume = clamped
    if (clamped > 0 && el.muted) {
      el.muted = false
      muted.value = false
      localStorage.setItem(MUTED_STORAGE_KEY, 'false')
    }
  }

  function toggleMute() {
    const next = !muted.value
    muted.value = next
    localStorage.setItem(MUTED_STORAGE_KEY, String(next))
    if (mediaRef.value) mediaRef.value.muted = next
  }

  function setPlaybackRate(rate) {
    playbackRate.value = rate
    if (mediaRef.value) mediaRef.value.playbackRate = rate
  }

  return {
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
  }
}
