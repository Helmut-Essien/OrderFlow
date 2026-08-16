---
name: orderflow-ui-ux
description: >-
  OrderFlow UI/UX: mobile-first shells (bottom nav until lg, sidebar lg+),
  iOS safe areas, Auth Gateway 42svh brand panel, dashboard, inventory cards
  until lg, product forms, landing with motion flair, atmosphere textures.
  Form field limits and validators must mirror backend Shared DTOs in the same
  slice. Generated Angular is production-ready (same-origin API, OnPush, lazy
  routes) and performance-conscious (track-by id, debounced search, no giant
  unpaged lists). Document with JSDoc (why, not what). Use when building Angular
  pages, navigation, forms, tables, empty states, or marketing UI.
---

# OrderFlow UI/UX

Visual tokens + atmosphere: [orderflow-design-system](../orderflow-design-system/SKILL.md). Screen specs: [screens.md](screens.md). Product rules / plan limits: [orderflow](../orderflow/SKILL.md).

**Stitch is optional reference.** Design and implement from these skills.

## Principles

1. **Mobile-first, then tablet, then desktop** — design the phone experience first; escalate density, not a different product.
2. **One job per view** — dashboard = situation awareness; inventory = catalog; form = one product; landing = convert.
3. **WhatsApp + GHS first-class** — pending WhatsApp work and Cedis are never buried.
4. **Warm OrderFlow, not cold console** — Paper + Forest + restrained Gold. Texture/illustration flair on brand surfaces only.
5. **Plan copy from product docs** — pricing/limits from `PlanQuota` / Platform `planName`, not marketing mock numbers.
6. **Accessible motion** — illustration loops respect `prefers-reduced-motion`.
7. **Production + performance** — same-origin `/api` in production builds; `OnPush` + Signals; `@for track` by id; debounce search; page from the API. Details: [production.md](../orderflow/production.md), [performance.md](../orderflow/performance.md).

## Responsive shells

App chrome lives in `core/layout/ShellComponent`. Do **not** put a second header or bottom nav on feature pages.

| Breakpoint | Shell |
|------------|--------|
| Phone + tablet (`< lg`) | Sticky top bar (wordmark + truncated shop name + 44px Sign out). **Bottom nav** for routes that exist. Content `px-4`, `pb-[calc(6rem+env(safe-area-inset-bottom))]`. |
| Desktop (`lg+`) | Fixed sidebar `w-64`, main `lg:ml-64`, max width 1280px. No bottom nav. |

**Why `lg`, not `md`:** landscape phones are often ≥768px wide. Sidebar at `md` steals ~256px from a short screen. Keep bottom nav until `lg` (1024px).

**Safe area (required on every new sticky/fixed chrome)**

- `index.html` viewport: `width=device-width, initial-scale=1, viewport-fit=cover` (do not drop `viewport-fit=cover`)
- Top chrome: `pt-[env(safe-area-inset-top)]`
- Bottom chrome: `pb-[env(safe-area-inset-bottom)]`
- Main under bottom nav: include the inset in the bottom padding (see shell). Landing sticky **Get started** already does this; keep a matching spacer (`h-[calc(5rem+env(safe-area-inset-bottom))]`)
- Truncate shop names (`min-w-0` + `truncate`). Sign out / icon buttons ≥ 44×44px

**Bottom nav**

- Only links for routes that exist (Dashboard, Inventory today; Orders when slice 3 ships)
- Active: `bg-forest text-white` (same as sidebar), not forest text alone
- `lg:hidden` on top bar + bottom nav; `hidden lg:flex` on sidebar

**Nav IA** (implement links only for routes that exist)

| Item | Route | Notes |
|------|-------|-------|
| Dashboard | `/app` | Active: soft forest tint / `bg-forest text-white` |
| Inventory | `/app/products` | Slice 2+ |
| Orders | `/app/orders` | Slice 3+ |
| Customers | `/app/customers` | Later |
| Settings | `/app/settings` | Slice 6 |
| + New Order | action | **Gold** accent only (when orders exist) |
| Support / Logout | footer | Logout → `/login` |

## Screen patterns

### Authentication Gateway (canonical)

Prefer **illustrated gateway** over a bare centered card.

