import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5068';

// Custom metrics
const journeyCompletions = new Counter('journey_completions');
const journeyDuration = new Trend('journey_duration');

// Configuration
export const options = {
  scenarios: {
    rampingVUs: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 5 },
        { duration: '2m', target: 10 },
        { duration: '3m', target: 20 },
        { duration: '2m', target: 10 },
        { duration: '1m', target: 0 },
      ],
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<3000'],
    'http_req_failed': ['rate<0.05'],
    'journey_completions': ['count>50'],
    'journey_duration': ['p(95)<30000'],
    'checks': ['rate>0.90'],
  },
};

// CSRF token extraction function
function extractCSRFToken(htmlContent) {
  const match = htmlContent.match(/name="__RequestVerificationToken".*?value="([^"]+)"/);
  return match ? match[1] : null;
}

// Generate unique test user email
function generateTestUserEmail() {
  const timestamp = Math.floor(Date.now() / 1000);
  const vu = __VU;
  const iter = __ITER;
  return `testuser_${timestamp}_${vu}_${iter}@loadtest.com`;
}

export default function () {
  const startTime = new Date();
  let sessionCookie = '';

  // ===== GROUP 1: Browse Products =====
  group('Browse Products', () => {
    // Homepage
    let res = http.get(`${BASE_URL}/`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'homepage loads': (r) => r.status === 200,
    });
    sleep(1);

    // Product list with pagination
    res = http.get(`${BASE_URL}/Product?page=1`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'product list loads': (r) => r.status === 200,
      'product list has content': (r) => r.body.includes('Product'),
    });
    sleep(1);

    // Category filter
    res = http.get(`${BASE_URL}/Product?categoryId=1`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'category filter works': (r) => r.status === 200,
    });
    sleep(1);
  });

  // ===== GROUP 2: View Product Details =====
  group('View Product Details', () => {
    const res = http.get(`${BASE_URL}/Product/Details/1`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'product details load': (r) => r.status === 200,
      'product details have content': (r) => r.body.includes('Price') || r.body.includes('Description'),
    });
    sleep(2);
  });

  // ===== GROUP 3: Search Products =====
  group('Search Products', () => {
    // Search suggestions (rate-limited, 429 is acceptable)
    let res = http.get(`${BASE_URL}/Product/SearchSuggestions?query=laptop`, {
      headers: {
        'Accept': 'application/json',
      },
    });
    check(res, {
      'search suggestions respond': (r) => r.status === 200 || r.status === 429,
    });
    sleep(0.5);

    // Full search
    res = http.get(`${BASE_URL}/Product?searchTerm=laptop`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'search results load': (r) => r.status === 200,
    });
    sleep(1);
  });

  // ===== GROUP 4: Add to Cart (Unauthenticated) =====
  group('Add to Cart - Unauthenticated', () => {
    const res = http.post(
      `${BASE_URL}/Product/AddToCart`,
      {
        productId: '1',
        quantity: '1',
      },
      {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        redirects: 0, // Don't follow redirect
      }
    );
    check(res, {
      'add to cart redirects (not auth)': (r) => r.status === 302 || r.status === 303,
    });
    sleep(1);
  });

  // ===== GROUP 5: Register New User =====
  const testUserEmail = generateTestUserEmail();
  let csrfToken = '';

  group('Register New User', () => {
    // Get registration page to extract CSRF token
    let res = http.get(`${BASE_URL}/Account/Register`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
    });
    check(res, {
      'registration page loads': (r) => r.status === 200,
    });

    csrfToken = extractCSRFToken(res.body);
    check({ csrfToken }, {
      'CSRF token extracted': (c) => c.csrfToken !== null,
    });
    sleep(1);

    // Submit registration
    res = http.post(
      `${BASE_URL}/Account/Register`,
      {
        FirstName: 'Load',
        LastName: 'Tester',
        Email: testUserEmail,
        Password: 'TestPassword123!',
        ConfirmPassword: 'TestPassword123!',
        __RequestVerificationToken: csrfToken,
      },
      {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        redirects: 0,
      }
    );
    check(res, {
      'registration succeeds or redirects': (r) => r.status === 200 || r.status === 302 || r.status === 303,
    });

    // Extract session cookie if present
    const cookies = res.cookies;
    if (cookies && cookies['.AspNetCore.Identity.Application']) {
      sessionCookie = cookies['.AspNetCore.Identity.Application'][0].value;
    }
    sleep(1);
  });

  // ===== GROUP 6: Login =====
  group('Login', () => {
    // Get login page to extract CSRF token
    let res = http.get(`${BASE_URL}/Account/Login`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
      cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
    });
    check(res, {
      'login page loads': (r) => r.status === 200,
    });

    csrfToken = extractCSRFToken(res.body);
    check({ csrfToken }, {
      'CSRF token extracted from login': (c) => c.csrfToken !== null,
    });
    sleep(1);

    // Submit login
    res = http.post(
      `${BASE_URL}/Account/Login`,
      {
        Email: testUserEmail,
        Password: 'TestPassword123!',
        RememberMe: 'false',
        __RequestVerificationToken: csrfToken,
      },
      {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        redirects: 0,
        cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
      }
    );
    check(res, {
      'login succeeds or redirects': (r) => r.status === 200 || r.status === 302 || r.status === 303,
    });

    // Update session cookie
    if (res.cookies && res.cookies['.AspNetCore.Identity.Application']) {
      sessionCookie = res.cookies['.AspNetCore.Identity.Application'][0].value;
    }
    sleep(1);
  });

  // ===== GROUP 7: View Cart =====
  group('View Cart', () => {
    const res = http.get(`${BASE_URL}/Cart/Index`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
      cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
    });
    check(res, {
      'cart page loads': (r) => r.status === 200 || r.status === 302,
    });
    sleep(1);
  });

  // ===== GROUP 8: Checkout Flow =====
  group('Checkout Flow', () => {
    // Access checkout page
    let res = http.get(`${BASE_URL}/Checkout`, {
      headers: {
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
      },
      cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
      redirects: 0,
    });
    check(res, {
      'checkout page accessible': (r) => r.status === 200 || r.status === 302,
    });
    sleep(1);

    // Get shipping methods
    res = http.get(`${BASE_URL}/Checkout/GetShippingMethods`, {
      headers: {
        'Accept': 'application/json',
      },
      cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
    });
    check(res, {
      'shipping methods loaded': (r) => r.status === 200 || r.status === 302,
    });
    sleep(1);
  });

  // Calculate journey duration and record metrics
  const endTime = new Date();
  const duration = endTime - startTime;
  journeyCompletions.add(1);
  journeyDuration.add(duration);

  sleep(1);
}

// Summary handler
export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    scenarios: data.metrics,
    options: options,
  };

  // Write to JSON file
  const jsonSummary = JSON.stringify(summary, null, 2);
  return {
    'load-tests/results/user-journey-summary.json': jsonSummary,
    stdout: textSummary(data),
  };
}

function textSummary(data) {
  const vus = data.metrics.vus;
  const duration = data.metrics.iteration_duration;
  const errors = data.metrics.http_req_failed;

  return `
===== USER JOURNEY LOAD TEST SUMMARY =====
Test Duration: ${duration ? duration.values.max : 'N/A'}ms
Virtual Users Peak: ${vus ? vus.values.value : 'N/A'}
HTTP Request Failures: ${errors ? errors.values.rate : 0}
Timestamp: ${new Date().toISOString()}
=========================================
  `;
}
