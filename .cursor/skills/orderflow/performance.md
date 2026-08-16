# Performance generation

Read from [SKILL.md](SKILL.md) when implementing a slice. Generate **hot-path efficient** code by default. Do not add Redis, background workers, or micro-optimizations that fight the MVP non-goals. Measure-worthy work (compiled queries, response caching) only when a real list/order path is large — still keep the query cheap.

Shop owners are on phones and often slow networks. Prefer fewer round-trips, smaller payloads, and indexes over clever client caches.

## Backend (API + EF + PostgreSQL)

- **Reads:** `AsNoTracking()` on list/get/dashboard queries. Project to DTOs in the query (`Select`) when the handler does not mutate the entity. Do not materialize full graphs to map two fields.
- **Writes:** one transaction; no extra `SaveChanges` per line item. Stock reserve/deduct/release stays a **single atomic SQL UPDATE** (version + range). Never read-modify-write stock. **Plan caps** (`MaxProducts`, later orders/users) must serialize in the same transaction as the insert (PostgreSQL `pg_advisory_xact_lock` keyed by `ShopId`, then count, then insert). Do not check-then-insert without a lock.
- **Pagination:** every collection endpoint is paged (`page` / `pageSize` with a max, default 20, cap 100). Never `ToList()` a whole shop catalog or order history.
- **Indexes:** `(ShopId)` plus the columns you filter/sort (`Sku` unique per shop, `CreatedAt` for order lists, status where queried). Add them in the EF configuration in the **same slice**.
- **SQL shape:** filter in the database, not in memory. Prefer `EF.Functions.ILike` (or `ILIKE`) for search instead of `ToLower().Contains` on columns (that cannot use indexes). Keep `ShopId` in `WHERE` even with global filters when writing raw SQL.
- **N+1:** no per-row queries in a loop. Use a join, `Where(id ∈ ids)`, or a single grouped query. Dashboard aggregations are SQL `COUNT`/`SUM`, not loaded entities.
- **Caps:** dashboard widgets stay bounded (low-stock already max 50). New “recent X” lists take a `Take(n)` in SQL.
- **Async:** `CancellationToken` on every I/O. `HttpClient` from `IHttpClientFactory` with an explicit timeout. Do not `.Result` / `.Wait()`.
- **Payloads:** return only fields the Angular DTO needs. Bound string max lengths (DoS). Keep JSON body limits tight until uploads exist.
- **Concurrency:** `Version` token on inventory-affecting rows; 409 `code: concurrency` so the client retries instead of overselling.

## Frontend (Angular 19 + Signals)

- **Route-level lazy load** every feature (`loadChildren` / `loadComponent`). Do not eagerly import feature pages from `app.config.ts` or `core/`.
- **`ChangeDetectionStrategy.OnPush`** on new standalone components. Drive UI with Signals (`signal`, `computed`, `resource`/`rxResource` only if already used in-repo). No NgRx.
- **`@for` always has `track`** (entity `id`, not `$index`, unless the list is truly index-stable).
- **RxJS:** `takeUntilDestroyed()` on every subscribe. Debounce search inputs (≈300ms) before calling list APIs. Do not re-fetch the full list on every keystroke.
- **HTTP:** one request per user action; reuse `ShopStateService` instead of extra `/me` calls. Pagination from the API — do not download all rows and slice in the component.
- **Templates:** no heavy work in bindings (no `new Date()`, no deep filters in `@for`). Precompute with `computed()`. Avoid `*ngFor` of large untracked lists.
- **Motion:** CSS `transform`/`opacity` only; respect `prefers-reduced-motion`. No per-row animation on tables. Atmosphere textures stay on brand surfaces, not data grids.
- **Assets:** when product photos ship, `loading="lazy"`, explicit width/height, and compressed formats. Do not inline large SVGs in every card.
- **Bundles:** keep component CSS within the production budget; share Tailwind utilities rather than duplicating huge class strings. Do not import Node polyfills or moment.js.

## Anti-patterns

- Loading all products/orders into memory to count or filter, or materializing full entities then mapping two DTO fields
- Check-then-insert for plan caps (two concurrent creates can both pass `COUNT`)
- Chatty APIs (create + N GETs) when the write response already returns the DTO
- Client-side “infinite” lists without a server page token/offset
- `ChangeDetectorRef.detectChanges()` loops, or default CD on a large table
- Adding Redis/cache “for performance” in MVP
- Logging full webhook JSON at Information (payload + CPU + secret risk)
