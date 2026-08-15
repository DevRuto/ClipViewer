<script setup>
import { ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import ClipTile from './ClipTile.vue'
import { api } from '@/services/api'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Film } from '@lucide/vue'

const videos = ref([])
const error = ref('')

const props = defineProps({
  username: {
    type: String,
    default: null,
  },
  tag: {
    type: String,
    default: null,
  },
})

async function fetchVideos() {
  error.value = ''
  try {
    const params = new URLSearchParams()
    if (props.username) params.set('user', props.username)
    if (props.tag) params.set('tag', props.tag)
    const query = params.toString()
    const res = await api.get(`/api/videos${query ? `?${query}` : ''}`)
    videos.value = res.data
  } catch (err) {
    console.error('Failed to fetch videos:', err)
    error.value = err.response?.data?.message || 'Failed to load clips. Please try again.'
  }
}

watch(
  () => [props.username, props.tag],
  () => {
    videos.value = []
    fetchVideos()
  },
)
</script>

<template>
  <Alert v-if="error" variant="destructive">
    <AlertDescription>{{ error }}</AlertDescription>
  </Alert>
  <div v-else-if="videos.length > 0" class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    <RouterLink
      v-for="video in videos"
      :key="video.id"
      :to="`/clips/${video.videoId}`"
      class="group block bg-card text-card-foreground rounded-lg overflow-hidden border shadow-sm hover:shadow-md transition-shadow duration-200"
    >
      <ClipTile :video="video" />
    </RouterLink>
  </div>
  <div v-else class="text-center py-12">
    <Film class="size-16 text-muted-foreground mx-auto mb-4" />
    <p class="text-muted-foreground text-lg mb-2">No clips uploaded yet</p>
    <p class="text-muted-foreground/70">Upload your first video to get started</p>
  </div>
</template>
