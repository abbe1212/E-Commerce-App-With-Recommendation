import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5068';

// Custom metrics for stress test monitoring
const stressTestFailures = new Counter('stress_test_failures');
const stressTestDuration = new Trend('stress_test_duration');

// Configuration - DESIGNED TO FIND BREAKING POINT
// This test will FAIL when the application reaches capacity - that's the goal
export const options = {
  scenarios: {
    stressTest: {
      executor: 'ramping-arrival-rate',
      startRate: 0,
      timeUnit: '1s',
      stages: [
        { duration: '2m', target: 50 },   // Ramp to 50 arrivals/sec
        { duration: '3m', target: 100 },  // Sustain 100 arrivals/sec
        { duration: '3m', target: 150 },  // Push to 150 arrivals/sec
        { duration: '3m', target: 200 },  // Push to 200 arrivals/sec
        { duration: '2m', target: 250 },  // Push to 250 arrivals/sec
        { duration: '2m', target: 300 },  // Push to breaking point (300 arrivals/sec)
        { duration: '2m', target: 0 },    // Ramp down
      ],
      preAllocatedVUs: 50,
      maxVUs: 500, // Allow dynamic allocation up to 500 VUs
    },
  },
  thresholds: {
    // Aggressive thresholds - test WILL FAIL at breaking point (that's success)
    'http_req_duration': ['p(95)<5000'], // Fail if p95 response time > 5 seconds
    'http_req_failed': ['rate<0.10'],    // Fail if error rate > 10%
    'http_reqs': ['rate>50'],            // Require at least 50 requests/second
    'stress_test_failures': ['count<100000'], // Track failure count
    'checks': ['rate>0.85'],             // Allow some checks to fail at breaking point
  },
};

// CSRF token extraction function (copied from user-journey.js)
function extractCSRFToken(htmlContent) {
  const match = htmlContent.match(/name="__RequestVerificationToken".*?value="([^"]+)"/);
  return match ? match[1] : null;
}

// Generate unique test user email for stress testing
// Format: stresstest_{timestamp}_{VU}_{ITER}@loadtest.com
function generateTestUserEmail() {
  const timestamp = Math.floor(Date.now() / 1000);
  const vu = __VU;
  const iter = __ITER;
  return `stresstest_${timestamp}_${vu}_${iter}@loadtest.com`;
}

