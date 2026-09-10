import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../SecureFix.Api/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "https://localhost:7127",
        changeOrigin: true,
        secure: false,
      },
      "/health": {
        target: "https://localhost:7127",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    css: true,
    env: {
      VITE_DATA_MODE: "demo",
      VITE_AUTH_MODE: "demo",
    },
  },
});
