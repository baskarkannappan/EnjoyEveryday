# EnjoyEveryday Parent Portal Modernization — Architectural Assessment & Baseline

**Date:** October 2026  
**Environment:** .NET 10.0, ASP.NET Core Blazor Interactive Server, Microsoft Fluent UI 5.0  
**Scope:** `src/EnjoyEveryday.Parent.Web`, `src/EnjoyEveryday.UI.Shared`, and backing application/domain services.

---

## 1. Executive Summary

The EnjoyEveryday Parent Portal provides guardians with direct visibility into their enrolled child's daycare journey, daily attendance, wellbeing routines, upcoming celebrations, and educator communications. 

This assessment evaluates the current state of the portal, identifies performance and mobile usability bottlenecks, establishes an architectural baseline, and sets an incremental roadmap to modernize the portal into a mobile-first, modular, lightweight, and Android-ready application without breaking changes or duplicate services.

---

## 2. Technology Stack & Hosting Model Inspection

| Architectural Layer | Current Implementation | Observations & Considerations |
| :--- | :--- | :--- |
| **Framework Target** | .NET 10.0 (`net10.0`) | Modern C# 13 / .NET 10 features available. |
| **Hosting Model** | **Blazor Interactive Server** (`InteractiveServerRenderMode`) | Hub connection over WebSockets with SignalR (`MaximumReceiveMessageSize = 10MB`). Active server circuits maintain page state. Must keep circuit state lean, minimize re-renders, and avoid storing large datasets or byte arrays in memory. |
| **Component Libraries** | Microsoft Fluent UI Blazor (`5.0.0-rc.5-26219.1`) & `EnjoyEveryday.UI.Shared` | Custom theme variables configured in `app.css` and `customstyles.css`. Mixed use of inline styles and Fluent components. |
| **Authentication** | Cookie Authentication + Google OAuth + Dev Mock Login | Handled in `Endpoints/AuthEndpoints.cs`. Strict relationship verification ensures user is a verified parent before accessing child data. |
| **State Management** | Scoped `ParentSessionState` | Manages `AuthorizedChildren` and `SelectedChild`. Emits `OnChildChanged` events. Isolated per Blazor circuit. |
| **Database & ORM** | PostgreSQL via Dapper & `IDbConnectionFactory` | Clean repository pattern (`IParentRepository`, `IParentCommunicationRepository`). Scoped tenant context enforced. |

---

## 3. Inventory of Parent Portal Routes & Pages

| Route | Page Component | Current Responsibilities | Identified Gaps / Modernization Needs |
| :--- | :--- | :--- | :--- |
| `/` or `/today` | `Components/Pages/Today.razor` | Child attendance badge, daily timeline (attendance, planned experiences, moments), 2x2 daily care KPI grid (diapers, rest, lunch, mood), cubby replenishment alerts, upcoming events. | Large monolith with over 300 lines and hardcoded inline styles. Lacks modular sub-components. Needs pull-to-refresh / streamlined mobile view. |
| `/journey` | `Components/Pages/Children/ChildJourney.razor` | Milestone achievements, teacher observations, child voice quotations, participation tags. | Lacks pagination; loads all journey entries in a single query; missing media lightbox / photo gallery viewer. |
| `/family` | `Components/Pages/Children/FamilyProfile.razor` | Guardian profile information (name, email, mobile phone, contact preference), linked child cards, safety notice. | Form styling needs consistency with design system tokens; lack of child emergency contact details and consent status overview. |
| `/messages` | `Components/Pages/Communication/Messages.razor` | Direct messaging with classroom teachers. | **Major Gap:** Current message history is an in-memory mock record (`ChatMessage`). Not persisted to database or linked to `parent_communications` table. |
| `/events` | `Components/Pages/Learning/Events.razor` | Classroom celebrations, RSVP submission (Attending/Declined), event photos. | Needs responsive photo cards, thumbnail compression, and empty/loading state standardization. |
| *(Missing)* `/memories` | *None* | Photo & video gallery for authorized parents. | Parents currently only see photos inside `/events` or `/journey`. A dedicated, fast media gallery with thumbnail grid and modal viewer is needed. |

---

## 4. Evaluation of Reusable UI Components

### Existing UI Shared Components
- `PageHeader`, `PageTopbar`, `StatusBadge`, `SearchFilterRow`.
- Extensive admin/teacher components (`ChildDailyCareSection`, `ConsentManagementSection`, `ParentCommunicationAssistantSection`).
- **Gap:** These components were built for daycare teachers/administrators. The parent portal needs dedicated, lightweight presentation components optimized for small mobile screens.

