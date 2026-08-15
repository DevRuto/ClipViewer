<script setup>
import { ref, onMounted } from 'vue'
import { api } from '@/services/api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDuration } from '@/composables/useDuration'
import { Film, HardDrive, Clock, EyeOff, CalendarDays, Loader } from '@lucide/vue'

const stats = ref(null)
const loading = ref(true)
const error = ref('')

function formatBytes(bytes) {
  if (!bytes) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1)
  const value = bytes / 1024 ** exponent
  return `${value.toFixed(exponent === 0 ? 0 : 1)} ${units[exponent]}`
}

async function loadStats() {
  loading.value = true
  error.value = ''
  try {
    const response = await api.get('/api/stats/me')
    stats.value = response.data
  } catch {
    error.value = 'Failed to load stats'
  } finally {
    loading.value = false
  }
}

onMounted(loadStats)
</script>

<template>
  <div class="container mx-auto px-4 py-8 max-w-3xl">
    <h1 class="text-3xl font-bold mb-8">Your Stats</h1>

    <Alert v-if="error" variant="destructive" class="mb-4">
      <AlertDescription>{{ error }}</AlertDescription>
    </Alert>

    <div v-if="loading" class="grid grid-cols-2 md:grid-cols-3 gap-4">
      <Skeleton v-for="i in 6" :key="i" class="h-24 rounded-xl" />
    </div>

    <template v-else-if="stats">
      <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <Film class="size-4" />
              Total Clips
            </div>
            <div class="text-2xl font-bold">{{ stats.totalClips }}</div>
          </CardContent>
        </Card>

        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <Loader class="size-4" />
              Processing
            </div>
            <div class="text-2xl font-bold">{{ stats.processingClips }}</div>
          </CardContent>
        </Card>

        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <EyeOff class="size-4" />
              Unlisted
            </div>
            <div class="text-2xl font-bold">{{ stats.unlistedClips }}</div>
          </CardContent>
        </Card>

        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <Clock class="size-4" />
              Total Duration
            </div>
            <div class="text-2xl font-bold">{{ formatDuration(stats.totalDurationSeconds) }}</div>
          </CardContent>
        </Card>

        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <HardDrive class="size-4" />
              Storage Used
            </div>
            <div class="text-2xl font-bold">{{ formatBytes(stats.totalStorageBytes) }}</div>
          </CardContent>
        </Card>

        <Card>
          <CardContent class="p-4">
            <div class="flex items-center gap-2 text-muted-foreground text-sm mb-1">
              <CalendarDays class="size-4" />
              Member Since
            </div>
            <div class="text-2xl font-bold">{{ new Date(stats.memberSince).toLocaleDateString() }}</div>
          </CardContent>
        </Card>
      </div>

      <Card class="mt-4" v-if="stats.latestUploadAt">
        <CardHeader>
          <CardTitle class="text-lg">Latest Upload</CardTitle>
        </CardHeader>
        <CardContent class="pt-0 text-muted-foreground">
          {{ new Date(stats.latestUploadAt).toLocaleString() }}
        </CardContent>
      </Card>
    </template>
  </div>
</template>
