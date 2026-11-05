---
okf_version: "0.2.0"
type: concept
title: "Database Connection & Environment Configuration"
description: "Connection strings, psql CLI utilities, and standard development IDs."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - database
  - connection
  - psql
  - environment
---

# Database Connection & Environment Configuration

## PostgreSQL Environment
* **Host**: `localhost`
* **Port**: `5433`
* **Database**: `enjoyeveryday`
* **User**: `postgres`
* **Password**: `postgres` (or `$env:PGPASSWORD="postgres"`)
* **CLI Path**: `C:\Program Files\PostgreSQL\18\bin\psql.exe`

## Development Identifiers
* **Dev Tenant ID**: `10000000-0000-0000-0000-000000000001`
* **Dev Organization ID**: `20000000-0000-0000-0000-000000000001`
* **Dev Primary User ID**: `50000000-0000-0000-0000-000000000002` (`admin@littlestars.com`)

## PowerShell Diagnostic Snippet
```powershell
$env:PGPASSWORD="postgres"; & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday -c "<SQL_QUERY>"
```

