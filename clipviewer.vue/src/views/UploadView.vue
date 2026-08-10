<script setup>
import { ref, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/services/api'
import VideoUploadPreview from '@/components/VideoUploadPreview.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Progress } from '@/components/ui/progress'
import { UploadCloud } from '@lucide/vue'

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
      <h1 class="text-3xl font-bold mb-8">Upload New Clip</h1>

      <div class="mx-auto" :class="isEditingMode ? 'max-w-7xl' : 'max-w-4xl'">
        <Card>
          <CardContent>
            <!-- File Drop Area -->
            <div
              v-if="!videoUrl"
              class="border-2 border-dashed rounded-lg p-8 text-center transition-colors"
              :class="[
                dragOver ? 'border-primary bg-primary/5' : 'border-border',
                isUploading ? 'pointer-events-none opacity-50' : 'cursor-pointer hover:border-muted-foreground',
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
                :disabled="isUploading"
                @change="handleFileSelect"
              />

              <UploadCloud class="size-12 text-muted-foreground mx-auto mb-4" />

              <p class="text-muted-foreground mb-2">
                {{ file ? file.name : 'Drop your video file here or click to browse' }}
              </p>
              <p class="text-sm text-muted-foreground/70">Supported formats: MP4, WebM, AVI, MOV</p>
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
            <div class="mt-6 space-y-2">
              <Label for="videoName">Video Name</Label>
              <Input id="videoName" v-model="videoName" type="text" placeholder="Enter video name" :disabled="isUploading" />
            </div>

            <!-- Error Message -->
            <Alert v-if="error" variant="destructive" class="mt-4">
              <AlertDescription>{{ error }}</AlertDescription>
            </Alert>

            <!-- Upload Progress -->
            <div v-if="isUploading" class="mt-6">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-medium">Uploading...</span>
                <span class="text-sm text-muted-foreground">{{ uploadProgress }}%</span>
              </div>
              <Progress :model-value="uploadProgress" />
            </div>

            <!-- Action Buttons -->
            <div class="mt-6 flex gap-3">
              <Button
                class="flex-1"
                :disabled="isUploading || !file || (isEditingMode && !timestamps)"
                @click="uploadVideo"
              >
                {{ isUploading ? 'Uploading...' : (isEditingMode ? 'Upload Clip' : 'Upload Video') }}
              </Button>
              <Button v-if="isUploading" variant="outline" @click="cancelUpload">Cancel</Button>
              <Button v-else variant="outline" @click="router.back()">Cancel</Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  </div>
</template>
