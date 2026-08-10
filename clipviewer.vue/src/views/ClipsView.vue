<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import ClipList from '@/components/ClipList.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Plus } from '@lucide/vue'

const route = useRoute()
const router = useRouter()
const { user, isAuthenticated } = useAuth()

const isBrowsePage = ref(false)
const username = ref('')

const title = computed(() => {
  if (isBrowsePage.value) {
    return 'Browse Clips'
  }

  if (!isAuthenticated.value || user.value.username.toLowerCase() !== username.value?.toLowerCase()) {
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
  <div>
    <div class="container mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold mb-8">
        {{ title }}
      </h1>

      <Card class="mb-6">
        <CardHeader class="flex-row items-center justify-between">
          <CardTitle class="text-xl">Video Library</CardTitle>
          <Button
            v-if="isAuthenticated && user.username.toLowerCase() === username?.toLowerCase()"
            @click="router.push('/upload')"
          >
            <Plus class="size-4" />
            Upload New Clip
          </Button>
        </CardHeader>
        <CardContent>
          <ClipList :username="username" />
        </CardContent>
      </Card>
    </div>
  </div>
</template>
