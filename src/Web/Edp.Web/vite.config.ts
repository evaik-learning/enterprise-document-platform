import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/bff': {
        target: 'https://localhost:7067',
        changeOrigin: true,
        secure: false
      },
      '/api': {
        target: 'https://localhost:7067',
        changeOrigin: true,
        secure: false
      },
      '/health': {
        target: 'https://localhost:7067',
        changeOrigin: true,
        secure: false
      }
    }
  }
});
