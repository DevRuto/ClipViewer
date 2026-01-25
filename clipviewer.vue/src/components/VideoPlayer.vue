<script setup>
import { ref, onMounted } from 'vue'
import 'media-chrome'
import 'hls-video-element'
import '@/external/media-cinema-button.js'

const refVideo = ref(null)
const currentTime = ref(0)
const videoLoaded = ref(false);

const props = defineProps({
  src: {
    type: String,
    required: true,
  },
})

defineExpose({
  goToTime,
  currentTime,
  refVideo
})
const emit = defineEmits(['loaded', 'toggleCinemaMode'])

const isHLS = props.src.toLocaleLowerCase().endsWith('.m3u8')

onMounted(() => {
  refVideo.value.addEventListener('loadeddata', onLoaded)
  refVideo.value.addEventListener('timeupdate', onTimeUpdate)
})

function goToTime(time) {
  refVideo.value.currentTime = time
}

function onLoaded() {
  emit('loaded')
  videoLoaded.value = true
}

function onTimeUpdate() {
  if (refVideo.value) {
    currentTime.value = refVideo.value.currentTime
  }
}

function onCinemaModeToggle(event) {
  emit('toggleCinemaMode', event.detail.isCinemaMode)
}
</script>

<template>
  <media-controller v-show="videoLoaded">
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
      <media-cinema-button
        class="hidden sm:flex"
        @cinema-mode-enabled="onCinemaModeToggle"
        @cinema-mode-disabled="onCinemaModeToggle"
      >
      </media-cinema-button>
      <media-fullscreen-button></media-fullscreen-button>
    </media-control-bar>
  </media-controller>
</template>
