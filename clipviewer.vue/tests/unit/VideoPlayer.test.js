import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import VideoPlayer from '@/components/VideoPlayer.vue'

beforeEach(() => {
  localStorage.clear()
  HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
  HTMLMediaElement.prototype.pause = vi.fn()
})

function markLoaded(wrapper) {
  wrapper.find('video').element.dispatchEvent(new Event('loadeddata'))
}

describe('VideoPlayer', () => {
  it('shows a loading state until the media fires loadeddata, then reveals the player', async () => {
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })

    expect(wrapper.text()).toContain('Loading video...')
    expect(wrapper.find('video').exists()).toBe(true)

    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).not.toContain('Loading video...')
    expect(wrapper.emitted('loaded')).toHaveLength(1)
  })

  it('renders a plain <video> for non-HLS sources and <hls-video> for .m3u8 sources', () => {
    const mp4 = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    expect(mp4.find('video').exists()).toBe(true)
    expect(mp4.find('hls-video').exists()).toBe(false)

    const hls = mount(VideoPlayer, { props: { src: '/files/hls/abc/playlist.m3u8' } })
    expect(hls.find('hls-video').exists()).toBe(true)
    expect(hls.find('video').exists()).toBe(false)
  })

  it('exposes goToTime() which seeks the underlying media element', async () => {
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    wrapper.vm.goToTime('42')
    expect(wrapper.find('video').element.currentTime).toBe(42)
    expect(wrapper.vm.currentTime).toBe(42)
  })

  it('toggles play/pause when the video is clicked', async () => {
    vi.useFakeTimers()
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    // A click is held for the double-tap/double-click window (see resolveZoneGesture in
    // VideoPlayer.vue) before it commits to a play/pause toggle, in case a second click follows.
    await wrapper.find('video').trigger('click')
    await vi.advanceTimersByTimeAsync(300)
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalledTimes(1)
    vi.useRealTimers()
  })

  it('responds to space/arrow-key shortcuts on the player', async () => {
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()
    const video = wrapper.find('video').element
    Object.defineProperty(video, 'paused', { value: true, configurable: true })
    video.currentTime = 10

    const root = wrapper.find('[role="region"]')
    await root.trigger('keydown', { key: ' ' })
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalledTimes(1)

    await root.trigger('keydown', { key: 'ArrowRight' })
    expect(video.currentTime).toBe(15)
  })

  it('ignores shortcuts when the keydown originates from a text input', async () => {
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    const input = document.createElement('input')
    wrapper.find('[role="region"]').element.appendChild(input)
    input.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }))

    expect(HTMLMediaElement.prototype.play).not.toHaveBeenCalled()
  })
})
