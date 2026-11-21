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
| `fn_get_attendance_needs_attention(p_tenant_id uuid, p_date date)` | `FUNCTION` | `AttendanceRepository.GetAttendanceNeedsAttentionAsync` | Returns actionable attendance alerts (unmarked classrooms, missing departures, staffing shortages on branches with active rooms). |
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
| `sp_open_branch_day(p_tenant_id uuid, p_branch_id uuid, p_date date, p_user_id uuid, OUT p_success boolean, OUT p_message text)` | `PROCEDURE` | `DailyOperationsRepository.OpenBranchDayAsync` | Morning Gate: Branch Admin marks branch open for operations. Validates not already open or closed. |
| `sp_open_classroom_day(p_tenant_id uuid, p_classroom_id uuid, p_date date, p_user_id uuid, OUT p_success boolean, OUT p_message text)` | `PROCEDURE` | `DailyOperationsRepository.OpenClassroomDayAsync` | Classroom Gate: Lead Teacher opens classroom. Requires the parent branch to be Open for the day. |
| `sp_close_classroom_day(p_tenant_id uuid, p_classroom_id uuid, p_date date, p_user_id uuid, p_checkout_time time, OUT p_children_checked_out int, OUT p_activities_closed int, OUT p_success boolean, OUT p_message text)` | `PROCEDURE` | `DailyOperationsRepository.CloseClassroomDayAsync` | Classroom End-of-Day: Auto-checks out remaining children, auto-completes active sessions, records stats and closes classroom. |
| `sp_close_branch_day(p_tenant_id uuid, p_branch_id uuid, p_date date, p_user_id uuid, p_checkout_time time, OUT p_open_classrooms_count int, OUT p_teachers_clocked_out int, OUT p_success boolean, OUT p_message text)` | `PROCEDURE` | `DailyOperationsRepository.CloseBranchDayAsync` | Branch End-of-Day: Validates all classrooms in branch are closed (0 open), clocks out staff, and seals branch for the day. |
| `fn_get_branch_daily_status(p_tenant_id uuid, p_branch_id uuid, p_date date)` | `FUNCTION` | `DailyOperationsRepository.GetBranchDailyStatusAsync` | Returns current operational status (Closed, Open, Completed), open room count, timestamps and actors. |
| `fn_get_classroom_daily_status(p_tenant_id uuid, p_classroom_id uuid, p_date date)` | `FUNCTION` | `DailyOperationsRepository.GetClassroomDailyStatusAsync` | Returns current classroom status (Closed, Open, Completed), parent branch status, and children/activity counts. |

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
| `sp_sync_parent_relationships(...)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/callback`) | Links active, onboarded (`is_active = TRUE AND enrollment_status = 'Enrolled'`) children matching parent email and creates family profile person records (updated in `126_restrict_parent_portal_to_onboarded_children.sql`). |
| `fn_has_active_onboarded_child(p_user_id uuid)` | `FUNCTION` (returns `boolean`) | `EnjoyEveryday.Parent.Web` (`/auth/callback`) | Verifies that the parent user has at least one verified/active guardian relationship to an active, enrolled child before allowing Parent Portal sign-in. |
| `sp_sync_parent_external_login(...)` | `PROCEDURE` | `EnjoyEveryday.Parent.Web` (`/auth/callback`) | Synchronizes Google OAuth login for parents, creates missing user/membership/role records, and outputs user and tenant IDs. |
| `sp_remove_user_from_organization(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid)` | `PROCEDURE` | `UserRepository.RemoveUserFromOrganizationAsync` | Inactivates organization membership, deletes organization roles, revokes branch assignments, and removes classroom teachers. If user has 0 remaining active organizations, sets `users.is_active = FALSE` and `teachers.status = 'Inactive'`. |
| `sp_delete_teacher(p_tenant_id uuid, p_id uuid, p_organization_id uuid DEFAULT NULL)` | `PROCEDURE` | `TeacherRepository.DeleteAsync` | Marks teacher Inactive and removes user from the specified organization scope via `sp_remove_user_from_organization`. |
| `sp_delete_user(p_tenant_id uuid, p_id uuid)` | `PROCEDURE` | `UserRepository.DeleteAsync` | Soft-deactivates user from organization scope. If 0 active orgs remain, user account is deactivated globally. |
| `sp_delete_child(p_tenant_id uuid, p_id uuid)` | `PROCEDURE` | `ChildRepository.DeleteAsync` | Sets `children.is_active = FALSE`, `enrollment_status = 'Withdrawn'`, and revokes associated `family_relationships`. |
| `fn_get_organizations_by_user_id(p_user_id uuid)` | `FUNCTION` (returns `TABLE(...)`) | `OrganizationRepository.GetOrganizationsByUserIdAsync` | Returns strictly accessible, active organizations for the user. Excludes organizations where user membership status is `'Inactive'` or if user is globally inactive. |
| `fn_validate_and_set_organization(p_user_id uuid, p_organization_id uuid)` | `FUNCTION` (returns `TABLE(is_valid boolean, tenant_id uuid)`) | `AuthEndpoints.cs` (`/auth/set-organization`) | Validates that user has active access to the specified organization and blocks selection if user is deactivated or membership status is `'Inactive'`. |

