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
Task<IReadOnlySet<Guid>> GetBranchIdsOfAssignedClassroomsAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
Task<ClassroomScopeRecord?> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default);
```

## Underlying Database Tables

1. **Branch Assignments**: `public.user_branch_assignments`
   - Primary Key: `(user_id, branch_id)`
   - Used to scope **Branch Admins** to their physical centers.
2. **Classroom Assignments**: `public.classroom_teachers`
   - Unique Constraint: `(classroom_id, user_id)`
   - Used to scope **Teachers** and **Lead Teachers** to their specific rooms.
3. **Family Relationships**: `public.family_relationships`
   - Scopes **Parents** strictly to their verified dependents.

## Runtime Caching & Resolution
* `PageAuthorizationService.GetUserProfileAsync()` resolves `AssignedBranchIds` and `AssignedClassroomIds` once per circuit and caches it in `UserRoleProfile`.
* When a Branch Admin navigates to any classroom, `CanAccessClassroomAsync` looks up the classroom's `BranchId` via `GetClassroomScopeAsync` and checks if it exists in `AssignedBranchIds`.

## Multi-Organization & Multi-Branch Login Pipeline
1. **Organization Resolution (Requirement 6)**:
   - When a user logs in, `fn_sync_external_login` checks the number of active organization memberships.
   - If count == 1, the organization is auto-selected into `ActiveOrganizationId`.
   - If count > 1, `out_active_org_id` is returned as `NULL`, routing the user to `/select-organization` to choose their active workspace.
2. **Branch Resolution (Requirement 7 & 8)**:
   - Once an organization is active, the user's role and assigned branches (`fn_get_user_assigned_branches`) are resolved.
   - **Organization Admin**: Enters Org Admin Dashboard with access across all branches in the organization.
   - **Branch Admin**:
     - If assigned to 1 branch: That branch is auto-selected into `ActiveBranchId` claim and `IUserContext.BranchId`.
     - If assigned to > 1 branches: User is routed to `/select-branch` to choose their active campus location.
     - While in the application, the Branch Admin can switch branches anytime via the Hero scope selector or NavMenu, which calls `IUserViewPreferenceService.SetActiveBranch` and `/auth/set-branch`.
   - **Lead Teacher & Teacher**: Automatically scoped to their assigned branch and classroom rooms. All operational views (Dashboard, Attendance, Live Activities, Roster) filter strictly to the active branch.


## Organization & Branch Graphical Hierarchy Service (`IOrganizationHierarchyService`)
- Defined in `EnjoyEveryday.Application.Services.IOrganizationHierarchyService` and implemented in `EnjoyEveryday.Infrastructure.Repositories.OrganizationHierarchyService`.
- Powers the graphical flowchart/tree organization chart on `/users-by-role` (`UsersByRole.razor` & `OrganizationHierarchyView.razor`).
- **Strict Scope Enforcement**:
  - **Organization Admin**: Retrieves the entire organization tree, all branches, all branch admins, classrooms, lead teachers, teachers, and enrolled students.
  - **Branch Admin**: Scoped strictly to assigned branches in `user_branch_assignments` (`revoked_at IS NULL`). Classrooms, staff, and enrolled students outside their assigned branch(es) are excluded at the database query level.
- Supports interactive canvas features: Zoom (60% - 140%), Expand/Collapse all, search highlighting, and deep links to teacher profiles, branch management, and child journeys.
