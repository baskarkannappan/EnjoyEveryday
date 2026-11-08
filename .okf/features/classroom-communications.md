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

* **Service**: `IClassroomCommunicationService`, `IDaycareAnnouncementService` (`EnjoyEveryday.Application.Services`)
* **Entities**:
  - `TeacherHandover`: Captures morning-to-afternoon shifts (room supplies, health alerts, behavioral notes).
  - `ClassroomMessage`: Real-time announcements between staff members in a branch.
  - `DaycareAnnouncement`: Org and branch announcements published by administrators, synchronized to parent portals and educator streams with priority urgency badges.
  - `IncidentReport`: Formal safety incidents with parent acknowledgment gates and supervisor approvals.

## UI Endpoints
* **Cockpit Handover Modal**: Accessed from `TeacherWorkspace.razor` via the "+ Handover" action button.
* **Classroom Messages**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/Communication/ClassroomMessages.razor` wrapping `ClassroomMessagesView.razor`.
* **Daycare Announcements & Notifications**: 
  - Dedicated administrator broadcast management at `src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/AnnouncementsPage.razor` (`/announcements`, `/management/announcements`). Allows Org Admins and Branch Admins to publish scoped announcements with tags (Notice, Reminder, Urgent Alert, Weather, Event, Policy).
  - Admins can deactivate announcements via `DeactivateAnnouncementAsync`, instantly removing them from all active parent and educator screens while retaining audit history in the Deactivated / Archived tab, with one-click reactivate or permanent deletion options.
  - Urgent announcements and broadcasts synchronize immediately to:
    - Parent Home (`https://localhost:7005/`, `Today.razor`) as top-level broadcast alert cards.
    - Parent Messages (`https://localhost:7005/messages`, `Messages.razor`) as top-level broadcast alert pills.
    - Parent Family & Care Profile (`https://localhost:7005/family`, `FamilyProfile.razor`) in the live announcements feed.
* **On-Demand Loading & Read Tracking**: `ClassroomMessagesView` loads conversation metadata on initialization without auto-selecting the first child. Messages are loaded on-demand only when a teacher explicitly clicks a child conversation. Parent message reads execute `sp_mark_parent_communications_read_by_parent`, instantly resetting unread counters across parent today summaries.


