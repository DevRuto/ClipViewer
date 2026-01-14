import axios from 'axios'
import { useAuth } from '../composables/useAuth'

const api = axios.create({
  timeout: 10000,
})

api.interceptors.request.use((config) => {
  const { apiKey } = useAuth()
  if (apiKey.value) {
    config.headers['X-Api-Key'] = apiKey.value
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
