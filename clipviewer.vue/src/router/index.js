import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
    },
    {
      path: '/clips',
      name: 'clips',
      component: () => import('../views/ClipsView.vue'),
    },
    {
      path: '/clips/:videoId',
      name: 'video',
      component: () => import('../views/VideoView.vue'),
    },
  ],
})

export default router
