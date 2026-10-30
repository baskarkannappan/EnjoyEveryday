# EnjoyEveryday Web Application Theme & Design System

Every agent (including Antigravity) working in this repository MUST strictly follow the EnjoyEveryday design system and theme when creating, modifying, or refactoring pages.

## 1. Aesthetic Philosophy & Brand Identity

EnjoyEveryday is a daycare and early-childhood education management platform.
- **Tone**: Warm, calming, nature-inspired, organic, clean, and Scandinavian-crafted.
- **Strictly Prohibited**:
  - Do NOT use generic cold Tailwind blues (`#2563eb`, `#1d4ed8`, `#3b82f6`).
  - Do NOT use dark mode, neon colors, heavy purple gradients, or tech-bro SaaS templates.
  - Do NOT create ad-hoc generic bootstrap-style tables or unstyled `<div>` grids.

---

## 2. Core CSS Variables & Color Tokens

Always reference these CSS variables or their hex equivalents:

| Variable | Hex Value | Purpose |
|:---|:---|:---|
| `--bg` | `#f5f5ef` / `#f7f8f5` | Page background |
| `--surface` | `#ffffff` | Primary card / container surface |
| `--surface-soft` | `#f8f9f5` | Inner sections, tags, subtle input background |
| `--border` | `#e4e7df` / `#e1e6e0` | Subtle warm border lines |
| `--text` | `#26332d` | Primary body and heading text (deep forest slate) |
| `--muted` | `#6c7870` / `#7c877f` | Subtitles, labels, timestamps, secondary details |
| `--accent` | `#557c6c` / `#52745e` | Signature brand sage green (primary buttons & highlights) |
| `--accent-light` | `#e6eee9` / `#e5eee8` | Active badges, avatar background, highlighted pills |
| `--accent-dark` | `#3e6253` | Button hover, icon color, strong status badges |
| `--warm` | `#f4eee1` / `#f5efe3` | Warm sand / cream accent |
| `--shadow` | `0 8px 30px rgba(40, 55, 47, .06)` | Standard natural card elevation shadow |
| `--radius` | `18px` | Standard border radius for cards & major containers |

---

## 3. Typography & Fonts

- **Font Family**: `"Plus Jakarta Sans", "Figtree", Inter, -apple-system, sans-serif` — defined once as `--ee-font` in `src/EnjoyEveryday.UI.Shared/wwwroot/css/ee-foundation.css` (loaded last by every portal). Do NOT hardcode other font families in pages or components; use the shared type scale variables (`--fs-xs` … `--fs-2xl`).
- **Heading 1 / 2**: `font-weight: 750` to `800`, letter-spacing `-0.3px` to `-0.5px`, color `var(--text, #26332d)`.
- **Subtitles**: `font-size: 13px` to `14px`, color `var(--muted, #6c7870)`.

---

## 4. Standard Page Structure

Every page in `EnjoyEveryday.Daycare.Web` should follow this structure:

```razor
@page "/your-route"
@using EnjoyEveryday.UI.Shared.Components

<PageTitle>Page Title — EnjoyEveryday</PageTitle>

<div class="content">
    <PageHero Eyebrow="CATEGORY"
              Title="Page Title"
              Subtitle="Concise, friendly description of this page's purpose.">
        <RightContent>
            <a href="/secondary" class="btn secondary">Secondary Action</a>
            <button class="primary" @onclick="HandleAction">+ Primary Action</button>
        </RightContent>
    </PageHero>

    <!-- Page Content (Cards, Grids, Forms) -->
</div>
```

---

## 5. Standard Component Patterns

### A. Buttons
- **Primary**: `<button class="primary">` or `<a class="btn primary">`
  - Rounded `11px`, background `var(--accent)`, hover `var(--accent-dark)`, white text, `font-weight: 700`.
- **Secondary**: `<button class="secondary">` or `<a class="btn secondary">`
  - Rounded `11px`, background `white`, border `1px solid var(--border)`, color `var(--text)`.

### B. Directory & Listing Grids (`people-grid` & `person`)
When listing people (teachers, children, users, parents):
```razor
<!-- Search & Filter Row -->
<div class="search-row">
    <input class="search" @bind="searchQuery" placeholder="Search..." />
    <select class="filter" @bind="selectedFilter">
        <option value="All">All Items</option>
    </select>
</div>

<!-- Filter Tabs -->
<div class="tabs">
    <button class="tab @(activeTab == "All" ? "active" : "")" @onclick='() => SetTab("All")'>All</button>
</div>

<!-- People Grid -->
<div class="people-grid">
    <div class="person">
        <div class="person-header">
            <div class="person-avatar" style="background: var(--accent-light); color: var(--accent-dark);">
                A
            </div>
            <div>
                <h3>Name</h3>
                <p>Email or Subtitle</p>
            </div>
        </div>
        <div class="person-interest">
            <strong>Assignment</strong>
            <p>Classroom or Role details</p>
        </div>
    </div>
</div>
```

### C. Cards & Containers
- Cards must use `background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); box-shadow: var(--shadow); padding: 20px;`.
- Avatar badges must use `background: var(--accent-light); color: var(--accent-dark);` or gradient `linear-gradient(135deg, var(--accent), var(--accent-dark))`.

### D. Badges & Tags
- Active status: `background: var(--accent-light, #e6eee9); color: var(--accent-dark, #3e6253); border: 1px solid #c8dacd; border-radius: 6px; font-weight: 700;`
- Inactive status: `background: #fff5f5; color: #9b2c2c; border: 1px solid #fed7d7; border-radius: 6px; font-weight: 700;`

---

## 6. Testing Policy

- **CRITICAL**: Do NOT run automated tests (`dotnet test`, unit tests, integration tests, test projects, or test runner scripts) unless the user explicitly instructs you to do so.

