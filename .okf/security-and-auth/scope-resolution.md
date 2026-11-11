---
okf_version: "0.2.0"
type: concept
title: "Operational Scope Resolution & Scoped Repositories"
description: "How user assignments in user_branch_assignments and classroom_teachers are resolved into runtime security scopes."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - security
  - scopes
  - access-scope-repository
---

# Operational Scope Resolution & Scoped Repositories

## The Scope Contract: `IAccessScopeRepository`
Defined in `EnjoyEveryday.Domain.Repositories.Auth.IAccessScopeRepository` and implemented in `EnjoyEveryday.Infrastructure.Repositories.Auth.AccessScopeRepository`.

### Primary Methods

```csharp
Task<IReadOnlySet<Guid>> GetAssignedBranchIdsAsync(Guid tenantId, Guid organizationId, Guid userId, CancellationToken ct = default);
Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
Task<IReadOnlySet<Guid>> GetAssignedClassroomIdsAsync(Guid tenantId, Guid userId, Guid? organizationId, Guid? branchId, CancellationToken ct = default);
Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
Task<ClassroomScopeRecord?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default);
```

## Underlying Database Tables

1. **Branch Assignments**: `public.user_branch_assignments`
   - Primary Key: `(user_id, branch_id)`
   - Used to scope **Branch Admins** and **Teachers** to their physical centers.
2. **Classroom Assignments**: `public.classroom_teachers`
   - Unique Constraint: `(classroom_id, user_id)`
   - Used to scope **Teachers** and **Lead Teachers** to their specific rooms.
3. **Family Relationships**: `public.family_relationships`
   - Scopes **Parents** strictly to their verified dependents.

## Runtime Caching & Resolution
* `PageAuthorizationService.GetUserProfileAsync()` resolves `AssignedBranchIds` and `AssignedClassroomIds` scoped to the active organization and active branch (`_userContext.BranchId`), caching it in `UserRoleProfile`.
* When a Teacher is assigned to classrooms across multiple organizations or branches (e.g., Toddlers in Org A Branch A, Preschool in Org B Branch C), only the classrooms belonging to the currently active organization and branch are populated into `AssignedClassroomIds`.

## Multi-Organization & Multi-Branch Teacher Management
1. **Single Identity, Multi-Org Memberships**:
   - `users.normalized_email` enforces one unique account across the entire system.
   - Onboarding an existing email via `UserService.CreateUserAsync` or `TeacherWizard.razor` detects the existing account and creates an `organization_memberships` record + `user_organization_roles` without duplicate user creation or unique key collisions.
2. **Context Selection & Login Routing**:
   - `fn_sync_external_login` returns `out_active_org_id = NULL` whenever a user has > 1 active organization memberships, directing the user to `/select-organization`.
   - After selecting an organization, if the teacher has > 1 assigned branches in that organization, they are directed to `/select-branch`.
   - NavMenu provides one-click switching between assigned branches (`/select-branch`) and organizations (`/select-organization`).
3. **Independent Granular Removal Lifecycle**:
   - Removing from classroom: `sp_remove_teacher_from_classroom` deletes only the `classroom_teachers` row.
   - Revoking from branch: `sp_revoke_user_branch` sets `revoked_at = NOW()` on `user_branch_assignments`.
   - Deactivating from organization: `sp_remove_user_from_organization` marks `organization_memberships.status = 'Inactive'` and revokes assignments in that organization ONLY. Memberships and schedules in other organizations remain completely intact.
4. **Cross-Tenant Member Profile Updates & Activation**:
   - In `UserService.UpdateUserAsync`, when `user.TenantId != _tenantContext.TenantId`, updates are authorized if the user has an active membership or record in the caller's tenant/organization scope (verified via `IUserRepository.GetByIdAsync` / `fn_get_user_by_id`).
   - In `public.sp_update_user`, updates target `WHERE id = p_id` using the global user UUID primary key without failing on home `tenant_id` mismatch.
   - In `TeacherWizard.razor`, saving/activating multi-organization teachers queries `TeacherService.GetAllTeachersAsync(effectiveTenantId, targetOrgId)` and sets `user.IsActive = true` upon activation.

## Multi-Organization & Multi-Branch Login Pipeline
1. **Organization Resolution (Requirement 6 & Migration 109)**:
   - When a user logs in, `fn_sync_external_login` checks accessible organizations via `fn_get_organizations_by_user_id`.
   - Organizations are resolved comprehensively across:
     * Active memberships in `organization_memberships`
     * Organization roles in `user_organization_roles`
     * Active branch assignments in `user_branch_assignments`
     * Classroom assignments in `classroom_teachers` (linked through `classrooms -> branches -> organizations`)
     * Direct tenant organization match
   - If count == 1, the organization is auto-selected into `ActiveOrganizationId`.
   - If count > 1, `out_active_org_id` is returned as `NULL`, routing the user to `/select-organization` to choose their active workspace.
2. **Branch Resolution (Requirement 7, 8 & Migration 109)**:
   - Once an organization is active, the user's role and assigned branches are resolved via `fn_get_user_assigned_branches`.
   - `fn_get_user_assigned_branches` returns branches linked via both `user_branch_assignments` and `classroom_teachers` (ensuring Lead Teachers with classroom assignments automatically see their assigned branches).
   - **Organization Admin**: Enters Org Admin Dashboard with access across all branches in the organization.
   - **Branch Admin**:
     - If assigned to 1 branch: That branch is auto-selected into `ActiveBranchId` claim and `IUserContext.BranchId`.
     - If assigned to > 1 branches: User is routed to `/select-branch` to choose their active campus location.
     - While in the application, the Branch Admin can switch branches anytime via the Hero scope selector or NavMenu, which calls `IUserViewPreferenceService.SetActiveBranch` and `/auth/set-branch`.
   - **Lead Teacher & Teacher**: Automatically scoped to their assigned branch and classroom rooms. In `OrganizationHierarchyService`, educators with `LeadTeacher` role or `is_primary = true` are displayed under `⭐ Lead Educator`. All operational views (Dashboard, Attendance, Live Activities, Roster) filter strictly to the active branch.


## Organization & Branch Graphical Hierarchy Service (`IOrganizationHierarchyService`)
- Defined in `EnjoyEveryday.Application.Services.IOrganizationHierarchyService` and implemented in `EnjoyEveryday.Infrastructure.Repositories.OrganizationHierarchyService`.
- Powers the graphical flowchart/tree organization chart on `/users-by-role` (`UsersByRole.razor` & `OrganizationHierarchyView.razor`).
- **Strict Scope Enforcement**:
  - **Organization Admin**: Retrieves the entire organization tree, all branches, all branch admins, classrooms, lead teachers, teachers, and enrolled students.
  - **Branch Admin**: Scoped strictly to assigned branches in `user_branch_assignments` (`revoked_at IS NULL`). Classrooms, staff, and enrolled students outside their assigned branch(es) are excluded at the database query level.
- Supports interactive canvas features: Zoom (60% - 140%), Expand/Collapse all, search highlighting, and deep links to teacher profiles, branch management, and child journeys.
