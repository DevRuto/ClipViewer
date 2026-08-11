import { describe, it, expect, beforeEach, vi } from 'vitest'
import { ref, nextTick } from 'vue'
import { useVideoPlayer } from '@/composables/useVideoPlayer.js'

// HTMLMediaElement's paused/ended/duration/buffered are getter-only in jsdom (as in real
// browsers), so tests that need specific values must override them via defineProperty.
function createMediaEl({ paused = true, ended = false, duration = 0 } = {}) {
  const el = document.createElement('video')
  el.play = vi.fn().mockResolvedValue(undefined)
  el.pause = vi.fn()
  Object.defineProperty(el, 'paused', { value: paused, configurable: true })
  Object.defineProperty(el, 'ended', { value: ended, configurable: true })
  Object.defineProperty(el, 'duration', { value: duration, configurable: true })
  return el
}

beforeEach(() => {
  localStorage.clear()
})

describe('useVideoPlayer', () => {
  it('applies the current volume/mute/rate state to a newly attached element', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    player.setVolume(0.4)
    player.setPlaybackRate(1.5)

    const el = createMediaEl()
    mediaRef.value = el
    await nextTick()

    expect(el.volume).toBeCloseTo(0.4)
    expect(el.playbackRate).toBe(1.5)
  })

  it('re-attaches listeners when the media element is swapped (video <-> hls-video)', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)

    const first = createMediaEl()
    mediaRef.value = first
    await nextTick()

    const second = createMediaEl()
    mediaRef.value = second
    await nextTick()

    first.dispatchEvent(new Event('play'))
    expect(player.isPlaying.value).toBe(false)

    second.dispatchEvent(new Event('play'))
    expect(player.isPlaying.value).toBe(true)
  })

  it('tracks play/pause/ended state from media events', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl()
    mediaRef.value = el
    await nextTick()

    el.dispatchEvent(new Event('play'))
    expect(player.isPlaying.value).toBe(true)

    el.dispatchEvent(new Event('pause'))
    expect(player.isPlaying.value).toBe(false)

    el.dispatchEvent(new Event('play'))
    el.dispatchEvent(new Event('ended'))
    expect(player.isPlaying.value).toBe(false)
  })

  it('tracks buffering state from waiting/playing/canplay events', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl()
    mediaRef.value = el
    await nextTick()

    el.dispatchEvent(new Event('waiting'))
    expect(player.isBuffering.value).toBe(true)

    el.dispatchEvent(new Event('playing'))
    expect(player.isBuffering.value).toBe(false)
  })

  it('updates currentTime/duration from timeupdate/durationchange', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl({ duration: 120 })
    mediaRef.value = el
    await nextTick()

    el.currentTime = 12.5
    el.dispatchEvent(new Event('timeupdate'))
    expect(player.currentTime.value).toBe(12.5)

    el.dispatchEvent(new Event('durationchange'))
    expect(player.duration.value).toBe(120)
  })

  it('calls play() when toggling from paused, pause() when toggling from playing', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl({ paused: true })
    mediaRef.value = el
    await nextTick()

    player.togglePlay()
    expect(el.play).toHaveBeenCalledTimes(1)

    Object.defineProperty(el, 'paused', { value: false, configurable: true })
    player.togglePlay()
    expect(el.pause).toHaveBeenCalledTimes(1)
  })

  it('clamps seekTo to [0, duration] and updates currentTime immediately', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl({ duration: 60 })
    mediaRef.value = el
    await nextTick()
    el.dispatchEvent(new Event('durationchange'))

    player.seekTo(-5)
    expect(el.currentTime).toBe(0)
    expect(player.currentTime.value).toBe(0)

    player.seekTo(999)
    expect(el.currentTime).toBe(60)

    player.seekTo(30)
    expect(el.currentTime).toBe(30)
  })

  it('seekBy offsets from the element currentTime', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl({ duration: 60 })
    mediaRef.value = el
    await nextTick()
    el.dispatchEvent(new Event('durationchange'))
    el.currentTime = 10

    player.seekBy(5)
    expect(el.currentTime).toBe(15)

    player.seekBy(-100)
    expect(el.currentTime).toBe(0)
  })

  it('persists volume to localStorage and unmutes on a non-zero setVolume', async () => {
    const mediaRef = ref(null)
    const player = useVideoPlayer(mediaRef)
    const el = createMediaEl()
    mediaRef.value = el
    await nextTick()

    player.toggleMute()
    expect(player.muted.value).toBe(true)
    expect(localStorage.getItem('clipviewer:player-muted')).toBe('true')

    player.setVolume(0.7)
    expect(player.volume.value).toBeCloseTo(0.7)
    expect(player.muted.value).toBe(false)
    expect(el.muted).toBe(false)
    expect(localStorage.getItem('clipviewer:player-volume')).toBe('0.7')
  })

  it('reads persisted volume/mute from localStorage on creation', () => {
    localStorage.setItem('clipviewer:player-volume', '0.25')
    localStorage.setItem('clipviewer:player-muted', 'true')

    const player = useVideoPlayer(ref(null))
    expect(player.volume.value).toBeCloseTo(0.25)
    expect(player.muted.value).toBe(true)
  })
})
