# EnjoyEveryday AI Service (`EnjoyEveryday.AiAgent`) — Deployment & Communication Guide

## 1. Architecture & Network Boundaries

EnjoyEveryday runs four independent deployment units in Docker Compose (`enjoyeveryday-net`):

| Unit | Image (`ghcr.io/<owner>/...`) | Internal Port | Exposure |
|---|---|---|---|
| **UI** (`.NET Blazor`) | `enjoyeveryday-ui` (+ `enjoyeveryday-admin-ui`, `enjoyeveryday-parent-ui`) | `8080` | Published (`8081`, `8084`, `8085`) |
| **Application API** (`.NET ASP.NET Core`) | `enjoyeveryday-api` | `8080` | Published (`8082`) |
| **AI Service** (`Python 3.12 FastAPI`) | `enjoyeveryday-ai-service` | `8000` | **Internal only** (`expose: ["8000"]`) |
| **Database** (`PostgreSQL 16`) | `postgres:16-alpine` | `5432` | Persistent volume `postgres_data` |

The browser never communicates directly with FastAPI. All AI calls originate server-side from `.NET` (`EnjoyEveryday.Api` / Blazor Server hosts) over `http://ai-service:8000`.

---

## 2. Service Communication Contract

* **Base URL Configuration** (checked in order by [`ApplicationServiceRegistration.cs`](../src/EnjoyEveryday.Application/ApplicationServiceRegistration.cs)):
  1. `AiAgent:BaseUrl` (`AiAgent__BaseUrl` env var)
  2. `AI_SERVICE_BASE_URL` env var
  3. `Providers:AI:BaseUrl`
  4. Fallback default: `http://127.0.0.1:8000`
* **Health Check**: `GET /health` → returns `200 OK` with `{"status": "healthy", "app": ..., "version": ..., "defaultModel": ...}`.
* **Endpoints**:
  * `POST /api/ideas`, `POST /api/improve`, `POST /api/materials`
  * `POST /api/recommendations`
  * `POST /api/journey/insights`
  * `POST /api/weekly-plan`
  * `POST /api/parent/draft`
  * `POST /api/organization/kpi-intel`
* **Authentication Headers**:
  * `X-Service-Token`: Shared server-to-server secret (`AiAgent:ServiceSecret` / `AI_SERVICE_SECRET`). Enforced by FastAPI when `REQUIRE_SERVICE_AUTH=true`.
  * `X-Tenant-Api-Key`: Per-tenant Gemini/OpenAI key (or `DEFAULT_AI_KEY` to use server-configured `GEMINI_API_KEY`).
  * `X-Tenant-Id`: Tenant GUID for rate limiting and isolation.
  * `X-Correlation-Id`: Distributed trace ID propagated and logged without PII or API keys.
* **Timeouts, Retries & Graceful Degradation**:
  * `.NET HttpClient` timeout: `45s` (`AiAgent:TimeoutSeconds`).
  * FastAPI provider timeout: `25s` (`AI_TIMEOUT_SECONDS`) with `AI_MAX_RETRIES=2` and multi-provider failover (Gemini → OpenAI → deterministic fallback templates).
  * If `ai-service` is unreachable, `.NET` services catch `HttpRequestException`/`TaskCanceledException` and degrade gracefully without impacting core daycare operations.

---

## 3. Fast Enterprise Deployment & Rollback

Use [`scripts/deploy.ps1`](../scripts/deploy.ps1) for zero-downtime single-service updates, automatic healthcheck verification, and instant rollbacks:

```powershell
# 1. Deploy a specific Git SHA tag for ai-service only (leaves db, api, web running)
.\scripts\deploy.ps1 -Service ai-service -Tag <git-sha>

# 2. Instant rollback to the previous recorded tag (auto-runs if healthcheck fails)
.\scripts\deploy.ps1 -Service ai-service -Rollback

# 3. Check container health & verify .NET -> FastAPI communication
.\scripts\deploy.ps1 -Status
.\scripts\deploy.ps1 -Verify
```

Or using raw Docker Compose:

```bash
AI_TAG=<git-sha> docker compose pull ai-service
AI_TAG=<git-sha> docker compose up -d --no-deps ai-service
```

For local source builds in Docker Desktop without pulling from GHCR:

```bash
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

