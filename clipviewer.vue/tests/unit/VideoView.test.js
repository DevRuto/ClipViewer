import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'

vi.mock('@/services/api', () => ({
  api: { get: vi.fn(), put: vi.fn(), post: vi.fn(), delete: vi.fn() },
}))

const mockRoute = vi.hoisted(() => ({ params: { videoId: 'abc123' }, query: {} }))
const mockPush = vi.fn()
vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  useRouter: () => ({ push: mockPush }),
}))

import { api } from '@/services/api'
import VideoView from '@/views/VideoView.vue'

const processedVideo = {
  videoId: 'abc123',
  name: 'My Video',
  processed: true,
  thumbnail: '/files/thumbnails/abc.jpg',
  hlsPlaylistFile: '/files/hls/abc/playlist.m3u8',
  sourceVideoFile: '/files/source/abc.mp4',
}

const processingVideo = { ...processedVideo, processed: false }

const VideoPlayerStub = { template: '<div class="video-player-stub" />', methods: { goToTime: vi.fn() } }
const VideoInfoStub = {
  props: ['video', 'videoPlayer', 'saving', 'saveError'],
  emits: ['update-video', 'delete-video', 'refresh-video', 'retry-video'],
  template: '<div class="video-info-stub" />',
}
const stubs = { VideoPlayer: VideoPlayerStub, VideoInfo: VideoInfoStub, RouterLink: RouterLinkStub }

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}

describe('VideoView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockRoute.params = { videoId: 'abc123' }
    mockRoute.query = {}
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('fetches the video on mount and stops loading', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    expect(api.get).toHaveBeenCalledWith('/api/videos/abc123')
    expect(wrapper.text()).not.toContain('Loading video...')
    expect(wrapper.find('.video-info-stub').exists()).toBe(true)
  })

  it('shows a not-found state when the video does not exist (404)', async () => {
    api.get.mockRejectedValueOnce({ response: { status: 404 } })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    expect(wrapper.text()).toContain('Video not found')
  })

  it('shows an error banner when fetching the video fails for a non-404 reason', async () => {
    api.get.mockRejectedValueOnce(new Error('network down'))
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    expect(wrapper.text()).toContain('Unable to load video')
    expect(wrapper.text()).not.toContain('Video not found')
  })

  it('does not poll when the video is already processed', async () => {
    vi.useFakeTimers()
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    mount(VideoView, { global: { stubs } })
    await vi.advanceTimersByTimeAsync(0)

    await vi.advanceTimersByTimeAsync(20000)
    expect(api.get).toHaveBeenCalledTimes(1)
  })

  it('polls every 5 seconds while processing and stops once complete', async () => {
    vi.useFakeTimers()
    api.get.mockResolvedValueOnce({ status: 200, data: processingVideo })
    mount(VideoView, { global: { stubs } })
    await vi.advanceTimersByTimeAsync(0)
    expect(api.get).toHaveBeenCalledTimes(1)

    api.get.mockResolvedValueOnce({ status: 200, data: processingVideo })
    await vi.advanceTimersByTimeAsync(5000)
    expect(api.get).toHaveBeenCalledTimes(2)

    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    await vi.advanceTimersByTimeAsync(5000)
    expect(api.get).toHaveBeenCalledTimes(3)

    await vi.advanceTimersByTimeAsync(10000)
    expect(api.get).toHaveBeenCalledTimes(3)
  })

  it('updates the video on update-video and redirects to /browse on delete-video', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.put.mockResolvedValueOnce({ status: 200, data: { ...processedVideo, name: 'Renamed' } })
    await wrapper
      .findComponent(VideoInfoStub)
      .vm.$emit('update-video', { name: 'Renamed', unlisted: false, description: '' })
    await flushPromises()
    expect(api.put).toHaveBeenCalledWith('/api/videos/abc123', {
      unlisted: false,
      name: 'Renamed',
      description: '',
    })

    api.delete.mockResolvedValueOnce({ status: 200 })
    await wrapper.findComponent(VideoInfoStub).vm.$emit('delete-video')
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith('/api/videos/abc123')
    expect(mockPush).toHaveBeenCalledWith('/browse')
  })

  it('passes the save error to VideoInfo (not the page-level banner) when updating fails', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.put.mockRejectedValueOnce({ response: { data: { message: 'Update failed' } } })
    await wrapper
      .findComponent(VideoInfoStub)
      .vm.$emit('update-video', { name: 'Renamed', unlisted: false, description: '' })
    await flushPromises()

    // A failed save is surfaced inside VideoInfo's own edit modal via the saveError prop, not
    // the page-level banner (which stays reserved for load/retry/delete failures) - the modal
    // overlay would hide that banner while open anyway.
    expect(wrapper.findComponent(VideoInfoStub).props('saveError')).toBe('Update failed')
    expect(wrapper.findComponent(VideoInfoStub).props('saving')).toBe(false)
    expect(wrapper.text()).not.toContain('Update failed')
  })

  it('sets saving true on VideoInfo while an update PUT is in flight', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    let resolvePut
    api.put.mockReturnValueOnce(new Promise((resolve) => { resolvePut = resolve }))
    wrapper
      .findComponent(VideoInfoStub)
      .vm.$emit('update-video', { name: 'Renamed', unlisted: false, description: '' })
    await wrapper.vm.$nextTick()

    expect(wrapper.findComponent(VideoInfoStub).props('saving')).toBe(true)

    resolvePut({ status: 200, data: { ...processedVideo, name: 'Renamed' } })
    await flushPromises()

    expect(wrapper.findComponent(VideoInfoStub).props('saving')).toBe(false)
  })

  it('shows an error banner when deleting the video fails', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.delete.mockRejectedValueOnce(new Error('network down'))
    await wrapper.findComponent(VideoInfoStub).vm.$emit('delete-video')
    await flushPromises()

    expect(mockPush).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Failed to delete video')
  })

  it('retries a failed conversion job on retry-video', async () => {
    const erroredVideo = { ...processedVideo, processed: false, status: 'Error' }
    api.get.mockResolvedValueOnce({ status: 200, data: erroredVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.post.mockResolvedValueOnce({
      status: 200,
      data: { ...erroredVideo, status: 'Pending' },
    })
    await wrapper.findComponent(VideoInfoStub).vm.$emit('retry-video')
    await flushPromises()

    expect(api.post).toHaveBeenCalledWith('/api/videos/abc123/retry')
    expect(wrapper.find('.video-info-stub').exists()).toBe(true)
  })

  it('re-fetches the video when refresh-video is emitted', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    await wrapper.findComponent(VideoInfoStub).vm.$emit('refresh-video')
    await flushPromises()

    expect(api.get).toHaveBeenCalledTimes(2)
  })
})

