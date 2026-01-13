<script setup>
import 'media-chrome'
import 'hls-video-element'

const props = defineProps({
  src: {
    type: String,
    required: true,
  },
})

const isHLS = props.src.toLocaleLowerCase().endsWith('.m3u8')
</script>

<template>
  <media-controller>
    <video v-if="!isHLS" :src="props.src" slot="media"></video>
    <hls-video v-else :src="props.src" crossorigin slot="media"></hls-video>

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
