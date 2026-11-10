# Local Docker Desktop Deployment & Testing Guide

This guide explains how to containerize, deploy, and test the entire **EnjoyEveryday** application stack locally in **Docker Desktop**.

---

## 1. Stack Architecture & Ports

The containerized environment runs 4 isolated services connected via a private Docker bridge network (`enjoyeveryday-net`):

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Docker Desktop Network                          │
│                                                                        │
│   ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   │
│   │ enjoyeveryday_web│   │ enjoyeveryday_api│   │enjoyeveryday_ai  │   │
│   │   (Blazor Web)   │   │   (.NET 10 API)  │   │  (Python FastAPI)│   │
│   │ Host Port: 8081  │   │ Host Port: 8082  │   │ Host Port: 8083  │   │
│   └────────┬─────────┘   └────────┬─────────┘   └────────┬─────────┘   │
│            │                      │                      │             │
│            └──────────────────────┼──────────────────────┘             │
│                                   ▼                                    │
│                     ┌───────────────────────────┐                      │
│                     │     enjoyeveryday_db      │                      │
│                     │  (PostgreSQL 15 Alpine)   │                      │
│                     │  Host Port: 5433          │                      │
│                     │  Internal Port: 5432      │                      │
│                     └───────────────────────────┘                      │
└────────────────────────────────────────────────────────────────────────┘
```

> [!TIP]
> The host ports (`8081`, `8082`, `8083`) are intentionally separated from Visual Studio host ports (`7003`, `5165`, `8000`). This ensures you can run container testing **simultaneously** with Visual Studio debugging without port collisions!

---

## 2. Deploying the Complete Stack

1. **Start Docker Desktop** on your machine.
2. Open a PowerShell terminal in the repository root.
3. Build and launch all 4 services in the background:
   ```powershell
   docker compose up -d --build
   ```
4. Verify all containers are running and healthy:
   ```powershell
   docker compose ps
   ```

You should see all 4 containers listed with status `Up`:
* `enjoyeveryday_db`
* `enjoyeveryday_api`
* `enjoyeveryday_ai_agent`
* `enjoyeveryday_web`

---

## 3. Testing Endpoints & Verification

Once launched, access the following URLs in your browser:

| Component | URL | Verification Action |
| :--- | :--- | :--- |
| **Daycare Web** | `http://localhost:8081` | Log in with `admin@littlestars.com` or `myassociationmanager005@gmail.com`. |
| **Platform Admin** | `http://localhost:8084` | Platform administration, multi-tenant overview, warehouse & catalog. |
| **Parent Portal** | `http://localhost:8085` | Parent & guardian timeline, memories, routines, and communications. |
| **Core API** | `http://localhost:8082/swagger` | Verify OpenAPI documentation and test endpoints. |
| **AI Agent** | `http://localhost:8083/docs` | Verify FastAPI Swagger docs for Gemini ADK endpoints. |
| **Database** | `localhost:5433` | Connect with DBeaver, pgAdmin, or run `scripts\db.cmd`. |

---

## 4. Managing Individual Services

You do **not** need to restart the entire stack when modifying code. You can manage each service independently:

### Rebuilding Only the Web App
```powershell
docker compose build web
docker compose up -d web
```

### Rebuilding Only the API
```powershell
docker compose build api
docker compose up -d api
```

### Rebuilding Only the AI Agent
```powershell
docker compose build ai-agent
docker compose up -d ai-agent
```

### Running Only the Database (for Visual Studio Development)
If you switch back to local debugging in Visual Studio and only need PostgreSQL:
```powershell
# Stop all app containers but keep DB running
docker compose stop web api ai-agent

# Or launch only DB:
docker compose up -d db
```

---

## 5. Logs & Diagnostics

### View Real-time Logs
* **All services together**:
  ```powershell
  docker compose logs -f
  ```
* **Specific service only**:
  ```powershell
  docker compose logs -f web
  docker compose logs -f api
  docker compose logs -f ai-agent
  ```

### Stopping the Stack
* **Stop without losing database data**:
  ```powershell
  docker compose down
  ```
  *(Preserves the `postgres_data` volume — all child profiles, classrooms, and records remain safe).*

* **Complete clean wipe (Reset database to blank state)**:
  ```powershell
  docker compose down -v
  ```

---

## 6. Testing on Mobile Devices / Local Wi-Fi (LAN)

To allow colleagues or mobile phones on the same Wi-Fi network to test the containerized web app:

1. Find your machine's local Wi-Fi IP address:
   ```powershell
   ipconfig
   ```
   *(Look for `IPv4 Address`, e.g. `192.168.1.50`)*.
2. Ensure inbound TCP port `8081` is permitted in Windows Defender Firewall.
3. Open `http://192.168.1.50:8081` from any phone, tablet, or laptop connected to the same Wi-Fi.

