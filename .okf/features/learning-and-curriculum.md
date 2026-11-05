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
| **Today's Activities** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/TodayActivities.razor` (`/today-activities`) | Vertical chronological daily rhythm of assigned activities; accessible from Home "More Classroom Tools" → Start Activity |
| **Studio** | `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStudio.razor` | Curating and authoring activity templates |
| **Active Session Runner** | `src/EnjoyEveryday.UI.Shared/Components/ActiveSessionModal/ActiveSessionModal.razor` | Live activity guidance in the classroom (step timer, materials, child observations) |
| **Memories & Moments** | `src/EnjoyEveryday.UI.Shared/Components/ChildJourney/AddJourneyMomentDialog.razor` | Tagging learning moments and milestones to children |

## Key Services (`EnjoyEveryday.Application.Services`)
* **`PlannerService`**: Generates and stores weekly draft planning items, binds activities to classrooms and age tiers.
* **`ExecutionService`**: Manages live `ExperienceSession` states (`Scheduled`, `InProgress`, `Completed`) and participant attendance (`ExperienceParticipation`).
* **`IChildJourneyService`**: Logs developmental milestone progress linked to EYFS / Early Learning Standards.
* **`StandaloneHtmlGenerator` & `StorybookStorageService`**: Generates and caches visual pedagogical storybooks at `/api/experiences/{id}/storybook` dynamically incorporating execution phases, inquiry wonder prompts, and spoken scripts without static craft boilerplate. Supports 6-panel Pixar-style illustrated picture books via Gemini master prompts (`/api/experiences/{id}/storybook/prompt`) and image uploads (`/api/experiences/{id}/storybook/image`).
* **`IStorybookGenerationService`**: Encapsulates `BuildGeminiStorybookPrompt` formatting 6-panel 2x3 grid prompts for Google Gemini web generation.
* **`AiIdeaService` (`http://127.0.0.1:8000`)**: Orchestrates Gemini (`gemini-3.1-flash-lite`) for idea generation (`/api/ideas`) and pedagogical improvements (`/api/improve`), with 45s resilient failover.

