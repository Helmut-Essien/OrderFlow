# OrderFlow

Standalone B2B SaaS for small retailers in Ghana to take WhatsApp orders, keep inventory, and collect Mobile Money payments.

This repository is the OrderFlow product. License entitlement lives in the existing Platform hub at `../platform`. OrderFlow calls only `POST /api/licenses/validate`.

## Layout

```
OrderFlow.sln
frontend/                 Angular 19 + Tailwind (project name: frontend)
backend/src/
  OrderFlow.Api/          ASP.NET Core 9 Web API
  OrderFlow.Application/  Use cases
  OrderFlow.Domain/       Entities and plan rules
  OrderFlow.Infrastructure/
  OrderFlow.Shared/       DTOs
backend/tests/
docker-compose.yml        PostgreSQL 16 on localhost:5433
```

## Local development

### 1. Platform (required for real license checks)

From `../platform`:

```bash
docker compose up -d
dotnet run --project API/API.csproj
```

Development seed registers `ORDERFLOW` as a service product, logs the integration key, and issues demo license `ORDERFLOW-DEVK-TEST` (Growth plan).

### 2. OrderFlow API

```bash
cd "/home/helmut/Documents/My Projects/OrderFlow"
docker compose up -d
dotnet run --project backend/src/OrderFlow.Api/OrderFlow.Api.csproj
```

API: http://localhost:5180

### 3. Frontend

```bash
cd frontend
npm start
```

UI: http://localhost:4200

First-time **sign up**:

- License key: `ORDERFLOW-DEVK-TEST`
- Email / password: choose any (password at least 8 characters)
- Shop name: your shop

Later visits use **Sign in** with the same email and password. The license key is not asked again.

## Configuration

| Setting | Development default |
|---|---|
| Postgres | `localhost:5433`, db `orderflow_db`, user `orderflow` |
| JWT | `OrderFlow.Api` / `OrderFlow.Frontend` |
| Platform URL | `http://localhost:5176` |
| Platform integration key | `ORDERFLOW-INTEGRATION-DEV-KEY-1b7e3c4a5d8f` (Development only) |

Development secrets live in `appsettings.Development.json` only. `appsettings.json` ships empty JWT, Platform, and connection-string values so a Production process cannot boot on committed defaults.

## Production

The API refuses to start in Production unless environment variables replace every Development secret. JWT signing key must be at least 64 characters and must not be the Development key.

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=...;Database=orderflow_db;Username=...;Password=..."
export JWT__KEY="<at least 64 random characters>"
export PLATFORM__BASEURL="https://platform.example"
export PLATFORM__INTEGRATIONKEY="<from Platform, not the Development key>"
export CORS__ORIGINS="https://app.example"
export DataProtection__KeysPath="/var/lib/orderflow/keys"
```

Do not set `Include Error Detail=true` on the Production connection string. Persist `DataProtection__KeysPath` on a volume so encrypted license keys survive restarts.

```bash
dotnet publish backend/src/OrderFlow.Api/OrderFlow.Api.csproj -c Release
cd frontend && npm run build
```

`ng build` uses the production configuration by default. The SPA calls same-origin `/api/...`; put nginx or Caddy in front and proxy `/api` to the OrderFlow API. Health check: `GET /health`.

## Tests

```bash
dotnet test OrderFlow.sln
```
