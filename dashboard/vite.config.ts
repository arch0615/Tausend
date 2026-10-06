import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Forwarded server-to-server by Vite, so the browser never makes a
      // cross-origin request and the backend needs no CORS handling in dev.
      // Points at backend-core (ASP.NET Core, `cd backend-core/src/TausendBackend.Api && dotnet run`,
      // default Kestrel port 5000) -- either backend works since routes/JSON match, but backend-core
      // is the actively-developed, fully-ported target; point this at the WCF backend's port instead
      // if you're specifically testing against that one.
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
