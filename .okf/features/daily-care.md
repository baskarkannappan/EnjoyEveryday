---
okf_version: "0.2.0"
type: concept
title: "Daily Care Routines & Shift Handover"
description: "Routines for diapers/potty, meals, naps, and teacher handovers."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - daily-care
  - routines
  - naps
  - diapers
  - handover
---

# Daily Care Routines & Shift Handover

## Component Locations
* **Cockpit / Summary**: `src/EnjoyEveryday.UI.Shared/Components/DailyCare/ChildDailyCareSection.razor`
* **Detailed Care Page**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePage.razor`
* **Care Plans**: `src/EnjoyEveryday.UI.Shared/Components/DailyCare/ChildCarePlansView.razor`
* **Care Supplies / Cubby**: `src/EnjoyEveryday.UI.Shared/Components/DailyCare/ChildCareSuppliesView.razor`

## Routine Aggregates & Services
* **Service**: `IChildDailyCareService` (`EnjoyEveryday.Application.Services`)
* **Diapers & Potty**: `ChildCareEvent` with `EventType = "Diaper"` (Wet, BM, Both, Dry, Potty).
* **Meals & Feeding**: `ChildCareEvent` with `EventType = "Meal"` (Breakfast, Lunch, Snack, Bottle ounces).
* **Nap Sessions**: `ChildNapSession` (`StartTime`, `EndTime`, `Quality`).
* **Handovers**: `TeacherHandover` (`ShiftType`, `Notes`, `ClassroomId`, `AuthorUserId`).

