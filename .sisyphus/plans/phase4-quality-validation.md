# Phase 4 — Quality & Validation Implementation Plan

## TL;DR

> **Quick Summary**: Adds safety nets to the ECommerce MVC app — FluentValidation across all entry points, DRY refactor of duplicated TempData logic, hardened cookie security, OAuth startup warnings, and a complete unit test suite built from scratch.
>
> **Deliverables**:
> - `Ecommerce.Application/Validators/` — 4 new FluentValidation validator classes
> - `Ecoomerce.Web/Program.cs` — FluentValidation registration + OAuth startup logging
> - `Ecoomerce.Web/Controllers/CheckoutController.cs` — `RestoreCartFromTempData()` helper extracted
> - `Ecoomerce.Web/Controllers/ProductController.cs` — cookie cap 10→20, HttpOnly+SameSite flags
> - `Ecommerce.Tests/` — brand-new test project with ≥23 passing unit tests
>
> **Estimated Effort**: Medium  
> **Parallel Execution**: YES — 3 waves  
> **Critical Path**: Task 1 (NuGet + csproj) → Task 3 (validators) → Task 6 (Program.cs registration) → Task 8 (test project) → Task 9-12 (test classes) → Final Verification

---

## Context

### Original Request
Implement Phase 4 as described in `phase4.resolved` — a brownfield ASP.NET Core 8 clean-architecture ECommerce MVC app.

### Architecture Baseline
- **Solution**: `ECommerceApp.sln`
- **Projects**: `Ecommerce.Core` → `Ecommerce.Application` → `Ecommerce.Infrastracture` → `Ecoomerce.Web` (note: both infra and web project names have typos — do NOT rename)
- **Existing NuGet in Application**: `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1`, `Stripe.net 50.1.0`
- **Existing NuGet in Web**: includes Google/Facebook OAuth packages
- **No test project exists** — Ecommerce.Tests must be created from scratch
- **PromoCodeDto namespace**: `Ecommerce.Application.DTOs.Promotion` (not `.DTOs`)

### Key Architecture Decisions (from audit)
- `PromoCodeRequest` (the AJAX request class for promo code) is a nested class inside `CheckoutController.cs` in the Web layer. Its validator must NOT live in Application (that would be a layer violation). The validator for `PromoCodeRequest` stays in the **Web project** at `Ecoomerce.Web/Validators/PromoCodeRequestValidator.cs`.
- `RegisterDto` currently has data annotations (`[Required]`, `[EmailAddress]`, `[StringLength]`, `[Compare]`). The plan **replaces** them: remove all data annotations from `RegisterDto.cs` and add a pure FluentValidation `RegisterDtoValidator`.
- `CheckoutViewModel` already has `[Required]` annotations. The `CheckoutViewModelValidator` will **co-exist** (not replace) — FV adds business rules (ShippingMethod whitelist) that data annotations can't express; the `[Required]` annotations stay.
- `ProductDto` is read-only (returned by the catalog) — do NOT add a validator for it. The spec says `CreateProductDtoValidator` validates **admin create/edit input**, which uses `CreateProductDto` (or the view model used in admin product creation). Use `ProductDto` fields as the shape but name the validator `CreateProductDtoValidator` and target the same DTO.

### Spec Source
`C:\Users\DUBAI\source\repos\ECommerceApp\phase4.resolved`

---

## Work Objectives

### Core Objective
Harden the application's input validation layer and establish a testing baseline — zero tests → ≥23 passing tests, zero FluentValidation → 4 validators covering all major entry points.

### Concrete Deliverables
- `Ecommerce.Application/Validators/CreateProductDtoValidator.cs`
- `Ecommerce.Application/Validators/CheckoutViewModelValidator.cs`
- `Ecommerce.Application/Validators/RegisterDtoValidator.cs`
- `Ecoomerce.Web/Validators/PromoCodeRequestValidator.cs` (Web layer — avoids layer violation)
- `Ecommerce.Tests/Ecommerce.Tests.csproj`
- `Ecommerce.Tests/Services/OrderServiceTests.cs`
- `Ecommerce.Tests/Services/CartServiceTests.cs`
- `Ecommerce.Tests/Services/PromoCodeServiceTests.cs`
- `Ecommerce.Tests/Controllers/CheckoutControllerTests.cs`
- `Ecommerce.Tests/Repositories/ProductRepositoryTests.cs`

### Definition of Done
- [ ] `dotnet build ECommerceApp.sln` → 0 errors, 0 warnings
- [ ] `dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj` → 0 failures, ≥23 passing tests
- [ ] Submitting empty admin product form triggers FV error messages (manual)
- [ ] Cookie "RecentlyViewed" shows `HttpOnly` flag in browser DevTools

### Must Have
- ≥23 tests, 0 failures
- FluentValidation auto-validation hooked into ModelState pipeline
- `RegisterDto` data annotations removed (replaced by FV, not duplicated)
- `RestoreCartFromTempData()` private helper used in both `PaymentMethod()` and `OrderConfirmation()`
- Cookie cap = 20, HttpOnly = true, SameSite = Strict
- Bootstrap logger for OAuth — NO `BuildServiceProvider()` anti-pattern

### Must NOT Have (Guardrails)
- **No layer violation**: `PromoCodeRequestValidator` must NOT go in `Ecommerce.Application` (that class lives in Web). Keep it in `Ecoomerce.Web/Validators/`.
- **No double registration**: Do NOT use both data annotations AND FluentValidation on `RegisterDto` — remove annotations when adding FV
- **No `BuildServiceProvider()`** in Program.cs for the startup logger — use `LoggerFactory.Create()` pattern
- **No EF Core migrations or schema changes** — tests use InMemory provider, not the production DB
- **No touching any other controller or service** — scope is exactly the 5 sub-tasks in `phase4.resolved`
- **No renaming of typo'd project folders** (`Ecoomerce.Web`, `Ecommerce.Infrastracture`) — they are load-bearing

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: NO (creating from scratch in this plan)
- **Automated tests**: YES (Tests-first for service unit tests; tests-after for controller/repo tests)
- **Framework**: xUnit 2.7.x + Moq 4.20.x + FluentAssertions 6.x
- **EF InMemory**: Per-test unique DB name via `Guid.NewGuid().ToString()`

### QA Policy
Every task includes agent-executed QA scenarios. Evidence saved to `.sisyphus/evidence/task-{N}-{scenario-slug}.{ext}`.

- **Build verification**: `dotnet build` — Bash
- **Test suite**: `dotnet test` — Bash
- **Validator behavior**: Manual controller/action invocation via unit test assertions
- **Cookie flags**: Read source code diff — Bash/grep

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — scaffolding + infrastructure):
├── Task 1: Add FluentValidation NuGet to Application.csproj [quick]
├── Task 2: Extract RestoreCartFromTempData() helper in CheckoutController [quick]
├── Task 3: Harden RecentlyViewed cookie in ProductController [quick]
└── Task 4: Create Ecommerce.Tests.csproj (project scaffold) [quick]

Wave 2 (After Wave 1 — validators + OAuth warning + test classes):
├── Task 5: Write RegisterDtoValidator (+ remove data annotations from RegisterDto) [quick]
├── Task 6: Write CreateProductDtoValidator [quick]
├── Task 7: Write CheckoutViewModelValidator [quick]
├── Task 8: Write PromoCodeRequestValidator (Web layer) [quick]
├── Task 9: Add OAuth startup bootstrap logger to Program.cs [quick]
├── Task 10: Write OrderServiceTests (4 tests) [unspecified-high]
├── Task 11: Write CartServiceTests (7 tests) [unspecified-high]
└── Task 12: Write PromoCodeServiceTests (5 tests) [unspecified-high]

