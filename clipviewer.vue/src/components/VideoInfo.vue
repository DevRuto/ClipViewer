<script setup>
import { ref, watch, computed } from 'vue'
import { useAuth } from '@/composables/useAuth'
import { formatDuration } from '@/composables/useDuration.js'
import { useAuthorColor } from '@/composables/useAuthorColor.js'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

// Debounce utility function
function debounce(fn, delay) {
  let timeoutId
  return function (...args) {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn.apply(this, args), delay)
  }
}

const props = defineProps(['video', 'videoPlayer'])
const emit = defineEmits(['update-video', 'delete-video', 'refresh-video'])

const includeTimestamp = ref(false)
const currentTime = ref(0)

const { user } = useAuth()
const { stringToColor, getContrastColor } = useAuthorColor()
const ownsVideo = computed(() => user.value?.username === props.video.author)
const authorColor = stringToColor(props.video.author)
const textColor = getContrastColor(authorColor)
const isProcessing = computed(() => !props.video.processed)
const unlisted = ref(props.video.unlisted)
const name = ref(props.video.name)
const description = ref(props.video.description || '')
const isEditing = ref(false)
const showDeleteModal = ref(false)
const wasProcessing = ref(!props.video.processed)

// Computed property to render description as markdown
const renderedDescription = computed(() => {
  if (!description.value) return ''
  const rawHtml = marked(description.value)
  return DOMPurify.sanitize(rawHtml)
})

// Watch for changes and emit to parent
watch(unlisted, (newValue) => {
  emit('update-video', { ...props.video, unlisted: newValue })
})

// Debounced name watcher
const debouncedNameUpdate = debounce((newValue) => {
  emit('update-video', { ...props.video, name: newValue })
}, 500)

watch(name, debouncedNameUpdate)

// Debounced description watcher
const debouncedDescriptionUpdate = debounce((newValue) => {
  emit('update-video', { ...props.video, description: newValue })
}, 500)

watch(description, debouncedDescriptionUpdate)

// Watch for video processing status changes
watch(
  () => props.video.processed,
  (newProcessed, oldProcessed) => {
    if (oldProcessed === false && newProcessed === true) {
      // Video just finished processing
      wasProcessing.value = true
      emit('refresh-video')
    }
  },
  { immediate: true }
)

function toggleEdit() {
  isEditing.value = !isEditing.value
}

function handleEnterKey() {
  isEditing.value = false
}

function copyLink() {
  let url = new URL(window.location.href)

  if (includeTimestamp.value) {
    url.searchParams.set('t', currentTime.value)
  } else {
    url.searchParams.delete('t')
  }

  navigator.clipboard.writeText(url.toString())
}

function handleDelete() {
  showDeleteModal.value = true
}

function confirmDelete() {
  emit('delete-video', props.video)
  showDeleteModal.value = false
}

function cancelDelete() {
  showDeleteModal.value = false
}

