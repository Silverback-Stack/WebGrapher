import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: App
  },
  {
    path: '/Graph/:id',
    name: 'Graph',
    component: App
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
