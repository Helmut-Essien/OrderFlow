# OrderFlow slices

Deliver **one slice at a time**. Confirm with the user before starting the next. Every slice must follow [production.md](production.md) and [performance.md](performance.md) — Production fail-fast, same-origin SPA, paged indexed queries, Angular `OnPush`.

| Slice | Focus | Status |
|-------|-------|--------|
| 0 | Scaffold: sln, Clean Architecture, Angular `frontend`, Docker Postgres, README | **Done** |
| 1 | Auth + license: JWT, `/login` Auth Gateway, `/app` shell (`core/` + `features/auth|dashboard`) | **Done** |
| 2 | Products + inventory + dashboard numbers (today’s sales, order count, low stock) | **Done** |
| — | Landing marketing SEO: prerender `/`, meta/OG/JSON-LD, robots, sitemap | **Done** |
| 3 | Manual orders + status workflow + stock reserve/deduct | **Done** |
| 4 | WhatsApp webhook, one number per shop, catalog/list messages, strict free-text match | Planned |
| 5 | Paystack payment links + payment webhook → Paid | Planned |
| 6 | Settings, assistant users, low-stock alerts, PWA cache of products/recent orders | Planned |

## Slice 2 acceptance

- CRUD products scoped to Shop; enforce `PlanQuota.MaxProducts` (**active** SKUs; lock + count + insert in one transaction)
- `Product` includes concurrency token (`Version` long) from creation — API-only, not shop-facing UI
- Manual stock adjustment writes `StockMovement` via **atomic** stock update (`Stock >= qty` + expected version); `rows affected = 0` → `ConcurrencyAppException`
- Dashboard cards: today’s sales / order count / pending WhatsApp are 0 until slice 3; low-stock list is live (SQL-projected DTOs)
- Angular product feature: `features/products/{data,pages,routes}` under `/app/products`; shell nav Inventory; Signals for list/form state; DTO models mirror `Shared/DTOs`; inventory empty catalog ≠ no search matches
- **Full-stack constraints** for Product fields: Domain + EF CHECKs/MaxLength + FluentValidation + Shared DTO annotations + Angular `PRODUCT_FIELD_LIMITS` / validators / `maxlength`

## Slice 3 acceptance

- Manual `POST /api/orders` (source `Manual`). Optional `confirmImmediately` reserves stock in the same transaction
- Status: Pending → Confirmed → Paid → Fulfilled; Cancelled from Pending/Confirmed/Paid. Fulfilled and Cancelled are terminal. Pending cannot skip to Paid
- Stock: reserve on Confirmed (atomic `Stock -= qty`); Paid writes a Deduct **audit** only (on-hand already fell at Confirm); release on Cancelled from Confirmed or Paid. Pending does not touch stock
- `PlanQuota.MaxOrdersPerMonth` counts all orders created in the current UTC month (cancelled still consume a slot); lock + count + insert in one transaction
- Dashboard: `todaysSales` / `orderCount` from `PaidAt` on today’s UTC date for orders still Paid or Fulfilled (cancelled excluded); `recentOrders` capped at 10; `pendingWhatsAppCount` counts WhatsApp+Pending (0 until slice 4)
- Angular orders UI: `features/orders/{data,pages,routes}` under `/app/orders`; shell nav Orders + gold New Order (sidebar only); list empty ≠ no matches; `ORDER_FIELD_LIMITS` matches Domain + Shared DTOs
- **Full-stack constraints** for Order fields: Domain + EF CHECKs/MaxLength + FluentValidation + Shared DTO annotations + Angular `ORDER_FIELD_LIMITS` / validators / `maxlength`

## Slice 3 notes (stock)

- Reserve on Confirmed, deduct on Paid, release on Cancelled (Pending WhatsApp drafts do not touch stock)
- All reserve/deduct/release paths use the same optimistic concurrency SQL pattern as Slice 2
- On concurrency failure after payment webhook: do not silently oversell — return/log failure and surface retry or compensating flow to the shop

## Slice 4 notes

- One WhatsApp number per shop
- Adapter `IWhatsAppClient` so BSP (Arkesel / 360dialog / Meta Cloud) can swap
- **Before processing any incoming webhook**, verify the `X-Hub-Signature-256` header (Meta) or the BSP-specific signature. If verification fails, return HTTP 401 immediately. `IWhatsAppWebhookVerifier` lives in Application; implementation in Infrastructure.
- Unmatched free-text → Pending order flagged needs-clarification + reply asking to use the menu
- No AI in MVP

## Out of scope until explicitly requested

Native iOS/Android, AI free-text, multi-location, Excel/PDF reports, customer CRM, catalog sync, subscription billing portal, Redis, multiple WhatsApp numbers.
