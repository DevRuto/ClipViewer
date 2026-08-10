import { describe, expect, it, vi, beforeEach } from 'vitest'

vi.mock('@/services/api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

import { api } from '@/services/api'

async function loadUseAuth() {
  const mod = await import('@/composables/useAuth')
  return mod.useAuth
}

describe('useAuth', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.resetModules()
    vi.clearAllMocks()
  })

  it('login stores token and user info on success', async () => {
    api.post.mockResolvedValueOnce({ data: { token: 'jwt-token' } })
    api.get.mockResolvedValueOnce({ data: { username: 'alice', userId: '1' } })

    const useAuth = await loadUseAuth()
    const { login, isAuthenticated, user, token } = useAuth()

    const result = await login('api-key-guid')

    expect(result).toEqual({ success: true })
    expect(token.value).toBe('jwt-token')
    expect(user.value).toEqual({ username: 'alice', userId: '1' })
    expect(isAuthenticated.value).toBe(true)
    expect(localStorage.getItem('token')).toBe('jwt-token')
    expect(localStorage.getItem('apiKey')).toBe('api-key-guid')
    expect(api.post).toHaveBeenCalledWith('/api/auth/login', { apiKey: 'api-key-guid' })
  })

  it('login logs out and returns an error when the request fails', async () => {
    api.post.mockRejectedValueOnce(new Error('network error'))

    const useAuth = await loadUseAuth()
    const { login, isAuthenticated } = useAuth()

    const result = await login('bad-key')

    expect(result).toEqual({ success: false, error: 'Authentication failed' })
    expect(isAuthenticated.value).toBe(false)
    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('apiKey')).toBeNull()
  })

  it('logout clears user, token, apiKey and localStorage', async () => {
    localStorage.setItem('token', 'existing-token')
    localStorage.setItem('apiKey', 'existing-key')

    const useAuth = await loadUseAuth()
    const { logout, isAuthenticated, token, apiKey } = useAuth()

    logout()

    expect(isAuthenticated.value).toBe(false)
    expect(token.value).toBe('')
    expect(apiKey.value).toBe('')
    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('apiKey')).toBeNull()
  })

  it('checkAuth with a valid token fetches and stores the current user', async () => {
    localStorage.setItem('token', 'valid-token')
    api.get.mockResolvedValueOnce({ data: { username: 'bob' } })

    const useAuth = await loadUseAuth()
    const { checkAuth, user, isAuthenticated } = useAuth()

    const result = await checkAuth()

    expect(result).toBe(true)
    expect(user.value).toEqual({ username: 'bob' })
    expect(isAuthenticated.value).toBe(true)
    expect(api.get).toHaveBeenCalledWith('/api/auth/me')
  })

  it('checkAuth with no token and no stored apiKey returns false without calling the API', async () => {
    const useAuth = await loadUseAuth()
    const { checkAuth } = useAuth()

    const result = await checkAuth()

    expect(result).toBe(false)
    expect(api.get).not.toHaveBeenCalled()
    expect(api.post).not.toHaveBeenCalled()
  })

  it('checkAuth with an expired token retries login using the stored apiKey', async () => {
    localStorage.setItem('token', 'expired-token')
    localStorage.setItem('apiKey', 'stored-key')
    api.get.mockRejectedValueOnce(new Error('401'))
    api.post.mockResolvedValueOnce({ data: { token: 'new-token' } })
    api.get.mockResolvedValueOnce({ data: { username: 'carol' } })

    const useAuth = await loadUseAuth()
    const { checkAuth, token } = useAuth()

    const result = await checkAuth()

    expect(result).toBe(true)
    expect(token.value).toBe('new-token')
    expect(api.post).toHaveBeenCalledWith('/api/auth/login', { apiKey: 'stored-key' })
  })

  it('checkAuth with no token but a stored apiKey logs in directly', async () => {
    localStorage.setItem('apiKey', 'stored-key')
    api.post.mockResolvedValueOnce({ data: { token: 'fresh-token' } })
    api.get.mockResolvedValueOnce({ data: { username: 'dave' } })

    const useAuth = await loadUseAuth()
    const { checkAuth, token } = useAuth()

    const result = await checkAuth()

    expect(result).toBe(true)
    expect(token.value).toBe('fresh-token')
  })

  it('checkAuth logs out when the token is invalid and there is no apiKey to retry with', async () => {
    localStorage.setItem('token', 'expired-token')
    api.get.mockRejectedValueOnce(new Error('401'))

    const useAuth = await loadUseAuth()
    const { checkAuth, isAuthenticated, token } = useAuth()

    const result = await checkAuth()

    expect(result).toBe(false)
    expect(isAuthenticated.value).toBe(false)
    expect(token.value).toBe('')
  })
})
