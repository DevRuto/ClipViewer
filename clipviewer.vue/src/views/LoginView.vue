<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const router = useRouter()
const { login, user } = useAuth()

const apiKeyInput = ref('')
const loading = ref(false)
const error = ref('')

const handleLogin = async () => {
  loading.value = true
  error.value = ''

  const result = await login(apiKeyInput.value)

  if (result.success) {
    router.push({
      name: 'user-clips',
      params: {
        username: user.value.username,
      },
    })
  } else {
    error.value = result.error
  }

  loading.value = false
}
</script>
<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-200 dark:bg-gray-900">
    <div class="max-w-md w-full space-y-8 p-8">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
          Sign in to ClipViewer
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
          Enter your API key to access your clips
        </p>
      </div>
      <form class="mt-8 space-y-6" @submit.prevent="handleLogin">
        <div>
          <label for="api-key" class="sr-only">API Key</label>
          <input
            id="api-key"
            v-model="apiKeyInput"
            type="password"
            required
            class="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white bg-white dark:bg-gray-800 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
            placeholder="Enter your API key"
          />
        </div>

        <div v-if="error" class="text-red-600 dark:text-red-400 text-sm text-center">
          {{ error }}
        </div>

        <div>
          <button
            type="submit"
            :disabled="loading"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="loading">Signing in...</span>
            <span v-else>Sign in</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
