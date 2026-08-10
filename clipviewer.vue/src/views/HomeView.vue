<script setup>
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { Button } from '@/components/ui/button'
import { PlayCircle, UploadCloud, LogIn } from '@lucide/vue'

const { isAuthenticated } = useAuth()
</script>

<template>
  <div class="flex flex-col justify-center bg-gradient-to-br from-primary/10 via-background to-background">
    <div class="container mx-auto px-4 py-24">
      <div class="text-center">
        <h1 class="text-5xl font-bold tracking-tight mb-4">ClipViewer</h1>
        <p class="text-xl text-muted-foreground mb-2">Your personal video viewing platform</p>
        <p class="text-sm text-muted-foreground mb-8">Created by Ruto</p>

        <div class="flex flex-wrap items-center justify-center gap-3">
          <Button size="lg" as-child>
            <RouterLink to="/browse">
              <PlayCircle class="size-4" />
              Browse Clips
            </RouterLink>
          </Button>

          <Button v-if="isAuthenticated" size="lg" variant="outline" as-child>
            <RouterLink to="/upload">
              <UploadCloud class="size-4" />
              Upload a Clip
            </RouterLink>
          </Button>
          <Button v-else size="lg" variant="outline" as-child>
            <RouterLink to="/login">
              <LogIn class="size-4" />
              Sign In
            </RouterLink>
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
