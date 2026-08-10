<script setup>
import { ref, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/services/api'
import VideoUploadPreview from '@/components/VideoUploadPreview.vue'

const router = useRouter()

const file = ref(null)
const videoName = ref('')
const isUploading = ref(false)
const uploadProgress = ref(0)
const error = ref('')
const dragOver = ref(false)
const videoUrl = ref('')
const isEditingMode = ref(false)
const timestamps = ref(null)
let uploadAbortController = null

function handleFileSelect(event) {
  const selectedFile = event.target.files[0]
  if (selectedFile && selectedFile.type.startsWith('video/')) {
    file.value = selectedFile
    videoUrl.value = URL.createObjectURL(selectedFile)
    if (!videoName.value) {
      videoName.value = selectedFile.name.replace(/\.[^/.]+$/, '')
    }
    error.value = ''
  } else {
    error.value = 'Please select a valid video file'
  }
}

function handleDrop(event) {
  event.preventDefault()
  dragOver.value = false
  const droppedFile = event.dataTransfer.files[0]
  if (droppedFile && droppedFile.type.startsWith('video/')) {
    file.value = droppedFile
    videoUrl.value = URL.createObjectURL(droppedFile)
    if (!videoName.value) {
      videoName.value = droppedFile.name.replace(/\.[^/.]+$/, '')
    }
    error.value = ''
  } else {
    error.value = 'Please drop a valid video file'
  }
}

function handleDragOver(event) {
  event.preventDefault()
  dragOver.value = true
}

function handleDragLeave() {
  dragOver.value = false
}

async function uploadVideo() {
  if (!file.value) {
    error.value = 'Please select a video file'
    return
  }

  if (!videoName.value.trim()) {
    error.value = 'Please enter a video name'
    return
  }

  if (isEditingMode.value && (!timestamps.value || timestamps.value.startTime === undefined || timestamps.value.endTime === undefined)) {
    error.value = 'Please set valid start and end times for the clip'
    return
  }

  isUploading.value = true
  error.value = ''
  uploadProgress.value = 0

  try {
    let url = `/api/upload?name=${encodeURIComponent(videoName.value.trim())}`

    // Add timestamps if in edit mode
    // Format: startTime and endTime as seconds from beginning of video
    // Example: &startTime=30&endTime=120 (for a 30-90 second clip)
    if (isEditingMode.value &&
        timestamps.value &&
        timestamps.value.startTime !== undefined &&
        timestamps.value.endTime !== undefined &&
        timestamps.value.startTime >= 0 &&
        timestamps.value.endTime > timestamps.value.startTime &&
        !(timestamps.value.startTime === 0 && timestamps.value.endTime === Math.floor(timestamps.value.videoDuration))) {
          // Skip adding timestamps if the entire video is being uploaded
          url += `&startTime=${timestamps.value.startTime}&endTime=${timestamps.value.endTime}`
    }

    uploadAbortController = new AbortController()
    const response = await api.post(
      url,
      file.value,
      {
        headers: {
          'Content-Type': file.value.type,
        },
        onUploadProgress: (progressEvent) => {
          uploadProgress.value = Math.round((progressEvent.loaded * 100) / progressEvent.total)
        },
        timeout: 0,
        signal: uploadAbortController.signal,
      },
    )

    if (response.status === 202) {
      // Redirect to the uploaded video page
      router.push(`/clips/${response.data.videoId}`)
    }
  } catch (err) {
    if (err.code !== 'ERR_CANCELED') {
      error.value = err.response?.data?.message || 'Upload failed. Please try again.'
    }
  } finally {
    isUploading.value = false
    uploadAbortController = null
  }
}

function cancelUpload() {
  if (isUploading.value) {
    uploadAbortController?.abort()
    isUploading.value = false
    uploadProgress.value = 0
  }
}

function clearVideoPreview() {
  if (videoUrl.value) {
    URL.revokeObjectURL(videoUrl.value)
    videoUrl.value = ''
  }
  file.value = null
  timestamps.value = null
  videoName.value = ''
}

function onTimestampsChange(newTimestamps) {
  timestamps.value = newTimestamps
}

onUnmounted(() => {
  if (videoUrl.value) {
    URL.revokeObjectURL(videoUrl.value)
  }
})
</script>

<template>
  <div class="min-h-screen">
    <div class="container mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-800 dark:text-white mb-8">Upload New Clip</h1>

      <div class="mx-auto" :class="isEditingMode ? 'max-w-7xl' : 'max-w-4xl'">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
          <!-- File Drop Area -->
          <div
            v-if="!videoUrl"
            class="border-2 border-dashed rounded-lg p-8 text-center transition-colors"
            :class="[
              dragOver
                ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                : 'border-gray-300 dark:border-gray-600',
              isUploading
                ? 'pointer-events-none opacity-50'
                : 'cursor-pointer hover:border-gray-400 dark:hover:border-gray-500',
            ]"
            @drop="handleDrop"
            @dragover="handleDragOver"
            @dragleave="handleDragLeave"
            @click="$refs.fileInput.click()"
          >
            <input
              ref="fileInput"
              type="file"
              accept="video/*"
              class="hidden"
              @change="handleFileSelect"
              :disabled="isUploading"
            />

            <svg
              class="w-12 h-12 text-gray-400 dark:text-gray-500 mx-auto mb-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
              ></path>
            </svg>

            <p class="text-gray-600 dark:text-gray-400 mb-2">
              {{ file ? file.name : 'Drop your video file here or click to browse' }}
            </p>
            <p class="text-sm text-gray-500 dark:text-gray-500">
              Supported formats: MP4, WebM, AVI, MOV
            </p>
          </div>

          <!-- Video Upload Preview Component -->
          <VideoUploadPreview
            v-else
            :video-url="videoUrl"
            :file="file"
            :is-editing-mode="isEditingMode"
            @clear-preview="clearVideoPreview"
            @timestamps-change="onTimestampsChange"
            @toggle-edit-mode="isEditingMode = !isEditingMode"
          />

          <!-- Video Name Input -->
          <div class="mt-6">
            <label
              for="videoName"
              class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
            >
              Video Name
            </label>
            <input
              id="videoName"
              v-model="videoName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
              placeholder="Enter video name"
              :disabled="isUploading"
            />
          </div>

          <!-- Error Message -->
          <div
            v-if="error"
            class="mt-4 p-3 bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-600 rounded-md"
          >
            <p class="text-red-700 dark:text-red-400 text-sm">{{ error }}</p>
          </div>

          <!-- Upload Progress -->
          <div v-if="isUploading" class="mt-6">
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Uploading...</span>
              <span class="text-sm text-gray-600 dark:text-gray-400">{{ uploadProgress }}%</span>
            </div>
            <div class="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                class="bg-blue-600 h-2 rounded-full transition-all duration-300 ease-out"
                :style="{ width: `${uploadProgress}%` }"
              ></div>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="mt-6 flex gap-3">
            <button
              @click="uploadVideo"
              :disabled="isUploading || !file || (isEditingMode && !timestamps)"
              class="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-medium py-2 px-4 rounded-lg transition duration-200"
            >
              {{ isUploading ? 'Uploading...' : (isEditingMode ? 'Upload Clip' : 'Upload Video') }}
            </button>
            <button
              v-if="isUploading"
              @click="cancelUpload"
              class="px-4 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-200"
            >
              Cancel
            </button>
            <button
              v-else
              @click="router.back()"
              class="px-4 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-200"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
