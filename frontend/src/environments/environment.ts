/** Client environment. `ng serve` uses this file; production builds replace it with `environment.production.ts`. */
export const environment = {
  production: false,
  /** OrderFlow API origin used by feature `*.api.ts` services. */
  apiUrl: 'http://localhost:5180',
  /**
   * Public origin for canonical, Open Graph, JSON-LD, and sitemap loc values.
   * No trailing slash. Empty skips absolute URL tags.
   */
  siteUrl: 'http://localhost:4200'
};
