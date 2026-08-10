<script setup>
import { RouterLink, RouterView } from 'vue-router'
import { Menu, LogOut, UserRound } from '@lucide/vue'
import ThemeSwitcher from '@/components/ThemeSwitcher.vue'
import ThemeCustomizer from '@/components/ThemeCustomizer.vue'
import GithubIcon from '@/components/icons/GithubIcon.vue'
import { useAuth } from '@/composables/useAuth'
import { onMounted, ref } from 'vue'
import { Button, buttonVariants } from '@/components/ui/button'
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
import { cn } from '@/lib/utils'

const { user, isAuthenticated, logout, checkAuth } = useAuth()
const isMenuOpen = ref(false)

onMounted(() => {
  checkAuth()
})

const navLinkClass = cn(buttonVariants({ variant: 'ghost' }), 'justify-start md:justify-center')
</script>

<template>
  <div class="bg-background min-h-screen flex flex-col">
    <nav class="border-b sticky top-0 z-40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div class="container mx-auto px-4 h-16 flex items-center justify-between">
        <RouterLink to="/" class="text-xl font-bold shrink-0">ClipViewer</RouterLink>

        <!-- Desktop Navigation -->
        <div class="hidden md:flex items-center gap-1">
          <RouterLink
            to="/browse"
            exact
            :class="navLinkClass"
            active-class="text-primary bg-accent"
          >
            Clips
          </RouterLink>
          <RouterLink
            v-if="isAuthenticated"
            :to="`/users/${user.username}`"
            :class="navLinkClass"
            active-class="text-primary bg-accent"
          >
            Your Clips
          </RouterLink>
          <RouterLink
            v-if="!isAuthenticated"
            to="/login"
            :class="navLinkClass"
            active-class="text-primary bg-accent"
          >
            Sign In
          </RouterLink>

          <DropdownMenu v-if="isAuthenticated">
            <DropdownMenuTrigger as-child>
              <Button variant="ghost" class="gap-2">
                <UserRound class="size-4" />
                {{ user?.username || 'User' }}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>{{ user?.username }}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" @click="logout">
                <LogOut class="size-4" />
                Sign Out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <Separator orientation="vertical" class="h-6 mx-1" />

          <ThemeCustomizer />
          <ThemeSwitcher />
        </div>

        <!-- Mobile menu trigger -->
        <div class="md:hidden flex items-center gap-1">
          <ThemeCustomizer />
          <ThemeSwitcher />
          <Sheet v-model:open="isMenuOpen">
            <SheetTrigger as-child>
              <Button variant="ghost" size="icon" aria-label="Toggle menu">
                <Menu class="size-6" />
              </Button>
            </SheetTrigger>
            <SheetContent side="right" class="w-72">
              <SheetHeader>
                <SheetTitle>ClipViewer</SheetTitle>
              </SheetHeader>
              <div class="flex flex-col gap-1 px-4">
                <SheetClose as-child>
                  <RouterLink to="/browse" exact :class="navLinkClass" active-class="text-primary bg-accent">
                    Clips
                  </RouterLink>
                </SheetClose>
                <SheetClose v-if="isAuthenticated" as-child>
                  <RouterLink
                    :to="`/users/${user.username}`"
                    :class="navLinkClass"
                    active-class="text-primary bg-accent"
                  >
                    Your Clips
                  </RouterLink>
                </SheetClose>
                <SheetClose v-if="!isAuthenticated" as-child>
                  <RouterLink to="/login" :class="navLinkClass" active-class="text-primary bg-accent">
                    Sign In
                  </RouterLink>
                </SheetClose>

                <template v-if="isAuthenticated">
                  <Separator class="my-2" />
                  <p class="px-4 py-1 text-sm text-muted-foreground">{{ user?.username }}</p>
                  <SheetClose as-child>
                    <Button variant="ghost" class="justify-start text-destructive" @click="logout">
                      <LogOut class="size-4" />
                      Sign Out
                    </Button>
                  </SheetClose>
                </template>
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </div>
    </nav>
    <RouterView class="flex-1" />

    <footer class="border-t">
      <div class="container mx-auto px-4 py-6 flex flex-col sm:flex-row items-center justify-between gap-2 text-sm text-muted-foreground">
        <p>&copy; {{ new Date().getFullYear() }} ClipViewer. Created by Ruto.</p>
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
