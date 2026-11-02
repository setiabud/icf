import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'
import { readFileSync } from 'node:fs'
import { homedir } from 'node:os'
import { resolve } from 'node:path'
import { Agent } from 'node:https'

// https://vite.dev/config/
export default defineConfig(({ command, mode, isPreview }) => {
  const https = command === 'serve' && mode !== 'test' && !isPreview ? {
    cert: readFileSync(resolve(homedir(), '.aspnet/https/icfaccounting-web.pem')),
    key: readFileSync(resolve(homedir(), '.aspnet/https/icfaccounting-web.key')),
  } : undefined
  return {
    plugins: [react()],
    server: {
      host: 'localhost',
      port: 5173,
      cors: true,
      open: true,
      strictPort: true,
      https,
      proxy: {
        '/api': {
          target: 'https://localhost:7206',
          agent: new Agent({ ca: https?.cert }),
          rewrite: (path) => path.replace(/^\/api/, ''),
        },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      env: {
        VITE_API_BASE_URL: 'https://api.test',
      },
    },
  }
})
