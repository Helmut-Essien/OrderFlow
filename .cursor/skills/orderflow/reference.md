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

Sign-up body: `licenseKey`, `email`, `password`, `shopName`, `displayName?`, `phone?`.  
Login body: `email`, `password` (no license key).  
Auth response: `token`, `expiresAt`, `shopId`, `shopName`, `userId`, `email`, `displayName`, `role`, `plan`.

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

Roles: `UserRole`. WhatsApp: `Disconnected` \| `Connected` \| `Error`. DB CHECKs enforce enum sets and non-empty required strings — see [Constraints](#constraints-full-stack).

## Domain (planned — add when that slice starts)

- **Product** — sku, name, price GHS, stock qty, low-stock threshold, active, **concurrency token** (`Version` long or `RowVersion` byte[])
- **StockMovement** — adjustments and reservations
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
  {Verb}{Name}Command.cs          # or Query
  {Verb}{Name}CommandHandler.cs
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

Enums with DB CHECKs: `UserRole` → `Owner` \| `Assistant`; `WhatsAppConnectionStatus` → `Disconnected` \| `Connected` \| `Error`.

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
    dashboard/
      pages/dashboard/
      routes.ts
    # Slice 2+: products/{ data/, pages/, routes.ts }
    # Slice 3+: orders/{ data/, pages/, routes.ts }
  app.routes.ts       # compose lazy feature routes
  environments/environment.ts   # apiUrl http://localhost:5180
```

### Routing

| Path | Guard | Loads |
|------|-------|--------|
| `/login` | `guestGuard` | `features/auth/routes` |
| `/app` | `authGuard` | `core/layout/ShellComponent` |
| `/app` (child `''`) | — | `features/dashboard/routes` |
| `/` | — | redirect → `/app` |

Future children under `/app`: `products`, `orders`, `settings` (add nav links in shell only when routes exist).

### Rules

- Standalone components; each feature exports `ROUTES` / `AUTH_ROUTES` / `DASHBOARD_ROUTES` from `routes.ts`
- JWT in `localStorage` key `orderflow.token`; interceptor attaches `Authorization: Bearer`
- Tailwind tokens: `forest`, `forest-dark`, `gold`, `paper`, `ink`; font Source Sans 3
- **Signals:** local UI in components; shop/plan in `ShopStateService` (updated by `AuthService` on login/me/logout). No NgRx. Use `takeUntilDestroyed()` for RxJS cleanup.
- **Layering:** `core` must not import `features`. Feature `data/` owns HTTP + DTO models + `*_FIELD_LIMITS` for domain features. Auth HTTP + `AUTH_FIELD_LIMITS` stay in `core/auth`.
- **DTO mirror:** TypeScript interfaces match `OrderFlow.Shared/DTOs` camelCase — never Domain entities. Limits constants must match Shared `[StringLength]` values.
- **Form constraints:** never rely on API 400 alone; client validators + `maxlength` must match backend before submit.
- When adding a feature (e.g. products): create `features/products/{data,pages,routes}`, register under `/app` children, extend shell nav, apply the constraints checklist above.

## Logging

Implement **Serilog** with `Destructure` / a custom `IDestructuringPolicy` to automatically redact `LicenseKey`, `Password`, and `IntegrationKey` from logs. In `Program.cs`, use `UseSerilog()`. Log to console in Development and to a file / Application Insights in Production. All external HTTP calls (Platform, Paystack, WhatsApp) log at `Information` with request/response sanitized. Never log plaintext license keys or integration keys.

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
