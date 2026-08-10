<script setup>
import { ref, computed, onUnmounted, watch } from 'vue'
import { useAuth } from '@/composables/useAuth'
import { formatDuration } from '@/composables/useDuration.js'
import { useAuthorColor } from '@/composables/useAuthorColor.js'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { Button, buttonVariants } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Switch } from '@/components/ui/switch'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Progress } from '@/components/ui/progress'
import { Separator } from '@/components/ui/separator'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Pencil, Trash2, Loader2, AlertTriangle, CheckCircle2, RefreshCw, Link as LinkIcon, Download } from '@lucide/vue'

// Debounce utility function
const debounceTimeoutIds = []
function debounce(fn, delay) {
  let timeoutId
  return function (...args) {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn.apply(this, args), delay)
    debounceTimeoutIds.push(timeoutId)
  }
}

onUnmounted(() => {
  debounceTimeoutIds.forEach(clearTimeout)
})

const props = defineProps(['video', 'videoPlayer'])
const emit = defineEmits(['update-video', 'delete-video', 'refresh-video', 'retry-video'])

const includeTimestamp = ref(false)
const currentTime = ref(0)

const { user } = useAuth()
const { stringToColor, getContrastColor } = useAuthorColor()
const ownsVideo = computed(() => user.value?.username === props.video.author)
const authorColor = stringToColor(props.video.author)
const textColor = getContrastColor(authorColor)
const normalizedStatus = computed(() => {
  const status = props.video?.status

  if (status === 'Pending' || status === 'Processing' || status === 'Error' || status === 'Completed') {
    return status
  }

  return props.video?.processed ? 'Completed' : 'Processing'
})

const isProcessing = computed(
  () => normalizedStatus.value === 'Pending' || normalizedStatus.value === 'Processing',
)

const isError = computed(() => normalizedStatus.value === 'Error')
const isCompleted = computed(() => normalizedStatus.value === 'Completed')

const processingProgress = computed(() => {
  const raw = props.video?.progress
  const value = typeof raw === 'number' && Number.isFinite(raw) ? raw : 0
  return Math.min(100, Math.max(0, Math.round(value)))
})

const unlisted = ref(props.video.unlisted)
const name = ref(props.video.name)
const description = ref(props.video.description || '')
const isEditing = ref(false)
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

function confirmDelete() {
  emit('delete-video', props.video)
}

function refreshVideo() {
  emit('refresh-video')
}

