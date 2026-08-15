# OrderFlow reference

Read from [SKILL.md](SKILL.md). Do not load unless implementing a slice or changing contracts.

## Platform contract

Service code: `ORDERFLOW`. Product-app header: `X-Integration-Key`.

`POST {Platform:BaseUrl}/api/licenses/validate`

```json
{ "licenseKey": "ORDERFLOW-XXXX-YYYY", "serviceCode": "ORDERFLOW" }
```

```json
{ "isValid": true, "planName": "Growth", "expiresAt": "2027-12-31T00:00:00Z", "message": null }
```

Invalid licenses still return HTTP 200 with `isValid: false`. Missing header may be 400.

OrderFlow maps this to `LicenseValidationResult` in Application. Implementation: `OrderFlow.Infrastructure/Platform/PlatformLicenseClient.cs`.

Dev secrets (Development log / `appsettings.Development.json` only):

- Integration key: `ORDERFLOW-INTEGRATION-DEV-KEY-1b7e3c4a5d8f`
- Demo license: `ORDERFLOW-DEVK-TEST` (Growth), seeded by Platform `SeedData.SeedOrderFlowAsync`

Production: prefer env vars (`PLATFORM__INTEGRATIONKEY`, `JWT__KEY`, etc.) — see Configuration table below. User secrets are fine for local Development only.

## Plan limits (enforced in Domain `PlanQuota`)

| Plan | Max products | Max orders/month | Max users | AI |
|------|--------------|------------------|-----------|----|
| Starter | 50 | 300 | 1 | No |
| Growth | 300 | unlimited | 3 | No |
| Business | unlimited | unlimited | 10 | Yes (later) |

`PlanQuota.FromPlanName` matches prefix `Starter` / `Growth` / `Business` (case-insensitive). Anything else → Starter + `IsUnrecognized`.

## Current HTTP API

JSON camelCase. Auth rate limit policy `auth`: 20 req/min.

| Method | Route | Auth | Handler |
|--------|-------|------|---------|
| POST | `/api/auth/signup` | Anonymous | `SignUpCommand` |
| POST | `/api/auth/login` | Anonymous | `LoginCommand` |
| GET | `/api/auth/me` | JWT | `GetMeQuery` |
| GET | `/api/products` | JWT | `ListProductsQuery` (`search`, `category`, `page`, `pageSize` 1–100, default 20) |
| GET | `/api/products/{id}` | JWT | `GetProductQuery` |
| POST | `/api/products` | JWT | `CreateProductCommand` |
| PUT | `/api/products/{id}` | JWT | `UpdateProductCommand` (`ExpectedVersion`; does not change stock) |
| POST | `/api/products/{id}/stock` | JWT | `AdjustStockCommand` (`QuantityDelta`, `ExpectedVersion`, `Notes?`) |
| GET | `/api/dashboard` | JWT | `GetDashboardQuery` |

Sign-up body: `licenseKey`, `email`, `password`, `shopName`, `displayName?`, `phone?`.  
Login body: `email`, `password` (no license key).  
Auth response: `token`, `expiresAt`, `shopId`, `shopName`, `userId`, `email`, `displayName`, `role`, `plan`.

Product create: `name`, `sku` (stored uppercase), `category?`, `price`, `stock`, `lowStockThreshold`.  
Product update: same except no `stock`; plus `isActive`, `expectedVersion`.  
SKU unique per shop. Create is blocked at `PlanQuota.MaxProducts` (403). Duplicate SKU → 409. Stale `expectedVersion` → 409 `{ "code": "concurrency" }`.

Dashboard: `todaysSales`, `orderCount`, `pendingWhatsAppCount` are 0 until orders exist; `lowStock` is active products with `stock <= lowStockThreshold` (max 50).

Exception middleware maps:

- `UnauthorizedAppException` → 401
- `ConflictAppException` → 409
- `ConcurrencyAppException` → 409 (or 409 with distinct code; client may retry)
- `NotFoundAppException` → 404
- `ForbiddenAppException` → 403
- FluentValidation `ValidationException` → 400

## Domain (implemented)

