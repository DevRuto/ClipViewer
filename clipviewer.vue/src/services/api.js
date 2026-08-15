import axios from 'axios'
import { useAuth } from '@/composables/useAuth'

const api = axios.create({
  timeout: 10000,
})

api.interceptors.request.use((config) => {
  const { token } = useAuth()
  if (token.value) {
    config.headers['Authorization'] = `Bearer ${token.value}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const originalRequest = error.config

      // A 401 from the login call itself is a bad API key, not an expired token - let it
      // propagate to useAuth.login()'s own catch instead of retrying login from here too.
      if (originalRequest?.url?.includes('/api/auth/login')) {
        return Promise.reject(error)
      }

      const { logout, login, apiKey } = useAuth()

      // If we have an API key and haven't already retried this request, try to refresh the token
      if (apiKey.value && !originalRequest?._retry) {
        originalRequest._retry = true
        try {
          const result = await login(apiKey.value)
          if (result.success) {
            // Retry the original request with new token
            originalRequest.headers['Authorization'] = `Bearer ${useAuth().token.value}`
            return api(originalRequest)
          }
        } catch {
          // Token refresh failed, logout
        }
      }

      // No API key, already retried, or refresh failed, logout
      logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)

export { api }