Wave 3 (After Wave 2 — registration wiring + remaining tests + verification):
├── Task 13: Register FluentValidation in Program.cs + PromoCodeRequestValidator [quick]
├── Task 14: Write CheckoutControllerTests (3 tests) [unspecified-high]
└── Task 15: Write ProductRepositoryTests (4 tests, EF InMemory) [unspecified-high]

Wave FINAL (After ALL tasks — parallel review):
├── Task F1: Plan compliance audit [oracle]
├── Task F2: Build + test suite run [unspecified-high]
├── Task F3: Code quality review [unspecified-high]
└── Task F4: Scope fidelity check [deep]
```

### Dependency Matrix

| Task | Depends On | Blocks |
|------|-----------|--------|
| 1 | — | 5, 6, 7, 13 |
| 2 | — | F1 |
| 3 | — | F1 |
| 4 | — | 10, 11, 12, 14, 15 |
| 5 | 1 | 13 |
| 6 | 1 | 13 |
| 7 | 1 | 13 |
| 8 | — | 13 |
| 9 | — | F1 |
| 10 | 4 | F2 |
| 11 | 4 | F2 |
| 12 | 4 | F2 |
| 13 | 5, 6, 7, 8 | F1 |
| 14 | 4 | F2 |
| 15 | 4 | F2 |
| F1-F4 | 13, 14, 15 | — |

### Agent Dispatch Summary

- **Wave 1**: 4 × `quick`
- **Wave 2**: 5 × `quick`, 3 × `unspecified-high`
- **Wave 3**: 1 × `quick`, 2 × `unspecified-high`
- **Final**: 1 × `oracle`, 2 × `unspecified-high`, 1 × `deep`

---

## TODOs

---

- [x] 1. Add FluentValidation NuGet to `Ecommerce.Application.csproj`

  **What to do**:
  - Open `Ecommerce.Application/Ecommerce.Application.csproj`
  - Add: `<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />`
  - Add: `<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.3.0" />`
  - Run `dotnet restore` to confirm the packages resolve
  - Create the directory `Ecommerce.Application/Validators/` (it does not exist yet)

  **Must NOT do**:
  - Do NOT add FluentValidation to `Ecoomerce.Web.csproj` — it belongs in Application only (DI extensions are also registered via Application's assembly scan)
  - Do NOT change the TargetFramework or any other property in the .csproj

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2, 3, 4)
  - **Blocks**: Tasks 5, 6, 7
  - **Blocked By**: None

  **References**:
  - `Ecommerce.Application/Ecommerce.Application.csproj` — existing file to modify; already has AutoMapper and Stripe.net as examples of the PackageReference format
  - FluentValidation v11 docs: https://docs.fluentvalidation.net/en/latest/aspnet.html

  **Acceptance Criteria**:
  - [ ] `Ecommerce.Application/Ecommerce.Application.csproj` contains `FluentValidation.AspNetCore` PackageReference
  - [ ] `dotnet restore Ecommerce.Application/Ecommerce.Application.csproj` exits with code 0

  **QA Scenarios**:

  ```
  Scenario: NuGet package resolves successfully
    Tool: Bash
    Steps:
      1. Run: dotnet restore Ecommerce.Application/Ecommerce.Application.csproj
      2. Assert: exit code = 0, no "error NU" in output
    Expected Result: Restore completes with 0 errors
    Evidence: .sisyphus/evidence/task-1-restore.txt
  ```

  **Commit**: YES (group with Task 2, 3, 4 as Commit A)

---

- [x] 2. Extract `RestoreCartFromTempData()` helper in `CheckoutController.cs`

  **What to do**:
  - Open `Ecoomerce.Web/Controllers/CheckoutController.cs`
  - The TempData cart-restore block is duplicated **identically** at:
    - Lines 174–199 in `PaymentMethod()` (restores CartItems, CartSubTotal, CartTax, CartShipping, CartDiscount, ShippingMethod, OrderNotes, PromoCode)
    - Lines 250–276 in `OrderConfirmation()` (identical logic plus TempData.Keep for shipping fields)
  - Extract a private helper: `private CartViewModel RestoreCartFromTempData()`
    - Returns a populated `CartViewModel` (not CheckoutViewModel)
    - Deserializes `TempData["CartItems"]` → `List<CartItemDto>` using `System.Text.Json.JsonSerializer`
    - Calls `TempData.Keep("CartItems")`, `TempData.Keep("CartSubTotal")`, `TempData.Keep("CartTax")`, `TempData.Keep("CartShipping")`, `TempData.Keep("CartDiscount")`
    - Sets `.Tax`, `.Shipping`, `.Discount` from TempData using `Convert.ToDecimal`
    - Returns a `CartViewModel` with items and totals populated
  - In `PaymentMethod()`: replace lines 174–199 with `model.Cart = RestoreCartFromTempData();`
  - In `OrderConfirmation()`: replace lines 250–276 with `model.Cart = RestoreCartFromTempData();`
  - Keep the `TempData.Keep()` calls for non-cart fields (FullName, ShippingMethod, etc.) WHERE THEY EXIST outside the extracted block — do not touch those

  **Must NOT do**:
  - Do NOT move the shipping TempData restore (lines 235–248 in OrderConfirmation) into the helper — that restores shipping form fields, not cart data. The helper is ONLY for cart data.
  - Do NOT change method signatures, route attributes, or any other logic

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3, 4)
  - **Blocks**: F1 (final audit)
  - **Blocked By**: None

  **References**:
  - `Ecoomerce.Web/Controllers/CheckoutController.cs` — full file read required; duplicated block at lines 174-199 and 250-276
  - `Ecommerce.Application/ViewModels/Cart & Sheckout/CheckoutViewModel.cs` — CartViewModel structure (Cart.Items: `List<CartItemDto>`, Cart.Tax/Shipping/Discount: `decimal`)
  - `Ecommerce.Application/DTOs/Cart/CartItemDto.cs` — type used in `JsonSerializer.Deserialize<List<CartItemDto>>()`

  **Acceptance Criteria**:
  - [ ] `CheckoutController.cs` contains `private CartViewModel RestoreCartFromTempData()` method
  - [ ] `PaymentMethod()` contains `model.Cart = RestoreCartFromTempData();` (no inline TempData cart block)
  - [ ] `OrderConfirmation()` contains `model.Cart = RestoreCartFromTempData();` (no inline TempData cart block)
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: Build succeeds after refactor
    Tool: Bash
    Steps:
      1. Run: dotnet build ECommerceApp.sln
      2. Assert: "Build succeeded" in output, 0 errors
    Expected Result: Solution compiles cleanly
    Evidence: .sisyphus/evidence/task-2-build.txt

  Scenario: Duplicate code is gone
    Tool: Bash (grep)
    Steps:
      1. Run: grep -c "TempData\[\"CartItems\"\]" Ecoomerce.Web/Controllers/CheckoutController.cs
      2. Assert: count ≤ 2 (one in ShippingInfo, one in the new helper — NOT in PaymentMethod or OrderConfirmation body)
    Expected Result: grep count ≤ 2
    Evidence: .sisyphus/evidence/task-2-grep.txt
  ```

  **Commit**: YES (group with Tasks 1, 3, 4 as Commit A)

---

- [x] 3. Harden `RecentlyViewed` cookie in `ProductController.cs`

  **What to do**:
  - Open `Ecoomerce.Web/Controllers/ProductController.cs`
  - In `AddToRecentlyViewed()` (lines 200-217):
    - Change `recentlyViewed.Count > 10` → `recentlyViewed.Count > 20`
    - Change `recentlyViewed.Take(10)` → `recentlyViewed.Take(20)`
    - Update the `CookieOptions` to:
      ```csharp
      new CookieOptions
      {
          Expires = DateTimeOffset.UtcNow.AddDays(30),
          HttpOnly = true,
          SameSite = SameSiteMode.Strict
      }
      ```
  - `GetRecentlyViewedIds()` (lines 219-229) — **no changes needed** (the `int.TryParse` guard is already correct per audit)

  **Must NOT do**:
  - Do NOT touch any other method in ProductController
  - Do NOT change the cookie name ("RecentlyViewed") or expiry (30 days)
  - Do NOT add `Secure = true` — that would break local HTTP dev; the spec does not require it

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2, 4)
  - **Blocks**: F1
  - **Blocked By**: None

  **References**:
  - `Ecoomerce.Web/Controllers/ProductController.cs` lines 199-229 — exact current code to modify

  **Acceptance Criteria**:
  - [ ] `AddToRecentlyViewed()` uses cap of 20 (not 10)
  - [ ] `CookieOptions` in `AddToRecentlyViewed()` includes `HttpOnly = true` and `SameSite = SameSiteMode.Strict`
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: Cookie options contain HttpOnly and SameSite
    Tool: Bash (grep)
    Steps:
      1. Run: grep -A 5 "CookieOptions" Ecoomerce.Web/Controllers/ProductController.cs
      2. Assert: output contains "HttpOnly = true" and "SameSite = SameSiteMode.Strict"
    Expected Result: Both security flags present
    Evidence: .sisyphus/evidence/task-3-cookie.txt

  Scenario: Cap is 20 not 10
    Tool: Bash (grep)
    Steps:
      1. Run: grep "Count > " Ecoomerce.Web/Controllers/ProductController.cs
      2. Assert: "Count > 20" present, "Count > 10" absent
    Expected Result: Cap is 20
    Evidence: .sisyphus/evidence/task-3-cap.txt
  ```

  **Commit**: YES (group with Tasks 1, 2, 4 as Commit A)