**Mobile**

- Forest brand zone **`h-[42svh]`** (`lg:h-auto lg:min-h-full lg:w-[42%]`) so Login fields are on screen without scrolling
- Page root `min-h-dvh`; brand `pt-[max(1.5rem,env(safe-area-inset-top))]`; form `pb-[max(2rem,env(safe-area-inset-bottom))]`
- Compact type + smaller illustration on phone; license microcopy **desktop only**
- Atmosphere: grain + soft dots on forest zone
- Sheet: Login | Sign up tabs, fields ≥44px, **full-width** forest Sign in, trust line

**Tablet / Desktop**

- Split ~42/58: left forest illustrated panel; right Paper form
- Headline example: “Transform every WhatsApp chat into a sale.” / “Orders from WhatsApp. Stock you can trust.”
- Tabs: Login | Sign up (forest underline)
- Login: email, password (+ Forgot)
- Sign up: license key, shop name, owner fields — **license only at signup**
- Trust microcopy under CTA

### Shop Dashboard

- KPIs: Today’s sales (GHS), Orders, Pending WhatsApp (gold emphasis if > 0), optional Low stock count
- Body: Low stock list + Recent orders (status pills + chevron)
- Mobile / tablet: stacked KPIs + lists; shell bottom nav
- Desktop (`lg+`): sidebar + 2-column body
- Empty/early slice: same skeleton with zeros — no fake charts

### Inventory

- Title + gold **Add Product** (`w-full` until `sm`)
- Search + category chips: **horizontal scroll** on phone (`shrink-0`, overflow-x); wrap from `md`
- Cards until `lg` (name, SKU, category, price, qty, status); **table from `lg`**
- Low stock: strong qty color + pill
- Pagination: “Showing x to y of n”

### Add / Edit Product

- Back → Inventory; title + one support line
- Mobile: stacked upload then fields; SKU + Generate **stack** (`flex-col` → `sm:flex-row`)
- Desktop (`lg+`): upload | fields
- Required: name, price (GHS). SKU + Generate. Stock + low-stock threshold helper
- Cancel (secondary) | Save (forest primary — never black, never gold)
- Phone actions: **`w-full` until `sm`**. Form footer `flex-col-reverse` so Save is on top. Stock **Update** is full-width on phone

### Landing (marketing)

- Nav: Features, Pricing, About, Contact, Login, Get started
- **Hero budget:** OrderFlow brand, one headline, one sentence, CTA group, one dominant illustrated visual with light motion + grain/dot-mesh on hero band
- **Type:** Fraunces for hero / section / card titles and the wordmark; Source Sans 3 for body, labels, nav, CTAs
- No hero clutter (no stat strips, floating promo stickers)
- Features: WhatsApp, Inventory, Paystack — monoline icons or mini illustrations
- Pricing: Starter / Growth / Business — limits from product skill
- Footer: legal + WhatsApp support
- Localized proof OK (Accra / Kumasi) — keep short
- **Mobile:** hamburger + drawer; sticky Get started + safe-area; hero type `clamp` **must be able to shrink below 44px** (do not set a 2.75rem minimum); 3D stage/props scale down under `sm`

## Mobile layout rules (every new screen)

Apply these when generating Orders, Settings, or any `/app` page. Copy patterns from `shell.component.html`, `login.component.html`, `product-list`, `product-form`.

1. **Phone-first** — default layout is a single column. Add `sm:` / `md:` density, then `lg:` for sidebar-era tables and two-pane forms.
2. **Do not introduce a page-local nav** — use the shell. Add a bottom-nav + sidebar link only when the route exists.
3. **Touch** — every control `min-h-[44px]`. Inputs stay ~16px font (avoid iOS focus zoom).
4. **Primary CTAs** — `w-full sm:w-auto` (Save, Sign in, Add Product, Update stock, Get started).
5. **Paired fields** (SKU + Generate, amount + apply) — stack on `xs`, row from `sm`.
6. **Lists** — cards/`<ul>` below `lg`; tables `hidden lg:block`. Truncate titles (`min-w-0` + `truncate`).
7. **Filter chips** — one row + horizontal scroll on phone; wrap from `md`.
8. **Fixed/sticky UI** — always add `env(safe-area-inset-*)`. Never a raw `bottom-0` bar on a notched phone.
9. **Long names** — truncate in headers and cards; do not let shop/product names shove actions off-screen.
10. **Auth/marketing brand blocks** — cap phone height (`~42svh` or scaled art) so the form/CTA stays on screen.

