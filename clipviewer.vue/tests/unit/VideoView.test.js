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
  props: ['video', 'videoPlayer'],
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

  it('shows an error banner when updating the video fails', async () => {
    api.get.mockResolvedValueOnce({ status: 200, data: processedVideo })
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    api.put.mockRejectedValueOnce({ response: { data: { message: 'Update failed' } } })
    await wrapper
      .findComponent(VideoInfoStub)
      .vm.$emit('update-video', { name: 'Renamed', unlisted: false, description: '' })
    await flushPromises()

    expect(wrapper.text()).toContain('Update failed')
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