---

- [x] 4. Create `Ecommerce.Tests` project scaffold

  **What to do**:
  - Create file: `Ecommerce.Tests/Ecommerce.Tests.csproj` with:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
        <PackageReference Include="xunit" Version="2.7.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
        <PackageReference Include="Moq" Version="4.20.70" />
        <PackageReference Include="FluentAssertions" Version="6.12.0" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
      </ItemGroup>
      <ItemGroup>
        <ProjectReference Include="..\Ecommerce.Application\Ecommerce.Application.csproj" />
        <ProjectReference Include="..\Ecommerce.Core\Ecommerce.Core.csproj" />
        <ProjectReference Include="..\Ecommerce.Infrastracture\Ecommerce.Infrastracture.csproj" />
        <ProjectReference Include="..\Ecoomerce.Web\Ecoomerce.Web.csproj" />
      </ItemGroup>
    </Project>
    ```
  - Add the project to the solution: `dotnet sln ECommerceApp.sln add Ecommerce.Tests/Ecommerce.Tests.csproj`
  - Create folder structure: `Ecommerce.Tests/Services/`, `Ecommerce.Tests/Controllers/`, `Ecommerce.Tests/Repositories/`
  - Create placeholder file `Ecommerce.Tests/Services/.gitkeep`, etc. to scaffold folders

  **Must NOT do**:
  - Do NOT add `coverlet.collector` (not in spec) unless it doesn't break the build — keep it minimal
  - Reference the exact typo'd project name `Ecoomerce.Web.csproj` — that is the actual filename

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 2, 3)
  - **Blocks**: Tasks 10, 11, 12, 14, 15
  - **Blocked By**: None

  **References**:
  - `ECommerceApp.sln` — needs `dotnet sln add` after creating csproj
  - `Ecommerce.Infrastracture/Ecommerce.Infrastracture.csproj` — note the typo in "Infrastracture" is the real filename
  - `Ecoomerce.Web/Ecoomerce.Web.csproj` — note the typo in "Ecoomerce"

  **Acceptance Criteria**:
  - [ ] `Ecommerce.Tests/Ecommerce.Tests.csproj` exists
  - [ ] `dotnet restore Ecommerce.Tests/Ecommerce.Tests.csproj` → 0 errors
  - [ ] `dotnet build Ecommerce.Tests/Ecommerce.Tests.csproj` → 0 errors (even with no test files yet)
  - [ ] Project appears in `dotnet sln list` output

  **QA Scenarios**:

  ```
  Scenario: Test project scaffolds and builds empty
    Tool: Bash
    Steps:
      1. Run: dotnet build Ecommerce.Tests/Ecommerce.Tests.csproj
      2. Assert: "Build succeeded" in output
    Expected Result: Empty test project compiles
    Evidence: .sisyphus/evidence/task-4-build.txt
  ```

  **Commit**: YES (group with Tasks 1, 2, 3 as Commit A)

---

- [x] 5. Write `RegisterDtoValidator` and remove data annotations from `RegisterDto`

  **What to do**:
  - Create file: `Ecommerce.Application/Validators/RegisterDtoValidator.cs`
  - The validator targets `Ecommerce.Application.DTOs.Auth.RegisterDto`
  - Rules:
    ```csharp
    RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
    RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
    RuleFor(x => x.Email).NotEmpty().EmailAddress();
    RuleFor(x => x.Password)
        .NotEmpty()
        .MinimumLength(8)
        .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
        .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
        .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
        .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    RuleFor(x => x.ConfirmPassword)
        .Equal(x => x.Password).WithMessage("The password and confirmation password do not match.");
    ```
  - Open `Ecommerce.Application/DTOs/Auth/RegisterDto.cs` and **remove** all data annotation attributes:
    - Remove `[Required]` from FirstName, LastName, Email, Password
    - Remove `[EmailAddress]` from Email
    - Remove `[StringLength(100, MinimumLength = 6)]` from Password
    - Remove `[DataType(DataType.Password)]` from Password and ConfirmPassword
    - Remove `[Compare("Password", ...)]` from ConfirmPassword
    - Remove the `using System.ComponentModel.DataAnnotations;` import if no other annotations remain

  **Must NOT do**:
  - Do NOT leave both data annotations AND FluentValidation rules on the same field — pick one. This task picks FV.
  - Do NOT change property names or types in RegisterDto

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 1 completes)
  - **Parallel Group**: Wave 2 (with Tasks 6, 7, 8, 9, 10, 11, 12)
  - **Blocks**: Task 13
  - **Blocked By**: Task 1

  **References**:
  - `Ecommerce.Application/DTOs/Auth/RegisterDto.cs` lines 1-31 — full file; all annotations to remove are visible here
  - `Ecommerce.Application/Ecommerce.Application.csproj` — must already have FluentValidation NuGet (Task 1)
  - FluentValidation `AbstractValidator<T>` pattern — namespace `FluentValidation`

  **Acceptance Criteria**:
  - [ ] `Ecommerce.Application/Validators/RegisterDtoValidator.cs` exists and inherits `AbstractValidator<RegisterDto>`
  - [ ] `RegisterDto.cs` has NO `[Required]`, `[EmailAddress]`, `[StringLength]`, `[Compare]`, `[DataType]` attributes
  - [ ] Password rule: MinimumLength(8), 4 Matches() calls (upper, lower, digit, special)
  - [ ] ConfirmPassword rule: Equal(x => x.Password)
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: Validator class compiles and rules exist
    Tool: Bash
    Steps:
      1. Run: dotnet build Ecommerce.Application/Ecommerce.Application.csproj
      2. Assert: Build succeeded, 0 errors
      3. Run: grep -c "Matches" Ecommerce.Application/Validators/RegisterDtoValidator.cs
      4. Assert: count = 4 (upper, lower, digit, special)
    Expected Result: Build passes, 4 Matches() rules present
    Evidence: .sisyphus/evidence/task-5-build.txt

  Scenario: RegisterDto has no data annotations
    Tool: Bash (grep)
    Steps:
      1. Run: grep "\[Required\]\|\[EmailAddress\]\|\[StringLength\]\|\[Compare\]" Ecommerce.Application/DTOs/Auth/RegisterDto.cs
      2. Assert: (no output — all annotations removed)
    Expected Result: Zero annotation attributes found
    Evidence: .sisyphus/evidence/task-5-annotations.txt
  ```

  **Commit**: YES (group with Tasks 6, 7, 8, 9 as Commit B)

