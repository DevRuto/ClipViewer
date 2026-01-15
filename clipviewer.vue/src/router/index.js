import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
      meta: { title: 'Home - ClipViewer' },
    },
    {
      path: '/browse',
      name: 'browse',
      component: () => import('../views/ClipsView.vue'),
      meta: { title: 'Browse Clips - ClipViewer' },
    },
    {
      path: '/users/:username',
      name: 'user-clips',
      component: () => import('../views/ClipsView.vue'),
      meta: { requiresAuth: true, title: 'Your Clips - ClipViewer' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { title: 'Sign In - ClipViewer' },
    },
    {
      path: '/clips/:videoId',
      name: 'video',
      component: () => import('../views/VideoView.vue'),
      meta: { title: 'Video - ClipViewer' },
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  const { checkAuth, isAuthenticated } = useAuth()

  // Update document title
  if (to.meta.title) {
    document.title = to.meta.title
  } else {
    document.title = 'ClipViewer'
  }

  if (to.meta.requiresAuth) {
    if (!isAuthenticated.value) {
      const isValid = await checkAuth()
      if (!isValid) {
        next('/login')
        return
      }
    }
  }

  if (to.name === 'login' && isAuthenticated.value) {
    next('/clips')
    return
  }

  next()
})

export default router
