import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

const gateway = process.env.VITE_DEV_GATEWAY_URL ?? 'https://localhost:7067';
const proxy = { target: gateway, changeOrigin: true, secure: false };

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    // The browser only ever talks to the Gateway; same-origin proxying keeps the BFF session cookie first-party.
    proxy: { '/bff': proxy, '/api': proxy, '/health': proxy, '/signin-oidc': proxy, '/signout-callback-oidc': proxy }
  },
  build: { sourcemap: false, chunkSizeWarningLimit: 600 },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/unit/**/*.test.{ts,tsx}', 'tests/component/**/*.test.{ts,tsx}', 'tests/integration/**/*.test.{ts,tsx}'],
    css: false,
    restoreMocks: true
  }
});
