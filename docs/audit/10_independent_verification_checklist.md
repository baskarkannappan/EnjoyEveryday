# EnjoyEveryday — Independent Verification Checklist

This checklist provides a reproducible, step-by-step verification procedure that any engineer or auditor can execute to independently validate the findings in this audit report.

---

## 1. Environment & Solution Build Verification

- [ ] **Verify Solution Projects**:
  Open [EnjoyEveryday.slnx](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/EnjoyEveryday.slnx). Confirm it lists 9 projects and that neither `EnjoyEveryday.Application.Tests` nor `EnjoyEveryday.MemoryLayer.Tests` is included.
- [ ] **Run Solution Build**:
  Execute:
  ```powershell
  dotnet build EnjoyEveryday.slnx
  ```
  Confirm the build succeeds with **0 errors and 8 compiler warnings** (all nullability warnings in `AuthEndpoints.cs`).
- [ ] **Verify Test Project Build Failure**:
  Execute:
  ```powershell
  dotnet build src/EnjoyEveryday.Application.Tests/EnjoyEveryday.Application.Tests.csproj
  ```
  Confirm the build **fails with 9 errors** (missing implementations of `TeacherHandover` in `MockChildDailyCareRepository` and `RazorpayGatewayOrderResult`/`Settings` in `TestRazorpayGateway`).

---

## 2. Security Vulnerability Verification

- [ ] **Verify Host Header Dev Bypass (SEC-01)**:
  Open [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs#L42-L44). Observe lines 42–44:
  `var host = _httpContextAccessor?.HttpContext?.Request.Host.Host;`
  `return !string.IsNullOrEmpty(host) && (string.Equals(host, "localhost", ...) || host == "127.0.0.1");`
  Observe that lines 172, 185, and 211 grant `IsAuthenticated = true`, `PlatformAdmin` roles, and `Permissions.All` whenever `IsDevelopment()` is true.
- [ ] **Verify Hardcoded Admin Email Backdoor (SEC-02)**:
  Open [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs#L98-L103). Observe lines 98–100:
  `Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase) || UserId == Guid.Parse("50000000-0000-0000-0000-000000000002")`
  Observe lines 246–250 where `HasPermission` returns `true` unconditionally for `IsAdminUser`.
- [ ] **Verify Committed Secrets in Git (SEC-03)**:
  Open [Daycare.Web/appsettings.json](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/appsettings.json#L15-L21). Confirm live test keys `rzp_test_SWtZ26EZdULbQy` and `whsec_enjoyeveryday_webhook` are committed in plain text.
- [ ] **Verify Unprotected Account Book Route (SEC-04)**:
  Open [AccountBookPage.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/AccountBookPage.razor#L1-L10). Confirm there is no `@attribute [Authorize]` on the page.
  Open [AccountBookService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Commercial/AccountBookService.cs#L486). Confirm it calls `GetAllInvoicesAsync(null, null, null, ct)` which queries across all tenants.

---

## 3. Architecture & Separation of Concerns Verification

- [ ] **Verify Dapper Package in Application Layer**:
  Open [EnjoyEveryday.Application.csproj](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/EnjoyEveryday.Application.csproj#L9). Confirm `<PackageReference Include="Dapper" Version="2.1.89" />` is referenced directly.
- [ ] **Verify Direct SQL in Application Services**:
  Open [StaffRosterService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Organization/StaffRosterService.cs#L91-L130). Confirm the service injects `IDbConnectionFactory`, uses `using Dapper;`, and executes raw SQL `SELECT` statements rather than using a repository.
  Verify the same pattern in `DaycareOperationsBoardService.cs`, `DashboardService.cs`, `BranchBenchmarkingService.cs`, `MemoryService.cs`, `ExecutionService.cs`, `ChildDevelopmentPortfolioService.cs`, `EnrollmentPipelineService.cs`, `SecurePickupService.cs`, and `PlatformAdminService.cs`.
- [ ] **Verify Leaked SQL in Presentation Layer**:
  Open [ActiveSessionModal.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/ActiveSessionModal/ActiveSessionModal.razor#L14-L16). Confirm `@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory Db` and `@using Dapper`. Confirm lines 253–259 execute raw Dapper SQL without a tenant filter.
- [ ] **Verify Abandoned API Project**:
  Open [EnjoyEveryday.Api/Program.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Api/Program.cs). Confirm `AddApplicationServices()` and `AddInfrastructureServices()` are missing.
  Open [ChildrenController.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Api/Controllers/Children/ChildrenController.cs#L15). Confirm `ChildService` is injected, which cannot be resolved by the DI container.

---

## 4. Modularity & Duplication Verification

- [ ] **Verify 20 Duplicate Pages Between Web Hosts**:
  Run in PowerShell:
  ```powershell
  $daycare = Get-ChildItem -Path src\EnjoyEveryday.Daycare.Web\Components\Pages -Recurse -File -Filter *.razor | ForEach-Object { $_.FullName.Replace('C:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Daycare.Web\Components\Pages\', '') }
  $admin = Get-ChildItem -Path src\EnjoyEveryday.Admin.Web\Components\Pages -Recurse -File -Filter *.razor | ForEach-Object { $_.FullName.Replace('C:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\', '') }
  $daycare | Where-Object { $admin -contains $_ }
  ```
  Confirm 20 matching `.razor` page files are output. Compare [Daycare.Web/ExperienceStudio.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStudio.razor) (916 lines) and [Admin.Web/ExperienceStudio.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/ExperienceStudio.razor) (880 lines).
- [ ] **Verify Zero Code-Behind Files**:
  Run:
  ```powershell
  (Get-ChildItem -Path src -Recurse -File -Filter *.razor.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }).Count
  ```
  Confirm the output is `0`.
- [ ] **Verify Missing HttpClient BaseAddress in AI Services**:
  Open [WeeklyPlanningAssistantService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Ai/WeeklyPlanningAssistantService.cs#L144). Confirm `new HttpRequestMessage(HttpMethod.Post, "/api/weekly-plan")` is called.
  Open [ApplicationServiceRegistration.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/ApplicationServiceRegistration.cs#L60). Confirm `AddHttpClient<IWeeklyPlanningAssistantService, WeeklyPlanningAssistantService>()` registers the client without setting a `BaseAddress`.

---

## 5. Performance & Scalability Verification

- [ ] **Verify N+1 Query in Invoicing**:
  Open [BillingRepository.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Infrastructure/Repositories/Commercial/BillingRepository.cs#L39-L45). Confirm that lines 39–45 loop over fetched invoices and await `GetLineItemsForInvoiceInternalAsync` and `GetPaymentsForInvoiceInternalAsync` sequentially inside the loop.
- [ ] **Verify Empty CI/CD Directory**:
  Inspect `.github/workflows/`. Confirm it is completely empty.
