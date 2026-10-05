import { defineConfig, devices } from "@playwright/test";

/**
 * C7 (TV3) — E2E Playwright, tách riêng khỏi Jest (jest.config.mjs đã bỏ qua /e2e/).
 * Cần: Postgres + API http://localhost:5080 (đã --migrate, có danh mục) và frontend http://localhost:3000.
 * Test tự đăng ký user mới qua POST /auth/register (mật khẩu sinh ngẫu nhiên, không cần tài khoản seed).
 *   npx playwright install chromium     (lần đầu)
 *   npm run e2e                         (đặt E2E_START_SERVER=1 để Playwright tự chạy `npm run dev`;
 *                                        E2E_API_URL nếu API không ở http://localhost:5080/api/v1)
 */
const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:3000";

export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1, // máy yếu + luồng tạo dữ liệu thật -> chạy tuần tự
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: process.env.E2E_START_SERVER
    ? { command: "npm run dev", url: baseURL, reuseExistingServer: true, timeout: 120_000 }
    : undefined,
});
