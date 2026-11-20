import { defineConfig, devices } from "@playwright/test";

/**
 * Cấu hình Playwright E2E cho Perfo.
 * Không dùng webServer: cần bật sẵn backend + frontend (npm run dev) trước khi chạy.
 * Đổi địa chỉ frontend bằng biến môi trường E2E_BASE_URL.
 */
export default defineConfig({
  testDir: "e2e",
  use: {
    baseURL: process.env.E2E_BASE_URL || "http://localhost:5173",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
