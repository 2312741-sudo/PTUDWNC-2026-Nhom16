// N2-A1 — hạ tầng test E2E thật (Playwright).
//
// Vì sao cần: lỗi 500 toàn bộ trang `/search` từng lọt vào `main` vì không có bất kỳ test E2E nào
// chạm route dynamic đó. `npx playwright test --list` phải ra danh sách luồng thì cổng này mới
// được coi là "thật" (bằng chứng A1).
//
// Chạy:
//   npm run test:e2e -- --list        # liệt kê luồng, không cần server
//   npm run test:e2e                  # chạy thật (tự bật frontend + backend)
//
// Backend: mặc định http://localhost:5080. Muốn trỏ chỗ khác thì đặt E2E_API_URL.
// Backend CẦN Postgres + Redis + S3 đã lên. Trong CI những dịch vụ đó do workflow bật sẵn và CI
// tự bật backend (nên `E2E_START_BACKEND=0`); ngoài local thì Playwright tự bật — xem `webServer`.
//
// Về `E2E_START_SERVER` (biến của C7/TV3 trong `playwright.config.ts` cũ): không còn dùng.
// Config này **luôn** tự bật frontend trừ khi đã đặt `E2E_BASE_URL` (tức là server bên ngoài),
// nên đặt `E2E_START_SERVER=1` chỉ là thừa. Các spec của C7 dùng `E2E_API_URL`/`E2E_BASE_URL` —
// hai biến này vẫn được giữ nguyên.
import { defineConfig, devices } from '@playwright/test'

const PORT = Number(process.env.E2E_PORT ?? 3000)
const BASE_URL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${PORT}`

/** API URL mà e2e/api.ts gọi — phải trùng cổng backend bật ở `webServer` bên dưới. */
const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5080'

/**
 * Có tự bật backend hay không.
 *
 * Vì sao mặc định **có**: trước đây backend phải được khởi động thủ công từ terminal khác. Nếu tiến
 * trình đó chết giữa lúc test đang chạy (ngắt session, đóng terminal, máy khởi động lại service),
 * các test còn lại nhận `ECONNREFUSED` và báo đỏ — **lỗi hạ tầng bị quy nhầm thành lỗi sản phẩm**.
 * Thực tế đã gặp: một lần chạy mất 14/26 test chỉ vì API bị dọn giữa chừng. Khi `webServer` tự quản
 * lý backend, vòng đời của nó gắn với đúng lần chạy test nên không còn khe hở này.
 *
 * Đặt `E2E_START_BACKEND=0` để tự quản lý (CI làm vậy vì đã có sẵn Postgres/Redis/S3).
 */
const START_BACKEND = process.env.E2E_START_BACKEND !== '0'

export default defineConfig({
  testDir: './e2e',
  // Route `/search` gọi API thật nên cần chờ network — timeout mặc định 30 s hơi chật khi CI yếu.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  // 2 API instance không liên quan; chạy tuần tự để log đọc được và tránh tranh DB.
  workers: 1,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list']],

  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    locale: 'vi-VN',
    timezoneId: 'Asia/Ho_Chi_Minh',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  webServer: [
    ...(process.env.E2E_BASE_URL
      ? []
      : [
          {
            // `next dev` khởi động nhanh hơn `next build && next start` và không cần build trước.
            // Route /search là dynamic nên không bị static prerender giả.
            command: `npm run dev -- --port ${PORT}`,
            url: BASE_URL,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
            stdout: 'pipe' as const,
            stderr: 'pipe' as const,
          },
        ]),
    ...(!START_BACKEND || process.env.CI
      ? []
      : [
          {
            command:
              'dotnet run --project ../../src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj ' +
              '--no-launch-profile -- --urls ' +
              API_URL,
            // Chờ **`/health/ready`** chứ không phải cổng: cổng mở sớm hơn lúc DB/Hangfire sẵn sàng,
            // và `ready` trả 503 khi DB chết. Chờ ở đây giúp lỗi "DB chưa lên" hiện ngay từ đầu
            // thay vì rơi thành hàng loạt test đỏ với `ECONNREFUSED` khó đọc.
            url: `${API_URL}/health/ready`,
            reuseExistingServer: !process.env.CI,
            // Lần `dotnet run` đầu tiên phải build, mà build sạch mất lâu hơn 60 s mặc định.
            timeout: 300_000,
            stdout: 'pipe' as const,
            stderr: 'pipe' as const,
          },
        ]),
  ],
})
