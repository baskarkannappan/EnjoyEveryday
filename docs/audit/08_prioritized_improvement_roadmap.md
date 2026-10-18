# EnjoyEveryday — Prioritized Improvement Roadmap

## 1. Roadmap Overview

The remediation plan is structured into four distinct phases prioritized by business risk:
1. **Phase 1: Immediate Security & Stability Corrections** (Sprint 1)
2. **Phase 2: Build, Test Suite & Entry Point Alignment** (Sprint 2)
3. **Phase 3: Architecture, Modularity & Performance Hardening** (Sprints 3–4)
4. **Phase 4: Ongoing Engineering & Operational Excellence** (Continuous)

---

## 2. Phase 1: Immediate Security & Stability Corrections (Sprint 1)

| Task ID | Action / Deliverable | Category | Effort | Risk | Prerequisites | Acceptance Criteria |
|:---:|:---|:---:|:---:|:---:|:---:|:---|
| **P1-1** | **Fix Host Header Bypass in `AppIdentityContext.cs`** | Security | Small | Low | None | Replace `Request.Host.Host` check with `IHostEnvironment.IsDevelopment()`. Requests with `Host: localhost` in production environments are strictly unauthenticated. |
| **P1-2** | **Remove Hardcoded Admin Email Backdoor** | Security | Small | Low | None | Remove `admin@littlestars.com` string checks from `AppIdentityContext.cs`. Role permissions must originate strictly from database tables. |
| **P1-3** | **Rotate and Evict Secrets from `appsettings.json`** | Security | Small | Low | None | Rotate Razorpay keys and webhook secrets in Razorpay dashboard. Replace keys in `appsettings.json` with empty strings or environment variable placeholders. |
| **P1-4** | **Enforce Authorization on `/account-book`** | Security | Small | Low | None | Add `@attribute [Authorize(Roles = "PlatformAdmin")]` to `AccountBookPage.razor`. Enforce tenant ID verification in `AccountBookService.cs`. |
| **P1-5** | **Remove Leaked SQL from `ActiveSessionModal.razor`** | Architecture | Small | Low | None | Remove `IDbConnectionFactory` and `@using Dapper` from `ActiveSessionModal.razor`. Delegate queries to `IAttendanceService` with strict tenant scoping. |

---

## 3. Phase 2: Build, Test Suite & Entry Point Alignment (Sprint 2)

| Task ID | Action / Deliverable | Category | Effort | Risk | Prerequisites | Acceptance Criteria |
|:---:|:---|:---:|:---:|:---:|:---:|:---|
| **P2-1** | **Fix 9 Compilation Errors in Test Suite** | Testing | Medium | Low | None | Update `MockChildDailyCareRepository` to implement `TeacherHandover` methods; update `TestRazorpayGateway` to implement `RazorpayGatewayOrderResult` and `Settings`. |
| **P2-2** | **Include Test Projects in Solution File** | Testing | Small | Low | P2-1 | Add `EnjoyEveryday.Application.Tests.csproj` and `EnjoyEveryday.MemoryLayer.Tests.csproj` to `EnjoyEveryday.slnx`. `dotnet build EnjoyEveryday.slnx` builds both code and tests. |
| **P2-3** | **Decommission or Repair `EnjoyEveryday.Api`** | Architecture | Medium | Low | None | Decide whether to repair `EnjoyEveryday.Api` (register services and build controllers) or remove it from the solution to eliminate developer confusion. Update `run.ps1` accordingly. |
| **P2-4** | **Fix AI Service HttpClient Configuration** | Reliability | Small | Low | None | In `ApplicationServiceRegistration.cs`, configure `BaseAddress = new Uri("http://127.0.0.1:8000")` for all AI assistant HTTP clients. Update `run.ps1` to start the Python agent. |
| **P2-5** | **Establish GitHub Actions CI Workflow** | CI/CD | Small | Low | P2-2 | Add `.github/workflows/ci.yml` to build the solution, run static analysis, and verify clean compilation on every push and pull request. |

---

## 4. Phase 3: Architecture, Modularity & Performance Hardening (Sprints 3–4)

| Task ID | Action / Deliverable | Category | Effort | Risk | Prerequisites | Acceptance Criteria |
|:---:|:---|:---:|:---:|:---:|:---:|:---|
| **P3-1** | **Eliminate 20 Duplicate Pages Between Web Hosts** | Modularity | Large | Medium | None | Refactor duplicate pages (`ExperienceStudio`, `Experiences`, `Children`, `Teachers`, etc.) into shared components in `EnjoyEveryday.UI.Shared`. Delete duplicate `.razor` files in `Admin.Web`. |
| **P3-2** | **Extract Direct SQL into Infrastructure Repositories** | Architecture | Large | Medium | None | Migrate raw Dapper queries out of the 10 Application services into dedicated repositories in `EnjoyEveryday.Infrastructure`. Remove `Dapper` package reference from `Application.csproj`. |
| **P3-3** | **Fix N+1 Query Bottleneck in `BillingRepository`** | Performance | Medium | Low | None | Rewrite `GetInvoicesForOrganizationAsync` and `GetAllInvoicesAsync` to retrieve line items and payments via SQL `JOIN` or Dapper `QueryMultipleAsync` in a single database roundtrip. |
| **P3-4** | **Extract Massive Razor Components into Code-Behind** | Maintainability | Large | Low | None | Decompose 12 largest Razor components (>900 lines) into `.razor` markup and `.razor.cs` code-behind classes. Extract inline `<style>` tags into `.razor.css` scoped styles. |
| **P3-5** | **Cloud-Ready Document Storage Provider** | Reliability | Medium | Low | None | Introduce `IBlobStorageService` abstraction. Implement LocalDisk for development and Azure Blob / AWS S3 for production to eliminate ephemeral storage data loss. |

---

## 5. Phase 4: Ongoing Engineering & Operational Excellence

| Task ID | Action / Deliverable | Category | Effort | Ongoing Cadence |
|:---:|:---|:---:|:---:|:---:|
| **P4-1** | **Repository Root Hygiene**: Delete 25+ orphaned Python patch scripts from repository root; commit clean documentation. | Cleanup | Small | Once |
| **P4-2** | **Observability & Diagnostics**: Add ASP.NET Core `/health` and `/healthz` endpoints; introduce ambient correlation ID middleware. | Observability | Medium | Sprint 5 |
| **P4-3** | **PostgreSQL Testcontainers**: Author automated integration test suite running against real PostgreSQL containers. | Testing | Large | Sprint 5 |
| **P4-4** | **Database Migration Rollback Practice**: Mandate reversible migrations (`down.sql`) for all future database schema modifications. | Database | Ongoing | Per PR |
