# EnjoyEveryday — Refactoring Architecture Decisions

This document records key architectural decisions made during the Blazor page and component refactoring, including rationale for component placement, boundary separation, and trade-offs.

---

## Decision Record 001: Component Placement Boundaries (UI.Shared vs. Feature Directories)

- **Date**: 2026-10-18
- **Status**: Approved
- **Context**: 
  EnjoyEveryday features multiple web applications (`Daycare.Web`, `Admin.Web`, `Parent.Web`). There is a temptation either to place every newly extracted component into `EnjoyEveryday.UI.Shared`, leading to an unmanageable dumping ground, or to keep everything isolated in host apps, leading to code duplication.
- **Decision**:
  1. `EnjoyEveryday.UI.Shared` is strictly reserved for:
     - Pure presentation components (layout containers, design tokens, badges, buttons, empty/loading states).
     - Cross-host domain views where identical UX and logic is shared between Daycare and Admin (e.g. `ChildrenDirectoryView`, `TeachersDirectoryView`, `HarvestView`).
  2. Feature-specific components (e.g., `MaterialOrderTimeline`, `WarehouseStockAdjuster`, `EnrollmentStageCard`, `AddOperationalGoalModal`) remain located in their feature directories (or within a feature-grouped folder in UI.Shared if shared between hosts).
  3. Presentation components in `UI.Shared` must NEVER reference `IDbConnectionFactory`, execute raw SQL, or depend on `EnjoyEveryday.Infrastructure`.
- **Consequences**:
  Clean architecture boundaries are preserved. Feature components can evolve without risking regressions in unrelated modules.

---

## Decision Record 002: Dual-Host Experience Studio & Catalog Consolidation Strategy

- **Date**: 2026-10-18
- **Status**: Approved & Executed (Phase 1)
- **Context**:
  `ExperienceStudio.razor` exists in both `Daycare.Web` and `Admin.Web` with nearly identical markup, state management, and AI idea co-creation logic. Similarly, `Experiences.razor` exists in both hosts. Both pages embedded duplicated modals for version comparison and curation submission.
- **Decision**:
  1. Extracted `ExperienceVersionCompareModal` into `EnjoyEveryday.UI.Shared`, completely unifying the 85-line version history dialog and diff calculation across all 4 host files.
  2. Extracted `ExperienceCurationSubmitModal` into `EnjoyEveryday.UI.Shared` for the community library submission workflow.
  3. Maintained host-specific routing and user security contexts while removing ~350 lines of duplicate code.
- **Consequences**:
  Cuts significant duplicated Razor markup and diffing logic, centralizes version auditing UX, and guarantees that enhancements to version comparisons automatically benefit all host applications.

---

## Decision Record 003: Large File Refactoring Sequence & Method

- **Date**: 2026-10-18
- **Status**: Approved
- **Context**:
  Files like `WarehouseInventoryPage.razor` (1,525 lines), `MaterialOrdersPage.razor` (1,432 lines), `EnrollmentPipeline.razor` (1,264 lines), and `AccountBookPage.razor` (1,253 lines) contain mixed concerns: UI markup, complex dialogs, event handlers, client-side filtering, and service calls.
- **Decision**:
  Refactor each large page by:
  1. Keeping page routing, parameter binding, and top-level data loading in the page / `.razor.cs`.
  2. Extracting distinct modals/dialogs into dedicated components with clear `EventCallback` boundaries.
  3. Extracting complex status timelines, detail drawers, and filter bars into sub-components.
  4. Preserving exact routes, styling, and business rules without behavioral regressions.
- **Consequences**:
  Pages become readable (reducing file size from >1,000 lines down to 200–500 lines) while remaining 100% backward compatible.

---

## Decision Record 004: Standardizing on Companion Scoped `.razor.css` for Complex Pages