---

- [x] 6. Write `CreateProductDtoValidator`

  **What to do**:
  - Create file: `Ecommerce.Application/Validators/CreateProductDtoValidator.cs`
  - The validator targets `Ecommerce.Application.DTOs.Products.ProductDto`
  - Rules:
    ```csharp
    RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Product name is required and must be under 200 characters.");
    RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
    RuleFor(x => x.CategoryID).GreaterThan(0).WithMessage("A valid category is required.");
    RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    ```
  - Class name: `CreateProductDtoValidator` (inherits `AbstractValidator<ProductDto>`)
  - Namespace: `Ecommerce.Application.Validators`

  **Must NOT do**:
  - Do NOT add annotations to `ProductDto.cs` — it's a read-only DTO from the catalog; FV is the only validation layer
  - Do NOT validate optional fields like Description, ImageURL — spec says only the 4 fields above

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 1)
  - **Parallel Group**: Wave 2 (with Tasks 5, 7, 8, 9, 10, 11, 12)
  - **Blocks**: Task 13
  - **Blocked By**: Task 1

  **References**:
  - `Ecommerce.Application/DTOs/Products/ProductDto.cs` — full file; relevant fields: Name (string), Price (decimal), CategoryID (int), StockQuantity (int)
  - `Ecommerce.Application/Validators/RegisterDtoValidator.cs` (Task 5 output) — use as structural pattern

  **Acceptance Criteria**:
  - [ ] `CreateProductDtoValidator.cs` exists, inherits `AbstractValidator<ProductDto>`
  - [ ] 4 rules: Name (NotEmpty + MaximumLength(200)), Price (GreaterThan(0)), CategoryID (GreaterThan(0)), StockQuantity (GreaterThanOrEqualTo(0))
  - [ ] `dotnet build` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: Validator rules compile and count is correct
    Tool: Bash
    Steps:
      1. Run: dotnet build Ecommerce.Application/Ecommerce.Application.csproj
      2. Assert: Build succeeded
      3. Run: grep -c "RuleFor" Ecommerce.Application/Validators/CreateProductDtoValidator.cs
      4. Assert: count = 4
    Expected Result: Build passes, 4 RuleFor() calls
    Evidence: .sisyphus/evidence/task-6-build.txt
  ```

  **Commit**: YES (group with Tasks 5, 7, 8, 9 as Commit B)

---

- [x] 7. Write `CheckoutViewModelValidator`

  **What to do**:
  - Create file: `Ecommerce.Application/Validators/CheckoutViewModelValidator.cs`
  - The validator targets `Ecommerce.Application.ViewModels.CheckoutViewModel`
  - Rules:
    ```csharp
    RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.");
    RuleFor(x => x.ShippingAddress).NotEmpty().WithMessage("Shipping address is required.");
    RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");
    RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.");
    RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
    RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone number is required.");
    RuleFor(x => x.ShippingMethod)
        .Must(m => new[] { "standard", "express", "free" }.Contains(m?.ToLower()))
        .WithMessage("Shipping method must be Standard, Express, or Free.");
    ```
  - Namespace: `Ecommerce.Application.Validators`
  - The `[Required]` data annotations on CheckoutViewModel fields **remain** (co-existence is intentional — FV adds the ShippingMethod whitelist that annotations cannot express)

  **Must NOT do**:
  - Do NOT remove `[Required]` annotations from `CheckoutViewModel.cs` — they stay for client-side validation compat
  - Do NOT validate payment card fields (CardNumber, CVV, etc.) — out of scope for this phase

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 1)
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 8, 9, 10, 11, 12)
  - **Blocks**: Task 13
  - **Blocked By**: Task 1

  **References**:
  - `Ecommerce.Application/ViewModels/Cart & Sheckout/CheckoutViewModel.cs` — full file; note path has space and typo in folder name
  - `Ecoomerce.Web/Controllers/CheckoutController.cs` lines 460-495 — shows ShippingMethod values used in the system

  **Acceptance Criteria**:
  - [ ] `CheckoutViewModelValidator.cs` exists, 7 RuleFor() calls
  - [ ] ShippingMethod rule uses `.Must()` with case-insensitive whitelist of "standard", "express", "free"
  - [ ] `dotnet build` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: ShippingMethod whitelist rule exists
    Tool: Bash (grep)
    Steps:
      1. Run: grep -c "ShippingMethod" Ecommerce.Application/Validators/CheckoutViewModelValidator.cs
      2. Assert: count ≥ 1
      3. Run: grep "express\|standard\|free" Ecommerce.Application/Validators/CheckoutViewModelValidator.cs
      4. Assert: all three values present
    Expected Result: ShippingMethod validation includes all three valid values
    Evidence: .sisyphus/evidence/task-7-shipping.txt
  ```

  **Commit**: YES (group with Tasks 5, 6, 8, 9 as Commit B)

---