### Proposed Reusable Parent UI Components
1. **Layout & Navigation:**
   - `ParentMobileNav`: Bottom navigation bar for mobile viewports (Today, Journey, Memories, Messages, More) + responsive desktop sidebar.
   - `ChildSwitcher`: Compact child avatar pill with dropdown for families with multiple children.
   - `ParentPageHeader`: Standardized, warm nature-inspired header following `GEMINI.md`.
2. **Cards & Overview:**
   - `DailyUpdateCard`: Timeline item for meals, rest, check-in, and activities.
   - `CareSummaryCard`: High-density 2x2 wellbeing card (diapers, rest, meals, mood).
   - `ActivityTimeline`: Clean vertical feed with status indicators.
   - `ConsentStatusCard`: Shows status of required consent documents with signing action.
3. **Media & Gallery:**
   - `MediaThumbnail`: Lazy-loaded image card with aspect ratio preservation and thumbnail optimization.
   - `MediaGallery`: Filterable grid of child photos and celebration memories.
   - `MediaViewer`: Accessible full-screen modal lightbox with keyboard/touch navigation.
4. **Communication & Messaging:**
   - `ConversationList`: Displays active threads with classroom educators.
   - `ChatMessageBubble`: Formatted message bubble (inbound teacher vs outbound parent) with timestamps.
   - `ChatComposer`: Mobile-friendly message input with send button and auto-clear on submit.
   - `MessageStatus`: Indicator for sending, delivered, or read states.
5. **State & Feedback:**
   - `LoadingState`: Gentle skeleton or spinner loader.
   - `EmptyState`: Friendly empty state with icon, title, and descriptive advice.
   - `ErrorState`: Clear error notice with retry button.
   - `PaginationOrLoadMore`: Infinite scroll or "Load Older" action for message/journey history.

---

## 5. Security & Multi-Tenant Isolation Assessment

1. **Child Access Boundaries:**
   - Handled via `family_relationships` table.
   - Every service method in `ParentService` invokes `ValidateAccessOrThrow(childId)` which runs `SELECT EXISTS(...)` against `(tenant_id, user_id, child_id, status = 'Verified')`.
   - Guaranteed tenant isolation across organizations.
2. **Media Privacy & Consent:**
   - Child photos are private. Under no circumstance should arbitrary URLs allow cross-tenant enumeration.
   - Respects parental consent (`receives_photos` flag on `family_relationships` and `persons`).
3. **Messaging Security:**
   - Messages between parent and teachers must be scoped to `(tenant_id, organization_id, child_id, recipient_user_id)`.
   - Prevent injection or unauthorized thread snooping.

---

## 6. Performance & Circuit Optimization Baseline

- **Initial Load:** Interactive Server Blazor renders the prerendered HTML shell and opens a WebSocket connection to `_blazor`.
- **Payload Size:** Page HTML is ~15-25 KB; Blazor framework bundle is cached.
- **Circuit Footprint:** Because each connected parent holds a circuit on the server, viewmodels must remain lightweight:
  - Do not load 100+ journey observations or full-resolution photos into component fields.
  - Paginate journey entries and chat messages (e.g., initial 15 items, load more on demand).
  - Use DateOnly/TimeSpan or compact DTOs rather than full database entities.

---

## 7. Android Application Readiness Strategy

To prepare for a future native Android application without creating unnecessary duplicates:
1. All business logic remains in `EnjoyEveryday.Application` services (`IParentService`, `IConsentService`, etc.).
2. Data contracts are defined as pure C# records/DTOs with JSON serialization attributes.
3. Blazor components act strictly as presentation views, calling services or lightweight HTTP endpoints.
4. Prepare standard REST endpoints (`/api/parent/...`) matching the service interface so the Android app can consume the exact same backend endpoints.
5. Maintain cookie/token authentication compatibility.

---

## 8. Prioritized Implementation Roadmap

- [x] **Phase 0:** Assessment, route inventory, architectural baseline, and documentation.
- [ ] **Phase 1:** Build reusable parent UI component foundation (Headers, Cards, Media, States).
- [ ] **Phase 2:** Implement mobile-first navigation (responsive bottom bar & top header) and refactor Home (`/today`).
- [ ] **Phase 3:** Create dedicated Media & Memories gallery (`/memories`) with lazy loading and lightbox viewer.
- [ ] **Phase 4:** Upgrade teacher-parent messaging with persisted database communications and responsive chat UI.
- [ ] **Phase 5:** Refactor Journey, Events, Consent status, and Family Profile using modular components.
- [ ] **Phase 6:** Document Android-readiness, API endpoints, and mobile integration points.
- [ ] **Phase 7:** Compile, verify compilation, audit design compliance, and deliver final report.
