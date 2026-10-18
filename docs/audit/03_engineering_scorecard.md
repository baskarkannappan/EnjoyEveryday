# EnjoyEveryday — Engineering Scorecard

## 1. Scorecard Summary & Weighted Result

| Dimension | Weight | Score (0–100) | Weighted Points | Confidence Level | Remediation Effort |
|:---|---:|---:|---:|:---:|:---:|
| **1. Architecture and separation of concerns** | 15% | 52 | 7.80 | High | Large |
| **2. Modularity and dependency management** | 12% | 48 | 5.76 | High | Medium |
| **3. Code maintainability and readability** | 12% | 50 | 6.00 | High | Large |
| **4. Reusability and consistency** | 8% | 44 | 3.52 | High | Medium |
| **5. Testability and automated testing** | 12% | 38 | 4.56 | High | Large |
| **6. Security and authorization** | 12% | 42 | 5.04 | High | Medium |
| **7. Database design and data integrity** | 8% | 62 | 4.96 | High | Medium |
| **8. Reliability and error handling** | 7% | 46 | 3.22 | High | Medium |
| **9. Performance and scalability** | 6% | 52 | 3.12 | High | Medium |
| **10. Developer onboarding and documentation** | 5% | 36 | 1.80 | High | Small |
| **11. Build, deployment and operational readiness** | 3% | 28 | 0.84 | High | Medium |
| **Total Weighted Score** | **100%** | **46.6** | **46.62 / 100** | **High** | **Large** |

> **Final Weighted Score: 47 / 100**  
> **Grade**: **Needs Significant Structural Remediation**  
> **Critical Risk Overrides Triggered**: **YES** (Score capped; production deployment disallowed until critical security and testability defects are resolved).

---

## 2. Evaluation Rubric Definitions

| Score | Meaning & Criteria |
|:---:|:---|
| **0** | Missing or non-functional. Pervasive design flaws, complete lack of safeguards or documentation. |
| **25** | Primitive or severely broken. High bug density, architectural violations throughout, active security liabilities. |
| **50** | Partially implemented with substantial inconsistencies. Core features work locally, but significant technical debt, duplication, or boundary leaks impede maintenance. |
| **75** | Professional and robust. Consistent patterns, strong test coverage, solid boundaries, clear documentation, few minor gaps. |
| **100** | Exceptional, reference-grade engineering. Fault-tolerant, self-healing, fully automated CI/CD, comprehensive observability, zero known vulnerabilities. |

---

## 3. Detailed Dimension Scorecards

### Dimension 1: Architecture and Separation of Concerns (Weight: 15%)
- **Score**: **52 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `EnjoyEveryday.Application.csproj` contains `<PackageReference Include="Dapper" />`.
  - 10 application services inject `IDbConnectionFactory` and write inline SQL queries (`StaffRosterService.cs`, `DaycareOperationsBoardService.cs`, `DashboardService.cs`, `BranchBenchmarkingService.cs`, `MemoryService.cs`, `ExecutionService.cs`, `ChildDevelopmentPortfolioService.cs`, `EnrollmentPipelineService.cs`, `SecurePickupService.cs`, `PlatformAdminService.cs`).
  - Presentation component `ActiveSessionModal.razor` injects `IDbConnectionFactory`, uses Dapper, and queries the database directly.
  - `EnjoyEveryday.Api/Program.cs` registers no application or infrastructure dependencies, rendering it non-functional.
  - Web applications act as monolithic host servers rather than consuming a separated backend.
- **Positive Findings**: Clear directory layout, strong domain POCOs in `EnjoyEveryday.Domain`, excellent abstractions in `IResourceAuthorizer` and `IAccessScopeRepository`.
- **Major Weaknesses**: Clean architecture boundaries violated; database drivers leak into Application and UI layers.
- **Recommended Improvements**: Remove Dapper from `EnjoyEveryday.Application`; extract all raw SQL into repositories in `Infrastructure`; repair or decommission `EnjoyEveryday.Api`.
- **Estimated Effort**: Large | **Dependencies**: None.

---

### Dimension 2: Modularity and Dependency Management (Weight: 12%)
- **Score**: **48 / 100** | **Confidence**: High
- **Observed Evidence**:
  - 20 entire pages are duplicate files between `Daycare.Web` and `Admin.Web`.
  - `ActivityRecommendationService` constructor has 11 dependencies.
  - `ActiveSessionModal.razor` has 10 injected dependencies.
  - `ApplicationServiceRegistration.cs` registers 15+ services twice (`AddScoped<IService, Service>()` AND `AddScoped<Service>()`) due to inconsistent injection patterns.
- **Positive Findings**: Project dependencies flow without circular references.
- **Major Weaknesses**: High fan-in/fan-out on central services; page duplication across web hosts.
- **Recommended Improvements**: Move common page components to `EnjoyEveryday.UI.Shared`; standardize on interface-only service injection.
- **Estimated Effort**: Medium | **Dependencies**: Architecture cleanup.