- [x] 8. Write `PromoCodeRequestValidator` in the Web layer

  **What to do**:
  - Create directory: `Ecoomerce.Web/Validators/`
  - Create file: `Ecoomerce.Web/Validators/PromoCodeRequestValidator.cs`
  - The validator targets `Ecoomerce.Web.Controllers.PromoCodeRequest` (the nested class at line 533 of `CheckoutController.cs`)
  - Rules:
    ```csharp
    RuleFor(x => x.Code)
        .NotEmpty().WithMessage("Promo code is required.")
        .MaximumLength(50).WithMessage("Promo code must not exceed 50 characters.")
        .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Promo code must contain only alphanumeric characters.");
    RuleFor(x => x.SubTotal).GreaterThan(0).WithMessage("SubTotal must be greater than 0.");
    ```
  - Namespace: `Ecoomerce.Web.Validators`
  - The validator references `using Ecoomerce.Web.Controllers;` to access `PromoCodeRequest`
  - Add `using FluentValidation;` — the Web project already has FluentValidation transitively via Application, but confirm it compiles

  **Must NOT do**:
  - Do NOT put this validator in `Ecommerce.Application` — that would create an upward dependency (Application → Web layer). The `PromoCodeRequest` class lives in Web, so the validator must too.

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7, 9, 10, 11, 12)
  - **Blocks**: Task 13
  - **Blocked By**: None (PromoCodeRequest already exists in CheckoutController.cs)

  **References**:
  - `Ecoomerce.Web/Controllers/CheckoutController.cs` lines 532-537 — the `PromoCodeRequest` class definition: `public string Code` and `public decimal SubTotal`
  - `Ecommerce.Application/Validators/CreateProductDtoValidator.cs` (Task 6) — use as structural pattern
  - `Ecoomerce.Web/Ecoomerce.Web.csproj` — check that FluentValidation is available (it will be via transitive reference from Application project)

  **Acceptance Criteria**:
  - [ ] `Ecoomerce.Web/Validators/PromoCodeRequestValidator.cs` exists
  - [ ] Targets `PromoCodeRequest` with 2 RuleFor() calls (Code, SubTotal)
  - [ ] Code rule: NotEmpty + MaximumLength(50) + Matches alphanumeric regex
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: No layer violation — PromoCodeRequestValidator is NOT in Application
    Tool: Bash (find/glob)
    Steps:
      1. Run: ls Ecommerce.Application/Validators/
      2. Assert: "PromoCodeRequestValidator.cs" NOT in the listing
      3. Run: ls Ecoomerce.Web/Validators/
      4. Assert: "PromoCodeRequestValidator.cs" IS in the listing
    Expected Result: Validator is in Web, not Application
    Evidence: .sisyphus/evidence/task-8-location.txt
  ```

  **Commit**: YES (group with Tasks 5, 6, 7, 9 as Commit B)

---

- [x] 9. Add OAuth startup bootstrap logger to `Program.cs`

  **What to do**:
  - Open `Ecoomerce.Web/Program.cs`
  - After the `.AddFacebook()` block (currently ends at line 112 with closing `}`), insert the following **before** `var app = builder.Build();` (currently line 122):
    ```csharp
    // Startup validation — warn if OAuth secrets are missing
    using var startupLoggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
    var startupLogger = startupLoggerFactory.CreateLogger("Startup");

    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];

    if (string.IsNullOrWhiteSpace(googleClientId))
        startupLogger.LogWarning("Authentication:Google:ClientId is empty. Google OAuth will not work.");
    if (string.IsNullOrWhiteSpace(facebookAppId))
        startupLogger.LogWarning("Authentication:Facebook:AppId is empty. Facebook OAuth will not work.");
    ```
  - The `using` keyword on `startupLoggerFactory` ensures it's disposed after the `if` checks — this is intentional and correct.

  **Must NOT do**:
  - Do NOT use `builder.Services.BuildServiceProvider()` — this creates a second DI container and is an anti-pattern that can cause double-initialization of singletons
  - Do NOT place this code AFTER `builder.Build()` — it must be before the build so warnings appear at startup configuration time

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7, 8, 10, 11, 12)
  - **Blocks**: F1
  - **Blocked By**: None

  **References**:
  - `Ecoomerce.Web/Program.cs` lines 102-122 — the AddAuthentication block; insert after line 112 (closing `}` of AddFacebook), before `var app = builder.Build();`

  **Acceptance Criteria**:
  - [ ] `Program.cs` contains `LoggerFactory.Create(` — bootstrap logger pattern
  - [ ] `Program.cs` does NOT contain `BuildServiceProvider()` anywhere
  - [ ] Warning messages reference `Authentication:Google:ClientId` and `Authentication:Facebook:AppId`
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: Bootstrap logger uses LoggerFactory not BuildServiceProvider
    Tool: Bash (grep)
    Steps:
      1. Run: grep "BuildServiceProvider" Ecoomerce.Web/Program.cs
      2. Assert: (no output)
      3. Run: grep "LoggerFactory.Create" Ecoomerce.Web/Program.cs
      4. Assert: match found
    Expected Result: No BuildServiceProvider, LoggerFactory.Create is present
    Evidence: .sisyphus/evidence/task-9-logger.txt
  ```

  **Commit**: YES (group with Tasks 5, 6, 7, 8 as Commit B)

---

- [x] 10. Write `OrderServiceTests` (4 tests)

  **What to do**:
  - Create file: `Ecommerce.Tests/Services/OrderServiceTests.cs`
  - Using: xUnit, Moq, FluentAssertions
  - Mock all 6 constructor dependencies: `IUnitOfWork`, `IOrderRepository`, `ICartRepository`, `IProductRepository`, `IMapper`, `ILogger<OrderService>`
  - Setup pattern: `new Mock<T>()`, pass `.Object` to constructor
  - **Test 1**: `CreateOrderAsync_HappyPath_ReturnsOrder`
    - Arrange: Cart with 1 item (ProductID=1, Qty=2). Product(ID=1, Price=50m, StockQuantity=10, IsAvailable=true). `_cartRepository.GetCartByUserIdAsync("user1")` returns cart. `_productRepository.GetByIdsAsync(...)` returns product list. `_orderRepository.AddAsync(...)` returns task. `_unitOfWork.SaveChangesAsync()` returns 1. `_mapper.Map<OrderDto>(...)` returns a new `OrderDto { OrderID = 1, TotalAmount = 108m }` (50*2=100 + 8% tax = 108).
    - Act: `await service.CreateOrderAsync("user1", "123 Street", "CreditCard")`
    - Assert: result is not null, result.TotalAmount = 108m
  - **Test 2**: `CreateOrderAsync_InsufficientStock_ThrowsInvalidOperationException`
    - Arrange: Cart with 1 item (ProductID=1, Qty=5). Product(StockQuantity=2) — less than requested.
    - Act: `Func<Task> act = () => service.CreateOrderAsync(...)`
    - Assert: `await act.Should().ThrowAsync<InvalidOperationException>()`
  - **Test 3**: `CancelOrderAsync_RestoresStock`
    - Arrange: Order with OrderStatus.pending, 1 OrderItem (ProductID=1, Qty=3). Product(StockQuantity=7). `_orderRepository.GetByIdAsync(orderId)` returns order. `_productRepository.GetByIdsAsync(...)` returns product.
    - Act: `await service.CancelOrderAsync(1)`
    - Assert: `_productRepository.Verify(r => r.UpdateAsync(It.Is<Product>(p => p.StockQuantity == 10)), Times.Once)` — 7+3=10
  - **Test 4**: `CancelOrderAsync_AlreadyCancelled_NoOp`
    - Arrange: Order with OrderStatus.cancelled. `_orderRepository.GetByIdAsync(orderId)` returns order.
    - Act: `await service.CancelOrderAsync(1)`
    - Assert: `_unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never)` — no transaction started

  **Key technical notes**:
  - `OrderService` constructor needs `ILogger<OrderService>` — mock it with `Mock<ILogger<OrderService>>()`
  - `cart.Items` must be a concrete `List<CartItem>` (not `ICollection<CartItem>`) for `.Any()` to work in the service
  - `Order.OrderItems` in CancelOrderAsync test must be populated — the service calls `.Select(i => i.ProductID)` on it
  - `_unitOfWork.BeginTransactionAsync()` / `CommitTransactionAsync()` / `RollbackTransactionAsync()` — mock them to return `Task.CompletedTask`

  **Must NOT do**:
  - Do NOT use EF InMemory for these tests — pure Moq mocking only
  - Do NOT test `GetOrderDetailsAsync` or `UpdateOrderStatusAsync` — not in spec

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 4)
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7, 8, 9, 11, 12)
  - **Blocks**: F2
  - **Blocked By**: Task 4

  **References**:
  - `Ecommerce.Application/Services/Implementations/OrderService.cs` — full file; CreateOrderAsync lines 73-173, CancelOrderAsync lines 37-71
  - `Ecommerce.Core/Entities/Order.cs` — fields: OrderID, UserID, Status (OrderStatus enum), OrderItems (ICollection<OrderItem>)
  - `Ecommerce.Core/Entities/CartItem.cs` — fields: CartItemID, CartID, ProductID, Quantity
  - `Ecommerce.Core/Entities/Product.cs` — StockQuantity, IsAvailable, Price, ProductID
  - `Ecommerce.Core/Enums/OrderStatus.cs` — values: pending, processing, confirmed, shipped, delivered, cancelled, returned
  - `Ecommerce.Application/DTOs/Order/OrderDto.cs` — return type of CreateOrderAsync
  - `Ecommerce.Core/Interfaces/IRepository.cs` — base interface: GetByIdAsync, AddAsync, UpdateAsync, ListAllAsync
  - `Ecommerce.Core/Interfaces/IUnitOfWork.cs` — interface with BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync, SaveChangesAsync

  **Acceptance Criteria**:
  - [ ] `OrderServiceTests.cs` exists with 4 test methods decorated `[Fact]`
  - [ ] `dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~OrderServiceTests"` → 4 passed, 0 failed

  **QA Scenarios**:

  ```
  Scenario: All 4 OrderService tests pass
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~OrderServiceTests" --logger "console;verbosity=normal"
      2. Assert: "Passed: 4" in output, "Failed: 0"
    Expected Result: 4/4 pass
    Evidence: .sisyphus/evidence/task-10-tests.txt

  Scenario: InsufficientStock test actually throws (not passes vacuously)
    Tool: Bash
    Steps:
      1. Run: dotnet test --filter "InsufficientStock" --logger "console;verbosity=detailed"
      2. Assert: "Passed" status for that test
    Evidence: .sisyphus/evidence/task-10-stock-test.txt
  ```

  **Commit**: YES (group with Tasks 11, 12, 14, 15 as Commit C)

