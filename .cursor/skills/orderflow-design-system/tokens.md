# OrderFlow design tokens

Prefer product aliases in [SKILL.md](SKILL.md). Atmosphere recipes: [atmosphere.md](atmosphere.md).

## Brand seeds

| Token | Value |
|-------|--------|
| Forest | `#0F6B4C` |
| Forest dark | `#0A4D37` |
| Forest light | `#17835E` |
| Gold | `#C9A227` |
| Gold dark | `#A6851C` |
| Paper | `#F3EEE3` |
| Paper border | `#E5E0D5` |
| Ink | `#1C1917` |
| White | `#FFFFFF` |
| Success | `#2D8A66` |
| Warning | `#D4A017` |
| Error | `#B91C1C` |
| Muted | `#64748B` |
| Mode | Light |
| Font | Source Sans 3 (product); Fraunces on landing headlines only |
| Base radius | 4–8px |

## Extended surface scale (optional)

Useful when porting older Stitch HTML; map to Paper/white in product UI.

| Name | Hex | Prefer in app |
|------|-----|---------------|
| surface / background | `#fcf9f8` | near Paper; use `paper` for page |
| surface-container-lowest | `#ffffff` | `white` cards |
| surface-container-low | `#f6f3f2` | sidebar tint |
| on-surface | `#1c1b1b` | `ink` |
| on-surface-variant | `#3f4943` | muted body |
| outline-variant | `#bec9c1` | hairlines |
| primary | `#005138` | `forest-dark` |
| primary-container | `#0f6b4c` | `forest` |
| secondary-container | `#fed255` | soft gold fill / chips |

## Typography scale

Product UI is **Source Sans 3**. Landing (`/`) headlines use **Fraunces** (`font-display`) — see [SKILL.md](SKILL.md#typography).

| Token | Size | Weight | Line height | Letter spacing |
|-------|------|--------|-------------|----------------|
| headline-xl | 36px | 700 | 44px | -0.02em |
| headline-lg | 28px | 700 | 36px | -0.01em |
| headline-lg-mobile | 24px | 700 | 32px | — |
| headline-md | 22px | 600 | 30px | — |
| body-lg | 18px | 400 | 28px | — |
| body-md | 16px | 400 | 24px | — |
| body-sm | 14px | 400 | 20px | — |
| label-md | 14px | 600 | 16px | 0.05em |
| label-sm | 12px | 600 | 14px | 0.05em |
| landing-display | clamp 44–72px | 600 | 1.04 | -0.03em |

## Spacing

| Name | Value |
|------|--------|
| unit | 4px |
| gutter | 16px |
| margin-mobile | 16px |
| margin-tablet | 24px |
| margin-desktop | 32px |
| container-max | 1280px |
| touch-min | 44px |

## Radius

| Name | Value | Use |
|------|--------|-----|
| DEFAULT | 0.25–0.5rem | Inputs |
| lg | 0.5rem | Cards, modals |
| xl | 0.75rem | Large panels, active nav |
| full | 9999px | Status pills, some accent CTAs |

## Elevation

| Level | CSS |
|-------|-----|
| 1 | `0 2px 8px rgba(15, 107, 76, 0.08)` |
| 2 | `0 8px 24px rgba(15, 107, 76, 0.12)` |
| Outline | `1px solid #E5E0D5` |

## Angular aliases (`frontend/tailwind.config.js`)

```js
forest: { DEFAULT: "#0F6B4C", dark: "#0A4D37", light: "#17835E" }
gold: { DEFAULT: "#C9A227", dark: "#A6851C" }
paper: "#F3EEE3"
ink: "#1C1917"
```

Extend when implementing atmosphere or semantics: `paper-border`, `success`, `warning`.
