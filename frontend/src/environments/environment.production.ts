import { GENERATED_PUBLIC_ORIGIN } from './public-origin.generated';

/** Production client environment. API calls are same-origin unless `apiUrl` is a full origin. */
export const environment = {
  production: true,
  /**
   * Empty origin = same-host `/api/...` (nginx/Caddy proxies to the OrderFlow API).
   * Set a full origin here only when the SPA and API are on different hosts, then rebuild.
   */
  apiUrl: '',
  /**
   * Public HTTPS origin (no trailing slash). Set `ORDERFLOW_SITE_URL` before `npm run build`
   * so prerendered canonical, OG, JSON-LD, and sitemap loc values are absolute.
   */
  siteUrl: GENERATED_PUBLIC_ORIGIN
};
