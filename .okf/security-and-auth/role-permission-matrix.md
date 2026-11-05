---
okf_version: "0.2.0"
type: concept
title: "Role Hierarchy & Permission Matrix"
description: "Specification of user roles, legacy naming conventions, permission flags, and assignment scopes."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - roles
  - permissions
  - authorization
---

# Role Hierarchy & Permission Matrix

## Canonical Role Templates (`RoleTemplates.cs`)

| Canonical Template | String Token | Scope | Operational Authority |
|:---|:---|:---|:---|
| **Platform Admin** | `PlatformAdmin` | Platform | Platform-level operations and tenant lifecycle; strictly segregated from tenant business data. |
| **Organization Owner** | `OrganizationOwner` | Organization | Full authority across all branches, classrooms, staff, billing, and settings. |
| **Organization Admin** | `OrganizationAdmin` | Organization | Same as Owner (governance, billing, staffing, all branches). |
| **Branch Admin** | `BranchAdmin` | Branch | Operational control over assigned branch(es) and **all classrooms** within those branches. |
| **Lead Teacher** | `LeadTeacher` | Classroom | Assigned classroom(s), observation reviews, handover coordination, coverage requests. |
| **Classroom Teacher** | `ClassroomTeacher` / `Teacher` | Classroom | Assigned classroom(s) only; care routines, attendance marking, daily moments. |
| **Parent** | `Parent` | Family | Verified children only; notifications, daily timeline, media consent. |

## Legacy Aliases & Space Tolerance
Legacy strings in database seed scripts must be normalized:
* `"Org Admin"`, `"OrgAdmin"`, `"Daycare Admin"`, `"Admin"` -> Treat as `OrganizationAdmin`.
* `"Branch Admin"`, `"Branch Director"` -> Treat as `BranchAdmin`.
* `"Daycare Teacher"`, `"Assistant Teacher"`, `"AssistantTeacher"` -> Treat as `ClassroomTeacher`.

## Core Permissions (`Permissions.cs`)
* `Permissions.OrganizationManage`: Configure org details, branches, settings.
* `Permissions.BranchManage`: Manage assigned branch operations.
* `Permissions.StaffManage`: Manage teachers, view roster, mark teacher attendance.
* `Permissions.AttendanceRecord`: Mark child and teacher attendance.
* `Permissions.ClassroomManage`: Create and edit classrooms.

