# Phase 3 — Feature Completion

## TL;DR

> **Quick Summary**: Complete all remaining Phase 3 features from the production readiness plan. The codebase is further along than the plan implies — several items (password reset, compare view, order history, address management, two-factor auth) are already fully done and require zero work. The plan targets only the genuine gaps.
>
> **Deliverables**:
> - Stripe webhook endpoint wired end-to-end (PaymentController + Order entity migration + raw body middleware)
> - Product recommendations using `IRecommendationService` in `ProductController.Details`
> - Root `WishlistController` stub eliminated (redirect to Profile area)
> - `[Authorize(Roles="Admin")]` added to all 3 Reporting controllers
> - 9 missing Reporting sub-views created (DailySales, TopProducts, Revenue, LowStock, OutOfStock, InventoryLogs, Activity, RegistrationTrend, TopCustomers)
> - Profile `DashboardController` wired with real data (orders, wishlist count, profile completeness)
> - Admin `OrderManagementController.Index` fully wired — filter, search, pagination, UpdateStatus, BulkUpdateStatus
>
> **Estimated Effort**: Large
> **Parallel Execution**: YES — 3 waves
> **Critical Path**: Task 1 (DB migration) → Task 3 (Stripe webhook) → Final Verification

---

## Context

### Original Request
Plan Phase 3 Feature Completion from the production readiness plan as a senior software architect with 15+ YOE.

### Codebase Reality (ground truth from direct file reads)

| Original Phase 3 Item | True Status | Work Needed |
|---|---|---|
| Wishlist | ✅ Done in Profile area — root stub is dead code | Fix/redirect root stub only |
| Stripe Webhook | ❌ 0% — PaymentController is 14-line stub, no PaymentIntentId on Order, no WebhookSecret config | Full implementation |
| Recommendations | ⚠️ Service exists, not injected into ProductController | Inject + wire, 1 file change |
| Reporting sub-views | ⚠️ Controllers done, only Index views exist | Create 9 missing sub-views |
| Reporting auth | ❌ No [Authorize(Roles="Admin")] on any reporting controller | Add attribute to 3 files |
| Profile Dashboard | ⚠️ Stub controller — returns View() with no data | Wire with real data |
| Admin Order Mgmt | ⚠️ 20% done — GetAllOrdersAsync commented out, no filter/update actions | Uncomment + add actions |
| Profile / Address | ✅ Fully done | None |
| Product Compare | ✅ Fully done (controller + view) | None |
| Password Reset | ✅ Fully done | None |
| Email Confirmation | ✅ Fully done | None |
| Order History | ✅ Fully done | None |
| Two-Factor Auth | ✅ Views and controller exist | None |

### Key Code Signatures
```csharp
// Needs adding to Order entity:
public string? PaymentIntentId { get; set; }

// Needs adding to IOrderService / OrderService:
Task ConfirmPaymentByIntentIdAsync(string paymentIntentId);

// ProductController — field to add:
private readonly IRecommendationService _recommendationService;

// ProductController.Details — replace lines 158-160 with:
var recommendedProducts = (await _recommendationService.GetRelatedProductsAsync(id, 6))
    .Where(p => p.ProductID != id).Take(4).ToList();

// OrderManagementController.Index — uncomment:
var orders = await _orderService.GetAllOrdersAsync();
```

### Infrastructure Status
- **Program.cs DI**: All Phase 3 services already registered (IWishlistService, IRecommendationService, IOrderService, ISalesReportService, IInventoryReportService, IUserReportService, IEmailSenderService) ✅
- **_Layout.cshtml**: Navbar fully wired to all Profile area pages ✅
- **appsettings.json**: Stripe.PublishableKey + SecretKey present, **WebhookSecret missing** ❌
- **Order entity**: No PaymentIntentId field ❌
- **Raw body middleware**: Not configured ❌

---

## Work Objectives

### Core Objective
Close the 7 genuine implementation gaps in Phase 3, producing a fully feature-complete application that compiles, runs, and passes all QA scenarios — without touching already-complete areas.

