// K22 (TV3) — tải nhẹ cho trang chi tiết công thức: API GET /api/v1/recipes/{slug} và trang Next.js /recipes/{slug}.
// Chạy (k6 trong Docker, app chạy trên máy Windows):
//   Get-Content -Raw tests/k6/recipe-detail.js | docker run --rm -i grafana/k6 run --quiet `
//     -e API=http://host.docker.internal:5080/api/v1 -e WEB=http://host.docker.internal:3000 -e SLUG=ga-lac-pho-mai-cay -
// Ngưỡng theo NFR-PERF-001 (GET p50 <= 150 ms) cho API; trang Next ghi số đo, ngưỡng lỏng hơn (chạy `next dev` không phải bản build).
import http from "k6/http";
import { check } from "k6";

const API = __ENV.API || "http://host.docker.internal:5080/api/v1";
const WEB = __ENV.WEB || "http://host.docker.internal:3000";
const SLUG = __ENV.SLUG || "ga-lac-pho-mai-cay";
const VUS = Number(__ENV.VUS || 5);
const DURATION = __ENV.DURATION || "20s";

export const options = {
  scenarios: {
    api: { executor: "constant-vus", vus: VUS, duration: DURATION, exec: "api", tags: { target: "api" } },
    page: { executor: "constant-vus", vus: VUS, duration: DURATION, exec: "page", tags: { target: "page" }, startTime: DURATION },
  },
  thresholds: {
    "http_req_failed": ["rate<0.01"],
    "http_req_duration{target:api}": ["p(50)<150", "p(95)<500"],
    "http_req_duration{target:page}": ["p(95)<2000"],
  },
  summaryTrendStats: ["avg", "min", "med", "p(90)", "p(95)", "p(99)", "max"],
};

export function api() {
  const r = http.get(`${API}/recipes/${SLUG}`);
  check(r, { "api 200": x => x.status === 200, "api co authorName": x => x.body.includes("\"authorName\"") });
}

export function page() {
  const r = http.get(`${WEB}/recipes/${SLUG}`);
  check(r, { "page 200": x => x.status === 200, "page co JSON-LD": x => x.body.includes("application/ld+json") });
}
