# EnjoyEveryday — AI-Powered Child Progress Insights Implementation Record

## 1. Executive Summary & Objective

This document records the architectural audit, evidence source mapping, server-side authorization enforcement, AI contract validation, deterministic fallback synthesis, and UI integration for the **AI-Powered Child Progress Insights** capability in EnjoyEveryday.

The purpose of this capability is to help authorized educators, teachers, and daycare administrators understand a child's recorded pedagogical journey over a selectable reporting period. It synthesizes observations, guided experience participations, milestones, and teacher reflections without diagnosing, developmental grading, or manufacturing ungrounded claims.

---

## 2. Evidence Source Mapping & Integrity Boundaries

| Evidence Source | Meaning & Authority | Permitted Use in Child Progress Insights |
| :--- | :--- | :--- |
| `child_stories` | Teacher-recorded anecdotal narrative observations | Summarize recorded interests, strengths, and behaviors |
| `child_journey_entries` + `child_journey_evidence` | Structured milestone/moment observations with media | Citations of developmental focus and pedagogical moments |
| `experience_participations` | Logged participation in scheduled classroom experiences | Establishing engagement in guided learning activities |
| `child_milestones` | Authoritative developmental milestones recorded by staff | Reflecting recorded accomplishments |
| `child_care_events` | Routine care (meals, sleep, diaper, comfort checks) | **Administrative only**: Explicitly excluded from developmental inferences |
| `attendance_records` | Attendance and check-in / check-out timestamps | **Administrative only**: Explicitly excluded from learning inferences |
| `experience_schedules` (Planned) | Future calendar events | **Not completed**: Treated as upcoming opportunities, not historical achievements |
| Candidate Experiences | Organization Library & Marketplace | Grounded suggestions for next educational opportunities (`IdeasToConsider`) |

---

## 3. Architecture & Service Boundaries

### Reused Existing Architecture
- **SQL Memory Layer**: [MemoryService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Learning/MemoryService.cs) retrieves unified chronological records scoped strictly by tenant, child, and authorized visibility.
- **Experience Library & Marketplace**: [ExperienceService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Learning/ExperienceService.cs) provides valid candidate activities for future suggestions.
- **Planner Service**: [PlannerService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Learning/PlannerService.cs) allows one-click planning handoff when an educator accepts an idea to consider.
- **Persistence Layer**: [ChildJourneyInsightRepository.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Infrastructure/Repositories/Ai/ChildJourneyInsightRepository.cs) persists insight requests and items into `child_journey_insight_requests` and `child_journey_insight_items` separate from authoritative child records.
- **Shared UI**: [ChildJourneyInsightPanel.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/Journey/ChildJourneyInsightPanel.razor) embedded directly into the Child Journey page ([ChildJourney.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildJourney.razor)).

---

## 4. Input & Output Contracts

### Input Contract (`GenerateJourneyInsightInput`)
- `ChildId` (Guid, validated server-side against tenant/classroom permissions)
- `PeriodType` ("Last7Days", "Last30Days", "CurrentMonth", "Custom")
- `CustomStartDate`, `CustomEndDate` (DateOnly?, bounded to prevent unbounded scans)
- `FocusArea` (string?, e.g., "Communication", "SocialInteraction", "Creativity", "Movement", "EarlyMath", "EmotionalExpression", "Environment")
- `TeacherQuestion` (string?, optional educator inquiry)

### Structured Output Contract (`ChildJourneyInsightRequest` & `ChildJourneyInsightItem`)
- **Overall Summary**: High-level factual narrative of moments captured.
- **Categorized Items**:
  - `JourneySummary`: Recorded count of observations and experiences during the period.
  - `RecordedInterests`: Explicitly logged topics, play preferences, and engagement areas.
  - `ParticipationPatterns`: Frequency and variety of completed classroom activities.
  - `LearningOpportunities`: Grounded synthesis of domain progress (and inquiry response if teacher question supplied).
  - `IdeasToConsider`: Suggested next experiences with valid `SuggestedExperienceId` for planning handoff.
  - `EvidenceLimitations`: Scope boundary disclaimer affirming that administrative care/attendance are excluded and absent records are not deficits.
- **Classifications**:
  - `RecordedFact`: Direct statements from primary records.
  - `ObservedPattern`: Multiple-record pattern.
  - `EducationalSuggestion`: Next step activity consideration.
  - `EvidenceLimitations`: Boundary note.
- **Evidence Strength**:
  - `"Pattern supported by multiple records"`
  - `"Single observation"`
  - `"Limited evidence"`
  - `"Insufficient evidence"`
- **Supporting Evidence Citations**:
  - JSON array of `{ SourceId, SourceType, Title, Summary, EventDate }` linking claims to primary database records.

---

## 5. Security, Authorization & Privacy Controls

1. **Child & Organization Isolation**: `_childService.GetChildByIdAsync` confirms the child belongs to the authenticated user's active tenant and organization.
2. **Classroom Scope**: Rejects requests for children not assigned to an active classroom.
3. **No Cross-Tenant Leaks**: All memory queries and insight persistence require `tenant_id` matching `_tenantContext.TenantId`.
4. **Data Minimization**: AI payload contains only first name, reporting period, focus area, teacher question, and minimal memory descriptors. No family contact details or sensitive personal identifiers are shared.
5. **Human-in-the-Loop Review**: Insight cards include an educator feedback row (Helpful / Not Helpful / Needs Review). Insights do not automatically publish to parents or alter child records.

---

## 6. Deterministic Fallback Mode

If the external AI Agent (`EnjoyEveryday.AiAgent` FastAPI service) is unavailable or throws a connection refusal, [ChildJourneyInsightService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Ai/ChildJourneyInsightService.cs) immediately executes `GenerateDeterministicGroundedFallback`:
- Generates categorized statements derived directly from `MemoryEvent` counts and descriptions.
- Tailors `LearningOpportunities` to the selected `FocusArea`.
- Explicitly answers `TeacherQuestion` with matching citations, or flags `"Insufficient evidence"`.
- Selects an eligible candidate experience from the library for `IdeasToConsider`.
- Emits `EvidenceLimitations` explaining administrative boundaries.

---

## 7. Phase Acceptance Checklist

- [x] **Phase 1 — Repository inspection & evidence mapping**: Evidence taxonomy and existing insight architecture mapped.
- [x] **Phase 2 — Secure evidence retrieval**: [MemoryService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Learning/MemoryService.cs) and child authorization enforced.
- [x] **Phase 3 — AI insight generation & validation**: Grounded fallback, citation sanitization, and structured contracts implemented in [ChildJourneyInsightService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Ai/ChildJourneyInsightService.cs).
- [x] **Phase 4 — Child Journey UI integration**: [ChildJourneyInsightPanel.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/Journey/ChildJourneyInsightPanel.razor) provides period dropdown, focus area, inquiry input, limitation banners, and citation inspect modals.
- [x] **Phase 5 — Follow-up opportunities & workflow**: "+ Plan Activity" button links `IdeasToConsider` into the planner.
- [x] **Phase 6 — Security, compilation & verification**: [EnjoyEveryday.Application](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/EnjoyEveryday.Application.csproj), [EnjoyEveryday.UI.Shared](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/EnjoyEveryday.UI.Shared.csproj), and [EnjoyEveryday.Application.Tests](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application.Tests/EnjoyEveryday.Application.Tests.csproj) build cleanly with 0 errors.
