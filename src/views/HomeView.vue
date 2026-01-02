<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const inputValue = ref('')

const extractVideoId = (input) => {
  const trimmed = input.trim()
  // Check if it's a full URL
  const urlMatch = trimmed.match(
    /(?:https?:\/\/)?(?:share\.ruto\.sh\/u\/)?([a-zA-Z0-9]+)(?:\.mp4)?/,
  )
  if (urlMatch && urlMatch[1]) {
    return urlMatch[1]
  }
  return null
}

const loadVideo = () => {
  const videoId = extractVideoId(inputValue.value)
  if (videoId) {
    router.push(`/v/${videoId}`)
    inputValue.value = ''
  }
}

const handleKeypress = (e) => {
  if (e.key === 'Enter') {
    loadVideo()
  }
}
</script>

<template>
  <div class="min-h-screen bg-black flex flex-col items-center justify-center">
    <div class="w-full max-w-2xl px-4">
      <!-- Logo/Title -->
      <div class="text-center mb-12">
        <h1 class="text-4xl font-bold text-white mb-2">ClipViewer</h1>
        <p class="text-gray-400">Share and watch your clips with timestamps</p>
      </div>

      <!-- Input Section -->
      <div class="space-y-4">
        <div class="space-y-2">
          <label class="block text-gray-300 text-sm font-medium">Enter Video URL or ID</label>
          <input
            v-model="inputValue"
            type="text"
            placeholder="e.g., zAJfeM or https://share.ruto.sh/u/zAJfeM.mp4"
            class="w-full px-4 py-3 bg-gray-800 text-white placeholder-gray-500 rounded-lg border border-gray-700 focus:border-red-600 focus:outline-none transition-colors"
            @keypress="handleKeypress"
          />
        </div>

        <button
          @click="loadVideo"
          class="w-full px-6 py-3 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg transition-colors"
        >
          Load Clip
        </button>
      </div>

      <!-- Example Section -->
      <div class="mt-12 p-6 bg-gray-900 rounded-lg border border-gray-800">
        <h2 class="text-white font-semibold mb-3">Examples</h2>
        <div class="space-y-2 text-sm text-gray-400">
          <p><code class="bg-gray-800 px-2 py-1 rounded">zAJfeM</code> - Just the video ID</p>
          <p>
            <code class="bg-gray-800 px-2 py-1 rounded">share.ruto.sh/u/zAJfeM.mp4</code> - Full URL
          </p>
          <p>
            <code class="bg-gray-800 px-2 py-1 rounded">https://share.ruto.sh/u/zAJfeM.mp4</code> -
            Full URL with protocol
          </p>
          <p class="mt-4 text-gray-500">
            After loading a clip, use the "Copy Link" button to generate shareable URLs with
            timestamps!
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
