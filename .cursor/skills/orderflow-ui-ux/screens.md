# OrderFlow screen specs

Implementation blueprints. Visual system: [../orderflow-design-system/SKILL.md](../orderflow-design-system/SKILL.md). UX rules: [SKILL.md](SKILL.md).

Design each screen for **mobile, tablet, and desktop** (same IA; escalate density).

---

## 1. Authentication Gateway

**Route:** `/login` → `features/auth/pages/login`

### Purpose
License-backed signup and day-to-day login for shop owners.

### Mobile
- Forest brand **`h-[42svh]`** so the Login sheet is on screen (not a tall illustration stack)
- Compact wordmark/headline; smaller art; `min-h-dvh`; safe-area padding on brand top and form bottom
- Atmosphere: grain + soft dots on forest zone
- Sheet: Login | Sign up tabs, fields ≥44px, **full-width** forest Sign in, trust line

### Tablet / Desktop
- Split from **`lg`**: illustrated forest panel | Paper form (`lg:w-[42%]` / rest)
- Left: full headline + floating WhatsApp/order/stock art + atmosphere textures
- Right: white form card, tabs, forest CTA

### Content rules
- Signup only: license key, shop name, display name?, phone?
- Login: email + password only
- Trust: e.g. “Secure access for verified merchants.”
- Success navigates to `/app`

---

## 2. Landing page

### Purpose
Marketing convert → signup. Brand-forward, illustrated, warm.

### Hero (all devices)
- OrderFlow as hero-level brand signal (Fraunces wordmark + display headline)
- One headline, one sentence, primary + secondary CTA
- Dominant illustration (animated-ready layers)
- Grain/dot-mesh on hero band only
- No floating promo badges on the art
- Body / labels / CTAs remain Source Sans 3

### Below fold
- Features: WhatsApp Ordering | Smart Inventory | Paystack payments
- Pricing: Starter / Growth / Business — **limits from product skill**
- Footer: Privacy and Terms as **non-link labels** until legal pages exist (never `href="#"`). WhatsApp support only with a real number — not `wa.me/` empty.

### Responsive
- Mobile: single column, hamburger, sticky Get started with `env(safe-area-inset-bottom)` + spacer
- Hero type: `clamp()` that **can shrink on a 320px phone** (minimum below 2.75rem)
- 3D stage ~240px on phone, larger from `sm` / `lg`; scale `--phone-w` / `--cube` down on small screens
- Desktop (`lg+`): nav links + side-by-side hero art

### SEO (required with the landing)

- Document shell (`index.html`) is generic + `noindex` with **no** OG/JSON-LD — so `/login` CSR fallback cannot look like home
- Unique title + description live in `LANDING_SEO`; prerender of `/` writes them via `SeoService.applyMarketingHome()`
- Open Graph + Twitter `summary_large_image` + `public/assets/og/og-image.jpg` (1200×630) with width/height/alt
- JSON-LD `@graph` of Organization + WebSite + SoftwareApplication — no invented GHS prices
- `html lang="en-GH"`; hreflang `en-GH` + `x-default` when a public origin exists
- Prerender `/` and `/404`. `/login` and `/app` are client-rendered and `noindex`
- `robots.txt` allows `/`, disallows `/login`, `/app`, `/404`. Sitemap loc is absolute from `ORDERFLOW_SITE_URL`
- One H1 (hero). Section titles are H2; feature/plan names H3. Skip link + `<main id="main">`
- Guest CTAs in the prerendered HTML; session CTAs only after hydration (`sessionReady`)
- Unknown URLs render the 404 page — never `redirectTo: ''`

---

## 3. Shop Dashboard

**Route:** `/app` → shell + `features/dashboard/pages/dashboard`

### Purpose
“What needs attention today?” — sales, WhatsApp backlog, low stock, recent orders.

### Structure
- Chrome from `ShellComponent` (not page-local header)
- KPI row (3–4 white cards); plan from `ShopStateService`
- Low stock list + Recent orders (pills + chevron) when data exists

### Responsive
- Phone / tablet: stacked KPIs and lists; **shell** bottom nav (`< lg`)
- Desktop (`lg+`): sidebar + two-column body

### Data honesty
Zeros and “—” for unimplemented metrics; no decorative fake charts in MVP.

---

