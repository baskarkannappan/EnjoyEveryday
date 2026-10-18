# EnjoyEveryday Parent Portal — Next-Level Experience Assessment
**Version**: 2.0  
**Date**: October 2026  
**Status**: Phase 0 — Assessment & Architecture Baseline  

---

## 1. Executive Summary

The initial modernization of the EnjoyEveryday Parent Portal established a mobile-first, responsive Blazor architecture with zero build errors, reusable UI components, and clean design tokens complying with the warm, Scandinavian daycare aesthetic in `GEMINI.md`.

This assessment lays the architectural foundation for transforming the Parent Portal from a simple observation portal into a **trusted daily companion** for parents. It addresses the 10 high-impact capability areas requested by leadership while prioritizing security dependencies identified in the previous repository audit.

---

## 2. Existing Feature Inventory & Capabilities

An inspection of the codebase reveals extensive existing business services, repositories, and domain models that can be directly leveraged without creating duplicate tables or services:

| Domain Area | Existing Services & Repositories | Current Capabilities | Reusability in Parent Portal |
|:---|:---|:---|:---|
| **Child Daily Care** | `ChildDailyCareService`, `IChildDailyCareRepository` | Diaper counts, nap sessions, meals (lunch status), hygiene, comfort observations, supply alerts (`ChildCareSupply`), and daily reassurance text. | **High**: Directly consumed via `GetParentDailyCareReportAsync(childId, date)`. Currently needs refinement so "Not yet updated" is displayed instead of placeholder text. |
| **Attendance & Absences** | `AttendanceService`, `IAttendanceRepository` | Child check-in/out, arrival times, branch attendance, present child queries. | **High**: Can record absence / late arrival notices for daycare staff review. |
| **Secure Pickups & Custody** | `SecurePickupService`, `ISecurePickupRepository` | Authorized pickup list, custody restrictions, digital code/photo ID verification, emergency authorizations, departure logging. | **High**: Exposes authorized guardians and allows parents to submit pickup changes for staff approval. |
| **Child Development Journey** | `ChildJourneyService`, `HarvestService`, `ChildJourneyInsightService` | Dated observations, milestones, developmental areas, evidence linkage to memories, deterministic fallback for AI insights. | **High**: Rich source records already exist. Can present strengths in communication, social collaboration, and understanding without ranking. |
| **Parent-Teacher Messaging** | `ParentService`, `IParentCommunicationRepository` | `parent_communications` table (Migration 041) with indexed `(tenant_id, child_id)`, status tracking (`Sent`, `Delivered`, `Read`), and audit logging. | **High**: Fully backed by PostgreSQL. Needs quick replies, quiet hours, and status distinction. |
| **Billing & Payments** | `BillingService`, `IBillingService` | Invoices, receipts, Razorpay orders, payment ledger, offline/cash tracking. | **High**: Can surface pending tuition invoices and payment history directly to authorized parents. |
| **Consent & Permissions** | `ConsentService`, `IConsentService` | Child photo consent, excursion consent, emergency medical authorization, revision audit trail. | **High**: Surface consent status cards and provide instant digital signature/approval workflows. |
| **Family Relationships** | `ParentRepository`, `IParentRepository` | `persons` table with `preferred_language`, parent-child links, primary guardian flags. | **High**: Foundation for family sharing, multiple caregiver accounts, and localized interface. |

---

## 3. Codebase Audit Review & Security Dependencies

Before exposing additional child and family data through the Parent Portal, the following critical security findings must be enforced at every tier:

### A. Strict Multi-Tenant & Family Isolation
1. **Never Trust Client-Supplied Identifiers**: A parent providing a `childId`, `conversationId`, or `invoiceId` in a URL or form submission must NEVER be assumed to own that entity. Every service call must pass through `ValidateAccessOrThrow(childId)` verifying that `tenant_id` matches `_tenantContext.TenantId` AND `_parentRepository.HasChildAccessAsync(tenantId, userId, childId)` returns `true`.
2. **Prevent Cross-Family Leakage**: Family relationships must be explicitly linked in `person_family_members` or `child_parents`. Never infer that two adults are authorized to view a child because they share an email domain, surname, or physical address.
3. **Guardianship Revocation**: If a guardian's access is revoked by staff or the primary parent, subsequent circuit interactions or API calls must immediately reject access.

### B. Self-Service Action Approval Gate
- **No Direct Mutation of Legal Records**: When a parent reports an absence, late arrival, or authorizes a new pickup person, this request must NOT automatically mutate legal attendance or custody tables. Requests must be stored as pending requests or notes with audit logging, requiring daycare staff acknowledgment.

### C. Media Storage & Privacy
- **No Predictable Public URLs**: Child photos and videos must never be stored in publicly guessable cloud buckets or rendered with permanent public links.
- **Strict Anti-Caching on Shared Devices**: Private child media must not be aggressively cached in browser service workers or shared HTTP proxies. Logout or child-switch must clear active image views.

### D. Rules-First Child Development Insights
- In accordance with `GEMINI.md` Rule 7, child development summaries must be deterministic, transparent, and derived from actual teacher observations. The portal must NEVER present an AI conjecture as a medical or developmental diagnosis.

---

## 4. Feature Gap Analysis & Next-Level Enhancements