export default function () {
  const startTime = new Date();
  let sessionCookie = '';
  let failures = 0;

  try {
    // ===== CRITICAL PATH 1: Browse Products =====
    group('Browse Products', () => {
      // GET /Product - Product listing
      const res = http.get(`${BASE_URL}/Product`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
      });
      if (!check(res, {
        'product list loads': (r) => r.status === 200,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.5);
    });

    // ===== CRITICAL PATH 2: View Product Details =====
    group('View Product Details', () => {
      // GET /Product/Details/{id} - Product details page (use a valid seeded product ID)
      const res = http.get(`${BASE_URL}/Product/Details/12`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
      });
      if (!check(res, {
        'product details load': (r) => r.status === 200,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.5);
    });

    // ===== CRITICAL PATH 3: Search Suggestions =====
    group('Search Suggestions', () => {
      // GET /Product/SearchSuggestions - Search with rate limiting (429 acceptable)
      const res = http.get(`${BASE_URL}/Product/SearchSuggestions?query=laptop`, {
        headers: {
          'Accept': 'application/json',
        },
      });
      if (!check(res, {
        'search suggestions respond': (r) => r.status === 200 || r.status === 429,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);
    });

    // ===== CRITICAL PATH 4: Register New User =====
    const testUserEmail = generateTestUserEmail();
    let csrfToken = '';

    group('Register New User', () => {
      // Get registration page to extract CSRF token
      let res = http.get(`${BASE_URL}/Account/Register`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
      });
      if (!check(res, {
        'registration page loads': (r) => r.status === 200,
      })) {
        failures++;
        stressTestFailures.add(1);
      }

      csrfToken = extractCSRFToken(res.body);
      if (!check({ csrfToken }, {
        'CSRF token extracted for registration': (c) => c.csrfToken !== null,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);

      // POST /Account/Register - Submit registration with CSRF token
      res = http.post(
        `${BASE_URL}/Account/Register`,
        {
          FirstName: 'Stress',
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
      if (!check(res, {
        'registration succeeds or redirects': (r) => r.status === 200 || r.status === 302 || r.status === 303,
      })) {
        failures++;
        stressTestFailures.add(1);
      }

      // Extract session cookie (.AspNetCore.Identity.Application)
      const cookies = res.cookies;
      if (cookies && cookies['.AspNetCore.Identity.Application']) {
        sessionCookie = cookies['.AspNetCore.Identity.Application'][0].value;
      }
      sleep(0.3);
    });

    // ===== CRITICAL PATH 5: Login =====
    group('Login', () => {
      // Get login page to extract CSRF token
      let res = http.get(`${BASE_URL}/Account/Login`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
        cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
      });
      if (!check(res, {
        'login page loads': (r) => r.status === 200,
      })) {
        failures++;
        stressTestFailures.add(1);
      }

      csrfToken = extractCSRFToken(res.body);
      if (!check({ csrfToken }, {
        'CSRF token extracted for login': (c) => c.csrfToken !== null,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);

      // POST /Account/Login - Submit login with CSRF token
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
      if (!check(res, {
        'login succeeds or redirects': (r) => r.status === 200 || r.status === 302 || r.status === 303,
      })) {
        failures++;
        stressTestFailures.add(1);
      }

      // Update session cookie
      if (res.cookies && res.cookies['.AspNetCore.Identity.Application']) {
        sessionCookie = res.cookies['.AspNetCore.Identity.Application'][0].value;
      }
      sleep(0.3);
    });

    // ===== CRITICAL PATH 6: Add to Cart (Authenticated) =====
    group('Add to Cart', () => {
      // POST /Product/AddToCart - Add product to cart (authenticated, requires session)
      const res = http.post(
        `${BASE_URL}/Product/AddToCart`,
        {
          productId: '12',
          quantity: '1',
        },
        {
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
          },
          redirects: 0,
          cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
        }
      );
      if (!check(res, {
        'add to cart succeeds or redirects': (r) => r.status === 200 || r.status === 302 || r.status === 303,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);
    });

    // ===== CRITICAL PATH 7: View Cart =====
    group('View Cart', () => {
      // GET /Cart/Index - View shopping cart
      const res = http.get(`${BASE_URL}/Cart/Index`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
        cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
      });
      if (!check(res, {
        'cart page loads': (r) => r.status === 200 || r.status === 302,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);
    });

    // ===== CRITICAL PATH 8: Checkout =====
    group('Checkout', () => {
      // GET /Checkout - Access checkout page
      const res = http.get(`${BASE_URL}/Checkout`, {
        headers: {
          'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        },
        cookies: sessionCookie ? { '.AspNetCore.Identity.Application': sessionCookie } : undefined,
        redirects: 0,
      });
      if (!check(res, {
        'checkout page accessible': (r) => r.status === 200 || r.status === 302,
      })) {
        failures++;
        stressTestFailures.add(1);
      }
      sleep(0.3);
    });

    // Calculate iteration duration and record metric
    const endTime = new Date();
    const duration = endTime - startTime;
    stressTestDuration.add(duration);

  } catch (error) {
    stressTestFailures.add(1);
  }

  sleep(0.5);
}

// Summary handler - output results to JSON and stdout
export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    note: 'This stress test is DESIGNED TO FAIL at the breaking point - that indicates success in finding capacity limits',
    breakingPoint: {
      description: 'Test fails when one or more of these conditions are met:',
      conditions: [
        'HTTP p95 response time exceeds 5000ms',
        'HTTP error rate exceeds 10%',
        'Request rate drops below 50 requests/second',
      ],
    },
    metrics: {
      vus: data.metrics.vus,
      duration: data.metrics.iteration_duration,
      httpReqDuration: data.metrics.http_req_duration,
      httpReqFailed: data.metrics.http_req_failed,
      stressTestFailures: data.metrics.stress_test_failures,
      stressTestDuration: data.metrics.stress_test_duration,
      checks: data.metrics.checks,
    },
    options: options,
  };

  const jsonSummary = JSON.stringify(summary, null, 2);
  return {
    'load-tests/results/stress-test-summary.json': jsonSummary,
    stdout: textSummary(data),
  };
}

function textSummary(data) {
  const vus = data.metrics.vus;
  const duration = data.metrics.iteration_duration;
  const httpReqDuration = data.metrics.http_req_duration;
  const httpErrors = data.metrics.http_req_failed;
  const checks = data.metrics.checks;
  const stressFailures = data.metrics.stress_test_failures;

  return `
╔════════════════════════════════════════════════════════════════╗
║              STRESS TEST RESULTS - BREAKING POINT ANALYSIS      ║
╚════════════════════════════════════════════════════════════════╝

⚠️  TEST DESIGNED TO FAIL AT BREAKING POINT - FAILURE = SUCCESS ⚠️

📊 PERFORMANCE METRICS:
  Peak VUs (Virtual Users):    ${vus ? vus.values.value : 'N/A'}
  Max Request Duration (ms):   ${httpReqDuration ? httpReqDuration.values.max : 'N/A'}
  P95 Response Time (ms):      ${httpReqDuration && httpReqDuration.values['p(95)'] ? httpReqDuration.values['p(95)'] : 'N/A'}
  P99 Response Time (ms):      ${httpReqDuration && httpReqDuration.values['p(99)'] ? httpReqDuration.values['p(99)'] : 'N/A'}

📉 ERROR METRICS:
  HTTP Error Rate:            ${httpErrors ? (httpErrors.values.rate * 100).toFixed(2) : 0}%
  Failed Checks Rate:         ${checks ? ((1 - checks.values.rate) * 100).toFixed(2) : 0}%
  Total Stress Test Failures: ${stressFailures ? stressFailures.values.count : 0}

⏱️  TEST DURATION & LOAD:
  Test Duration:              ${duration ? (duration.values.max / 1000).toFixed(1) : 'N/A'}s
  Avg Iteration Duration:     ${duration ? (duration.values.avg / 1000).toFixed(2) : 'N/A'}s
  Test Timestamp:             ${new Date().toISOString()}

🎯 BREAKING POINT INDICATORS (Test fails if any threshold exceeded):
  ✓ P95 Response Time Limit:   5000ms
  ✓ Error Rate Limit:          10%
  ✓ Minimum Request Rate:      50 req/sec

💡 INTERPRETATION:
  If test failed above thresholds = BREAKING POINT FOUND ✓
  If test passed all thresholds = System can handle higher load

═══════════════════════════════════════════════════════════════════
  `;
}
