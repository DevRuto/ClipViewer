<script setup>
import { RouterLink, RouterView } from 'vue-router'
import ThemeSwitcher from './components/ThemeSwitcher.vue'
</script>

<template>
  <div class="bg-gray-200 dark:bg-gray-900">
    <nav class="bg-gray-200 dark:bg-gray-900 shadow-md">
      <div class="container mx-auto px-4">
        <div class="flex justify-between items-center h-16">
          <div class="flex items-center">
            <RouterLink to="/" class="text-xl font-bold text-gray-800 dark:text-white"
              >ClipViewer</RouterLink
            >
          </div>
          <div class="flex items-center space-x-8">
            <RouterLink
              to="/"
              class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
            >
              Home
            </RouterLink>
            <RouterLink
              to="/clips"
              class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
            >
              Clips
            </RouterLink>
            <ThemeSwitcher />
          </div>
        </div>
      </div>
    </nav>
    <RouterView />
  </div>
</template>

<style scoped></style>
