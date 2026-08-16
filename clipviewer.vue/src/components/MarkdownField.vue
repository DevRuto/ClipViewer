<script setup>
import { ref, computed, nextTick } from 'vue'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Bold, Italic, Link2, Code, List } from '@lucide/vue'
import { renderMarkdown } from '@/lib/markdown'

const props = defineProps({
  modelValue: { type: String, default: '' },
})
const emit = defineEmits(['update:modelValue'])

const localValue = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
})

const textareaRef = ref(null)
const previewHtml = computed(() => renderMarkdown(props.modelValue))

// Wraps the current selection (or inserts empty markers at the caret) and restores the
// selection afterwards so repeated clicks (e.g. bold-ing several words in a row) feel natural.
function wrapSelection(before, after = before) {
  const el = textareaRef.value?.$el
  if (!el) return
  const { selectionStart: start, selectionEnd: end, value } = el
  const selected = value.slice(start, end)
  localValue.value = value.slice(0, start) + before + selected + after + value.slice(end)
  nextTick(() => {
    el.focus()
    el.setSelectionRange(start + before.length, start + before.length + selected.length)
  })
}

function insertLink() {
  const el = textareaRef.value?.$el
  if (!el) return
  const { selectionStart: start, selectionEnd: end, value } = el
  const label = value.slice(start, end) || 'link text'
  const insert = `[${label}](url)`
  localValue.value = value.slice(0, start) + insert + value.slice(end)
  nextTick(() => {
    el.focus()
    // Select the "url" placeholder so typing the real address replaces it immediately
    const urlStart = start + label.length + 3
    el.setSelectionRange(urlStart, urlStart + 3)
  })
}

function toggleListPrefix() {
  const el = textareaRef.value?.$el
  if (!el) return
  const { selectionStart: start, selectionEnd: end, value } = el
  const lineStart = value.lastIndexOf('\n', start - 1) + 1
  const nextBreak = value.indexOf('\n', end)
  const lineEnd = nextBreak === -1 ? value.length : nextBreak
  const block = value.slice(lineStart, lineEnd)
  const prefixed = block
    .split('\n')
    .map((line) => `- ${line}`)
    .join('\n')
  localValue.value = value.slice(0, lineStart) + prefixed + value.slice(lineEnd)
  nextTick(() => {
    el.focus()
    el.setSelectionRange(lineStart, lineStart + prefixed.length)
  })
}
</script>

<template>
  <div class="space-y-1">
    <!-- mousedown.prevent keeps focus (and the selection) on the textarea so the toolbar
         buttons never lose track of what to wrap -->
    <div class="flex items-center gap-0.5">
      <Button type="button" variant="ghost" size="icon" class="size-7" title="Bold" @mousedown.prevent @click="wrapSelection('**')">
        <Bold class="size-3.5" />
      </Button>
      <Button type="button" variant="ghost" size="icon" class="size-7" title="Italic" @mousedown.prevent @click="wrapSelection('*')">
        <Italic class="size-3.5" />
      </Button>
      <Button type="button" variant="ghost" size="icon" class="size-7" title="Link" @mousedown.prevent @click="insertLink">
        <Link2 class="size-3.5" />
      </Button>
      <Button type="button" variant="ghost" size="icon" class="size-7" title="Code" @mousedown.prevent @click="wrapSelection('`')">
        <Code class="size-3.5" />
      </Button>
      <Button type="button" variant="ghost" size="icon" class="size-7" title="Bulleted list" @mousedown.prevent @click="toggleListPrefix">
        <List class="size-3.5" />
      </Button>
    </div>

    <Textarea
      ref="textareaRef"
      v-model="localValue"
      rows="4"
      placeholder="Add a description... (markdown supported)"
      class="resize-none"
    />

    <p class="text-xs text-muted-foreground">Preview</p>
    <div class="min-h-12 rounded-md border px-3 py-2 text-sm prose prose-sm max-w-none dark:prose-invert">
      <div v-if="modelValue" v-html="previewHtml"></div>
      <p v-else class="text-muted-foreground">Nothing to preview yet.</p>
    </div>
  </div>
</template>
