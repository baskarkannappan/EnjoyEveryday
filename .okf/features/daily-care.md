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
* **Handovers**: `TeacherHandover` (`ShiftType`, `Notes`, `ClassroomId`, `AuthorUserId`). Features **1-Click Auto-Drafting from Active Care Logs** in `ChildDailyCareSection.razor` which synthesizes room energy, child nap durations, meal intakes, allergy flags, and outstanding care timers automatically from `_classroomOverview`.

## Parent Daily Care Feed & Chronological Timeline
* `ParentService.GetChildDailySummaryAsync` queries `IChildDailyCareService.GetParentDailyCareReportAsync(effectiveTenantId, childId, date)` using the child's effective tenant scope.
* Combines classroom arrival check-in, routine care events (meals, snacks, diaper changes, nap sessions, comfort observations), scheduled learning experiences, moments, and departure checkout.
* All entries are sorted strictly in ascending chronological time-series order (`OrderBy(t => t.EventTime)`) and displayed in `ActivityTimeline.razor` and `DailyUpdateCard.razor` with dedicated category badges and iconography.

## Mobile Ergonomics & Navigation
* **Compact Mobile Care Entry**: `ChildDailyCareSection.razor.css` enforces a 3-column (`repeat(3, minmax(0, 1fr)) !important`) grid for `.activity-tiles-grid` with compact tile padding and typography on viewports `<= 768px`, so all 6 care activities (`Diaper change`, `Meal`, `Sleep / rest`, `Toileting`, `Hygiene`, `Comfort`) fit above the fold on mobile devices (`375px` width).
* **Auto-Closing Mobile Navigation Drawer**: `MainLayout.razor` (`Daycare.Web` and `Admin.Web`) binds `@ref="_hamburger"` on `<FluentLayoutHamburger>` and calls `_hamburger.HideAsync()` both on `NavigationManager.LocationChanged` and via a cascading `CloseMobileMenu` callback triggered when any `.nav-group` item in `NavMenu.razor` is clicked.
* **Responsive Dashboard Snapshot Metrics (`/today`, `/management/dashboard`, `/branch-dashboard`)**: `ee-foundation.css`, `OrgAdminDashboard.razor.css`, and `BranchAdminDashboard.razor.css` enforce a responsive 2-column grid (`repeat(2, 1fr) !important; gap: 8px !important;`) on mobile `<= 768px` for `.snapshot-metrics-grid`, with the 5th metric card spanning full width horizontally (`grid-column: 1 / -1 !important;`). This keeps all key operational numbers (children in care, attendance status, experiences, staff ratios, messages) above the fold with 20px typography and 10px compact padding, preventing vertical card blowout.

## Teacher Workspace Everyday Productivity Suite (`/classroom`, `/my-classroom`)
`src/EnjoyEveryday.Daycare.Web/Components/Pages/TeacherWorkspace.razor` provides 6 high-speed educator features designed for fast, frictionless daily classroom routines:
0. **Daily Operational Gate, Branch Lockdown & Classroom Open/Close Workflow**:
   - **Strict Higher-Level Branch Closed Lockdown**: When a branch is closed, all classroom operations are completely blocked:
     - No teacher attendance or staff breaks (`ToggleStaffBreak`).
     - No activities or learning sessions (`StartExperienceAsync`).
     - No classroom readiness setup or inspections.
     - No student arrivals, check-in (`CheckInChildAsync`, `MarkAllScheduledPresentAsync`), or departures (`CheckOutChildAsync`).
     - No daily care logs (diaper changes, meals, naps, moments).
     - No classroom operations or batch cockpit tools (`Check-In All`, `Group Snack`, `Table Lunch`, `Potty Line`, `Group Rest`, `Timer`).
     - Action buttons and child quick-actions are disabled with locked tooltips and trigger informative warning toasts.
     - Enforced comprehensively across both `/classroom` cockpit and `/daily-care` page (`ChildDailyCareSection`), preventing quick care, bulk routines, sleep rest, and shift handovers when the branch or classroom is closed.
     - Backend business services (`ChildDailyCareService.RecordBatchCareEventsAsync`, `WakeChildAsync`, `SubmitHandoverAsync`, and `ChildJourneyService.CreateJourneyEntryAsync`) strictly validate `ValidateBranchOperationsAllowedAsync` and `ValidateClassroomOperationsAllowedAsync`.
   - **Branch Dependent**: Classroom cannot be opened until Branch Admin marks the branch open for the day (`sp_open_branch_day`).
   - **Open Classroom Gate**: Lead teacher / Educator opens the room (`sp_open_classroom_day`). Attendance, learning sessions, and care actions remain locked until opened.
   - **End of Day Auto-Checkout**: Clicking `Close Classroom (End of Day)` (`sp_close_classroom_day`) prompts the educator with a summary modal showing present children and active activities. Upon confirming, it automatically departs all remaining children with the chosen departure time, completes active sessions, and seals classroom logs for the day.
