<script setup>
import { Gauge } from '@lucide/vue'
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
} from '@/components/ui/dropdown-menu'
import { playerButtonClass } from './playerButtonClass.js'

const RATES = [0.5, 0.75, 1, 1.25, 1.5, 1.75, 2]

const props = defineProps({
  rate: {
    type: Number,
    default: 1,
  },
})

const emit = defineEmits(['update:rate'])

function onRateSelect(value) {
  emit('update:rate', Number(value))
}
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <button type="button" :class="[playerButtonClass, 'relative']" title="Playback speed">
        <Gauge class="size-[18px]" />
        <span
          v-if="props.rate !== 1"
          class="bg-primary text-primary-foreground absolute -right-1 -bottom-1 rounded-full px-1 text-[9px] leading-tight font-semibold"
        >
          {{ props.rate }}x
        </span>
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" side="top" :side-offset="10" class="min-w-24">
      <DropdownMenuRadioGroup :model-value="String(props.rate)" @update:model-value="onRateSelect">
        <DropdownMenuRadioItem v-for="r in RATES" :key="r" :value="String(r)">
          {{ r === 1 ? 'Normal' : `${r}x` }}
        </DropdownMenuRadioItem>
      </DropdownMenuRadioGroup>
    </DropdownMenuContent>
  </DropdownMenu>
</template>
