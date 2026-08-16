<script setup>
import { ref, reactive, computed, nextTick, onUnmounted, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { formatDuration } from '@/composables/useDuration.js'
import { useAuthorColor } from '@/composables/useAuthorColor.js'
import { renderMarkdown } from '@/lib/markdown'
import { Button, buttonVariants } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Switch } from '@/components/ui/switch'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Progress } from '@/components/ui/progress'
import MarkdownField from '@/components/MarkdownField.vue'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
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
  Tag as TagIcon,
  X,
} from '@lucide/vue'

const MAX_TAGS = 15
const MAX_TAG_LENGTH = 30

let copiedTimeoutId = null

onUnmounted(() => {
  clearTimeout(copiedTimeoutId)
})

const props = defineProps(['video', 'videoPlayer', 'saving', 'saveError'])
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

const wasProcessing = ref(!props.video.processed)

// Computed property to render description as markdown
const renderedDescription = computed(() => renderMarkdown(props.video.description))

// The rendered description is clamped to 2 lines by default; Show more/less only appears when
// the content actually overflows that clamp (measured via scrollHeight vs clientHeight).
const descRef = ref(null)
const descExpanded = ref(false)
const descTruncated = ref(false)

function checkDescTruncation() {
  nextTick(() => {
    const el = descRef.value
    descTruncated.value = !!el && el.scrollHeight - el.clientHeight > 1
  })
}

watch(
  renderedDescription,
  () => {
    descExpanded.value = false
    checkDescTruncation()
  },
  { immediate: true },
)

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

// --- Edit modal -------------------------------------------------------
// All edits (name/description/tags/unlisted) are staged in `draft` and only sent to the
// parent as a single combined update on Save - this avoids the previous per-field autosave
// design where independent debounced watchers could race and silently clobber each other.
const dialogOpen = ref(false)
const draft = reactive({ name: '', description: '', tags: [], unlisted: false })
const newTag = ref('')
const nameInputRef = ref(null)

function resetDraft() {
  draft.name = props.video.name
  draft.description = props.video.description || ''
  draft.tags = [...(props.video.tags || [])]
  draft.unlisted = props.video.unlisted
}

function setDialogOpen(value) {
  if (!value && props.saving) return // don't allow closing while a save is in flight
  if (value) resetDraft()
  dialogOpen.value = value
}

// Auto-close on a successful save; on failure stay open so saveError stays visible
watch(
  () => props.saving,
  (isSaving, wasSaving) => {
    if (wasSaving && !isSaving && !props.saveError) {
      dialogOpen.value = false
    }
  },
)

// Focus (and select) the name field each time the modal opens
watch(dialogOpen, (open) => {
  if (!open) return
  nextTick(() => {
    nameInputRef.value?.$el?.focus()
    nameInputRef.value?.$el?.select()
  })
})

function addTag() {
  const tag = newTag.value.trim()
  newTag.value = ''
  if (!tag || tag.length > MAX_TAG_LENGTH) return
  if (draft.tags.length >= MAX_TAGS) return
  if (draft.tags.some((t) => t.toLowerCase() === tag.toLowerCase())) return

  draft.tags = [...draft.tags, tag]
}

function removeTag(tag) {
  draft.tags = draft.tags.filter((t) => t !== tag)
}

function submitEdit() {
  const trimmed = draft.name.trim()
  if (!trimmed) return
  emit('update-video', {
    ...props.video,
    name: trimmed,
    description: draft.description,
    tags: draft.tags,
    unlisted: draft.unlisted,
  })
}

