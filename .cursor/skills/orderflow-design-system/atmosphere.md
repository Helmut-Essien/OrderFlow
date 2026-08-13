# OrderFlow atmosphere recipes

Subtle **zof.ai-inspired** textures for brand surfaces. Keep opacity low. Prefer CSS utilities in `styles.css` or a shared `atmosphere` layer — not inline noise on every component.

## When to use

| Surface | Grain | Dot-mesh |
|---------|-------|----------|
| Auth forest brand panel | Yes | Yes |
| Landing hero band | Yes | Yes |
| Marketing section band (optional) | Light | Optional |
| App Paper shell | Optional, very light | Rare |
| Tables, forms, KPI cards | No | No |

## Film grain (~2% opacity)

SVG fractal noise (same idea as Zof `.mono-texture-noise`):

```css
.of-texture-grain {
  position: relative;
  isolation: isolate;
}
.of-texture-grain::after {
  content: "";
  pointer-events: none;
  position: absolute;
  inset: 0;
  opacity: 0.02;
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
  z-index: 1;
}
```

On forest panels, place grain above the fill but **below** text/illustration content (`z-index` layering).

## Soft dot-mesh

```css
.of-texture-dots {
  background-image: radial-gradient(
    circle at 1px 1px,
    rgba(15, 23, 42, 0.08) 0.75px,
    transparent 0
  );
  background-size: 22px 22px;
}
```

On **forest** panels, lighten dots so they read on green:

```css
.of-texture-dots-on-forest {
  background-image: radial-gradient(
    circle at 1px 1px,
    rgba(255, 255, 255, 0.12) 0.75px,
    transparent 0
  );
  background-size: 22px 22px;
}
```

Combine: forest fill + dots layer + grain overlay + content.

## Optional faint lines (marketing only)

```css
.of-texture-lines {
  background-image: repeating-linear-gradient(
    0deg,
    transparent,
    transparent 3px,
    rgba(0, 0, 0, 0.02) 3px,
    rgba(0, 0, 0, 0.02) 4px
  );
}
```

## Motion-ready illustration stack

Recommended DOM order (auth left panel / landing hero):

1. Base fill (`bg-forest` or `bg-paper`)
2. Dot-mesh layer (`pointer-events-none`)
3. Illustration layer (SVG/img with floating bubbles, boxes)
4. Grain overlay
5. Copy + CTAs (`relative z-10`)

CSS motion example (respect reduced motion):

```css
@keyframes of-float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-6px); }
}
.of-float {
  animation: of-float 4s ease-in-out infinite;
}
@media (prefers-reduced-motion: reduce) {
  .of-float { animation: none; }
}
```

Use on **2–3** illustration nodes max per viewport.

## Do not

- Copy Zof brand colors (`#EC652B`, `#111A4A`, `#011821`)
- Apply grain/mesh over tables or input fields
- Raise grain opacity above ~0.03 — it will look dirty on Paper
