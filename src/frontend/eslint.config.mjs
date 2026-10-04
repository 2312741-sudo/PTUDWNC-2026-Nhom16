// N2-A2 — cổng CI chặn lỗi frontend.
//
// Bối cảnh: lỗi 500 toàn bộ trang /search từng lọt vào `main` vì không có cổng CI nào lint/build
// frontend. Workflow `frontend.yml` giờ chạy `tsc --noEmit` → `lint` → `next build`; file này là
// phần cấu hình mà `npm run lint` cần.
//
// `eslint-config-next@15` xuất theo kiểu eslintrc (chưa có `exports` flat), nên phải bọc qua
// FlatCompat. Cấu hình chỉ bật những rule đã bật sẵn trong preset `next/core-web-vitals` — không
// thêm rule mới — để cổng CI bắt lỗi thật mà không biến thành blocker vì vặt style.
import { FlatCompat } from '@eslint/eslintrc'
import tsPlugin from '@typescript-eslint/eslint-plugin'
import tsParser from '@typescript-eslint/parser'
import { dirname } from 'path'
import { fileURLToPath } from 'url'

const compat = new FlatCompat({ baseDirectory: dirname(fileURLToPath(import.meta.url)) })

const config = [
  {
    ignores: ['.next/**', 'node_modules/**', 'next-env.d.ts', 'playwright-report/**', 'test-results/**'],
  },
  ...compat.extends('next/core-web-vitals'),
  {
    // Đăng ký plugin TypeScript để các chỉ thị `// eslint-disable-next-line @typescript-eslint/...`
    // sẵn có trong mã nguồn được resolve. `next/core-web-vitals` parse file bằng parser mặc định
    // của ESLint nên `.ts`/`.tsx` cần parser riêng.
    files: ['**/*.ts', '**/*.tsx'],
    plugins: { '@typescript-eslint': tsPlugin },
    languageOptions: { parser: tsParser },
    rules: {
      // Cố ý để `off`. Trước khi đăng ký plugin, các chỉ thị
      // `// eslint-disable-next-line @typescript-eslint/no-explicit-any` trong mã nguồn là **vô
      // hiệu** vì rule chưa tồn tại — ESLint báo "rule not found" chứ không ẩn lỗi. Bật rule lên
      // `error` lộ ngay 20 chỗ `any` ở `src/lib/api.ts`, `recipe-editor.ts`, `recipe-jsonld.ts`.
      // Việc thay `any` bằng type thật **ngoài phạm vi GĐ1/N2-A2** và sẽ chặn CI vì thứ không liên
      // quan tới cổng này → ghi nhận phát sinh, xử lý ở đợt sau. Rule vẫn được **đăng ký** để các
      // chỉ thị disable sẵn có trở lại đúng nghĩa khi ai đó bật rule.
      '@typescript-eslint/no-explicit-any': 'off',
    },
  },
]

export default config
