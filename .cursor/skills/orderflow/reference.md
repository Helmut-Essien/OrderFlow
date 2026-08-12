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

Production: `Platform:IntegrationKey` and `Jwt:Key` via env / user secrets.

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
- `NotFoundAppException` → 404
- `ForbiddenAppException` → 403
- FluentValidation `ValidationException` → 400

## Domain (implemented)

**Shop:** Id, Name, Phone?, Address?, LicenseLookupHash, ProtectedLicenseKey, PlanName, PlanExpiresAt?, PlanUnrecognized, WhatsAppConnectionStatus, CreatedAt, UpdatedAt

**User:** Id, ShopId, Email (unique, lowercase), DisplayName, PasswordHash, Role (`Owner` \| `Assistant`), CreatedAt

Roles: `UserRole`. WhatsApp: `Disconnected` \| `Connected` \| `Error`.

## Domain (planned — add when that slice starts)

- **Product** — sku, name, price GHS, stock qty, low-stock threshold, active
- **StockMovement** — adjustments and reservations
- **Customer** — shop’s WhatsApp end-customer (not Platform Customer)
- **Order** — status `Pending \| Confirmed \| Paid \| Fulfilled \| Cancelled`, source `WhatsApp \| Manual`
- **OrderLine** — product, qty, unit price
- **Payment** — Paystack reference, status, amount

**Stock rule:** reserve on Confirmed, deduct on Paid, release on Cancelled. Pending WhatsApp drafts do not touch stock.

## Application feature pattern

```
Features/{Name}/
  {Verb}{Name}Command.cs          # or Query
  {Verb}{Name}CommandHandler.cs
  {Verb}{Name}CommandValidator.cs
```

Register happens automatically via `AddMediatR` + `AddValidatorsFromAssembly`. Controllers send MediatR requests only.

New external systems: interface in `Application/Common/Interfaces`, adapter in `Infrastructure/{Area}/`.

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

## Ports and config

| Service | URL / value |
|---------|-------------|
| OrderFlow API | http://localhost:5180 |
| Angular | http://localhost:4200 |
| OrderFlow Postgres | localhost:5433, db `orderflow_db`, user `orderflow` |
| Platform API | http://localhost:5176 |
| JWT issuer / audience | `OrderFlow.Api` / `OrderFlow.Frontend` |

CORS origins: `Cors:Origins` (default `http://localhost:4200`).

Testing environment: skip `MigrateAsync`; WebApplicationFactory uses InMemory + stub `IPlatformLicenseClient`.
