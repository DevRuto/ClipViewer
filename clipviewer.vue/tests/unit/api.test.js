import { describe, expect, it, vi, beforeEach } from 'vitest'

const mockAxiosInstance = Object.assign(vi.fn(), {
  interceptors: {
    request: { use: vi.fn() },
    response: { use: vi.fn() },
  },
})

vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => mockAxiosInstance),
  },
}))

const mockUseAuth = vi.fn()
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => mockUseAuth(),
}))

describe('api service', () => {
  let requestInterceptor
  let responseSuccessInterceptor
  let responseErrorInterceptor

  beforeEach(async () => {
    vi.resetModules()
    vi.clearAllMocks()
    mockAxiosInstance.mockReset()

    Object.defineProperty(window, 'location', {
      writable: true,
      configurable: true,
      value: { href: '' },
    })

    await import('@/services/api')
    requestInterceptor = mockAxiosInstance.interceptors.request.use.mock.calls[0][0]
    ;[responseSuccessInterceptor, responseErrorInterceptor] =
      mockAxiosInstance.interceptors.response.use.mock.calls[0]
  })

  it('attaches an Authorization header when a token is present', () => {
    mockUseAuth.mockReturnValue({ token: { value: 'abc123' } })

    const config = requestInterceptor({ headers: {} })

    expect(config.headers['Authorization']).toBe('Bearer abc123')
  })

  it('does not attach an Authorization header when there is no token', () => {
    mockUseAuth.mockReturnValue({ token: { value: '' } })

    const config = requestInterceptor({ headers: {} })

    expect(config.headers['Authorization']).toBeUndefined()
  })

  it('passes successful responses through unchanged', () => {
    const response = { data: 'ok', status: 200 }
    expect(responseSuccessInterceptor(response)).toBe(response)
  })

  it('rejects non-401 errors without touching auth state', async () => {
    const logout = vi.fn()
    mockUseAuth.mockReturnValue({ logout, login: vi.fn(), apiKey: { value: '' } })
    const error = { response: { status: 500 }, config: {} }

    await expect(responseErrorInterceptor(error)).rejects.toBe(error)
    expect(logout).not.toHaveBeenCalled()
  })

  it('on 401 with a stored apiKey, refreshes the token and retries the original request', async () => {
    const login = vi.fn().mockResolvedValue({ success: true })
    const logout = vi.fn()
    mockUseAuth.mockReturnValue({
      login,
      logout,
      apiKey: { value: 'stored-key' },
      token: { value: 'new-token' },
    })
    mockAxiosInstance.mockResolvedValueOnce({ data: 'retried' })

    const originalRequest = { headers: {} }
    const error = { response: { status: 401 }, config: originalRequest }

    const result = await responseErrorInterceptor(error)

    expect(login).toHaveBeenCalledWith('stored-key')
    expect(originalRequest._retry).toBe(true)
    expect(originalRequest.headers['Authorization']).toBe('Bearer new-token')
    expect(mockAxiosInstance).toHaveBeenCalledWith(originalRequest)
    expect(result).toEqual({ data: 'retried' })
    expect(logout).not.toHaveBeenCalled()
  })

  it('on 401 when refresh fails, logs out and redirects to /login', async () => {
    const login = vi.fn().mockResolvedValue({ success: false, error: 'Authentication failed' })
    const logout = vi.fn()
    mockUseAuth.mockReturnValue({
      login,
      logout,
      apiKey: { value: 'stored-key' },
      token: { value: '' },
    })

    const originalRequest = { headers: {} }
    const error = { response: { status: 401 }, config: originalRequest }

    await expect(responseErrorInterceptor(error)).rejects.toBe(error)
    expect(logout).toHaveBeenCalled()
    expect(window.location.href).toBe('/login')
  })

  it('on 401 with no stored apiKey, logs out and redirects immediately', async () => {
    const logout = vi.fn()
    mockUseAuth.mockReturnValue({
      login: vi.fn(),
      logout,
      apiKey: { value: '' },
      token: { value: '' },
    })

    const error = { response: { status: 401 }, config: { headers: {} } }

    await expect(responseErrorInterceptor(error)).rejects.toBe(error)
    expect(logout).toHaveBeenCalled()
    expect(window.location.href).toBe('/login')
  })

  it('on 401 does not retry a request that has already been retried', async () => {
    const login = vi.fn()
    const logout = vi.fn()
    mockUseAuth.mockReturnValue({
      login,
      logout,
      apiKey: { value: 'stored-key' },
      token: { value: '' },
    })

    const originalRequest = { headers: {}, _retry: true }
    const error = { response: { status: 401 }, config: originalRequest }

    await expect(responseErrorInterceptor(error)).rejects.toBe(error)
    expect(login).not.toHaveBeenCalled()
    expect(logout).toHaveBeenCalled()
  })
})
