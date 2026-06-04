import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/+$/, '');
const targetPath = __ENV.TARGET_PATH || '/.well-known/openid-configuration';
const vus = Number.parseInt(__ENV.K6_VUS || '5', 10);
const duration = __ENV.K6_DURATION || '30s';

export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus,
      duration,
      gracefulStop: '5s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500']
  }
};

export default function () {
  const response = http.get(`${baseUrl}${targetPath}`, {
    tags: {
      service: 'auth',
      path: targetPath
    }
  });

  check(response, {
    'status is 200': (res) => res.status === 200,
    'has payload': (res) => res.body && res.body.length > 0
  });

  sleep(1);
}
