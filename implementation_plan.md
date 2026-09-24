# EnjoyEveryday — Unified Implementation Plan

## Overview

**EnjoyEveryday** is a multi-tenant platform for childcare organizations, centered on helping teachers create wonderful daily experiences for children. The child is the center of the system. The platform is **not** a daycare ERP — it is an experience-creation, experience-learning, and childhood-memory platform.

This plan unifies the requirements from:
- [Product Vision](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/EnjoyEveryday_product_vision.md)
- [Implementation Sequence](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/Implementation%20Sequence.md)
- [Management Wireframe](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/index.html) + [styles.css](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/styles.css) + [app.js](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/app.js)
- [Teacher View Wireframes](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/teacherview) (10 HTML wireframe files)

> [!IMPORTANT]
> The project workspace currently only contains the `requirements/` folder. No solution, no code, no database exists yet. This is a **greenfield** implementation starting from scratch.

---

## Technology Stack (Mandated)

| Layer | Technology | Version |
|---|---|---|
| **Runtime** | .NET | 10.0 |
| **Frontend** | Blazor Server + Fluent UI Blazor | v5.0.0.26098 |
| **Icons** | Microsoft.FluentUI.AspNetCore.Components.Icons | 4.14.4 |
| **API** | .NET REST API with Swagger/OpenAPI | — |
| **Authentication** | ASP.NET Identity | — |
| **Application** | .NET Application Services (Modular Monolith) | — |
| **ORM** | Dapper (no Entity Framework) | — |
| **Database** | PostgreSQL (authoritative source of truth) | — |
| **Migrations** | Raw SQL scripts | — |
| **Vector Search** | pgvector (for AI-powered search) | — |
| **Graph Intelligence** | SQLite graph projection (rebuilt from PostgreSQL) | — |
| **AI** | Google ADK (Agents, Controlled Tools) | — |
| **Deployment** | Azure App Service | — |
| **Architecture** | Modular Monolith — one domain at a time, vertical slices | — |

---

## Core Principles

1. **Wireframe is UI source of truth** — implement faithfully, do not redesign
2. **Vertical implementation** — each feature built database → dapper → application → API → authorization → Blazor → wireframe interaction
3. **AI Proposes → Application Validates → Human Decides → Database Records**
4. **Tenant isolation** enforced server-side at every layer
5. **PostgreSQL is authoritative** — SQLite graph is a rebuildable projection
6. **One domain at a time** — complete each before moving on
7. **No business logic in Blazor** — all logic behind REST APIs

---

## User Roles

| Role | Primary Question |
|---|---|
| **Teacher** | "What should I do next?" → TODAY screen |
| **Management** | "Are we creating a good rhythm?" → Org/Branch views |
| **Parent/Family** | "What happened in my child's world?" → Family view |
| **Org Owner/Admin** | Configuration, safety, trust |

---

## Approved Wireframes Inventory

The following **10 wireframes** exist in the `teacherview/` directory and serve as UI source-of-truth:

| Wireframe File | View |
|---|---|
| `enjoyeveryday-teacher.html` | Teacher View (Today, Experience execution) |
| `enjoyeveryday-daycareview.html` | Management/Daycare dashboard |
| `enjoyeveryday-experienceStudio.html` | Experience Studio (creation) |
| `enjoyeveryday-Experience_Planning.html` | Experience Planning / Monthly Planner |
| `enjoyeveryday-Child Journey.html` | Child Journey view |
| `enjoyeveryday-family.html` | Family/Parent experience |
| `enjoyeveryday-parent.html` | Parent stories view |
| `enjoyeveryday-intelligence.html` | AI Intelligence dashboard |
| `enjoyeveryday_multibranch_and_org_iew.html` | Multi-branch & Organization view |
| `enjoyeveryday_saftey_trustview.html` | Safety & Trust view |

Additionally, the main wireframe ([index.html](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/index.html) + [styles.css](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/styles.css) + [app.js](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/requirements/app.js)) provides the management navigation prototype.

---

## Implementation Phases

