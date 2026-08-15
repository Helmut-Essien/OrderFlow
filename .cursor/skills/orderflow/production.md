# Production-ready generation

Read from [SKILL.md](SKILL.md) when implementing or changing a slice. Generate **as if** `ASPNETCORE_ENVIRONMENT=Production` and `ng build` (production configuration) will run before merge. Local Development convenience must not leak into Production artifacts.

Existing host wiring lives in `OrderFlow.Api/Program.cs`, `StartupConfiguration`, and `frontend/src/environments/`. Extend those; do not invent a second config story.

## Non-negotiables (every slice)

1. No placeholders (`TODO`, `FIXME`, `add logic here`, stub methods that throw `NotImplementedException`).
2. No committed Production secrets. Dev JWT / Platform integration key / Postgres password stay in `appsettings.Development.json` only. `appsettings.json` ships empty secrets. `CopyToPublishDirectory=Never` on Development and Testing json.
3. Production must **fail fast** via `StartupConfiguration.Validate` — never boot on the Development JWT key, Development integration key, `Password=orderflow_dev`, `Include Error Detail`, or a localhost Platform URL. JWT ≥ 64 characters in Production.
4. New env vars use `__` nesting and are documented in [reference.md](reference.md) **and** [README.md](../../../README.md) in the same slice.
5. `dotnet test OrderFlow.sln` and `npm run build` (production) must succeed. Do not ship a CSS budget that fails `ng build`.
6. Honest empties and zeros until the owning slice exists — no fake charts, sample orders, or hidden “coming soon” APIs.

## Backend

- **Secrets:** never log license keys, passwords, integration keys, or Data Protection payloads. Extend `SecretRedactingPolicy` when a new secret property name appears.
- **Auth:** payload max length on login/verify too (DoS bound). Unknown-email login must still run password verify (dummy BCrypt hash) so timing does not enumerate users. Rate-limit auth **per client IP**, not globally. Behind a proxy, `UseForwardedHeaders` must run in Production so `X-Forwarded-For` reaches the limiter.
- **Tenancy:** every new business table has `ShopId`; EF global query filter; handlers read shop from JWT. Cross-shop ids → 404, not 403.
- **Webhooks (WhatsApp / Paystack):** verify signature **before** parsing or writing. Fail → 401 immediately. Timeouts on outbound HTTP (`HttpClient` ≤ 10s unless the provider requires more).
- **Host:** Production gets HSTS, HTTPS redirection, security headers (`nosniff`, `DENY` frame, `no-store`), anonymous `GET /health`, Kestrel JSON body cap (128 KB until file uploads exist). OpenAPI mapped in Development only.
- **Data Protection:** persist keys to `DataProtection:KeysPath` (volume in prod, gitignored). Testing may use ephemeral keys.
- **CORS:** `CorsOrigins.Resolve` — JSON array **or** comma-separated `CORS__ORIGINS`. Empty origins = same-origin SPA only (no `localhost` default in Production).
- **Logging:** console in Production (12-factor). File sink is Development only. Do not log request bodies that contain secrets.
- **Errors:** unhandled exceptions → 500 without internals. `Include Error Detail` is Development-only on the connection string.
- **Migrations:** generate from EF config; never hand-edit the snapshot. Startup `MigrateAsync` is OK for single-instance MVP; do not add a second ad-hoc schema path.

## Frontend

- **`environment.ts`:** `production: false`, `apiUrl: 'http://localhost:5180'`.
- **`environment.production.ts`:** `production: true`, `apiUrl: ''` (same-origin `/api/...` behind nginx/Caddy). Add `fileReplacements` in `angular.json` production config. Never hard-code `localhost` in feature `*.api.ts`.
- **Guards / interceptor:** expired JWT is signed out; 401 on `/app` clears session. Do not attach tokens to login/signup error handling in a way that loops.
- **Forms:** `*_FIELD_LIMITS` + validators + `[attr.maxlength]` + trim/normalize on submit — same slice as the API.
- **Build:** production `outputHashing: all`; keep `anyComponentStyle` budget honest (landing illustration CSS may be larger — raise the budget in the same slice rather than shipping a failing `ng build`).
- **PII:** JWT stays in `localStorage` key `orderflow.token` for MVP; do not log tokens.

## Slice checklist (tick in the same PR)

```
- [ ] No Dev secrets in appsettings.json or the Angular production bundle
- [ ] New settings fail fast in Production (extend StartupConfiguration when needed)
- [ ] Auth/webhook paths: rate limit, signature verify, payload bounds
- [ ] Tenant ShopId on new tables + query filter
- [ ] environment.production.ts still same-origin; no localhost in dist
- [ ] npm run build (production) and dotnet test pass
```
