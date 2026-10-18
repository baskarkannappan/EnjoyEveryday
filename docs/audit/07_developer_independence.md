# EnjoyEveryday — Developer Independence Assessment

## 1. Developer Independence Verdict

> **Current Verdict: FAIL — HIGH RISK OF DEVELOPER LOCK-IN**

A competent software engineer joining the EnjoyEveryday project today without access to the original developer or an AI assistant cannot reliably build, test, troubleshoot, or deploy the system independently.

---

## 2. Onboarding Scenario Evaluation

We evaluated the codebase against **14 standard developer onboarding scenarios**:

| # | Scenario | Status | Friction Points & Observed Reality |
|:---:|:---|:---:|:---|
| **1** | **Set up the development environment** | **FAIL** | No root `README.md`. No list of required SDKs (.NET 10.0 preview, Python 3.11+, PostgreSQL 15, Docker). |
| **2** | **Configure local settings safely** | **FAIL** | Secrets (Razorpay keys) are committed in plain text in `appsettings.json`. No template (`appsettings.example.json`). |
| **3** | **Create or migrate local database** | **PARTIAL** | `docker-compose.yml` starts PostgreSQL, and migrations run automatically on startup in Development. However, no standalone migration CLI exists, and startup migration errors are swallowed as warnings. |
| **4** | **Build the solution** | **PARTIAL** | `dotnet build EnjoyEveryday.slnx` succeeds with 8 warnings, but creates false confidence because test projects are excluded. |
| **5** | **Run the application** | **FAIL** | `run.ps1` starts `EnjoyEveryday.Api` (which immediately fails on request) and `Daycare.Web`. It does not start the Python AI agent. |
| **6** | **Understand architecture & boundaries** | **FAIL** | Confusing duality: docs describe Clean Architecture, but 10 services run raw SQL in Application; `Api` is abandoned while Web apps act as monoliths. |
| **7** | **Trace a user action end-to-end** | **PARTIAL** | Can trace from Blazor `.razor` file into service, but tracing stops or diverges when encountering duplicate pages or direct SQL. |
| **8** | **Add a small feature following conventions** | **FAIL** | Developer does not know whether to add a repository or write Dapper SQL in a service; adding a page requires duplicating it between `Daycare.Web` and `Admin.Web`. |
| **9** | **Add and run automated tests** | **FAIL** | Running `dotnet test` on the solution runs zero tests (tests not in `.slnx`). Building the test project directly fails with 9 compilation errors. |
| **10** | **Diagnose a failed request or job** | **FAIL** | No correlation IDs. Swallowed exceptions with `Console.WriteLine`. No `/health` checks. |
| **11** | **Make a safe database migration** | **PARTIAL** | Writing a `.sql` file in `database/migrations/` works, but there are zero down-migrations and no automated schema verification. |
| **12** | **Diagnose tenant authorization failure** | **PARTIAL** | `ResourceAuthorizer` produces audit logs, but `AppIdentityContext` bypasses them if Host header is `localhost`. |
| **13** | **Deploy to a non-production environment** | **FAIL** | No Dockerfile for web apps, no Helm charts, empty `.github/workflows` (no CI/CD). |
| **14** | **Roll back a failed deployment / migration** | **FAIL** | Zero rollback procedures or down-migrations documented or implemented. |

**Summary**: 2 Partial, 12 Fail, 0 Complete.

---

## 3. Undocumented Implicit Knowledge

The following critical system facts exist only in the codebase or previous developer memory, with no documentation:
1. **The API Project is Dead**: `EnjoyEveryday.Api` is non-functional and not used by the web applications.
2. **Web Frontends are In-Process Monoliths**: `Daycare.Web`, `Admin.Web`, and `Parent.Web` each host application and database services directly.
3. **The Test Suite is Broken and Hidden**: `EnjoyEveryday.Application.Tests` was removed from the solution file rather than fixed when interfaces changed.
4. **AI Services Require a Python Sidecar**: The AI recommendations depend on a FastAPI application in `src/EnjoyEveryday.AiAgent` that is not launched by `run.ps1`.
5. **Admin Email Grants Superuser**: The email `admin@littlestars.com` is hardcoded to receive all permissions unconditionally.

---

## 4. Minimum Documentation Set Required

To enable independent human maintenance, the repository must introduce:

1. **Root `README.md`**:
   - High-level architecture explanation (Blazor Server in-process monoliths + Python AI sidecar).
   - Prerequisites (.NET 10 SDK, Docker, Python 3.11).
   - 3-step quickstart command list to run the database, Python agent, and web applications.
2. **Environment & Secrets Template (`appsettings.example.json`)**:
   - Clean template showing required connection strings, Google OAuth IDs, and Razorpay test placeholders without hardcoded credentials.
3. **Architecture Decision Records (ADRs)**:
   - ADR 001: Decommissioning `EnjoyEveryday.Api` or transitioning to an API-first model.
   - ADR 002: Service Layer Data Access Conventions (banning direct SQL in Application).
4. **Database Migration & Rollback Guide**:
   - Rules for numbering migrations, authoring down-migrations, and testing against clean database instances.
