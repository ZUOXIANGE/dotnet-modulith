import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers'
import { resolve } from 'path'

function resolveApiTarget(): string {
  const aspireServiceDiscoveryTarget =
    process.env.services__api__http__0 ||
    process.env.SERVICES__API__HTTP__0 ||
    process.env.services__api__https__0 ||
    process.env.SERVICES__API__HTTPS__0
  const aspireTarget = process.env.API_HTTP || process.env.API_HTTPS
  const configuredTarget = process.env.VITE_API_TARGET
  const fallbackTarget = 'http://localhost:12580'

  return (aspireServiceDiscoveryTarget || aspireTarget || configuredTarget || fallbackTarget)
    .replace(/\/$/, '')
}

const apiProxyTarget = resolveApiTarget()

export default defineConfig({
  plugins: [
    vue(),
    Components({
      resolvers: [NaiveUiResolver()]
    })
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    port: parseInt(process.env.PORT || '5173'),
    proxy: {
      '/api': {
        target: apiProxyTarget,
        changeOrigin: true,
        secure: false
      }
    }
  }
})
