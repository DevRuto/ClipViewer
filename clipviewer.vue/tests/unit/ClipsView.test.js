import { describe, expect, it, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'

// ClipsView's template reads `isAuthenticated` and `user.username` directly
// (relying on Vue's template auto-unwrap), so the mocked values must be real
// refs rather than plain `{ value }` objects.
const mockAuthState = { user: ref(null), isAuthenticated: ref(false) }
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => mockAuthState,
}))

const mockRoute = vi.hoisted(() => ({ name: 'browse', params: {} }))
const mockPush = vi.fn()
vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  useRouter: () => ({ push: mockPush }),
}))

import ClipsView from '@/views/ClipsView.vue'

const ClipListStub = { props: ['username'], template: '<div class="clip-list-stub" />' }

describe('ClipsView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockAuthState.user.value = null
    mockAuthState.isAuthenticated.value = false
  })

  it('shows "Browse Clips" and no upload button on the browse route', async () => {
    mockRoute.name = 'browse'
    mockRoute.params = {}
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    expect(wrapper.find('h1').text()).toBe('Browse Clips')
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('shows "<user>\'s Clips" for an unauthenticated visitor on a user page', async () => {
    mockRoute.name = 'user-clips'
    mockRoute.params = { username: 'Alice' }
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    expect(wrapper.find('h1').text()).toBe("alice's Clips")
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('shows "Your Clips" and the upload button for the owner of the page', async () => {
    mockRoute.name = 'user-clips'
    mockRoute.params = { username: 'alice' }
    mockAuthState.isAuthenticated.value = true
    mockAuthState.user.value = { username: 'alice' }
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    expect(wrapper.find('h1').text()).toBe('Your Clips')
    expect(wrapper.find('button').exists()).toBe(true)
  })

  it('shows "<user>\'s Clips" and hides the upload button for another user\'s page while authenticated', async () => {
    mockRoute.name = 'user-clips'
    mockRoute.params = { username: 'alice' }
    mockAuthState.isAuthenticated.value = true
    mockAuthState.user.value = { username: 'bob' }
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    expect(wrapper.find('h1').text()).toBe("alice's Clips")
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('passes the username through to ClipList', async () => {
    mockRoute.name = 'user-clips'
    mockRoute.params = { username: 'alice' }
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    expect(wrapper.findComponent(ClipListStub).props('username')).toBe('alice')
  })

  it('navigating to the upload page happens via the router when the button is clicked', async () => {
    mockRoute.name = 'user-clips'
    mockRoute.params = { username: 'alice' }
    mockAuthState.isAuthenticated.value = true
    mockAuthState.user.value = { username: 'alice' }
    const wrapper = mount(ClipsView, { global: { stubs: { ClipList: ClipListStub } } })
    await wrapper.vm.$nextTick()

    await wrapper.find('button').trigger('click')

    expect(mockPush).toHaveBeenCalledWith('/upload')
  })
})
