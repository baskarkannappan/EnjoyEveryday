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

## 4. Skip Rebuilds When Visual / Style Inspection is Sufficient
Pure CSS or markup adjustments (spacing, colors, typography, UI layout tweaks) do not need a `dotnet build` cycle when the application has hot reload running or when inspecting the live DOM. If the user explicitly asks for a UI/CSS fix or visual check, modify the Razor/CSS files directly and omit `dotnet build`.

## 5. Explicit BypassSandbox for MSBuild Compilations
When compilation checks are mandatory (e.g. C# contract changes, interface updates, model refactoring):
- **Never run sandboxed MSBuild**: Windows sandboxing blocks `Directory.Build.targets` process kills, resulting in error code 1 with 0 errors.
- **Always invoke with `BypassSandbox: true`**: Directly execute targeted builds with `--no-restore` (e.g., `dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore`) with `BypassSandbox: true` to avoid the initial 20–30s failure and retry loop.

