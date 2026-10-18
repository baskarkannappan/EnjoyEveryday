# EnjoyEveryday — Reusable Component Guidelines & Architecture Strategy

This document establishes the official component reuse strategy, design system alignment, and architectural patterns for refactoring the EnjoyEveryday Blazor application. Its goal is to enable a single .NET developer to easily understand, modify, debug, test, and maintain the codebase with high confidence and minimal cognitive load.

---

## 1. Architectural Philosophy & Layering

```
+-------------------------------------------------------------------+
| Host Applications: Daycare.Web | Admin.Web | Parent.Web          |
|  - Page Routing & Top-Level Page Orchestration                    |
|  - Security & Authorization Policies (@attribute [Authorize])     |
|  - Feature-Specific Presentation Components (feature folders)     |
+-------------------------------------------------------------------+
                                 |
                                 v
+-------------------------------------------------------------------+
| EnjoyEveryday.UI.Shared                                            |
|  - Reusable Presentation Components (Cards, Modals, Badges, Tables)|
|  - Shared Domain Views (e.g. ChildrenDirectoryView, HarvestView)  |
|  - Pure UI controls with explicit typed parameters & EventCallbacks|
|  * Strictly NO direct database access or SQL execution             |
+-------------------------------------------------------------------+
                                 |
                                 v
+-------------------------------------------------------------------+
| EnjoyEveryday.Application & Domain                                |
|  - Application Services, Business Rules, Validation, DTOs         |
|  - Repositories, Domain Entities, Multi-tenant Isolation          |
+-------------------------------------------------------------------+
```

### A. Shared UI Components (`EnjoyEveryday.UI.Shared`)
Components belong in `EnjoyEveryday.UI.Shared` if and only if they represent presentation patterns shared across multiple features or across multiple web hosts (`Daycare.Web`, `Admin.Web`, `Parent.Web`).

**Standard Shared Component Library:**
- **Layout & Structure**: `PageHero.razor`, `PageTopbar.razor`, `PageHeader.razor`, `WizardSidebar.razor`
- **Feedback & States**: `LoadingState.razor`, `EmptyState.razor`, `StatusBadge.razor`, `Badge.razor`
- **Data Display**: `MetricCard.razor`, `PersonCard.razor`, `Card.razor`, `IconCard.razor`, `DnaCard.razor`
- **Filtering & Navigation**: `FilterTabs.razor`, `SearchFilterRow.razor`
- **Modals & Dialogs**: `ActiveSessionModal.razor`, `SecurePickupModal.razor`, `EventPhotoUploader.razor`

*Rule*: Shared UI components must never inject `IDbConnectionFactory`, execute direct queries, or manipulate database transactions. They receive data through parameters and emit intent through typed callbacks.

### B. Feature-Specific Components
Keep components inside their owning feature directory when they are only used by that feature or tightly coupled to that feature's lifecycle.
Examples:
- **Materials / Warehouse**: Material order timelines, stock balance adjusters, delivery verification panels.
- **Child Journey & Milestones**: Journey milestone tags, conference portfolio summaries, observation cards.
- **Attendance & Operations**: Live room headcounts, daily care log entries, emergency evacuation lists.

Do not prematurely hoist a component into `UI.Shared` unless genuine cross-feature reuse exists or is planned with verified compatibility.

---

## 2. Component Design & Contract Standards

Every component in EnjoyEveryday must adhere to a clean, declarative contract:

### 1. Explicit Typed Parameters
- **Data Input**: Strongly typed DTOs or Domain Entities (e.g., `[Parameter] public MaterialOrderDto Order { get; set; } = default!;`).
- **Display Configurations**: Clear labels, titles, and layout modes.
- **States**: `[Parameter] public bool IsLoading { get; set; }`, `[Parameter] public bool IsReadOnly { get; set; }`.
- **Avoid Boolean Explosion**: If a component accepts more than 3–4 boolean flags controlling divergent rendering paths, decompose it into smaller focused components or an enum/strategy.

### 2. Event Handling with `EventCallback<T>`
- Always use `EventCallback<T>` or `EventCallback` instead of `Action<T>` or `Func<T>`.
- `EventCallback` automatically dispatches state updates to the hosting parent without requiring manual `StateHasChanged()` calls.

```razor
[Parameter] public EventCallback<Guid> OnSelect { get; set; }
[Parameter] public EventCallback<UpdateItemRequest> OnSave { get; set; }
```

### 3. Asynchronous Initialization & Lifecycle
- Prefer `OnInitializedAsync` or `OnParametersSetAsync` for asynchronous data handling.
- Avoid calling blocking `.Result` or `.Wait()`.
- Always implement `IDisposable` or `IAsyncDisposable` if attaching event subscriptions, timers, or JS event listeners to prevent circuit memory leaks in Blazor Server.

---

## 3. EnjoyEveryday Visual Design System & Tokens

All components must strictly adhere to `GEMINI.md`:
- **Philosophy**: Warm, calming, nature-inspired, organic, clean, and Scandinavian-crafted early-childhood education aesthetic.
- **Strictly Prohibited**: Generic cold Tailwind blues (`#2563eb`), dark mode, neon accents, heavy purple SaaS gradients.

### Core Token Palette:

| CSS Variable | Hex Equivalent | Description |
|---|---|---|
| `--bg` | `#f5f5ef` / `#f7f8f5` | Page & canvas background |
| `--surface` | `#ffffff` | Primary card / container surface |
| `--surface-soft` | `#f8f9f5` | Inner sections, subtle inputs, secondary tags |
| `--border` | `#e4e7df` / `#e1e6e0` | Subtle warm border lines |
| `--text` | `#26332d` | Primary headings and text (deep forest slate) |
| `--muted` | `#6c7870` / `#7c877f` | Subtitles, labels, timestamps |
| `--accent` | `#557c6c` / `#52745e` | Signature brand sage green (primary buttons/pills) |
| `--accent-light` | `#e6eee9` / `#e5eee8` | Active badges, avatar background |
| `--accent-dark` | `#3e6253` | Button hover, strong status badges |
| `--warm` | `#f4eee1` / `#f5efe3` | Warm sand / cream accent |
| `--shadow` | `0 8px 30px rgba(40, 55, 47, .06)` | Natural card elevation shadow |
| `--radius` | `18px` | Standard border radius for cards & containers |

---

## 4. Multi-Tenant Isolation & Security Safeguards

1. **Authorization is Server-Enforced**: Hiding UI elements using `if (!user.IsInRole(...))` is for user experience only. All mutations, data retrieval, and state transitions must be authorized by application services.
2. **Context Propagation**: Never store tenant or user information in singleton services, static fields, or shared component state. User context must flow from ambient `IUserContext` / `ITenantContext` scoped to the active circuit.
3. **No Cross-Circuit State Leakage**: Do not use `static` event buses or global collections for transient UI notifications.

---

## 5. Performance Safeguards

1. **Query Consolidation**: Do not invoke individual database queries inside loop-rendered child components (avoids N+1 query amplification). The parent page or view should load data in bulk and pass read-only projections to children.
2. **Virtualization**: Use `<Virtualize>` or existing server-side pagination for long lists of children, transactions, or materials.
3. **Rendering Efficiency**: Keep component trees shallow. Decompose large monoliths into 2–4 cohesive child components rather than 20 micro-wrappers.
