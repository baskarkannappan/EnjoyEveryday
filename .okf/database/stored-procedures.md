---
okf_version: "0.2.0"
type: concept
title: "Stored Functions & Procedures Catalog"
description: "Core PostgreSQL functions used by repositories for attendance, daily care, staff roster, and classroom scoping."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - postgresql
  - functions
  - stored-procedures
  - repositories
---

# Stored Functions & Procedures Catalog

All PostgreSQL functions reside in `public` schema and enforce multi-tenant isolation via `p_tenant_id uuid`.

## 1. Staff, Teachers & Classroom Assignments

| Function Signature | Return Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_branch_teachers(p_tenant_id uuid, p_branch_id uuid)` | `TABLE(...)` | `StaffRosterRepository.GetBranchTeachersAsync` | Joins `teachers`, `user_branch_assignments`, and `classroom_teachers` to list assigned branch teachers. |
| `fn_is_teacher_assigned_to_classroom(p_tenant_id uuid, p_classroom_id uuid, p_teacher_id uuid, p_user_id uuid)` | `boolean` | `TeacherRepository.IsTeacherAssignedToClassroomAsync` | Checks if a teacher/user is assigned to a classroom in `classroom_teachers`. |
| `fn_get_all_teachers(p_tenant_id uuid, p_organization_id uuid)` | `SETOF teachers` | `TeacherRepository.GetAllAsync` | Retrieves all teacher profiles for an organization. |
| `fn_get_teacher_user_id(p_tenant_id uuid, p_teacher_id uuid)` | `uuid` | `StaffRosterRepository.GetTeacherUserIdAsync` | Maps a `Teacher.Id` to their underlying auth `User.Id`. |
| `sp_dispatch_teacher_to_classroom(...)` | `void` | `StaffRosterRepository` | Temporarily reassigns or dispatches a teacher to cover an understaffed room. |

## 2. Attendance & Roster Operations

| Function / Procedure Signature | Routine Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_child_attendance_counts(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `FUNCTION` | `StaffRosterRepository.GetChildAttendanceCountsAsync` | Aggregates Present, Late, Absent, and Not Marked counts across classrooms. |
| `fn_get_child_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `FUNCTION` | `AttendanceRepository.GetChildAttendanceByDateAsync` | Raw attendance records including arrival and departure times. |
| `fn_get_teacher_attendance_summary(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `FUNCTION` | `StaffRosterRepository.GetTeacherAttendanceAsync` | Clock-in, clock-out, and active duty status for teachers on a specific date. |
| `fn_get_attendance_summary(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `FUNCTION` | `AttendanceService` | High-level summary of total present vs expected for stat strips. |
| `sp_record_child_attendance(...)` | `PROCEDURE` | `AttendanceRepository.RecordChildAttendanceAsync` | Upserts daily child check-in, arrival timestamp, and pickup signature. |
| `sp_close_end_of_day_teachers(p_clock_out interval/time, p_ids uuid[])` | `PROCEDURE` | `EndOfDayRepository.CloseTeachersAsync` / `EndOfDayService` | Auto-clocks out teachers remaining in status `Present`/`Late` at close of shift, sets `clock_out_time` and status `ClockedOut`. |
| `sp_close_end_of_day_children(p_departure interval/time, p_user_id uuid, p_ids uuid[])` | `PROCEDURE` | `EndOfDayRepository.CloseChildrenAsync` / `EndOfDayService` | Auto-checks out remaining children without departure, sets `departure_time` to end-of-day time and status `CheckedOut`. |
| `sp_close_end_of_day_activities(p_tenant_id uuid, p_schedule_ids uuid[], p_end_time interval/time)` | `PROCEDURE` | `EndOfDayRepository.CloseActivitiesAsync` / `EndOfDayService` | Auto-closes all active/planned experience schedules and running sessions with status `Completed` and end timestamp. |
| `sp_auto_close_daily_activities(p_tenant_id uuid, p_today date, p_cutoff_time time, INOUT p_closed_count int)` | `PROCEDURE` | `EndOfDayService.AutoCloseActiveActivitiesAsync` | Transactionally auto-closes running experience sessions and active schedules at daycare end of day. |
| `sp_auto_checkout_unattended_children(p_tenant_id uuid, p_today date, p_cutoff_time time, INOUT p_updated_count int)` | `PROCEDURE` | `EndOfDayService.AutoCheckoutUncheckedChildrenAsync` | Transactionally checks out children missing departure marks with status `CheckedOut` at cutoff time. |
| `sp_get_monthly_child_attendance(p_tenant_id uuid, p_branch_id uuid, p_year int, p_month int, p_classroom_id uuid, INOUT cur refcursor)` | `PROCEDURE` | `AttendanceRepository.GetMonthlyChildAttendanceAsync` | Retrieves enrolled children and their daily attendance marks (1..31) for the given branch/month. |
| `sp_get_monthly_teacher_attendance(p_tenant_id uuid, p_branch_id uuid, p_year int, p_month int, INOUT cur refcursor)` | `PROCEDURE` | `AttendanceRepository.GetMonthlyTeacherAttendanceAsync` | Retrieves assigned teachers and their daily clock-in/out records for the given branch/month. |
| `sp_get_branch_location_context(p_tenant_id uuid, p_branch_id uuid, INOUT cur refcursor)` | `PROCEDURE` | `AttendanceRepository.GetBranchLocationContextAsync` | Returns organization, branch, and physical address context for attendance headers. |
| `sp_get_user_accessible_branches(p_tenant_id uuid, p_user_id uuid, p_is_org_admin boolean, INOUT cur refcursor)` | `PROCEDURE` | `AttendanceRepository.GetUserAccessibleBranchesAsync` | Scopes branch access based on role and explicit branch assignments. |

## 3. Statutory Ratios & Staff Leaves

| Function Signature | Return Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_statutory_ratio_rules(p_tenant_id uuid)` | `TABLE(...)` | `StaffRosterRepository.GetStatutoryRatioRulesAsync` | Returns statutory staff-to-child ratio thresholds by age category (Infant, Toddler, Preschool). |
| `fn_get_active_staff_leaves(p_tenant_id uuid, p_date date)` | `TABLE(...)` | `StaffRosterRepository.GetActiveStaffLeavesAsync` | Returns approved and pending staff leaves for the specified date. |
| `sp_insert_staff_leave(...)` | `void` | `StaffRosterRepository.InsertStaffLeaveAsync` | Submits a staff leave request. |
| `sp_assign_substitute_coverage(...)` | `void` | `StaffRosterRepository.AssignSubstituteCoverageAsync` | Links a substitute teacher ID to cover an active staff leave. |

## 4. Daily Care & Teacher Handovers

| Function Signature | Return Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_board_teacher_children_care_status(p_tenant_id uuid, p_classroom_id uuid, p_date date)` | `TABLE(...)` | `DaycareOperationsBoardService` | Returns live diaper, meal, and nap status icons for all children on the teacher workspace cockpit. |
| `fn_get_pending_teacher_handovers_by_branch(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `TABLE(...)` | `ClassroomCommunicationService` | Lists unacknowledged shift handovers awaiting supervisor or incoming teacher sign-off. |
| `sp_create_teacher_handover(...)` | `void` | `ClassroomCommunicationService` | Records shift handover notes, health flags, and supply alerts. |
| `sp_acknowledge_teacher_handover(...)` | `void` | `ClassroomCommunicationService` | Acknowledges receipt of shift handover by the incoming teacher. |

## 5. Authentication & Portal Onboarding Procedures

| Routine Signature | Routine Type | Calling Endpoint / Portal | Purpose |
|:---|:---|:---|:---|
| `sp_seed_dev_parent_environment(...)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/dev-login`) | Transactionally seeds mock parent user, child, family relationship, guardian, and sample moments/milestones. |
| `sp_sync_parent_relationships(...)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/callback`) | Automatically links active children to verified parent by matching draft data parent email and creates family profile person records. |
| `sp_sync_parent_external_login(...)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/callback`) | Synchronizes Google OAuth login for parents, creates missing user/membership/role records, and outputs user and tenant IDs. |
| `sp_validate_parent_portal_credentials(p_email text, INOUT cur refcursor)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/login`) | Replaces inline SQL credential check with stored procedure returning user, role, and password hash. |
| `sp_get_parent_portal_user(p_user_id uuid, INOUT cur refcursor)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/login`) | Fetches parent user record, family relationships, and active child scopes via refcursor. |
| `sp_validate_admin_portal_credentials(p_email text, INOUT cur refcursor)` | `PROCEDURE` | `EnjoyEveryday.Admin.Web` (`/auth/login`) | Authenticates platform administrators and verifies system roles. |
| `sp_get_admin_portal_user(p_user_id uuid, INOUT cur refcursor)` | `PROCEDURE` | `EnjoyEveryday.Admin.Web` (`/auth/login`) | Retrieves admin profile, assigned organizations, and permissions via refcursor. |
| `sp_update_user_password_hash(p_user_id uuid, p_password_hash text)` | `PROCEDURE` | Auth Endpoints | Updates user password hash during rehash or password reset. |
| `sp_get_parent_messages(p_tenant_id uuid, p_classroom_id uuid, p_limit int, INOUT cur refcursor)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/messages`) | Fetches recent classroom communication thread for parents via refcursor. |
| `sp_send_parent_message(p_tenant_id uuid, p_classroom_id uuid, p_sender_id uuid, p_content text, p_is_from_parent boolean, INOUT p_new_id uuid)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/messages`) | Transactionally inserts a parent or educator message into the classroom channel. |
| `sp_mark_parent_communications_read_by_parent(p_tenant_id uuid, p_child_id uuid)` | `PROCEDURE` | `ParentCommunicationRepository.MarkAsReadByParentAsync` | Updates all unread teacher communications for the child to `delivery_status = 'Read'`, resetting unread message counts in the parent portal. |
| `fn_sync_external_login(...)` | `FUNCTION` | `EnjoyEveryday.Daycare.Web`, `EnjoyEveryday.Admin.Web` | Synchronizes external OAuth login for Daycare and Admin portals, returning user status and active organization. |
| `fn_dev_login(...)` | `FUNCTION` | `EnjoyEveryday.Daycare.Web`, `EnjoyEveryday.Admin.Web` | Resolves dev login user profile and active organization in development environments. |
| `fn_validate_and_set_organization(...)` | `FUNCTION` | `EnjoyEveryday.Daycare.Web`, `EnjoyEveryday.Admin.Web` | Validates user membership in an organization and returns tenant context. |


