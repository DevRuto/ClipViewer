import { describe, expect, it, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'

const mockLogin = vi.fn()
const mockUser = vi.hoisted(() => ({ value: null }))
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => ({ login: mockLogin, user: mockUser }),
}))

const mockPush = vi.fn()
vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mockPush }),
}))

import LoginView from '@/views/LoginView.vue'

describe('LoginView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockUser.value = null
  })

  it('logs in and redirects to the user clips page on success', async () => {
    mockLogin.mockImplementation(async () => {
      mockUser.value = { username: 'alice' }
      return { success: true }
    })
    const wrapper = mount(LoginView)

    await wrapper.find('input#api-key').setValue('my-api-key')
    await wrapper.find('form').trigger('submit.prevent')
    await wrapper.vm.$nextTick()
    await Promise.resolve()
    await wrapper.vm.$nextTick()

    expect(mockLogin).toHaveBeenCalledWith('my-api-key')
    expect(mockPush).toHaveBeenCalledWith({
      name: 'user-clips',
      params: { username: 'alice' },
    })
    expect(wrapper.find('.text-red-600').exists()).toBe(false)
  })

  it('shows an error message and does not redirect on failed login', async () => {
    mockLogin.mockResolvedValue({ success: false, error: 'Authentication failed' })
    const wrapper = mount(LoginView)

    await wrapper.find('input#api-key').setValue('bad-key')
    await wrapper.find('form').trigger('submit.prevent')
    await Promise.resolve()
    await wrapper.vm.$nextTick()

    expect(mockPush).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Authentication failed')
  })

  it('disables the submit button while logging in', async () => {
    let resolveLogin
    mockLogin.mockReturnValue(
      new Promise((resolve) => {
        resolveLogin = resolve
      }),
    )
    const wrapper = mount(LoginView)

    await wrapper.find('input#api-key').setValue('my-api-key')
    const submitPromise = wrapper.find('form').trigger('submit.prevent')
    await wrapper.vm.$nextTick()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Signing in...')

    mockUser.value = { username: 'alice' }
    resolveLogin({ success: true })
    await submitPromise
    await flushPromises()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeUndefined()
  })
})

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}
