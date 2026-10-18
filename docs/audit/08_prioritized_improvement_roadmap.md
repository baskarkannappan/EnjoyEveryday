# EnjoyEveryday — Prioritized Improvement Roadmap & Remediation Status

## 1. Roadmap Overview

The remediation plan is structured into four distinct phases prioritized by business risk:
1. **Phase 1: Immediate Security & Stability Corrections** (Sprint 1) — **100% COMPLETE**
2. **Phase 2: Build, Test Suite & Entry Point Alignment** (Sprint 2) — **100% COMPLETE**
3. **Phase 3: Architecture, Modularity & Performance Hardening** (Sprints 3–4) — **100% COMPLETE**
4. **Phase 4: Ongoing Engineering & Operational Excellence** (Continuous) — **100% COMPLETE**

---

## 2. Phase 1: Immediate Security & Stability Corrections (Sprint 1) — COMPLETE

| Task ID | Action / Deliverable | Category | Status | Acceptance Criteria & Verification |
|:---:|:---|:---:|:---:|:---|
| **P1-1** | **Fix Host Header Bypass in `AppIdentityContext.cs`** | Security | **DONE** | Replaced `Request.Host.Host` check with `IHostEnvironment.IsDevelopment()`. Requests with `Host: localhost` in production environments are strictly unauthenticated. Verified by unit tests. |
| **P1-2** | **Remove Hardcoded Admin Email Backdoor** | Security | **DONE** | Removed `admin@littlestars.com` string checks from `AppIdentityContext.cs`. Role permissions originate strictly from database tables. |
| **P1-3** | **Rotate and Evict Secrets from `appsettings.json`** | Security | **DONE** | Evicted live Razorpay keys and webhook secrets from `appsettings.json`. Replaced with empty environment variable placeholders and User Secrets. |
| **P1-4** | **Enforce Authorization on `/account-book`** | Security | **DONE** | Added `@attribute [Authorize(Roles = "PlatformAdmin")]` to `AccountBookPage.razor`. Enforced tenant ID verification in `AccountBookService.cs`. |
| **P1-5** | **Remove Leaked SQL from `ActiveSessionModal.razor`** | Architecture | **DONE** | Removed `IDbConnectionFactory` and `@using Dapper` from `ActiveSessionModal.razor`. Delegated queries to `IAttendanceService` with strict tenant scoping. |

---

## 3. Phase 2: Build, Test Suite & Entry Point Alignment (Sprint 2) — COMPLETE

| Task ID | Action / Deliverable | Category | Status | Acceptance Criteria & Verification |
|:---:|:---|:---:|:---:|:---|
| **P2-1** | **Fix 9 Compilation Errors in Test Suite** | Testing | **DONE** | Updated `MockChildDailyCareRepository` to implement `TeacherHandover` methods; updated `TestRazorpayGateway` to implement `RazorpayGatewayOrderResult` and `Settings`. |
| **P2-2** | **Include Test Projects in Solution File** | Testing | **DONE** | Added `EnjoyEveryday.Application.Tests.csproj` and `EnjoyEveryday.MemoryLayer.Tests.csproj` to `EnjoyEveryday.slnx`. `dotnet build EnjoyEveryday.slnx` builds all code and tests with 0 errors. |
| **P2-3** | **Decommission or Repair `EnjoyEveryday.Api`** | Architecture | **DONE** | Repaired `EnjoyEveryday.Api` program entry point, registered application and infrastructure services, and enabled clean build. |
| **P2-4** | **Fix AI Service HttpClient Configuration** | Reliability | **DONE** | In `ApplicationServiceRegistration.cs`, configured 15-second bounded timeouts for all AI assistant HTTP clients. Updated `run.ps1` to launch Python agent. |
| **P2-5** | **Establish GitHub Actions CI Workflow** | CI/CD | **DONE** | Added `.github/workflows/ci.yml` to build the solution and verify clean compilation on every push and pull request. |

---

## 4. Phase 3: Architecture, Modularity & Performance Hardening (Sprints 3–4) — COMPLETE

| Task ID | Action / Deliverable | Category | Status | Acceptance Criteria & Verification |
|:---:|:---|:---:|:---:|:---|
| **P3-1** | **Eliminate Duplicate Pages Between Web Hosts** | Modularity | **DONE** | Refactored diverged views into reusable shared components in `EnjoyEveryday.UI.Shared`: `ChildrenDirectoryView`, `TeachersDirectoryView`, `HarvestView`, `SelectOrganizationView`, `ChildJourneyView`, and `ClassroomsDirectoryView`. Deleted duplicated `.razor` implementations. |
| **P3-2** | **Extract Direct SQL into Infrastructure Repositories** | Architecture | **DONE** | Migrated all raw SQL queries from 10+ Application services into dedicated repositories in `EnjoyEveryday.Infrastructure`. Removed Dapper package reference from `EnjoyEveryday.Application.csproj`. Enforced Clean Architecture repository boundaries. |
| **P3-3** | **Fix N+1 Query Bottleneck in `BillingRepository`** | Performance | **DONE** | Rewrote `GetInvoicesForOrganizationAsync` and `GetAllInvoicesAsync` to retrieve line items and payments in bulk via single round-trip `invoice_id = ANY(@InvoiceIds)` queries. |
| **P3-4** | **Extract Massive Razor Components into Code-Behind** | Maintainability | **DONE** | Decomposed 9 largest Razor components (>900 lines) into `.razor` markup, `.razor.cs` code-behind classes, and `.razor.css` scoped styles (`WarehouseInventoryPage`, `MaterialOrdersPage`, `BillingPage`, `AccountBookPage`, `EnrollmentPipeline`, `ChildDailyCareSection`, `Memories`, `MarketplaceOperationsPage`, `ExperiencePlanning`). |
| **P3-5** | **Cloud-Ready Document Storage Provider** | Reliability | **DONE** | Introduced `IBlobStorageService` abstraction in `EnjoyEveryday.Domain.Storage`. Implemented `LocalDiskBlobStorageService` in `EnjoyEveryday.Infrastructure.Storage` with container scoping, checksum metadata, and directory virtualization. |

---

## 5. Phase 4: Ongoing Engineering & Operational Excellence

| Task ID | Action / Deliverable | Category | Status | Acceptance Criteria & Verification |
|:---:|:---|:---:|:---:|:---|
| **P4-1** | **Repository Root Hygiene** | Cleanup | **DONE** | Deleted 27 orphaned Python patch scripts from repository root; committed clean documentation and solution layout. |
| **P4-2** | **Observability & Diagnostics** | Observability | **DONE** | Added `CorrelationIdMiddleware` for ambient `X-Correlation-Id` propagation and logging enrichment. Added `/healthz` (liveness) and `/health` (readiness with database verification) across `Api`, `Daycare.Web`, `Admin.Web`, and `Parent.Web`. |
| **P4-3** | **PostgreSQL Testcontainers** | Testing | **DONE** | Established `EnjoyEveryday.IntegrationTests` project in solution using `Testcontainers.PostgreSql`. Authored `PostgresTestcontainerFixture` and `DatabaseMigrationIntegrationTests` verifying automated PostgreSQL container lifecycle and migration schema application. |
| **P4-4** | **Database Migration Rollback Practice** | Database | **DONE** | Updated `SqlMigrationRunner` to support atomic `RollbackLastMigrationAsync` and automatically exclude `*.down.sql` scripts from forward migrations. Authored reversible `*.down.sql` scripts for migrations 052–058 and established test coverage. |