---

### Dimension 3: Code Maintainability and Readability (Weight: 12%)
- **Score**: **50 / 100** | **Confidence**: High
- **Observed Evidence**:
  - 12 Razor components exceed 900 lines (e.g., `WarehouseInventoryPage.razor`: 1,418 lines; `MaterialOrdersPage.razor`: 1,313 lines).
  - 0 `.razor.cs` code-behind files exist across 265 Razor components.
  - 25+ Python patch scripts (`patch_attendance.py`, `fix_all.py`, etc.) clutter the root directory.
  - `MaterialMarketplaceService.cs` is 1,510 lines long.
- **Positive Findings**: Modern C# syntax; consistent naming conventions; design system followed.
- **Major Weaknesses**: Monolithic Razor files mixing HTML, styling, event handling, and data mapping; repository root clutter.
- **Recommended Improvements**: Separate large Razor components into `.razor` and `.razor.cs` files; decompose massive views into sub-components; remove root Python scripts.
- **Estimated Effort**: Large | **Dependencies**: None.

---

### Dimension 4: Reusability and Consistency (Weight: 8%)
- **Score**: **44 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `AiIdeaService` and `ActivityRecommendationService` hardcode `http://127.0.0.1:8000`, while other AI services use relative paths without a configured `BaseAddress`.
  - In `AuthEndpoints.cs`, 350 lines of OAuth and cookie code are duplicated across `Daycare.Web`, `Admin.Web`, and `Parent.Web`.
  - Inline `<style>` blocks (250+ lines) embedded directly inside `AccountBookPage.razor` and `ActiveSessionModal.razor`.
- **Positive Findings**: Shared design tokens and Scandinavian palette in `GEMINI.md`; shared UI primitives in `UI.Shared`.
- **Major Weaknesses**: Inconsistent HTTP client configuration; copy-pasted authentication endpoints.
- **Recommended Improvements**: Unify authentication endpoints in a shared assembly; configure AI agent `BaseAddress` in `appsettings.json`.
- **Estimated Effort**: Medium | **Dependencies**: None.

---

### Dimension 5: Testability and Automated Testing (Weight: 12%)
- **Score**: **38 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `EnjoyEveryday.slnx` deliberately omits `EnjoyEveryday.Application.Tests` and `EnjoyEveryday.MemoryLayer.Tests`.
  - Compiling `EnjoyEveryday.Application.Tests` produces **9 compilation errors** (broken mocks in `ChildDailyCareTests.cs` and `RazorpayAndCashPaymentLedgerTests.cs`).
  - Zero integration tests against PostgreSQL.
  - Zero UI tests (no bUnit, Playwright, or E2E tests).
  - Services using direct `IDbConnectionFactory` cannot be tested without mocking raw Dapper calls or connecting to a live database.
- **Positive Findings**: 128 NUnit test cases exist covering role templates and security assertions.
- **Major Weaknesses**: Broken compilation; exclusion from solution; zero integration or presentation testing.
- **Recommended Improvements**: Fix 9 compilation errors; add test projects to `EnjoyEveryday.slnx`; introduce PostgreSQL testcontainers for integration tests.
- **Estimated Effort**: Large | **Dependencies**: Architecture cleanup.

---

### Dimension 6: Security and Authorization (Weight: 12%)
- **Score**: **42 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `AppIdentityContext.IsDevelopment()` treats any request with `Host: localhost` or `127.0.0.1` as authenticated admin (`Permissions.All`).
  - Hardcoded email `admin@littlestars.com` automatically bypasses all permission checks in `AppIdentityContext.HasPermission`.
  - Plaintext Razorpay keys and webhook secrets committed in `appsettings.json`.
  - `AccountBookPage.razor` lacks authorization attributes and queries transactions across all tenants.
  - `ActiveSessionModal.razor` queries attendance without tenant isolation parameters.
  - Reflection-based `GetAwaiter().GetResult()` in `AppIdentityContext.ResolveUser()` risks deadlocks.
- **Positive Findings**: `ResourceAuthorizer` provides fine-grained multi-tenant evaluation; Razorpay webhook HMAC verification and idempotency implemented.
- **Major Weaknesses**: Critical dev bypass vulnerability; hardcoded admin overrides; exposed secrets.
- **Recommended Improvements**: Replace Host header inspection with ASP.NET Core environment checks; remove hardcoded admin email; move secrets to environment variables; enforce `[Authorize]` across all pages.
- **Estimated Effort**: Medium | **Dependencies**: None (Immediate priority).

---