function refreshVideo() {
  emit('refresh-video')
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
      <!-- Editable title with toggle icon -->
      <div class="flex items-center gap-2">
        <!-- Edit toggle button -->
        <button
          @click="toggleEdit"
          class="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          :class="{
            'text-blue-500': isEditing,
            'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300': !isEditing,
          }"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            ></path>
          </svg>
        </button>

        <!-- Title display or input -->
        <input
          v-if="isEditing"
          v-model="name"
          ref="titleInput"
          class="flex-1 min-w-0 text-2xl font-bold text-gray-800 dark:text-white bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 hover:border-gray-400 dark:hover:border-gray-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:focus:ring-blue-800 focus:outline-none transition-colors break-words line-clamp-2 sm:line-clamp-3"
          placeholder="Video title"
          @blur="isEditing = false"
          @keydown.enter="handleEnterKey"
        />
        <h1
          v-else
          class="flex-1 min-w-0 text-2xl font-bold text-gray-800 dark:text-white break-words line-clamp-2 sm:line-clamp-3 cursor-text hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
          @click="toggleEdit"
        >
          {{ name }}
        </h1>
      </div>

      <!-- Unlisted checkbox and Delete button - only show when editing -->
      <div v-if="isEditing" class="flex items-center justify-between">
        <!-- Public/Unlisted toggle switch -->
        <label
          class="flex items-center gap-3 text-sm text-gray-600 dark:text-gray-400 cursor-pointer select-none"
        >
          <span class="text-sm font-medium">Public</span>
          <div class="relative">
            <input type="checkbox" v-model="unlisted" class="sr-only peer" />
            <div
              class="w-11 h-6 bg-gray-200 dark:bg-gray-700 rounded-full peer peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 peer-checked:bg-blue-600 after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:w-5 after:h-5 after:bg-white after:border after:border-gray-300 after:rounded-full after:transition-all peer-checked:after:translate-x-5"
            ></div>
          </div>
          <span class="text-sm font-medium">Unlisted</span>
        </label>

        <!-- Delete button -->
        <button
          @click="handleDelete"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-md bg-red-600 hover:bg-red-700 text-white text-sm font-medium transition-colors"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            ></path>
          </svg>
          Delete Video
        </button>
      </div>
    </div>

    <!-- Processing indicator -->
    <div v-if="isProcessing" class="mb-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
      <div class="flex items-center gap-3">
        <div class="animate-spin rounded-full h-5 w-5 border-2 border-yellow-600 border-t-transparent"></div>
        <div class="flex-1">
          <p class="text-sm font-medium text-yellow-800 dark:text-yellow-200">Video is being processed</p>
          <p class="text-xs text-yellow-600 dark:text-yellow-400">Automatically checking status every 5 seconds</p>
        </div>
      </div>
    </div>

    <!-- Processed success indicator with refresh button -->
    <div v-else-if="video.processed && wasProcessing" class="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
      <div class="flex items-center gap-3">
        <div class="w-5 h-5 bg-green-600 rounded-full flex items-center justify-center">
          <svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"></path>
          </svg>
        </div>
        <div class="flex-1">
          <p class="text-sm font-medium text-green-800 dark:text-green-200">Video processing complete!</p>
        </div>
        <button
          @click="refreshVideo"
          class="inline-flex items-center gap-2 px-3 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-md transition-colors"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
          </svg>
          Refresh
        </button>
      </div>
    </div>

    <!-- Description section -->
    <div class="mb-4">
      <div v-if="ownsVideo && isEditing" class="mt-3">
        <textarea
          v-model="description"
          class="w-full text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 hover:border-gray-400 dark:hover:border-gray-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 dark:focus:ring-blue-800 focus:outline-none transition-colors resize-none"
          rows="3"
          placeholder="Add a description..."
        ></textarea>
      </div>
      <div v-else-if="description" class="mt-3">
        <div
          class="text-gray-700 dark:text-gray-300 prose prose-sm max-w-none dark:prose-invert"
          v-html="renderedDescription"
        ></div>
      </div>
    </div>

    <!-- Non-owner title and description -->
    <div v-if="!ownsVideo">
      <h1
        class="flex-1 min-w-0 text-2xl font-bold text-gray-800 dark:text-white break-words line-clamp-2 sm:line-clamp-3"
      >
        {{ name }}
      </h1>
      <!-- Description section for non-owners -->
      <div v-if="description" class="mt-3">
        <article
          class="text-gray-700 dark:text-gray-300 prose prose-sm max-w-none dark:prose-invert"
          v-html="renderedDescription"
        ></article>
      </div>
    </div>
    <div class="block my-4 border-t border-gray-200 dark:border-gray-700"></div>
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4">
      <div
        class="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-500 dark:text-gray-400"
      >
        <div
          class="text-xs px-2 py-1 rounded font-medium"
          :style="{ backgroundColor: authorColor, color: textColor }"
        >
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

    <!-- Delete Confirmation Modal -->
    <div
      v-if="showDeleteModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click="cancelDelete"
    >
      <div class="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-sm mx-4 shadow-xl" @click.stop>
        <div class="flex items-center gap-3 mb-4">
          <div
            class="flex-shrink-0 w-10 h-10 bg-red-100 dark:bg-red-900 rounded-full flex items-center justify-center"
          >
            <svg
              class="w-5 h-5 text-red-600 dark:text-red-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              ></path>
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Delete Video</h3>
        </div>

        <p class="text-gray-600 dark:text-gray-300 mb-6">
          Are you sure you want to delete this video? This action cannot be undone.
        </p>

        <div class="flex gap-3 justify-end">
          <button
            @click="cancelDelete"
            class="px-4 py-2 text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-md font-medium transition-colors"
          >
            Cancel
          </button>
          <button
            @click="confirmDelete"
            class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-md font-medium transition-colors"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
