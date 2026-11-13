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
3. **Branch Lead Teacher**: An active Lead Teacher exists in the branch:
   - User assigned role `LeadTeacher` / `Lead Teacher` in the branch's organization or tenant, AND
   - Assigned to the branch via `user_branch_assignments` (unrevoked) OR assigned to a classroom within the branch in `classroom_teachers` (multi-tenant safe resolution).
   - Fallback: Any active educator assigned to a branch classroom with `is_primary = true`.
4. **Classroom Active**: The selected classroom is active (`is_active = true` and `status != 'Inactive'`).
5. **Assigned Educator**: At least one active teacher is assigned to the classroom in `classroom_teachers`.
6. **Available Capacity**: Classroom enrolled count (`currentEnrolled`) is strictly less than classroom capacity (`currentEnrolled < capacity`).

### Application Enforcement Points
* **UI**:
  - `ChildrenDirectoryView.razor` replaces standalone child addition with `+ Start New Admission`, routing users to `/enrollment?action=new-inquiry`. `ChildFormModal.razor` is strictly dedicated to editing existing child profiles.
  - `ChildEnrollment.razor` automatically redirects any `/children/add` requests to `/enrollment?action=new-inquiry`.
  - `ChildEnrollmentWizard.razor`: Incorporates non-blocking soft failure handling (`IAppNotificationService` toast notifications and an inline warning banner). Step transitions and async database operations catch `UnauthorizedAccessException`, `ClassroomReadinessException`, and operational failures without breaking the Blazor circuit or hanging the user interface.
* **Backend**:
  - `ChildService.CreateChildAsync` and `ChildService.UpdateChildAsync` permit both `child.manage` and `child.enrollment.manage` permissions, ensuring users with admissions/enrollment authority can successfully update draft and enrolled child records.
  - `ChildService.CreateChildAsync` enforces `isOfficialAdmission: true`, disallowing direct child creation without the official pipeline.
  - `ChildrenController` rejects direct `POST /api/children` requests with HTTP 400 Bad Request, directing callers to `/enrollment` or `/api/admissions/inquiry`.
  - `EnrollmentPipelineService.FinalizeEnrollmentAndBillingAsync` checks `app.PipelineStage != "Enrolled"` to prevent duplicate submissions and double-invoicing.
* **Database**:
  - `sp_ensure_child_enrolled` handles idempotent upserting of child records upon official admission confirmation.
  - `ChildEnrollmentWizard.razor` (Step 7: Branch & Classroom selection): Displays readiness warnings, action buttons (`Create Classroom`, `Create Lead Teacher`, `Assign Teacher`, `Manage Classrooms`), and status badges (`Ready`, `No Teacher Assigned`, `Full`, `Inactive`). Blocks advancing to Step 8 if unready.
  - `FinalizeEnrollmentModal.razor` (Admissions Pipeline): Re-checks target classroom readiness, displays status banner, and disables the "Confirm Enrollment & Bind Tuition Plan" button if unready.
* **Server-Side & APIs**:
  - `ChildService.CreateChildAsync` & `UpdateChildAsync`: Revalidates readiness atomically before persisting child classroom placement.
  - `EnrollmentPipelineService.FinalizeEnrollmentAndBillingAsync`: Revalidates readiness before calling `EnsureChildEnrolledAsync`.
  - `ClassroomsController.cs`: Exposes `GET /api/branches/{branchId}/classrooms/readiness` and `GET /api/branches/{branchId}/classrooms/{id}/readiness`.

## Admissions to Guardian Linking & Guardian Management

### 1. Automated Parent Guardian Association
When an enrollment application is confirmed via `EnrollmentPipelineService.FinalizeEnrollmentAndBillingAsync`:
1. Extracts parent/guardian details (first name, last name, email, phone, relationship) captured in the enrollment application.
2. Invokes `_childRepository.SyncParentGuardianAsync` which executes PostgreSQL procedure `sp_sync_parent_guardian`.
3. Creates or links the corresponding `persons` record, `child_guardians` record, and `family_relationships` link with `status = 'Verified'` and pickup authorization.
4. Triggers `SendParentWelcomeAsync` via `IEmailNotificationService` with portal access instructions and contact information.

### 2. Daycare Staff Guardian Management UI
Staff and Administrators manage child guardians directly from `ChildrenDirectoryView.razor`:
* **Component**: `src/EnjoyEveryday.UI.Shared/Components/Children/ChildGuardiansModal.razor`.
* **Capabilities**:
  - Lists all connected guardians with primary status, relationship, email, phone, pickup authorization, and verified portal access badge.
  - Links new guardians to the child with pickup permissions and emergency contact flags.
  - Revokes portal access atomically via `sp_revoke_child_guardian(tenantId, childId, userId, revokedBy)` without destroying historical audit data.

## 3. End-to-End Onboarding Data Retention & Automatic Pre-population

To eliminate redundant data entry when onboarding and managing children:
1. **Admissions Inquiry Data Ingestion**:
   - `NewFamilyInquiryModal.razor` captures child identity, DOB, gender, parent/guardian contact, schedule preference, branch/classroom, tuition rates, and special requirements/internal notes (e.g., allergies, dietary restrictions).
2. **Atomic Onboarding Data Persistence**:
   - When enrollment is finalized via `EnrollmentPipelineService.FinalizeEnrollmentAndBillingAsync`, the system executes `sp_finalize_enrolled_child_profile`:
     - Compiles a complete `draft_data` JSON payload matching the `ChildEnrollmentWizard` contract.
     - Sets `enrollment_status = 'Enrolled'`, `is_active = TRUE`, and `profile_completion_percentage = 90`.
     - Automatically creates an `emergency_contacts` record for the primary guardian.
     - Parses `internal_notes` for allergy and dietary keywords (e.g. Dairy, Peanuts, Gluten, Halal, Vegetarian) and automatically creates `child_medical_profiles`, `child_allergies`, and `child_dietary_profiles` records so alerts display immediately in educator daily care feeds and classroom rosters.
3. **Child Profile & Wizard Auto-Fill**:
   - When viewing or editing the child in `ChildEnrollmentWizard.razor` (`/children/edit/{Id}`):
     - Automatically pre-populates any empty fields from linked guardians (`fn_get_child_guardians_for_admin`) and inquiry applications (`fn_get_enrollment_application_by_child_id`).
     - Allows free navigation (`maxAllowedStep = 10`) across all 10 wizard steps without requiring the user to click step-by-step.
     - Provides one-click action buttons (`📋 Use Parent / Guardian Info`) in Emergency Contacts (Step 4) and Authorized Pickups (Step 8) to instantly reuse guardian details without re-typing.



