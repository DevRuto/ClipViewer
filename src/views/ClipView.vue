<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import VideoPlayer from '@/components/VideoPlayer.vue'

const route = useRoute()
const router = useRouter()
const playerRef = ref(null)

const videoId = computed(() => route.params.videoId)

const goHome = () => {
  router.push('/')
}

const metadata = ref({
  width: 0,
  height: 0,
  duration: 0,
  fileSize: 0,
})

function onVideoMetadata(data) {
  metadata.value = data
}

const formatFileSize = (bytes) => {
  if (!bytes || bytes === 0) return 'N/A'
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + ' ' + sizes[i]
}

const formatDuration = (seconds) => {
  if (!seconds || !isFinite(seconds)) return 'N/A'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = Math.floor(seconds % 60)

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }
  return `${minutes}:${String(secs).padStart(2, '0')}`
}
</script>

<template>
  <div class="min-h-screen bg-black">
    <!-- Header -->
    <header class="bg-gray-900 border-b border-gray-800 py-4">
      <div class="max-w-4xl mx-auto px-4 flex items-center justify-between">
        <h1 class="text-2xl font-bold text-white">ClipViewer</h1>
        <button
          @click="goHome"
          class="px-4 py-2 text-gray-300 hover:text-white hover:bg-gray-800 rounded transition-colors"
        >
          ← Back
        </button>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-4xl mx-auto px-4 py-8">
      <div class="space-y-6">
        <VideoPlayer
          ref="playerRef"
          :key="videoId"
          :video-id="videoId"
          @video-loaded="onVideoMetadata"
        />

        <!-- Video Info Display -->
        <div class="bg-gray-900 p-4 rounded">
          <div class="space-y-3">
            <!-- Video ID -->
            <div class="text-sm">
              <p class="text-gray-400">Video ID:</p>
              <code class="text-green-400 font-mono">{{ videoId }}</code>
            </div>

            <!-- Video Details Grid -->
            <div class="grid grid-cols-2 gap-4 pt-2">
              <div>
                <p class="text-gray-500 text-xs">Resolution</p>
                <p class="text-white font-mono">{{ metadata.width }}x{{ metadata.height }}</p>
              </div>
              <div>
                <p class="text-gray-500 text-xs">Duration</p>
                <p class="text-white font-mono">{{ formatDuration(metadata.duration) }}</p>
              </div>
              <div>
                <p class="text-gray-500 text-xs">File Size</p>
                <p class="text-white font-mono">{{ formatFileSize(metadata.fileSize) }}</p>
              </div>
              <div>
                <p class="text-gray-500 text-xs">Format</p>
                <p class="text-white font-mono">MP4</p>
              </div>
            </div>

            <!-- Direct Link -->
            <div class="border-t border-gray-700 pt-3">
              <p class="text-gray-500 text-xs">Direct Link:</p>
              <a
                :href="`https://share.ruto.sh/u/${videoId}.mp4`"
                target="_blank"
                rel="noopener"
                class="text-blue-400 hover:underline text-sm break-all"
              >
                share.ruto.sh/u/{{ videoId }}.mp4
              </a>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>
