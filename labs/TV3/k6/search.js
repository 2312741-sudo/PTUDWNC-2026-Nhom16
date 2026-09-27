// k6 cho LAB L3 (K11/K22):  k6 run -e BASE=http://localhost:5090 labs/TV3/k6/search.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 20,
  duration: '30s',
  thresholds: { http_req_duration: ['p(95)<300'], checks: ['rate>0.99'] },
};

const words = ['pho', 'bun cha', 'com tam', 'ga', 'banh xeo'];

export default function () {
  const q = encodeURIComponent(words[Math.floor(Math.random() * words.length)]);
  const res = http.get(`${__ENV.BASE || 'http://localhost:5090'}/lab/l3/search?q=${q}`);
  check(res, { 'status 200': r => r.status === 200 });
}