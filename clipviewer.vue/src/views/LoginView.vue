<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'

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
  <div class="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4">
    <Card class="w-full max-w-md">
      <CardHeader class="text-center">
        <CardTitle class="text-2xl">Sign in to ClipViewer</CardTitle>
        <CardDescription>Enter your API key to access your clips</CardDescription>
      </CardHeader>
      <CardContent>
        <form class="space-y-4" @submit.prevent="handleLogin">
          <div class="space-y-2">
            <Label for="api-key">API Key</Label>
            <Input id="api-key" v-model="apiKeyInput" type="password" required placeholder="Enter your API key" />
          </div>

          <Alert v-if="error" variant="destructive">
            <AlertDescription>{{ error }}</AlertDescription>
          </Alert>

          <Button type="submit" class="w-full" :disabled="loading">
            {{ loading ? 'Signing in...' : 'Sign in' }}
          </Button>
        </form>
      </CardContent>
    </Card>
  </div>
</template>
