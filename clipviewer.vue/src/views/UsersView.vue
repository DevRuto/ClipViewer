<script setup>
import { ref, onMounted } from 'vue'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { RefreshCw, Copy, Check, ShieldCheck } from '@lucide/vue'

const users = ref([])
const loading = ref(true)
const error = ref('')

const newUsername = ref('')
const newUserIsAdmin = ref(false)
const creating = ref(false)
const createError = ref('')

const rotatingId = ref(null)

const revealedKey = ref('')
const revealedFor = ref('')
const keyDialogOpen = ref(false)
const copied = ref(false)

async function loadUsers() {
  loading.value = true
  error.value = ''
  try {
    const response = await api.get('/api/users')
    users.value = response.data
  } catch {
    error.value = 'Failed to load users'
  } finally {
    loading.value = false
  }
}

async function createUser() {
  const username = newUsername.value.trim()
  if (!username) {
    createError.value = 'Username is required'
    return
  }

  creating.value = true
  createError.value = ''
  try {
    const response = await api.post('/api/users', {
      username,
      isAdmin: newUserIsAdmin.value,
    })
    users.value.push(response.data)
    users.value.sort((a, b) => a.username.localeCompare(b.username))
    showKey(response.data)
    newUsername.value = ''
    newUserIsAdmin.value = false
  } catch (err) {
    createError.value = err.response?.data || 'Failed to create user'
  } finally {
    creating.value = false
  }
}

async function rotateKey(user) {
  rotatingId.value = user.id
  try {
    const response = await api.post(`/api/users/${user.id}/rotate-key`)
    showKey(response.data)
  } catch {
    error.value = `Failed to rotate key for ${user.username}`
  } finally {
    rotatingId.value = null
  }
}

function showKey(userDto) {
  revealedKey.value = userDto.apiKey
  revealedFor.value = userDto.username
  copied.value = false
  keyDialogOpen.value = true
}

async function copyKey() {
  await navigator.clipboard.writeText(revealedKey.value)
  copied.value = true
}

onMounted(loadUsers)
</script>

<template>
  <div class="container mx-auto px-4 py-8 max-w-3xl">
    <h1 class="text-3xl font-bold mb-8">Manage Users</h1>

    <Card class="mb-8">
      <CardHeader>
        <CardTitle class="text-lg">Create User</CardTitle>
      </CardHeader>
      <CardContent>
        <form class="flex flex-col sm:flex-row sm:items-end gap-4" @submit.prevent="createUser">
          <div class="space-y-2 flex-1">
            <Label for="new-username">Username</Label>
            <Input id="new-username" v-model="newUsername" placeholder="Enter username" :disabled="creating" />
          </div>
          <div class="flex items-center gap-2 pb-2">
            <Switch id="new-user-admin" v-model="newUserIsAdmin" :disabled="creating" />
            <Label for="new-user-admin">Admin</Label>
          </div>
          <Button type="submit" :disabled="creating">
            {{ creating ? 'Creating...' : 'Create User' }}
          </Button>
        </form>
        <Alert v-if="createError" variant="destructive" class="mt-4">
          <AlertDescription>{{ createError }}</AlertDescription>
        </Alert>
      </CardContent>
    </Card>

    <Alert v-if="error" variant="destructive" class="mb-4">
      <AlertDescription>{{ error }}</AlertDescription>
    </Alert>

    <Card>
      <CardContent class="p-0">
        <div v-if="loading" class="p-6 text-center text-muted-foreground">Loading users...</div>
        <div v-else-if="!users.length" class="p-6 text-center text-muted-foreground">No users yet</div>
        <div v-else class="divide-y">
          <div
            v-for="user in users"
            :key="user.id"
            class="flex items-center justify-between gap-4 px-6 py-4"
          >
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="font-medium truncate">{{ user.username }}</span>
                <span
                  v-if="user.role === 'Admin'"
                  class="inline-flex items-center gap-1 text-xs font-medium text-primary bg-primary/10 rounded-full px-2 py-0.5"
                >
                  <ShieldCheck class="size-3" />
                  Admin
                </span>
              </div>
              <p class="text-sm text-muted-foreground">
                Joined {{ new Date(user.createdAt).toLocaleDateString() }}
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              :disabled="rotatingId === user.id"
              @click="rotateKey(user)"
            >
              <RefreshCw class="size-4" :class="{ 'animate-spin': rotatingId === user.id }" />
              Rotate Key
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>

    <AlertDialog v-model:open="keyDialogOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>API Key for {{ revealedFor }}</AlertDialogTitle>
          <AlertDialogDescription>
            This key is shown only once. Copy it now and share it securely - it won't be shown again.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div class="flex items-center gap-2 rounded-md border bg-muted px-3 py-2 font-mono text-sm break-all">
          {{ revealedKey }}
        </div>
        <AlertDialogFooter>
          <Button variant="outline" @click="copyKey">
            <Check v-if="copied" class="size-4" />
            <Copy v-else class="size-4" />
            {{ copied ? 'Copied' : 'Copy' }}
          </Button>
          <AlertDialogAction>Done</AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </div>
</template>
