<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { useTagColor } from '@/composables/useTagColor.js'
import { api } from '@/services/api'
import ClipList from '@/components/ClipList.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Plus, Tag as TagIcon, X } from '@lucide/vue'

const route = useRoute()
const router = useRouter()
const { user, isAuthenticated } = useAuth()
const { tagColorClasses, tagColorSolidClasses } = useTagColor()

const isBrowsePage = ref(false)
const username = ref('')
const tag = ref('')
const availableTags = ref([])

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
  tag.value = route.query.tag || ''
}

async function fetchAvailableTags() {
  try {
    const res = await api.get('/api/videos/tags')
    availableTags.value = res.data
  } catch (err) {
    console.error('Failed to fetch tags:', err)
  }
}

function selectTag(t) {
  router.push({ query: { ...route.query, tag: t } })
}

function clearTag() {
  const query = { ...route.query }
  delete query.tag
  router.push({ query })
}

onMounted(() => {
  init()
  fetchAvailableTags()
})

watch(
  () => [route.params.username, route.query.tag],
  () => {
    init()
  },
)
</script>

<template>
  <div>
    <div class="container mx-auto px-4 py-8">
      <div class="flex items-center justify-between mb-8 gap-4">
        <h1 class="text-3xl font-bold">
          {{ title }}
        </h1>
        <Button
          v-if="isAuthenticated && user.username.toLowerCase() === username?.toLowerCase()"
          @click="router.push('/upload')"
        >
          <Plus class="size-4" />
          Upload New Clip
        </Button>
      </div>

      <Card class="mb-6">
        <CardHeader>
          <CardTitle class="text-xl">Video Library</CardTitle>
        </CardHeader>
        <CardContent>
          <div v-if="availableTags.length || tag" class="mb-4 flex flex-wrap items-center gap-2">
            <span
              v-if="tag"
              :class="tagColorSolidClasses(tag)"
              class="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium"
            >
              <TagIcon class="size-3" />
              {{ tag }}
              <button type="button" class="hover:opacity-75" @click="clearTag">
                <X class="size-3" />
              </button>
            </span>
            <button
              v-for="t in availableTags.filter((t) => t !== tag)"
              :key="t"
              type="button"
              :class="tagColorClasses(t)"
              class="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium hover:opacity-75 transition-opacity"
              @click="selectTag(t)"
            >
              <TagIcon class="size-3" />
              {{ t }}
            </button>
          </div>
          <ClipList :username="username" :tag="tag" />
        </CardContent>
      </Card>
    </div>
  </div>
</template>
