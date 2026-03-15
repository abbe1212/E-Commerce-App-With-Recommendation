import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 5,
    duration: '1m',
    thresholds: {
        http_req_duration: ['p(95)<2000'],
        http_req_failed: ['rate<0.01'],
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5068';

export default function () {
    // Product listing — most common page
    const productList = http.get(`${BASE_URL}/Product`);
    check(productList, {
        'product list status 200': (r) => r.status === 200,
    });
    sleep(1);

    // Home page
    const home = http.get(`${BASE_URL}/`);
    check(home, {
        'home status 200': (r) => r.status === 200,
    });
    sleep(1);

    // Search suggestions (rate-limited — 200 or 429 both acceptable)
    const search = http.get(`${BASE_URL}/Product/SearchSuggestions?query=phone`);
    check(search, {
        'search status 200 or 429': (r) => r.status === 200 || r.status === 429,
    });
    sleep(0.5);
}
