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

