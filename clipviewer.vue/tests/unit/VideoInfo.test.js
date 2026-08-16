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

// The edit modal's content is teleported to document.body (same as AlertDialog), so any mount
// that opens it needs attachTo: document.body plus a DOMWrapper around document.body to query it.
async function openEditModal(wrapper) {
  await wrapper.find('[aria-label="Edit video details"]').trigger('click')
}

describe('VideoInfo', () => {
  beforeEach(() => {
    mockUser.value = null
    navigator.clipboard = { writeText: vi.fn() }
  })

  afterEach(() => {
    vi.useRealTimers()
    // Dialog/AlertDialog content is teleported straight to document.body rather than into the
    // mounted wrapper's own element, so tests using `attachTo: document.body` would otherwise
    // leak stale teleported nodes (and stale "Save"/tag-chip elements) into the next test.
    document.body.innerHTML = ''
  })

  it('does not show owner-only edit controls for a non-owner', () => {
    mockUser.value = { username: 'someone-else' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('[aria-label="Edit video details"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('My Clip')
  })

  it('shows the edit toggle for the video owner', () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    expect(wrapper.find('[aria-label="Edit video details"]').exists()).toBe(true)
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

  it('includes a timestamp in the copied link when the "Copy Link at ..." button is clicked', async () => {
    // `currentTime` is only picked up via a watcher on `videoPlayer.currentTime`,
    // so the prop needs to be reactive and actually change after mount to fire it.
    const videoPlayer = reactive({ currentTime: 0 })
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer } })

    videoPlayer.currentTime = 42
    await wrapper.vm.$nextTick()

    const copyAtTimeButton = wrapper.find('button[title="Copy Link at 0:42"]')
    await copyAtTimeButton.trigger('click')

    const copiedUrl = navigator.clipboard.writeText.mock.calls[0][0]
    expect(copiedUrl).toContain('t=42')
  })

  it('opens a confirmation modal before deleting and only emits delete-video on confirm', async () => {
    mockUser.value = { username: 'alice' }
    // Both the edit Dialog and the nested delete AlertDialog are teleported to document.body,
    // outside the mounted wrapper's own element tree, so they're queried via a DOMWrapper
    // around document.body instead of `wrapper.find()`/`wrapper.text()`.
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const deleteButton = findByText(body, 'button', 'Delete Video')
    await deleteButton.trigger('click')

    expect(body.text()).toContain('Are you sure you want to delete "My Clip"?')
    expect(wrapper.emitted('delete-video')).toBeFalsy()

    // Scoped to the confirmation dialog specifically - the edit modal behind it has its own,
    // unrelated "Cancel" button visible at the same time.
    const confirmDialog = body.find('[data-slot="alert-dialog-content"]')
    const cancelButton = findByText(confirmDialog, 'button', 'Cancel')
    await cancelButton.trigger('click')
    expect(wrapper.emitted('delete-video')).toBeFalsy()
    expect(body.text()).not.toContain('Are you sure you want to delete "My Clip"?')

    await deleteButton.trigger('click')
    const confirmButton = findByText(body, 'button', 'Delete')
    await confirmButton.trigger('click')

    expect(wrapper.emitted('delete-video')).toBeTruthy()
    expect(wrapper.emitted('delete-video')[0]).toEqual([baseVideo])
  })

  it('stages edits in the modal and only emits update-video once, on Save', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const titleInput = body.find('input[placeholder="Video title"]')
    await titleInput.setValue('New Title')

    expect(wrapper.emitted('update-video')).toBeFalsy()

    const saveButton = findByText(body, 'button', 'Save')
    await saveButton.trigger('click')

    expect(wrapper.emitted('update-video')).toBeTruthy()
    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.name).toBe('New Title')
  })

  it('emits update-video with the toggled unlisted value on Save', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const unlistedSwitch = body.find('[role="switch"]')
    await unlistedSwitch.trigger('click')
    const saveButton = findByText(body, 'button', 'Save')
    await saveButton.trigger('click')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.unlisted).toBe(true)
  })

  it('shows a persistent unlisted badge for the owner when the video is unlisted', () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({ props: { video: { ...baseVideo, unlisted: true }, videoPlayer: null } })

    expect(wrapper.text()).toContain('Unlisted')
  })

  it('disables Save while the title is blank', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const titleInput = body.find('input[placeholder="Video title"]')
    await titleInput.setValue('   ')

    const saveButton = findByText(body, 'button', 'Save')
    expect(saveButton.attributes('disabled')).not.toBeUndefined()
  })

  it('discards unsaved edits when Cancel is clicked', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const titleInput = body.find('input[placeholder="Video title"]')
    await titleInput.setValue('Discard Me')

    const cancelButton = findByText(body, 'button', 'Cancel')
    await cancelButton.trigger('click')

    expect(wrapper.emitted('update-video')).toBeFalsy()
    expect(wrapper.find('h1').text()).toBe('My Clip')

    // Reopening should show the original value again, not the discarded edit
    await openEditModal(wrapper)
    expect(body.find('input[placeholder="Video title"]').element.value).toBe('My Clip')
  })

  it('shows a toast confirming the link was copied', async () => {
    const wrapper = mountVideoInfo({ props: { video: baseVideo, videoPlayer: null } })

    const copyButton = wrapper.find('button[title="Copy Link"]')
    await copyButton.trigger('click')
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(wrapper.find('[role="status"]').text()).toBe('Link copied')
  })

  it('adds a tag to the draft and emits it on Save', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const tagInput = body.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('funny')
    await tagInput.trigger('keydown.enter')

    expect(tagInput.element.value).toBe('')

    const saveButton = findByText(body, 'button', 'Save')
    await saveButton.trigger('click')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.tags).toEqual(['funny'])
  })

  it('does not add a duplicate tag (case-insensitive)', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: { ...baseVideo, tags: ['Funny'] }, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const tagInput = body.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('funny')
    await tagInput.trigger('keydown.enter')

    expect(body.findAll('[data-testid="tag-chip"]').length).toBe(1)
  })

  it('rejects a tag longer than 30 characters', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const tagInput = body.find('input[placeholder="Add a tag..."]')
    await tagInput.setValue('a'.repeat(31))
    await tagInput.trigger('keydown.enter')

    expect(body.findAll('[data-testid="tag-chip"]').length).toBe(0)
  })

  it('removes a tag from the draft and emits the change on Save', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: { ...baseVideo, tags: ['funny', 'gaming'] }, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)
    const removeButton = findByText(body, '[data-testid="tag-chip"]', /funny/)?.find('button')
    await removeButton.trigger('click')
    const saveButton = findByText(body, 'button', 'Save')
    await saveButton.trigger('click')

    const lastEmit = wrapper.emitted('update-video').at(-1)[0]
    expect(lastEmit.tags).toEqual(['gaming'])
  })

  it('hides the tag input once the maximum of 15 tags is reached', async () => {
    mockUser.value = { username: 'alice' }
    const tags = Array.from({ length: 15 }, (_, i) => `tag${i}`)
    const wrapper = mountVideoInfo({
      props: { video: { ...baseVideo, tags }, videoPlayer: null, saving: false, saveError: '' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)

    expect(body.find('input[placeholder="Add a tag..."]').exists()).toBe(false)
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

  it('shows the saveError message inside the modal and keeps it open while saving', async () => {
    mockUser.value = { username: 'alice' }
    const wrapper = mountVideoInfo({
      props: { video: baseVideo, videoPlayer: null, saving: false, saveError: 'Failed to save changes.' },
      attachTo: document.body,
    })
    const body = new DOMWrapper(document.body)

    await openEditModal(wrapper)

    expect(body.text()).toContain('Failed to save changes.')
  })
})
