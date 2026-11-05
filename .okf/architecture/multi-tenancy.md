---
okf_version: "0.2.0"
type: concept
title: "Multi-Tenancy & Data Isolation Model"
description: "Tenant isolation rules, tenant scoping middleware, and repository database filtering."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - multi-tenancy
  - isolation
  - security
---

# Multi-Tenancy & Data Isolation

## Architecture Philosophy
EnjoyEveryday enforces strict multi-tenant logical partitioning across all database tables, repository queries, and Blazor pages. A tenant represents an independent daycare enterprise.

## Key Components

1. **`ITenantContext` (`EnjoyEveryday.Shared.Tenancy`)**:
   * Scoped service providing `TenantId: Guid`.
   * Resolved per request / Blazor circuit from the user's authenticated claims.
2. **`IUserContext` (`EnjoyEveryday.Shared.Tenancy`)**:
   * Scoped service providing `UserId: Guid`, `OrganizationId: Guid?`, `Roles: IReadOnlyList<string>`, and `HasPermission(string)`.
3. **Database Scoping**:
   * Every business table has a `tenant_id UUID NOT NULL` column with index `idx_<table>_tenant`.
   * Stored procedures and PostgreSQL functions require `p_tenant_id uuid` as their first parameter.
   * Repositories MUST always pass `_tenantContext.TenantId` to prevent cross-tenant data leakage.

## Development Test Context
* **Default Dev Tenant ID**: `10000000-0000-0000-0000-000000000001`
* **Default Dev Org ID**: `20000000-0000-0000-0000-000000000001`
* **Default Dev Admin User**: `50000000-0000-0000-0000-000000000002` (`admin@littlestars.com`)

