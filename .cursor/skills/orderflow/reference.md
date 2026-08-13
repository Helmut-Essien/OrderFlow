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

**Shop:** Id, Name, Phone?, Address?, LicenseLookupHash, ProtectedLicenseKey, PlanName, PlanExpiresAt?, PlanUnrecognized, WhatsAppConnectionStatus, CreatedAt, UpdatedAt

**User:** Id, ShopId, Email (unique, lowercase), DisplayName, PasswordHash, Role (`Owner` \| `Assistant`), CreatedAt

Roles: `UserRole`. WhatsApp: `Disconnected` \| `Connected` \| `Error`.

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
  {Verb}{Name}CommandValidator.cs
```

Register happens automatically via `AddMediatR` + `AddValidatorsFromAssembly`. Controllers send MediatR requests only.

New external systems: interface in `Application/Common/Interfaces`, adapter in `Infrastructure/{Area}/`.

## Shared DTOs

`OrderFlow.Shared/DTOs` contains **public, external-facing contracts** used by the Angular frontend (e.g. `AuthResponse`, `ProductDto`, `OrderListDto`). Internal MediatR responses can be simple records or DTOs, but must not expose Domain entities directly. Use AutoMapper or explicit mapping (`ProductDto.FromEntity(product)`) inside the handler, not in the Controller.

## Frontend conventions

```
frontend/src/app/
  core/auth/          # AuthService, guards, interceptor, models
  features/auth/      # sign in / sign up
  features/dashboard/
  environments/environment.ts   # apiUrl http://localhost:5180
```

- Standalone components, lazy `loadComponent` routes
- `authGuard` on app shell, `guestGuard` on `/login`
- JWT in `localStorage` key `orderflow.token`; interceptor attaches `Authorization: Bearer`
- Tailwind tokens: `forest`, `forest-dark`, `gold`, `paper`, `ink`; font Source Sans 3
- **Signals:** `signal` for local component state; `toSignal` for HTTP observables. For cross-feature state (current shop, plan limits), injectable `StateService` with `signals` + `computed`. Do **not** bring NgRx or other external state managers into the MVP. Use `takeUntilDestroyed()` for automatic RxJS cleanup.

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
