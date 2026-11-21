---
okf_version: "0.2.0"
type: concept
title: "Attendance Management & Security Gates"
description: "Workflows and components for branch rosters, room views, child check-in/departure, and teacher attendance."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - attendance
  - roster
  - check-in
  - check-out
---

# Attendance Management & Security Gates

## Page & Component Architecture

| View / Page | URL Route | Core Component | Responsible Roles |
|:---|:---|:---|:---|
| **Overview** | `/management/attendance`, `/attendance` | `AttendanceOverviewView.razor` | Org Admin, Branch Admin, Teacher (scoped) |
| **Branch View** | `/management/attendance/branches/{BranchName}` | `BranchDetailSection.razor` (`IsRoomView = false`) | Org Admin, Branch Admin |
| **Classroom View** | `/management/attendance/classrooms/{ClassroomName}` | `BranchDetailSection.razor` (`IsRoomView = true`) | Org Admin, Branch Admin, Classroom Teacher |
| **Children Monthly Register** | `/attendance/monthly-children`, `/management/attendance/monthly-children` | `MonthlyChildAttendancePage.razor`, `MonthlyChildAttendanceGrid.razor` | Org Admin, Branch Admin, Lead Teacher, Teacher, Educator |
| **Staff Monthly Register** | `/attendance/monthly-teachers`, `/management/attendance/monthly-teachers` | `MonthlyTeacherAttendancePage.razor`, `MonthlyTeacherAttendanceGrid.razor` | Org Admin, Branch Admin, Lead Teacher, Teacher, Educator |
| **Monthly Attendance Hub** | `/attendance/monthly`, `/management/attendance/monthly` | `MonthlyAttendanceHub.razor` | Org Admin, Branch Admin, Lead Teacher, Teacher, Educator |

> [!NOTE]
> Monthly child and teacher register aggregation is encapsulated in `IMonthlyRegisterService` (`MonthlyRegisterService`), with `AttendanceService` (`IAttendanceService`) delegating register queries while `ChildAttendance` exposes domain transition helpers (`CheckIn`, `CheckOut`, `MarkAbsent`).

## Component Hierarchy: `BranchDetailSection.razor`
`BranchDetailSection.razor` renders:
1. **Filters Bar**: Date selector, person search, type filter (`both`, `children`, `teachers`).
2. **KPI Strip**: `AttendanceStatStrip.razor` (Children present, Teachers present, Not marked, Missing checkout).
3. **Tabbed Content**:
   * `classrooms`: `AttendanceClassroomTable.razor` (branch-level list with "Open →" action).
   * `children`: `AttendancePeopleTable.razor` (`Type = "children"`).
   * `teachers`: `AttendancePeopleTable.razor` (`Type = "teachers"`).

## Key Workflows

### 1. Child Check-In, Secure Departure & Action Locking
* **Daily Operations Gate Workflow (Start & End of Day)**:
  * **Morning Gate (Branch Admin)**: Each branch must be opened in the morning by the Branch Admin or Director (`sp_open_branch_day`). Until the branch is opened, classrooms cannot be opened and child attendance is locked.
  * **Classroom Gate (Lead Teacher / Educator)**: Once the branch is opened, the lead teacher or educator opens their classroom (`sp_open_classroom_day`). Only after the classroom is opened can educators mark child attendance, conduct learning activities, or log daily care.
  * **Classroom End of Day (Teacher)**: When the classroom teacher completes their day (`sp_close_classroom_day`), the system auto-checks out any remaining present children with the specified departure time, completes any remaining active experience sessions, and locks classroom logs.
  * **Branch End of Day (Branch Admin)**: When all classrooms within the branch have completed end-of-day closure, the Branch Admin can close the branch (`sp_close_branch_day`). The system validates that `0` classrooms remain open before allowing the branch to seal for the day.