**Shop:** Id, Name (≤200), Phone? (≤50), Address? (≤400), LicenseLookupHash (exactly 64), ProtectedLicenseKey, PlanName (≤100), PlanExpiresAt?, PlanUnrecognized, WhatsAppConnectionStatus, CreatedAt, UpdatedAt

**User:** Id, ShopId, Email (unique, lowercase, ≤320), DisplayName (≤200), PasswordHash, Role (`Owner` \| `Assistant`), CreatedAt

**Product:** Id, ShopId, Name (≤200), Sku (≤50, unique per shop, stored uppercase), Category? (≤80), Price (0–999,999,999.99, numeric 12,2), Stock (0–99,999,999), LowStockThreshold (0–99,999,999), IsActive, Version (concurrency token, ≥1), CreatedAt, UpdatedAt. `IsLowStock` is computed (`Stock <= LowStockThreshold`), not stored.

**StockMovement:** Id, ShopId, ProductId, QuantityDelta, ResultingStock (0–99,999,999), Type (`Adjustment` \| `Reserve` \| `Deduct` \| `Release`), Notes? (≤400), CreatedByUserId?, CreatedAt. Slice 2 writes `Adjustment` only.

Roles: `UserRole`. WhatsApp: `Disconnected` \| `Connected` \| `Error`. DB CHECKs enforce enum sets and non-empty required strings — see Constraints below.

## Domain (planned — add when that slice starts)

- **Customer** — shop’s WhatsApp end-customer (not Platform Customer)
- **Order** — status `Pending \| Confirmed \| Paid \| Fulfilled \| Cancelled`, source `WhatsApp \| Manual`
- **OrderLine** — product, qty, unit price
- **Payment** — Paystack reference, status, amount

**Stock rule:** reserve on Confirmed, deduct on Paid, release on Cancelled. Pending WhatsApp drafts do not touch stock.

**Optimistic concurrency (prevent overselling):** On stock deduction/reserve/release, the SQL update must be atomic, e.g.:

```sql
UPDATE Products
SET Stock = Stock - @qty, Version = Version + 1
WHERE Id = @id AND Stock >= @qty AND Version = @expectedVersion
```

If rows affected = 0, throw `ConcurrencyAppException` and notify the customer/shop to retry. Do not rely on read-modify-write alone.

## Application feature pattern

```
Features/{Name}/
  {Verb}{Name}Command.cs          # or Query — XML summary of the use case
  {Verb}{Name}CommandHandler.cs   # XML: tenancy, side effects, exceptions
  {Verb}{Name}CommandValidator.cs # required for every write; lengths match Shared DTO + EF
```

Register happens automatically via `AddMediatR` + `AddValidatorsFromAssembly`. Controllers send MediatR requests only.

New external systems: interface in `Application/Common/Interfaces`, adapter in `Infrastructure/{Area}/`.

Passwords and other secrets: always set `MaximumLength` on login/verify commands too (DoS / payload bound), not only signup.

## Shared DTOs

`OrderFlow.Shared/DTOs` contains **public, external-facing contracts** used by the Angular frontend (e.g. `AuthResponse`, `ProductDto`, `OrderListDto`). Internal MediatR responses can be simple records or DTOs, but must not expose Domain entities directly. Use AutoMapper or explicit mapping (`ProductDto.FromEntity(product)`) inside the handler, not in the Controller.

