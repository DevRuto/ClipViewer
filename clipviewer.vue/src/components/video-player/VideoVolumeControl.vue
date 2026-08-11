<script setup>
import { computed } from 'vue'
import { Volume2, Volume1, VolumeX } from '@lucide/vue'
import { playerButtonClass } from './playerButtonClass.js'

const props = defineProps({
  volume: {
    type: Number,
    default: 1,
  },
  muted: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['update:volume', 'toggle-mute'])

const effectiveVolume = computed(() => (props.muted ? 0 : props.volume))
const icon = computed(() => {
  if (effectiveVolume.value === 0) return VolumeX
  if (effectiveVolume.value < 0.5) return Volume1
  return Volume2
})

function onSliderInput(event) {
  emit('update:volume', Number(event.target.value))
}
</script>

<template>
  <div class="group/volume flex items-center">
    <button
      type="button"
      :class="playerButtonClass"
      :title="effectiveVolume === 0 ? 'Unmute' : 'Mute'"
      @click="emit('toggle-mute')"
    >
      <component :is="icon" class="size-[18px]" />
    </button>
    <input
      type="range"
      min="0"
      max="1"
      step="0.05"
      class="volume-slider ml-0.5 w-0 opacity-0 transition-[width,opacity] group-hover/volume:w-20 group-hover/volume:opacity-100 group-focus-within/volume:w-20 group-focus-within/volume:opacity-100 pointer-coarse:w-14 pointer-coarse:opacity-100"
      :value="effectiveVolume"
      :style="{ '--volume-pct': `${effectiveVolume * 100}%` }"
      aria-label="Volume"
      @input="onSliderInput"
    />
  </div>
</template>

<style scoped>
.volume-slider {
  appearance: none;
  height: 4px;
  border-radius: 9999px;
  background: linear-gradient(
    to right,
    var(--primary) var(--volume-pct),
    rgb(255 255 255 / 25%) var(--volume-pct)
  );
  outline: none;
}
.volume-slider::-webkit-slider-thumb {
  appearance: none;
  width: 12px;
  height: 12px;
  border-radius: 9999px;
  background: white;
  cursor: pointer;
}
.volume-slider::-moz-range-thumb {
  width: 12px;
  height: 12px;
  border: none;
  border-radius: 9999px;
  background: white;
  cursor: pointer;
}
</style>
