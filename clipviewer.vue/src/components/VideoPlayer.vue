<script setup>
import { ref, onMounted } from 'vue'
import 'media-chrome'
import 'hls-video-element'

const props = defineProps({
  src: {
    type: String,
    required: true,
  },
})
defineExpose({
  goToTime,
})
const emit = defineEmits(['loaded'])

const refVideo = ref(null)
const isHLS = props.src.toLocaleLowerCase().endsWith('.m3u8')

onMounted(() => {
  refVideo.value.addEventListener('loadeddata', onLoaded)
})

function goToTime(time) {
  refVideo.value.currentTime = time
}

function onLoaded() {
  emit('loaded')
}
</script>

<template>
  <media-controller>
    <video v-if="!isHLS" :src="props.src" slot="media" ref="refVideo"></video>
    <hls-video v-else :src="props.src" crossorigin slot="media" ref="refVideo"></hls-video>

    <media-loading-indicator slot="centered-chrome" noautohide></media-loading-indicator>
    <media-control-bar>
      <media-play-button></media-play-button>
      <media-seek-backward-button seekoffset="10"></media-seek-backward-button>
      <media-seek-forward-button seekoffset="10"></media-seek-forward-button>
      <media-mute-button></media-mute-button>
      <media-volume-range></media-volume-range>
      <media-time-range></media-time-range>
      <media-time-display showduration remaining></media-time-display>
      <media-playback-rate-button></media-playback-rate-button>
      <media-fullscreen-button></media-fullscreen-button>
    </media-control-bar>
  </media-controller>
</template>
