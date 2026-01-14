import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
    },
    {
      path: '/browse',
      name: 'browse',
      component: () => import('../views/ClipsView.vue'),
    },
    {
      path: '/users/:username',
      name: 'user-clips',
      component: () => import('../views/ClipsView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
    },
    {
      path: '/clips/:videoId',
      name: 'video',
      component: () => import('../views/VideoView.vue'),
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  const { checkAuth, isAuthenticated } = useAuth()

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
