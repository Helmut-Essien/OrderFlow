/** Client environment. `ng serve` uses this file; production builds replace it with `environment.production.ts`. */
export const environment = {
  production: false,
  /** OrderFlow API origin used by feature `*.api.ts` services. */
  apiUrl: 'http://localhost:5180'
};
