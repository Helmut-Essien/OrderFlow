/** Production client environment. API calls are same-origin unless `apiUrl` is a full origin. */
export const environment = {
  production: true,
  /**
   * Empty origin = same-host `/api/...` (nginx/Caddy proxies to the OrderFlow API).
   * Set a full origin here only when the SPA and API are on different hosts, then rebuild.
   */
  apiUrl: ''
};