function retryVideo() {
  emit('retry-video')
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
        <Button
          variant="ghost"
          size="icon"
          :class="isEditing ? 'text-primary' : 'text-muted-foreground'"
          @click="toggleEdit"
        >
          <Pencil class="size-5" />
        </Button>

        <!-- Title display or input -->
        <Input
          v-if="isEditing"
          v-model="name"
          ref="titleInput"
          class="flex-1 min-w-0 text-2xl font-bold h-auto py-2"
          placeholder="Video title"
          @blur="isEditing = false"
          @keydown.enter="handleEnterKey"
        />
        <h1
          v-else
          class="flex-1 min-w-0 text-2xl font-bold break-words line-clamp-2 sm:line-clamp-3 cursor-text hover:text-muted-foreground transition-colors"
          @click="toggleEdit"
        >
          {{ name }}
        </h1>
      </div>

      <!-- Unlisted switch and Delete button - only show when editing -->
      <div v-if="isEditing" class="flex items-center justify-between">
        <!-- Public/Unlisted toggle switch -->
        <label class="flex items-center gap-3 text-sm text-muted-foreground cursor-pointer select-none">
          <span class="text-sm font-medium">Public</span>
          <Switch v-model="unlisted" />
          <span class="text-sm font-medium">Unlisted</span>
        </label>

        <!-- Delete button -->
        <AlertDialog>
          <AlertDialogTrigger as-child>
            <Button variant="destructive" size="sm">
              <Trash2 class="size-4" />
              Delete Video
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete Video</AlertDialogTitle>
              <AlertDialogDescription>
                Are you sure you want to delete this video? This action cannot be undone.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction :class="buttonVariants({ variant: 'destructive' })" @click="confirmDelete">
                Delete
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </div>

    <!-- Processing indicator -->
    <Alert v-if="isProcessing" class="mb-4 border-yellow-200 bg-yellow-50 dark:border-yellow-800 dark:bg-yellow-900/20">
      <Loader2 class="size-5 animate-spin text-yellow-600" />
      <AlertDescription class="w-full">
        <p class="text-sm font-medium text-yellow-800 dark:text-yellow-200">
          Video status: {{ normalizedStatus }} ({{ processingProgress }}%)
        </p>
        <Progress :model-value="processingProgress" class="mt-2 h-2 [&>div]:bg-yellow-600 bg-yellow-100 dark:bg-yellow-900/40" />
        <p class="text-xs text-yellow-600 dark:text-yellow-400 mt-2">Automatically checking status every 5 seconds</p>
      </AlertDescription>
    </Alert>

    <Alert v-else-if="isError" variant="destructive" class="mb-4">
      <AlertTriangle class="size-5" />
      <AlertDescription class="w-full flex items-center gap-3">
        <div class="flex-1">
          <p class="text-sm font-medium">Video processing failed</p>
          <p class="text-xs">Status: {{ normalizedStatus }}</p>
        </div>
        <Button v-if="ownsVideo" variant="destructive" size="sm" @click="retryVideo">
          <RefreshCw class="size-4" />
          Retry
        </Button>
      </AlertDescription>
    </Alert>

    <!-- Processed success indicator with refresh button -->
    <Alert
      v-else-if="(isCompleted || video.processed) && wasProcessing"
      class="mb-4 border-green-200 bg-green-50 dark:border-green-800 dark:bg-green-900/20"
    >
      <CheckCircle2 class="size-5 text-green-600" />
      <AlertDescription class="w-full flex items-center gap-3">
        <p class="flex-1 text-sm font-medium text-green-800 dark:text-green-200">Video processing complete!</p>
        <Button size="sm" class="bg-green-600 hover:bg-green-700 text-white" @click="refreshVideo">
          <RefreshCw class="size-4" />
          Refresh
        </Button>
      </AlertDescription>
    </Alert>

    <!-- Description section -->
    <div class="mb-4">
      <div v-if="ownsVideo && isEditing" class="mt-3">
        <Textarea v-model="description" rows="3" placeholder="Add a description..." class="resize-none" />
      </div>
      <div v-else-if="description" class="mt-3">
        <div class="text-muted-foreground prose prose-sm max-w-none dark:prose-invert" v-html="renderedDescription"></div>
      </div>
    </div>

    <!-- Non-owner title and description -->
    <div v-if="!ownsVideo">
      <h1 class="flex-1 min-w-0 text-2xl font-bold break-words line-clamp-2 sm:line-clamp-3">
        {{ name }}
      </h1>
      <!-- Description section for non-owners -->
      <div v-if="description" class="mt-3">
        <article class="text-muted-foreground prose prose-sm max-w-none dark:prose-invert" v-html="renderedDescription"></article>
      </div>
    </div>
    <Separator class="my-4" />
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4">
      <div class="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
        <div class="text-xs px-2 py-1 rounded font-medium" :style="{ backgroundColor: authorColor, color: textColor }">
          {{ video.author }}
        </div>
        <span>Duration: {{ formatDuration(video.duration) }}</span>
        <span>Uploaded: {{ new Date(video.createdAt).toLocaleDateString() }}</span>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <!-- Copy button -->
        <Button variant="secondary" size="sm" @click="copyLink">
          <LinkIcon class="size-4" />
          Copy Link
          <span v-if="includeTimestamp">{{ formatDuration(currentTime) }}</span>
        </Button>

        <!-- Timestamp toggle -->
        <label class="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer select-none">
          <Switch v-model="includeTimestamp" />
          <span class="whitespace-nowrap">Timestamp</span>
        </label>

        <Separator class="block sm:hidden my-4" />
        <!-- Download button -->
        <a :href="video.sourceVideoFile" :download="video.name" :class="buttonVariants({ variant: 'default', size: 'sm' })" class="bg-green-600 hover:bg-green-700 text-white">
          <Download class="size-4" />
          Download
        </a>
      </div>
    </div>
  </div>
</template>
