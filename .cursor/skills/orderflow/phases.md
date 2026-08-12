# OrderFlow slices

Deliver **one slice at a time**. Confirm with the user before starting the next.

| Slice | Focus | Status |
|-------|-------|--------|
| 0 | Scaffold: sln, Clean Architecture, Angular `frontend`, Docker Postgres, README | **Done** |
| 1 | Auth + license: sign up with Platform license key, email/password login, JWT, Angular shell | **Done** |
| 2 | Products + inventory + dashboard numbers (today’s sales, order count, low stock) | Next |
| 3 | Manual orders + status workflow + stock reserve/deduct | Planned |
| 4 | WhatsApp webhook, one number per shop, catalog/list messages, strict free-text match | Planned |
| 5 | Paystack payment links + payment webhook → Paid | Planned |
| 6 | Settings, assistant users, low-stock alerts, PWA cache of products/recent orders | Planned |

## Slice 2 acceptance (when started)

- CRUD products scoped to Shop; enforce `PlanQuota.MaxProducts`
- Manual stock adjustment writes `StockMovement`
- Dashboard cards use real counts (sales may be 0 until slice 3)
- Low-stock list on dashboard
- Angular product list/edit, mobile-first

## Slice 4 notes

- One WhatsApp number per shop
- Adapter `IWhatsAppClient` so BSP (Arkesel / 360dialog / Meta Cloud) can swap
- Unmatched free-text → Pending order flagged needs-clarification + reply asking to use the menu
- No AI in MVP

## Out of scope until explicitly requested

Native iOS/Android, AI free-text, multi-location, Excel/PDF reports, customer CRM, catalog sync, subscription billing portal, Redis, multiple WhatsApp numbers.
