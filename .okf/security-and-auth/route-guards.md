---
okf_version: "0.2.0"
type: concept
title: "Programmatic Route Guards & Page Authorization"
description: "Implementation details of IPageAuthorizationService, CanAccessRoute, CanAccessBranch, and CanAccessClassroom."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - route-guards
  - page-authorization
  - access-denied
---

# Programmatic Route Guards & Page Authorization

## Service Overview: `IPageAuthorizationService`
Defined in `src/EnjoyEveryday.Daycare.Web/Services/PageAuthorizationService.cs`. Injected into Blazor pages to enforce uniform route and resource security.

### Core Methods

```csharp
Task<UserRoleProfile> GetUserProfileAsync(CancellationToken ct = default);
Task<bool> CanAccessRouteAsync(string relativePath, CancellationToken ct = default);
Task<bool> CanAccessBranchAsync(Guid branchId, CancellationToken ct = default);
Task<bool> CanAccessClassroomAsync(Guid classroomId, CancellationToken ct = default);
Task<bool> EnsureAuthorizedAsync(string relativePath, Guid? resourceId = null, string resourceType = "Organization", CancellationToken ct = default);
```

## Access Rules by Resource

### 1. Branch Access (`CanAccessBranchAsync`)
* **Organization Admin / Platform Admin**: Unconditional access to any branch in the active organization.
* **Branch Admin**: Must exist in `profile.AssignedBranchIds` (queried from `user_branch_assignments`).
* **Teacher / Lead Teacher**: Permitted if their assigned classroom belongs to the branch.

### 2. Classroom Access (`CanAccessClassroomAsync`)
* **Organization Admin / Platform Admin**: Unconditional access.
* **Branch Admin**: Permitted if the classroom's `BranchId` is among the user's `AssignedBranchIds`.
* **Teacher / Lead Teacher**: Permitted strictly if the classroom's `Id` is in `profile.AssignedClassroomIds` (queried from `classroom_teachers`).

### 3. Production Authentication Hardening (`IsDevLoginPermitted`)
* **Multi-Layer Defensive Guards**:
  - `IWebHostEnvironment.IsDevelopment()` must be true.
  - Remote IP address must be a loopback address (`IPAddress.IsLoopback`).
  - Request host must resolve to `localhost`, `127.0.0.1`, or `[::1]`.
  - Configuration flag `Authentication:AllowDevLogin` must not be explicitly disabled (`false`).
* **Enforcement**: Disallows dev-login endpoints and development mock shortcuts across `Admin.Web`, `Daycare.Web`, and `Parent.Web` in production, public cloud hosting, and non-loopback network calls.

### 4. Organization Activation & Magic Onboarding (`/auth/activate` & `/auth/accept-invite`)
* **Public Route Guard**: Anonymous access permitted exclusively with valid, unexpired activation token.
* **Token Verification**: Evaluated via `fn_validate_organization_invitation(p_token)` against `organization_invitations`.
* **Access Granting**: Upon token acceptance (`fn_accept_organization_invitation`), assigns `OrganizationAdmin` role and tenant context, signs in with auth cookie, and routes to `/management/branch-dashboard`.

### 5. Organization Suspension & Frozen Account Shield (`/account-suspended`)
* **Trigger Condition**: When an organization's `account_status` is marked as `'Suspended'` in PostgreSQL (via `sp_update_organization_account_status` from Platform Admin), or `is_active = FALSE`.
* **Lock Screen Enforcement**:
  - `UserRoleProfile.IsOrganizationSuspended` is evaluated during profile resolution in `PageAuthorizationService.GetUserProfileAsync`.
  - Platform Admins (`RoleTemplates.PlatformAdmin`) bypass the suspension check so they can inspect and manage organizations.
  - Organization Admins, Branch Admins, Teachers, and Staff belonging to the suspended organization are immediately locked out of all operational routes.
  - In `MainLayout.razor`, if `profile.IsOrganizationSuspended && !profile.IsPlatformAdmin`, the navigation sidebar is locked and the main viewport renders the `<AccountSuspended />` shield.
  - Permitted routes during suspension are limited strictly to `/account-suspended`, `/select-organization`, and `/auth/logout`.
* **UI Experience**: Adheres strictly to the Scandinavian nature-inspired EnjoyEveryday design system (`GEMINI.md`), presenting a warm, clear notification with the organization name, suspension status, reason, contact email, and actions to switch organizations or sign out.

## Access Denied Reasons
Redirects navigate to `/access-denied?reason=<token>`:
* `unauthorized_route`: User does not hold the necessary role or route permission.
* `branch_mismatch`: User attempted to access a branch outside their assignment.
* `classroom_mismatch` / `unauthorized_classroom`: Teacher attempted to access a classroom outside their assignment.
* `organization_suspended`: Organization account is frozen/suspended by platform administration.