---

- [x] 11. Write `CartServiceTests` (7 tests)

  **What to do**:
  - Create file: `Ecommerce.Tests/Services/CartServiceTests.cs`
  - Mock 4 dependencies: `IUnitOfWork`, `ICartRepository`, `IProductRepository`, `IMapper`
  - **Test 1**: `AddItemToCartAsync_NewItem_AddsToCart`
    - Arrange: Empty cart (Items = new List<CartItem>()). Product available, StockQuantity=10. `_cartRepository.GetCartByUserIdAsync` returns cart. `_productRepository.GetByIdAsync(1)` returns product.
    - Act: `await service.AddItemToCartAsync("user1", 1, 2)`
    - Assert: `_cartRepository.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.Any(i => i.ProductID == 1))), Times.Once)`
  - **Test 2**: `AddItemToCartAsync_ExistingItem_IncrementsQuantity`
    - Arrange: Cart with existing CartItem(ProductID=1, Quantity=3). Product available, StockQuantity=10.
    - Act: `await service.AddItemToCartAsync("user1", 1, 2)`
    - Assert: `_cartRepository.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.First().Quantity == 5)), Times.Once)` — 3+2=5
  - **Test 3**: `AddItemToCartAsync_OutOfStock_ThrowsArgumentException`
    - Arrange: Product has StockQuantity=1, request Quantity=5.
    - Act/Assert: `await act.Should().ThrowAsync<ArgumentException>().WithParameterName("productId")`
  - **Test 4**: `UpdateItemQuantityAsync_ValidQty_Updates`
    - Arrange: Cart with CartItem(CartItemID=1, Quantity=3). NewQuantity=7.
    - Act: `await service.UpdateItemQuantityAsync("user1", 1, 7)`
    - Assert: `_cartRepository.Verify(r => r.UpdateAsync(It.Is<Cart>(c => c.Items.First(i => i.CartItemID == 1).Quantity == 7)), Times.Once)`
  - **Test 5**: `UpdateItemQuantityAsync_ZeroQty_RemovesItem`
    - Arrange: Cart with CartItem(CartItemID=1). NewQuantity=0.
    - Act: `await service.UpdateItemQuantityAsync("user1", 1, 0)`
    - Assert: `_cartRepository.Verify(r => r.UpdateAsync(It.Is<Cart>(c => !c.Items.Any(i => i.CartItemID == 1))), Times.Once)`
  - **Test 6**: `RemoveItemFromCartAsync_RemovesItem`
    - Arrange: Cart with CartItem(CartItemID=5). Act: `await service.RemoveItemFromCartAsync("user1", 5)`. Assert: item removed via UpdateAsync verify.
  - **Test 7**: `ClearCartAsync_EmptiesCart`
    - Arrange: Cart with 3 items. Act: `await service.ClearCartAsync("user1")`. Assert: `_cartRepository.Verify(r => r.UpdateAsync(It.Is<Cart>(c => !c.Items.Any())), Times.Once)`

  **Key technical notes**:
  - `CartService` throws `ArgumentException` (not InvalidOperationException) — use `.ThrowAsync<ArgumentException>()`
  - `Cart.CartID == 0` triggers `AddAsync` not `UpdateAsync` — arrange existing cart with `CartID = 1` for update tests
  - `UpdateItemQuantityAsync` delegates to `RemoveItemFromCartAsync` when qty≤0 — Test 5 verifies the cart repo update, not two separate calls

  **Must NOT do**:
  - Do NOT test `GetOrCreateCartAsync` or `GetCartTotalAsync` — not in spec

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 4)
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7, 8, 9, 10, 12)
  - **Blocks**: F2
  - **Blocked By**: Task 4

  **References**:
  - `Ecommerce.Application/Services/Implementations/CartService.cs` — full file lines 1-157
  - `Ecommerce.Core/Entities/Cart.cs` — CartID, UserID, Items (ICollection<CartItem>)
  - `Ecommerce.Core/Entities/CartItem.cs` — CartItemID, CartID, ProductID, Quantity
  - `Ecommerce.Core/Entities/Product.cs` — IsAvailable, StockQuantity
  - `Ecommerce.Tests/Services/OrderServiceTests.cs` (Task 10) — use as structural pattern for mock setup

  **Acceptance Criteria**:
  - [ ] `CartServiceTests.cs` exists with 7 test methods
  - [ ] `dotnet test --filter "FullyQualifiedName~CartServiceTests"` → 7 passed, 0 failed

  **QA Scenarios**:

  ```
  Scenario: All 7 CartService tests pass
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~CartServiceTests" --logger "console;verbosity=normal"
      2. Assert: "Passed: 7", "Failed: 0"
    Expected Result: 7/7 pass
    Evidence: .sisyphus/evidence/task-11-tests.txt
  ```

  **Commit**: YES (group with Tasks 10, 12, 14, 15 as Commit C)

---

- [x] 12. Write `PromoCodeServiceTests` (5 tests)

  **What to do**:
  - Create file: `Ecommerce.Tests/Services/PromoCodeServiceTests.cs`
  - Mock 2 dependencies: `IPromoCodeRepository`, `IUnitOfWork`
  - **Test 1**: `ValidatePromoCodeAsync_ValidCode_ReturnsTrue`
    - Arrange: PromoCode(IsActive=true, ExpirationDate=DateTime.UtcNow.AddDays(30), UsedCount=0, MaxUsage=10). `_repo.GetPromoCodeByCodeAsync("SAVE10")` returns this.
    - Act/Assert: `result.Should().BeTrue()`
  - **Test 2**: `ValidatePromoCodeAsync_ExpiredCode_ReturnsFalse`
    - Arrange: PromoCode(IsActive=true, ExpirationDate=DateTime.UtcNow.AddDays(-1), UsedCount=0, MaxUsage=10).
    - Act/Assert: `result.Should().BeFalse()`
  - **Test 3**: `ValidatePromoCodeAsync_MaxUsageReached_ReturnsFalse`
    - Arrange: PromoCode(IsActive=true, ExpirationDate=UtcNow.AddDays(30), UsedCount=10, MaxUsage=10).
    - Act/Assert: `result.Should().BeFalse()`
  - **Test 4**: `ApplyPromoCodeAsync_PercentageDiscount_CalculatesCorrectly`
    - Arrange: PromoCode(DiscountType=DiscountType.Percentage, DiscountValue=20, IsActive=true, ExpirationDate=UtcNow+30d, UsedCount=0, MaxUsage=10).
    - Act: `var discount = await service.ApplyPromoCodeAsync("PERCENT20", 100m)`
    - Assert: `discount.Should().Be(20m)` — 100 * 20/100 = 20
  - **Test 5**: `ApplyPromoCodeAsync_FixedDiscount_CapsAtOrderTotal`
    - Arrange: PromoCode(DiscountType=DiscountType.FixedAmount, DiscountValue=200, IsActive=true, ExpirationDate=UtcNow+30d, UsedCount=0, MaxUsage=10).
    - Act: `var discount = await service.ApplyPromoCodeAsync("FIXED200", 100m)` — order total is only 100
    - Assert: `discount.Should().Be(100m)` — capped at order total

  **Key technical notes**:
  - `ValidatePromoCodeAsync` calls `_promoCodeRepository.GetPromoCodeByCodeAsync(code.ToUpper().Trim())` — arrange the mock with `"SAVE10"` (uppercase) if the test passes "save10" as input, or just use uppercase directly in test input
  - `ApplyPromoCodeAsync` internally calls `ValidatePromoCodeAsync` which calls the repo AGAIN — the mock needs `ReturnsAsync` that works for multiple calls
  - `DiscountType` enum is in `Ecommerce.Core.Enums`: `Percentage`, `FixedAmount`

  **Must NOT do**:
  - Do NOT test `UsePromoCodeAsync` — not in spec

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 4)
  - **Parallel Group**: Wave 2 (with Tasks 5, 6, 7, 8, 9, 10, 11)
  - **Blocks**: F2
  - **Blocked By**: Task 4

  **References**:
  - `Ecommerce.Application/Services/Implementations/PromoCodeService.cs` — full file lines 1-135
  - `Ecommerce.Core/Entities/PromoCode.cs` — all fields including ExpirationDate, MaxUsage, UsedCount, DiscountType, DiscountValue, IsActive
  - `Ecommerce.Core/Enums/DiscountType.cs` — Percentage, FixedAmount
  - `Ecommerce.Application/DTOs/PromoCodeDto.cs` (namespace: `Ecommerce.Application.DTOs.Promotion`) — return type of GetPromoCodeByCodeAsync

  **Acceptance Criteria**:
  - [ ] `PromoCodeServiceTests.cs` exists with 5 test methods
  - [ ] `dotnet test --filter "FullyQualifiedName~PromoCodeServiceTests"` → 5 passed, 0 failed

  **QA Scenarios**:

  ```
  Scenario: All 5 PromoCode tests pass
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~PromoCodeServiceTests" --logger "console;verbosity=normal"
      2. Assert: "Passed: 5", "Failed: 0"
    Expected Result: 5/5 pass
    Evidence: .sisyphus/evidence/task-12-tests.txt
  ```

  **Commit**: YES (group with Tasks 10, 11, 14, 15 as Commit C)