1. **15-Second "Pickup Cheat-Sheet" (At the Door)**:
   - 1-tap `📋 Cheat-Sheet` button is placed neatly in `.child-card-footer` alongside the `🚪 Check Out` button. This keeps the child card header clean and spacious for child full name, arrival timestamp, and status badge (`● Present`), preventing label breaks or cramped cards.
   - Summarizes 3 key bullets in under 3 seconds: Nap duration/status, meal/snack intake, active experience highlights, allergy/safety alerts, and a tailored parent greeting prompt (e.g. *"Ask Noah about the ice bridge he made today!"*).
   - Allows immediate checkout directly from the cheat-sheet modal (`🚪 Check Out Child`).
2. **Hands-Free "Voice-to-Moment" Observation Capture with Text Fallback**:
   - Web Speech API integration (`voiceRecognition.js`) allowing teachers to dictate observations hands-free when hands are engaged in activities or care routines.
   - Automatic real-time transcription appending directly into the observation text area and child quote field.
   - Transparent non-AI text fallback: If Web Speech is unsupported or permission is denied, an informative fallback banner activates and the educator enters text directly.
3. **"Allergy & Sensory Material Safety Shield"**:
   - Integrated into Next Planned Experience and Active Running Experience cards.
   - Cross-references ingredients/materials against currently checked-in children.
   - Displays `🛡️🟢 Material Safety Shield: All Safe` when no present children have allergies, or `🛡️⚠️ Safety Shield Alert` naming children with allergies and suggesting certified adaptations.
4. **Group Care Quick-Tickers ("Whole-Table" Sync)**:
   - **Table Lunch**: 1-tap batch button in `cockpit-batch-bar` marking `Most (75%)` lunch for all checked-in children seated at once.
   - **Post-Nap Potty/Diaper Checklist**: 1-tap `🧷 Potty Line` modal displaying all present children in a clean single-screen table with 1-tap buttons (`✨ Dry`, `💧 Wet`, `💩 BM`, `🚽 Potty`) to complete line routines in 20 seconds.
5. **Transition Chime & Visual Gentle Timer**:
   - Accessible via `⏳ Timer` in `cockpit-batch-bar`.
   - Offers quick presets (2 min quick clean, 5 min wind-down, 10 min circle prep) with an animated visual countdown bar.
   - Synthesizes a calming dual-sine chime (528 Hz & 660 Hz) via Web Audio API without requiring external audio asset files, prompting educators with clean-up cues.
6. **Real-Time Co-Teacher Live Sync**:
   - Periodic 15-second background synchronization polling (`_coTeacherSyncTimer`) comparing care timestamps.
   - Prompts a 3.5-second gentle notification (`🔄 Co-Teacher Live Sync`) whenever a co-teacher logs a meal, diaper, or nap in the same room.
   - Includes a manual `🔄 Sync` button in the cockpit header for instant multi-device alignment.
7. **Zero-Click Adaptive Classroom Routine Smart Bar**:
   - Time-aware contextual bar mounted above the live cockpit dynamically resolving the active room rhythm:
     - `Arrival` (07:00–10:30): Prominently surfaces `✓ 1-Tap Check-In All Expected`.
     - `Meal` (10:30–12:45): Prominently surfaces `🍽️ 1-Tap Record Whole Table Lunch`.
     - `Rest` (12:45–15:00): Prominently surfaces `😴 1-Tap Rest All Awake` with live resting vs awake counter (`X resting, Y awake`).
     - `Diaper` (15:00–16:30): Surfaces direct diaper chips and `🧷 Open Rapid Potty Checklist`.
     - `Pickup` (16:30–19:00): Surfaces doorway cheat-sheets and `🌙 Close Classroom (End of Day)`.
   - Educators can run on automatic clock mode (`🕒 Auto Clock`) or override any routine with a single pill tap.
8. **Direct 1-Tap Rapid Care Strip (Zero Popovers)**:
   - Each checked-in child card includes a direct rapid-action strip: `[ 💧 Wet ]`, `[ 💩 BM ]`, `[ ✨ Dry ]`, `[ 🥣 Ate All ]`, `[ 🥪 Half ]`.
   - Bypasses all modal dialogs and dropdown popovers, persisting care logs in a single tap and immediately updating child card badges.
9. **Floating Mobile Thumb Bar (Thumb Zone Ergonomics)**:
   - Fixed to the bottom of the viewport on mobile devices (`<= 768px`) with safe-area insets (`.ee-floating-thumb-bar`).
   - Keeps 5 essential actions permanently in the one-handed thumb reach zone without vertical scrolling: `🌅 Check In`, `🍽️ Lunch All`, `😴 Rest All`, `🧷 Potty Line`, and `🎙️ Voice Note`.
10. **Classroom Gifting Arrivals & Child Distribution**:
   - 7th action card in Teacher Workspace: `🎁 Classroom Gifts (Arrivals & Distribution)`.
   - Teachers view pending and arrived gifts specifically assigned to their classroom.
   - Teachers record physical arrival (`ReceivedAtDestination` / `ClassroomDelivered`) and confirm distribution to celebrating children or classroom use (`Distributed`) with distribution notes.
   - Role-scoped privacy: Financial prices and parent payment methods are masked to preserve teacher focus on classroom care and distribution dignity.




