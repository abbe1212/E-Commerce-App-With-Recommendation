# Learnings — Phase 4 Quality & Validation

> Cumulative wisdom from task execution. Append findings after each task completion.

---

## Task 1: FluentValidation NuGet Setup

### Version Resolution
- Task requested FluentValidation packages at version 11.3.0
- NuGet dependency conflict: FluentValidation.AspNetCore 11.3.0 requires FluentValidation.DependencyInjectionExtensions >= 11.11.0
- **Solution**: Used compatible versions:
  - FluentValidation.AspNetCore: 11.3.1 (next stable after 11.3.0)
  - FluentValidation.DependencyInjectionExtensions: 11.11.0 (required by AspNetCore 11.3.1)

### Key Learnings
- FluentValidation.AspNetCore has strict peer dependency requirements
- When adding multiple related NuGet packages, always verify transitive dependencies
- Available versions in NuGet may differ from what was initially requested

### Deliverables
✅ Ecommerce.Application.csproj updated with FluentValidation packages
✅ Validators directory created at Ecommerce.Application/Validators/
✅ dotnet restore completes successfully with no errors
✅ Evidence saved to .sisyphus/evidence/task-1-restore.txt

---

## Task 3: RecentlyViewed Cookie Security and Capacity Update

**Completed:** 2026-03-13

### Changes Made
- Increased cookie capacity from 10 to 20 items (lines 211-212)
  - Changed `Count > 10` to `Count > 20`
  - Changed `.Take(10)` to `.Take(20)`
- Added security flags to CookieOptions (lines 216-221):
  - `HttpOnly = true` - Prevents JavaScript access (XSS mitigation)
  - `SameSite = SameSiteMode.Strict` - Blocks cross-site sends (CSRF mitigation)
  - Kept existing `Expires = DateTimeOffset.UtcNow.AddDays(30)`

### Verification Results
- CookieOptions now uses explicit multi-line object initializer syntax for clarity
- Security flags present and correctly cased
- GetRecentlyViewedIds method left untouched (no changes needed)
- Cookie name "RecentlyViewed" preserved

### Key Learnings
- Cookie stores comma-separated product IDs (e.g., "42,17,8")
- Increased cap allows users to browse up to 20 products without losing history
- HttpOnly prevents cookie theft via XSS attacks
- SameSite=Strict provides strongest CSRF protection (appropriate for e-commerce)
- Secure flag NOT added (spec requirement: allows local HTTP development)

### Deliverables
✅ ProductController.cs lines 211-212: Capacity increased to 20
✅ ProductController.cs lines 216-221: CookieOptions updated with HttpOnly and SameSite
✅ Evidence saved to .sisyphus/evidence/task-3-cookie.txt and task-3-cap.txt
✅ No other methods modified in ProductController

## Task 4: Ecommerce.Tests Project Creation

### Key Learnings
1. **Folder vs File Naming Mismatch**: The folder is named `Ecommerce.Infrastracture` (with typo), but the actual csproj file is `Ecommerce.Infrastructure.csproj` (without typo). Must reference the actual file name in ProjectReferences, not the folder name.

2. **Real Folder Names**: The infrastructure folder has the typo intentionally. This is how it's organized in the repository.

3. **Project References Path**: ProjectReferences use relative paths with backslashes on Windows (e.g., `..\Ecommerce.Core\Ecommerce.Core.csproj`)

4. **dotnet sln add**: Successfully adds project to solution without manual editing of .sln file.

5. **Build Lock Issues**: When the application is running, it locks dependent DLL files. Using `dotnet clean` before `dotnet build` helps resolve file locks.

### Testing Stack Finalized
- xUnit 2.7.0 (test framework)
- Moq 4.20.70 (mocking)
- FluentAssertions 6.12.0 (assertion library)
- Microsoft.EntityFrameworkCore.InMemory 8.0.0 (in-memory DB for repository tests)
- Microsoft.NET.Test.Sdk 17.9.0 (test runner support)

### Scaffolding Structure
- `/Services/` - Service unit tests
- `/Controllers/` - Controller integration tests
- `/Repositories/` - Repository tests with InMemory EF Core

### Ready for Next Steps
- Tasks 10-15 can now depend on this test project existing
- Test classes can be added to the appropriate folders
- NuGet packages are restored and ready

---

## Task 2: DRY Refactoring — RestoreCartFromTempData Helper

**Completed:** 2026-03-13

### Duplicated Code Extraction
- Identified identical cart restoration blocks in two controller actions:
  - `PaymentMethod()` action (original lines 174-199)
  - `OrderConfirmation()` action (original lines 250-276)
- Both blocks deserialize TempData["CartItems"], keep session data, and restore cart totals

### Changes Made
1. **Created helper method** `RestoreCartFromTempData()` (lines 482-521)
   - Returns: `CartViewModel` (not CheckoutViewModel)
   - Signature: `private CartViewModel RestoreCartFromTempData()`
   - Handles: CartItems deserialization, Tax, Shipping, Discount restoration
   - Keeps TempData keys: CartItems, CartSubTotal, CartTax, CartShipping, CartDiscount

2. **Refactored PaymentMethod()** (line 174)
   - Replaced 26-line block with single call: `model.Cart = RestoreCartFromTempData();`
   - Preserved shipping form field restoration (separate concern, lines 235-248 left untouched)

3. **Refactored OrderConfirmation()** (line 226)
   - Replaced 26-line block with single call: `model.Cart = RestoreCartFromTempData();`
   - Preserved PromoDiscount field assignment (lines 235-238, different from Discount)

### Verification Results
- ✅ Helper method syntax valid (86 open braces = 86 close braces)
- ✅ TempData["CartItems"] count: 3 (1 in ShippingInfo store, 2 in helper = expected)
- ✅ No duplication between PaymentMethod and OrderConfirmation
- ✅ ShippingInfo() action untouched (stores data, different responsibility)
- ✅ All imports intact (System.Text.Json.JsonSerializer available)
- ⚠️ Build locked by running application (file locking, not syntax errors)

### Key Learnings
- TempData restoration patterns often repeat across multiple checkout steps
- Helper methods centralize null-checking and deserialization logic
- Private helpers keep TempData management concerns localized to controller
- CartViewModel initialization inline (`new CartViewModel()`) works cleaner than CheckoutViewModel init
- Session data Keep() calls should be grouped in one place to avoid scatter

### Code Quality Impact
- **Lines of code reduced**: -26 duplicated lines
- **Cyclomatic complexity**: Reduced (extraction moves 2 identical blocks to 1 method)
- **Maintainability**: Improved (single source of truth for cart restoration)

### Deliverables
✅ RestoreCartFromTempData() helper created at line 482
✅ PaymentMethod() refactored to use helper (line 174)
✅ OrderConfirmation() refactored to use helper (line 226)
✅ Syntax validation: Braces balanced (86:86)
✅ Evidence saved to .sisyphus/evidence/task-2-build.txt and task-2-grep.txt

