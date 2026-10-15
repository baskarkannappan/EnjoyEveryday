# EnjoyEveryday — Platform Admin Implementation Record

## Master Status Table

| Phase | Scope | Status | Evidence |
|---|---|---|---|
| **1** | Component modularization and folder organization | **Completed** | Shared UI components extracted into `EnjoyEveryday.UI.Shared`, feature folders organized in `EnjoyEveryday.Admin.Web`, navigation updated |
| **2** | Platform Admin dashboard & brand-compliant UI | **Completed** | `PlatformDashboard.razor`, `LifecyclePipeline.razor`, `MetricCard.razor`, and operational telemetry implemented with Scandinavian theme |
| **3** | Organizations, plans, tariffs, and billing controls | **In Progress** | Domain entities (`SubscriptionPlan`, `Invoice`, `Payment`, `PlatformAuditLog`) & service endpoints created; dedicated feature pages scaffolded |
| **4** | AI Parent Communication Assistant | **Pending** | Pending Phase 3 completion |
| **5** | Organization Intelligence | **Pending** | Pending Phase 4 completion |
| **6** | Final verification, security & production readiness | **Pending** | Full verification & regression tests |

---

## 1. Repository Architecture & Web Application Split

- **`EnjoyEveryday.Admin.Web`**: Dedicated **Platform Administration** web application for SaaS super-administrators (multi-tenant management, tariffs, subscription plans, billing, payment verification, and audit logs).
- **`EnjoyEveryday.Daycare.Web`**: Daycare center operations for directors, administrators, and teachers (classrooms, attendance, daily care, experience studio, curriculum planning).
- **`EnjoyEveryday.Parent.Web`**: Family engagement portal for parents and guardians (stories, Child Journey, communications).
- **`EnjoyEveryday.UI.Shared`**: External shared Razor class library containing reusable UI components, brand design tokens, and layout primitives.
- **`EnjoyEveryday.Domain`**: Core domain entities and repository contracts.
- **`EnjoyEveryday.Application`**: Business logic, workflow orchestration, AI services, and `IPlatformAdminService`.
- **`EnjoyEveryday.Infrastructure`**: PostgreSQL/Npgsql data access, Dapper repositories, encryption, and audit storage.

---

## 2. Shared Component Catalog (`EnjoyEveryday.UI.Shared`)

| Component | Path | Responsibility |
|---|---|---|
| `StatusBadge` | `Components/StatusBadge/` | Brand-compliant status pill with color tokens for both Account and Financial states |
| `LifecyclePipeline` | `Components/LifecyclePipeline/` | 5-stage interactive stage-gate pipeline for daycare onboarding and activation |
| `MetricCard` | `Components/MetricCard.razor` | KPI card with support for drill-down actions, attention badges, and trend indicators |
| `PageHero` | `Components/PageHero.razor` | Standardized header banner with eyebrow, title, and quick action buttons |
| `SearchFilterRow` | `Components/SearchFilterRow/` | Search bar with integrated clear button and slot for filter dropdowns |
| `FilterTabs` | `Components/FilterTabs/` | Tab bar supporting active pill styles and item count badges |
| `LoadingState` | `Components/LoadingState.razor` | Standardized spinner and message feedback |
| `EmptyState` | `Components/EmptyState.razor` | Friendly empty message with action button |

---

## 3. Platform Admin Folder Organization (`EnjoyEveryday.Admin.Web`)

```text
EnjoyEveryday.Admin.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor (Updated for Platform Admin hierarchy)
│   │   └── ReconnectModal.razor
│   ├── Pages/
│   │   ├── Dashboard/
│   │   │   ├── PlatformDashboard.razor (Main Platform Admin Dashboard)
│   │   │   └── PlatformDashboard.razor.css
│   │   ├── Organizations/
│   │   │   ├── OrganizationsPage.razor
│   │   │   ├── BranchesPage.razor
│   │   │   └── AdminsPage.razor
│   │   ├── Billing/
│   │   │   ├── PlansPage.razor
│   │   │   ├── BillingPage.razor
│   │   │   ├── PaymentsPage.razor
│   │   │   └── DiscountsPage.razor
│   │   └── Operations/
│   │       ├── ReportsPage.razor
│   │       ├── AuditLogsPage.razor
│   │       └── SettingsPage.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
```

---

## 4. Brand Compliance & Aesthetics (`GEMINI.md`)

- **Color Palette Enforced**:
  - Background: `#f5f5ef`
  - Card Surface: `#ffffff`
  - Subtle background: `#f8f9f5`
  - Border: `#e4e7df`
  - Signature Sage Accent: `#557c6c` (active), `#3e6253` (dark hover)
  - Warm Sand: `#f4eee1`
  - Forest Slate: `#26332d`
- **Strictly Avoided**: Cold SaaS blues (`#2563eb`), dark mode, rainbow pastels, and ad-hoc bootstrap tables.
- **Elevation & Radius**: `18px` border radius and `0 8px 30px rgba(40, 55, 47, .06)` soft natural shadow.

---

## 5. Security & Multi-Tenant Isolation Controls

1. **Independent Account vs Financial Status**: Organizations cannot be activated merely by paying an invoice; payment and account status transitions are independently validated.
2. **Payment Verification Rule**: Payments require manual administrative verification or audited webhook callbacks before clearing invoices.
3. **Audit Trail**: Sensitive actions (`Organization Created`, `Account Status Changed`, `Payment Reconciled`, `Invoice Issued`) append immutable log records into `PlatformAuditLog`.
