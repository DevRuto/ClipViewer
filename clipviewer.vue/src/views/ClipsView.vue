<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import ClipList from '@/components/ClipList.vue'

const route = useRoute()
const { user, isAuthenticated } = useAuth()

const isBrowsePage = ref(false)
const username = ref('')

const title = computed(() => {
  if (isBrowsePage.value) {
    return 'Browse Clips'
  }

  if (!isAuthenticated.value || user.value.username !== username.value) {
    return `${username.value.toLocaleLowerCase()}'s Clips`
  }

  return 'Your Clips'
})

function init() {
  username.value = route.params.username
  isBrowsePage.value = route.name === 'browse'
}

onMounted(() => {
  init()
})

watch(
  () => route.params.username,
  () => {
    init()
  },
)
</script>

<template>
  <div class="min-h-screen">
    <div class="container mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-800 dark:text-white mb-8">
        {{ title }}
      </h1>

      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-xl font-semibold text-gray-700 dark:text-gray-200">Video Library</h2>
          <button
            v-if="isAuthenticated && user.username === username?.toLowerCase()"
            class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-lg transition duration-200"
          >
            Upload New Clip
          </button>
        </div>

        <ClipList :username="username" />
      </div>
    </div>
  </div>
</template>