## 6. Admissions, Pipeline & Storage Operations

| Function / Procedure Signature | Routine Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_classroom_vacancies(p_tenant_id uuid, p_branch_id uuid)` | `FUNCTION` | `EnrollmentService` / `EnrollmentPipeline.razor` | Computes active classroom capacities, current active enrollments, and pending pipeline offers. Filters deactivated classrooms (`is_active = TRUE`), system dummy rooms (`name NOT ILIKE '%pool%'`), and dummy capacities (`capacity < 900`). |
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
| `fn_get_user_by_email(p_email character varying)` | `FUNCTION` | `IUserRepository.GetByEmailAsync` | Retrieves user profile by email or normalized email across global users table. |
| `sp_assign_organization_role(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid, p_role_name character varying)` | `PROCEDURE` | `IUserRepository.AssignOrganizationRoleAsync` | Upserts organization membership and assigns organization role, supporting existing users from any tenant joining organizations. |
| `fn_get_users_hierarchy(p_tenant_id uuid)` | `FUNCTION` | `IUserRepository.GetUsersHierarchyAsync` | Returns team members and roles scoped to the tenant, including members joined via organization memberships and branch assignments. |
| `fn_get_user_assigned_branches(p_user_id uuid, p_organization_id uuid)` | `FUNCTION` | `AuthEndpoints.cs`, `SelectBranch.razor` | Returns active branches assigned to a user within an organization via `user_branch_assignments`, enforcing multi-branch routing and scoping. |

## 6. Organization Onboarding & Platform Administration

| Routine Signature | Routine Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_create_organization_invitation(p_organization_id uuid, p_admin_email varchar, p_token varchar, p_expires_days int)` | `FUNCTION` | `PlatformAdminRepository.CreateOrganizationInvitationAsync` | Revokes prior pending invitations, inserts new invitation with 7-day expiry token, and returns invitation record. |
| `fn_get_organization_latest_invitations()` | `FUNCTION` | `PlatformAdminRepository.GetLatestInvitationsAsync` | Returns the latest invitation per organization with token, expiry, status, and acceptance timestamp. |
| `fn_validate_organization_invitation(p_token varchar)` | `FUNCTION` | `PlatformAdminRepository.ValidateInvitationAsync`, `/auth/activate` | Validates invitation token validity, expiry, and returns organization name, admin email, and tenant context. |
| `fn_accept_organization_invitation(p_token varchar, p_user_id uuid)` | `FUNCTION` | `PlatformAdminRepository.AcceptInvitationAsync`, `/auth/accept-invite` | Marks invitation Accepted, binds user to `organization_memberships`, and assigns `OrganizationAdmin` role in `user_organization_roles`. |
| `sp_provision_platform_organization(...)` | `PROCEDURE` | `PlatformAdminRepository.ProvisionOrganizationAsync` | Provisions a new tenant, organization, default branches, initial admin user, and assigns `OrganizationAdmin` role. |
| `sp_update_platform_organization_details(...)` | `PROCEDURE` | `PlatformAdminRepository.UpdateOrganizationDetailsAsync` | Updates organization name, location, and branch counts. |
| `sp_update_organization_account_status(p_org_id uuid, p_status varchar, p_reason text)` | `PROCEDURE` | `PlatformAdminRepository.UpdateOrganizationAccountStatusAsync`, `PlatformAdminService.UpdateAccountStatusAsync` | Persists lifecycle state changes (`Active`, `Suspended`, `Deactivated`), setting `organizations.is_active` and `suspension_reason`. Enforces account lock in Daycare portals. |
| `sp_update_organization_plan_quota(p_org_id uuid, p_plan_id uuid, p_plan_code varchar, p_plan_name varchar, p_max_branches int, p_max_classrooms int, p_max_children int, p_includes_ai boolean)` | `PROCEDURE` | `PlatformAdminRepository.UpdateOrganizationPlanQuotaAsync`, `PlatformAdminService.AssignPlanAndTariffToOrganizationAsync` | Persists subscription plan capacity limits on `organizations` table (`plan_id`, `plan_code`, `plan_name`, `max_branches`, `max_classrooms`, `max_children`, `includes_ai`). |
| `fn_get_organization_quota_usage(p_org_id uuid)` | `FUNCTION` | `PlatformAdminRepository.GetOrganizationQuotaUsageAsync`, `PlanQuotaService` | Atomically computes current counts vs maximum quotas for branches, classrooms, and enrolled children for an organization. |
| `fn_get_platform_organizations_overview()` | `FUNCTION` | `PlatformAdminRepository.GetOrganizationsOverviewAsync` | Retrieves organizations with real-time `account_status`, `suspension_reason`, branch count, primary admin email, plan details, region details, `tariff_name`, and exact `monthly_rate` joined from `region_plan_tariffs` (updated in `128_fix_organizations_overview_tariff_and_rate.sql`). |
| `fn_create_branch(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_name varchar, p_location varchar, p_is_active boolean, p_operational_status varchar, p_active_closure_id uuid, p_created_at timestamptz, p_updated_at timestamptz)` | `FUNCTION` | `BranchRepository.AddAsync` | Creates a branch record matching all 10 `Branch` entity properties (updated in `125_fix_branch_create_and_update_routines.sql` to include `operational_status` and `active_closure_id`). |
| `sp_update_branch(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_name varchar, p_location varchar, p_is_active boolean, p_operational_status varchar, p_active_closure_id uuid, p_created_at timestamptz, p_updated_at timestamptz)` | `PROCEDURE` | `BranchRepository.UpdateAsync` | Updates branch details, operational status, and active closure ID matching all 10 `Branch` entity properties. |

