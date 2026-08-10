<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
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
  placeholder: {
    type: String,
    default: null,
  },
})

defineExpose({
  goToTime,
  currentTime,
  refVideo
})
const emit = defineEmits(['loaded', 'toggleCinemaMode'])

const isHLS = computed(() => props.src.toLocaleLowerCase().endsWith('.m3u8'))

function attachListeners() {
  if (!refVideo.value) return
  refVideo.value.addEventListener('loadeddata', onLoaded)
  refVideo.value.addEventListener('timeupdate', onTimeUpdate)
}

function detachListeners() {
  if (!refVideo.value) return
  refVideo.value.removeEventListener('loadeddata', onLoaded)
  refVideo.value.removeEventListener('timeupdate', onTimeUpdate)
}

onMounted(() => {
  attachListeners()
})

onBeforeUnmount(() => {
  detachListeners()
})

// isHLS switches the underlying element between <video> and <hls-video> (v-if/v-else), which
// unmounts/remounts a new DOM node - re-bind listeners to it once the swap has happened.
watch(isHLS, async () => {
  videoLoaded.value = false
  await nextTick()
  attachListeners()
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
  <!-- Loading state -->
  <div
    v-if="!videoLoaded"
    class="aspect-video bg-muted bg-cover bg-center flex items-center justify-center relative"
    :style="placeholder ? { backgroundImage: `url(${placeholder})` } : undefined"
  >
    <div class="absolute inset-0" :class="placeholder ? 'bg-black/40' : ''"></div>
    <div class="text-center relative">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-primary mb-4"></div>
      <p class="text-sm" :class="placeholder ? 'text-gray-100' : 'text-muted-foreground'">Loading video...</p>
    </div>
  </div>

  <!-- Video player -->
  <media-controller v-show="videoLoaded">
    <video v-if="!isHLS" :src="props.src" :poster="placeholder" slot="media" ref="refVideo"></video>
    <hls-video v-else :src="props.src" :poster="placeholder" crossorigin slot="media" ref="refVideo"></hls-video>
    <div class="center" slot="centered-chrome">
      <media-seek-backward-button seekoffset="10"></media-seek-backward-button>
      <media-play-button></media-play-button>
      <media-seek-forward-button seekoffset="10"></media-seek-forward-button>
    </div>
    <media-control-bar>
      <media-play-button></media-play-button>
      <media-seek-backward-button seekoffset="10"></media-seek-backward-button>
      <media-seek-forward-button seekoffset="10"></media-seek-forward-button>
      <media-mute-button></media-mute-button>
      <media-volume-range></media-volume-range>
      <media-time-range></media-time-range>
      <media-time-display showduration></media-time-display>
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

<style scoped>
media-controller {
  container-name: media-chrome;
  container-type: inline-size;
  width: 100%;
  --media-primary-color: var(--primary);
  --media-control-hover-background: var(--accent);
}

.center {
  display: none;
}

@container (inline-size < 420px) {
  .center {
    display: block;
  }
  media-control-bar media-play-button,
  media-control-bar media-seek-backward-button,
  media-control-bar media-seek-forward-button {
    display: none;
  }
}

@container (max-width: 420px) {
  .center {
    display: block;
  }
  media-play-button,
  media-seek-backward-button,
  media-seek-forward-button {
    width: 35px;
    height: 40px;
  }
}

@container (420px <= inline-size <= 590px) {
  .center {
    display: block;
  }
  media-control-bar {
    display: flex;
  }
  media-control-bar media-play-button,
  media-control-bar media-seek-backward-button,
  media-control-bar media-seek-forward-button {
    display: none;
  }
}

@container (min-width: 420px) and (max-width: 590px) {
  .center {
    display: block;
  }
  media-control-bar {
    display: flex;
  }
  media-control-bar media-play-button,
  media-control-bar media-seek-backward-button,
  media-control-bar media-seek-forward-button {
    display: none;
  }
  media-play-button,
  media-seek-backward-button,
  media-seek-forward-button {
    width: 50px;
    height: 40px;
  }
}

@container (inline-size > 590px) {
  .center {
    display: none;
  }
  media-control-bar {
    display: flex;
  }
  media-control-bar media-play-button,
  media-control-bar media-seek-backward-button,
  media-control-bar media-seek-forward-button {
    width: 50px;
  }
}
</style>
