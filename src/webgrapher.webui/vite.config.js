import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [plugin()],
  server: {
    https: {
      pfx: fs.readFileSync('C:/certs/localhost.pfx'),
      passphrase: '1234'
    },
    port: 52924,
  }
})
