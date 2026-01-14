import { ref, computed, readonly } from 'vue'
import { api } from '../services/api'

const user = ref(null)
const apiKey = ref(localStorage.getItem('apiKey') || '')

export const useAuth = () => {
  const isAuthenticated = computed(() => !!user.value)

  const login = async (key) => {
    try {
      apiKey.value = key
      localStorage.setItem('apiKey', key)

      const response = await api.get('/api/auth/me')
      user.value = response.data
      return { success: true }
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
    apiKey.value = ''
    localStorage.removeItem('apiKey')
  }

  const checkAuth = async () => {
    if (!apiKey.value) return false

    try {
      const response = await api.get('/api/auth/me')
      user.value = response.data
      return true
    } catch {
      logout()
      return false
    }
  }

  return {
    user: readonly(user),
    apiKey: readonly(apiKey),
    isAuthenticated,
    login,
    logout,
    checkAuth,
  }
}
