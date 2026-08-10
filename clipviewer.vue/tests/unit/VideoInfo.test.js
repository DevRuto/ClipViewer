import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'

const mockUser = vi.hoisted(() => ({ value: null }))
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => ({ user: mockUser }),
}))

import VideoInfo from '@/components/VideoInfo.vue'

const baseVideo = {
  name: 'My Clip',
  description: '',
  author: 'alice',
  duration: 90,
  createdAt: '2026-01-15T00:00:00Z',
  unlisted: false,
  processed: true,
  status: 'Completed',
  progress: 100,
  sourceVideoFile: '/files/source/video.mp4',
}

function findByText(wrapper, selector, text) {
  return wrapper
    .findAll(selector)
    .find((el) => (text instanceof RegExp ? text.test(el.text().trim()) : el.text().trim() === text))
}

describe('VideoInfo', () => {
  beforeEach(() => {
    mockUser.value = null
    navigator.clipboard = { writeText: vi.fn() }
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('does not show owner-only edit controls for a non-owner', () => {
    mockUser.value = { username: 'someone-else' }
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('.mb-4.space-y-3').exists()).toBe(false)
    expect(wrapper.text()).toContain('My Clip')
  })

  it('shows the edit toggle for the video owner', () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('.mb-4.space-y-3').exists()).toBe(true)
  })

  it('shows a processing indicator with clamped progress while pending', () => {
    const video = { ...baseVideo, status: 'Processing', progress: 250, processed: false }
    const wrapper = mount(VideoInfo, { props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('Video status: Processing (100%)')
  })

  it('clamps negative progress values to zero', () => {
    const video = { ...baseVideo, status: 'Pending', progress: -5, processed: false }
    const wrapper = mount(VideoInfo, { props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('(0%)')
  })

  it('shows an error indicator when the job status is Error', () => {
    const video = { ...baseVideo, status: 'Error', processed: false }
    const wrapper = mount(VideoInfo, { props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('Video processing failed')
  })

  it('falls back to a processed/processing status when no job status is present', () => {
    const processed = mount(VideoInfo, {
      props: { video: { ...baseVideo, status: undefined, processed: true }, videoPlayer: null },
    })
    expect(processed.text()).not.toContain('Video status:')

    const processing = mount(VideoInfo, {
      props: { video: { ...baseVideo, status: undefined, processed: false }, videoPlayer: null },
    })
    expect(processing.text()).toContain('Video status: Processing')
  })

  it('copies the current URL to the clipboard without a timestamp by default', async () => {
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    const copyButton = findByText(wrapper, 'button', 'Copy Link')
    await copyButton.trigger('click')

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(window.location.href)
  })

  it('includes a timestamp in the copied link when the timestamp toggle is enabled', async () => {
    // `currentTime` is only picked up via a watcher on `videoPlayer.currentTime`,
    // so the prop needs to be reactive and actually change after mount to fire it.
    const videoPlayer = reactive({ currentTime: 0 })
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer } })

    videoPlayer.currentTime = 42
    await wrapper.vm.$nextTick()

    const timestampCheckbox = wrapper.find('input[type="checkbox"]')
    await timestampCheckbox.setValue(true)

    const copyButton = findByText(wrapper, 'button', /Copy Link/)
    await copyButton.trigger('click')

    const copiedUrl = navigator.clipboard.writeText.mock.calls[0][0]
    expect(copiedUrl).toContain('t=42')
  })

  it('opens a confirmation modal before deleting and only emits delete-video on confirm', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click') // open edit mode
    const deleteButton = findByText(wrapper, 'button', 'Delete Video')
    await deleteButton.trigger('click')

    expect(wrapper.text()).toContain('Are you sure you want to delete this video?')
    expect(wrapper.emitted('delete-video')).toBeFalsy()

    const cancelButton = findByText(wrapper, 'button', 'Cancel')
    await cancelButton.trigger('click')
    expect(wrapper.emitted('delete-video')).toBeFalsy()
    expect(wrapper.text()).not.toContain('Are you sure you want to delete this video?')

    await deleteButton.trigger('click')
    const confirmButton = findByText(wrapper, 'button', 'Delete')
    await confirmButton.trigger('click')

    expect(wrapper.emitted('delete-video')).toBeTruthy()
    expect(wrapper.emitted('delete-video')[0]).toEqual([baseVideo])
  })

  it('debounces name edits before emitting update-video', async () => {
    vi.useFakeTimers()
    mockUser.value = { username: 'alice' }
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const titleInput = wrapper.find('input[placeholder="Video title"]')
    await titleInput.setValue('New Title')

    expect(wrapper.emitted('update-video')).toBeFalsy()

    await vi.advanceTimersByTimeAsync(500)

    expect(wrapper.emitted('update-video')).toBeTruthy()
    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.name).toBe('New Title')
  })

  it('emits update-video immediately when the unlisted toggle changes', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mount(VideoInfo, { props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const unlistedCheckbox = wrapper.find('.mb-4.space-y-3 input[type="checkbox"]')
    await unlistedCheckbox.setValue(true)

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.unlisted).toBe(true)
  })

  it('emits refresh-video when the refresh button is clicked after processing completes', async () => {
    const wrapper = mount(VideoInfo, {
      props: { video: { ...baseVideo, status: undefined, processed: false }, videoPlayer: null },
    })

    await wrapper.setProps({ video: { ...baseVideo, status: undefined, processed: true } })
    await wrapper.vm.$nextTick()

    const refreshButton = findByText(wrapper, 'button', /Refresh/)
    expect(refreshButton).toBeTruthy()
    await refreshButton.trigger('click')

    expect(wrapper.emitted('refresh-video')).toBeTruthy()
  })
})
