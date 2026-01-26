import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { vuetify } from './plugins/vuetify'

createApp(App)
  .use(router)      // ← si router es undefined, no se ve nada
  .use(vuetify)
  .mount('#app')
