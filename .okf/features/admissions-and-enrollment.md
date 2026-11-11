---
okf_version: "0.2.0"
type: concept
title: "Admissions & Enrollment Pipeline"
description: "Inquiry management, parent tours, waitlists, application reviews, and child enrollment onboarding."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - admissions
  - enrollment
  - waitlist
  - applications
  - onboarding
---

# Admissions & Enrollment Pipeline

## Architecture Overview
The admissions pipeline tracks prospective families from initial inquiry through scheduled visits, offers, registration, and classroom placement.

## Primary Routes & Components
* **Pipeline Kanban / Board**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/EnrollmentPipeline.razor`
  - URL: `/management/enrollment`
  - Stages: `Inquiry` -> `TourScheduled` -> `TourCompleted` -> `Waitlist` -> `Offered` -> `Enrolled` -> `Withdrawn`
* **Child Wizard / Enrollment Form**: `src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildWizard.razor`
  - URL: `/children/new`
  - Collects child demographics, primary classroom selection, parent/guardian links, and initial medical disclosures.

## Domain Entities (`EnjoyEveryday.Domain.Entities.Children`)
* `ChildEnrollmentApplication`: Tracks application date, requested start date, desired schedule (Full-time, Mon/Wed/Fri, etc.), and status.
* `Child`: Active child profile created once application status transitions to `Enrolled`.
* `FamilyRelationship`: Maps enrolled child to parent users.

## Role & Permission Gates
* **Allowed Roles**: `OrganizationAdmin`, `BranchAdmin`.
* **Lead Teacher / Teacher**: Read-only or restricted to viewing enrolled children assigned to their classroom.

## Classroom Readiness & Student Onboarding Validation

Before a student can be onboarded, enrolled, or assigned to a branch/classroom, the application enforces atomic readiness validation via `IClassroomReadinessService` (`ClassroomReadinessService`).

### Mandatory Pre-Conditions
1. **Branch Setup**: Branch exists and is active (`is_active = true`).
2. **Classroom Setup**: At least one active non-pool classroom exists in the branch.
3. **Branch Lead Teacher**: An active Lead Teacher exists in the branch (via role `LeadTeacher` assigned to branch, or primary educator in `classroom_teachers`).
4. **Classroom Active**: The selected classroom is active (`is_active = true` and `status != 'Inactive'`).
5. **Assigned Educator**: At least one active teacher is assigned to the classroom in `classroom_teachers`.
6. **Available Capacity**: Classroom enrolled count (`currentEnrolled`) is strictly less than classroom capacity (`currentEnrolled < capacity`).

### Application Enforcement Points
* **UI**:
  - `ChildEnrollmentWizard.razor` (Step 7: Branch & Classroom selection): Displays readiness warnings, action buttons (`Create Classroom`, `Create Lead Teacher`, `Assign Teacher`, `Manage Classrooms`), and status badges (`Ready`, `No Teacher Assigned`, `Full`, `Inactive`). Blocks advancing to Step 8 if unready.
  - `FinalizeEnrollmentModal.razor` (Admissions Pipeline): Re-checks target classroom readiness, displays status banner, and disables the "Confirm Enrollment & Bind Tuition Plan" button if unready.
* **Server-Side & APIs**:
  - `ChildService.CreateChildAsync` & `UpdateChildAsync`: Revalidates readiness atomically before persisting child classroom placement.
  - `EnrollmentPipelineService.FinalizeEnrollmentAndBillingAsync`: Revalidates readiness before calling `EnsureChildEnrolledAsync`.
  - `ClassroomsController.cs`: Exposes `GET /api/branches/{branchId}/classrooms/readiness` and `GET /api/branches/{branchId}/classrooms/{id}/readiness`.


