import { describe, expect, it, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'

vi.mock('@/services/api', () => ({
  api: { get: vi.fn() },
}))

import { api } from '@/services/api'
import ClipList from '@/components/ClipList.vue'

const sampleVideos = [
  { id: '1', videoId: 'abc123', name: 'Clip One', duration: 30, createdAt: '2026-01-01', author: 'alice' },
  { id: '2', videoId: 'def456', name: 'Clip Two', duration: 60, createdAt: '2026-01-02', author: 'bob' },
]

function makeRouter() {
  return createRouter({
    history: createWebHistory(),
    routes: [{ path: '/clips/:videoId', name: 'video', component: { template: '<div />' } }],
  })
}

describe('ClipList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows an empty state before any videos have been fetched', async () => {
    const router = makeRouter()
    const wrapper = mount(ClipList, {
      global: { plugins: [router] },
    })

    expect(wrapper.text()).toContain('No clips uploaded yet')
    expect(api.get).not.toHaveBeenCalled()
  })

  it('fetches and renders videos for the given user when the username prop changes', async () => {
    api.get.mockResolvedValueOnce({ data: sampleVideos })
    const router = makeRouter()
    const wrapper = mount(ClipList, {
      props: { username: null },
      global: { plugins: [router] },
    })

    await wrapper.setProps({ username: 'alice' })
    await flushPromises()

    expect(api.get).toHaveBeenCalledWith('/api/videos?user=alice')
    expect(wrapper.text()).toContain('Clip One')
    expect(wrapper.text()).toContain('Clip Two')
    expect(wrapper.text()).not.toContain('No clips uploaded yet')
  })

  it('fetches all public videos when no username is set', async () => {
    api.get.mockResolvedValueOnce({ data: sampleVideos })
    const router = makeRouter()
    const wrapper = mount(ClipList, {
      props: { username: 'alice' },
      global: { plugins: [router] },
    })

    await wrapper.setProps({ username: null })
    await flushPromises()

    expect(api.get).toHaveBeenCalledWith('/api/videos')
  })

  it('clears the current list while a new fetch is in flight', async () => {
    let resolveFetch
    api.get.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveFetch = resolve
      }),
    )
    const router = makeRouter()
    const wrapper = mount(ClipList, {
      props: { username: null },
      global: { plugins: [router] },
    })

    await wrapper.setProps({ username: 'alice' })
    await flushPromises()

    expect(wrapper.text()).toContain('No clips uploaded yet')

    resolveFetch({ data: sampleVideos })
    await flushPromises()

    expect(wrapper.text()).toContain('Clip One')
  })
})

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}
