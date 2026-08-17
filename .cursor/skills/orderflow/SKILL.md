---
name: orderflow
description: >-
  Guides the OrderFlow B2B SaaS: Angular 19 + Tailwind frontend, ASP.NET Core 9
  Clean Architecture API, PostgreSQL, JWT, Platform license validation, WhatsApp
  orders, inventory, Paystack. Generates production-ready, performance-optimized
  C# and Angular (fail-fast Production config, same-origin SPA, indexed paged
  queries, OnPush Signals). Enforces full-stack field constraints (Domain, EF
  checks, FluentValidation, Shared DTOs, Angular limits/validators) in the same
  slice. Generated code must be documented (XML/JSDoc). Use for this repo, any
  implementation slice, auth, shops, products, orders, WhatsApp, payments, plan
  limits, migrations, validations, production hardening, performance, landing
  SEO/prerender, or local dev. Delivers one slice at a time per user confirmation.
---

# OrderFlow

Standalone WhatsApp-native order and inventory SaaS for small retailers in Ghana. License entitlement lives in the **Platform** hub (`../platform`). OrderFlow calls only `POST /api/licenses/validate`. Companion files: [phases.md](phases.md), [reference.md](reference.md), [documentation.md](documentation.md), [production.md](production.md), [performance.md](performance.md).

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
5. See [phases.md](phases.md) for slice scope. See [reference.md](reference.md) for APIs, entities, and frontend conventions. See [documentation.md](documentation.md) for XML/JSDoc rules.
6. **Constraints are full-stack** — any new/changed field limit, requiredness, enum set, or normalization ships in the **same slice** on Domain + EF + FluentValidation + Shared DTOs **and** Angular (limits constant, validators, `maxlength`, submit normalize). Never leave backend-only or frontend-only constraints. Checklist: [reference.md](reference.md).
7. **Document generated code** — every public C# member gets XML (`///`) docs (CS1591 is a src build error); exported Angular APIs get JSDoc (`/** */`). Inline comments explain **why** (invariants, tenancy, concurrency, security), never the next obvious line. Details: [documentation.md](documentation.md).
8. **Production-ready by default** — generate for Production + `ng build`, not only `dotnet run` / `ng serve`. Fail-fast secrets, same-origin SPA, health/security headers, no localhost in the production bundle. Apply [production.md](production.md) in the same slice.
9. **Performance by default** — paged indexed SQL, `AsNoTracking` reads, atomic stock updates, lazy feature routes, `OnPush` + Signals, `@for` track by id, debounced search. Apply [performance.md](performance.md) in the same slice. Do not add Redis.

## Technology stack

| Layer | Technology | Status |
|-------|------------|--------|
| Frontend | Angular 19 standalone + Tailwind 3 + Signals | Auth + products + dashboard + orders |
| Backend | ASP.NET Core 9 Web API, MediatR, FluentValidation | Auth + products + dashboard + orders |
| ORM | EF Core 9 + Npgsql | Active |
| Database | PostgreSQL 16 (Docker, port 5433) | Active |
| IDs | NUlid string PKs | Active |
| Auth | OrderFlow-issued JWT (not Platform JWT) | Active |
| Passwords | BCrypt.Net-Next | Active |
| License keys | SHA-256 lookup hash + Data Protection (never log plaintext) | Active |
| Logging | Serilog + secret redaction | Active |
| Platform | `IPlatformLicenseClient` → `X-Integration-Key` | Active |
| WhatsApp | `IWhatsAppClient` + webhook signature verify (not implemented) | Later slice |
| Payments | `IPaymentGateway` / Paystack (not implemented) | Later slice |
| Tests | xUnit, NSubstitute, FluentAssertions, Testcontainers.PostgreSql | Active |

## Solution layout

```
OrderFlow.sln
frontend/                          # Angular project name: frontend
  src/app/
    core/                          # cross-cutting (auth session, shell, shop state)
    shared/                        # pipes, validators, dumb UI
    features/                      # mirrors Application/Features + API areas
backend/src/
  OrderFlow.Api/                   # Controllers, middleware, Program.cs
  OrderFlow.Application/           # Features, handlers, validators, interfaces
  OrderFlow.Domain/                # Entities, enums, PlanQuota
  OrderFlow.Infrastructure/        # EF, Platform HTTP, JWT, adapters
  OrderFlow.Shared/                # DTOs, constants
backend/tests/
```

Root namespaces match project names (`OrderFlow.Api`, `OrderFlow.Application`, …). Do not rename `frontend` or the `backend/` folder.

### Frontend ↔ API mapping

| Backend | Angular |
|---------|---------|
| `Application/Features/{Name}/` | `features/{name}/` |
| `Shared/DTOs/{Name}/*.cs` (+ `[StringLength]`) | `features/{name}/data/*.models.ts` + `*_FIELD_LIMITS` (domain features) |
| `Api/Controllers/{Name}Controller` | `features/{name}/data/*.api.ts` |
| FluentValidation max/min/required | Angular `Validators` + `shared/validators` + `[attr.maxlength]` |
| JWT / tenancy / session | `core/auth` (+ `AUTH_FIELD_LIMITS`) + `core/shop` |
| Domain entities | Never on the client |

