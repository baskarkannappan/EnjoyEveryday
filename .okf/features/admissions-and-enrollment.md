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

