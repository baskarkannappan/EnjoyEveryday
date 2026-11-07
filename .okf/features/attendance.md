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

## Component Hierarchy: `BranchDetailSection.razor`
`BranchDetailSection.razor` renders:
1. **Filters Bar**: Date selector, person search, type filter (`both`, `children`, `teachers`).
2. **KPI Strip**: `AttendanceStatStrip.razor` (Children present, Teachers present, Not marked, Missing checkout).
3. **Tabbed Content**:
   * `classrooms`: `AttendanceClassroomTable.razor` (branch-level list with "Open →" action).
   * `children`: `AttendancePeopleTable.razor` (`Type = "children"`).
   * `teachers`: `AttendancePeopleTable.razor` (`Type = "teachers"`).

## Key Workflows

### 1. Child Check-In & Departure
* **Mark Present**: Dropdown in `AttendancePeopleTable` or bulk action "Mark selected present".
* **Departure / Pickup**: Clicking "Check Out" opens `SecurePickupModal.razor`. Requires collector name, relationship, and verification method (PIN, ID Check, Signature).
* **End of Day & Closed-Day Prohibitions**:
  * `EndOfDayProcessModal.razor` and `DaycareEndOfDayBackgroundService` auto-close unclosed records at end of day (7:00 PM). Unclosed child attendances are marked with `departure_time = '19:00:00'` and status `CheckedOut`. Unclosed teacher attendances are marked with `clock_out_time = '19:00:00'` and status `ClockedOut`. Remaining active activities are marked `Completed`.
  * **Daycare Closed-State Enforcement (`AttendanceService`, `SecurePickupService`, `BranchDetailSection.razor`, `AttendancePeopleTable.razor`, `TeacherWorkspace.razor`)**: When the daycare is closed (weekends Saturday/Sunday, before 7:00 AM or at/after 7:00 PM, past dates, or after End-of-Day execution via `EndOfDayService.IsDaycareClosedForDate`), all attendance operations (check-in, check-out, status changes, bulk marking present, and secure pickup departure recording) for children, teachers, and staff are strictly prohibited and locked in both application services and UI components.
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


