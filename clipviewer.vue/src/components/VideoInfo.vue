<script setup>
import { ref, computed, nextTick, onUnmounted, watch } from 'vue'
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
import {
  Pencil,
  Trash2,
  Loader2,
  AlertTriangle,
  CheckCircle2,
  RefreshCw,
  Link as LinkIcon,
  Check,
  Download,
  Calendar,
  Clock,
  EyeOff,
} from '@lucide/vue'

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

let copiedTimeoutId = null

onUnmounted(() => {
  debounceTimeoutIds.forEach(clearTimeout)
  clearTimeout(copiedTimeoutId)
})

const props = defineProps(['video', 'videoPlayer'])
const emit = defineEmits(['update-video', 'delete-video', 'refresh-video', 'retry-video'])

const includeTimestamp = ref(false)
const currentTime = ref(0)
const justCopied = ref(false)

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
const savedName = ref(props.video.name)
const description = ref(props.video.description || '')
const isEditing = ref(false)
const wasProcessing = ref(!props.video.processed)
const titleInputRef = ref(null)

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

// Debounced name watcher - blank/whitespace-only titles are never autosaved
const debouncedNameUpdate = debounce((newValue) => {
  const trimmed = newValue.trim()
  if (!trimmed) return
  savedName.value = trimmed
  emit('update-video', { ...props.video, name: trimmed })
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

// Focus (and select) the title input as soon as edit mode opens
watch(
  isEditing,
  (editing) => {
    if (!editing) return
    nextTick(() => {
      titleInputRef.value?.$el?.focus()
      titleInputRef.value?.$el?.select()
    })
  },
)

function toggleEdit() {
  isEditing.value = !isEditing.value
}

function handleEnterKey(event) {
  event.target.blur()
  isEditing.value = false
}

function handleEscapeKey(event) {
  // Unlike blur/Enter, Escape discards whatever hasn't been saved yet
  name.value = savedName.value
  event.target.blur()
  isEditing.value = false
}

// Deliberately does not close edit mode - the edit panel also holds the unlisted switch and
// the delete confirmation dialog, and that dialog moves focus outside the title input (into a
// teleported AlertDialogContent) as soon as it opens, which would otherwise blur the input and
// collapse the whole panel (dialog included) right as it's opening. Closing edit mode is left to
// the pencil toggle, Enter, or Escape.
function onTitleBlur() {
  // Revert to the last successfully saved title rather than leaving the video nameless
  if (!name.value.trim()) {
    name.value = savedName.value
  }
}

async function copyLink() {
  const url = new URL(window.location.href)

  if (includeTimestamp.value) {
    url.searchParams.set('t', currentTime.value)
  } else {
    url.searchParams.delete('t')
  }

  try {
    await navigator.clipboard.writeText(url.toString())
    justCopied.value = true
    clearTimeout(copiedTimeoutId)
    copiedTimeoutId = setTimeout(() => {
      justCopied.value = false
    }, 2000)
  } catch (err) {
    console.error('Failed to copy link:', err)
  }
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
  <div class="p-4 sm:p-6">
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
          ref="titleInputRef"
          class="flex-1 min-w-0 text-2xl font-bold h-auto py-2"
          placeholder="Video title"
          @blur="onTitleBlur"
          @keydown.enter="handleEnterKey"
          @keydown.esc="handleEscapeKey"
        />
        <h1
          v-else
          class="flex-1 min-w-0 text-2xl font-bold break-words line-clamp-2 sm:line-clamp-3 cursor-text hover:text-muted-foreground transition-colors"
          @click="toggleEdit"
        >
          {{ name }}
        </h1>

        <!-- Persistent unlisted indicator, shown outside of edit mode where the switch already covers it -->
        <span
          v-if="!isEditing && unlisted"
          class="shrink-0 inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium text-muted-foreground"
        >
          <EyeOff class="size-3" />
          Unlisted
        </span>
      </div>

      <!-- Unlisted switch and Delete button - only show when editing -->
      <div v-if="isEditing" class="flex items-center justify-between">
        <!-- Public/Unlisted toggle switch -->
        <label class="flex items-center gap-3 text-sm cursor-pointer select-none">
          <span :class="unlisted ? 'text-muted-foreground' : 'font-medium text-foreground'">Public</span>
          <Switch v-model="unlisted" />
          <span :class="unlisted ? 'font-medium text-foreground' : 'text-muted-foreground'">Unlisted</span>
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
                Are you sure you want to delete "{{ video.name }}"? This action cannot be undone.
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
      v-else-if="isCompleted && wasProcessing"
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

    <!-- Non-owner title (owner's title is rendered above, alongside edit controls) -->
    <h1
      v-if="!ownsVideo"
      class="flex-1 min-w-0 text-2xl font-bold break-words line-clamp-2 sm:line-clamp-3"
    >
      {{ name }}
    </h1>

    <!-- Description: editable textarea for the owner, rendered markdown otherwise -->
    <div v-if="isEditing" class="mb-4 mt-3">
      <Textarea v-model="description" rows="3" placeholder="Add a description..." class="resize-none" />
    </div>
    <div v-else-if="description" class="mb-4 mt-3">
      <div class="text-muted-foreground prose prose-sm max-w-none dark:prose-invert" v-html="renderedDescription"></div>
    </div>

    <Separator class="my-4" />
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4">
      <div class="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
        <div class="text-xs px-2 py-1 rounded font-medium" :style="{ backgroundColor: authorColor, color: textColor }">
          {{ video.author }}
        </div>
        <span class="inline-flex items-center gap-1.5">
          <Clock class="size-3.5" />
          {{ formatDuration(video.duration) }}
        </span>
        <span class="inline-flex items-center gap-1.5">
          <Calendar class="size-3.5" />
          {{ new Date(video.createdAt).toLocaleDateString() }}
        </span>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <!-- Copy button -->
        <Button variant="secondary" size="sm" @click="copyLink">
          <Check v-if="justCopied" class="size-4" />
          <LinkIcon v-else class="size-4" />
          {{ justCopied ? 'Copied!' : 'Copy Link' }}
          <span v-if="includeTimestamp && !justCopied">{{ formatDuration(currentTime) }}</span>
        </Button>

        <!-- Timestamp toggle -->
        <label class="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer select-none">
          <Switch v-model="includeTimestamp" />
          <span class="whitespace-nowrap">Timestamp</span>
        </label>

        <Separator class="block sm:hidden my-4" />
        <!-- Download button -->
        <a :href="video.sourceVideoFile" :download="video.name" :class="buttonVariants({ variant: 'secondary', size: 'sm' })">
          <Download class="size-4" />
          Download
        </a>
      </div>
    </div>
  </div>
</template>
