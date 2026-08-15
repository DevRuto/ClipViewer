import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount, DOMWrapper } from '@vue/test-utils'
import { reactive } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'

const mockUser = vi.hoisted(() => ({ value: null }))
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => ({ user: mockUser }),
}))

import VideoInfo from '@/components/VideoInfo.vue'

function makeRouter() {
  return createRouter({
    history: createWebHistory(),
    routes: [{ path: '/browse', name: 'browse', component: { template: '<div />' } }],
  })
}

// Tag chips render as RouterLinks even outside of edit mode, so every mount needs a router.
function mountVideoInfo(options = {}) {
  return mount(VideoInfo, {
    ...options,
    global: { ...options.global, plugins: [makeRouter(), ...(options.global?.plugins || [])] },
  })
}

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
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('.mb-4.space-y-3').exists()).toBe(false)
    expect(wrapper.text()).toContain('My Clip')
  })

  it('shows the edit toggle for the video owner', () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('.mb-4.space-y-3').exists()).toBe(true)
  })

  it('shows a processing indicator with clamped progress while pending', () => {
    const video = { ...baseVideo, status: 'Processing', progress: 250, processed: false }
    const wrapper = mountVideoInfo({ props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('Video status: Processing (100%)')
  })

  it('clamps negative progress values to zero', () => {
    const video = { ...baseVideo, status: 'Pending', progress: -5, processed: false }
    const wrapper = mountVideoInfo({ props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('(0%)')
  })

  it('shows an error indicator when the job status is Error', () => {
    const video = { ...baseVideo, status: 'Error', processed: false }
    const wrapper = mountVideoInfo({ props: { video, videoPlayer: null } })

    expect(wrapper.text()).toContain('Video processing failed')
  })

  it('falls back to a processed/processing status when no job status is present', () => {
    const processed = mountVideoInfo({
      props: { video: { ...baseVideo, status: undefined, processed: true }, videoPlayer: null },
    })
    expect(processed.text()).not.toContain('Video status:')

    const processing = mountVideoInfo({
      props: { video: { ...baseVideo, status: undefined, processed: false }, videoPlayer: null },
    })
    expect(processing.text()).toContain('Video status: Processing')
  })

  it('copies the current URL to the clipboard without a timestamp by default', async () => {
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    const copyButton = wrapper.find('button[title="Copy Link"]')
    await copyButton.trigger('click')

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(window.location.href)
  })

  it('includes a timestamp in the copied link when the timestamp toggle is enabled', async () => {
    // `currentTime` is only picked up via a watcher on `videoPlayer.currentTime`,
    // so the prop needs to be reactive and actually change after mount to fire it.
    const videoPlayer = reactive({ currentTime: 0 })
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer } })

    videoPlayer.currentTime = 42
    await wrapper.vm.$nextTick()

    const timestampSwitch = wrapper.find('[role="switch"]')
    await timestampSwitch.trigger('click')

    const copyButton = wrapper.find('button[title="Copy Link"]')
    await copyButton.trigger('click')

    const copiedUrl = navigator.clipboard.writeText.mock.calls[0][0]
    expect(copiedUrl).toContain('t=42')
  })

  it('opens a confirmation modal before deleting and only emits delete-video on confirm', async () => {
    mockUser.value = { username: 'alice' }
    // AlertDialog content is teleported to document.body, outside the mounted wrapper's own
    // element tree, so it has to be queried via a DOMWrapper around document.body instead of
    // `wrapper.find()`/`wrapper.text()`.
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await wrapper.find('.mb-4.space-y-3 button').trigger('click') // open edit mode
    const deleteButton = findByText(wrapper, 'button', 'Delete Video')
    await deleteButton.trigger('click')

    expect(body.text()).toContain('Are you sure you want to delete "My Clip"?')
    expect(wrapper.emitted('delete-video')).toBeFalsy()

    const cancelButton = findByText(body, 'button', 'Cancel')
    await cancelButton.trigger('click')
    expect(wrapper.emitted('delete-video')).toBeFalsy()
    expect(body.text()).not.toContain('Are you sure you want to delete "My Clip"?')

    await deleteButton.trigger('click')
    const confirmButton = findByText(body, 'button', 'Delete')
    await confirmButton.trigger('click')

    expect(wrapper.emitted('delete-video')).toBeTruthy()
    expect(wrapper.emitted('delete-video')[0]).toEqual([baseVideo])
  })

  it('debounces name edits before emitting update-video', async () => {
    vi.useFakeTimers()
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

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
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const unlistedSwitch = wrapper.find('.mb-4.space-y-3 [role="switch"]')
    await unlistedSwitch.trigger('click')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.unlisted).toBe(true)
  })

  it('shows a persistent unlisted badge for the owner when the video is unlisted', () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: { ...baseVideo, unlisted: true }, videoPlayer: null } })

    expect(wrapper.text()).toContain('Unlisted')
  })

  it('reverts an emptied title back to the last saved value on blur', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const titleInput = wrapper.find('input[placeholder="Video title"]')
    await titleInput.setValue('   ')
    await titleInput.trigger('blur')

    expect(titleInput.element.value).toBe('My Clip')
  })

  it('reverts unsaved title edits when Escape is pressed', async () => {
    vi.useFakeTimers()
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const titleInput = wrapper.find('input[placeholder="Video title"]')
    await titleInput.setValue('Discard Me')
    await titleInput.trigger('keydown', { key: 'Escape' })

    expect(wrapper.find('h1').text()).toBe('My Clip')
    expect(wrapper.emitted('update-video')).toBeFalsy()
  })

  it('shows a temporary "Copied!" confirmation after copying the link', async () => {
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    const copyButton = wrapper.find('button[title="Copy Link"]')
    await copyButton.trigger('click')
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(copyButton.attributes('title')).toBe('Copied!')
  })

  it('adds a tag and emits update-video immediately', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const tagInput = wrapper.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('funny')
    await tagInput.trigger('keydown.enter')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.tags).toEqual(['funny'])
    expect(tagInput.element.value).toBe('')
  })

  it('does not add a duplicate tag (case-insensitive)', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: { ...baseVideo, tags: ['Funny'] }, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const tagInput = wrapper.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('funny')
    await tagInput.trigger('keydown.enter')

    expect(wrapper.emitted('update-video')).toBeFalsy()
  })

  it('rejects a tag longer than 30 characters', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const tagInput = wrapper.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('a'.repeat(31))
    await tagInput.trigger('keydown.enter')

    expect(wrapper.emitted('update-video')).toBeFalsy()
  })

  it('removes a tag and emits update-video', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: { ...baseVideo, tags: ['funny', 'gaming'] }, videoPlayer: null },
    })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')
    const removeButton = findByText(wrapper, 'span', /funny/)?.find('button')
    await removeButton.trigger('click')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.tags).toEqual(['gaming'])
  })

  it('hides the tag input once the maximum of 15 tags is reached', async () => {
    mockUser.value = { username: 'alice' }
    const tags = Array.from({ length: 15 }, (_, i) => `tag${i}`)
    const wrapper = mountVideoInfo({ props: { video: { ...baseVideo, tags }, videoPlayer: null } })

    await wrapper.find('.mb-4.space-y-3 button').trigger('click')

    expect(wrapper.find('input[placeholder="Add a tag..."]').exists()).toBe(false)
  })

  it('renders tags as links to the browse page when not editing', async () => {
    const wrapper = mountVideoInfo({
      props: { video: { ...baseVideo, tags: ['funny'] }, videoPlayer: null },
    })

    const tagLink = wrapper.findComponent({ name: 'RouterLink' })
    expect(tagLink.props('to')).toBe('/browse?tag=funny')
  })

  it('emits refresh-video when the refresh button is clicked after processing completes', async () => {
    const wrapper = mountVideoInfo({
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
