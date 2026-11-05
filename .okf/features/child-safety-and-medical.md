---
okf_version: "0.2.0"
type: concept
title: "Child Safety, Medical Profiles & Dietary Alerts"
description: "Allergies, dietary requirements, emergency contacts, authorized pickups, and medication logs."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - safety
  - medical
  - allergies
  - dietary
  - emergency-contacts
---

# Child Safety, Medical Profiles & Dietary Alerts

## Overview
Critical child welfare domain safeguarding children with life-threatening allergies, medical action plans (e.g. EpiPen), dietary restrictions, and strictly authorized emergency pickups.

## Core Aggregates (`EnjoyEveryday.Domain.Entities.SafetyTrust`)
* `ChildMedicalProfile`: Holds blood group, pediatrician contact, immunization status, and emergency hospital preference.
* `ChildAllergy`: Individual allergy records with severity (`Mild`, `Moderate`, `Severe/Anaphylactic`), symptoms, and emergency intervention steps.
* `ChildDietaryRequirement`: Vegetarian, Vegan, Halal, Kosher, Nut-free, Dairy-free, Gluten-free, etc. Displayed prominently in meal routines.
* `ChildEmergencyContact`: Prioritized list of guardians with phone numbers, relationship, and legal pickup authorization status.

## UI Presentation & Emergency Badges
* In `TeacherWorkspace.razor` and `BranchDetailSection.razor`, children with severe allergies display a bright red medical badge with instant hover tooltip listing emergency protocols.
* During meal logging in `ChildDailyCareSection.razor`, dietary restrictions appear in amber pills to prevent accidental allergen exposure.
* In `SecurePickupModal.razor`, collectors must match an authorized `ChildEmergencyContact` or require supervisor sign-off.