### Phase 1 — Foundation
**Goal:** Runnable solution with authentication, tenant context, and basic UI shell.

| # | Task | Vertical Slice |
|---|---|---|
| 1.1 | Create .NET solution structure (Modular Monolith) | `EnjoyEveryday.sln` with projects: `API`, `Application`, `Domain`, `Infrastructure`, `Web` (Blazor Server) |
| 1.2 | Configuration & appsettings | Connection strings, tenant config, logging config |
| 1.3 | PostgreSQL connection + Dapper infrastructure | Connection factory, base repository, migration strategy |
| 1.4 | REST API project with Swagger/OpenAPI | Minimal API or Controllers, Swagger UI |
| 1.5 | Blazor Server + Fluent UI Blazor setup | App shell, layout, Fluent UI integration |
| 1.6 | Authentication foundation | Cookie/JWT auth, login page |
| 1.7 | Authorization foundation | Claims-based policies, role checks |
| 1.8 | Tenant context middleware | Extract/validate tenant from authenticated user, propagate through request pipeline |
| 1.9 | Error handling (global) | Structured error responses, user-friendly messages |
| 1.10 | Logging (Serilog/structured) | Request logging, audit-ready |
| 1.11 | Audit foundation | `audit_log` table, audit service, WHO/WHAT/WHEN/WHERE/WHY/RESULT |
| 1.12 | Basic UI shell matching wireframe sidebar/topbar | Sidebar navigation, tenant selector, breadcrumb, user info |

**Completion Criteria:** Can log in, see tenant context, understand "Who am I? Which org? What can I access?"

---

### Phase 2 — Organization
**Goal:** Multi-tenant organizational hierarchy.

| # | Task | Database → API → Blazor |
|---|---|---|
| 2.1 | `tenants` table + CRUD | Tenant creation, configuration |
| 2.2 | `organizations` table + CRUD | Name, settings, owned by tenant |
| 2.3 | `branches` table + CRUD | Belongs to organization |
| 2.4 | `classrooms` table + CRUD | Belongs to branch, age group, capacity |
| 2.5 | Organization settings | Configuration per tenant |
| 2.6 | Environment/Material configuration | Materials, spaces per classroom |
| 2.7 | Blazor pages: Org settings, Branch list, Classroom list | Matching wireframe navigation |

**Completion Criteria:** Can create Tenant → Organization → Branch → Classroom → Configure.

---

### Phase 3 — People
**Goal:** Users, roles, children, families with relationships.

| # | Task | Notes |
|---|---|---|
| 3.1 | `users` table, role assignment | Organization owner, admin, branch admin, teacher, assistant teacher |
| 3.2 | `roles` and `permissions` tables | Granular permission system |
| 3.3 | `teachers` table + Blazor page | Teacher profile, classroom assignment |
| 3.4 | `children` table + Blazor page | Child profile, age, classroom, interests |
| 3.5 | `parents` / `families` tables | Parent/guardian profiles |
| 3.6 | Family relationships | Child-parent links, authorized access |
| 3.7 | Privacy boundaries | Parent sees only their authorized children |

**Completion Criteria:** Full people hierarchy. Teacher assigned to classroom with children. Parents linked to their children.

---

### Phase 4 — Experience Foundation
**Goal:** The central domain concept — Experience lifecycle.

| # | Task | Notes |
|---|---|---|
| 4.1 | `experiences` table with full DNA | Mission, Discovery, Challenge, Together, Create, Child Choice, Magic Moment, Accomplishment, Reflection, Teacher Guidance, Safety, Materials, Environment, Age, Duration, Group Size |
| 4.2 | Experience statuses/state machine | Draft → Approved → Scheduled → InProgress → Completed (with Skipped, Replaced, Adapted) |
| 4.3 | Experience Library API + Blazor | Search, filter, favorite, duplicate, archive — matching wireframe tabs (All, Created by Us, Created by Me, AI Suggested, Favorites) |
| 4.4 | Experience detail modal | Full experience view with DNA elements |

**Completion Criteria:** Can create, search, filter, view, favorite experiences in library.