---

- [x] 13. Register FluentValidation in `Program.cs` + include `PromoCodeRequestValidator`

  **What to do**:
  - Open `Ecoomerce.Web/Program.cs`
  - Add the following `using` statements at the top:
    ```csharp
    using FluentValidation;
    using FluentValidation.AspNetCore;
    ```
  - In the services section (after line 17 `builder.Services.AddControllersWithViews();` but before `builder.Services.AddDbContextPool`), add:
    ```csharp
    // FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Ecommerce.Application.Validators.CreateProductDtoValidator>();
    // Register Web-layer validators (not in Application assembly)
    builder.Services.AddScoped<IValidator<Ecoomerce.Web.Controllers.PromoCodeRequest>, Ecoomerce.Web.Validators.PromoCodeRequestValidator>();
    ```
  - The `AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>()` call will auto-discover all 3 Application validators: `RegisterDtoValidator`, `CreateProductDtoValidator`, `CheckoutViewModelValidator`.
  - `PromoCodeRequestValidator` is in the Web project so it must be registered manually.

  **Must NOT do**:
  - Do NOT use the old `AddFluentValidation()` method — it's deprecated in v11+
  - Do NOT use `AddFluentValidationClientsideAdapters()` unless the front-end views use unobtrusive validation — out of scope for this phase

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO — must run after Tasks 5, 6, 7, 8
  - **Parallel Group**: Wave 3 (with Tasks 14, 15)
  - **Blocks**: F1
  - **Blocked By**: Tasks 5, 6, 7, 8

  **References**:
  - `Ecoomerce.Web/Program.cs` — full file; insert after line 17 in services section
  - `Ecommerce.Application/Validators/CreateProductDtoValidator.cs` (Task 6) — the anchor type for assembly scan
  - `Ecoomerce.Web/Validators/PromoCodeRequestValidator.cs` (Task 8) — manually registered

  **Acceptance Criteria**:
  - [ ] `Program.cs` contains `AddFluentValidationAutoValidation()`
  - [ ] `Program.cs` contains `AddValidatorsFromAssemblyContaining<`
  - [ ] `Program.cs` manually registers `PromoCodeRequestValidator`
  - [ ] `dotnet build ECommerceApp.sln` → 0 errors

  **QA Scenarios**:

  ```
  Scenario: FV registration is in Program.cs
    Tool: Bash (grep)
    Steps:
      1. Run: grep "AddFluentValidationAutoValidation\|AddValidatorsFromAssemblyContaining" Ecoomerce.Web/Program.cs
      2. Assert: both lines present
    Expected Result: Both registration calls found
    Evidence: .sisyphus/evidence/task-13-registration.txt

  Scenario: Full solution builds after registration
    Tool: Bash
    Steps:
      1. Run: dotnet build ECommerceApp.sln
      2. Assert: "Build succeeded" in output, 0 errors
    Evidence: .sisyphus/evidence/task-13-build.txt
  ```

  **Commit**: YES (Final Commit — after Commit C)

---

- [x] 14. Write `CheckoutControllerTests` (3 tests)

  **What to do**:
  - Create file: `Ecommerce.Tests/Controllers/CheckoutControllerTests.cs`
  - The `CheckoutController` constructor takes 8 parameters — mock all of them: `ICartService`, `IProductService`, `IOrderService`, `ILogger<CheckoutController>`, `IActivityLogService`, `IPromoCodeService`, `IShippingService`, `IEmailSenderService`
  - Create a helper method to build `ClaimsPrincipal`:
    ```csharp
    private static ClaimsPrincipal CreateUser(string userId) =>
        new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));
    ```
  - Assign user to controller: `controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };`
  - **Test 1**: `OrderComplete_WrongUser_ReturnsForbid`
    - Arrange: Logged-in user = "user-A". `_orderService.GetOrderDetailsAsync(1)` returns `OrderDetailsDto { OrderID = 1, UserID = "user-B" }`.
    - Act: `var result = await controller.OrderComplete(1)`
    - Assert: `result.Should().BeOfType<ForbidResult>()`
  - **Test 2**: `OrderComplete_ValidUser_ReturnsView`
    - Arrange: Logged-in user = "user-A". `_orderService.GetOrderDetailsAsync(1)` returns `OrderDetailsDto { OrderID = 1, UserID = "user-A" }`.
    - Act: `var result = await controller.OrderComplete(1)`
    - Assert: `result.Should().BeOfType<ViewResult>()`
  - **Test 3**: `OrderComplete_OrderNotFound_ReturnsNotFound`
    - Arrange: `_orderService.GetOrderDetailsAsync(999)` returns `null`.
    - Act: `var result = await controller.OrderComplete(999)`
    - Assert: `result.Should().BeOfType<NotFoundResult>()`

  **Key technical notes**:
  - `CheckoutController.OrderComplete` is at lines 372-402 of `CheckoutController.cs`
  - The method calls `User.FindFirstValue(ClaimTypes.NameIdentifier)` — requires `ClaimsPrincipal` to have a `NameIdentifier` claim
  - `OrderDetailsDto` must be found in `Ecommerce.Application/DTOs/Order/` — read that file to confirm field names before writing
  - The controller is in namespace `Ecoomerce.Web.Controllers` — import that namespace

  **Must NOT do**:
  - Do NOT test other controller actions (ShippingInfo, PaymentMethod, etc.) — spec only requires the 3 OrderComplete tests

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 4)
  - **Parallel Group**: Wave 3 (with Tasks 13, 15)
  - **Blocks**: F2
  - **Blocked By**: Task 4

  **References**:
  - `Ecoomerce.Web/Controllers/CheckoutController.cs` lines 372-402 — `OrderComplete()` method
  - `Ecommerce.Application/DTOs/Order/OrderDetailsDto.cs` — must confirm `UserID` field name (verify by reading the file)
  - `Ecommerce.Application/Services/Interfaces/IOrderService.cs` — `GetOrderDetailsAsync(int orderId)` signature
  - `Ecommerce.Tests/Services/OrderServiceTests.cs` (Task 10) — structural pattern for mock setup

  **Acceptance Criteria**:
  - [ ] `CheckoutControllerTests.cs` exists with 3 test methods
  - [ ] `dotnet test --filter "FullyQualifiedName~CheckoutControllerTests"` → 3 passed, 0 failed

  **QA Scenarios**:

  ```
  Scenario: All 3 controller tests pass
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~CheckoutControllerTests" --logger "console;verbosity=normal"
      2. Assert: "Passed: 3", "Failed: 0"
    Expected Result: 3/3 pass
    Evidence: .sisyphus/evidence/task-14-tests.txt
  ```

  **Commit**: YES (group with Tasks 10, 11, 12, 15 as Commit C)