### Concrete Deliverables
- `Ecommerce.Core/Entities/Order.cs` — `PaymentIntentId` property added
- EF Core migration for `PaymentIntentId` column
- `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — `ConfirmPaymentByIntentIdAsync` added
- `Ecommerce.Application/Services/Implementations/OrderService.cs` — method implemented
- `appsettings.json` + `appsettings.Development.json` — `Stripe:WebhookSecret` key added (placeholder value)
- `Ecoomerce.Web/Controllers/PaymentController.cs` — full implementation: `Index`, `Success`, `Cancel`, `Webhook`
- `Program.cs` — raw body middleware for `/Payment/Webhook` route
- `Ecoomerce.Web/Controllers/ProductController.cs` — `IRecommendationService` injected, `Details` action updated
- `Ecoomerce.Web/Controllers/WishlistController.cs` — redirect stub to Profile area or removed
- `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs` — `[Authorize(Roles="Admin")]` added
- `Ecoomerce.Web/Areas/Reporting/Controllers/InventoryController.cs` — `[Authorize(Roles="Admin")]` added
- `Ecoomerce.Web/Areas/Reporting/Controllers/UserController.cs` — `[Authorize(Roles="Admin")]` added
- 9 Reporting sub-views created (see Tasks 6–14)
- `Ecoomerce.Web/Areas/Profile/Controllers/DashboardController.cs` — wired with real data
- `Ecoomerce.Web/Areas/Admin/Controllers/OrderManagementController.cs` — fully wired

### Definition of Done
- [ ] `dotnet build` exits with 0 errors
- [ ] All QA scenarios pass (see individual tasks)
- [ ] No unauthenticated access to `/Reporting/**` endpoints
- [ ] `/Payment/Webhook` returns 200 for valid Stripe events
- [ ] Product Details page shows recommendations from `IRecommendationService`, not raw search
- [ ] Profile Dashboard shows real order count and wishlist count
- [ ] Admin Order list loads, filters, and status updates work

### Must Have
- Stripe webhook uses raw body for signature verification (HMAC failure if buffered)
- `[Authorize(Roles="Admin")]` on all reporting AND admin order management controllers
- EF Core migration generated and applied before any webhook test
- `PaymentIntentId` on Order entity is nullable (`string?`) — not all orders use Stripe
- Webhook handler is `[AllowAnonymous]` and disables CSRF (`[IgnoreAntiforgeryToken]`) since it's called by Stripe servers

### Must NOT Have (Guardrails)
- **DO NOT** touch `Areas/Profile/Controllers/WishlistController.cs` — it is complete
- **DO NOT** touch `AccountController.cs` — all auth flows are done
- **DO NOT** touch `ProductController.Compare` or `Views/Product/Compare.cshtml` — complete
- **DO NOT** add duplicate DI registrations in `Program.cs` — all services already registered
- **DO NOT** buffer the raw body before the Stripe webhook action — breaks HMAC verification
- **DO NOT** require `Stripe:WebhookSecret` to be a real value for the build to succeed — use placeholder with graceful fallback
- **DO NOT** create new service layers or repositories — use existing `IOrderService`, `IWishlistService`, `IRecommendationService`
- **DO NOT** change `OrderService.GetAllOrdersAsync()` — it is already implemented correctly
- **DO NOT** add PayPal implementation — PayPal keys in appsettings are placeholders, out of scope
- **DO NOT** over-engineer reporting sub-views — simple Bootstrap 5 tables/cards matching existing style in Index views, no Chart.js unless already imported

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: Unknown (not verified in this session)
- **Automated tests**: None (no test tasks in this plan — focus is feature completion)
- **Agent-Executed QA**: MANDATORY for all tasks

### QA Policy
- **Frontend/UI**: Use Playwright (playwright skill) for reporting views, wishlist, dashboard
- **API/Backend**: Use Bash (curl) for webhook endpoint
- **Build**: `dotnet build` must pass as acceptance criterion for every task

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Foundation — must be sequential within, but start immediately):
├── Task 1: Add PaymentIntentId to Order entity + EF migration [quick]
├── Task 2: Add ConfirmPaymentByIntentIdAsync to IOrderService + OrderService [quick]
└── Task 3: Add Stripe:WebhookSecret to appsettings + configure raw body middleware [quick]

Wave 2 (Core implementations — all parallel after Wave 1):
├── Task 4: Implement PaymentController (full — Index, Success, Cancel, Webhook) [unspecified-high]
├── Task 5: Wire IRecommendationService into ProductController.Details [quick]
├── Task 6: Fix root WishlistController stub → redirect to Profile area [quick]
├── Task 7: Add [Authorize(Roles="Admin")] to 3 Reporting controllers [quick]
├── Task 8: Wire Profile DashboardController with real data [unspecified-high]
└── Task 9: Wire Admin OrderManagementController (Index, UpdateStatus, BulkUpdateStatus) [unspecified-high]

Wave 3 (Reporting sub-views — all parallel, depend only on Task 7):
├── Task 10: Sales/DailySales.cshtml [visual-engineering]
├── Task 11: Sales/TopProducts.cshtml [visual-engineering]
├── Task 12: Sales/Revenue.cshtml [visual-engineering]
├── Task 13: Inventory/LowStock.cshtml [visual-engineering]
├── Task 14: Inventory/OutOfStock.cshtml [visual-engineering]
├── Task 15: Inventory/InventoryLogs.cshtml [visual-engineering]
├── Task 16: User/Activity.cshtml [visual-engineering]
├── Task 17: User/RegistrationTrend.cshtml [visual-engineering]
└── Task 18: User/TopCustomers.cshtml [visual-engineering]

Wave FINAL (After ALL tasks — parallel independent review):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Build + code quality (unspecified-high)
├── Task F3: Full QA pass — all scenarios (unspecified-high + playwright)
└── Task F4: Scope fidelity — no creep, no regressions (deep)
```

### Dependency Matrix

| Task | Depends On | Blocks |
|---|---|---|
| 1 | — | 2, 4 |
| 2 | 1 | 4 |
| 3 | — | 4 |
| 4 | 1, 2, 3 | F1–F4 |
| 5 | — | F1–F4 |
| 6 | — | F1–F4 |
| 7 | — | 10–18 |
| 8 | — | F1–F4 |
| 9 | — | F1–F4 |
| 10–18 | 7 | F1–F4 |
| F1–F4 | ALL | — |

### Agent Dispatch Summary
- **Wave 1**: Tasks 1–3 → `quick` (3 agents or sequential)
- **Wave 2**: Tasks 4, 8, 9 → `unspecified-high`; Tasks 5, 6, 7 → `quick`
- **Wave 3**: Tasks 10–18 → `visual-engineering` (9 parallel)
- **Wave Final**: F1 → `oracle`; F2, F3 → `unspecified-high`; F4 → `deep`

---

## TODOs

---

- [x] 1. Add `PaymentIntentId` to `Order` entity and generate EF Core migration

  **What to do**:
  - Open `Ecommerce.Core/Entities/Order.cs`
  - Add after the `OrderNotes` property: `public string? PaymentIntentId { get; set; }`
  - Run `dotnet ef migrations add AddPaymentIntentIdToOrder --project Ecommerce.Infrastructure --startup-project Ecoomerce.Web` from solution root
  - Run `dotnet ef database update --project Ecommerce.Infrastructure --startup-project Ecoomerce.Web`
  - Verify migration file created in `Ecommerce.Infrastructure/Migrations/`

  **Must NOT do**:
  - Do NOT make `PaymentIntentId` required/non-nullable — not all orders go through Stripe
  - Do NOT alter any existing columns or rename anything in the migration
  - Do NOT touch `OrderService.cs` in this task

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single property addition + CLI migration command — trivial, well-defined
  - **Skills**: []
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed — no complex git operations

  **Parallelization**:
  - **Can Run In Parallel**: YES (with Task 3; must complete before Tasks 2 and 4)
  - **Parallel Group**: Wave 1
  - **Blocks**: Tasks 2, 4
  - **Blocked By**: None (can start immediately)

  **References**:

  **Pattern References**:
  - `Ecommerce.Core/Entities/Order.cs:28-36` — existing nullable string properties pattern (`ShippingMethod`, `OrderNotes`) — follow exact same syntax
  - `Ecommerce.Infrastructure/Migrations/` — look at any existing migration file to understand the migration naming convention used in this project

  **Acceptance Criteria**:
  - [ ] `Ecommerce.Core/Entities/Order.cs` contains `public string? PaymentIntentId { get; set; }`
  - [ ] Migration file exists in `Ecommerce.Infrastructure/Migrations/` with "PaymentIntentId" in the name
  - [ ] `dotnet build` exits with 0 errors
  - [ ] `dotnet ef migrations list` shows migration as Applied

  **QA Scenarios**:
  ```
  Scenario: Migration applied successfully
    Tool: Bash
    Preconditions: App compiled with new Order.cs change
    Steps:
      1. Run: dotnet ef migrations list --project Ecommerce.Infrastructure --startup-project Ecoomerce.Web
      2. Assert: last migration entry contains "PaymentIntentId" and shows "(applied)" status
    Expected Result: Migration listed as applied
    Failure Indicators: "pending" status or no migration listed
    Evidence: .sisyphus/evidence/task-1-migration-applied.txt

  Scenario: Order entity compiles without errors
    Tool: Bash
    Steps:
      1. Run: dotnet build from solution root
      2. Assert: "Build succeeded" in output, "0 Error(s)"
    Expected Result: Clean build
    Evidence: .sisyphus/evidence/task-1-build.txt
  ```

  **Commit**: YES (groups with Task 2)
  - Message: `feat(domain): add PaymentIntentId to Order entity`
  - Files: `Ecommerce.Core/Entities/Order.cs`, `Ecommerce.Infrastructure/Migrations/*`

---

- [x] 2. Add `ConfirmPaymentByIntentIdAsync` to `IOrderService` and implement in `OrderService`

  **What to do**:
  - Open `Ecommerce.Application/Services/Interfaces/IOrderService.cs`
  - Add method signature: `Task ConfirmPaymentByIntentIdAsync(string paymentIntentId);`
  - Open `Ecommerce.Application/Services/Implementations/OrderService.cs`
  - Implement the method:
    1. Query `_orderRepository` for an order where `PaymentIntentId == paymentIntentId` (use `ListAllAsync()` then LINQ `.FirstOrDefault()`, or add a dedicated repo query if `IOrderRepository` supports it)
    2. If order found, set `order.Status = OrderStatus.processing` (or `paid` — use whatever enum value exists for confirmed payment; check `Ecommerce.Core/Enums/OrderStatus.cs`)
    3. Call `_orderRepository.UpdateAsync(order)` and `_unitOfWork.SaveChangesAsync()`
    4. If not found, log a warning — do not throw (webhook may arrive before order is committed in rare edge cases)
  - Verify `OrderStatus` enum values by reading `Ecommerce.Core/Enums/OrderStatus.cs` first

  **Must NOT do**:
  - Do NOT add a new repository method if `ListAllAsync()` + LINQ is sufficient
  - Do NOT change existing `UpdateOrderStatusAsync` method
  - Do NOT change existing `GetAllOrdersAsync` method

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Adding one interface method and its implementation — low complexity
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO — depends on Task 1 (needs PaymentIntentId on Order entity to compile)
  - **Parallel Group**: Wave 1 (sequential after Task 1)
  - **Blocks**: Task 4
  - **Blocked By**: Task 1

  **References**:

  **Pattern References**:
  - `Ecommerce.Application/Services/Implementations/OrderService.cs:195-203` — `UpdateOrderStatusAsync` — follow exact same pattern (GetById, set property, UpdateAsync, SaveChangesAsync)
  - `Ecommerce.Application/Services/Implementations/OrderService.cs:187-192` — `GetAllOrdersAsync` — shows how `ListAllAsync()` is used
  - `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — add method following same signature conventions

  **API/Type References**:
  - `Ecommerce.Core/Enums/OrderStatus.cs` — READ THIS FIRST to find the correct enum value for "payment confirmed/paid"

  **Acceptance Criteria**:
  - [ ] `IOrderService` has `Task ConfirmPaymentByIntentIdAsync(string paymentIntentId);`
  - [ ] `OrderService` has a non-stub implementation
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Interface and implementation compile
    Tool: Bash
    Steps:
      1. Run: dotnet build from solution root
      2. Assert: "Build succeeded", "0 Error(s)"
    Expected Result: Clean build with new method
    Evidence: .sisyphus/evidence/task-2-build.txt
  ```

  **Commit**: YES (groups with Task 1)
  - Message: `feat(domain): add PaymentIntentId to Order entity`
  - Files: `Ecommerce.Application/Services/Interfaces/IOrderService.cs`, `Ecommerce.Application/Services/Implementations/OrderService.cs`

---

- [x] 3. Add `Stripe:WebhookSecret` to appsettings and configure raw body middleware in `Program.cs`

  **What to do**:
  - Open `Ecoomerce.Web/appsettings.json`
  - Add `"WebhookSecret": "whsec_REPLACE_WITH_REAL_SECRET"` inside the existing `"Stripe"` object (alongside PublishableKey and SecretKey)
  - Open `Ecoomerce.Web/appsettings.Development.json` (create if it doesn't exist) and add the same placeholder
  - Open `Ecoomerce.Web/Program.cs`
  - BEFORE `app.UseRouting()` or `app.UseAuthentication()`, add raw body buffering for the webhook route:
    ```csharp
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/Payment/Webhook"))
        {
            context.Request.EnableBuffering();
            var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            context.Items["RawBody"] = body;
        }
        await next();
    });
    ```
  - Do NOT use `EnableBuffering()` globally — only for the webhook path

  **Must NOT do**:
  - Do NOT commit real Stripe secrets — use `whsec_REPLACE_WITH_REAL_SECRET` placeholder
  - Do NOT enable raw body buffering for all routes
  - Do NOT change any existing middleware order

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Config key addition + simple middleware registration
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (parallel with Task 1)
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/appsettings.json:9-12` — existing `"Stripe"` object — add `WebhookSecret` key here
  - `Ecoomerce.Web/Program.cs` — find where `app.Use*` middleware calls are registered and insert before routing

  **External References**:
  - Stripe docs: raw body requirement — https://stripe.com/docs/webhooks#verify-official-libraries

  **Acceptance Criteria**:
  - [ ] `appsettings.json` `Stripe` section has `WebhookSecret` key
  - [ ] `Program.cs` has raw body middleware that activates only for `/Payment/Webhook`
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Build succeeds with new config
    Tool: Bash
    Steps:
      1. Run: dotnet build from solution root
      2. Assert: "Build succeeded", "0 Error(s)"
    Expected Result: Clean build
    Evidence: .sisyphus/evidence/task-3-build.txt
  ```

  **Commit**: NO (groups with Task 4)

---

- [x] 4. Implement `PaymentController` — `Index`, `Success`, `Cancel`, `Webhook`

  **What to do**:
  - Open `Ecoomerce.Web/Controllers/PaymentController.cs` (currently 14-line stub)
  - Inject: `IOrderService _orderService`, `IConfiguration _configuration`, `ILogger<PaymentController> _logger`
  - **`[HttpGet] Index(int orderId)`** — `[Authorize]` — load order details, display payment options (Stripe card). Pass `orderId` and `Stripe:PublishableKey` to view via `ViewBag`. Return `Views/Payment/Index.cshtml` (create this simple view too — Stripe Elements form or redirect to Stripe Checkout).
  - **`[HttpGet] Success(string sessionId, int orderId)`** — `[Authorize]` — show order success confirmation. Return `Views/Payment/Success.cshtml` (simple success page with order summary link).
  - **`[HttpGet] Cancel(int orderId)`** — `[Authorize]` — show cancellation message with retry link. Return `Views/Payment/Cancel.cshtml`.
  - **`[HttpPost] Webhook()`** — `[AllowAnonymous]`, `[IgnoreAntiforgeryToken]` — Stripe webhook handler:
    1. Read raw body from `HttpContext.Items["RawBody"]` as string (set by middleware in Task 3)
    2. Read `Request.Headers["Stripe-Signature"]`
    3. Read `_configuration["Stripe:WebhookSecret"]`
    4. Call `StripeClient` / `EventUtility.ConstructEvent(json, stripeSignature, webhookSecret)` — wrap in try/catch `StripeException`, return `BadRequest()` on failure
    5. Handle `payment_intent.succeeded` event: extract `PaymentIntent.Id`, call `_orderService.ConfirmPaymentByIntentIdAsync(paymentIntentId)`
    6. Return `Ok()`
  - Create 3 simple Razor views: `Views/Payment/Index.cshtml`, `Views/Payment/Success.cshtml`, `Views/Payment/Cancel.cshtml`
  - Check that `Stripe.net` NuGet package is already installed — if not, add it: `dotnet add Ecoomerce.Web package Stripe.net`

  **Must NOT do**:
  - Do NOT implement PayPal — out of scope
  - Do NOT buffer raw body in the action itself (Task 3 handles this via middleware)
  - Do NOT add `[ValidateAntiForgeryToken]` to the Webhook action
  - Do NOT require a real `WebhookSecret` value — gracefully handle missing/placeholder config

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Multi-action controller with external SDK integration (Stripe.net), security-sensitive HMAC verification, and 3 supporting views
  - **Skills**: []
  - **Skills Evaluated but Omitted**:
    - `frontend-ui-ux`: Views are intentionally simple — no complex UI design needed

  **Parallelization**:
  - **Can Run In Parallel**: NO — depends on Tasks 1, 2, 3
  - **Parallel Group**: Wave 2 (first task in wave after 1–3 complete)
  - **Blocks**: F1–F4
  - **Blocked By**: Tasks 1, 2, 3

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Controllers/CartController.cs` or `CheckoutController.cs` — constructor injection pattern, `[Authorize]` usage, how IConfiguration is used for Stripe keys
  - `Ecoomerce.Web/Views/Order/` — look at any existing success/confirmation view for styling reference
  - `Ecoomerce.Web/Program.cs` — verify Stripe NuGet is configured (look for `builder.Services.AddStripeInfrastructure` or similar)

  **API/Type References**:
  - `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — `ConfirmPaymentByIntentIdAsync` (added in Task 2)
  - `Ecommerce.Core/Enums/OrderStatus.cs` — verify status values

  **External References**:
  - Stripe webhook verification: https://stripe.com/docs/webhooks/signatures
  - `EventUtility.ConstructEvent()` signature in `Stripe.net`

  **Acceptance Criteria**:
  - [ ] `PaymentController` has 4 actions: `Index`, `Success`, `Cancel`, `Webhook`
  - [ ] `Webhook` action has `[AllowAnonymous]` and `[IgnoreAntiforgeryToken]`
  - [ ] 3 views exist: `Views/Payment/Index.cshtml`, `Views/Payment/Success.cshtml`, `Views/Payment/Cancel.cshtml`
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Webhook returns 400 for missing Stripe-Signature header
    Tool: Bash (curl)
    Preconditions: App running on localhost:5000
    Steps:
      1. curl -X POST http://localhost:5000/Payment/Webhook -H "Content-Type: application/json" -d '{"type":"payment_intent.succeeded"}'
      2. Assert: HTTP 400 response (not 404, not 500)
    Expected Result: 400 Bad Request (signature verification failed gracefully)
    Failure Indicators: 404 means route missing; 500 means unhandled exception
    Evidence: .sisyphus/evidence/task-4-webhook-400.txt

  Scenario: Webhook route is reachable (not 404)
    Tool: Bash (curl)
    Steps:
      1. curl -I -X POST http://localhost:5000/Payment/Webhook
      2. Assert: response is NOT 404
    Expected Result: 400 or 200 (depending on header presence)
    Evidence: .sisyphus/evidence/task-4-webhook-route.txt

  Scenario: Payment Index page loads for authenticated user
    Tool: Playwright
    Preconditions: User logged in, valid orderId exists
    Steps:
      1. Navigate to http://localhost:5000/Payment/Index?orderId=1
      2. Assert: page renders without 500 error
      3. Assert: page title or h1 contains "Payment"
    Expected Result: Payment form/options visible
    Evidence: .sisyphus/evidence/task-4-payment-index.png
  ```

  **Commit**: YES
  - Message: `feat(payment): implement Stripe webhook endpoint and PaymentController`
  - Files: `Ecoomerce.Web/Controllers/PaymentController.cs`, `Ecoomerce.Web/Views/Payment/*`, `Ecoomerce.Web/appsettings.json`, `Ecoomerce.Web/Program.cs`, migration files

---

- [x] 5. Inject `IRecommendationService` into `ProductController` and use it in `Details`

  **What to do**:
  - Open `Ecoomerce.Web/Controllers/ProductController.cs`
  - In the constructor, add `IRecommendationService recommendationService` parameter and assign to `private readonly IRecommendationService _recommendationService;`
  - In `Details(int id)` action, replace lines 158–160 (the `SearchPagedAsync` call for recommended products):
    ```csharp
    // BEFORE (remove this):
    var (recommendedProducts, _) = await _productService.SearchPagedAsync(
        null, product.CategoryID, null, null, null, null, 1, 4);
    var filteredRecommended = recommendedProducts.Where(p => p.ProductID != id).ToList();

    // AFTER (replace with this):
    var recommendedProducts = (await _recommendationService.GetRelatedProductsAsync(id, 6))
        .Where(p => p.ProductID != id).Take(4).ToList();
    ```
  - Update `viewModel.RecommendedProducts = recommendedProducts;` — variable name is the same so this may require no change
  - Add `using Ecommerce.Application.Services.Interfaces;` if not already present

  **Must NOT do**:
  - Do NOT remove `IWishlistService` from `ProductController` — it is correctly used for `IsProductInWishlistAsync`
  - Do NOT change `GetFrequentlyBoughtTogetherAsync` — the Details view only has one recommendations slot (`RecommendedProducts`), keep it simple
  - Do NOT change the view (`Views/Product/Details.cshtml`)

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Single-file constructor injection + 3-line change in one action
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES — no dependencies on Wave 1 tasks
  - **Parallel Group**: Wave 2 (with Tasks 6, 7, 8, 9)
  - **Blocks**: F1–F4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Controllers/ProductController.cs:1-50` — existing constructor and field declarations — follow exact same private field + constructor injection pattern
  - `Ecommerce.Application/Services/Interfaces/IRecommendationService.cs` — read method signatures before editing

  **Acceptance Criteria**:
  - [ ] `ProductController` constructor has `IRecommendationService` parameter
  - [ ] `Details` action calls `_recommendationService.GetRelatedProductsAsync(id, 6)`
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Product Details page renders with recommendations
    Tool: Playwright
    Preconditions: App running, at least 2 products in DB
    Steps:
      1. Navigate to http://localhost:5000/Product/Details/1
      2. Assert: page renders (no 500 error)
      3. Assert: recommendations section exists — look for CSS selector matching existing recommendations section in Details.cshtml
    Expected Result: Details page loads, recommendations section present
    Evidence: .sisyphus/evidence/task-5-recommendations.png

  Scenario: Build succeeds
    Tool: Bash
    Steps:
      1. dotnet build
      2. Assert: "0 Error(s)"
    Evidence: .sisyphus/evidence/task-5-build.txt
  ```

  **Commit**: YES (groups with Tasks 6, 7, 8, 9)
  - Message: `feat(features): wire recommendations, wishlist redirect, dashboard, order management`
  - Files: `Ecoomerce.Web/Controllers/ProductController.cs`

---

- [x] 6. Fix root `WishlistController` stub — redirect to Profile area

  **What to do**:
  - Open `Ecoomerce.Web/Controllers/WishlistController.cs` (14-line stub)
  - Replace the `Index()` action body with a redirect to the Profile area wishlist:
    ```csharp
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Wishlist", new { area = "Profile" });
    }
    ```
  - Keep `[Authorize]` attribute on the class
  - The file can remain — it acts as a URL alias (e.g., `/Wishlist` → `Profile/Wishlist/Index`)

  **Must NOT do**:
  - Do NOT inject any services — this is a pure redirect stub
  - Do NOT touch `Areas/Profile/Controllers/WishlistController.cs` — it is complete and correct

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: One-line body change in an existing stub
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocks**: F1–F4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Controllers/WishlistController.cs` — current stub (14 lines) — only change the Index body

  **Acceptance Criteria**:
  - [ ] `GET /Wishlist` redirects to `GET /Profile/Wishlist` (302)
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: /Wishlist redirects to Profile area wishlist
    Tool: Bash (curl)
    Preconditions: App running, user cookie available (or test with -L to follow redirects)
    Steps:
      1. curl -I http://localhost:5000/Wishlist (unauthenticated)
      2. Assert: response is 302 redirect to /Account/Login (auth kicks in first) OR 302 to /Profile/Wishlist
      3. Alternatively: curl -I -c cookies.txt -b cookies.txt http://localhost:5000/Wishlist (with valid auth cookie)
    Expected Result: Redirect chain leads to Profile area wishlist page
    Failure Indicators: 404 means action is missing
    Evidence: .sisyphus/evidence/task-6-wishlist-redirect.txt
  ```

  **Commit**: YES (groups with Tasks 5, 7, 8, 9)

---

- [x] 7. Add `[Authorize(Roles="Admin")]` to the 3 Reporting area controllers

  **What to do**:
  - Open `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs`
    - Add `[Authorize(Roles = "Admin")]` attribute on the class (immediately below `[Area("Reporting")]`)
  - Open `Ecoomerce.Web/Areas/Reporting/Controllers/InventoryController.cs`
    - Add `[Authorize(Roles = "Admin")]` attribute on the class
  - Open `Ecoomerce.Web/Areas/Reporting/Controllers/UserController.cs`
    - Add `[Authorize(Roles = "Admin")]` attribute on the class
  - Verify `using Microsoft.AspNetCore.Authorization;` is present in each file (add if missing)

  **Must NOT do**:
  - Do NOT change any action methods — auth attribute on class covers all actions
  - Do NOT touch any other controller files

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Attribute addition to 3 files — mechanical, no logic
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocks**: Tasks 10–18 (reporting sub-views must not be accessible to unauthenticated users)
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Areas/Admin/Controllers/DashboardController.cs` (or any Admin controller) — check how `[Authorize(Roles = "Admin")]` is applied there and replicate exactly
  - `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs:1-15` — class declaration, add attribute here

  **Acceptance Criteria**:
  - [ ] All 3 reporting controllers have `[Authorize(Roles = "Admin")]` at class level
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Unauthenticated access to Reporting is blocked
    Tool: Bash (curl)
    Preconditions: App running, no auth cookies
    Steps:
      1. curl -I http://localhost:5000/Reporting/Sales
      2. Assert: HTTP 302 redirect to /Account/Login (not 200)
      3. Repeat for /Reporting/Inventory and /Reporting/User
    Expected Result: All 3 redirect to login
    Failure Indicators: 200 means auth is not working
    Evidence: .sisyphus/evidence/task-7-auth-redirect.txt
  ```

  **Commit**: YES (groups with Tasks 5, 6, 8, 9)

---

- [x] 8. Wire `Profile/DashboardController` with real data

  **What to do**:
  - Open `Ecoomerce.Web/Areas/Profile/Controllers/DashboardController.cs` (currently a stub — `Index()` returns `View()`)
  - Inject: `IOrderService _orderService`, `IWishlistService _wishlistService`, `UserManager<ApplicationUser> _userManager`
  - In `Index()`:
    1. Get `userId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)`
    2. Fetch recent orders: `var orders = await _orderService.GetUserOrdersAsync(userId)` — take last 5
    3. Fetch wishlist: `var wishlist = await _wishlistService.GetWishlistAsync(userId)` — get item count
    4. Fetch user: `var user = await _userManager.GetUserAsync(User)` — compute profile completeness (e.g., % of optional fields filled: FirstName, LastName, PhoneNumber, ProfilePicture)
    5. Build a `ProfileDashboardViewModel` (create it if it doesn't exist, or check `Ecommerce.Application/ViewModels/` for an existing one)
    6. Return `View(viewModel)`
  - Create/update `Areas/Profile/Views/Dashboard/Index.cshtml` to display:
    - Welcome header: "Welcome back, [FirstName]!"
    - Summary cards: Total Orders, Wishlist Items, Profile Completion %
    - Recent Orders table (last 5): OrderId, Date, Status, Total
    - Quick links to Order History, Address Book, Profile Settings

  **Must NOT do**:
  - Do NOT fetch ALL orders into memory for counting — use `GetUserOrdersAsync` and call `.Count()` in memory (it's per-user, not global)
  - Do NOT create a new service — use existing `IOrderService` and `IWishlistService`
  - Do NOT add pagination to the dashboard — just show last 5 orders

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Multi-service data aggregation + view model creation + Razor view implementation
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Dashboard view needs proper Bootstrap 5 card layout matching existing Profile area style

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocks**: F1–F4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Areas/Profile/Controllers/ManageProfileController.cs` — constructor injection pattern, `UserManager` usage, `ClaimTypes.NameIdentifier` for userId
  - `Ecoomerce.Web/Areas/Profile/Controllers/OrderHistoryController.cs` — how `GetUserOrdersAsync` is called
  - `Ecoomerce.Web/Areas/Profile/Views/ManageProfile/Index.cshtml` — visual style reference for Profile area views
  - `Ecoomerce.Web/Areas/Profile/Views/Dashboard/Index.cshtml` — existing stub to be replaced with real content

  **API/Type References**:
  - `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — `GetUserOrdersAsync(string userId)` returns `IEnumerable<OrderDto>`
  - `Ecommerce.Application/Services/Interfaces/IWishlistService.cs` — `GetWishlistAsync(string userId)` returns `WishlistDto`
  - `Ecommerce.Application/DTOs/Wishlist/WishlistDto.cs` — check what properties exist (especially `Items` collection)
  - `Ecommerce.Application/ViewModels/` — check if `ProfileDashboardViewModel` already exists before creating

  **Acceptance Criteria**:
  - [ ] `DashboardController.Index` fetches real data (not hardcoded)
  - [ ] Dashboard view displays order count, wishlist count, recent orders table
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Dashboard loads with real data for logged-in user
    Tool: Playwright
    Preconditions: App running, user logged in (use test credentials)
    Steps:
      1. Navigate to http://localhost:5000/Profile/Dashboard
      2. Assert: page title or h1 contains "Dashboard" or "Welcome"
      3. Assert: at least one summary card is visible (CSS: .card or similar)
      4. Assert: no "0 Error(s)" means build is clean — page renders without exceptions
    Expected Result: Dashboard page renders with data
    Failure Indicators: 500 error, blank page, or hardcoded "0" for all counts
    Evidence: .sisyphus/evidence/task-8-dashboard.png

  Scenario: Dashboard redirects unauthenticated users to login
    Tool: Bash (curl)
    Steps:
      1. curl -I http://localhost:5000/Profile/Dashboard (no auth)
      2. Assert: 302 redirect to /Account/Login
    Expected Result: Auth protection working
    Evidence: .sisyphus/evidence/task-8-dashboard-auth.txt
  ```

  **Commit**: YES (groups with Tasks 5, 6, 7, 9)

---

- [x] 9. Wire `Admin/OrderManagementController` — Index, UpdateStatus, BulkUpdateStatus

  **What to do**:
  - Open `Ecoomerce.Web/Areas/Admin/Controllers/OrderManagementController.cs`
  - **`Index(string? statusFilter, string? searchTerm, int page = 1)` (GET)**:
    1. Uncomment the `GetAllOrdersAsync()` call
    2. Add in-memory filtering: if `statusFilter` is provided, filter by `OrderStatus` enum value; if `searchTerm` provided, filter by orderId or userId contains
    3. Add in-memory pagination: `pageSize = 20`, compute `totalPages`, slice with `.Skip().Take()`
    4. Pass `statusFilter`, `searchTerm`, `currentPage`, `totalPages` to the view via ViewModel or ViewBag
  - **`UpdateStatus(int orderId, OrderStatus newStatus)` (POST)**:
    - `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Authorize(Roles="Admin")]`
    - Call `_orderService.UpdateOrderStatusAsync(orderId, newStatus)`
    - Redirect to `Index` with success TempData message
  - **`BulkUpdateStatus(int[] orderIds, OrderStatus newStatus)` (POST)**:
    - `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Authorize(Roles="Admin")]`
    - Loop: for each orderId, call `_orderService.UpdateOrderStatusAsync(orderId, newStatus)`
    - Redirect to `Index` with count in TempData message
  - Update `Areas/Admin/Views/OrderManagement/Index.cshtml` to:
    - Add filter form (status dropdown, search input, submit)
    - Add pagination controls
    - Add a status update dropdown + submit button per row
    - Add bulk-select checkboxes + bulk action form

  **Must NOT do**:
  - Do NOT add a new repository query for filtering — do it in-memory (the order list is per admin, not per user, but `GetAllOrdersAsync` uses `ListAllAsync()` — acceptable for now)
  - Do NOT add any new services or repositories
  - Do NOT implement order deletion — only status updates

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Three action methods + view update with filter/pagination/bulk UI — moderate complexity
  - **Skills**: [`frontend-ui-ux`]
    - `frontend-ui-ux`: Admin table needs proper Bootstrap 5 table with checkboxes, dropdowns, pagination

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2
  - **Blocks**: F1–F4
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `Ecoomerce.Web/Areas/Admin/Controllers/OrderManagementController.cs` — current 45-line stub — the commented-out code is the starting point
  - `Ecoomerce.Web/Areas/Admin/Controllers/` — any other Admin controller for `[Authorize(Roles="Admin")]` pattern, TempData usage
  - `Ecoomerce.Web/Areas/Admin/Views/OrderManagement/Index.cshtml` — existing view to augment (not replace from scratch)
  - `Ecoomerce.Web/Areas/Profile/Controllers/OrderHistoryController.cs` — how `IOrderService` is used

  **API/Type References**:
  - `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — `GetAllOrdersAsync()`, `UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)`
  - `Ecommerce.Core/Enums/OrderStatus.cs` — all valid status values for dropdown
  - `Ecommerce.Application/ViewModels/OrderManagement/ManageOrdersViewModel.cs` — check existing shape before adding properties

  **Acceptance Criteria**:
  - [ ] `Index` action loads and displays orders (not empty list)
  - [ ] `UpdateStatus` POST action changes order status and redirects
  - [ ] `BulkUpdateStatus` POST action processes multiple orders
  - [ ] Filter by status works
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: Admin order list loads with data
    Tool: Playwright
    Preconditions: App running, admin user logged in, at least 1 order in DB
    Steps:
      1. Navigate to http://localhost:5000/Admin/OrderManagement
      2. Assert: page renders (not 500)
      3. Assert: orders table is visible (CSS: table or .order-list)
      4. Assert: at least 1 row present
    Expected Result: Order list displayed
    Evidence: .sisyphus/evidence/task-9-order-list.png

  Scenario: Status filter works
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Admin/OrderManagement?statusFilter=pending
      2. Assert: all visible rows show "Pending" status
    Expected Result: Filtered list
    Evidence: .sisyphus/evidence/task-9-status-filter.png

  Scenario: UpdateStatus changes order status
    Tool: Playwright
    Steps:
      1. On order list, find first order row
      2. Select a new status from dropdown
      3. Submit the update form
      4. Assert: page reloads, success TempData message visible
      5. Assert: order row shows updated status
    Expected Result: Status updated and confirmed
    Evidence: .sisyphus/evidence/task-9-update-status.png
  ```

  **Commit**: YES (groups with Tasks 5, 6, 7, 8)
  - Message: `feat(features): wire recommendations, wishlist redirect, dashboard, order management`
  - Files: `Ecoomerce.Web/Controllers/ProductController.cs`, `Ecoomerce.Web/Controllers/WishlistController.cs`, `Ecoomerce.Web/Areas/Reporting/Controllers/*.cs`, `Ecoomerce.Web/Areas/Profile/Controllers/DashboardController.cs`, `Ecoomerce.Web/Areas/Admin/Controllers/OrderManagementController.cs`, related views

---

- [x] 10. Create `Areas/Reporting/Views/Sales/DailySales.cshtml`

  **What to do**:
  - Read `SalesController.DailySales` action to understand the ViewModel/data it passes
  - Read `Areas/Reporting/Views/Sales/Index.cshtml` for layout/style reference
  - Create `Areas/Reporting/Views/Sales/DailySales.cshtml`:
    - Include `_ReportingNavigation` partial: `<partial name="_ReportingNavigation" />`
    - Display daily sales data in a Bootstrap 5 table: columns — Date, Orders Count, Revenue, Avg Order Value
    - Date range filter form at top (from/to date inputs)
    - Summary row at bottom (totals)
    - Page title: "Daily Sales Report"
  - Match Bootstrap 5 card + table styling from `Index.cshtml`

  **Must NOT do**:
  - Do NOT add Chart.js or any new JavaScript library not already in the project
  - Do NOT change `SalesController.DailySales` action
  - Do NOT add a new ViewModel — use what the action already passes

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: Razor view with Bootstrap 5 table, consistent with existing reporting style
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — parallel with Tasks 11–18
  - **Parallel Group**: Wave 3
  - **Blocks**: F1–F4
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Sales/Index.cshtml` — style and layout reference
  - `Ecoomerce.Web/Areas/Reporting/Views/Shared/_ReportingNavigation.cshtml` — must be included
  - `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs` — `DailySales` action — read the return type and data shape

  **Acceptance Criteria**:
  - [ ] File exists at correct path
  - [ ] Renders without exceptions when accessed by Admin user
  - [ ] Includes `_ReportingNavigation` partial
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: DailySales view renders for Admin
    Tool: Playwright
    Preconditions: App running, admin logged in
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Sales/DailySales
      2. Assert: HTTP 200 (not 500, not 404)
      3. Assert: page contains "Daily Sales" text
      4. Assert: nav partial visible (look for reporting navigation links)
    Expected Result: View renders correctly
    Evidence: .sisyphus/evidence/task-10-daily-sales.png
  ```

  **Commit**: YES (groups with Tasks 11–18)
  - Message: `feat(reporting): add all reporting sub-views with admin auth`

---

- [x] 11. Create `Areas/Reporting/Views/Sales/TopProducts.cshtml`

  **What to do**:
  - Read `SalesController.TopProducts` action to understand the ViewModel
  - Create `Areas/Reporting/Views/Sales/TopProducts.cshtml`:
    - Include `_ReportingNavigation` partial
    - Display top-selling products table: columns — Rank, Product Name, Units Sold, Revenue, Category
    - Top-N count selector (dropdown: Top 10, 20, 50)
    - Page title: "Top Selling Products"
  - Match `Index.cshtml` Bootstrap 5 style

  **Must NOT do**:
  - Do NOT change `SalesController.TopProducts`
  - Do NOT add new JavaScript libraries

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — parallel with Tasks 10, 12–18
  - **Parallel Group**: Wave 3
  - **Blocks**: F1–F4
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Sales/Index.cshtml` — style reference
  - `Ecoomerce.Web/Areas/Reporting/Views/Shared/_ReportingNavigation.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs` — `TopProducts` action

  **Acceptance Criteria**:
  - [ ] File exists, renders without exception, includes nav partial
  - [ ] `dotnet build` exits with 0 errors

  **QA Scenarios**:
  ```
  Scenario: TopProducts view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Sales/TopProducts
      2. Assert: 200, page contains "Top" text, nav partial visible
    Evidence: .sisyphus/evidence/task-11-top-products.png
  ```

  **Commit**: YES (groups with Tasks 10, 12–18)

---

- [x] 12. Create `Areas/Reporting/Views/Sales/Revenue.cshtml`

  **What to do**:
  - Read `SalesController.Revenue` action for ViewModel
  - Create `Areas/Reporting/Views/Sales/Revenue.cshtml`:
    - Include `_ReportingNavigation` partial
    - Revenue summary: Total Revenue, Average Order Value, Revenue by Period (table with Period, Revenue, Orders, Avg)
    - Period filter: monthly/weekly/yearly dropdown
    - Page title: "Revenue Report"

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Sales/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/SalesController.cs` — `Revenue` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: Revenue view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Sales/Revenue
      2. Assert: 200, page contains "Revenue", nav partial visible
    Evidence: .sisyphus/evidence/task-12-revenue.png
  ```

  **Commit**: YES (groups with Tasks 10, 11, 13–18)

---

- [x] 13. Create `Areas/Reporting/Views/Inventory/LowStock.cshtml`

  **What to do**:
  - Read `InventoryController.LowStock` action for ViewModel
  - Create `Areas/Reporting/Views/Inventory/LowStock.cshtml`:
    - Include `_ReportingNavigation` partial
    - Low stock products table: columns — Product Name, SKU/ID, Current Stock, Reorder Threshold, Category
    - Warning badge on rows where stock < 5 (use Bootstrap `badge bg-warning`)
    - Page title: "Low Stock Alert"

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Inventory/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/InventoryController.cs` — `LowStock` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: LowStock view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Inventory/LowStock
      2. Assert: 200, page contains "Low Stock", nav partial visible
    Evidence: .sisyphus/evidence/task-13-low-stock.png
  ```

  **Commit**: YES (groups with Tasks 10–12, 14–18)

---

- [x] 14. Create `Areas/Reporting/Views/Inventory/OutOfStock.cshtml`

  **What to do**:
  - Read `InventoryController.OutOfStock` action for ViewModel
  - Create view with out-of-stock products table: Product Name, ID, Category, Last Updated, Action (restock link)
  - Include `_ReportingNavigation` partial
  - Use Bootstrap `badge bg-danger` for "Out of Stock" status labels

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Inventory/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/InventoryController.cs` — `OutOfStock` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: OutOfStock view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Inventory/OutOfStock
      2. Assert: 200, page visible, nav partial present
    Evidence: .sisyphus/evidence/task-14-out-of-stock.png
  ```

  **Commit**: YES (groups with Tasks 10–13, 15–18)

---

- [x] 15. Create `Areas/Reporting/Views/Inventory/InventoryLogs.cshtml`

  **What to do**:
  - Read `InventoryController.InventoryLogs` action for ViewModel
  - Create view with inventory activity log table: Date, Product, Change Type (In/Out/Adjustment), Quantity Changed, New Stock Level, Changed By
  - Include `_ReportingNavigation` partial
  - Date range filter form

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/Inventory/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/InventoryController.cs` — `InventoryLogs` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: InventoryLogs view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/Inventory/InventoryLogs
      2. Assert: 200, page visible
    Evidence: .sisyphus/evidence/task-15-inventory-logs.png
  ```

  **Commit**: YES (groups with Tasks 10–14, 16–18)

---

- [x] 16. Create `Areas/Reporting/Views/User/Activity.cshtml`

  **What to do**:
  - Read `UserController.Activity` action for ViewModel
  - Create view with user activity log: Date, User Email, Action Type, Entity, Details
  - Include `_ReportingNavigation` partial
  - Filter by action type and date range

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/User/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/UserController.cs` — `Activity` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: Activity view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/User/Activity
      2. Assert: 200, page visible, nav partial present
    Evidence: .sisyphus/evidence/task-16-user-activity.png
  ```

  **Commit**: YES (groups with Tasks 10–15, 17–18)

---

- [x] 17. Create `Areas/Reporting/Views/User/RegistrationTrend.cshtml`

  **What to do**:
  - Read `UserController.RegistrationTrend` action for ViewModel
  - Create view with registration trend table: Period (day/week/month), New Registrations, Cumulative Total
  - Include `_ReportingNavigation` partial
  - Period selector (daily/weekly/monthly)

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/User/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/UserController.cs` — `RegistrationTrend` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: RegistrationTrend view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/User/RegistrationTrend
      2. Assert: 200, page visible
    Evidence: .sisyphus/evidence/task-17-registration-trend.png
  ```

  **Commit**: YES (groups with Tasks 10–16, 18)

---

- [x] 18. Create `Areas/Reporting/Views/User/TopCustomers.cshtml`

  **What to do**:
  - Read `UserController.TopCustomers` action for ViewModel
  - Create view with top customers table: Rank, Customer Name/Email, Total Orders, Total Spent, Last Order Date, Member Since
  - Include `_ReportingNavigation` partial
  - Top-N selector (Top 10/25/50)

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: [`frontend-ui-ux`]

  **Parallelization**:
  - **Can Run In Parallel**: YES — Wave 3
  - **Blocked By**: Task 7

  **References**:
  - `Ecoomerce.Web/Areas/Reporting/Views/User/Index.cshtml`
  - `Ecoomerce.Web/Areas/Reporting/Controllers/UserController.cs` — `TopCustomers` action

  **Acceptance Criteria**:
  - [ ] File exists, renders, includes nav partial, build passes

  **QA Scenarios**:
  ```
  Scenario: TopCustomers view renders
    Tool: Playwright
    Steps:
      1. Navigate to http://localhost:5000/Reporting/User/TopCustomers
      2. Assert: 200, page visible, nav partial present
    Evidence: .sisyphus/evidence/task-18-top-customers.png
  ```

  **Commit**: YES (groups with Tasks 10–17)
  - Message: `feat(reporting): add all reporting sub-views with admin auth`
  - Files: `Areas/Reporting/Views/Sales/*.cshtml`, `Areas/Reporting/Views/Inventory/*.cshtml`, `Areas/Reporting/Views/User/*.cshtml`, `Areas/Reporting/Controllers/*.cs`

---

## Final Verification Wave

- [x] F1. **Plan Compliance Audit** — `oracle`
  Read plan end-to-end. For each "Must Have": verify implementation exists (read file, curl endpoint, run command). For each "Must NOT Have": search codebase for forbidden patterns. Check evidence files exist in `.sisyphus/evidence/`. Compare deliverables list against plan.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [x] F2. **Build + Code Quality** — `unspecified-high`
  Run `dotnet build` from solution root. Run linter if configured. Review all changed files for: missing null checks, empty catch blocks, unused using directives, `var` where type is unclear. Check no AI slop: excessive comments, over-abstraction, generic variable names.
  Output: `Build [PASS/FAIL] | Files [N clean/N issues] | VERDICT`

- [x] F3. **Full QA Pass** — `unspecified-high` + `playwright` skill
  Start from clean state with app running. Execute EVERY QA scenario from EVERY task. Test cross-task integration. Save all evidence to `.sisyphus/evidence/final-qa/`.
  Output: `Scenarios [N/N pass] | Integration [N/N] | VERDICT`

- [x] F4. **Scope Fidelity Check** — `deep`
  For each task: read "What to do", read actual diff (git diff). Verify 1:1 — everything in spec was built, nothing beyond spec was built. Check "Must NOT do" compliance. Flag unaccounted changes.
  Output: `Tasks [N/N compliant] | Contamination [CLEAN/N issues] | VERDICT`

---

## Commit Strategy

- After Task 4: `feat(payment): implement Stripe webhook endpoint and PaymentController`
- After Tasks 5–9: `feat(features): wire recommendations, wishlist redirect, dashboard, order management`
- After Tasks 10–18: `feat(reporting): add all reporting sub-views with admin auth`
- Final: `chore(phase3): complete Phase 3 feature completion`

---

## Success Criteria

### Verification Commands
```bash
dotnet build          # Expected: Build succeeded, 0 Error(s)
dotnet ef migrations list  # Expected: latest migration for PaymentIntentId is Applied
curl -X POST http://localhost:5000/Payment/Webhook -H "Content-Type: application/json" -d '{}'  # Expected: 400 (missing Stripe-Signature) not 404
curl http://localhost:5000/Reporting/Sales  # Expected: 302 redirect to /Account/Login (auth working)
```

### Final Checklist
- [ ] All "Must Have" present
- [ ] All "Must NOT Have" absent
- [ ] `dotnet build` passes with 0 errors
- [ ] No unauthenticated access to Reporting area
- [ ] All 9 reporting sub-views render without exceptions
- [ ] Product Details page shows IRecommendationService results
- [ ] Profile Dashboard shows real data
- [ ] Admin can update order status
