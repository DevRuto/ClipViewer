<script setup>
import { Settings } from '@lucide/vue'
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent } from '@/components/ui/dropdown-menu'
import { Switch } from '@/components/ui/switch'
import { playerButtonClass } from './playerButtonClass.js'

const props = defineProps({
  doubleTapSeek: {
    type: Boolean,
    default: true,
  },
})

const emit = defineEmits(['update:doubleTapSeek'])
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <button type="button" :class="playerButtonClass" title="Settings">
        <Settings class="size-[18px]" />
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" side="top" :side-offset="10">
      <label class="flex cursor-pointer items-center gap-3 rounded-sm px-2 py-2 text-sm whitespace-nowrap select-none">
        <Switch :model-value="props.doubleTapSeek" @update:model-value="emit('update:doubleTapSeek', $event)" />
        <span>Double-tap to seek</span>
      </label>
    </DropdownMenuContent>
  </DropdownMenu>
</template>
