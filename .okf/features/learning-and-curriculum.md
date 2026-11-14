---
okf_version: "0.2.0"
type: concept
title: "Learning, Curriculum & Experience Runner"
description: "Workflows for monthly/weekly planning, experience studio, activity runner modal, and child milestones."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - curriculum
  - planning
  - experiences
  - activity-runner
---

# Learning, Curriculum & Experience Runner

## Architectural Overview
EnjoyEveryday provides an activity runner and pedagogical curriculum framework rooted in early childhood learning domains (Cognitive, Social-Emotional, Physical, Language, Creative Arts).

## Core UI Components

| View | Location | Purpose |
|:---|:---|:---|
| **Planner** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Planning/` | Weekly and monthly classroom calendar schedules |
| **Experience Library & Storybooks** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Experiences.razor` (`/experiences`) | Paginated 3-row (`PageSize = 9` in a 3-column grid) experience catalog with Previous/Next pagination controls, filter/search reset to page 1, and Scandinavian theme-aligned `ExperienceCard` components (`src/EnjoyEveryday.UI.Shared/Components/ExperienceCard/`) |
| **Harvest & Insights** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Harvest.razor` (`/harvest`), `src/EnjoyEveryday.UI.Shared/Views/Experiences/HarvestView.razor` | Program reflection & performance analytics (`ExperiencePerformanceGrid` with 5-record pagination, search, and column sorting). Enforces `ExecutionService.IsDaycareClosedForActivity` so rating experiences, logging child moments, and creating/editing feedback are prohibited when the classroom/daycare is closed |
| **Today's Activities** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/TodayActivities.razor` (`/today-activities`) | Vertical chronological daily rhythm of assigned activities; accessible from Home "More Classroom Tools" → Start Activity |
| **Studio** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStudio.razor` | Curating and authoring activity templates with integrated AI Engine Status diagnostics console and 1-click **Schedule to Classrooms by Age Tier** deployment modal |
| **Experience Storybook & Playbook** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStorybook.razor` (`/experiences/{id}/story`) | Dual-mode activity runner providing Child Story Circle mode with warm narrative (`ChildStory`) and Teacher Playbook mode with pedagogical guidance, styled with Scandinavian theme tokens and scoped component styling (`PlaybookStepStepper`, `PlaybookMaterialsKitCard`, `PlaybookObservationLensCard`) |
| **Active Session Runner** | `src/EnjoyEveryday.UI.Shared/Components/ActiveSessionModal/ActiveSessionModal.razor` | Live activity guidance in the classroom (step timer, materials, child observations, care readiness indicators) |
| **Memories & Moments Stream** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Memories.razor` (`/memories`) | Paginated chronological memory stream (`PageSize = 10`, with Previous/Next server-side pagination via `IMemoryService.GetChildMemoriesAsync` / `fn_get_child_memories`) |
| **Parent Memories & Photos Gallery** | `src/EnjoyEveryday.Parent.Web/Components/Pages/Children/Memories.razor` (`http://localhost:7005/memories`), `src/EnjoyEveryday.Parent.Web/Components/Shared/MediaGallery.razor` | Parent portal photo & observation gallery with top calendar date picker (`Filter by Date` + `All Dates` reset), category filter tabs (`All`, `Celebrations`, `Journey & Discoveries`), and 10-item pagination (`PageSize = 10` with `← Previous` / `Next →` controls) |
| **Classroom Profile Experience History** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/ClassroomProfile.razor` (`/classrooms/view/{id}`), `src/EnjoyEveryday.UI.Shared/Components/Organization/ClassroomProfileCard.razor` | Read-only classroom configuration and paginated Experience History card (`_pageSize = 5` with `← Previous` / `Next →` navigation controls, page indicator, and status badge styling for Completed/Active/Draft) |
| **Add Moment Dialog** | `src/EnjoyEveryday.UI.Shared/Components/ChildJourney/AddJourneyMomentDialog.razor` | Tagging learning moments and milestones to children with **Hands-Free Voice-to-Moment Observation Capture** (Web Speech Recognition with automatic text fallback), child quote capture, and multi-child tagging |

## Key Services (`EnjoyEveryday.Application.Services`)
* **`PlannerService`**: Generates and stores weekly draft planning items, binds activities to classrooms and age tiers. Supports scheduling multiple experiences throughout operating hours (8:00 AM – 7:00 PM) across categories (`Morning`, `Mid-Day`, `Afternoon`, `Evening`) with exact time-window collision detection (`[startA, endA)` vs `[startB, endB)`). Enforces `IsDaycareClosedForPlanning` so experiences cannot be scheduled, updated, or completed on closed daycare days (weekends, past dates, or after closing `>= 7:00 PM`), with support for test override via `Bypass_daycare_closed`. In `PlanExperienceDialog`, real-time slot conflict warning displays existing classroom sessions and validates time overlaps dynamically.
* **`ExecutionService`**: Manages live `ExperienceSession` states (`Scheduled`, `InProgress`, `Completed`) and participant attendance (`ExperienceParticipation`). Enforces `IsDaycareClosedForActivity` in `StartExperienceAsync`, `CompleteExperienceAsync`, and `CompleteExperienceWithParticipationsAsync` so starting or ending any activity is prohibited whenever the daycare is closed (weekends, outside 7:00 AM – 7:00 PM operating hours, non-current dates, or after End-of-Day closure).
* **`IMemoryService` / `MemoryService`**: Queries `public.fn_get_child_memories` via `MemoryRepository` with server-side pagination (`Limit = 11`, `Offset = (_currentPage - 1) * 10`) to display 10 records per page with Previous/Next navigation and avoid loading large base64 evidence payloads all at once.
* **`IChildJourneyService`**: Logs developmental milestone progress linked to EYFS / Early Learning Standards.
  - **Session-to-Journey Integration**: `ActiveSessionModal.razor` includes a guided Step 2 review allowing educators to select participating children, pre-fill notes from session reflections, and publish moments with `PendingReview` moderation.
  - **Moderation Workflow**: `sp_approve_journey_entry` enables lead teachers and directors to review and approve child moments (`parent_visibility_status = 'Approved'`, `is_parent_visible = true`) before making them visible in the Parent Portal.
  - **Deduplication Guard**: `fn_has_journey_entry_for_participation` ensures an activity session participation cannot be duplicated in a child's developmental timeline.
* **`StandaloneHtmlGenerator` & `StorybookStorageService`**: Generates and caches visual pedagogical storybooks at `/api/experiences/{id}/storybook` dynamically incorporating execution phases, inquiry wonder prompts, and spoken scripts without static craft boilerplate. Supports 6-panel Pixar-style illustrated picture books via Gemini master prompts (`/api/experiences/{id}/storybook/prompt`), image uploads (`/api/experiences/{id}/storybook/image`), in-page responsive Lightbox modal viewer with safe new-tab opening and image download, and professional multi-step `@media print` formatting that eliminates blank pages and renders the complete manual with all instructional chapters.
* **`IStorybookGenerationService`**: Encapsulates `BuildGeminiStorybookPrompt` formatting 6-panel 2x3 grid prompts for Google Gemini web generation.
* **`AiIdeaService` (`http://127.0.0.1:8000`)**: Orchestrates Gemini (`gemini-3.1-flash-lite`) for idea generation (`/api/ideas`) and pedagogical improvements (`/api/improve`), with 45s resilient failover, structured metadata (`AiExecutionMeta`), and live streaming diagnostic logging (`AiIdeaDiagnostics`) for studio enhancements.
* **`ExperienceMarketplaceService` (`/marketplace`)**: Manages platform curated curriculum, entitlements, and import to private organization libraries. Enforces strict organization-wide duplicate prevention via `uq_experiences_org_platform_exp` and pre-import checks so an experience can be imported only once per organization across all branches and users. Marketplace cards dynamically display `✓ In Library` badges and `View in Library` navigation when already imported.


