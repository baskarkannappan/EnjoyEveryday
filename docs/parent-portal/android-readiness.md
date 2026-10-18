# EnjoyEveryday Parent Portal — Android Application Readiness Architecture

**Date:** October 2026  
**Audience:** Mobile Engineers, Backend Architects, Security Team

---

## 1. Overview & Architectural Goals

The goal of Android-Readiness is to structure the EnjoyEveryday Parent Portal backend, business domain, and service contracts so that a future native Android application (built with Kotlin / Jetpack Compose) can seamlessly reuse the exact same backend logic, database tables, business rules, and security enforcement without duplicating code or creating competing services.

---

## 2. Decoupling Rules Applied

1. **Zero Business Rules in Razor Pages:**
   - All validation (e.g. child relationship checks, RSVP status rules, phone number sanitization, photo consent verification) resides strictly in `EnjoyEveryday.Application.Services` (`IParentService`, `IConsentService`, `IParentCommunicationAssistantService`).
   - Razor components only render UI, format dates, and invoke application service methods.
2. **Explicit, Serialisable Request & Response DTOs:**
   - Methods return strongly-typed DTOs (e.g., `ParentAuthorizedChild`, `ChildDailySummary`, `ParentCommunication`, `Event`) rather than Blazor-specific models or raw database entities.
   - All DTO properties are standard .NET primitive types, DateOnly, TimeSpan, or standard JSON records.
3. **No Browser-Exclusive Dependencies in Business Operations:**
   - No `localStorage` or browser-specific JavaScript is used for business calculations, access decisions, or state verification.

---

## 3. Future Android API Endpoints Specification

The existing `IParentService` methods map 1-to-1 to a clean REST API surface for the Android application:

| HTTP Method | Route | Description | Backing Service Method |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/parent/children` | List authorized children for authenticated parent | `IParentService.GetAuthorizedChildrenAsync` |
| `GET` | `/api/parent/children/{childId}/today` | Daily summary (attendance, schedule, care report) | `IParentService.GetChildDailySummaryAsync` |
| `GET` | `/api/parent/children/{childId}/journey` | Approved journey observations and milestones | `IParentService.GetApprovedChildJourneyAsync` |
| `GET` | `/api/parent/children/{childId}/events` | Approved upcoming celebrations & photos | `IParentService.GetApprovedEventsAsync` |
| `POST` | `/api/parent/events/{eventId}/rsvp` | Submit celebration RSVP (`Attending`/`Declined`) | `IParentService.SubmitEventRsvpAsync` |
| `GET` | `/api/parent/children/{childId}/messages` | Message history with classroom educators | `IParentService.GetMessagesAsync` |
| `POST` | `/api/parent/children/{childId}/messages` | Send message to classroom educators | `IParentService.SendMessageAsync` |
| `GET` | `/api/parent/profile` | Guardian contact details and preferences | `IParentService.GetParentProfileAsync` |
| `PUT` | `/api/parent/profile/contact` | Update mobile phone and contact preferences | `IParentService.UpdateParentContactAsync` |
| `GET` | `/api/parent/children/{childId}/consent` | Consent documents status & signing requirements | `IConsentService.GetDocumentsAsync` |

---

## 4. Mobile Security & Authentication Design

1. **Authentication Compatibility:**
   - The Android application can authenticate using OAuth2 / OpenID Connect (Google Sign-In) exchanging tokens via standard PKCE or using session cookies via Android `CookieManager` / OkHttp `CookieJar`.
2. **Multi-Tenant Token Verification:**
   - The authenticated user's `tenant_id` is extracted from claims or headers, never trusting client-supplied tenant identifiers.
3. **Child Access Boundaries:**
   - The backend enforces `ValidateAccessOrThrow(childId)` on every request, verifying the relationship in the `family_relationships` table before returning data.
4. **Push Notifications (Future Android):**
   - Push notifications will be integrated with Firebase Cloud Messaging (FCM).
   - Sensitive child information (e.g. medical notes, specific incidents) must **never** be transmitted inside the notification payload. Only an unread badge or generic notification (e.g. *"New daily update from Little Stars Daycare"*) is sent, prompting the parent to open the secure app.

---

## 5. Offline & Caching Constraints

For child safety and privacy:
- Photos, daily care records, and custody details must **not** be stored indefinitely in unencrypted Android device storage.
- If offline caching is introduced, it must utilize Android Jetpack Security (`EncryptedSharedPreferences` / encrypted SQLite Room) and be cleared immediately upon sign-out or session expiration.
