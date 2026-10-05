// ==============================================================================
// Script: auth-profile-load.js (k6 Load & Performance Testing)
// Tác giả: Nguyễn Thanh Tâm (TV1 - 2312741) - Tuần 5
// Mục đích: Đo lường hiệu năng, độ trễ p95, throughput RPS các endpoint Auth & API
// Tiêu chuẩn nghiệm thu: p95 < 200ms, tỷ lệ lỗi < 1%
// ==============================================================================
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10s', target: 20 }, // Ramp up 20 virtual users
    { duration: '30s', target: 50 }, // Duy trì tải 50 users
    { duration: '10s', target: 0 },  // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<200'], // 95% requests dưới 200ms
    http_req_failed: ['rate<0.01'],   // Tỷ lệ lỗi dưới 1%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080/api/v1';

export default function () {
  // 1. Test Discovery / Recipes (Cache / DB Query)
  const recipesRes = http.get(`${BASE_URL}/recipes?pageSize=10`);
  check(recipesRes, {
    'recipes status is 200': (r) => r.status === 200,
    'recipes duration < 100ms': (r) => r.timings.duration < 100,
  });

  // 2. Test Search FTS
  const searchRes = http.get(`${BASE_URL}/recipes/search?q=pho`);
  check(searchRes, {
    'search status is 200': (r) => r.status === 200,
  });

  // 3. Test Health Check
  const healthRes = http.get(`${BASE_URL.replace('/api/v1', '')}/health/live`);
  check(healthRes, {
    'health live is 200': (r) => r.status === 200,
  });

  sleep(0.5);
}
