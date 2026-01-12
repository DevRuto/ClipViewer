<script setup>
import ClipList from '../components/ClipList.vue'
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="container mx-auto px-4 py-8">
      <h1 class="text-3xl font-bold text-gray-800 dark:text-white mb-8">Your Clips</h1>

      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-xl font-semibold text-gray-700 dark:text-gray-200">Video Library</h2>
          <button
            class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-lg transition duration-200"
          >
            Upload New Clip
          </button>
        </div>

        <ClipList />
      </div>
    </div>
  </div>
</template>
