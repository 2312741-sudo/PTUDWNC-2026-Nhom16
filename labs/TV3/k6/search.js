// k6 cho LAB TV3 (K11/K22). Ngưỡng p95 < 500 ms là GIẢ ĐỊNH của nhóm (đề không ghi số cụ thể).
//   k6 run -e BASE=http://localhost:5090 labs/TV3/k6/search.js
//   Không cài k6: docker run --rm -i grafana/k6 run -e BASE=http://host.docker.internal:5090 - < labs/TV3/k6/search.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 20,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    'http_req_duration{page:api}': ['p(95)<500'],
    'http_req_duration{page:ssr}': ['p(95)<500'],
    'http_req_duration{page:list}': ['p(95)<500'],
    checks: ['rate>0.99'],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const BASE = __ENV.BASE || 'http://localhost:5090';
const words = ['pho', 'bun cha', 'com tam', 'ga', 'banh xeo'];

export default function () {
  const q = encodeURIComponent(words[Math.floor(Math.random() * words.length)]);
  const api = http.get(`${BASE}/lab/l3/search?q=${q}`, { tags: { page: 'api' } });       // JSON + Redis cache
  const ssr = http.get(`${BASE}/lab/l16/search?q=${q}`, { tags: { page: 'ssr' } });      // HTML render phía server (K16)
  const list = http.get(`${BASE}/lab/l22/recipes?take=20`, { tags: { page: 'list' } });  // danh sách + ảnh, 1 câu SQL (K22)
  check(api, { 'api 200': r => r.status === 200 });
  check(ssr, { 'ssr 200': r => r.status === 200 });
  check(list, { 'list 200 + 1 SQL': r => r.status === 200 && r.headers['X-Sql-Count'] === '1' });
}
