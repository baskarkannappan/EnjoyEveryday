# EnjoyEveryday PostgreSQL Full Database Scripts & Backup Plan

This directory contains the complete database scripts and backup files for the **EnjoyEveryday** PostgreSQL database (`enjoyeveryday`), running in the `enjoyeveryday_db` container on port `5433`.

---

## 📁 Directory Structure

```text
EnjoyEveryday.DBFullScript/
├── 00_FullSchema.sql                  # Complete consolidated DDL (all tables, functions, triggers, constraints)
├── 01_FullDatabaseBackup_WithData.sql # Full disaster recovery backup (DROP/CREATE + schema + data)
├── 02_FullDataOnly.sql                # Data-only backup in INSERT statement format
├── Export-DBObjects.ps1               # Automated PowerShell script to re-export/refresh all objects
├── Restore-Database.ps1               # Automated PowerShell script to restore a backup
├── Extensions/
│   └── 01_extensions.sql              # pgcrypto & uuid-ossp extension DDL
├── Tables/                            # 133 individual modular table DDL scripts
│   ├── children.sql
│   ├── experiences.sql
│   ├── organizations.sql
│   ├── tenants.sql
│   ├── users.sql
│   └── ... (all 133 tables)
├── Functions/                         # User-defined PostgreSQL procedures & functions
│   ├── audit_log_append_only.sql
│   ├── audit_org_role_change.sql
│   ├── enjoy_role_template.sql
│   ├── ensure_tenant_roles.sql
│   └── trg_tenants_ensure_roles.sql
├── Triggers/                          # User-defined triggers
│   ├── audit_log_no_change.sql
│   ├── audit_log_no_truncate.sql
│   ├── tenants_ensure_roles.sql
│   └── user_organization_roles_audit.sql
├── Indexes/
│   └── all_indexes.sql                # Complete index definitions for all tables
└── Sequences/
    └── sequences.sql                  # Database sequence definitions
```

---

## 🚀 How to Run Backups & Refresh Scripts

To take a fresh backup and refresh all object files at any time, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\EnjoyEveryday.DBFullScript\Export-DBObjects.ps1
```

Or from within the `EnjoyEveryday.DBFullScript` folder:

```powershell
.\Export-DBObjects.ps1
```

### Script Parameters

| Parameter | Default | Description |
|---|---|---|
| `-ContainerName` | `enjoyeveryday_db` | Docker container running PostgreSQL |
| `-DatabaseName` | `enjoyeveryday` | PostgreSQL database name |
| `-Username` | `postgres` | PostgreSQL username |
| `-OutputDir` | Current script directory | Target directory for generated `.sql` files |

Example targeting a custom container or database:
```powershell
.\Export-DBObjects.ps1 -ContainerName "my_postgres" -DatabaseName "enjoyeveryday" -Username "postgres"
```

---

## 🔄 How to Restore Database

To restore the complete database into your PostgreSQL container or instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\EnjoyEveryday.DBFullScript\Restore-Database.ps1
```

Or directly using `docker exec` / `psql`:

```powershell
# Restore full schema + data:
Get-Content .\EnjoyEveryday.DBFullScript\01_FullDatabaseBackup_WithData.sql -Raw | docker exec -i enjoyeveryday_db psql -U postgres -d enjoyeveryday

# Or restore schema only:
Get-Content .\EnjoyEveryday.DBFullScript\00_FullSchema.sql -Raw | docker exec -i enjoyeveryday_db psql -U postgres -d enjoyeveryday
```

---

## 🛡️ Recommended Backup Strategy

1. **Daily / Weekly Automated Export**: Schedule `Export-DBObjects.ps1` via Windows Task Scheduler or CI/CD pipeline to back up schema and data into your version control or secure storage.
2. **Schema Tracking in Git**: Check in `00_FullSchema.sql`, `Tables/`, `Functions/`, and `Triggers/` into Git to review DDL changes over time.
3. **Off-site Data Retention**: Store `01_FullDatabaseBackup_WithData.sql` in secure cloud storage (e.g., AWS S3, Azure Blob, or Google Cloud Storage) with versioning and retention rules enabled.
