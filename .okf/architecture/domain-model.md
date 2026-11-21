---
okf_version: "0.2.0"
type: concept
title: "Organizational Hierarchy and Domain Aggregates"
description: "Entity hierarchy covering Organization, Branch, Classroom, Child, and Teacher relationships."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - domain
  - entities
  - organization-hierarchy
---

# Organizational Hierarchy & Domain Aggregates

## Hierarchy Tree
```
Tenant (Top-level SaaS partition)
└── Organization (Daycare Provider Company)
    └── Branch (Physical facility / daycare center)
        ├── Classroom (Specific room, age group, e.g., Toddlers)
        │   ├── Children (Assigned via child.classroom_id)
        │   └── Teachers (Assigned via classroom_teachers)
        └── Staff / Branch Admins (Assigned via user_branch_assignments)
```

## Primary Domain Entities

### 1. Organization (`EnjoyEveryday.Domain.Entities.Organization`)
* Root business entity for a daycare provider.
* Holds billing configuration, high-level policies, and multi-branch management.

### 2. Branch (`EnjoyEveryday.Domain.Entities.Branch`)
* Represents a physical building/location (e.g., "Adayar Branch", "Downtown").
* Identified by `Id: Guid`, `OrganizationId: Guid`, `Name: string`.
* Branch Admins oversee all classrooms and staffing within their assigned branch.

### 3. Classroom (`EnjoyEveryday.Domain.Entities.Classroom`)
* Physical or logical grouping of children within a branch (e.g., "Adyar Toddlers").
* Holds capacity, statutory staff-to-child ratio rules, and age ranges.
* Belongs to exactly one `BranchId`.

### 4. Staff & Teachers (`EnjoyEveryday.Domain.Entities.Teacher`)
* Linked to identity via `user_id`.
* Mapped to branches via `user_branch_assignments` / `auth_user_branches`.
* Mapped to classrooms via `classroom_teachers` (with `is_primary` flag).
* Attendance tracked via `TeacherAttendance` (`ClockInTime`, `ClockOutTime`).

### 5. Children (`EnjoyEveryday.Domain.Entities.Child`)
* Assigned to a primary classroom via `ClassroomId`.
* Attendance tracked via `ChildAttendance` (`Status`, `ArrivalTime`, `DepartureTime`, `PickupPersonName`, `PickupVerificationMethod`).

---

## Type-Safe Constants & Enum Architecture
To preserve 100% backward compatibility with PostgreSQL `varchar` columns and Dapper object-relational mapping without risk of runtime serialization breakage, domain statuses and options are modeled via type-safe string constants and read-only sets:

| Namespace / Class | Purpose | Primary Constants |
|:---|:---|:---|
| `EnjoyEveryday.Domain.Constants.JourneyTypes` | Observation and portfolio entry categories | `Observation`, `ActivityObservation`, `LittleMoment`, `Discovery`, `Milestone`, `Attendance`, `ArtAndSensory` |
| `EnjoyEveryday.Domain.Constants.ParentVisibilityStatuses` | Review workflow states for family sharing | `Draft`, `PendingReview`, `Approved` |
| `EnjoyEveryday.Domain.Constants.AttendanceStatuses` | Attendance register states and helpers | `Present`, `CheckedOut`, `Late`, `Absent`, `Excused`, `NotMarked` |
| `EnjoyEveryday.Domain.Constants.EnrollmentStages` | Admissions funnel stages | `Inquiry`, `TourScheduled`, `ApplicationSubmitted`, `Review`, `Waitlist`, `OfferSent`, `Enrolled`, `Withdrawn`, `Graduated` |
| `EnjoyEveryday.Shared.Audit.AuditActions` | Structured audit log actions by domain module | `Journey.*`, `DailyCare.*`, `Attendance.*`, `Auth.*`, `Commercial.*`, `Parent.*`, `Operations.*` |
| `EnjoyEveryday.Shared.Audit.AuditEntityTypes` | Entity names for audit entries | `ChildJourneyEntry`, `ChildMilestone`, `ChildCareEvent`, `ChildAttendance`, `User`, `Child` |
| `EnjoyEveryday.Application.Constants.DailyOperationReasons` | Validations for operational day readiness | `ChildJourneyObservation`, `ChildAttendance`, `TeacherAttendance`, `NapRoutine`, `WakeUpRoutine`, `TeacherHandover` |


