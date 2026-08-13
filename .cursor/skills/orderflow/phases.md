# OrderFlow slices

Deliver **one slice at a time**. Confirm with the user before starting the next.

| Slice | Focus | Status |
|-------|-------|--------|
| 0 | Scaffold: sln, Clean Architecture, Angular `frontend`, Docker Postgres, README | **Done** |
| 1 | Auth + license: JWT, `/login` Auth Gateway, `/app` shell (`core/` + `features/auth|dashboard`) | **Done** |
| 2 | Products + inventory + dashboard numbers (today’s sales, order count, low stock) | Next |
| 3 | Manual orders + status workflow + stock reserve/deduct | Planned |
| 4 | WhatsApp webhook, one number per shop, catalog/list messages, strict free-text match | Planned |
| 5 | Paystack payment links + payment webhook → Paid | Planned |
| 6 | Settings, assistant users, low-stock alerts, PWA cache of products/recent orders | Planned |

## Slice 2 acceptance (when started)

- CRUD products scoped to Shop; enforce `PlanQuota.MaxProducts`
- `Product` includes concurrency token (`Version` long or `RowVersion` byte[]) from creation
- Manual stock adjustment writes `StockMovement` via **atomic** stock update (`Stock >= qty` + expected version); `rows affected = 0` → `ConcurrencyAppException`
- Dashboard cards use real counts (sales may be 0 until slice 3)
- Low-stock list on dashboard
- Angular product feature: `features/products/{data,pages,routes}` under `/app/products`; extend shell nav; Signals for list/form state; DTO models mirror `Shared/DTOs`

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
