import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import 'media-chrome'
import 'hls-video-element'
import './main.css'

const app = createApp(App)

app.use(router)

app.mount('#app')
