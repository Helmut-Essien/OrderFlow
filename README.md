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

Production must set `Platform:IntegrationKey` and `Jwt:Key` via environment variables or user secrets. Never commit production keys.

## Tests

```bash
dotnet test OrderFlow.sln
```
