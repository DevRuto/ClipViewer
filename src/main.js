import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import HomeView from './views/HomeView.vue'
import ClipView from './views/ClipView.vue'
import './main.css'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/v/:videoId', component: ClipView },
  ],
})

createApp(App).use(router).mount('#app')
