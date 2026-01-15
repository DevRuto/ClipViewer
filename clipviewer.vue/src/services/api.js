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
      const { logout, login, apiKey } = useAuth()

      // If we have an API key, try to refresh the token
      if (apiKey.value) {
        try {
          const result = await login(apiKey.value)
          if (result.success) {
            // Retry the original request with new token
            const originalRequest = error.config
            originalRequest.headers['Authorization'] = `Bearer ${useAuth().token.value}`
            return api(originalRequest)
          }
        } catch {
          // Token refresh failed, logout
        }
      }

      // No API key or refresh failed, logout
      logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)

export { api }
