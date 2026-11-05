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