### Dimension 7: Database Design and Data Integrity (Weight: 8%)
- **Score**: **62 / 100** | **Confidence**: High
- **Observed Evidence**:
  - 132 relational tables covering daycare operations.
  - Composite foreign keys on `organization_memberships` enforce tenant scoping.
  - `SqlMigrationRunner` uses `pg_advisory_lock` for safe concurrency.
  - Global unique index on `users.normalized_email` prevents multi-tenant email reuse.
  - No down-migrations exist for any of the 62 migration scripts.
  - Migrations run only on Development startup; no production migration pipeline.
- **Positive Findings**: Strong relational modeling and constraint enforcement in foundation tables.
- **Major Weaknesses**: Lack of rollback scripts; global email constraint; no CI migration runner.
- **Recommended Improvements**: Implement standalone migration CLI tool; adjust user email constraint to `(tenant_id, normalized_email)` where appropriate; author down-migrations for critical schemas.
- **Estimated Effort**: Medium | **Dependencies**: None.

---

### Dimension 8: Reliability and Error Handling (Weight: 7%)
- **Score**: **46 / 100** | **Confidence**: High
- **Observed Evidence**:
  - Swallowed exceptions with `Console.WriteLine` in `AccountBookService`, `ActivityRecommendationService`, and `AccountBookPage.razor`.
  - Relative URLs in AI services cause guaranteed runtime exceptions that are silently swallowed.
  - Startup database migration failures are logged as warnings and ignored.
- **Positive Findings**: Deterministic fallbacks for AI; transaction management in payment workflows.
- **Major Weaknesses**: Unstructured logging; silent exception swallowing hiding system defects.
- **Recommended Improvements**: Replace `Console.WriteLine` with `ILogger`; configure proper HTTP client base addresses; fail startup on migration errors.
- **Estimated Effort**: Medium | **Dependencies**: None.

---

### Dimension 9: Performance and Scalability (Weight: 6%)
- **Score**: **52 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `BillingRepository` has an N+1 query pattern (2,001 queries for 1,000 invoices).
  - `ExperienceService.GetExperiencesAsync()` loads all records into memory and filters with LINQ.
  - Zero caching layer across all projects.
  - Floor map storage writes to local ephemeral disk.
- **Positive Findings**: SignalR message buffer increased to 10MB; indexes present on foreign keys.
- **Major Weaknesses**: N+1 queries; unbounded in-memory collection processing; ephemeral disk dependency.
- **Recommended Improvements**: Rewrite invoice retrieval to use SQL `JOIN` or Dapper `QueryMultiple`; introduce `IMemoryCache` for tenant settings and permissions; abstract file storage to S3/Azure Blob.
- **Estimated Effort**: Medium | **Dependencies**: None.

---

### Dimension 10: Developer Onboarding and Documentation (Weight: 5%)
- **Score**: **36 / 100** | **Confidence**: High
- **Observed Evidence**:
  - No root `README.md`.
  - No local development setup instructions.
  - `run.ps1` runs non-functional `EnjoyEveryday.Api` and omits `EnjoyEveryday.AiAgent`.
  - No API documentation or architectural decision records.
- **Positive Findings**: Clear design system guide in `GEMINI.md`; detailed implementation notes in `PROJECT_STATE.md`.
- **Major Weaknesses**: Missing basic developer onboarding files; confusing root directory layout.
- **Recommended Improvements**: Author master `README.md`; fix `run.ps1`; clean up scratch scripts; document architecture.
- **Estimated Effort**: Small | **Dependencies**: None.

---

### Dimension 11: Build, Deployment and Operational Readiness (Weight: 3%)
- **Score**: **28 / 100** | **Confidence**: High
- **Observed Evidence**:
  - `.github/workflows` is empty.
  - Test projects excluded from `EnjoyEveryday.slnx`.
  - Zero health check endpoints.
  - No ambient correlation ID middleware.
  - No production deployment configuration.
- **Positive Findings**: Solution builds cleanly; Docker Compose provides local PostgreSQL.
- **Major Weaknesses**: No CI/CD automation; no health checks; zero observability infrastructure.
- **Recommended Improvements**: Create GitHub Actions CI workflow; add `/health` endpoints; implement correlation ID middleware.
- **Estimated Effort**: Medium | **Dependencies**: Test project fixes.

---

## 4. Critical Risk Overrides

Regardless of individual scores, the following **5 Critical Defects** prohibit production readiness:

1. **Security Vulnerability CRIT-01**: Host Header spoofing allows complete administrative bypass in `AppIdentityContext.cs`.
2. **Security Vulnerability CRIT-02**: Hardcoded admin email `admin@littlestars.com` unconditionally grants all permissions.
3. **Security Vulnerability CRIT-03**: Plaintext Razorpay payment keys and webhook secrets committed in `appsettings.json`.
4. **Architecture Defect CRIT-04**: `EnjoyEveryday.Api` is non-functional and throws unhandled DI exceptions on every request.
5. **Quality Defect CRIT-05**: Test suite is excluded from the solution and broken with 9 compilation errors.
