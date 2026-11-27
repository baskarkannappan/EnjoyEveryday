# EnjoyEveryday: Solo Developer Deployment & Database Safety Guide

> **Created**: November 26, 2026  
> **Environment**: 1 Local Windows Laptop + 1 Production Server (Zero Staging Server Cost)  
> **Repository**: [https://github.com/baskarkannappan/EnjoyEveryday](https://github.com/baskarkannappan/EnjoyEveryday)

---

## 1. Quick Nightly / Morning Routine

### Before Going to Sleep (Shut Down Containers to Free RAM)
Open PowerShell in your project folder and run:
```powershell
docker compose down
```
> [!NOTE]
> **Zero Data Loss Guarantee**: This safely stops the application containers. Your database (`enjoyeveryday`) and all 172 tables remain permanently preserved in the `enjoyeveryday_postgres_data` Docker disk volume.

### In the Morning (Start Up Everything in 5 Seconds)
When you wake up and want all 6 services running:
```powershell
docker compose up -d
```
All 6 services will start up with green icons in Docker Desktop:
* **Daycare Operations**: [http://localhost:8081](http://localhost:8081)
* **Platform Admin**: [http://localhost:8084](http://localhost:8084)
* **Parent Portal**: [http://localhost:8085](http://localhost:8085)
* **Backend REST API**: [http://localhost:8082/swagger](http://localhost:8082/swagger)
* **Python AI Agent**: Internal port `8000` (FastAPI)
* **PostgreSQL Database**: Port `5433` (`Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres`)

---

## 2. The 3-Tier Architecture (How It Works Without Crashing Your Laptop)

| Environment | Purpose | What Runs There |
| :--- | :--- | :--- |
| **1. Your Laptop** | **Fast Local Development** | • Write C# / Python / SQL in VS / VS Code.<br>• Run database in Docker (`localhost:5433`).<br>• Run `dotnet run` natively (instant hot reload, zero heavy Docker builds). |
| **2. GitHub Actions (Free Cloud Sandbox)** | **Heavy Container Building & Migration Testing** | • GitHub's 7 GB cloud servers spin up throwaway Postgres, test your SQL migrations, build your .NET & Python containers, and push lean (~115 MB) images to GitHub Packages (`ghcr.io`).<br>• Costs your laptop **0 MB disk** and **0% CPU**. |
| **3. Production Server** | **Live Daycare Operations** | • Runs pre-built Docker containers.<br>• Managed safely via `scripts/deploy.ps1`. |

---

## 3. How to Update PostgreSQL Tables in Production Without Data Loss

### Golden Rule A: Transactional DDL (`BEGIN; ... COMMIT;`)
PostgreSQL supports transactional schema updates. If a script fails halfway, it rolls back automatically with 0 damage:
```sql
BEGIN;

-- Add new column with safe default (Metadata-only operation in Postgres 11+, zero table lock)
ALTER TABLE children ADD COLUMN IF NOT EXISTS dietary_notes TEXT DEFAULT '';

-- Add index safely
CREATE INDEX IF NOT EXISTS idx_children_dietary ON children(tenant_id, dietary_notes);

COMMIT;
```

### Golden Rule B: The "Never-Break-Old-Code" Rule (Backward Compatibility)
* Always make schema changes **backward-compatible** with the old code (use `DEFAULT` or `NULL` on new columns).
* Why? If you deploy new application code and find a bug, you can run:
  ```powershell
  .\scripts\deploy.ps1 -Service web -Rollback
  ```
  This reverts the container to the previous version in **3 seconds**. Because the database change didn't break old columns, the old app works immediately and **no user data is lost**!

### Golden Rule C: Routine Rollback vs Nuclear Restore
* **Routine Rollback**: Roll back the application container tag (`deploy.ps1 -Rollback`). Takes 3 seconds, zero data loss.
* **Nuclear Restore**: Restoring a database backup (`pg_restore`) wipes out all user transactions created since the backup. **Only do this if a table was accidentally dropped or corrupted.**

---

## 4. Disaster Recovery & Backup Strategy

Before running any deployment or migration on production:

### 1. Pre-Deployment Snapshot Backup
In PowerShell or command prompt:
```powershell
scripts\backup.cmd
```
* Creates a plain SQL backup (`.sql`) and a binary custom dump (`.dump`) in `database\backups\`.
* Always verify the backup file size is **greater than 0 bytes**.

### 2. Emergency In-Place Restore (If Catastrophe Strikes)
If a destructive error occurs and you must restore:
```powershell
docker exec -i enjoyeveryday_db pg_restore -U postgres -d enjoyeveryday --clean --if-exists -F c < database\backups\your_backup.dump
```

---

## 5. Daily Development Flow for a Solo Developer

```
[Step 1: Write Code / SQL on Laptop]
  - Put schema changes in scripts/migrations/YYYYMMDD_name.sql
  - Test locally on localhost:5433
               │
               ▼
[Step 2: Commit & Push to Git]
  - git add .
  - git commit -m "feat: your new feature"
  - git push origin master
               │
               ▼
[Step 3: GitHub Actions Cloud Sandbox (Automatic)]
  - Cloud runner verifies SQL against throwaway Postgres.
  - Cloud runner compiles .NET and Python containers.
  - Image published to ghcr.io with tag sha-xxxxxxx.
               │
               ▼
[Step 4: Deploy on Production]
  - Take backup: scripts\backup.cmd
  - Apply migration: psql -f scripts/migrations/your_script.sql
  - Deploy new container: .\scripts\deploy.ps1 -Service web -Tag sha-xxxxxxx
  - If unhealthy: .\scripts\deploy.ps1 -Service web -Rollback
```

---

## 6. What Was Configured & Fixed in This Session

1. **Docker Ignore Optimized** ([`.dockerignore`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.dockerignore)):
   * Excluded `database/backups` dumps (over 350 MB) from being sent into Docker build contexts.
2. **Background Laptop Builds Disabled** ([`.github/workflows/local-dev-deploy.yml`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.github/workflows/local-dev-deploy.yml)):
   * Changed from automatic trigger to `workflow_dispatch` (manual) so GitHub Actions never overloads your laptop.
3. **PowerShell 5.1 Compatibility** ([`scripts/deploy.ps1`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/scripts/deploy.ps1)):
   * Fixed `ConvertFrom-Json -AsHashtable` bug so deployment works on default Windows PowerShell.
4. **Docker Dev-Login Enabled** ([`AuthEndpoints.cs`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Endpoints/AuthEndpoints.cs)):
   * Added container bridge gateway detection (`DOTNET_RUNNING_IN_CONTAINER=true`) so 1-click dev authentication works inside Docker without real Google OAuth credentials.
5. **Project Knowledge Base Updated** ([`.okf/troubleshooting/common-pitfalls.md`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/troubleshooting/common-pitfalls.md) & [`.okf/security-and-auth/route-guards.md`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/security-and-auth/route-guards.md)):
   * Documented Rule 9 (Docker Desktop & WSL2 memory limits) and container route guards.
