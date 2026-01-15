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
  (error) => {
    if (error.response?.status === 401) {
      const { logout } = useAuth()
      logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)

export { api }
