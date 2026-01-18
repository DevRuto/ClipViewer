<script setup>
import { RouterLink, RouterView } from 'vue-router'
import ThemeSwitcher from '@/components/ThemeSwitcher.vue'
import { useAuth } from '@/composables/useAuth'
import { onMounted, ref } from 'vue'

const { user, isAuthenticated, logout, checkAuth } = useAuth()
const isMenuOpen = ref(false)

onMounted(() => {
  checkAuth()
})

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value
}

function closeMenu() {
  isMenuOpen.value = false
}
</script>

<template>
  <div class="bg-gray-200 dark:bg-gray-900 min-h-screen flex flex-col">
    <nav class="bg-gray-200 dark:bg-gray-900 shadow-md p-4 h-16 relative z-40">
      <div class="container mx-auto">
        <div class="flex justify-between items-center">
          <div class="flex items-center">
            <RouterLink to="/" class="text-xl font-bold text-gray-800 dark:text-white"
              >ClipViewer</RouterLink
            >
          </div>

          <!-- Desktop Navigation -->
          <div class="hidden md:flex items-center space-x-8">
            <RouterLink
              to="/browse"
              exact
              class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
            >
              Clips
            </RouterLink>
            <RouterLink
              v-if="isAuthenticated"
              :to="`/users/${user.username}`"
              class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
            >
              Your Clips
            </RouterLink>
            <RouterLink
              v-if="!isAuthenticated"
              to="/login"
              class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
            >
              Sign In
            </RouterLink>
            <div v-if="isAuthenticated" class="flex items-center space-x-4">
              <span class="text-gray-600 dark:text-gray-300 text-sm">
                {{ user?.username || 'User' }}
              </span>
              <button
                @click="logout"
                class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                Sign Out
              </button>
            </div>
            <div class="flex items-center space-x-2">
              <a
                href="https://github.com/DevRuto/ClipViewer"
                target="_blank"
                rel="noopener noreferrer"
                class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
                title="View on GitHub"
              >
                <svg
                  class="w-5 h-5 text-gray-700 dark:text-gray-300"
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
                  />
                </svg>
              </a>
              <ThemeSwitcher />
            </div>
          </div>

          <!-- Mobile menu button -->
          <div class="md:hidden flex items-center space-x-2">
            <a
              href="https://github.com/DevRuto/ClipViewer"
              target="_blank"
              rel="noopener noreferrer"
              class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
              title="View on GitHub"
            >
              <svg
                class="w-5 h-5 text-gray-700 dark:text-gray-300"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"
                />
              </svg>
            </a>
            <ThemeSwitcher />
            <button
              @click="toggleMenu"
              class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
              aria-label="Toggle menu"
            >
              <svg
                v-if="!isMenuOpen"
                class="w-6 h-6 text-gray-700 dark:text-gray-300"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M4 6h16M4 12h16M4 18h16"
                />
              </svg>
              <svg
                v-else
                class="w-6 h-6 text-gray-700 dark:text-gray-300"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>
        </div>

        <!-- Mobile menu -->
        <transition
          enter-active-class="transition duration-300 ease-out"
          enter-from-class="transform -translate-y-2 opacity-0"
          enter-to-class="transform translate-y-0 opacity-100"
          leave-active-class="transition duration-200 ease-in"
          leave-from-class="transform translate-y-0 opacity-100"
          leave-to-class="transform -translate-y-2 opacity-0"
        >
          <div
            v-if="isMenuOpen"
            class="md:hidden absolute left-0 right-0 mt-4 pt-4 border-t border-gray-300 dark:border-gray-700 relative z-50 bg-gray-200 dark:bg-gray-900 shadow-lg"
          >
            <div class="flex flex-col space-y-2">
              <RouterLink
                to="/browse"
                @click="closeMenu"
                exact
                class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
                active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
              >
                Clips
              </RouterLink>
              <RouterLink
                v-if="isAuthenticated"
                :to="`/users/${user.username}`"
                @click="closeMenu"
                class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
                active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
              >
                Your Clips
              </RouterLink>
              <RouterLink
                v-if="!isAuthenticated"
                to="/login"
                @click="closeMenu"
                class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
                active-class="text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20"
              >
                Sign In
              </RouterLink>
              <div v-if="isAuthenticated" class="flex flex-col space-y-2 px-3 py-2">
                <span class="text-gray-600 dark:text-gray-300 text-sm">
                  {{ user?.username || 'User' }}
                </span>
                <button
                  @click="
                    () => {
                      logout()
                      closeMenu()
                    }
                  "
                  class="text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-2 rounded-md text-sm font-medium transition-colors text-left"
                >
                  Sign Out
                </button>
              </div>
            </div>
          </div>
        </transition>
      </div>
    </nav>
    <RouterView class="flex-1" />
  </div>
</template>

<style scoped></style>
