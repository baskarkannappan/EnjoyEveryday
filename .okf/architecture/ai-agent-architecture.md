---
okf_version: "0.2.0"
type: concept
title: "AI & Cost-Control Architecture Guidelines"
description: "Rules-first architecture, bounded Google ADK usage, on-demand execution, and fallback strategies."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - ai
  - gemini
  - adk
  - cost-control
  - architecture
---

# AI & Cost-Control Architecture Guidelines

EnjoyEveryday operates with a **Rules-First Architecture**. AI enhances human experience but never governs deterministic daycare business rules.

## The 9 Core AI Architectural Rules

1. **Rules-First**: Ordinary C# code, PostgreSQL, and repositories govern authentication, multi-tenancy, attendance, ratios, billing, and scheduling. Never call AI for deterministic logic.
2. **Selective ADK Usage**: Google ADK / Gemini is used strictly for contextual value (e.g. suggesting why an activity fits a child's milestone, summarizing daily observations, drafting parent comms).
3. **On-Demand Only**: AI runs on explicit user click. Never trigger background loops on page load, filter change, or row updates.
4. **Deterministic Fallbacks**: Every AI feature must have a graceful non-AI fallback (e.g. standard milestone descriptions if Gemini is unreachable). Never fabricate responses.
5. **Cost & Token Budgeting**: Inputs are strictly bounded; cancellation tokens and timeouts are enforced. No caching personalized data across tenants.
6. **Data Minimization**: Send minimal authorized tokens (e.g. first name, age group, activity tag). Never pass raw database tables, financial records, or credentials to LLMs.
7. **No AI Business Decisions**: AI must never record attendance, alter child health records, approve billing, or grant security roles.
8. **No Vector Bloat**: Standard relational indices are used. No `pgvector` or extraneous vector stores without proven, measured justification.
9. **Zero Operational Downtime**: Essential daycare operations (check-in, diaper logs, emergency pickup) remain 100% functional when AI services are offline or rate-limited.

## 3-Tier Multi-Provider Generation Cascade
Every AI service endpoint (`/api/ideas`, `/api/improve`, `/api/materials`, `/api/recommendations`, `/api/journey/insights`, `/api/weekly-plan`, `/api/parent/draft`, `/api/organization/kpi-intel`) is routed through `ai_provider_registry.generate_with_failover`:

1. **Tier 1 (Google Gemini)**:
   * **Primary**: `gemini-3.5-flash-lite` (High-throughput, ultra-fast generation for studio ideation and weekly planning).
   * **Secondary**: `gemini-3.1-flash-lite` (Frontier performance with cost-efficiency).
   * **Model Fallbacks**: `gemini-3.5-flash`, `gemini-flash-lite-latest`, `gemini-flash-latest`.
2. **Tier 2 (OpenAI Automatic Failover)**:
   * If Gemini encounters an authentication rejection, quota limit, or temporary 503 outage, the engine automatically attempts execution with **OpenAI (`gpt-4o-mini`)** using the configured OpenAI key.
3. **Tier 3 (Deterministic Safe Fallback)**:
   * If both Google Gemini and OpenAI fail or are offline, the engine builds deterministic pedagogical fallback templates directly from the prompt keywords so that no user request ever hangs, crashes, or fails.

## Context Grounding & Quality Engineering

* **Classroom Grounding & Inventory Prioritization**: `/api/ideas` receives structured classroom parameters (`ageGroup`, `environment`, `focusArea`, `availableMaterials`, `topExperiences`). Prompts prioritize on-hand inventory supplies and align pedagogical depth with the center's top exemplar experiences.
* **Domain-Clustered Memory Pre-Processing**: In `ChildJourneyInsightService`, raw chronological memories are classified into 4 early childhood developmental domains (Communication & Language, Social-Emotional, Physical & Motor, Cognitive & STEM Inquiry) with balanced quotas before prompting.
* **PII Minimization & De-Identification**: Child surnames, phone numbers, and email patterns are masked (`[Surname]`, `[Phone]`, `[Email]`) before transmitting evidence to external LLM providers.
* **Pediatric Early Childhood Safety Validator**: `SafetyValidator` inspects generated activities for choking hazards (water beads, small loose parts, magnets), sharp cutting tools, toxic substances, and common early childhood allergens (peanuts, tree nuts, raw egg cartons) with age-adaptive thresholding (<36 months vs 3–5 years).
* **Trajectory & Zone of Proximal Development (ZPD) Modeling**: Child journey prompts (`JOURNEY_INSIGHTS_INSTRUCTION` v2.1.0) analyze the gap between independent evidence and proximal emergent milestones, generating actionable next-step suggestions with 1-click scheduling directly into the classroom calendar.
* **On-the-Fly In-Session Pivots**: `/api/in-session-pivot` gives teachers 2 immediate classroom micro-adaptations using existing materials when children finish early, get restless, or encounter difficulty.
* **Constructive Tone & Empathy Refinement**: `ParentCommunicationAssistantService.RefineMessageToneAsync` transforms sensitive notes into strength-based developmental growth language.
* **Shift Handover Auto-Drafter**: Synthesizes room energy, child nap lengths, meal intakes, allergy alerts, and care timers directly from active daily care logs into shift handovers.
* **Curriculum Health & Domain Gap Audit**: In `HarvestView.razor`, evaluates learning domain distributions and recommends library activities to fill under-represented areas with 1-click calendar planning.

## API Key Encryption & Configuration
* Primary Gemini key is encrypted at rest with AES-256 in `tenants.encrypted_ai_api_key` and decrypted per-tenant.
* OpenAI fallback key is configured via `OPENAI_API_KEY` in `src/EnjoyEveryday.AiAgent/.env` or passed via `X-OpenAI-Api-Key` request header.
* **Container & Service-to-Service Security**:
  * `.dockerignore` excludes all `.env` files so local secrets are never baked into Docker images.
  * `src/EnjoyEveryday.AiAgent/Dockerfile` runs under a non-root `appuser` (`UID 10001`) with a `/health` `HEALTHCHECK` and internal-only `expose: ["8000"]` in Compose.
  * `.NET` `HttpClient` registration in `ApplicationServiceRegistration.cs` supports `AiAgent:BaseUrl` / `AI_SERVICE_BASE_URL` and attaches `X-Service-Token` when `AiAgent:ServiceSecret` / `AI_SERVICE_SECRET` is configured.

