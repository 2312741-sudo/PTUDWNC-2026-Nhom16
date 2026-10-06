// N2-C3/C4 — k6 load test cho các endpoint đọc công khai.
//
// Vì sao script nằm trong repo thay vì gõ tay bằng heredoc:
//   1. Người khác (TV1/TV2/reviewer) phải chạy lại được đúng workload mà TV4 đã đo, không phải đoán.
//   2. Ngưỡng (threshold) là **hợp đồng**: CI hoặc máy khác chạy lại mà đỏ thì biết ngay là hồi quy.
//   3. Kết quả p50/p95/p99 chỉ có ý nghĩa nếu biết trước workload, thời lượng và trạng thái cache.
//
// BA CÀNH ĐO RIÊNG, vì đây là điều dễ nhầm nhất khi báo cáo số liệu:
//   - list_cached   : lấy đúng trang đã có trong Redis  -> đo đường đọc cache
//   - list_uncached : page ngẫu nhiên                   -> buộc cache miss, đo đường xuống Postgres
//   - read_mixed    : list + search + categories         -> workload người dùng thật
// Gộp cả ba rồi báo một con số p95 sẽ che mất việc đường DB chậm hơn đường cache bao nhiêu.
//
// CÁCH CHẠY (xem tests/performance/README.md để có bản ghi log đầy đủ):
//   k6 run tests/performance/read-load.js
//   BASE_URL=http://localhost:5080 RATE=20 DURATION=30s k6 summary -r ...
//
// LƯU Ý KHI ĐO: phải nói rõ trạng thái cache. Chạy lại ngay sau lần chạy trước thì phần lớn request
// đã trong Redis và p95 sẽ đẹp hơn thực tế. Muốn đo đường DB thì flush cache trước (C2 cũng dùng).

import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

// Workload: arrival-rate cố định nên p95 ổn định hơn `per-vu` (không phụ thuộc máy tải chậm bao lâu).
const RATE = Number(__ENV.RATE || 20); // request/giây, chia đều cho 3 scenario
const DURATION = __ENV.DURATION || '30s';
const PRE_VUS = Number(__ENV.PRE_VUS || 50);
const MAX_VUS = Number(__ENV.MAX_VUS || 100);

// Ngưỡng rút gọn cho môi trường lab. Production phải siết theo SLO thật, đừng copy nguyên xi.
const THRESHOLD_CACHED_MS = Number(__ENV.THRESHOLD_CACHED_MS || 200);
const THRESHOLD_UNCACHED_MS = Number(__ENV.THRESHOLD_UNCACHED_MS || 800);
const THRESHOLD_MIXED_MS = Number(__ENV.THRESHOLD_MIXED_MS || 500);

export const options = {
  // p(50)=med: k6 tên là `med`. Ghi rõ ở đây để lệnh `k6 summary` mặc định in ra cả p50/p95/p99.
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
  scenarios: {
    list_cached: {
      executor: 'constant-arrival-rate',
      exec: 'listCached',
      rate: RATE,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: PRE_VUS,
      maxVUs: MAX_VUS,
      tags: { scenario: 'list_cached' },
    },
    list_uncached: {
      executor: 'constant-arrival-rate',
      exec: 'listUncached',
      rate: RATE,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: PRE_VUS,
      maxVUs: MAX_VUS,
      tags: { scenario: 'list_uncached' },
    },
    read_mixed: {
      executor: 'constant-arrival-rate',
      exec: 'readMixed',
      rate: RATE,
      timeUnit: '1s',
      duration: DURATION,
      preAllocatedVUs: PRE_VUS,
      maxVUs: MAX_VUS,
      tags: { scenario: 'read_mixed' },
    },
  },
  thresholds: {
    // Một lỗi 5xx/timeout là báo động đỏ — tính cả request bị ngắt (http_req_failed).
    http_req_failed: ['rate==0'],
    'http_req_duration{scenario:list_cached}': [`p(95)<${THRESHOLD_CACHED_MS}`],
    'http_req_duration{scenario:list_uncached}': [`p(95)<${THRESHOLD_UNCACHED_MS}`],
    'http_req_duration{scenario:read_mixed}': [`p(95)<${THRESHOLD_MIXED_MS}`],
  },
};