---

### Phase 5 — Experience Studio
**Goal:** Creative workspace for teachers/management to design experiences.

| # | Task | Notes |
|---|---|---|
| 5.1 | Experience creation flow | Idea → Explore possibilities → Shape → DNA → Safety check → Preview → Save → Submit |
| 5.2 | Experience Studio Blazor pages | Matching `enjoyeveryday-experienceStudio.html` wireframe |
| 5.3 | Experience editing | Modify existing, maintain integrity |

**Completion Criteria:** Teacher can create a complete experience through the studio workflow.

---

### Phase 6 — Experience Versioning
**Goal:** Never overwrite experience history.

| # | Task | Notes |
|---|---|---|
| 6.1 | `experience_versions` table | Version number, creator, date, reason, changes, feedback, status |
| 6.2 | Version API + UI | View version history, compare versions |
| 6.3 | Edit creates new version | Old versions remain traceable |

**Completion Criteria:** Editing an experience creates v2, v3, etc. with full history.

---

### Phase 7 — Monthly Planner
**Goal:** Management plans experiences across a month.

| # | Task | Notes |
|---|---|---|
| 7.1 | `experience_schedule` table | Date, time, classroom, experience, status (Planned/Published/Started/Completed/Skipped/Replaced) |
| 7.2 | Calendar UI matching wireframe | 5-day week grid, month navigation, AI suggestions panel, balance indicators |
| 7.3 | Publish/unpublish month | Teachers see published plans |
| 7.4 | Month settings | Theme, classroom filter, age group |

**Completion Criteria:** Management selects month → classroom → assigns experiences → publishes.

---

### Phase 8 — Teacher TODAY
**Goal:** The teacher's primary screen — "What should I do next?"

| # | Task | Notes |
|---|---|---|
| 8.1 | TODAY page matching `enjoyeveryday-teacher.html` | Welcome card, weather, classroom context |
| 8.2 | Today's Rhythm section | 4-card grid of today's experiences with START button |
| 8.3 | Classroom feeling tracker | Emoji-based classroom mood |
| 8.4 | Little Moments panel | Quick observation capture |
| 8.5 | AI surprise/suggestion panel | Contextual suggestion |

**Completion Criteria:** Teacher opens TODAY, sees who is here, what's happening, what to do next.

---

### Phase 9 — Experience Execution
**Goal:** Record what actually happened.

| # | Task | Notes |
|---|---|---|
| 9.1 | Start experience flow | Scheduled → Started → InProgress → Teacher Completes |
| 9.2 | During-execution view | Mission, teacher prompt, questions, variations, safety reminders, materials |
| 9.3 | `experience_executions` table | Tracks actual start/end, who participated |

**Completion Criteria:** Teacher clicks START → experience runs → teacher completes.

---

### Phase 10 — Experience Feedback
**Goal:** Quick post-experience teacher feedback.

| # | Task | Notes |
|---|---|---|
| 10.1 | Feedback form | ❤️ Loved it / 🙂 Good / 😐 Difficult / 🙅 Didn't work |
| 10.2 | Optional deep feedback | Observation text, voice note, accomplishment, teacher note |
| 10.3 | `experience_feedback` table | Links to execution, simple + detailed feedback |

**Completion Criteria:** After completing, teacher gives quick feedback. Optional deeper notes.

---

### Phase 11 — Experience History
**Goal:** Every execution creates traceable history.

| # | Task | Notes |
|---|---|---|
| 11.1 | `experience_history` table | Aggregate usage data: times used, teachers, common observations, adjustments, difficulties |
| 11.2 | History view on experience detail | Usage stats, teacher quotes, patterns |

**Completion Criteria:** Experience detail shows "used 12 times, 7 teachers, common observations."

---

### Phase 12 — Experience Harvest
**Goal:** Convert history into learning and improvement.

| # | Task | Notes |
|---|---|---|
| 12.1 | Harvest & Insights page matching wireframe | Insight banner, experience table, teacher quotes, flywheel visualization |
| 12.2 | Pattern detection (deterministic first) | Aggregate feedback patterns |
| 12.3 | Suggested improvements → new version flow | Human reviews AI/system suggestion → creates new version |

