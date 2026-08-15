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
      meta: { title: 'Clips - ClipViewer' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { title: 'Sign In - ClipViewer' },
    },
    {
      path: '/upload',
      name: 'upload',
      component: () => import('../views/UploadView.vue'),
      meta: { requiresAuth: true, title: 'Upload - ClipViewer' },
    },
    {
      path: '/clips/:videoId',
      name: 'video',
      component: () => import('../views/VideoView.vue'),
      meta: { title: 'Video - ClipViewer' },
    },
    {
      path: '/stats',
      name: 'stats',
      component: () => import('../views/StatsView.vue'),
      meta: { requiresAuth: true, title: 'Your Stats - ClipViewer' },
    },
    {
      path: '/admin/users',
      name: 'admin-users',
      component: () => import('../views/UsersView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true, title: 'Manage Users - ClipViewer' },
    },
  ],
})

router.beforeEach(async (to, from, next) => {
  const { checkAuth, isAuthenticated, isAdmin, user } = useAuth()

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

    if (to.meta.requiresAdmin && !isAdmin.value) {
      next('/')
      return
    }
  }

  if (to.name === 'login' && isAuthenticated.value) {
    if (user.value?.username) {
      next({ name: 'user-clips', params: { username: user.value.username } })
    } else {
      next({ name: 'browse' })
    }
    return
  }

  next()
})

router.onError((error, to) => {
  if (error.message.includes('error loading dynamically imported module')) {
    window.location = to.fullPath
  }
})

export default router
