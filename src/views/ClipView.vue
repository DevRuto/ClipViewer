<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import VideoPlayer from '@/components/VideoPlayer.vue'

const route = useRoute()
const router = useRouter()

const videoId = computed(() => route.params.videoId)

const goHome = () => {
  router.push('/')
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
        <VideoPlayer :key="videoId" :video-id="videoId" />

        <!-- Video ID Display -->
        <div class="bg-gray-900 p-4 rounded text-sm">
          <p class="text-gray-400">
            Video ID: <code class="text-green-400 font-mono">{{ videoId }}</code>
          </p>
          <p class="text-gray-500 text-xs mt-2">
            Direct link:
            <a
              :href="`https://share.ruto.sh/u/${videoId}.mp4`"
              target="_blank"
              rel="noopener"
              class="text-blue-400 hover:underline"
            >
              share.ruto.sh/u/{{ videoId }}.mp4
            </a>
          </p>
        </div>
      </div>
    </main>
  </div>
</template>
