---
name: orderflow-design-system
description: >-
  OrderFlow visual design system: Forest/Gold/Paper tokens, Source Sans 3
  (Fraunces display on landing headlines only), atmosphere textures (film grain,
  soft dot-mesh), elevation, buttons, cards, forms, status chips, illustration
  flair. Use when styling Angular/Tailwind UI, building components, marketing
  pages, auth, or brand/visual work.
---

# OrderFlow Design System

**This skill is the visual source of truth** for Angular/Tailwind work. Stitch mocks are optional reference only when available.

Implement in `frontend/` with **Tailwind 3** + **Source Sans 3** (Fraunces on landing headlines only). Full token tables: [tokens.md](tokens.md). Atmosphere CSS recipes: [atmosphere.md](atmosphere.md). Screen/UX patterns: [orderflow-ui-ux](../orderflow-ui-ux/SKILL.md). Document custom CSS/utilities with a short comment stating **why** the class exists (token mapping, atmosphere intensity, reduced-motion). Do not comment every Tailwind utility in templates.

## Design intent

OrderFlow is a **warm, trustworthy ops tool** for Ghanaian shop owners — not a cold enterprise console, not a playful consumer app.

| Trait | Design implication |
|-------|-------------------|
| Trustworthy | Clear hierarchy, honest empty states, no dark patterns |
| Grounded | Warm Paper canvas, Forest brand, restrained Gold |
| Efficient | Mobile-first, 44×44px targets, one job per view |
| Alive | Subtle texture + illustration motion on brand surfaces — never noise on data tables |

**Borrow from zof.ai:** film-grain and soft dot-mesh *atmosphere only*. **Do not** copy Zof navy/orange, console chrome, or sparse “control plane” redesigns that made OrderFlow feel colder.

## Core palette

| Role | Hex | Tailwind |
|------|-----|----------|
| Forest | `#0F6B4C` | `forest` |
| Forest dark (hover) | `#0A4D37` | `forest-dark` |
| Forest light | `#17835E` | `forest-light` |
| Gold | `#C9A227` | `gold` |
| Gold dark | `#A6851C` | `gold-dark` |
| Paper (page) | `#F3EEE3` | `paper` |
| Ink | `#1C1917` | `ink` |
| White (cards) | `#FFFFFF` | `white` |
| Border | `#E5E0D5` | `border-[#E5E0D5]` or extend `paper-border` |
| Success | `#2D8A66` | extend `success` when needed |
| Warning | `#D4A017` | extend `warning` |
| Error | `#B91C1C` | `red-700` ok |
| Muted | `#64748B` | `slate-500` ok |

**Rules**

- **Forest** — nav active, primary buttons, brand wordmark, auth brand panels.
- **Gold** — only high-intent actions (`New Order`, `Add Product`) and illustration sparks / highlighted WhatsApp metrics — not every button.
- **Paper** — app + marketing canvas. Cards are **white** so they lift.
- Shadows — green-tinted: `0 2px 8px rgba(15, 107, 76, 0.08)` (never pure black).

## Atmosphere (use sparingly)

Apply textures to **brand surfaces**, not to dense data UI.

| Texture | Where | Intensity |
|---------|--------|-----------|
| Film grain (SVG `feTurbulence`) | Auth forest panel, landing hero, optional Paper shell | ~2% opacity |
| Soft dot-mesh (~22px) | Same brand panels; empty dashboard “breathing room” | low, `rgba(15,23,42,.06–.08)` |
| Faint line texture | Optional marketing section bands | barely visible |

Recipes and utility class guidance → [atmosphere.md](atmosphere.md).

## Typography

**Source Sans 3** (`font-sans`) is the product typeface — app, auth, forms, tables. Hierarchy via weight/size, not rainbow colors.

**Landing exception (`/` only):** marketing headlines may use **Fraunces** (`font-display`) — hero, section titles, feature/plan titles, and the wordmark. Body, labels, nav links, and CTAs stay Source Sans 3. Do **not** use Fraunces on auth, dashboard, or any `/app` screen.

| Token | Size / weight | Use |
|-------|---------------|-----|
| `headline-xl` | 36px / 700 / -0.02em | Desktop page titles (app/auth) |
| `headline-lg` | 28px / 700 | Section titles; sidebar brand |
| `headline-lg-mobile` | 24px / 700 | Mobile titles |
| `headline-md` | 22px / 600 | Cards, dialogs |
| `body-lg` | 18px / 400 | Lead copy |
| `body-md` | 16px / 400 | Body / inputs |
| `body-sm` | 14px / 400 | Secondary / cells |
| `label-md` | 14px / 600 / 0.05em | Nav, buttons |
| `label-sm` | 12px / 600 / 0.05em | Uppercase field labels, table headers |