Auth is special: DTO models + HTTP live in `core/auth` because session is app-wide. Core must **not** import features.

## Strict layering

| Type | Location | Never in |
|------|----------|----------|
| Entities / invariants | `OrderFlow.Domain` | Api, Shared, frontend |
| Use cases / handlers | `OrderFlow.Application/Features/{Feature}/` | Api, Infrastructure (except tests) |
| Persistence, HTTP clients, JWT | `OrderFlow.Infrastructure` | Domain, Application implementations |
| Public HTTP contracts | `OrderFlow.Shared/DTOs/` | Domain entities exposed to HTTP |
| Controllers / webhooks | `OrderFlow.Api` | Business rules |
| UI | `frontend/src/app/features/` (+ `core/`, `shared/`) | Backend projects |

Application depends on Domain + Shared only. Infrastructure implements Application interfaces. Api wires both.

**DTOs vs responses:** `OrderFlow.Shared/DTOs` holds **public, external-facing contracts** used by the Angular frontend (e.g. `AuthResponse`, `ProductDto`, `OrderListDto`). Internal MediatR responses may be simple records, but must not expose Domain entities. Map with explicit `FromEntity` (or AutoMapper) **inside the handler**, not in the Controller.

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
- Private entity setters; factory methods (`Shop.Create`, `User.CreateOwner`) with **domain guards** (null/whitespace, max lengths, fixed-size hashes, normalize email lowercase / trim)
- Enums stored as PostgreSQL strings; EF **CHECK** constraints for allowed enum string values and non-empty required strings
- Shared DTOs: `[Required]` / `[StringLength]` / `[EmailAddress]` must match FluentValidation + EF `HasMaxLength`
- Angular: standalone components, `inject()`, `ChangeDetectionStrategy.OnPush`, feature `routes.ts` lazy-loaded from `app.routes.ts`, Tailwind utilities
- Angular tree: `core/` (auth, layout shell, `ShopStateService`), `shared/` (pipes, **validators**), `features/{name}/pages|data|routes` — see [reference.md](reference.md)
- Angular routes: `/` (landing, prerendered), `/login` (guest, `noindex`), `/app` (auth shell + dashboard, `noindex`), `/app/products`, `/app/orders`, `/404` and `**` (`noindex`, never redirect home)
- Marketing SEO: prerender `/` (`outputMode: static`); HTML shell is `noindex` (no OG); `SeoService` + `ORDERFLOW_SITE_URL` for absolute canonical/OG/sitemap; `/login`, `/app`, and unknown URLs stay `noindex`. Browser-only APIs (`localStorage`, `matchMedia`) must not run during prerender.
- Angular state: **Signals** + `OnPush`; shop/plan via `ShopStateService` (synced from auth session). No NgRx in MVP. Use `takeUntilDestroyed()` for RxJS cleanup. `@for` must `track` by entity id.
- Angular forms: field limits live in a named `*_FIELD_LIMITS` constant next to the DTO models (auth: `AUTH_FIELD_LIMITS` in `core/auth/auth.models.ts`); reuse `shared/validators`; HTML `[attr.maxlength]` + inline errors; normalize email `.toLowerCase()` on submit
- UI: mobile-first; forest `#0F6B4C` + gold `#C9A227` + paper `#F3EEE3`; Auth Gateway + light atmosphere textures/illustration flair. App sidebar only at `lg+`; bottom nav + iOS safe areas below `lg`. Tokens → [orderflow-design-system](../orderflow-design-system/SKILL.md). UX / mobile rules → [orderflow-ui-ux](../orderflow-ui-ux/SKILL.md).
- Inventory writes: optimistic concurrency on `Product`; plan-cap creates lock then count then insert (see [reference.md](reference.md), [performance.md](performance.md)). List/dashboard reads: `AsNoTracking`, paged, **project to DTOs in SQL**.
- Logging: Serilog with redaction of secrets (see [reference.md](reference.md))
- Tests: unit (xUnit + NSubstitute + FluentAssertions) for **every** handler and validator (auth included); integration with **Testcontainers.PostgreSql**. Assert list `pageSize` cap and CORS `Allow-Origin` on error JSON. Mock external adapters. Test method names document behavior.
- **Documentation:** XML on **every** public C# member (CS1591 is an error on `backend/src`). JSDoc on exported TypeScript. Document exceptions, plan limits, Shop tenancy, and concurrency. Infrastructure ports use `/// <inheritdoc />`. Tests and EF migrations never get XML. Do not narrate obvious code or leave commented-out dead code. Full rules: [documentation.md](documentation.md).
- Commits only when the user asks; never commit production secrets
- Config: nested settings via env `__` (e.g. `PLATFORM__BASEURL`) — full table in [reference.md](reference.md)
- Production + performance checklists: [production.md](production.md), [performance.md](performance.md). `StartupConfiguration` must keep rejecting Development secrets in Production.

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
dotnet publish backend/src/OrderFlow.Api/OrderFlow.Api.csproj -c Release
cd frontend && npm run build
dotnet ef migrations add Name --project backend/src/OrderFlow.Infrastructure --startup-project backend/src/OrderFlow.Api --output-dir Persistence/Migrations
```
