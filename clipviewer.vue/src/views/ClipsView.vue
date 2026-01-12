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

        <div class="text-center py-12">
          <svg
            class="w-16 h-16 text-gray-400 dark:text-gray-500 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"
            ></path>
          </svg>
          <p class="text-gray-500 dark:text-gray-400 text-lg mb-2">No clips uploaded yet</p>
          <p class="text-gray-400 dark:text-gray-500">Upload your first video to get started</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
// Component logic will go here
</script>
