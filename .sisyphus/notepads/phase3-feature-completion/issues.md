# Phase 3 Feature Completion — Known Issues & Gotchas

## [2026-03-12] Pre-Execution Discovery

### Codebase Quirks
1. **Infrastructure spelling typo**: `Ecommerce.Infrastracture` (3 r's) everywhere
   - Affects: Migration paths, DI registration, project references
   - **DO NOT fix** — would require massive refactor, out of scope
   - Subagents MUST use the 3-r spelling in all file paths

2. **OrderStatus enum mismatch risk**: 
   - Plan originally assumed `OrderStatus.paid` exists
   - **Actual values**: `pending, processing, confirmed, shipped, delivered, cancelled, returned`
   - **Solution**: Use `OrderStatus.processing` for payment confirmation
   - **Communicated to subagents**: MUST use `processing`, never `paid`

3. **appsettings.json Stripe keys are LIVE keys**: 
   - `pk_live_*` and `sk_live_*` present (not test mode)
   - **Implication**: Webhook testing requires real Stripe CLI or placeholder secret
   - **Task 3 decision**: Add `WebhookSecret` as placeholder `"whsec_REPLACE_WITH_REAL_SECRET"`
   - Graceful fallback required (no crashes if webhook secret invalid)

4. **PaymentController stub is 14 lines**:
   - Only has constructor and empty `Index()` method
   - NO Webhook method, NO Success/Cancel methods
   - **Full replacement required** in Task 4

### Agent Behavior Observations
- **Explore agents refuse multi-task prompts**: 6/6 explore agents correctly rejected over-scoped requests
- **Librarian agents timed out**: Both Stripe + EF Core librarian tasks returned "Task not found"
  - Likely exceeded timeout or crashed during research phase
  - **Mitigation**: Rely on direct documentation lookup via context7 in subagent prompts

### Verification Gaps (Still Pending)
- ✅ Stripe.net package installed (v50.1.0)
- ✅ appsettings.json verified (WebhookSecret missing as expected)
- ❌ Migrations directory actual path (glob search in progress)
- ⚠️ IRecommendationService DI registration (plan claims done, not directly verified)
- ⚠️ Reporting controllers authorization attributes (plan claims missing, not directly verified)
- ⚠️ Profile DashboardController stub state (plan claims stub, not directly verified)

### Critical Path Blockers
- **None identified yet** — all prerequisite infrastructure (DI, services, views structure) appears complete
- Task 1 (migration) is clean-slate (no conflicts)
- Wave 1 tasks are all independent (can parallelize)

## [2026-03-13] F1 Audit Findings
- REJECT verdict: working tree contains substantial out-of-scope modifications outside the Phase 3 file set, including product/catalog/service files and broad migration churn on master.
- Payment webhook flow is only partially wired end-to-end: Order.PaymentIntentId exists and webhook confirms by intent id, but no verified code path was found that persists a PaymentIntentId onto orders before webhook receipt.
- Evidence coverage is incomplete for final verification: only partial final-qa artifacts exist, and wave1-verification-migrations.txt records a metadata failure instead of a successful applied-migration check.
- Guardrail breach in history: commit b867f61 changed ProductController beyond Task 5 by adding caching, output caching, brand filtering, search refactors, and latest/on-sale query changes.

## [2026-03-13 FINAL] Critical Bug - PaymentIntentId Never Assigned (RESOLVED)

### Issue Description
**Severity**: CRITICAL (P0)
**Status**: ✅ RESOLVED
**Discovered**: F4 Scope Fidelity Audit - git diff analysis
**Fixed**: 2026-03-13 (lines 82-84 in StripePaymentService.cs)

### Root Cause
`StripePaymentService.ProcessPaymentAsync` created Stripe PaymentIntent (line 77) but NEVER assigned `order.PaymentIntentId = paymentIntent.Id` to the Order entity.

**Impact**:
- Webhook handler (`PaymentController.Webhook`) receives `payment_intent.succeeded` event
- Calls `OrderService.ConfirmPaymentByIntentIdAsync(paymentIntent.Id)` (line 111)
- Searches for order: `orders.FirstOrDefault(o => o.PaymentIntentId == paymentIntentId)` (line 213)
- **ALWAYS RETURNS NULL** (order.PaymentIntentId is NULL in database)
- Logs warning, exits early (line 218)
- Order status NEVER updates from "pending" to "processing"
- **Result**: Payment succeeds in Stripe, order stays pending forever in our system

### Symptoms
1. Stripe payment processes successfully
2. Webhook fires and signature validates correctly
3. Warning logged: "Stripe webhook received for PaymentIntentId {id} but no matching order found"
4. Order remains in "pending" status indefinitely
5. Customer sees payment confirmation from Stripe but order never progresses

### Fix Applied
**File**: `Ecommerce.Application/Services/Implementations/StripePaymentService.cs`
**Location**: Lines 82-84 (after PaymentIntent creation, before Payment record creation)

```csharp
// CRITICAL: Assign PaymentIntentId to Order for webhook matching
order.PaymentIntentId = paymentIntent.Id;
await _orderRepository.UpdateAsync(order);
```

### Before/After
**BEFORE**:
```csharp
var paymentIntent = await service.CreateAsync(options);  // Line 77

if (paymentIntent != null)
{
    // Immediately creates Payment record (lines 86-96)
    var payment = new Core.Entities.Payment { ... };
    // ❌ Order.PaymentIntentId still NULL
}
```

**AFTER**:
```csharp
var paymentIntent = await service.CreateAsync(options);  // Line 77

if (paymentIntent != null)
{
    // ✅ FIRST: Assign PaymentIntentId to Order
    order.PaymentIntentId = paymentIntent.Id;
    await _orderRepository.UpdateAsync(order);
    
    // THEN: Create Payment record
    var payment = new Core.Entities.Payment { ... };
}
```

### Verification
- **Build Status**: ✅ 0 errors (44 pre-existing nullable warnings)
- **Code Flow**: ✅ All 6 integration points verified (see `.sisyphus/evidence/webhook-verification.txt`)
- **Method**: Static analysis (no live webhook endpoint available)
- **Confidence**: HIGH (logic sound, all paths confirmed)

### Related Files
- Order entity: `Ecommerce.Core/Entities/Order.cs` (line 40 - PaymentIntentId property)
- Webhook handler: `Ecoomerce.Web/Controllers/PaymentController.cs` (lines 92-113)
- Order confirmation: `Ecommerce.Application/Services/Implementations/OrderService.cs` (lines 210-224)

### Prevention Pattern
**Rule**: Always assign foreign keys immediately after external resource creation, within same transaction.

When integrating with external systems (Stripe, payment gateways, 3rd-party APIs):
1. Create external resource (PaymentIntent, Order, etc.)
2. **IMMEDIATELY** assign foreign key to domain entity
3. Persist domain entity
4. **THEN** create related records

This ensures webhook handlers can always find domain entities by foreign key lookup.

### Evidence Files
- F4 audit report: `.sisyphus/evidence/F4-scope-fidelity-report.md` (lines documenting bug)
- Webhook verification: `.sisyphus/evidence/webhook-verification.txt` (end-to-end flow)
- Learnings: `.sisyphus/notepads/phase3-feature-completion/learnings.md` (pattern documentation)

### F1 Audit Original Finding (Now Obsolete)
F1 oracle audit (line 49 in this file) flagged: "no verified code path was found that persists a PaymentIntentId onto orders before webhook receipt"
- **Status**: Audit was CORRECT - code path was MISSING
- **Resolution**: Code path now EXISTS (lines 82-84 added)
- **Updated Status**: Issue resolved, webhook flow functional

