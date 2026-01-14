<script setup>
import { ref, watch, computed } from 'vue'
import { useAuth } from '@/composables/useAuth'
import { formatDuration } from '@/composables/useDuration.js'

// Debounce utility function
function debounce(fn, delay) {
  let timeoutId
  return function (...args) {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn.apply(this, args), delay)
  }
}

const props = defineProps(['video', 'videoPlayer'])
const emit = defineEmits(['update-video'])

const includeTimestamp = ref(false)
const currentTime = ref(0)

const { user } = useAuth()
const ownsVideo = computed(() => user.value?.username === props.video.author)
const unlisted = ref(props.video.unlisted)
const name = ref(props.video.name)

// Watch for changes and emit to parent
watch(unlisted, (newValue) => {
  emit('update-video', { ...props.video, unlisted: newValue })
})

// Debounced name watcher
const debouncedNameUpdate = debounce((newValue) => {
  emit('update-video', { ...props.video, name: newValue })
}, 500)

watch(name, debouncedNameUpdate)

function copyLink() {
  let url = new URL(window.location.href)

  if (includeTimestamp.value) {
    url.searchParams.set('t', currentTime.value)
  } else {
    url.searchParams.delete('t')
  }

  navigator.clipboard.writeText(url.toString())
}

watch(
  () => props.videoPlayer?.currentTime,
  () => {
    if (props.videoPlayer) {
      currentTime.value = Math.floor(props.videoPlayer.currentTime)
    }
  },
)
</script>

<template>
  <div class="p-6">
    <div v-if="ownsVideo" class="mb-4 space-y-3">
      <!-- Editable title -->
      <input
        v-model="name"
        class="flex-1 min-w-0 text-2xl font-bold text-gray-800 dark:text-white bg-transparent border-b border-transparent hover:border-gray-300 dark:hover:border-gray-600 focus:border-blue-500 focus:outline-none transition-colors break-words line-clamp-2 sm:line-clamp-3"
        placeholder="Video title"
      />

      <!-- Unlisted checkbox -->
      <label
        class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer select-none"
      >
        <input
          type="checkbox"
          v-model="unlisted"
          class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 focus:ring-2 dark:bg-gray-700 dark:border-gray-600"
        />
        <span>Unlisted</span>
      </label>
    </div>

    <h1
      v-else
      class="flex-1 min-w-0 text-2xl font-bold text-gray-800 dark:text-white break-words line-clamp-2 sm:line-clamp-3"
    >
      {{ name }}
    </h1>
    <div class="block my-4 border-t border-gray-200 dark:border-gray-700"></div>
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4">
      <div
        class="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-500 dark:text-gray-400"
      >
        <div class="bg-blue-500 text-white text-xs px-2 py-1 rounded font-medium">
          {{ video.author }}
        </div>
        <span>Duration: {{ formatDuration(video.duration) }}</span>
        <span>Uploaded: {{ new Date(video.createdAt).toLocaleDateString() }}</span>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <!-- Copy button -->
        <button
          @click="copyLink"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-md bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:hover:bg-gray-600 text-sm font-medium text-gray-700 dark:text-white transition cursor-pointer"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M8 16h8a2 2 0 002-2V6a2 2 0 00-2-2H8a2 2 0 00-2 2v8a2 2 0 002 2z"
            />
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M16 8v10a2 2 0 01-2 2H6"
            />
          </svg>
          Copy Link
          <span v-if="includeTimestamp">{{ formatDuration(currentTime) }}</span>
        </button>

        <!-- Timestamp toggle -->
        <label
          class="flex items-center gap-2 text-md text-gray-600 dark:text-gray-400 cursor-pointer select-none"
        >
          <input type="checkbox" v-model="includeTimestamp" class="sr-only peer" />

          <!-- Switch -->
          <div
            class="relative w-9 h-5 bg-gray-200 dark:bg-gray-700 rounded-full peer peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 peer-checked:bg-blue-600 after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:w-4 after:h-4 after:bg-white after:border after:border-gray-300 after:rounded-full after:transition-all peer-checked:after:translate-x-4"
          ></div>

          <span class="whitespace-nowrap"> Timestamp </span>
        </label>

        <div class="block sm:hidden my-4 border-t border-gray-200 dark:border-gray-700"></div>
        <!-- Download button -->
        <a
          :href="video.sourceVideoFile"
          :download="video.name"
          class="inline-flex items-center justify-center px-3 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-md transition-colors"
        >
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
            ></path>
          </svg>
          Download
        </a>
      </div>
    </div>
  </div>
</template>
