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

JSON camelCase. Auth rate limit policy `auth`: 20 req/min **per client IP** (not global). Anonymous `GET /health` (liveness).

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
| GET | `/api/orders` | JWT | `ListOrdersQuery` (`search`, `status`, `page`, `pageSize` 1–100, default 20) |
| GET | `/api/orders/{id}` | JWT | `GetOrderQuery` |
| POST | `/api/orders` | JWT | `CreateOrderCommand` (`confirmImmediately` reserves in the same transaction) |
| POST | `/api/orders/{id}/status` | JWT | `ChangeOrderStatusCommand` (`Status`, `ExpectedVersion`) |
| GET | `/health` | Anonymous | ASP.NET health checks (process up) |

Sign-up body: `licenseKey`, `email`, `password`, `shopName`, `displayName?`, `phone?`.  
Login body: `email`, `password` (no license key).  
Auth response: `token`, `expiresAt`, `shopId`, `shopName`, `userId`, `email`, `displayName`, `role`, `plan`.

Product create: `name`, `sku` (stored uppercase), `category?`, `price`, `stock`, `lowStockThreshold`.  
Product update: same except no `stock`; plus `isActive`, `expectedVersion`.  
SKU unique per shop. Create is blocked at `PlanQuota.MaxProducts` (403). **Cap counts active SKUs only** — inactive products do not consume a slot (deactivating frees one). Duplicate SKU → 409. Stale `expectedVersion` → 409 `{ "code": "concurrency" }`. `Version` is an API concurrency token — do not show it in shop-facing UI.

Dashboard: `todaysSales` and `orderCount` use the UTC date of `PaidAt` for orders still `Paid` or `Fulfilled` (cancelled sales are excluded; Ghana is UTC). `pendingWhatsAppCount` is WhatsApp+Pending (0 until that slice). `recentOrders` is the 10 newest. `lowStock` is active products with `stock <= lowStockThreshold` (max 50).

Order create: `customerName`, `customerPhone?`, `notes?`, `confirmImmediately`, `lines[]` (`productId`, `quantity`). Prices are snapshotted from the catalog. Duplicate product ids → 400. Inactive product → 409. Starter monthly cap → 403. Confirm with insufficient stock → 409.

Order status: `Pending → Confirmed → Paid → Fulfilled`, or `Cancelled` from Pending/Confirmed/Paid. Stale `expectedVersion` → 409 `{ "code": "concurrency" }`. Illegal jump (e.g. Pending → Paid) → 409. `Version` is an API concurrency token — do not show it in shop-facing UI.

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

**StockMovement:** Id, ShopId, ProductId, QuantityDelta, ResultingStock (0–99,999,999), Type (`Adjustment` \| `Reserve` \| `Deduct` \| `Release`), Notes? (≤400), CreatedByUserId?, CreatedAt. Manual adjust writes `Adjustment`. Orders write `Reserve` / `Deduct` (audit only after reserve) / `Release`.

**Order:** Id, ShopId, CustomerName (≤200), CustomerPhone? (≤50), Notes? (≤400), Status (`Pending` \| `Confirmed` \| `Paid` \| `Fulfilled` \| `Cancelled`), Source (`Manual` \| `WhatsApp`), NeedsClarification, TotalAmount (GHS, numeric 18,2), Version (concurrency token, ≥1), CreatedByUserId?, CreatedAt, UpdatedAt, ConfirmedAt?, PaidAt?, FulfilledAt?, CancelledAt?

**OrderLine:** Id, OrderId, ShopId, ProductId, ProductName snapshot (≤200), Sku snapshot (≤50, uppercase), Quantity (1–99,999,999), UnitPrice (0–999,999,999.99, numeric 12,2), LineTotal (numeric 18,2). Unique product per order.

Roles: `UserRole`. WhatsApp: `Disconnected` \| `Connected` \| `Error`. DB CHECKs enforce enum sets and non-empty required strings — see Constraints below.

## Domain (planned — add when that slice starts)

- **Customer** — shop’s WhatsApp end-customer (not Platform Customer); manual orders store name/phone on the order
- **Payment** — Paystack reference, status, amount

**Stock rule:** reserve on Confirmed (atomic decrement), deduct on Paid is an audit movement only (stock already held), release on Cancelled from Confirmed or Paid. Pending WhatsApp drafts do not touch stock.

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

Register happens automatically via `AddMediatR` + `AddValidatorsFromAssembly`. Controllers send MediatR requests only. List/get handlers: `AsNoTracking`, page/cap results, filter in SQL ([performance.md](performance.md)).

New external systems: interface in `Application/Common/Interfaces`, adapter in `Infrastructure/{Area}/`. Timeouts on outbound HTTP. Webhooks verify signatures before any write ([production.md](production.md)).

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

Enums with DB CHECKs: `UserRole` → `Owner` \| `Assistant`; `WhatsAppConnectionStatus` → `Disconnected` \| `Connected` \| `Error`; `StockMovementType` → `Adjustment` \| `Reserve` \| `Deduct` \| `Release`; `OrderStatus` → `Pending` \| `Confirmed` \| `Paid` \| `Fulfilled` \| `Cancelled`; `OrderSource` → `Manual` \| `WhatsApp`.

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