## 8. High-Performance Query & Stored Function Optimizations (Migration 139)

| Routine Signature | Routine Type | Calling Repository / Service | Optimization & Impact |
|:---|:---|:---|:---|
| `fn_get_parent_authorized_children(p_tenant_id uuid, p_user_id uuid)` | `FUNCTION` (returns `TABLE`) | `ParentService.GetAuthorizedChildrenAsync` | Converted to `LANGUAGE sql STABLE`; covered by `idx_family_rel_user_verified` (`user_id, child_id`). Latency dropped from **111.8ms max / 7.8ms mean** to **0.33ms** (>20x speedup). |
| `fn_has_parent_child_access(p_tenant_id uuid, p_user_id uuid, p_child_id uuid)` | `FUNCTION` (returns `boolean`) | `ParentAuthorizationMiddleware`, auth guards | Inlinable `LANGUAGE sql STABLE` with `idx_family_rel_user_verified` index-only evaluation. Latency dropped from **59.1ms max / 6.6ms mean** to **0.35ms**. |
| `fn_get_classroom_by_id(p_tenant_id uuid, p_id uuid)` | `FUNCTION` (returns `TABLE`) | `ClassroomRepository.GetByIdAsync` | Converted to `LANGUAGE sql STABLE`; covered by composite index `idx_classrooms_id_tenant`. Latency dropped from **109.8ms max / 28.1ms mean** to **0.82ms** (>30x speedup). |
| `fn_get_organization_role_names(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid)` | `FUNCTION` (returns `TABLE`) | `AccessControlService`, claims builders | Converted to `LANGUAGE sql STABLE`; covered by `idx_uor_tenant_org_user_role`. Latency dropped from **35.8ms max / 1.7ms mean** to **0.16ms** (>10x speedup). |
| `fn_get_end_of_day_pending_teachers(p_tenant_id uuid, p_date date, p_branch_id uuid)` | `FUNCTION` (returns `TABLE`) | `DaycareOperationsBoardService` | Replaced degraded `OR` join on `users` with split left joins; covered by partial index `idx_teacher_attendance_pending_clockout`. Latency dropped from **42.4ms max / 24.0ms mean** to **0.11ms** (>200x speedup). |
| `fn_get_end_of_day_pending_children(p_tenant_id uuid, p_date date, p_branch_id uuid)` | `FUNCTION` (returns `TABLE`) | `DaycareOperationsBoardService` | Converted to `LANGUAGE sql STABLE`; backed by `idx_child_attendance_active_present`. |
| `fn_get_end_of_day_pending_activities(p_tenant_id uuid, p_date date, p_branch_id uuid)` | `FUNCTION` (returns `TABLE`) | `DaycareOperationsBoardService` | Converted to `LANGUAGE sql STABLE`; backed by `idx_experience_schedules_pending`. |


