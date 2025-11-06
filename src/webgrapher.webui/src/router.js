import { createRouter, createWebHistory } from 'vue-router'
import LoginPage from './LoginPage.vue'
import LogoutPage from './LogoutPage.vue'
import LoginCallback from './LoginCallback.vue'
import GraphPage from './GraphPage.vue'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: LoginPage
  },
  {
    path: '/callback',
    name: 'Callback',
    component: LoginCallback
  },
  {
    path: '/logout',
    name: 'Logout',
    component: LogoutPage
  },
  {
    path: '/Graph/:id?',
    name: 'Graph',
    component: GraphPage
  },
  {
    path: '/',
    name: 'Home',
    component: GraphPage
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
