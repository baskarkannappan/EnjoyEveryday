# Open Knowledge Format (OKF): Architecture & Component Map

> [!IMPORTANT]
> **Mandatory Policy for AI Agents (Antigravity)**:
> 1. **Refer to `.okf/` on Every Task**: The official, granular Open Knowledge Format catalog lives in [`.okf/`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/index.md). Agents MUST consult [`.okf/index.md`](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/index.md) before performing exploratory code or database searches.
> 2. **Continuous Maintenance Rule**: Whenever any new feature, stored procedure, database table, role rule, or route is created or refactored, the agent MUST update the corresponding `.okf/` document(s) in the same task to keep the knowledge catalog 100% current.

---

## 1. Solution Architecture & Project Hierarchy

```
EnjoyEveryday (Root)
│
├── src/EnjoyEveryday.Domain/              # Pure Domain: Entities, Repository Interfaces, DTOs, Enums
├── src/EnjoyEveryday.Application/         # Core Services & Business Rules (Attendance, Care, Planning)
├── src/EnjoyEveryday.Infrastructure/      # DB Context, Repositories, PostgreSQL Stored Procedures
├── src/EnjoyEveryday.UI.Shared/           # Shared Razor Components, Modals, Dialogs, Foundations
│
├── src/EnjoyEveryday.Daycare.Web/         # Primary Daycare & Teacher Web Portal (Port 7003)
├── src/EnjoyEveryday.Admin.Web/           # Platform & Operations Admin Web Portal (Port 7001)
├── src/EnjoyEveryday.Parent.Web/          # Parent Engagement Portal (Port 7002)
├── src/EnjoyEveryday.Api/                 # Backend RESTful API Gateway
└── src/EnjoyEveryday.AiAgent/             # Google ADK Python Engine (Deterministic fallback required)
```

---

## 2. Feature-to-File Directory Map

| Feature Area | Razor / UI Components | Application Service | Domain Entity / DTO |
| :--- | :--- | :--- | :--- |
| **Teacher Workspace & Cockpit** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/TeacherWorkspace.razor` | `IDaycareOperationsBoardService`<br>`IChildDailyCareService` | `ClassroomChildCareStatusDto`<br>`OperationsBoard` |
| **Attendance Tracking** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/`<br>`src/EnjoyEveryday.UI.Shared/Components/Attendance/` | `AttendanceService` | `ChildAttendance`<br>`TeacherAttendance` |
| **Daily Care & Routines**<br>*(Diapers, Meals, Naps)* | `src/EnjoyEveryday.UI.Shared/Components/DailyCare/ChildDailyCareSection.razor`<br>`src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePage.razor` | `IChildDailyCareService`<br>`(ChildDailyCareService.cs)` | `ChildCareEvent`<br>`ChildNapSession`<br>`CareEventTypes` |
| **Journey Moments & Observations** | `src/EnjoyEveryday.UI.Shared/Components/ChildJourney/AddJourneyMomentDialog.razor`<br>`src/EnjoyEveryday.Daycare.Web/Components/Pages/Journey/` | `IChildJourneyService`<br>`(ChildJourneyService.cs)` | `ChildJourneyEntry`<br>`ChildJourneyEvidence` |
| **Experience Activity Runner** | `src/EnjoyEveryday.UI.Shared/Components/ActiveSessionModal/ActiveSessionModal.razor` | `ExecutionService`<br>`PlannerService`<br>`ExperienceService` | `ExperienceSession`<br>`ExperienceParticipation` |
| **Shift Handovers & Communication**| `src/EnjoyEveryday.Daycare.Web/Components/Pages/ClassroomMessages/`<br>`TeacherWorkspace.razor` (Handover modal) | `IClassroomCommunicationService` | `TeacherHandover`<br>`ClassroomMessage` |
| **Classroom Rhythm & Timetable** | `TeacherWorkspace.razor` (Section E)<br>`src/EnjoyEveryday.Daycare.Web/Components/Pages/Planning/` | `PlannerService` | `ClassroomSchedule`<br>`TodayActivityItemDto` |

---

## 3. Core Business Rules & Invariants

1. **Attendance Gate for Child Care & Activities**:
   - Only children with attendance status `Present` (`child.IsCheckedIn == true`) can receive care event logs (diapers, BM, meals, rest/naps, hygiene, comfort) and journey moment observations.
   - When a child is `Absent`, `CheckedOut`, or `Not Checked In`:
     - Live Cockpit locks action buttons and displays a locked indicator with an option to mark present or re-admit.
     - `ActiveSessionModal` sets status to `Absent` and hides the `+ Log Moment` button.
     - `AddJourneyMomentDialog` displays a warning banner and disables selecting absent children.
     - `ChildDailyCareSection` disables submission and bulk inclusion.

2. **Operating Hours (7:00 AM - 7:00 PM)**:
   - Daycare operating hours end at 7:00 PM.
   - After 7:00 PM, new check-ins and batch arrivals are locked (`_isDaycareClosed == true`).

3. **Multi-Tenant Isolation**:
   - Every database query, repository call, and service method MUST filter by `TenantId`.
   - Never query or update data without scoping to the active tenant.

4. **Deterministic AI Fallback**:
   - Core operations must never depend on the availability of the AI agent. If the AI service is unreachable, deterministic rule fallbacks must execute immediately.

---

## 4. Database Connection & Environment Quick-Reference

- **Host**: `localhost` | **Port**: `5433` | **Database**: `enjoyeveryday`
- **Username**: `postgres` | **Password**: `postgres`
- **CLI Command**:
  ```powershell
  $env:PGPASSWORD="postgres"; & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday -c "<SQL_QUERY>"
  ```
- **Backup Script**: `.\backup_db_to_git.ps1` (dumps to `database_backups/enjoyeveryday_latest.sql` and pushes to git).

---

## 5. Fast Development & Build Protocols

1. **Never Build the Full Solution**:
   - Full solution builds take 40–70+ seconds.
   - Always run targeted project builds with `--no-restore`:
     ```powershell
     dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore
     ```
2. **Database Changes Require No Rebuilds**:
   - SQL schema, stored procedures, and row edits take effect immediately in PostgreSQL without recompiling C# or Razor.

