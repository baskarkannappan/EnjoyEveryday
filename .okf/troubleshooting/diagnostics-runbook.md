---
okf_version: "0.2.0"
type: concept
title: "Diagnostics & Root Cause Runbook"
description: "Structured diagnostic procedures for database, authentication, and layout issues."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - runbook
  - diagnostics
  - sql
  - root-cause
---

# Diagnostics & Root Cause Runbook

## 1. Batch SQL Diagnostics (Zero Overhead)
Instead of running multiple round-trip `psql` queries, batch checks into a single command:
```powershell
$env:PGPASSWORD="postgres"; & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday -c "
  SELECT count(*) as total_children FROM children WHERE tenant_id = '10000000-0000-0000-0000-000000000001';
  SELECT count(*) as total_teachers FROM teachers WHERE tenant_id = '10000000-0000-0000-0000-000000000001';
  SELECT count(*) as active_branches FROM branches WHERE is_active = true;
"
```

## 2. Hypothesis-First 0/0 Metric Triage
When stats show `0/0` or phantom rows:
1. **Date Mismatch**: Compare `attendance_date` column with `DateTime.Today` (or simulated demo date).
2. **Missing Active Filter**: Verify if stored procedures filter on `is_active = true` or `status = 'Active'`.
3. **Draft Data Fallback**: Check if entity metadata is stored in `draft_data` JSON versus relational tables.

## 3. Database Changes Require No Solution Rebuild
PostgreSQL schema changes, function updates (`CREATE OR REPLACE FUNCTION`), and row modifications take effect instantly in the live application on port 7003. Do not run `dotnet build` unless Razor or C# files are altered.