describe('VideoView player resize', () => {
  const RESIZE_HANDLE_TITLE = 'Drag to resize the player (double-click to reset)'

  beforeEach(() => {
    localStorage.clear()
  })

  function findResizableContainer(wrapper) {
    // The resizable width lives on containerRef (wraps Alert + Card), not the inner .aspect-video
    // player box, so the whole card - not just the video - grows/shrinks together. Both it and the
    // page's outermost wrapper carry "mx-auto", so take the second (innermost) match.
    return wrapper.findAll('.mx-auto')[1]
  }

  it('applies a persisted player width from localStorage', async () => {
    localStorage.setItem('clipviewer:player-width', '555')
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    expect(findResizableContainer(wrapper).attributes('style')).toContain('width: 555px')
  })

  it('resizes on drag, clamps to the available browser width, and persists on release', async () => {
    vi.spyOn(Element.prototype, 'getBoundingClientRect').mockReturnValue({
      width: 600,
      height: 0,
      top: 0,
      left: 0,
      right: 600,
      bottom: 0,
      x: 0,
      y: 0,
      toJSON() {},
    })
    // maxWidth = innerWidth - 32, so this caps drag growth at 618px.
    Object.defineProperty(window, 'innerWidth', { value: 650, configurable: true })
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    // vue-test-utils' trigger() can't set MouseEvent's getter-only `button`/`clientX` on a
    // PointerEvent, so dispatch a real PointerEvent (which accepts them via its constructor) directly.
    wrapper
      .find(`[title="${RESIZE_HANDLE_TITLE}"]`)
      .element.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 300, bubbles: true }))
    await wrapper.vm.$nextTick()

    // +200px from the (mocked) 600px drag start would reach 800px, exceeding the 618px max, so it clamps.
    window.dispatchEvent(new PointerEvent('pointermove', { clientX: 500 }))
    await wrapper.vm.$nextTick()
    expect(findResizableContainer(wrapper).attributes('style')).toContain('width: 618px')

    window.dispatchEvent(new PointerEvent('pointerup', { clientX: 500 }))
    await wrapper.vm.$nextTick()
    expect(localStorage.getItem('clipviewer:player-width')).toBe('618')
  })

  it('clamps drag shrinking to the minimum player width', async () => {
    vi.spyOn(Element.prototype, 'getBoundingClientRect').mockReturnValue({
      width: 600,
      height: 0,
      top: 0,
      left: 0,
      right: 600,
      bottom: 0,
      x: 0,
      y: 0,
      toJSON() {},
    })
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    wrapper
      .find(`[title="${RESIZE_HANDLE_TITLE}"]`)
      .element.dispatchEvent(new PointerEvent('pointerdown', { button: 0, clientX: 300, bubbles: true }))
    await wrapper.vm.$nextTick()

    // Dragging 1000px left from a 600px start would go well below the 320px floor.
    window.dispatchEvent(new PointerEvent('pointermove', { clientX: -700 }))
    await wrapper.vm.$nextTick()
    expect(findResizableContainer(wrapper).attributes('style')).toContain('width: 320px')
  })

  it('resets to a fluid 100% width and clears localStorage on double-click', async () => {
    localStorage.setItem('clipviewer:player-width', '555')
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    await wrapper.find(`[title="${RESIZE_HANDLE_TITLE}"]`).trigger('dblclick')

    // No custom width left - falls back to the normal max-w-5xl/7xl centered column.
    expect(findResizableContainer(wrapper).attributes('style')).toBeUndefined()
    expect(findResizableContainer(wrapper).classes()).toContain('max-w-5xl')
    expect(localStorage.getItem('clipviewer:player-width')).toBeNull()
  })
})