### Order field limits (canonical)

Canonical constants: `OrderFlow.Domain.OrderConstraints`.

| Field | Max | Notes |
|-------|-----|--------|
| CustomerName | 200 | Required, trimmed |
| CustomerPhone | 50 | Optional |
| Notes | 400 | Optional |
| Lines | 50 | At least one; unique `productId` per order |
| Quantity | 99,999,999 | Per line, min 1 |
| Version | — | `long`, starts at 1; optimistic concurrency on status |

Angular `ORDER_FIELD_LIMITS` lives in `features/orders/data/order.models.ts` and must stay in sync with these values.

### New entity checklist

Copy and tick when implementing a feature entity:

```
- [ ] Domain factory guards + normalization
- [ ] EF MaxLength / Required / indexes / CHECK (enums, non-empty, numeric ranges)
- [ ] FluentValidation on every write command
- [ ] Shared DTO annotations match
- [ ] Angular FIELD_LIMITS + validators + maxlength + errors
- [ ] Submit trim / lowercase email / omit empty optionals
- [ ] Validator unit tests for **every** write/query validator (auth included); migration if schema changed
- [ ] List endpoints: assert `pageSize` above 100 is 400
- [ ] XML docs on **every** public C# member (CS1591 is an error); JSDoc on **every** exported HTTP method (`list`/`get`/`create`/`update`/…)
- [ ] Production: no new secrets in `appsettings.json`; extend `StartupConfiguration` if a Production-only setting is required ([production.md](production.md))
- [ ] Performance: paged list, `AsNoTracking` reads, **SQL `Select` to DTOs** on list/dashboard, EF indexes, Angular `OnPush` + `@for track` ([performance.md](performance.md))
- [ ] Plan-cap writes: lock + count + insert in one transaction; integration-test concurrent creates at the cap
```

## Frontend conventions

```
frontend/src/app/
  core/
    auth/             # AuthService, guards, interceptor, models (+ AUTH_FIELD_LIMITS), auth HTTP
    layout/           # ShellComponent — sidebar lg+; top bar + bottom nav below lg (safe-area padded)
    seo/              # SeoService + LANDING_SEO copy (title/description/JSON-LD)
    not-found/        # Public 404 (`noindex`) — unknown URLs must not redirect to `/`
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
    orders/
      data/               # OrderDto, ORDER_FIELD_LIMITS, order.api.ts
      pages/order-list/
      pages/order-form/   # /new
      pages/order-detail/ # /:id + status actions
      routes.ts
  app.routes.ts       # compose lazy feature routes
  app.routes.server.ts # prerender `/` and `/404`; client-render `/login` and `/app`
  environments/
    environment.ts              # apiUrl http://localhost:5180, siteUrl http://localhost:4200 (ng serve)
    environment.production.ts   # apiUrl '' same-origin /api; siteUrl from public-origin.generated.ts
    public-origin.generated.ts  # ORDERFLOW_SITE_URL at build; empty until set
```

### Routing

| Path | Guard | Loads |
|------|-------|--------|
| `/` | — | `features/landing/routes` (marketing, prerendered) |
| `/login` | `guestGuard` | `features/auth/routes` (`noindex`) |
| `/app` | `authGuard` | `core/layout/ShellComponent` (`noindex`) |
| `/404` and `**` | — | `core/not-found` (`noindex`; never redirect unknown URLs to `/`) |
| `/app` (child `''`) | — | `features/dashboard/routes` |
| `/app/products` | — | `features/products/routes` (list) |
| `/app/products/new` | — | add product |
| `/app/products/:id` | — | edit product + stock adjust |
| `/app/orders` | — | `features/orders/routes` (list) |
| `/app/orders/new` | — | create manual order |
| `/app/orders/:id` | — | order detail + status actions |

Future children under `/app`: `settings`.

### Rules

