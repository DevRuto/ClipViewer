import { describe, expect, it, vi, beforeEach } from 'vitest'
import { shallowMount } from '@vue/test-utils'

vi.mock('@/services/api', () => ({
  api: { post: vi.fn() },
}))

const mockPush = vi.fn()
const mockBack = vi.fn()
vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mockPush, back: mockBack }),
}))

import { api } from '@/services/api'
import UploadView from '@/views/UploadView.vue'
import VideoUploadPreview from '@/components/VideoUploadPreview.vue'

function makeVideoFile(name = 'my-video.mp4', type = 'video/mp4') {
  return new File(['fake-bytes'], name, { type })
}

async function selectFile(wrapper, file) {
  const input = wrapper.find('input[type="file"]')
  Object.defineProperty(input.element, 'files', { value: [file], configurable: true })
  await input.trigger('change')
}

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}

describe('UploadView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    global.URL.createObjectURL = vi.fn(() => 'blob:mock-url')
    global.URL.revokeObjectURL = vi.fn()
  })

  it('accepts a valid video file and defaults the name from the filename', async () => {
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile('my-video.mp4'))

    expect(wrapper.find('#videoName').element.value).toBe('my-video')
    expect(wrapper.findComponent(VideoUploadPreview).exists()).toBe(true)
  })

  it('rejects a non-video file with an error message', async () => {
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, new File(['x'], 'doc.pdf', { type: 'application/pdf' }))

    expect(wrapper.text()).toContain('Please select a valid video file')
    expect(wrapper.findComponent(VideoUploadPreview).exists()).toBe(false)
  })

  it('shows an error when uploading without a video name', async () => {
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile())
    await wrapper.find('#videoName').setValue('')
    await wrapper.find('button.flex-1').trigger('click')

    expect(wrapper.text()).toContain('Please enter a video name')
    expect(api.post).not.toHaveBeenCalled()
  })

  it('uploads the video and redirects to the new clip on success', async () => {
    api.post.mockResolvedValueOnce({ status: 202, data: { videoId: 'xyz789' } })
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile('my-video.mp4'))
    await wrapper.find('button.flex-1').trigger('click')
    await flushPromises()

    expect(api.post).toHaveBeenCalledWith(
      expect.stringContaining('/api/upload?name=my-video'),
      expect.any(File),
      expect.objectContaining({ headers: { 'Content-Type': 'video/mp4' } }),
    )
    expect(mockPush).toHaveBeenCalledWith('/clips/xyz789')
  })

  it('surfaces the server error message when the upload fails', async () => {
    api.post.mockRejectedValueOnce({ response: { data: { message: 'Server exploded' } } })
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile())
    await wrapper.find('button.flex-1').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Server exploded')
  })

  it('guards against an incomplete timestamp range from the editor', async () => {
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile())
    await wrapper.findComponent(VideoUploadPreview).vm.$emit('toggle-edit-mode')
    // Simulate a malformed/partial payload (endTime missing) reaching the parent.
    await wrapper.findComponent(VideoUploadPreview).vm.$emit('timestamps-change', { startTime: 5 })
    await wrapper.find('button.flex-1').trigger('click')

    expect(wrapper.text()).toContain('Please set valid start and end times for the clip')
    expect(api.post).not.toHaveBeenCalled()
  })

  it('includes trim timestamps in the upload URL when a partial clip range is set', async () => {
    api.post.mockResolvedValueOnce({ status: 202, data: { videoId: 'clip1' } })
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile())
    await wrapper.findComponent(VideoUploadPreview).vm.$emit('toggle-edit-mode')
    await wrapper
      .findComponent(VideoUploadPreview)
      .vm.$emit('timestamps-change', { startTime: 5, endTime: 15 })
    await wrapper.find('button.flex-1').trigger('click')
    await flushPromises()

    const url = api.post.mock.calls[0][0]
    expect(url).toContain('startTime=5')
    expect(url).toContain('endTime=15')
  })

  it('clears the preview and revokes the object URL', async () => {
    const wrapper = shallowMount(UploadView)
    await selectFile(wrapper, makeVideoFile())
    await wrapper.findComponent(VideoUploadPreview).vm.$emit('clear-preview')

    expect(global.URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-url')
    expect(wrapper.findComponent(VideoUploadPreview).exists()).toBe(false)
  })
})
