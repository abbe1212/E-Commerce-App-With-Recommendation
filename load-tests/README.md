# Load Testing Guide

This directory contains k6 load tests for the e-commerce application. These tests help identify performance bottlenecks, validate system stability under load, and find the maximum capacity of the application.

## Overview

Load testing ensures the application can handle expected traffic and remains responsive during peak usage. We use [k6](https://k6.io/), an open-source load testing tool, to simulate various user behaviors and traffic patterns.

## Prerequisites

Before running the tests, ensure you have the following:

1.  **k6 Installed**: Follow the [official installation guide](https://k6.io/docs/get-started/installation/).
2.  **Application Running**: The .NET application must be running, typically at `http://localhost:5068`.
3.  **Seeded Database**: The database should contain at least one product and one category for the tests to function correctly.

## Environment Configuration

The tests use the `BASE_URL` environment variable to determine the target application.

*   **Default**: `http://localhost:5068`
*   **Override**: Set the variable before running k6.

```bash
# Windows (PowerShell)
$env:BASE_URL = "http://localhost:XXXX"; k6 run load-tests/smoke.js

# Linux/macOS
export BASE_URL=http://localhost:XXXX && k6 run load-tests/smoke.js
```

## Test Types

| Test Script | Type | Duration | Load Profile | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `smoke.js` | Smoke | 1 min | 5 VUs | Quick sanity check |
| `load.js` | Load | 10 min | 10 to 100 VUs | Performance under expected load |
| `user-journey.js` | Journey | 9 min | 0 to 20 VUs | Full shopping flow with auth |
| `stress-test.js` | Stress | 17 min | 50 to 300 req/s | Find breaking point (designed to fail) |

### Smoke Test (`smoke.js`)
A minimal load test to verify the application is up and basic endpoints respond correctly. Run this after every deployment or major change.
*   **Thresholds**: p95 < 2s, error rate < 1%.

### Load Test (`load.js`)
Simulates a typical day of traffic by ramping up to 100 concurrent users. It hits various product and search pages randomly.
*   **Thresholds**: p95 < 1s, error rate < 1%.

### User Journey Test (`user-journey.js`)
Simulates a complete shopping experience, including browsing, searching, registering a new account, logging in, and starting the checkout process.
*   **Thresholds**: p95 < 3s, error rate < 5%, journey duration < 30s.
*   **Note**: Creates test accounts with the format `testuser_{timestamp}_{VU}_{ITER}@loadtest.com`.

### Stress Test (`stress-test.js`)
Pushes the application to its limits by steadily increasing the arrival rate of new users. **This test is designed to fail.** The goal is to find where the system breaks.
*   **Thresholds**: p95 < 5s, error rate < 10%.
*   **Note**: Creates test accounts with the format `stresstest_{timestamp}_{VU}_{ITER}@loadtest.com`.

## Execution Commands

Run tests from the project root directory:

```bash
# Run smoke test
k6 run load-tests/smoke.js

# Run load test
k6 run load-tests/load.js

# Run user journey test
k6 run load-tests/user-journey.js

# Run stress test
k6 run load-tests/stress-test.js
```

## Interpreting Results

k6 provides a summary in the terminal after each run.

*   **Green Checkmarks**: All thresholds were met.
*   **Red X**: A threshold was breached. This is expected for the stress test.
*   **p95/p99**: These values show the response time for the 95th and 99th percentiles. For example, p95 < 1000ms means 95% of requests finished in less than 1 second.
*   **Error Rate**: The percentage of failed requests.

### Output Files
Summary results are saved to `load-tests/results/*.json`. These files are useful for CI/CD integration or comparing performance over time.

## Troubleshooting

*   **Connection Refused**: Ensure the application is running and the `BASE_URL` is correct.
*   **Rate Limiting (429)**: The `SearchSuggestions` endpoint is limited to 20 requests per 10 seconds. Receiving 429 errors on this endpoint is expected and handled by the tests.
*   **CSRF Token Not Found**: This happens if the HTML structure of the login or registration pages changes. Check if the `__RequestVerificationToken` input still exists.
*   **High Error Rate**: Check the application logs in `Ecoomerce.Web/Logs/` for server-side exceptions.

## Performance Baselines

Expect these results under normal conditions:

| Metric | Smoke/Load Test | User Journey | Stress Test |
| :--- | :--- | :--- | :--- |
| p95 Response Time | < 1s | < 3s | < 5s (until break) |
| Error Rate | < 1% | < 5% | < 10% |

Be concerned if p95 exceeds 3 seconds or the error rate goes above 5% during smoke or load tests.