## Interaction & content

| Topic | Rule |
|-------|------|
| Currency | `GHS 1,250.00` |
| Touch | ≥ 44×44px |
| Feedback | Inline banners; plan-unrecognized amber callout |
| Loading | Disable CTA + “Please wait…” |
| Empty | One sentence + one CTA. **Catalog empty ≠ search miss** — filtered lists say “no matches”, not “add the first SKU”. |
| Search | Global orders/products; inventory SKU/name |

## Angular notes

Structure and layering: [orderflow reference](../orderflow/reference.md) (Frontend conventions + Constraints).

- Shell: `core/layout/ShellComponent` under `/app` — sidebar **`lg+` only**; top bar + bottom nav below `lg`
- Pages: `features/{name}/pages/...`; feature `routes.ts` lazy-loaded; new components use `ChangeDetectionStrategy.OnPush`
- Shop/plan: `ShopStateService` — prefer over reading auth only in templates when sharing across features
- Tokens: `forest` / `gold` / `paper` / `ink`; atmosphere from [atmosphere.md](../orderflow-design-system/atmosphere.md)
- Currency pipe: `shared/pipes/ghsCurrency`
- Validators: `shared/validators` (`requiredTrimmed`, etc.); feature/auth `*_FIELD_LIMITS` must match backend Shared DTOs
- Forms: reactive forms; `[attr.maxlength]` + inline errors for every limited field; trim / lowercase email on submit
- Lists: `@for` with `track` by entity id; debounce search ~300ms; never load an unpaged catalog into the client
- Mobile layout first, then `sm:` / `md:` / `lg:` — **never `md:` for the app sidebar**
- Do not add nav items for routes that do not exist yet
- **Document generated UI code** — JSDoc on exported services, models, pipes, validators, and non-obvious component public APIs; template comments only for layout/a11y intent that classes do not make obvious. Full rules: [documentation.md](../orderflow/documentation.md).

## Workflow

1. Read [screens.md](screens.md) for the screen
2. Apply [orderflow-design-system](../orderflow-design-system/SKILL.md)
3. Mobile → tablet → desktop
4. Real API data; honest empties
5. If the screen writes data: apply the constraints checklist in [orderflow reference](../orderflow/reference.md) with the API in the same slice
6. Document new/changed public TypeScript APIs per [documentation.md](../orderflow/documentation.md) and non-obvious template structure
7. Apply [production.md](../orderflow/production.md) and [performance.md](../orderflow/performance.md) (`OnPush`, track-by, debounce, production `apiUrl`)
8. One slice; ask before the next

## Anti-patterns

- Zof-style console redesign (path chrome as primary UI, navy/orange, sterile “control plane” as the whole brand)
- Ignoring WhatsApp pending on home
- Gold for Save / Sign in
- Showing `version` / concurrency tokens in shop-facing copy
- `href="#"` Privacy/Terms or `https://wa.me/` with no number
- Texture on tables/forms
- Marketing pricing that contradicts Platform plans
- Desktop-only layouts, or app sidebar at `md` (landscape phones)
- Sticky/fixed bars without `env(safe-area-inset-*)`, or dropping `viewport-fit=cover`
- Auth forest panel taller than ~40% of the phone viewport (Login below the fold)
- Side-by-side SKU/action rows that squeeze inputs on 320px
- Primary actions that are not full-width on the phone
- Hero `clamp()` with a minimum that never shrinks on small screens
- Client forms without max length / requiredness that the API already enforces
- Hard-coding field limits in the template instead of a shared `*_FIELD_LIMITS` constant
- Shipping undocumented public TypeScript APIs, or commenting every line of a template
- Default change detection on new list/table pages, `@for` without `track`, or fetching the full catalog to filter in the browser
- Hard-coding `localhost` API URLs (production builds must use `environment.production.ts`)
