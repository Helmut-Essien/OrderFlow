---
name: orderflow
description: >-
  Guides the OrderFlow B2B SaaS: Angular 19 + Tailwind frontend, ASP.NET Core 9
  Clean Architecture API, PostgreSQL, JWT, Platform license validation, WhatsApp
  orders, inventory, Paystack. Use for this repo, any implementation slice,
  auth, shops, products, orders, WhatsApp, payments, plan limits, migrations,
  or local dev. Delivers one slice at a time per user confirmation.
---

# OrderFlow

Standalone WhatsApp-native order and inventory SaaS for small retailers in Ghana. License entitlement lives in the **Platform** hub (`../platform`). OrderFlow calls only `POST /api/licenses/validate`.

## Vision

1. Shop owners sign up with a Platform license key, then run the shop from a mobile-first Angular dashboard
2. Customers place orders on the shop WhatsApp number (one number per shop in MVP)
3. Real-time inventory, order statuses, Paystack payment links, simple dashboard numbers
4. Plan limits (products, orders/month, users) are **enforced in OrderFlow**, mapped from Platform `planName`

**Non-goals (MVP):** native mobile apps, AI parsing, multi-branch, multi-currency, delivery/riders, Redis, multiple WhatsApp numbers per shop.

## Delivery rules

1. **One slice at a time** — full code, tests, config, then stop.
2. **Ask for confirmation** before the next slice.
3. No placeholders (`// TODO`, `// add logic here`).
4. External setup (Postgres, Platform) → `docker-compose` or clear notes in [README.md](../../../README.md).
5. See [phases.md](phases.md) for slice scope. See [reference.md](reference.md) for APIs, entities, and frontend conventions.

## Technology stack

| Layer | Technology | Status |
|-------|------------|--------|
| Frontend | Angular 19 standalone + Tailwind 3 | Auth + empty dashboard |
| Backend | ASP.NET Core 9 Web API, MediatR, FluentValidation | Auth slice |
| ORM | EF Core 9 + Npgsql | Active |
| Database | PostgreSQL 16 (Docker, port 5433) | Active |
| IDs | NUlid string PKs | Active |
| Auth | OrderFlow-issued JWT (not Platform JWT) | Active |
| Passwords | BCrypt.Net-Next | Active |
| License keys | SHA-256 lookup hash + Data Protection (never log plaintext) | Active |
| Platform | `IPlatformLicenseClient` → `X-Integration-Key` | Active |
| WhatsApp | `IWhatsAppClient` (not implemented) | Later slice |
| Payments | `IPaymentGateway` / Paystack (not implemented) | Later slice |

## Solution layout

```
OrderFlow.sln
frontend/                          # Angular project name: frontend
backend/src/
  OrderFlow.Api/                   # Controllers, middleware, Program.cs
  OrderFlow.Application/           # Features, handlers, validators, interfaces
  OrderFlow.Domain/                # Entities, enums, PlanQuota
  OrderFlow.Infrastructure/        # EF, Platform HTTP, JWT, adapters
  OrderFlow.Shared/                # DTOs, constants
backend/tests/
```

Root namespaces match project names (`OrderFlow.Api`, `OrderFlow.Application`, …). Do not rename `frontend` or the `backend/` folder.

## Strict layering

| Type | Location | Never in |
|------|----------|----------|
| Entities / invariants | `OrderFlow.Domain` | Api, Shared, frontend |
| Use cases / handlers | `OrderFlow.Application/Features/{Feature}/` | Api, Infrastructure (except tests) |
| Persistence, HTTP clients, JWT | `OrderFlow.Infrastructure` | Domain, Application implementations |
| Request/response DTOs | `OrderFlow.Shared/DTOs/` | Domain entities exposed to HTTP |
| Controllers / webhooks | `OrderFlow.Api` | Business rules |
| UI | `frontend/src/app/features/` | Backend projects |

Application depends on Domain + Shared only. Infrastructure implements Application interfaces. Api wires both.

## Security and tenancy

- **Shop** is the tenant. Every business table has `ShopId`. EF global query filter from JWT `shopId` when present.
- First visit: `POST /api/auth/signup` (license key + owner email/password + shop name).
- Later: `POST /api/auth/login` (email/password only). License keys are not used at login.
- JWT claims: `sub` / NameIdentifier = userId, `shopId`, `role`, `planName`.
- Platform identity of a customer is the **license key**. Store lookup hash + Data Protection payload. Never log plaintext keys or integration keys.
- OrderFlow must **not** call Platform admin JWT APIs (`/api/customers`, `/api/licenses` CRUD).
- Unknown Platform `planName` → Starter limits + `PlanUnrecognized` dashboard warning.

## Coding standards

- Nullable reference types, async/await, constructor DI
- Feature folders + MediatR commands/queries + FluentValidation
- Private entity setters; factory methods (`Shop.Create`, `User.CreateOwner`)
- Enums stored as PostgreSQL strings
- Angular: standalone components, `inject()`, feature lazy routes, Tailwind utility classes
- UI: mobile-first; forest `#0F6B4C` + gold `#C9A227` + paper `#F3EEE3`
- Tests required for new Application handlers; mock `IPlatformLicenseClient` and other adapters
- Commits only when the user asks; never commit production secrets

## Local development

```bash
# Platform (required for real license checks) — ../platform
docker compose up -d
dotnet run --project API/API.csproj          # http://localhost:5176

# OrderFlow
docker compose up -d                         # Postgres :5433
dotnet run --project backend/src/OrderFlow.Api/OrderFlow.Api.csproj   # :5180
cd frontend && npm start                     # :4200
```

Dev sign up: license `ORDERFLOW-DEVK-TEST`, any email, password ≥ 8 chars.

```bash
dotnet test OrderFlow.sln
dotnet ef migrations add Name --project backend/src/OrderFlow.Infrastructure --startup-project backend/src/OrderFlow.Api --output-dir Persistence/Migrations
```
