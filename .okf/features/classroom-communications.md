---
okf_version: "0.2.0"
type: concept
title: "Classroom Communications & Shift Handovers"
description: "Teacher shift handovers, incident reporting, classroom announcements, and parent messaging."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - communication
  - handover
  - messages
  - incidents
---

# Classroom Communications & Shift Handovers

## Architectural Components

* **Service**: `IClassroomCommunicationService` (`EnjoyEveryday.Application.Services`)
* **Entities**:
  - `TeacherHandover`: Captures morning-to-afternoon shifts (room supplies, health alerts, behavioral notes).
  - `ClassroomMessage`: Real-time announcements between staff members in a branch.
  - `IncidentReport`: Formal safety incidents with parent acknowledgment gates and supervisor approvals.

## UI Endpoints
* **Cockpit Handover Modal**: Accessed from `TeacherWorkspace.razor` via the "+ Handover" action button.
* **Classroom Messages**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/Communication/ClassroomMessages.razor` wrapping `ClassroomMessagesView.razor`.
* **On-Demand Loading**: `ClassroomMessagesView` loads conversation metadata on initialization without auto-selecting the first child. Messages are loaded on-demand only when a teacher explicitly clicks a child conversation, preventing unnecessary tenant-wide message payload loading.


