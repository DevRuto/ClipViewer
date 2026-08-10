<script setup>
import { onMounted } from 'vue'
import { Palette, Check } from '@lucide/vue'
import { Button } from '@/components/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { useAccentColor, ACCENT_COLORS } from '@/composables/useAccentColor'

const { accent, setAccent, initAccent } = useAccentColor()

onMounted(() => {
  initAccent()
})
</script>

<template>
  <Popover>
    <PopoverTrigger as-child>
      <Button variant="ghost" size="icon" title="Accent color">
        <Palette class="size-5" />
      </Button>
    </PopoverTrigger>
    <PopoverContent class="w-56" align="end">
      <p class="mb-3 text-sm font-medium">Accent color</p>
      <div class="grid grid-cols-6 gap-2">
        <button
          v-for="color in ACCENT_COLORS"
          :key="color.value"
          type="button"
          class="relative flex size-8 items-center justify-center rounded-full ring-offset-2 ring-offset-popover transition-shadow outline-none focus-visible:ring-2 focus-visible:ring-ring"
          :style="{ backgroundColor: color.swatch }"
          :title="color.label"
          @click="setAccent(color.value)"
        >
          <Check v-if="accent === color.value" class="size-4 text-white drop-shadow" />
        </button>
      </div>
    </PopoverContent>
  </Popover>
</template>