/**
 * Một lần chạy trước để lấy dữ liệu thật: một slug recipe đang Published và một khoá tìm kiếm.
 * Không hard-code slug vì mỗi DB seed tự sinh slug khác nhau — hard-code là script chạy được trên
 * máy TV4 và hỏng trên máy người khác, đúng cái bài học của E2E tuần này.
 */
export function setup() {
  const list = http.get(`${BASE_URL}/api/v1/recipes?page=1&pageSize=5`, { tags: { phase: 'setup' } });
  // k6 Response KHÔNG có `.ok()` (đó là của fetch/axios). Phải so `status` trực tiếp.
  if (list.status !== 200) {
    throw new Error(`Không đọc được danh sách recipe (HTTP ${list.status}). Cần backend đang chạy tại ${BASE_URL}?`);
  }
  const body = list.json();
  // Shape thật của API là `{ data: [...], meta: {...} }` — `data` là MẢNG trực tiếp, không phải
  // `{ items: [...] }`. Đoán sai shape làm setup() ném "danh sách rỗng" dù DB có dữ liệu.
  const items = (body && body.data) || [];
  if (!Array.isArray(items) || !items.length) {
    throw new Error('Danh sách recipe rỗng — cần seed dữ liệu Published trước khi đo.');
  }

  const slug = items[0].slug;
  const keyword = (items[0].title || '').trim().split(/\s+/)[0];

  return { slug, keyword };
}

function expectOk(res, name) {
  check(res, {
    [`${name} trả 200`]: (r) => r.status === 200,
    [`${name} không phải 5xx`]: (r) => r.status < 500,
  });
}

/** Đường đọc cache: cùng một page đã được đảm bảo nằm trong Redis. */
export function listCached() {
  const res = http.get(`${BASE_URL}/api/v1/recipes?page=1&pageSize=12`, {
    tags: { scenario: 'list_cached', endpoint: 'recipes.list' },
  });
  expectOk(res, 'recipes.list');
}

/**
 * Đường xuống Postgres: page ngẫu nhiên trong khoảng rộng nên gần như chắc chắn cache miss.
 * Cách này lấy từ `TracingObservabilityTests` — trước đây dùng page cố định thì test chập chờn
 * theo thứ tự chạy vì lần trước đã làm ấm cache.
 */
export function listUncached() {
  const page = Math.floor(Math.random() * 900000) + 1000;
  const res = http.get(`${BASE_URL}/api/v1/recipes?page=${page}&pageSize=12`, {
    tags: { scenario: 'list_uncached', endpoint: 'recipes.list' },
  });
  expectOk(res, 'recipes.list(uncached)');
}

/** Workload người dùng thật: xem trang danh sách, tìm kiếm, xem chi tiết, xem danh mục. */
export function readMixed(data) {
  const list = http.get(`${BASE_URL}/api/v1/recipes?page=1&pageSize=12`, {
    tags: { scenario: 'read_mixed', endpoint: 'recipes.list' },
  });
  expectOk(list, 'recipes.list');

  if (data.keyword) {
    const search = http.get(
      `${BASE_URL}/api/v1/recipes/search?q=${encodeURIComponent(data.keyword)}&page=1&pageSize=12`,
      { tags: { scenario: 'read_mixed', endpoint: 'recipes.search' } },
    );
    expectOk(search, 'recipes.search');
  }

  const detail = http.get(`${BASE_URL}/api/v1/recipes/${encodeURIComponent(data.slug)}`, {
    tags: { scenario: 'read_mixed', endpoint: 'recipes.detail' },
  });
  expectOk(detail, 'recipes.detail');

  const cats = http.get(`${BASE_URL}/api/v1/categories`, {
    tags: { scenario: 'read_mixed', endpoint: 'categories' },
  });
  expectOk(cats, 'categories');
}