**Completion Criteria:** System shows discovered patterns. Human can accept suggestion to create improved version.

---

### Phase 13 — Child Journey
**Goal:** Connect experiences to individual children.

| # | Task | Notes |
|---|---|---|
| 13.1 | `observations` table | Teacher observations linked to child + experience |
| 13.2 | `accomplishments` table | Meaningful child moments |
| 13.3 | `memories` table | Curated memorable events |
| 13.4 | Child Journey page matching wireframe | Memory garden visualization, interests, accomplishments, timeline |
| 13.5 | No scores/rankings/percentages | Journey describes the child as a human being |

**Completion Criteria:** Child profile shows interests, recent moments, accomplishments — all factual, not scored.

---

### Phase 14 — Parent/Family Experience
**Goal:** "A little window into your child's day."

| # | Task | Notes |
|---|---|---|
| 14.1 | `parent_stories` table | Approved stories derived from observations |
| 14.2 | Family view matching wireframe | Today's experiences, little moments, memories, approved photos, teacher stories |
| 14.3 | Parent cannot see internal notes/AI/other children | Privacy enforcement server-side |
| 14.4 | Consent-based media sharing | Only approved content reaches parents |

**Completion Criteria:** Parent opens app → sees warm story about their child's day. Cannot see internal data.

---

### Phase 15 — Safety & Trust
**Goal:** Cross-cutting safety across all modules.

| # | Task | Notes |
|---|---|---|
| 15.1 | Consent management | Per-child consent records |
| 15.2 | Media permissions | Photo/video sharing approvals |
| 15.3 | Child protection rules | Safety checks in experience studio |
| 15.4 | Incident management | Record, track, resolve incidents |
| 15.5 | Safety & Trust dashboard matching wireframe | Consent status, incidents, audit trail, governance |
| 15.6 | AI governance rules | AI safety constraints |
| 15.7 | Data access policies | Purpose-driven access control |

**Completion Criteria:** Safety dashboard operational. Consent tracked. Incidents manageable. AI governed.

---

### Phase 16 — Organization View
**Goal:** Management sees the whole organization.

| # | Task | Notes |
|---|---|---|
| 16.1 | Organization dashboard matching wireframe | Branches, classrooms, experience rhythm, adoption metrics |
| 16.2 | Multi-branch view | Compare branches, patterns |
| 16.3 | Meaningful trends (not teacher rankings) | Experience adoption, emerging interests, support needs |

**Completion Criteria:** Management understands organization-wide patterns. Not micromanaging teachers.

---

### Phase 17 — AI Foundation (Google ADK)
**Goal:** Introduce AI agents only after real domain data exists.

| # | Task | Notes |
|---|---|---|
| 17.1 | AI Gateway service | Application → AI Gateway → Google ADK → Agent → Controlled Tools → Validation → Database |
| 17.2 | Initial agents | Experience Planner, Experience Improvement, Classroom Context, Child Journey, Parent Story, Experience Search, Safety Review, Coordinator |
| 17.3 | AI governance enforcement | AI never bypasses auth/consent/safety |
| 17.4 | AI UI matching wireframe | Intelligence dashboard, AI assistant modal |

**Completion Criteria:** AI agents can suggest, search, improve — but never autonomously publish or bypass rules.

---

### Phase 18 — Graph Intelligence
**Goal:** SQLite graph projection for relationship queries.

| # | Task | Notes |
|---|---|---|
| 18.1 | Graph projection from PostgreSQL | Child → Experience → Teacher → Classroom → Branch → Organization |
| 18.2 | Relationship queries | "Which experiences use materials in this classroom?" |
| 18.3 | Graph rebuild capability | Delete graph → rebuild from PostgreSQL |

**Completion Criteria:** Graph answers relationship questions. Deletable and rebuildable.

---

## Development Rules Summary

