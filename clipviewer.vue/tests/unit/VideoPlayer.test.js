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

  it('toggles play/pause immediately when the video is clicked', async () => {
    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    // Center clicks have no double-click gesture to disambiguate (that's only the left/right
    // edge zones - see resolveZoneGesture in VideoPlayer.vue), so they toggle without delay.
    await wrapper.find('video').trigger('click')
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalledTimes(1)
  })

  it('applies edge-zone clicks immediately, and undoes them if a second click confirms a seek', async () => {
    let paused = true
    HTMLMediaElement.prototype.play = vi.fn(function () {
      paused = false
      return Promise.resolve()
    })
    HTMLMediaElement.prototype.pause = vi.fn(function () {
      paused = true
    })

    const wrapper = mount(VideoPlayer, { props: { src: '/files/source/clip.mp4' } })
    markLoaded(wrapper)
    await wrapper.vm.$nextTick()

    const video = wrapper.find('video').element
    Object.defineProperty(video, 'paused', { get: () => paused, configurable: true })
    video.getBoundingClientRect = () => ({ left: 0, width: 300 })
    video.currentTime = 20

    // A click near the left edge toggles play immediately - it doesn't wait to see if a second
    // click follows (see resolveZoneGesture/toggleSingleClick in VideoPlayer.vue).
    await wrapper.find('video').trigger('click', { clientX: 10 })
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalledTimes(1)
    expect(paused).toBe(false)

    // A second click in the same edge zone within the window confirms a double-click: the toggle
    // is undone (paused again) and a 10s seek happens instead.
    await wrapper.find('video').trigger('click', { clientX: 10 })
    expect(HTMLMediaElement.prototype.pause).toHaveBeenCalledTimes(1)
    expect(paused).toBe(true)
    expect(video.currentTime).toBe(10)
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
