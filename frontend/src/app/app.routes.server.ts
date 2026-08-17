import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * `/` is prerendered so crawlers and link previews receive marketing HTML without executing JS.
 * `/404` is prerendered `noindex` so hosts can map missing URLs to a real not-found document.
 * `/login` and `/app` stay client-only — they are session-specific and `noindex`.
 */
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Prerender },
  { path: '404', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Client }
];
