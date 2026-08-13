---
name: orderflow-ui-ux
description: >-
  OrderFlow UI/UX: mobile-first responsive shells, Auth Gateway with illustrated
  brand panel, dashboard, inventory, product forms, landing with motion flair,
  atmosphere textures. Use when building Angular pages, navigation, forms,
  tables, empty states, or marketing UI.
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

## Responsive shells

| Breakpoint | Shell |
|------------|--------|
| Mobile (`< md`) | Sticky top bar (brand + menu/avatar). **Bottom nav**: Dashboard / Inventory / Orders. Content `px-4`, `pb-24`. |
| Tablet (`md`–`lg`) | Compact collapsible sidebar or top nav + drawer. Content denser; tables may still cardify. |
| Desktop (`lg+`) | Fixed sidebar `w-64`, main `lg:ml-64`, max width 1280px, search + bell + avatar in header. |

**Nav IA**

| Item | Route | Notes |
|------|-------|-------|
| Dashboard | `/` | Active: soft forest tint, bold |
| Inventory | `/inventory` | |
| Orders | `/orders` | Later slice |
| Customers | `/customers` | Later slice |
| Settings | `/settings` | Shop, WhatsApp, plan |
| + New Order | action | **Gold** accent only |
| Support / Logout | footer | |

## Screen patterns

### Authentication Gateway (canonical)

Prefer **illustrated gateway** over a bare centered card.

**Mobile**

- Top ~40%: forest brand zone + animated-looking illustration (WhatsApp bubbles, stock boxes, soft motion) + grain/dot-mesh atmosphere
- Bottom: Paper/white sheet with Login | Sign up underline tabs, fields, full-width forest Sign in, trust line

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
- Mobile: stacked KPIs + lists; bottom nav
- Desktop: sidebar + 2-column body
- Empty/early slice: same skeleton with zeros — no fake charts

### Inventory

- Title + gold **Add Product**
- Search + quiet category chips
- Desktop table; mobile card/list rows
- Low stock: strong qty color + pill
- Pagination: “Showing x to y of n”

### Add / Edit Product

- Back → Inventory; title + one support line
- Mobile: stacked upload then fields
- Desktop: upload | fields
- Required: name, price (GHS). SKU + Generate. Stock + low-stock threshold helper
- Cancel (secondary) | Save (forest primary — never black, never gold)

### Landing (marketing)

- Nav: Features, Pricing, About, Contact, Login, Get started
- **Hero budget:** OrderFlow brand, one headline, one sentence, CTA group, one dominant illustrated visual with light motion + grain/dot-mesh on hero band
- No hero clutter (no stat strips, floating promo stickers)
- Features: WhatsApp, Inventory, Paystack — monoline icons or mini illustrations
- Pricing: Starter / Growth / Business — limits from product skill
- Footer: legal + WhatsApp support
- Localized proof OK (Accra / Kumasi) — keep short

## Interaction & content

| Topic | Rule |
|-------|------|
| Currency | `GHS 1,250.00` |
| Touch | ≥ 44×44px |
| Feedback | Inline banners; plan-unrecognized amber callout |
| Loading | Disable CTA + “Please wait…” |
| Empty | One sentence + one CTA |
| Search | Global orders/products; inventory SKU/name |

## Angular notes

- Standalone, Tailwind, Signals — see orderflow skill
- Features under `frontend/src/app/features/`
- Shared layout in `core/` or `shared/layout/` once multi-page
- Use `forest` / `gold` / `paper` / `ink`; atmosphere utilities from design-system [atmosphere.md](../orderflow-design-system/atmosphere.md)
- Implement **mobile layout first**, then `md:` / `lg:` enhancements

## Workflow

1. Read [screens.md](screens.md) for the screen
2. Apply [orderflow-design-system](../orderflow-design-system/SKILL.md)
3. Mobile → tablet → desktop
4. Real API data; honest empties
5. One slice; ask before the next

## Anti-patterns

- Zof-style console redesign (path chrome as primary UI, navy/orange, sterile “control plane” as the whole brand)
- Ignoring WhatsApp pending on home
- Gold for Save / Sign in
- Texture on tables/forms
- Marketing pricing that contradicts Platform plans
- Desktop-only layouts
