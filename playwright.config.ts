import { defineConfig, devices } from "@playwright/test";
import { config } from "dotenv";

// Nạp biến môi trường E2E từ .env.e2e.local (nếu có). File này nằm trong .gitignore.
config({ path: ".env.e2e.local" });

/**
 * Cấu hình Playwright E2E cho Perfo.
 * Không dùng webServer: cần bật sẵn backend + frontend (npm run dev) trước khi chạy.
 * Đổi địa chỉ frontend bằng biến môi trường E2E_BASE_URL.
 */
export default defineConfig({
  testDir: "e2e",
  // workers=1 để tránh xung đột cổng mock gateway (9099) và race condition DB.
  workers: 1,
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
