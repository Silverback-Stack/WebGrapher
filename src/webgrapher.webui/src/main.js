import './assets/main.css'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import Buefy from 'buefy'
import 'buefy/dist/buefy.css'
import '@mdi/font/css/materialdesignicons.css'
import 'bulmaswatch/flatly/bulmaswatch.min.css'

const app = createApp(App)
app.use(router)
app.use(Buefy, {
  // optional configuration
})
app.mount('#app')
