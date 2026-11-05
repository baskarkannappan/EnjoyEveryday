---
okf_version: "0.2.0"
type: concept
title: "Database Schema Catalog (133 Tables)"
description: "Categorized dictionary of all 133 PostgreSQL database tables grouped by functional domain."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - database
  - schema
  - tables
  - dictionary
---

# Database Schema Catalog (133 Tables)

All tables belong to the `public` schema and enforce multi-tenancy via `tenant_id UUID NOT NULL` indexed by `idx_<table>_tenant`.

## 1. Tenancy, Identity & Access Control (13 Tables)
* `tenants`: Root SaaS tenant partitioning records.
* `users`, `user_roles`, `roles`, `permissions`, `role_permissions`: Core RBAC data model.
* `user_organization_roles`: Scopes users and roles within specific organizations.
* `user_branch_assignments`: Binds staff and Branch Admins to physical branch facilities.
* `user_external_logins`: OAuth2 / Google SSO provider federations.
* `audit_log`: High-security immutability audit trail of administrative actions.
* `support_access_grants`: Temporary impersonation / support access grants.

## 2. Organization, Facilities & Staff (27 Tables)
* `organizations`: Root daycare provider accounts.
* `organization_memberships`: Staff and admin associations.
* `branches`, `branch_floor_maps`, `floor_map_markers`, `branch_operational_goals`: Physical center facilities and floor plans.
* `classrooms`, `classroom_statutory_ratios`, `classroom_teachers`: Room capacities, statutory licensing ratios, and teacher assignments.
* `teachers`, `teacher_attendance`: Primary teacher profile and clock-in/out records.
* `teacher_availabilities`, `teacher_certifications`, `teacher_documents`, `teacher_education`, `teacher_emergency_contacts`, `teacher_employments`, `teacher_experiences`, `teacher_handovers`, `teacher_languages`, `teacher_observations`, `teacher_preferences`, `teacher_professional_developments`, `teacher_schedules`, `teacher_skills`, `teacher_specialties`, `teacher_trainings`: Comprehensive staff records.
* `staff_leaves`: Sick, vacation, and coverage logs.

## 3. Children, Families & Admissions (28 Tables)
* `children`: Primary child records (names, DOB, assigned classroom, enrollment status).
* `child_enrollment_histories`, `enrollments`, `enrollment_pipeline_applications`: Inquiries, waitlists, tours, and offers.
* `family_relationships`, `child_guardians`, `child_relationships`: Parent-child mapping.
* `authorized_pickups`, `emergency_contacts`: Verified collectors for departure security.
* `child_legal_profiles`, `child_person_restrictions`: Custody orders and protective injunctions.
* `child_documents`, `child_transportations`: Registration papers and busing routes.

## 4. Attendance & Care Routines (11 Tables)
* `child_attendance`: Daily arrivals, departures, verification methods, notes, and departure signers.
* `child_care_events`: Diaper, potty, meal, and bottle events.
* `child_nap_sessions`: Sleep start, end, quality, and check intervals.
* `child_care_plans`, `child_care_supplies`: Individual care instructions and cubby items.
* `child_feeding_profiles`, `child_sleep_profiles`, `child_toileting_profiles`, `child_routines`, `child_schedules`: Baseline child routines.

## 5. Health, Safety & Consents (14 Tables)
* `child_medical_profiles`: Blood group, pediatrician, immunization.
* `child_allergies`, `child_food_restrictions`, `child_dietary_profiles`: Allergy severity and dietary flags.
* `child_medications`, `medication_administrations`: Prescription logs and administration sign-offs.
* `child_consents`, `child_consent_documents`, `consent_audit_events`, `consent_choices`, `consent_signers`, `consent_templates`: Legal and media release forms.

## 6. Learning, Planning & Curriculum (25 Tables)
* `experiences`, `experience_versions`, `platform_experiences`, `platform_experience_versions`: Activity templates.
* `experience_schedules`, `experience_sessions`, `experience_session_teachers`, `experience_participations`: Classroom execution.
* `weekly_planning_draft_items`, `weekly_planning_draft_requests`: Curriculum planners.
* `child_milestones`, `development_areas`, `skills`: Early childhood learning standards.
* `child_journey_entries`, `child_journey_developments`, `child_journey_evidence`, `child_stories`: Parent timeline moments.
* `activity_recommendation_requests`, `activity_recommendation_items`, `activity_recommendation_decisions`: ADK activity suggestions.

## 7. Commercial, Billing & Marketplace (15 Tables)
* `invoices`, `invoice_line_items`, `payments`, `family_billing_contracts`: Tuition billing.
* `organization_billing_settings`, `organization_entitlements`: Tenant plan tier and add-ons.
* `material_products`, `material_offerings`, `material_orders`, `material_order_items`, `marketplace_orders`: Curriculum materials.
* `inventory_locations`, `inventory_balances`, `inventory_ledger`, `material_shipments`: Warehouse inventory.