---

## 7. AI Usage & Cost-Control Architecture Rules

Every agent working in this repository MUST strictly follow these AI and cost-control rules:

1. **Rules-First Architecture**: Use ordinary application code, PostgreSQL, repositories, and deterministic business rules for authentication, authorization, multi-tenant isolation, attendance, scheduling, billing, memory timelines, and validation. Never invoke AI for logic that ordinary code performs reliably.
2. **AI Only When Adding Clear Value**: Use Google ADK selectively (e.g. explaining why an activity fits, summarizing observations, drafting communications). Do NOT create competing AI services or duplicate agents.
3. **On-Demand Execution**: AI runs on-demand when requested by an authorized user. Never poll or run background regeneration loops on page loads, filter changes, or database updates.
4. **Deterministic Fallback**: Every AI feature must have a functional, transparent non-AI fallback. Never fabricate data or pretend an AI analysis succeeded when the provider is unavailable.
5. **Cost & Performance Controls**: Bounded inputs, timeouts, cancellation, token budgeting, and duplicate request prevention. Do not cache personalized insights across users/tenants.
6. **Data Minimization**: Send only the minimal authorized fields. Never send raw tables, full child histories, family details, credentials, or tokens. Enforce tenant and authorization boundaries *before* assembling AI input.
7. **No AI-Controlled Business Decisions**: AI must never independently grant access, verify payments, approve consent, publish content, override safety rules, record attendance, or alter child milestone records. Teachers remain in control.
8. **Simple Architecture**: No unnecessary microservices, event streams, or vector databases (no pgvector) without verified, measured justification.
9. **Core Operations Must Always Work**: EnjoyEveryday must continue to perform all essential daycare operations when the AI provider is unavailable, rate-limited, misconfigured, or disabled.

---

## 8. Database Connection & Environment Details

- **Host**: `localhost`
- **Port**: `5433`
- **Database**: `enjoyeveryday`
- **Username**: `postgres`
- **Password**: `postgres`
- **CLI Utility**: `C:\Program Files\PostgreSQL\18\bin\psql.exe`
- **PowerShell Invocation**:
  ```powershell
  $env:PGPASSWORD="postgres"; & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U postgres -d enjoyeveryday -c "<SQL_QUERY>"
  ```
- **Dev Tenant ID**: `10000000-0000-0000-0000-000000000001`
- **Dev User ID**: `50000000-0000-0000-0000-000000000002` (`admin@littlestars.com`)
- **Dev Org ID**: `20000000-0000-0000-0000-000000000001`

---

## 9. Autonomous Execution & Modification Policy

- **Full Permission Granted**: The agent has full, standing permission to make necessary code, configuration, database schema, and migration changes.
- **Do Not Prompt for Permission**: Proactively implement, test, and apply changes directly without asking for permission or confirmation before editing files or running operational database scripts.

---

## 10. Fast Development & UI Workflow Guidelines

Every agent working in this repository MUST follow these performance and UI rules to guarantee fast turnaround:

1. **Targeted Builds Only (Never Full Solution)**:
   - **Strictly Prohibited**: Do NOT run `dotnet build EnjoyEveryday.slnx` for UI, Razor, or single-project edits (it builds 10+ projects and wastes 40+ seconds per check).
   - **Always Use Targeted Project Builds with `--no-restore`**:
     ```powershell
     dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore
     ```
     This completes in ~3 seconds instead of 41+ seconds.

2. **CSS Cascade & Architecture Rules**:
   - `src/EnjoyEveryday.UI.Shared/wwwroot/css/ee-foundation.css` is loaded last and enforces global container sizing (`zoom: 0.82`, `max-width: 1720px !important`).
   - When creating focused pages (such as `TeacherWorkspace` with a 960px max width), **always** specify `max-width: 960px !important; margin: 0 auto !important;` to prevent the global container rule from stretching the layout.
   - For interactive cards with icons and text:
     - Wrap titles and subtitles inside an `.action-card-text` (or `.card-text`) container to prevent browser flex distribution bugs.
     - Use `<div class="action-card" role="button" tabindex="0">` instead of `<button>` for complex card layouts to prevent Chromium `<button>` vertical stretching.

3. **Mobile Layout Standards**:
   - **Stats / Metrics**: Always keep 3 stat cards in a single compact 3-column row on mobile (`repeat(3, 1fr)` with 18px numbers and 11px labels) rather than stacking them vertically.
   - **Primary Action Grids**: Use a 2-column grid (`repeat(2, 1fr)`) on mobile with compact padding (`12px`) so all primary actions remain immediately accessible above the fold.

4. **Live DOM Verification**:
   - When diagnosing layout or CSS issues, use the `browser_subagent` tool on `http://localhost:7003` to inspect computed styles and layout bounds directly in the DOM instead of trial-and-error edits.

---

## 11. Fast Data Diagnosis & Fix Workflow

When diagnosing and fixing data/database issues (especially when instructed not to modify UI):

1. **Batch SQL Diagnostics in a Single Execution**:
   - Do NOT run multiple single-statement `psql` queries one-by-one.
   - Batch all diagnostic checks into a single multi-query SQL execution to avoid PowerShell/psql connection overhead.
2. **Hypothesis-First Root Cause Pattern**:
   - For `0/0` metrics or phantom rows, immediately check:
     1. Date mismatches (`attendance_date` vs current calendar date).
     2. Inactive duplicates (`is_active = false` missing from stored function filters).
     3. System dummy records (e.g., virtual pool classrooms with 999 capacity).
3. **No Rebuilds for Database-Only Changes**:
   - PostgreSQL schema changes, stored functions, and row updates take effect instantly in the running application.
   - Do NOT run `dotnet build` unless C# code or Razor files have been modified.