- **Date**: 2026-10-18
- **Status**: Approved & Implemented
- **Context**:
  Multiple major pages (`MaterialOrdersPage.razor`, `WarehouseInventoryPage.razor`, `EnrollmentPipeline.razor`, `AccountBookPage.razor`, `Memories.razor`, `MaterialsMarketplace.razor`, `MarketplaceOperationsPage.razor`) contained 300 to 680 lines of embedded `<style>` tags directly within the `.razor` markup file. This caused massive file bloat (many exceeding 1,000 lines), prevented IDE syntax specialization, and violated Blazor CSS isolation best practices.
- **Decision**:
  Migrate embedded `<style>` tags into companion `.razor.css` files (e.g. `MaterialOrdersPage.razor.css`). Blazor automatically bundles and scopes these styles at build time without runtime overhead.
- **Consequences**:
  - Immediate reduction of 300–680 lines of noise from each page file.
  - Styles are cleanly scoped to their component, preventing cross-page CSS bleeding.
  - Developers can maintain markup and styles side-by-side in modern IDEs.

---

## Decision Record 005: Modal Encapsulation with Explicit EventCallbacks

- **Date**: 2026-10-18
- **Status**: Approved & Implemented
- **Context**:
  Many pages embedded inline modal dialogs inside `@if (_isModalOpen)` blocks directly in page markup, interleaving modal DOM trees and modal-specific form state with the primary page flow.
- **Decision**:
  Extract modals into dedicated components (e.g., `AddOperationalGoalModal.razor`, `ExperienceVersionCompareModal.razor`, `ExperienceCurationSubmitModal.razor`) using explicit `[Parameter]` models and `EventCallback` / `EventCallback<T>` for actions.
- **Consequences**:
  - Page readability improves drastically.
  - Modals can be reused or refactored independently.
  - State mutation remains controlled by the parent page through typed callbacks.

---

## Decision Record 006: Cross-Portal Invoice Details & Printable Statement Component

- **Date**: 2026-10-18
- **Status**: Approved & Implemented
- **Context**:
  Both `Daycare.Web/Components/Pages/Organization/OrganizationBilling.razor` and `Admin.Web/Components/Pages/Billing/BillingPage.razor` duplicated extensive inline markup (~200 lines each) for rendering printable invoice statements, company headers, line-item breakdowns, tax computations, balances due, and payment ledger audit trails.
- **Decision**:
  1. Created [InvoiceDetailsModal.razor](file:///src/EnjoyEveryday.UI.Shared/Components/InvoiceDetailsModal/InvoiceDetailsModal.razor) (+ `.razor.css`) in `EnjoyEveryday.UI.Shared`.
  2. Supported custom action footers via `RenderFragment? FooterContent` (enabling Daycare online balance payment while preserving default close actions).
  3. Supported contextual payment row actions via `RenderFragment<Payment>? PaymentActions` (enabling Admin payment reversal actions alongside standard receipt links).
  4. Wired the unified component into both Daycare and Admin billing pages.
- **Consequences**:
  - Eliminated ~400 lines of duplicate commercial billing UI.
  - Centralized invoice layout, print styles, and ledger representation in one verified location.
  - Changes to invoice formatting or tax presentation now automatically apply across both daycare tenant and admin portals.

---

## Decision Record 007: Scoped CSS Isolation for Domain and UI.Shared Components

- **Date**: 2026-10-18
- **Status**: Approved & Implemented
- **Context**:
  Domain pages (`RhythmHistory.razor`, `StaffRoster.razor`) and shared components (`ExperienceCard.razor`, `MaterialOrderTimeline.razor`) contained large inline `<style>` tag blocks (totaling >1,000 lines), adding clutter and preventing CSS isolation benefits.
- **Decision**:
  Migrated inline `<style>` blocks into companion `.razor.css` files alongside their respective components. Converted arbitrary inline radius overrides to standard EnjoyEveryday design system variables (`var(--radius, 18px)`, `var(--accent)`, `var(--border)`).
- **Consequences**:
  - Clean separation of CSS from Razor DOM trees.
  - Razor files reduced by 40%–60% in line count.
  - Native compile-time CSS scoping eliminates global selector clashes.

