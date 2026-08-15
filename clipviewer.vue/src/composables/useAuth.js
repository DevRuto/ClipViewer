import { ref, computed, readonly } from 'vue'
import { api } from '@/services/api'

const user = ref(null)
const token = ref(localStorage.getItem('token') || '')
const apiKey = ref(localStorage.getItem('apiKey') || '')

export const useAuth = () => {
  const isAuthenticated = computed(() => !!user.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  const login = async (key) => {
    try {
      // Store API key for future token refresh
      apiKey.value = key
      localStorage.setItem('apiKey', key)

      const response = await api.post('/api/auth/login', { apiKey: key })

      if (response.data.token) {
        token.value = response.data.token
        localStorage.setItem('token', response.data.token)

        // Get user info with the new token
        const userResponse = await api.get('/api/auth/me')
        user.value = userResponse.data
        return { success: true }
      }
    } catch {
      logout()
      return {
        success: false,
        error: 'Authentication failed',
      }
    }
  }

  const logout = () => {
    user.value = null
    token.value = ''
    apiKey.value = ''
    localStorage.removeItem('token')
    localStorage.removeItem('apiKey')
  }

  const checkAuth = async () => {
    if (!token.value && apiKey.value) {
      // Try to refresh token using stored API key
      return await login(apiKey.value).then((result) => result.success)
    }

    if (!token.value) return false

    try {
      const response = await api.get('/api/auth/me')
      user.value = response.data
      return true
    } catch {
      // Token might be expired, try to refresh
      if (apiKey.value) {
        return await login(apiKey.value).then((result) => result.success)
      }
      logout()
      return false
    }
  }

  return {
    user: readonly(user),
    token: readonly(token),
    apiKey: readonly(apiKey),
    isAuthenticated,
    isAdmin,
    login,
    logout,
    checkAuth,
  }
}