DTO DataAnnotations (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[MinLength]`) are documentation + secondary guard — **FluentValidation is authoritative** for API 400s. Lengths must match EF `HasMaxLength` and Angular `*_FIELD_LIMITS`.

## Constraints (full stack)

When adding or changing a writeable field, complete **all** applicable layers in the **same slice**. Do not ship backend-only or frontend-only limits.

| Layer | What to add |
|-------|-------------|
| Domain factory / mutator | `ArgumentException.ThrowIfNullOrWhiteSpace`; max-length / fixed-size checks; trim; email → `ToLowerInvariant()` |
| EF configuration | `HasMaxLength`, `IsRequired`, unique indexes; `HasCheckConstraint` for non-empty strings, enum string sets, hash length |
| Migration | Generated from EF config; never hand-edit snapshot |
| FluentValidation | `NotEmpty` / `EmailAddress` / `MinimumLength` / `MaximumLength`; optional fields: `.When(x => !string.IsNullOrWhiteSpace(...))` |
| Shared DTO | Matching `[StringLength]` / `[Required]` / `[EmailAddress]` |
| Angular models | `*_FIELD_LIMITS` constant beside DTOs (auth: `AUTH_FIELD_LIMITS` in `core/auth/auth.models.ts`) |
| Angular form | Same limits via `Validators.maxLength` / `minLength` + `requiredTrimmed` from `shared/validators`; HTML `[attr.maxlength]`; inline error messages |
| Submit payload | Trim strings; omit blank optionals; email `.toLowerCase()` |

### Auth field limits (canonical)

| Field | Max | Notes |
|-------|-----|--------|
| LicenseKey | 100 | Signup only |
| Email | 320 | Unique, stored lowercase |
| Password | 128 | Min 8 on signup; max on login too |
| ShopName | 200 | |
| DisplayName | 200 | Optional; blank → shop name |
| Phone | 50 | Optional |
| LicenseLookupHash | 64 | Exact SHA-256 hex (domain + DB check) |

Enums with DB CHECKs: `UserRole` → `Owner` \| `Assistant`; `WhatsAppConnectionStatus` → `Disconnected` \| `Connected` \| `Error`; `StockMovementType` → `Adjustment` \| `Reserve` \| `Deduct` \| `Release`.

### Product field limits (canonical)

Canonical constants: `OrderFlow.Domain.ProductConstraints`.

| Field | Max | Notes |
|-------|-----|--------|
| Name | 200 | Required, trimmed |
| Sku | 50 | Required; trim + uppercase; unique per shop |
| Category | 80 | Optional |
| Price | 999,999,999.99 | ≥ 0; stored `numeric(12,2)` |
| Stock | 99,999,999 | ≥ 0; change only via `POST .../stock` |
| LowStockThreshold | 99,999,999 | ≥ 0 |
| Notes | 400 | Optional; stock adjustment only |
| Version | — | `long`, starts at 1; optimistic concurrency |

Angular `PRODUCT_FIELD_LIMITS` lives in `features/products/data/product.models.ts` and must stay in sync with these values.

### New entity checklist

Copy and tick when implementing a feature entity:

```
- [ ] Domain factory guards + normalization
- [ ] EF MaxLength / Required / indexes / CHECK (enums, non-empty, numeric ranges)
- [ ] FluentValidation on every write command
- [ ] Shared DTO annotations match
- [ ] Angular FIELD_LIMITS + validators + maxlength + errors
- [ ] Submit trim / lowercase email / omit empty optionals
- [ ] Validator unit tests; migration if schema changed
- [ ] XML docs on public C# types/members; JSDoc on exported Angular APIs
```

## Frontend conventions

```
frontend/src/app/
  core/
    auth/             # AuthService, guards, interceptor, models (+ AUTH_FIELD_LIMITS), auth HTTP
    layout/           # ShellComponent — desktop sidebar, mobile top/bottom nav
    shop/             # ShopStateService (shopId, shopName, plan Signals)
  shared/
    pipes/            # ghsCurrency
    validators/       # requiredTrimmed, passwordsMatch, shared form helpers
  features/           # mirrors Application/Features + API controllers
    auth/
      pages/login/    # Auth Gateway UI
      routes.ts
    landing/
      pages/landing/  # Marketing home at `/`
      routes.ts
    dashboard/
      pages/dashboard/
      data/               # DashboardDto + dashboard.api.ts
      routes.ts
    products/
      data/               # ProductDto, PRODUCT_FIELD_LIMITS, product.api.ts
      pages/product-list/
      pages/product-form/ # /new and /:id
      routes.ts
    # Slice 3+: orders/{ data/, pages/, routes.ts }
  app.routes.ts       # compose lazy feature routes
  environments/environment.ts   # apiUrl http://localhost:5180
```

### Routing

| Path | Guard | Loads |
|------|-------|--------|
| `/` | — | `features/landing/routes` (marketing) |
| `/login` | `guestGuard` | `features/auth/routes` |
| `/app` | `authGuard` | `core/layout/ShellComponent` |
| `/app` (child `''`) | — | `features/dashboard/routes` |
| `/app/products` | — | `features/products/routes` (list) |
| `/app/products/new` | — | add product |
| `/app/products/:id` | — | edit product + stock adjust |

Future children under `/app`: `orders`, `settings` (add nav links in shell only when routes exist).

### Rules

- Standalone components; each feature exports `ROUTES` / `AUTH_ROUTES` / `DASHBOARD_ROUTES` from `routes.ts`
- JWT in `localStorage` key `orderflow.token`; interceptor attaches `Authorization: Bearer`
- Tailwind tokens: `forest`, `forest-dark`, `gold`, `paper`, `ink`; font Source Sans 3 (Fraunces display on landing headlines only)
- **Signals:** local UI in components; shop/plan in `ShopStateService` (updated by `AuthService` on login/me/logout). No NgRx. Use `takeUntilDestroyed()` for RxJS cleanup.
- **Layering:** `core` must not import `features`. Feature `data/` owns HTTP + DTO models + `*_FIELD_LIMITS` for domain features. Auth HTTP + `AUTH_FIELD_LIMITS` stay in `core/auth`.
- **DTO mirror:** TypeScript interfaces match `OrderFlow.Shared/DTOs` camelCase — never Domain entities. Limits constants must match Shared `[StringLength]` values.
- **Form constraints:** never rely on API 400 alone; client validators + `maxlength` must match backend before submit.
- When adding a feature (e.g. products): create `features/products/{data,pages,routes}`, register under `/app` children, extend shell nav, apply the constraints checklist above.
- **Documentation:** JSDoc on exported feature APIs — see [documentation.md](documentation.md).

## Documentation conventions

XML/JSDoc and inline-comment rules: [documentation.md](documentation.md).

## Logging

Serilog is wired in `Program.cs` via `UseSerilog()`. `SecretRedactingPolicy` redacts `LicenseKey`, `Password`, `ConfirmPassword`, `IntegrationKey`, and `ProtectedLicenseKey`. Console in Development/Production; rolling file `logs/orderflow-.log` (gitignored). Testing environment: Warning minimum, no console/file sinks. Never log plaintext license keys or integration keys. External HTTP (Paystack, WhatsApp) should log sanitized request/response at Information when those adapters ship.

## Ports and config

| Service | URL / value |
|---------|-------------|
| OrderFlow API | http://localhost:5180 |
| Angular | http://localhost:4200 |
| OrderFlow Postgres | localhost:5433, db `orderflow_db`, user `orderflow` |
| Platform API | http://localhost:5176 |
| JWT issuer / audience | `OrderFlow.Api` / `OrderFlow.Frontend` |

CORS origins: `Cors:Origins` (default `http://localhost:4200`).

### Environment variable mapping

Use `__` (double underscore) for nested .NET config (e.g. `Platform:BaseUrl` → `PLATFORM__BASEURL`).

| Env Var | Required | Default | Used For |
| :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | No | Development | Environment detection |
| `PLATFORM__BASEURL` | **Yes** | http://localhost:5176 | Platform API base |
| `PLATFORM__INTEGRATIONKEY` | **Yes** | (Dev key) | X-Integration-Key header |
| `JWT__KEY` | **Yes** | (Dev key) | Token signing (≥64 chars in prod) |
| `CORS__ORIGINS` | No | http://localhost:4200 | Comma-separated allowed origins |
| `ConnectionStrings__DefaultConnection` | **Yes** (non-Dev) | Docker compose value | PostgreSQL |

## Testing strategy

- **Unit tests:** xUnit, NSubstitute, FluentAssertions. Each command/query needs a handler unit test and a validator test. Mock `IPlatformLicenseClient` and other adapters.
- **Integration tests:** **Testcontainers.PostgreSql** (not EF InMemory). InMemory does not enforce relational constraints and diverges from PostgreSQL. Cover the full HTTP pipeline (Auth → Controller → Handler → DB) with an ephemeral Postgres container in the test fixture.
- Testing environment: skip host `MigrateAsync` as appropriate; apply migrations against the container; stub external HTTP clients.
