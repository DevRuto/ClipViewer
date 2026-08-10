<script setup>
import { ref, onMounted } from 'vue'
import { Sun, Moon } from '@lucide/vue'
import { Button } from '@/components/ui/button'

const isDark = ref(false)

const toggleTheme = () => {
  isDark.value = !isDark.value
  updateTheme()
}

const updateTheme = () => {
  if (isDark.value) {
    document.documentElement.classList.add('dark')
    localStorage.setItem('theme', 'dark')
  } else {
    document.documentElement.classList.remove('dark')
    localStorage.setItem('theme', 'light')
  }
}

onMounted(() => {
  // Check for saved theme preference or default to light mode
  const savedTheme = localStorage.getItem('theme')
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches

  isDark.value = savedTheme === 'dark' || (!savedTheme && prefersDark)
  updateTheme()
})
</script>
<template>
  <Button
    variant="ghost"
    size="icon"
    :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'"
    @click="toggleTheme"
  >
    <Moon v-if="isDark" class="size-5 text-yellow-500" />
    <Sun v-else class="size-5" />
  </Button>
</template>
