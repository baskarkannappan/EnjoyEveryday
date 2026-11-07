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

## Mobile Ergonomics & Navigation
* **Compact Mobile Care Entry**: `ChildDailyCareSection.razor.css` enforces a 3-column (`repeat(3, minmax(0, 1fr)) !important`) grid for `.activity-tiles-grid` with compact tile padding and typography on viewports `<= 768px`, so all 6 care activities (`Diaper change`, `Meal`, `Sleep / rest`, `Toileting`, `Hygiene`, `Comfort`) fit above the fold on mobile devices (`375px` width).
* **Auto-Closing Mobile Navigation Drawer**: `MainLayout.razor` (`Daycare.Web` and `Admin.Web`) binds `@ref="_hamburger"` on `<FluentLayoutHamburger>` and calls `_hamburger.HideAsync()` both on `NavigationManager.LocationChanged` and via a cascading `CloseMobileMenu` callback triggered when any `.nav-group` item in `NavMenu.razor` is clicked.


