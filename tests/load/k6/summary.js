// Smoke/load template. Run: k6 run -e BASE_URL=http://localhost:5004 -e TOKEN=<jwt> tests/load/k6/summary.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 20 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5004';
const TOKEN = __ENV.TOKEN || '';

export default function () {
  const res = http.get(`${BASE_URL}/api/summary`, {
    headers: { Authorization: `Bearer ${TOKEN}`, 'X-Correlation-ID': `k6-${__VU}-${__ITER}` },
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);
}
