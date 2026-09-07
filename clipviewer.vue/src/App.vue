<script setup>
import { RouterLink, RouterView } from 'vue-router'
import { Menu, LogOut, Users, ChartColumn, Plus } from '@lucide/vue'
import ThemeSwitcher from '@/components/ThemeSwitcher.vue'
import ThemeCustomizer from '@/components/ThemeCustomizer.vue'
import GithubIcon from '@/components/icons/GithubIcon.vue'
import { useAuth } from '@/composables/useAuth'
import { computed, onMounted, ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger, SheetClose } from '@/components/ui/sheet'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

const { user, isAuthenticated, isAdmin, logout, checkAuth } = useAuth()
const isMenuOpen = ref(false)

const initials = computed(() => user.value?.username?.slice(0, 1).toUpperCase() || '?')

onMounted(() => {
  checkAuth()
})
</script>

<template>
  <div class="bg-background min-h-screen flex flex-col">
    <nav class="border-b bg-background">
      <div class="container mx-auto px-4 h-14 flex items-center justify-between gap-4">
        <div class="flex items-center gap-8 min-w-0">
          <RouterLink to="/" class="flex items-center gap-2 shrink-0">
            <svg class="size-3 text-primary" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round">
              <path d="M4 3v8M4 3h8" />
            </svg>
            <span class="font-semibold tracking-tight">ClipViewer</span>
          </RouterLink>

          <!-- Desktop Navigation -->
          <div class="hidden md:flex items-center gap-5">
            <RouterLink to="/browse" exact class="nav-link">Clips</RouterLink>
            <RouterLink v-if="isAuthenticated" :to="`/users/${user.username}`" class="nav-link">Your Clips</RouterLink>
          </div>
        </div>

        <div class="hidden md:flex items-center gap-3 shrink-0">
          <RouterLink v-if="!isAuthenticated" to="/login" class="text-sm text-muted-foreground hover:text-foreground">
            Sign In
          </RouterLink>

          <Button v-if="isAuthenticated" as-child class="rounded-full gap-1.5">
            <RouterLink to="/upload">
              <Plus class="size-4" />
              New Clip
            </RouterLink>
          </Button>

          <DropdownMenu v-if="isAuthenticated">
            <DropdownMenuTrigger as-child>
              <Button
                variant="ghost"
                size="icon"
                class="rounded-full bg-primary/15 text-sm font-semibold text-primary hover:bg-primary/25 hover:text-primary"
              >
                {{ initials }}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>{{ user?.username }}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem as-child>
                <RouterLink to="/stats">
                  <ChartColumn class="size-4" />
                  Your Stats
                </RouterLink>
              </DropdownMenuItem>
              <DropdownMenuItem v-if="isAdmin" as-child>
                <RouterLink to="/admin/users">
                  <Users class="size-4" />
                  Manage Users
                </RouterLink>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" @click="logout">
                <LogOut class="size-4" />
                Sign Out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <ThemeCustomizer />
          <ThemeSwitcher />
        </div>

        <!-- Mobile menu trigger -->
        <Sheet v-model:open="isMenuOpen" class="md:hidden">
          <SheetTrigger as-child class="md:hidden">
            <Button variant="ghost" size="icon" aria-label="Toggle menu">
              <Menu class="size-5" />
            </Button>
          </SheetTrigger>
          <SheetContent side="right" class="w-72">
            <SheetHeader>
              <SheetTitle>ClipViewer</SheetTitle>
            </SheetHeader>
            <div class="flex flex-col gap-1 px-4">
              <SheetClose as-child>
                <RouterLink to="/browse" exact class="rounded-md px-3 py-2 text-sm font-medium hover:bg-accent" active-class="text-primary bg-accent">
                  Clips
                </RouterLink>
              </SheetClose>
              <SheetClose v-if="isAuthenticated" as-child>
                <RouterLink
                  :to="`/users/${user.username}`"
                  class="rounded-md px-3 py-2 text-sm font-medium hover:bg-accent"
                  active-class="text-primary bg-accent"
                >
                  Your Clips
                </RouterLink>
              </SheetClose>
              <SheetClose v-if="!isAuthenticated" as-child>
                <RouterLink to="/login" class="rounded-md px-3 py-2 text-sm font-medium hover:bg-accent" active-class="text-primary bg-accent">
                  Sign In
                </RouterLink>
              </SheetClose>

              <template v-if="isAuthenticated">
                <SheetClose as-child>
                  <RouterLink to="/upload" class="mt-1 inline-flex w-fit items-center gap-1.5 rounded-full bg-primary px-4 py-2 text-sm font-medium text-primary-foreground">
                    <Plus class="size-4" />
                    New Clip
                  </RouterLink>
                </SheetClose>
                <Separator class="my-2" />
                <p class="px-3 py-1 text-sm text-muted-foreground">{{ user?.username }}</p>
                <SheetClose as-child>
                  <RouterLink to="/stats" class="rounded-md px-3 py-2 text-sm font-medium hover:bg-accent" active-class="text-primary bg-accent">
                    Your Stats
                  </RouterLink>
                </SheetClose>
                <SheetClose v-if="isAdmin" as-child>
                  <RouterLink to="/admin/users" class="rounded-md px-3 py-2 text-sm font-medium hover:bg-accent" active-class="text-primary bg-accent">
                    Manage Users
                  </RouterLink>
                </SheetClose>
                <SheetClose as-child>
                  <Button variant="ghost" class="justify-start text-destructive" @click="logout">
                    <LogOut class="size-4" />
                    Sign Out
                  </Button>
                </SheetClose>
              </template>

              <Separator class="my-2" />
              <div class="flex items-center justify-between px-3 py-1">
                <span class="text-sm text-muted-foreground">Theme</span>
                <div class="flex items-center gap-1">
                  <ThemeCustomizer />
                  <ThemeSwitcher />
                </div>
              </div>
            </div>
          </SheetContent>
        </Sheet>
      </div>
    </nav>
    <RouterView class="flex-1" />

    <footer class="border-t">
      <div class="container mx-auto px-4 py-6 flex flex-col sm:flex-row items-center justify-between gap-2 text-sm text-muted-foreground">
        <p>Made by Ruto</p>
        <a
          href="https://github.com/DevRuto/ClipViewer"
          target="_blank"
          rel="noopener noreferrer"
          class="inline-flex items-center gap-1.5 hover:text-foreground transition-colors"
        >
          <GithubIcon class="size-4" />
          GitHub
        </a>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.nav-link {
  padding-bottom: 2px;
  border-bottom: 2px solid transparent;
  font-size: 0.875rem;
  color: var(--muted-foreground);
  transition:
    border-color 0.15s ease,
    color 0.15s ease;
}
.nav-link:hover {
  border-color: color-mix(in oklab, var(--foreground) 40%, transparent);
  color: var(--foreground);
}
.nav-link.router-link-exact-active {
  border-color: var(--primary);
  color: var(--foreground);
}
</style>