Optional: monospace for SKUs / order IDs (`font-mono` system stack) — not for headlines.

## Layout & responsive

- Base unit **4px**; gutters **16px**; section gap **24px**
- Margins: mobile **16px**, tablet **24px**, desktop **32px**
- Max content width **1280px**
- Breakpoints (Tailwind defaults): phone `< md`, tablet `md–lg`, desktop `lg+`
- **App chrome is `lg`, not `md`:** bottom nav + top bar below `lg`; sidebar `lg+`. Landscape phones must not get the desktop rail.
- **Safe area:** `viewport-fit=cover`; pad sticky/fixed chrome with `env(safe-area-inset-top|bottom)`. Never a flush `bottom-0` bar on a notched phone.
- Radius: inputs `rounded` (4–8px), cards `rounded-lg`–`rounded-xl`, pills `rounded-full`
- Touch: interactive controls ≥ **44×44px**; primary CTAs `w-full sm:w-auto`
- Marketing display type: `clamp()` minimum must still fit a 320px screen (do not lock a 2.75rem floor)

## Motion & illustration flair

Ship **2–3 intentional motions** on brand/marketing surfaces; keep ops tables calm.

| Allowed | Avoid |
|---------|--------|
| Floating WhatsApp bubbles, soft motion trails, parallax-ready layers | Continuous spinning logos |
| Gold micro-sparks on hero/auth art | Neon glow, purple gradients |
| Gentle CSS `transform`/`opacity` loops (`prefers-reduced-motion: reduce` → static) | Motion on every list row |
| Monoline forest/gold icons for features | Cluttered sticker overlays on hero |

Illustration subjects: WhatsApp chat → order confirmed, stock boxes, Paystack spark, Ghana retail context (shops, Accra/Kumasi copy — not stereotyped imagery).

## Components

### Buttons

| Variant | Style | When |
|---------|--------|------|
| Primary | `bg-forest text-white` → hover `forest-dark` | Submit / confirm / Sign in |
| Secondary | outline forest | Cancel-adjacent |
| Accent | Gold + ink text | `New Order`, `Add Product` only |
| Ghost | text / subtle hover | Header utilities |

**Solid fills for buttons** — no gradients on CTAs. Atmospheric panels may use texture overlays. On the phone, primary/accent/secondary actions in a form footer are **full width** until `sm`.

### Cards

White, `1px` `#E5E0D5`, Level-1 green-tinted shadow. KPI tiles and table shells on Paper.

### Forms

- Labels **above** fields (`label-sm` uppercase OK)
- White field, 1px border; focus: 2px forest + soft green ring
- Errors: tinted banner + clear message

### Status chips

Fully rounded pills; soft tint bg + strong text.

| Status | Treatment |
|--------|-----------|
| Paid / Active / Connected | success tint |
| Pending | neutral / slate |
| Low stock | warning / peach + strong qty |
| Error / Cancelled | error tint |

### Tables & lists

- Desktop: `label-sm` headers, hairline row dividers
- Mobile: list rows + trailing chevron
- Money: `GHS 1,250.00` (or ₵)

### Elevation

| Level | Use |
|-------|-----|
| 0 | Paper canvas |
| 1 | Cards |
| 2 | Modals, dropdowns |

## Anti-patterns

- Redesigning OrderFlow to look like Zof (navy, orange, console paths as primary UI)
- Purple / indigo SaaS gradients, neon glow, multi-layer black shadows
- Pure white full-page backgrounds
- Gold on every control; serif display fonts in the product UI (landing marketing headlines are the exception)
- Texture on data tables / dense forms (hurts scanability)
- Dark mode as default

## Ship checklist

- [ ] Paper canvas + white cards
- [ ] Forest primary / Gold reserved for accent CTAs
- [ ] Source Sans 3 hierarchy (Fraunces display only on landing headlines)
- [ ] Texture only on brand surfaces (if used)
- [ ] 44px touch targets; mobile layout first
- [ ] GHS formatting; status as pills
- [ ] `prefers-reduced-motion` respected for illustration loops
- [ ] Custom CSS/utilities commented with why (not a restatement of the selector)