---

- [x] 15. Write `ProductRepositoryTests` (4 tests, EF InMemory)

  **What to do**:
  - Create file: `Ecommerce.Tests/Repositories/ProductRepositoryTests.cs`
  - Use EF Core InMemory provider — per-test unique DB:
    ```csharp
    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    ```
  - **Test 1**: `SearchAsync_ByName_ReturnsMatching`
    - Seed: 3 products — "Laptop Pro", "Gaming Laptop", "Phone". Call `SearchAsync("laptop")`.
    - Assert: result count = 2, all names contain "laptop" (case-insensitive)
  - **Test 2**: `SearchAsync_ByCategoryId_Filters`
    - Seed: 3 products — 2 in CategoryID=1, 1 in CategoryID=2. Call `SearchAsync(null, categoryId: 1)`.
    - Assert: result count = 2, all have CategoryID = 1
  - **Test 3**: `SearchAsync_Paginated_ReturnsCorrectPage`
    - Seed: 15 products. Call `SearchPagedAsync(null, null, null, null, null, null, page:2, pageSize:5)`.
    - Assert: result.Products count = 5, result.TotalCount = 15
  - **Test 4**: `GetByIdsAsync_ReturnsMatchingProducts`
    - Seed: 5 products with IDs 1–5. Call `GetByIdsAsync(new[] { 1, 3, 5 })`.
    - Assert: result count = 3, IDs match {1, 3, 5}

  **Key technical notes**:
  - `AppDbContext` is in `Ecommerce.Infrastructure.Data` namespace (note: typo in project folder "Infrastracture")
  - `ProductRepository` constructor takes `AppDbContext` — instantiate directly: `new ProductRepository(context)` where context is `new AppDbContext(options)`
  - EF InMemory: Relationships and FK constraints are NOT enforced — seed products without needing real Category/Brand entities (set CategoryID/BrandID directly on the entity)
  - Check `ProductRepository` implementation to understand `SearchAsync` — it likely uses `.Where(p => p.Name.Contains(searchTerm))` — the InMemory provider handles string Contains
  - For Test 3 (pagination): seed 15 products with sequential IDs/names to avoid sort confusion

  **Must NOT do**:
  - Do NOT mock `IProductRepository` here — tests are for the REAL `ProductRepository` implementation against InMemory DB
  - Do NOT create a real SQL Server database connection

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES (after Task 4)
  - **Parallel Group**: Wave 3 (with Tasks 13, 14)
  - **Blocks**: F2
  - **Blocked By**: Task 4

  **References**:
  - `Ecommerce.Infrastracture/Repositories/ProductRepository.cs` — full implementation; read to understand SearchAsync and SearchPagedAsync exact query logic
  - `Ecommerce.Infrastracture/Data/AppDbContext.cs` — constructor signature (takes `DbContextOptions<AppDbContext>`)
  - `Ecommerce.Core/Entities/Product.cs` — entity fields for seeding: ProductID, Name, CategoryID, IsAvailable, Price, StockQuantity
  - `Ecommerce.Core/Interfaces/IProductRepository.cs` — SearchAsync signature: `(string searchTerm, int? categoryId, int? brandId)`, SearchPagedAsync signature: `(string?, int?, int?, decimal?, decimal?, string?, int page, int pageSize)`

  **Acceptance Criteria**:
  - [ ] `ProductRepositoryTests.cs` exists with 4 test methods
  - [ ] Each test uses a fresh InMemory DB (unique Guid name)
  - [ ] `dotnet test --filter "FullyQualifiedName~ProductRepositoryTests"` → 4 passed, 0 failed

  **QA Scenarios**:

  ```
  Scenario: All 4 repository tests pass
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --filter "FullyQualifiedName~ProductRepositoryTests" --logger "console;verbosity=normal"
      2. Assert: "Passed: 4", "Failed: 0"
    Expected Result: 4/4 pass
    Evidence: .sisyphus/evidence/task-15-tests.txt

  Scenario: Full test suite ≥23 tests, 0 failures
    Tool: Bash
    Steps:
      1. Run: dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --logger "console;verbosity=normal"
      2. Assert: "Passed: 23" or higher, "Failed: 0"
    Expected Result: ≥23 tests pass
    Evidence: .sisyphus/evidence/task-15-full-suite.txt
  ```

  **Commit**: YES (group with Tasks 10, 11, 12, 14 as Commit C)

---

## Final Verification Wave

- [ ] F1. **Plan Compliance Audit** — `oracle`

  Read the plan end-to-end. For each "Must Have": verify implementation exists. For each "Must NOT Have": search codebase — reject with file:line if found (`BuildServiceProvider`, layer violation, etc.). Check evidence files.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [ ] F2. **Build + Test Suite Run** — `unspecified-high`

  Run `dotnet build ECommerceApp.sln`. Run `dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --logger "console;verbosity=normal"`. Save full output.
  Expected: 0 build errors, 0 test failures, ≥23 tests passing.
  Evidence: `.sisyphus/evidence/final-build.txt`, `.sisyphus/evidence/final-tests.txt`
  Output: `Build [PASS/FAIL] | Tests [N pass / N fail] | VERDICT`

- [ ] F3. **Code Quality Review** — `unspecified-high`

  Review all changed/new files for: `as any`/patterns that bypass type safety, empty catch blocks that swallow exceptions, `Console.WriteLine` in production code (not allowed outside the bootstrap logger), duplicate validation logic (both data annotations AND FV on same DTO), `BuildServiceProvider()` anywhere.
  Output: `Files reviewed: N | Issues: N | VERDICT`

- [ ] F4. **Scope Fidelity Check** — `deep`

  For each task 1–15: read "What to do", check git diff. Verify 1:1 — nothing missing, nothing extra. Flag any test file that imports namespaces not in scope. Flag any modified file not listed in the plan.
  Output: `Tasks [N/N compliant] | Unaccounted changes [CLEAN/N files] | VERDICT`

---

## Commit Strategy

- **Commit A** (after Wave 1): `chore: scaffold test project and start Phase 4 infrastructure`
- **Commit B** (after Wave 2): `feat(validation): add FluentValidation validators for all DTO entry points`
- **Commit C** (after Wave 3): `test: add 23-test unit test suite covering services, controller, repository`
- **Final Commit**: `chore(phase4): wire FluentValidation registration and OAuth startup logging`

Pre-commit check: `dotnet build ECommerceApp.sln`

---

## Success Criteria

```bash
dotnet build ECommerceApp.sln
# Expected: Build succeeded. 0 Error(s)

dotnet test Ecommerce.Tests/Ecommerce.Tests.csproj --logger "console;verbosity=normal"
# Expected: Passed: ≥23, Failed: 0

grep -r "BuildServiceProvider" Ecoomerce.Web/Program.cs
# Expected: (no output)

grep -r "HttpOnly" Ecoomerce.Web/Controllers/ProductController.cs
# Expected: HttpOnly = true
```

### Final Checklist
- [ ] All "Must Have" implemented
- [ ] All "Must NOT Have" absent from codebase
- [ ] ≥23 tests pass, 0 fail
- [ ] `dotnet build` clean