## 4. Inventory

**Route (Slice 2):** `/app/products` → `features/products/`

### Purpose
Browse/search products; jump to add/edit; spot low stock.

### Structure
- Title + gold Add Product (`w-full` until `sm`)
- Search + category chips (horizontal scroll on phone; wrap `md+`)
- Cards until `lg` (include category); table from `lg`
- Columns: Product, SKU (mono ok), Category, Price GHS, Qty, Status, Actions
- Pagination footer
- Empty catalog vs no search/category matches (different copy; Add Product only on a true empty catalog)
- `data/`: models mirroring `ProductDto` + `product.api.ts`

---

## 5. Add / Edit Product

**Route (Slice 2):** `/app/products/new`, `/app/products/:id`

### Purpose
Create or update one SKU for WhatsApp selling.

### Structure
- Back to Inventory (`/app/products`)
- Title + one support line
- Upload zone + fields (name*, SKU+Generate, category, price GHS*, stock, low-stock threshold + helper)
- Cancel | Save product (forest). Do not display `version` to the shop owner.

### Responsive
- Mobile: vertical stack; SKU + Generate stack until `sm`; Save/Cancel/Update **full-width** (`flex-col-reverse` so Save is first)
- Desktop (`lg+`): two-pane upload | fields

---

## 6. Orders

**Route (Slice 3):** `/app/orders` → `features/orders/`

### Purpose
Browse/search orders; jump to create or open a status workflow.

### Structure
- Title + gold **New Order** (`w-full` until `sm`). Do **not** hide New Order from list `totalCount` — that is not the monthly cap. Show `maxOrdersPerMonth` as advisory copy; 403 on create is the hard stop.
- Search (customer name/phone) + status chips (All / Pending / Confirmed / Paid / Fulfilled / Cancelled). Horizontal scroll on phone; wrap `md+`
- Cards until `lg`; table from `lg`
- Columns: Customer, Status (pills), Total GHS, Lines, Created, Open
- Pagination footer
- Empty shop vs no search/status matches (different copy; New Order on a true empty list)
- `data/`: models mirroring `OrderDto` / `OrderListDto` + `ORDER_FIELD_LIMITS` + `order.api.ts`

---

## 7. New Order

**Route (Slice 3):** `/app/orders/new`

### Purpose
Create a manual order. Catalog prices are snapshotted; the shop does not type unit price.

### Structure
- Back to Orders (`/app/orders`)
- Customer name*, phone?, notes?
- **Reserve stock now** checkbox default **on** (`confirmImmediately`) — Confirmed + reserve in the same POST. Unchecked stays Pending and does not touch stock
- Product picker: paged `ProductApi.list` search (never load the full catalog). Active SKUs only; one product id per order; max 50 lines; qty 1–99,999,999. Show on-hand on each draft line; block reserve-now save when qty exceeds that snapshot
- First catalog fetch must not flash “no match” (searching until the first page returns)
- Cancel | Save order (forest). Trim/omit blank optionals. Success → order detail
- Empty catalog: send the shop to Add Product, not a fake picker

---

## 8. Order detail

**Route (Slice 3):** `/app/orders/:id`

### Purpose
Read line snapshots and move status along the lifecycle.

### Structure
- Back to Orders; customer name as title; status pill (do **not** show `version`)
- Actions from allowed transitions only: Confirm / Mark paid / Mark fulfilled (forest) and Cancel (outline danger). Cancel needs a second confirm; Confirmed/Paid copy says stock returns. Fulfilled and Cancelled are terminal; Pending cannot jump to Paid
- 409 concurrency: show the API message, reload the order, shop retries
- Line cards until `lg`; table from `lg`. Total GHS. Confirmed/Paid/Fulfilled/Cancelled timestamps when present

---

## Illustration & flair checklist (brand surfaces)

- [ ] 2–3 motion elements max; reduced-motion fallback
- [ ] Subjects: WhatsApp bubbles, stock, Paystack spark — professional, not cartoon chaos
- [ ] Gold only as micro-sparks or accent CTAs
- [ ] Atmosphere textures behind content, never on inputs/tables

## Optional Stitch reference

If Stitch is available later: project `OrderFlow B2B SaaS` / newer `OrderFlow` may hold exploratory mocks. **Skills override Stitch** when they conflict.
