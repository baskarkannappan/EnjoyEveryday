---
okf_version: "0.2.0"
type: concept
title: "Staff Roster, Shifts & Statutory Ratios"
description: "Staff coverage roster, statutory staff-to-child ratios, staff leaves, and substitute coverage matching."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - roster
  - ratios
  - staffing
  - leaves
  - substitutes
---

# Staff Roster, Shifts & Statutory Ratios

## Overview
Complies with statutory daycare regulatory licensing requirements by maintaining live staff-to-child ratio tracking per age group, alongside teacher shifts and leave coverage.

## Primary Routes & UI Components
* **Staff Roster Page**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/StaffRosterPage.razor`
  - URL: `/management/roster` or `/roster`
  - Displays: Live classroom ratio meters, shift schedules, active teacher leaves, and unassigned coverage gaps.
* **Coverage Dialog**: In `UI.Shared` for assigning substitute teachers to open shifts.

## Repository & Stored Procedures
* **Repository**: `IStaffRosterRepository` (`EnjoyEveryday.Infrastructure.Repositories.StaffRosterRepository`)
* **Key Stored Functions**:
  - `public.fn_get_statutory_ratio_rules(p_tenant_id uuid)`: Returns age-tier ratio thresholds (e.g., Infant 1:3, Toddler 1:4, Preschool 1:8).
  - `public.fn_get_branch_classrooms(p_branch_id uuid)`: Returns classroom capacities and current counts.
  - `public.fn_get_active_staff_leaves(p_tenant_id uuid, p_date date)`: Active leaves for the specified calendar date.
  - `public.sp_insert_staff_leave(...)`: Submits sick, vacation, or emergency leaves.
  - `public.sp_assign_substitute_coverage(...)`: Binds a substitute teacher to a leave record.

## Permissions Required
* `Permissions.StaffManage`: Required to approve leaves, assign shifts, and modify rosters.
* Branch Admins oversee their assigned branch; Org Admins can oversee all branches.

## Teacher Promotion to Lead Teacher & Classroom Management
* **Promotion Workflow**:
  - Authorized Roles: `OrganizationAdmin` and `BranchAdmin` (holding `staff.manage` / `role.assign`).
  - Stored Procedure: `public.sp_promote_teacher_to_lead(p_tenant_id, p_organization_id, p_branch_id, p_user_id, p_promoted_by, p_classroom_ids)`.
  - Atomic Action:
    - Retires lower teaching role in `user_organization_roles` for the target organization, granting `LeadTeacher`.
    - Guarantees branch assignment in `user_branch_assignments`.
    - Synchronizes multi-classroom assignments in `classroom_teachers` for that branch only.
    - Preserves all historical attendance, daily moments, and memberships in other organizations/branches.
    - Audited to `public.audit_log` with action `Teacher.PromotedToLead`.
* **Classroom Management Workflow**:
  - Stored Procedure: `public.sp_update_teacher_classrooms(p_tenant_id, p_organization_id, p_branch_id, p_user_id, p_classroom_ids)`.
  - Allows Organization Admin and Branch Admin to add, remove, or modify classroom assignments for teachers and lead teachers without changing their operational role.
  - Audited to `public.audit_log` with action `Teacher.ClassroomsUpdated`.
* **UI Components**:
  - [`PromoteTeacherModal.razor`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/Users/PromoteTeacherModal.razor): Two-step wizard (Branch & Room Selection -> Confirmation Review).
  - [`ManageTeacherClassroomsModal.razor`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/Users/ManageTeacherClassroomsModal.razor): Room assignment manager.
  - Accessible via `UsersByRoleView.razor`, `TeachersDirectoryView.razor`, and `TeacherProfile.razor`.

