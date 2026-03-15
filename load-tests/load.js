import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '2m', target: 10 },   // Warm up
        { duration: '3m', target: 50 },   // Ramp to medium load
        { duration: '3m', target: 100 },  // Ramp to peak load
        { duration: '2m', target: 0 },    // Cool down
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'],  // p95 must be under 1 second
        http_req_failed: ['rate<0.01'],     // Error rate must be under 1%
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5068';

export default function () {
    const pages = [
        `${BASE_URL}/`,
        `${BASE_URL}/Product`,
        `${BASE_URL}/Product?page=1&categoryId=1`,
        `${BASE_URL}/Product?page=2`,
        `${BASE_URL}/Product/SearchSuggestions?query=laptop`,
    ];

    const page = pages[Math.floor(Math.random() * pages.length)];
    const res = http.get(page);

    check(res, {
        'status is 200 or 429': (r) => r.status === 200 || r.status === 429,
        'response time < 2s': (r) => r.timings.duration < 2000,
    });

    sleep(Math.random() * 2 + 0.5); 
}
