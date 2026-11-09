---
okf_version: "0.2.0"
type: concept
title: "Common Pitfalls & Architectural Gotchas"
description: "High-frequency pitfalls including role string spacing, tab/dropdown filter desync, and sandbox build locks."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - pitfalls
  - gotchas
  - debugging
  - tips
---

# Common Pitfalls & Architectural Gotchas

## 1. Role Names With Whitespace (`"Branch Admin"` vs `"BranchAdmin"`)
* **Problem**: In `auth_roles`, roles are often stored with human spaces (`"Branch Admin"`, `"Daycare Admin"`, `"Organization Admin"`), while `RoleTemplates` constants omit spaces (`"BranchAdmin"`).
* **Fix**: Always normalize roles using `StringComparison.OrdinalIgnoreCase` and check both forms or rely on `IPageAuthorizationService` / permission flags.

## 2. Dropdown Filters Conflicting With Active Tabs
* **Problem**: Having an overarching `<select class="control">` (e.g. `Children only / Teachers only`) above discrete tabs (`[Children] [Teachers]`) will cause empty results ("No matching records") if a user switches tabs while the dropdown is locked on the opposite category.
* **Fix**: Synchronize tabs and dropdowns via `SetTab()` and `OnTypeFilterChanged()`, and never clear records when the active tab explicitly selects the entity type.

## 3. MSBuild Windows `KillLockingDevProcesses` in Sandbox
* **Problem**: `Directory.Build.targets` executes `taskkill /F /IM EnjoyEveryday.Daycare.Web.exe`. In the Windows sandbox, inter-process management is blocked, causing builds to fail with code 1 and 0 errors.
* **Fix**: Run `dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore` with `BypassSandbox: true`.

## 4. Full Solution Builds are Strictly Prohibited
* **Problem**: Running `dotnet build EnjoyEveryday.slnx` builds 10+ projects and takes 40+ seconds.
* **Fix**: Always build single target projects:
  ```powershell
  dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore
  ```

## 5. CSS Cascade & Maximum Page Width
* **Problem**: `ee-foundation.css` enforces `zoom: 0.82` and `max-width: 1720px !important`.
* **Fix**: For compact views (e.g. `TeacherWorkspace`), explicitly specify `max-width: 960px !important; margin: 0 auto !important;` in page-specific styles.

## 6. Sequential `if` Route Authorization Fall-through
* **Problem**: In Blazor pages checking both Branch Admin and Teacher scopes, checking `if (profile.IsBranchAdmin ...)` followed by an un-nested `if (profile.IsTeacher ...)` causes Branch Admins to fall through into the Teacher check (since `profile.IsTeacher` defaults to `true` for administrators in `PageAuthorizationService`). If the Branch Admin lacks individual teacher assignment to that specific classroom, they are erroneously redirected to `/access-denied?reason=classroom_mismatch`.
* **Fix**: Always enforce role hierarchy with mutually exclusive guards:
  ```csharp
  if (!profile.IsOrgAdmin && !profile.IsPlatformAdmin)
  {
      if (profile.IsBranchAdmin)
      {
          if (!profile.AssignedBranchIds.Contains(classroom.BranchId))
              Nav.NavigateTo("/access-denied?reason=branch_mismatch", true);
      }
      else if (profile.IsTeacher)
      {
          if (!profile.AssignedClassroomIds.Contains(classroom.Id))
              Nav.NavigateTo("/access-denied?reason=classroom_mismatch", true);
      }
  }
  ```

## 7. Multi-Tenant Branch Queries & Live Role Resolution
* **Problem**: In multi-tenant environments where organizations each have their own `tenant_id`, passing a stale session `TenantId` (such as the default development tenant) to `fn_get_branches_by_organization` or `fn_get_dashboard_branch_metrics` yields 0 rows because `branches.tenant_id` must match `organizations.tenant_id`. Additionally, reading user roles solely from static authentication cookie claims causes newly granted roles (e.g. Org Admin assigned via Platform Admin) to be invisible until re-login.
* **Fix**:
  1. `BranchRepository` and `DashboardRepository` dynamically resolve the organization's authoritative `tenant_id` via `fn_get_organization_by_id_global` before querying branches or dashboard metrics.
  2. `PageAuthorizationService` invokes `IAccessScopeRepository.GetOrganizationRoleNamesAsync(tenantId, orgId, userId)` live on each profile resolution so admin assignments take effect instantly.
  3. Map `/organization/branches` and `/organization` to `Management.razor` (the real organization & branch hub) rather than static mock tree components.

## 7. Blazor Server `IHttpContextAccessor` Lifecycle & Identity Null Safety
* **Problem**: `IHttpContextAccessor.HttpContext` is only active during the initial HTTP request/prerender. Once the Blazor circuit/WebSocket begins asynchronous processing or when navigating on mobile, `HttpContext` becomes `null` asynchronously. Accessing `_httpContextAccessor.HttpContext.User` without local capture or null propagation throws a `System.NullReferenceException` at runtime.
* **Fix**: Capture `var httpUser = _httpContextAccessor?.HttpContext?.User;` once in [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs), check `httpUser?.Identity?.IsAuthenticated == true`, and wrap Blazor navigation menu scope resolution in general `try-catch (Exception ex)` blocks to prevent circuit crashes.

## 8. Mobile Viewport `zoom: 0.82` Reset & Action List Grids
* **Problem**: `ee-foundation.css` applies `zoom: 0.82` to desktop containers. On mobile viewports (e.g., iPhone SE 375px), un-reset `zoom` inflates the coordinate layout (`375 / 0.82 = 457px`), pushing cards and grids offscreen. Additionally, trying to force 2-column grids on horizontal action cards with icons, long titles, and subtitles causes text truncation and container overflows.
* **Fix**: Always reset `zoom: 1 !important;` under `@media (max-width: 768px)` in `ee-foundation.css`. On mobile phones (`<= 640px`), convert dense multi-column shortcut grids to clean full-width single-column list cards with right arrow chevrons (`→`).

## 9. `roles` Table Schema & Tenant Role Instantiation
* **Problem**: The `roles` table contains columns `(id, tenant_id, name, description, scope_type, created_at)`. It does NOT have `is_system` or `updated_at`. When dynamically instantiating a tenant-scoped role from global template roles (e.g., `LeadTeacher`), attempting to reference non-existent columns causes error `42703: column "is_system" of relation "roles" does not exist`.
* **Fix**: In `sp_assign_organization_role`, insert only valid columns `(id, tenant_id, name, description, scope_type, created_at)` and copy mapped permissions from `role_permissions`.