- Standalone components with `ChangeDetectionStrategy.OnPush`; each feature exports `ROUTES` / `AUTH_ROUTES` / `DASHBOARD_ROUTES` from `routes.ts`
- JWT in `localStorage` key `orderflow.token`; interceptor attaches `Authorization: Bearer`. Read/write storage only in the browser (`isPlatformBrowser`) so prerender of `/` does not crash.
- Tailwind tokens: `forest`, `forest-dark`, `gold`, `paper`, `ink`; font Source Sans 3 (Fraunces display on landing headlines only)
- **Signals:** local UI in components; shop/plan in `ShopStateService` (updated by `AuthService` on login/me/logout). No NgRx. Use `takeUntilDestroyed()` for RxJS cleanup. Debounce search (~300ms) before list HTTP. `@for` tracks entity `id`.
- **Layering:** `core` must not import `features`. Feature `data/` owns HTTP + DTO models + `*_FIELD_LIMITS` for domain features. Auth HTTP + `AUTH_FIELD_LIMITS` stay in `core/auth`.
- **DTO mirror:** TypeScript interfaces match `OrderFlow.Shared/DTOs` camelCase — never Domain entities. Limits constants must match Shared `[StringLength]` values.
- **Form constraints:** never rely on API 400 alone; client validators + `maxlength` must match backend before submit.
- When adding a feature (e.g. products): create `features/products/{data,pages,routes}`, register under `/app` children, extend shell nav (bottom nav + sidebar, `lg` split), apply the constraints checklist above.
- **Mobile layout:** follow [orderflow-ui-ux](../orderflow-ui-ux/SKILL.md) — sidebar only at `lg+`, cards until `lg`, `w-full sm:w-auto` primary actions, `env(safe-area-inset-*)` on sticky/fixed chrome.
- **Documentation:** JSDoc on exported feature APIs — see [documentation.md](documentation.md).
- **Production SPA:** `environment.production.ts` keeps `apiUrl: ''`; `angular.json` production `fileReplacements` must stay. Set `siteUrl` to the public HTTPS origin before a marketing deploy. See [production.md](production.md) and [performance.md](performance.md).
- **Landing SEO:** prerender `/` with `outputMode: static` (no Node SSR server). The document shell (`index.html`) is `noindex` with no OG/JSON-LD so `/login` and `/app` CSR fallbacks cannot rank or look like the homepage. `SeoService.applyMarketingHome()` on the landing writes indexable tags during prerender; `applyPrivatePage()` on login, shell, and 404. Keep copy in `core/seo/seo.content.ts`. `robots.txt` allows `/` and disallows `/login`, `/app`, `/404`. Set `ORDERFLOW_SITE_URL` (no trailing slash) before `npm run build` for absolute canonical/OG/sitemap. Do not put fake GHS prices in JSON-LD — plans are license-backed. Unknown routes render `NotFoundComponent`; never `redirectTo: ''`.

## Documentation conventions

XML/JSDoc and inline-comment rules: [documentation.md](documentation.md).

## Logging

Serilog is wired in `Program.cs` via `UseSerilog()`. `SecretRedactingPolicy` redacts `LicenseKey`, `Password`, `ConfirmPassword`, `IntegrationKey`, and `ProtectedLicenseKey`. Console in Development and Production; rolling file `logs/orderflow-.log` in **Development only** (gitignored). Production hosts collect stdout. Testing environment: Warning minimum, no console/file sinks. Never log plaintext license keys or integration keys. External HTTP (Paystack, WhatsApp) should log sanitized request/response at Information when those adapters ship.

## Ports and config

| Service | URL / value |
|---------|-------------|
| OrderFlow API | http://localhost:5180 |
| Angular | http://localhost:4200 |
| OrderFlow Postgres | localhost:5433, db `orderflow_db`, user `orderflow` |
| Platform API | http://localhost:5176 |
| JWT issuer / audience | `OrderFlow.Api` / `OrderFlow.Frontend` |

CORS origins: `Cors:Origins` JSON array or comma-separated `CORS__ORIGINS`. Development default `http://localhost:4200`. Production empty = same-origin SPA only (`CorsOrigins.Resolve`).

Production refuses to start on Development JWT/integration keys, `Password=orderflow_dev`, `Include Error Detail`, or a localhost Platform URL (`StartupConfiguration`). JWT key ≥ 64 characters. Persist Data Protection keys (`DataProtection__KeysPath`). Dev secrets live in `appsettings.Development.json`; that file is not published.

### Environment variable mapping

Use `__` (double underscore) for nested .NET config (e.g. `Platform:BaseUrl` → `PLATFORM__BASEURL`).

| Env Var | Required | Default | Used For |
| :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | No | Development | Environment detection |
| `PLATFORM__BASEURL` | **Yes** | http://localhost:5176 | Platform API base |
| `PLATFORM__INTEGRATIONKEY` | **Yes** | (Dev key) | X-Integration-Key header |
| `JWT__KEY` | **Yes** | (Dev key) | Token signing (≥64 chars in prod) |
| `CORS__ORIGINS` | No | (empty in base json; Dev uses localhost:4200) | Comma-separated allowed origins |
| `ConnectionStrings__DefaultConnection` | **Yes** (non-Dev) | Docker compose value | PostgreSQL (no `Include Error Detail` in Production) |
| `DataProtection__KeysPath` | **Yes** (Production volume) | `dataprotection-keys` | License-key encryption key ring |

## Testing strategy

- **Unit tests:** xUnit, NSubstitute, FluentAssertions. Each command/query needs a handler unit test and a validator test. Mock `IPlatformLicenseClient` and other adapters.
- **Integration tests:** **Testcontainers.PostgreSql** (not EF InMemory). InMemory does not enforce relational constraints and diverges from PostgreSQL. Cover the full HTTP pipeline (Auth → Controller → Handler → DB) with an ephemeral Postgres container in the test fixture.
- Testing environment: skip host `MigrateAsync` as appropriate; apply migrations against the container; stub external HTTP clients.
- New Production fail-fast rules: unit-test `StartupConfiguration`. New list endpoints: assert page size is capped (HTTP 400). Auth/CORS: failed login with `Origin` must still return `Access-Control-Allow-Origin`.
