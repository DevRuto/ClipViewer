import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'

vi.mock('@/services/api', () => ({
  api: { get: vi.fn(), put: vi.fn(), delete: vi.fn() },
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
  emits: ['update-video', 'delete-video', 'refresh-video'],
  template: '<div class="video-info-stub" />',
}
const stubs = { VideoPlayer: VideoPlayerStub, VideoInfo: VideoInfoStub }

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

  it('shows a not-found state when the video does not exist', async () => {
    api.get.mockRejectedValueOnce(new Error('not found'))
    const wrapper = mount(VideoView, { global: { stubs } })
    await flushPromises()

    expect(wrapper.text()).toContain('Video not found')
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
