import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/

export default defineConfig({
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      // 登录/注册 + 收藏同步 → 桥接层(server/)
      '/api/auth': {
        target: 'http://localhost:3939',
        changeOrigin: true
      },
      '/api/favorites': {
        target: 'http://localhost:3939',
        changeOrigin: true
      },
      // 其余接口 → 原有桌面后端
      '/api': {
        target: 'http://localhost:13521',
        changeOrigin: true
      },
      // 图片等静态资源（后端返回的相对路径，如 /image/...）
      '/image': {
        target: 'http://localhost:13521',
        changeOrigin: true
      }
    }
  },
  plugins: [
    vue(),
  ],
})
