/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    // En développement, l'API tourne sur http://localhost:5215 (dotnet run) : le proxy évite
    // toute configuration CORS. En conteneur, nginx joue le même rôle.
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5215',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