| Rule | Description |
|---|---|
| **Vertical slicing** | Database → Dapper → App Logic → API → Swagger → Auth → Validation → Blazor → Fluent UI → Wireframe |
| **Wireframe fidelity** | Do not redesign. If conflict: STOP → REPORT → ASK |
| **Completion matrix** | Every feature generates a 14-layer completion checklist |
| **No fake data** | Use correct empty states, not invented demo data |
| **Tenant isolation** | Every query carries tenant context, validated server-side |
| **State machines** | Invalid lifecycle transitions rejected (e.g., Archived → InProgress) |
| **Transactions** | Multi-record operations use appropriate transactional boundaries |
| **Error handling** | Success, validation failure, auth failure, not found, conflict, system failure |
| **Audit** | WHO/WHAT/WHEN/WHERE/WHY/RESULT for important operations |

---

## First End-to-End Scenario (Validation Target)

The following must work as a single, complete flow before anything else is considered done:

```
Little Stars Daycare (Tenant)
  → Downtown (Branch)
    → Bluebirds (Classroom)
      → Priya (Teacher)
        → 18 Children (including Aarav)
          → "Our Little City" (Experience)
            → Scheduled for today
              → Teacher opens TODAY
                → Teacher starts experience
                  → Children experience it
                    → Teacher records feedback (❤️)
                      → Teacher records observation
                        → Accomplishment recorded
                          → Child Journey updated
                            → Parent receives approved story
```

---

## Resolved Decisions

| Decision | Answer |
|---|---|
| **Authentication** | ASP.NET Identity |
| **Database Migrations** | Raw SQL scripts |
| **Deployment Target** | Azure App Service |
| **Fluent UI Blazor** | v5.0.0.26098 (.NET 10, matching reference project patterns) |
| **Seed Data** | Yes — 3 sample tenants for dev/demo, production stays clean |
| **Reference Project** | `FertilityHospital.UI.InternalBlazor` — Fluent UI v5 patterns (FluentLayout, FluentNav, FluentCard, FluentDataGrid, FluentStack, etc.) |

---

## Proposed Solution Structure

```
EnjoyEveryday/
├── src/
│   ├── EnjoyEveryday.Api/              # REST API (Controllers, Swagger)
│   ├── EnjoyEveryday.Web/              # Blazor Server + Fluent UI
│   ├── EnjoyEveryday.Application/      # Application services, commands, queries
│   ├── EnjoyEveryday.Domain/           # Domain entities, value objects, rules
│   ├── EnjoyEveryday.Infrastructure/   # Dapper, PostgreSQL, external integrations
│   └── EnjoyEveryday.Shared/           # Cross-cutting: auth, tenant context, audit
├── database/
│   └── migrations/                     # SQL migration scripts
├── tests/
│   ├── EnjoyEveryday.Tests.Unit/
│   ├── EnjoyEveryday.Tests.Integration/
│   └── EnjoyEveryday.Tests.E2E/
├── requirements/                       # Existing requirements (unchanged)
│   ├── EnjoyEveryday_product_vision.md
│   ├── Implementation Sequence.md
│   ├── index.html / styles.css / app.js
│   └── teacherview/                    # 10 wireframe HTML files
└── EnjoyEveryday.sln
```

---

## Verification Plan

### Automated Tests
- Unit tests for domain rules (state machine transitions, validation)
- Integration tests for API endpoints (auth, tenant isolation, CRUD)
- Database integration tests (Dapper queries, migrations)

### Manual Verification
- Each phase concludes with wireframe fidelity check (layout, components, terminology, interaction, states)
- End-to-end scenario walkthrough after Phase 14
- Tenant isolation verified by attempting cross-tenant access

---

## Recommended Starting Point

I recommend we begin with **Phase 1 — Foundation** which involves:
1. Creating the .NET solution structure
2. Setting up PostgreSQL + Dapper
3. Blazor Server + Fluent UI shell
4. Authentication/Authorization foundation
5. Tenant context pipeline

Once you confirm the open questions above (especially auth provider, migration tool, and deployment target), I can begin implementation immediately.