### 1. Meaningful Daily Summary (P1 — Highest Priority)
- **Current State**: `ChildDailySummary` merges attendance and care data, but falls back to generic positive placeholders if no events are recorded.
- **Required Next-Level Behavior**:
  - Show real recorded data only.
  - If care updates have not been entered yet, explicitly display *"Not yet updated today"* rather than assuming lunch, nap, or diaper routines occurred.
  - Distinguish between *"Not recorded yet"* and *"Did not occur"*.
  - Surface active action items (pending consent, unpaid invoice, unread teacher note) at the top of the home screen.

### 2. Fast Private Memory Gallery (P1)
- **Current State**: `/memories` page supports category filtering and lightbox viewer.
- **Required Next-Level Behavior**:
  - Date and event groupings.
  - Download/save permissions governed by parental photo consent status.
  - Clear indicator when a media asset is unavailable or undergoing upload processing.

### 3. Teacher-Parent Communication (P1)
- **Current State**: Direct database-backed chat in `parent_communications`.
- **Required Next-Level Behavior**:
  - Unread message count badges.
  - Quick-reply prompts (e.g., *"Thank you for the update!"*, *"On my way for pickup"*, *"Running 10 mins late"*).
  - Quiet hours indicator reminding parents that teachers are engaged with children during classroom hours.
  - Transparent message delivery states (`Sent` vs `Delivered` vs `Read`).

### 4. Child Development Journey for Parents (P2)
- **Current State**: Displays basic journey entries and milestones.
- **Required Next-Level Behavior**:
  - Map teacher observations to positive developmental growth areas: Communication, Social & Emotional, Collaboration, and Cognitive Understanding.
  - Show real documented evidence moments (photos + notes).
  - Eliminate comparative grading or ranking between children.

### 5. Everyday Actions Without Phone Calls (P2)
- **Current State**: Only event RSVPs and contact phone updates existed.
- **Required Next-Level Behavior**:
  - **Absence & Late Arrival Reporting**: Single-tap submission allowing parents to state reason and expected return date.
  - **Authorized Pickup Management**: View current authorized pickups from `ISecurePickupService` and submit guest pickup authorizations.
  - **Invoices & Billing**: View tuition invoices, outstanding balance, and receipts via `IBillingService`.
  - **Consent Management**: Digital agreement review and signature via `IConsentService`.

### 6. Multilingual Parent Experience (P3)
- **Target Context**: Daycare centers in India (with special focus on South India).
- **Required Next-Level Behavior**:
  - Language selector supporting:
    - **English** (en-IN)
    - **Tamil** (ta-IN)
    - **Telugu** (te-IN)
    - **Hindi** (hi-IN)
    - **Kannada** (kn-IN)
  - Configurable UI dictionary for labels, navigation, care categories, and standard announcements.
  - Preserves original teacher messages in their native language with clear indicator for localized interface text.

### 7. Visible Privacy & Trust Center (P3)
- Dedicated parent view summarizing active photo consent, authorized pickups, data sharing policies, and direct contact options for daycare data protection officers.

---

## 5. Performance Baseline & Target Budget

| Area | Baseline (Phase 0) | Next-Level Target | Strategy |
|:---|:---|:---|:---|
| **Today Page Data Fetch** | 3 separate DB queries | Single combined query batch | Pre-join child, attendance, and care report in `ParentService`. |
| **Circuit Memory Footprint** | ~35 KB per parent circuit | $\le 45$ KB per parent circuit | Lean view models, discard non-visible media blobs. |
| **Slow Network Payload** | ~42 KB HTML/JSON | $\le 45$ KB (initial text-first) | Text, care statuses, and action items stream first; images lazy-loaded on demand. |
| **Offline Resilience** | Browser disconnected error | Ambient `OfflineState` badge | Retain read-only view of today's summary in memory circuit. |

---

## 6. Phased Implementation Plan

- **Phase 0 — Assessment & Security Dependencies** *(Completed)*
  - Completed repository inventory, security review, and architecture document.
- **Phase 1 — Shared Components & Mobile Usability Foundation** *(Completed)*
  - Created `LanguageSwitcher.razor`, `ReportAbsenceModal.razor`, `InvoicesListModal.razor`, and `AuthorizedPickupsModal.razor`.
- **Phase 2 — Parent Home Screen & Meaningful Daily Summary** *(Completed)*
  - Refined `ChildDailyCareService.GetParentDailyCareReportAsync` to distinguish "Not yet recorded" from completed routines without fabricating data. Added Action Required alerts.
- **Phase 3 — Photo & Memory Performance** *(Completed)*
  - Accessible `MediaViewer` lightbox modal, category filtering (All, Celebrations, Journey), consent-aware views.
- **Phase 4 — Teacher-Parent Messaging Reliability** *(Completed)*
  - Quick-reply chips ("Thank you!", "On my way", "Running late"), quiet hours notice (8:30 AM – 3:30 PM), verified delivery states.
- **Phase 5 — Everyday Parent Self-Service & Invoices** *(Completed)*
  - Absence reporting modal, authorized pickup security profile, tuition invoice statements (`IBillingService`), and child consent documents (`IConsentService`).
- **Phase 6 — Child Journey Presentation & Multilingual Localization** *(Completed)*
  - Non-ranking Growth & Curiosity Highlights (Communication, Social Collaboration, Emotional Expression, Cognitive Discovery), `ParentLocalizationService` supporting English, Tamil, Telugu, Hindi, and Kannada.
- **Phase 7 — Privacy & Trust Center, Final Verification** *(Completed)*
  - Visible privacy and trust portal in `FamilyProfile.razor`, full solution build verification with 0 errors across all projects.

