# OrderFlow screen specs

Implementation blueprints. Visual system: [../orderflow-design-system/SKILL.md](../orderflow-design-system/SKILL.md). UX rules: [SKILL.md](SKILL.md).

Design each screen for **mobile, tablet, and desktop** (same IA; escalate density).

---

## 1. Authentication Gateway

**Route:** `/login` → `features/auth/pages/login`

### Purpose
License-backed signup and day-to-day login for shop owners.

### Mobile
- Forest brand stack (wordmark, short value line, illustration cluster with light float animation)
- Atmosphere: grain + soft dots on forest zone
- Sheet: Login | Sign up tabs, fields ≥44px, forest Sign in, trust line

### Tablet / Desktop
- Split: illustrated forest panel | Paper form
- Left: headline + floating WhatsApp/order/stock art + atmosphere textures
- Right: white form card, underline tabs, forest CTA

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
- Footer: Privacy, Terms, WhatsApp support

### Responsive
- Mobile: single column, sticky Get started
- Desktop: nav links + side-by-side hero art

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
- Mobile: stacked KPIs and lists; bottom nav
- Desktop: sidebar + two-column body

### Data honesty
Zeros and “—” for unimplemented metrics; no decorative fake charts in MVP.

---

## 4. Inventory

**Route (Slice 2):** `/app/products` → `features/products/`

### Purpose
Browse/search products; jump to add/edit; spot low stock.

### Structure
- Title + gold Add Product
- Search + category chips
- Table (desktop) / cards (mobile)
- Columns: Product, SKU (mono ok), Category, Price GHS, Qty, Status, Actions
- Pagination footer
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
- Cancel | Save product (forest)

### Responsive
- Mobile: vertical stack
- Desktop: two-pane upload | fields

---

## Illustration & flair checklist (brand surfaces)

- [ ] 2–3 motion elements max; reduced-motion fallback
- [ ] Subjects: WhatsApp bubbles, stock, Paystack spark — professional, not cartoon chaos
- [ ] Gold only as micro-sparks or accent CTAs
- [ ] Atmosphere textures behind content, never on inputs/tables

## Optional Stitch reference

If Stitch is available later: project `OrderFlow B2B SaaS` / newer `OrderFlow` may hold exploratory mocks. **Skills override Stitch** when they conflict.