* **Higher-Level Constraint: Strict Branch Closed Lockdown**:
  * **Zero Operational Activity When Closed**: When the branch facility is closed, the system strictly blocks ALL operational actions across the entire facility:
    1. **No teacher attendance**: Staff clock-ins, duty assignments, and staff break recordings (`ToggleStaffBreak`) are blocked.
    2. **No activity**: Activity sessions, runs, and completions in `ExecutionService` and `TodayActivities` are blocked.
    3. **No classroom readiness**: Classroom readiness evaluations, opening rooms, and readiness logs in `ClassroomReadinessService` and `DaycareOperationsBoardService` are blocked.
    4. **No student attendance**: Child arrivals, check-ins, re-admits, and departures in `AttendanceService` and `SecurePickupService` are blocked.
    5. **No log activity**: Diaper changes, meals, naps, and moment captures in `ChildDailyCareService` are blocked.
    6. **No daycare operation**: Morning daycare rhythm, batch actions, and daily operations board updates are locked.
    7. **No classroom operation**: Cockpit action bar (`Check-In All`, `Group Snack`, `Table Lunch`, `Potty Line`, `Group Rest`, `Timer`) and individual child cards are locked.
  * **Service Enforcement**: `IDailyOperationsService.ValidateBranchOperationsAllowedAsync` and `ValidateClassroomOperationsAllowedAsync` cross-check the daily status from PostgreSQL stored functions `fn_get_branch_daily_status` and `fn_get_classroom_daily_status`. Attempted operations throw descriptive `InvalidOperationException` errors.
  * **UI Locking**: `TeacherWorkspace.razor`, `BranchDetailSection.razor`, `StaffRosterView.razor`, and `TodayActivities.razor` render prominent locked banners with disabled buttons (`opacity: 0.5`, `cursor: not-allowed`) and informative warning toasts.
* **Mark Present / Check-In**: Dropdown in `AttendancePeopleTable`, 1-tap card in `TeacherWorkspace.razor`, or bulk action "Mark selected present" / "Check-In All". Sets `status = 'Present'` and `arrival_time`.
* **Departure / Pickup Verification**: Clicking "Check Out" (or `🚪` in `TeacherWorkspace.razor`) opens `SecurePickupModal.razor`. Requires collector name, relationship, and verification method (PIN, ID Check, Signature).
* **Departure Status & Action Locking**:
  * Upon recording departure, `SecurePickupService.VerifyAndRecordChildDepartureAsync` sets `attendanceRecord.Status = "CheckedOut"` and `DepartureTime = departureTime`.
  * `ChildDailyCareService.GetClassroomOverviewAsync` evaluates departed children (`Status == "CheckedOut"` or `DepartureTime != null`), setting `IsCheckedIn = false` and `AttendanceStatus = "CheckedOut"`.
  * In `TeacherWorkspace.razor`, departed children display a `Checked Out` status badge and `Departed [time]`. All routine actions (diapers, meals, naps, moments, checkout) are completely locked, showing `🚪 Checked out • Logs locked`.
  * Individual actions (`QuickRecordDiaperAsync`, `QuickRecordMealAsync`, `ToggleNapAsync`, `OpenQuickMomentModalAsync`) reject attempts if `!child.IsCheckedIn`.
  * **Re-admit Flow**: Teachers can click `Re-admit` if checkout was recorded in error. `sp_record_child_attendance` detects `p_status = 'Present'` with null departure time, resetting `status = 'Present'` and clearing `departure_time`, collector names, and pickup verification fields back to null.
* **End of Day & Closed-Day Prohibitions**:
  * `EndOfDayProcessModal.razor` and `DaycareEndOfDayBackgroundService` auto-close unclosed records at end of day (7:00 PM). Unclosed child attendances are marked with `departure_time = '19:00:00'` and status `CheckedOut`. Unclosed teacher attendances are marked with `clock_out_time = '19:00:00'` and status `ClockedOut`. Remaining active activities are marked `Completed`.
  * **Daycare Closed-State Enforcement (`AttendanceService`, `SecurePickupService`, `BranchDetailSection.razor`, `AttendancePeopleTable.razor`, `TeacherWorkspace.razor`)**: When the daycare is closed (weekends Saturday/Sunday, before 7:00 AM or at/after 7:00 PM, past dates, or after End-of-Day execution via `EndOfDayService.IsDaycareClosedForDate`), all attendance operations (check-in, check-out, status changes, bulk marking present, and secure pickup departure recording) for children, teachers, and staff are locked in both application services and UI components.
  * **Unified Testing & Dev Bypass (`Bypass_daycare_closed`)**: To unblock local testing, demonstrations, or weekend development across all pages and services, set `"Bypass_daycare_closed": true` in `appsettings.json` or `appsettings.Development.json` (or set environment variable `BYPASS_DAYCARE_CLOSED=true`). All services (`AttendanceService`, `ExecutionService`, `PlannerService`, `SecurePickupService`) and UI views (`BranchDetailSection`, `TeacherWorkspace`, `HarvestView`, `TodayActivities`) centrally consult this configuration. Setting it to `false` re-enables full statutory enforcement.