// The `download` attribute replaces the filename entirely rather than just suggesting one, so
// video.name (a user-editable title, not a filename) needs the source file's extension appended
// or the browser saves the download with no extension at all.
const downloadFilename = computed(() => {
  const src = props.video?.sourceVideoFile || ''
  const extMatch = /\.[a-zA-Z0-9]+$/.exec(src)
  const ext = extMatch ? extMatch[0] : ''
  const baseName = props.video?.name || props.video?.videoId || 'video'
  return ext && !baseName.toLowerCase().endsWith(ext.toLowerCase()) ? `${baseName}${ext}` : baseName
})

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
    <!-- Title with edit toggle (owner) / plain read-only title (everyone) -->
    <div class="mb-1 flex items-center gap-2">
      <Button
        v-if="ownsVideo"
        variant="ghost"
        size="icon"
        class="size-10 sm:size-9 text-muted-foreground"
        aria-label="Edit video details"
        @click="setDialogOpen(true)"
      >
        <Pencil class="size-5" />
      </Button>
      <h1 class="flex-1 min-w-0 text-2xl font-bold break-words line-clamp-2 sm:line-clamp-3">
        {{ video.name }}
      </h1>
      <span
        v-if="ownsVideo && video.unlisted"
        class="shrink-0 inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium text-muted-foreground"
      >
        <EyeOff class="size-3" />
        Unlisted
      </span>
    </div>

    <!-- Edit modal (owner only) -->
    <Dialog v-if="ownsVideo" :open="dialogOpen" @update:open="setDialogOpen">
      <DialogContent class="sm:max-w-lg">
        <DialogHeader>
          <div class="flex items-center justify-between gap-3 pr-6">
            <DialogTitle>Edit video</DialogTitle>
            <label class="flex items-center gap-2 shrink-0 text-sm cursor-pointer select-none">
              <span :class="draft.unlisted ? 'text-muted-foreground' : 'font-medium text-foreground'">Public</span>
              <Switch v-model="draft.unlisted" />
              <span :class="draft.unlisted ? 'font-medium text-foreground' : 'text-muted-foreground'">Unlisted</span>
            </label>
          </div>
          <DialogDescription class="sr-only">
            Edit the title, description, tags, and visibility of this video.
          </DialogDescription>
        </DialogHeader>

        <Alert v-if="saveError" variant="destructive">
          <AlertDescription>{{ saveError }}</AlertDescription>
        </Alert>

        <form class="space-y-4" @submit.prevent="submitEdit">
          <Input ref="nameInputRef" v-model="draft.name" placeholder="Video title" />

          <div class="flex flex-wrap items-center gap-2">
            <span
              v-for="t in draft.tags"
              :key="t"
              data-testid="tag-chip"
              class="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium text-muted-foreground"
            >
              {{ t }}
              <button type="button" class="hover:text-destructive" @click="removeTag(t)">
                <X class="size-3" />
              </button>
            </span>
            <Input
              v-if="draft.tags.length < MAX_TAGS"
              v-model="newTag"
              placeholder="Add a tag..."
              class="h-7 w-32 text-xs"
              :maxlength="MAX_TAG_LENGTH"
              @keydown.enter.prevent="addTag"
            />
          </div>

          <MarkdownField v-model="draft.description" />

          <DialogFooter class="sm:justify-between">
            <AlertDialog>
              <AlertDialogTrigger as-child>
                <Button type="button" variant="destructive" size="sm">
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

            <div class="flex gap-2">
              <Button type="button" variant="outline" :disabled="saving" @click="setDialogOpen(false)">
                Cancel
              </Button>
              <Button type="submit" :disabled="saving || !draft.name.trim()">
                <Loader2 v-if="saving" class="size-4 animate-spin" />
                Save
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

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

    <!-- Compact meta strip: author, duration, date, tags, and the primary actions -->
    <div class="mt-3 mb-4 flex flex-wrap items-center gap-x-3 gap-y-2">
      <div class="text-xs px-2 py-1 rounded font-medium" :style="{ backgroundColor: authorColor, color: textColor }">
        {{ video.author }}
      </div>
      <span class="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
        <Clock class="size-3.5" />
        {{ formatDuration(video.duration) }}
      </span>
      <span class="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
        <Calendar class="size-3.5" />
        {{ new Date(video.createdAt).toLocaleDateString() }}
      </span>

      <RouterLink
        v-for="t in video.tags || []"
        :key="t"
        :to="`/browse?tag=${encodeURIComponent(t)}`"
        class="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:border-foreground transition-colors"
      >
        <TagIcon class="size-3" />
        {{ t }}
      </RouterLink>

      <div class="flex-1 min-w-2"></div>

      <!-- Bare for now - no room for the "With timestamp" label in the compact strip -->
      <Switch
        v-model="includeTimestamp"
        class="shrink-0"
        title="Include current timestamp when copying link"
        aria-label="Include current timestamp when copying link"
      />

      <Button
        variant="secondary"
        size="icon"
        class="size-10 sm:size-8 shrink-0"
        :title="justCopied ? 'Copied!' : 'Copy Link'"
        @click="copyLink"
      >
        <Check v-if="justCopied" class="size-4" />
        <LinkIcon v-else class="size-4" />
      </Button>

      <a
        :href="video.sourceVideoFile"
        :download="downloadFilename"
        :class="[buttonVariants({ variant: 'secondary', size: 'icon' }), 'size-10 sm:size-8 shrink-0']"
        title="Download"
      >
        <Download class="size-4" />
      </a>
    </div>

    <!-- Description: clamped rendered markdown -->
    <div v-if="video.description" class="mb-4">
      <div
        ref="descRef"
        class="text-muted-foreground prose prose-sm max-w-none dark:prose-invert"
        :class="{ 'line-clamp-2': !descExpanded }"
        v-html="renderedDescription"
      ></div>
      <button
        v-if="descTruncated || descExpanded"
        type="button"
        class="mt-1 text-xs font-semibold text-primary hover:underline"
        @click="descExpanded = !descExpanded"
      >
        {{ descExpanded ? 'Show less' : 'Show more' }}
      </button>
    </div>
  </div>
</template>