| Routine Signature | Routine Type | Calling Repository / Service | Purpose |
|:---|:---|:---|:---|
| `fn_get_organization_hierarchy(p_tenant_id uuid, p_org_id uuid, p_requesting_user_id uuid, p_is_org_admin boolean)` | `FUNCTION` (returns `jsonb`) | `OrganizationHierarchyService.GetOrganizationHierarchyAsync` | High-performance single-roundtrip aggregation of organization details, admins, branches, classrooms, educators, and children. Replaces 12–33 sequential queries. Optimized in `122_query_performance_optimizations.sql` (`MATERIALIZED` CTEs, single-pass `lead_teacher_users` join, single-pass staff/student totals). |
| `fn_get_branch_readiness(p_tenant_id uuid, p_branch_id uuid)` | `FUNCTION` (returns `TABLE`) | `ClassroomReadinessRepository.GetBranchReadinessDataAsync` | Consolidated single-roundtrip routine returning branch details, lead teacher count, and per-classroom enrollment/capacity/teacher metrics. |
| `fn_is_privacy_and_trust_approved(p_tenant_id uuid, p_child_id uuid)` | `FUNCTION` (returns `boolean`) | `ParentRepository.IsPrivacyAndTrustApprovedAsync` | Checks if child has active signed Privacy & Trust consent without hardcoded SQL in C#. |
| `sp_approve_privacy_and_trust(p_tenant_id uuid, p_child_id uuid, p_approved_by_name text)` | `PROCEDURE` | `ParentRepository.ApprovePrivacyAndTrustAsync` | Upserts approved Privacy & Trust consent record. |
| `fn_get_privacy_and_trust_approval_date(p_tenant_id uuid, p_child_id uuid)` | `FUNCTION` (returns `timestamptz`) | `ParentRepository.GetPrivacyAndTrustApprovalDateAsync` | Retrieves latest approval timestamp for Privacy & Trust. |
| `sp_save_authorized_pickup_with_person(p_tenant_id uuid, p_child_id uuid, p_first_name text, p_last_name text, p_relationship text, p_phone text, p_pickup_code text, p_notes text, INOUT p_pickup_id uuid, INOUT p_person_id uuid)` | `PROCEDURE` | `SecurePickupRepository.SaveAuthorizedPickupWithPersonAsync` | Atomically creates/updates person record and upserts authorized pickup in a single transaction. |
| `sp_delete_authorized_pickup(p_tenant_id uuid, p_child_id uuid, p_pickup_id uuid)` | `PROCEDURE` | `SecurePickupRepository.DeleteAuthorizedPickupAsync` | Safely soft-deletes authorized pickup record (`is_active = false`). |
| `fn_get_daycare_announcements(p_tenant_id uuid, p_organization_id uuid, p_branch_id uuid, p_active_only boolean, p_limit int)` | `FUNCTION` (returns `TABLE`) | `DaycareAnnouncementRepository.GetAnnouncementsAsync` | Fetches prioritized urgent and regular daycare announcement broadcasts. |
| `sp_create_daycare_announcement(...)` | `PROCEDURE` | `DaycareAnnouncementRepository.CreateAsync` | Creates a new daycare announcement broadcast. |
| `sp_set_daycare_announcement_status(p_id uuid, p_tenant_id uuid, p_is_active boolean)` | `PROCEDURE` | `DaycareAnnouncementRepository.DeactivateAsync` / `ReactivateAsync` | Toggles active state of daycare announcement. |
| `sp_delete_daycare_announcement(p_id uuid, p_tenant_id uuid)` | `PROCEDURE` | `DaycareAnnouncementRepository.DeleteAsync` | Deletes announcement record. |
| `fn_get_agent_tokens_by_user(p_tenant_id uuid, p_user_id uuid)` | `FUNCTION` (returns `TABLE`) | `AgentTokenRepository.GetUserTokensAsync` | Lists all agent personal access tokens for a user. |
| `fn_get_agent_token_by_hash(p_token_hash varchar)` | `FUNCTION` (returns `TABLE`) | `AgentTokenRepository.GetByHashAsync` | Authenticates and looks up active token by SHA hash. |
| `sp_create_agent_token(...)` | `PROCEDURE` | `AgentTokenRepository.CreateTokenAsync` | Registers a new agent personal access token. |
| `sp_revoke_agent_token(p_tenant_id uuid, p_token_id uuid)` | `PROCEDURE` | `AgentTokenRepository.RevokeTokenAsync` | Revokes agent PAT. |
| `sp_update_agent_token_last_used(p_token_id uuid)` | `PROCEDURE` | `AgentTokenRepository.UpdateLastUsedAsync` | Updates last-used timestamp on token usage. |
| `fn_get_agent_webhooks(p_tenant_id uuid)` | `FUNCTION` (returns `TABLE`) | `AgentWebhookRepository.GetTenantSubscriptionsAsync` | Lists webhook subscriptions for a tenant. |
| `fn_get_active_agent_webhooks_for_event(p_tenant_id uuid, p_event_type varchar)` | `FUNCTION` (returns `TABLE`) | `AgentWebhookRepository.GetActiveSubscriptionsForEventAsync` | Matches active webhooks subscribed to a specific event type. |
| `sp_create_agent_webhook(...)` | `PROCEDURE` | `AgentWebhookRepository.CreateSubscriptionAsync` | Registers a new webhook subscription. |
| `sp_delete_agent_webhook(p_tenant_id uuid, p_subscription_id uuid)` | `PROCEDURE` | `AgentWebhookRepository.DeleteSubscriptionAsync` | Deletes a webhook subscription. |