* **Teacher Workspace Rhythm Timeline**: In `TeacherWorkspace.razor` (for Teacher, Lead Teacher, Educator), a live timeline displays the daycare rhythm: `(started) ➔ (X children and Y teacher present) ➔ Z activity planned (W started) ➔ (ended)`. On end of day (>= 7:00 PM), the system automatically triggers auto-closure.

### 2. Teacher Attendance & Roster
* **Viewing**: Org Admin and Branch Admin can view all teachers assigned to the branch or classroom.
* **Editing (`CanEditTeacherAttendance`)**: Org Admin and Branch Admin have authority to check in, change status, and check out staff.
* **Checkout**: Clicking "Check Out" invokes `AttendanceService.MarkTeacherAttendanceAsync` with current timestamp.

### 3. Read-Only Monthly Attendance Registers (Children & Staff)
* **Monthly Matrix Structure**:
  * Inspired by the classic attendance register book matrix, modernized into a responsive, touch-friendly grid with sticky child/staff name columns and days 1..31.
  * Status badges: `P` (Present), `L` (Late), `A` (Absent), `E` (Excused for children), `LV` (Approved Leave for staff), `-` (Weekend/Off). Hover/tap shows full arrival/departure times and notes.
  * Mobile Experience: Sticky frozen `#` and Name column with smooth horizontal panning and an optional "Cards" view mode with mini calendar tracks for small phone displays.
* **Header & Scoping Requirements**:
  * Top header displays Organisation, Branch, and Location badges along with a Month Switcher (`◀ Prev`, `Next ▶`, quick month jump dropdown, `Today`).
  * Strict branch and location scoping: Org Admins can switch between accessible branches in their organization; Branch Admins, Lead Teachers, Teachers, and Educators are scoped exclusively to their authorized branch/location.
* **Zero Inline Queries (Stored Procedures Only)**:
  * `public.sp_get_branch_location_context(p_tenant_id, p_branch_id, INOUT cur)`: Fetches organization name, branch name, and physical location.
  * `public.sp_get_user_accessible_branches(p_tenant_id, p_user_id, p_is_org_admin, INOUT cur)`: Retrieves branches accessible to the logged-in user according to explicit assignments and roles.
  * `public.sp_get_monthly_child_attendance(p_tenant_id, p_branch_id, p_year, p_month, p_classroom_id, INOUT cur)`: Returns all enrolled/active children and their deduplicated monthly attendance marks via refcursor.
  * `public.sp_get_monthly_teacher_attendance(p_tenant_id, p_branch_id, p_year, p_month, INOUT cur)`: Returns all active teachers and educators with clock-in/out records for the month via refcursor.

### 4. Child Journey & Parent Portal Sync & Multi-Branch OrgAdmin Access
* **Check-In & Check-Out in Child Journey & Today Timeline**: `SecurePickupService.VerifyAndRecordChildDepartureAsync` records an approved `ChildJourneyEntry` (`JourneyType = "Attendance"`, `Title = "Classroom Departure / Check-Out"`) so departures appear in both Daycare Child Journey and Parent Portal (`/journey`). `ParentService.GetApprovedChildJourneyAsync` also surfaces today's `ArrivalTime` (Check-In) and `DepartureTime` (Check-Out) from `child_attendance` if not yet logged, and both `GetChildTodaySummaryAsync` and `GetApprovedChildJourneyAsync` normalize UTC timestamps to local time for chronological sorting.
* **Parent Portal Activity/Memory Photos**: `ParentRepository.GetApprovedJourneyEntriesAsync` loads `fn_get_parent_journey_evidence` whenever photo consent is `Signed` or no `PhotoVideoConsent` document exists yet (withholding photos only when a consent document exists and is not `Signed`). `MediaEndpoints` (`/api/media/{id}/{variant}`) also authorizes parents via `IParentRepository.GetAuthorizedChildrenAsync` when the parent's login `TenantId` claim differs from the enrolled child's `TenantId`.
* **OrgAdmin Branch-Closed Bypass**: `BranchOperationalUiGuard` (`EvaluateRouteAsync` & `IsOperationalNavItemLocked`) does not block operational routes or lock sidebar navigation when `profile.IsOrgAdmin` and `ActiveView == "OrgAdmin"`, and `ExperiencePlanning.razor.cs` defaults to the first open branch (`OperationalStatus == "Open"`), allowing Org Admins to switch between closed and open branches across attendance, events, planning, execution, harvest/insights, and child journey.


