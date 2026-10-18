# EnjoyEveryday — Security & Data Integrity Findings

## 1. Security Findings Summary

| Finding ID | Severity | Category | Title | Affected File / Endpoint |
|:---|:---:|:---|:---|:---|
| **SEC-01** | **CRITICAL** | Authentication / Privilege Escalation | Host Header Spoofing Grants Full Admin Access | `AppIdentityContext.cs` (lines 42–44, 117–124, 172) |
| **SEC-02** | **CRITICAL** | Authorization / Backdoor | Hardcoded Admin Email Unconditionally Bypasses All Permissions | `AppIdentityContext.cs` (lines 98–103, 184, 198–200, 246–250) |
| **SEC-03** | **CRITICAL** | Secrets Management | Live Payment Gateway API Keys & Webhook Secrets Committed in Git | `Daycare.Web/appsettings.json`, `Admin.Web/appsettings.json` |
| **SEC-04** | **HIGH** | Authorization / Multi-Tenancy | Missing Page-Level Authorization and Unbounded Tenant Queries | `Admin.Web/AccountBookPage.razor`, `AccountBookService.cs` |
| **SEC-05** | **HIGH** | Multi-Tenancy / Data Leak | Presentation Component Executes Raw SQL Without Tenant Scoping | `UI.Shared/ActiveSessionModal.razor` (lines 253–259) |
| **SEC-06** | **HIGH** | Concurrency / Stability | Reflection-Based Sync-Over-Async Can Deadlock Blazor Circuits | `AppIdentityContext.cs` (lines 57–82) |
| **SEC-07** | **MEDIUM** | Multi-Tenancy | Global Unique Index Prevents Multi-Tenant Email Reuse | `database/migrations/001_foundation.sql` (line 95) |
| **SEC-08** | **MEDIUM** | Cloud Readiness / Data Loss | Ephemeral Local Disk Used for Storage of Legal & Safety Documents | `FloorMapStorageService.cs`, `ConsentService.cs` |

---

## 2. In-Depth Vulnerability Analysis

---

### SEC-01: Host Header Spoofing Grants Full Admin Access
- **Severity**: **CRITICAL**
- **Affected File**: [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs#L25-L44)
- **Vulnerable Data Flow**:
  1. Incoming HTTP request reaches `AppIdentityContext`.
  2. `IsDevelopment()` checks:
     ```csharp
     var host = _httpContextAccessor?.HttpContext?.Request.Host.Host;
     return !string.IsNullOrEmpty(host) && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1");
     ```
  3. If the client sends `Host: localhost` (or if traffic is forwarded through an internal reverse proxy like NGINX, YARP, or AWS ALB without host rewriting), `IsDevelopment()` evaluates to `true`.
  4. When `IsDevelopment()` is `true`:
     - `IsAuthenticated` returns `true` (line 172).
     - `UserId` defaults to `50000000-0000-0000-0000-000000000002` (line 152).
     - `TenantId` defaults to `10000000-0000-0000-0000-000000000001` (line 123).
     - `Roles` includes `PlatformAdmin`, `OrganizationOwner`, `OrganizationAdmin`, `BranchAdmin` (line 185).
     - `Permissions` returns `Permissions.All` (line 211).
- **Realistic Impact**: Complete unauthorized platform takeover. Any unauthenticated external attacker who can spoof the `Host` header or connect via an internal proxy gains full root administrative privileges across the application.
- **Evidence**:
  ```csharp
  // AppIdentityContext.cs lines 42-44
  var host = _httpContextAccessor?.HttpContext?.Request.Host.Host;
  return !string.IsNullOrEmpty(host) && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1");
  ```
- **Recommended Fix**:
  Never determine the execution environment from user-controlled HTTP request headers. Use ASP.NET Core `IHostEnvironment.IsDevelopment()` injected via dependency injection.
- **Verification Test**:
  Send an unauthenticated request to an endpoint with `Host: localhost`. In a non-development environment, verify that `IsAuthenticated` is `false` and the request is rejected with `401 Unauthorized`.

---

### SEC-02: Hardcoded Admin Email Unconditionally Bypasses All Permissions
- **Severity**: **CRITICAL**
- **Affected File**: [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs#L98-L103)
- **Vulnerable Data Flow**:
  1. A user authenticates via Google OAuth with the email `admin@littlestars.com`.
  2. In `AppIdentityContext.HasPermission`:
     ```csharp
     if (IsAdminUser)
     {
         return true;
     }
     ```
  3. Where `IsAdminUser` is defined as:
     ```csharp
     private bool IsAdminUser =>
         Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase) ||
         UserId == Guid.Parse("50000000-0000-0000-0000-000000000002") || ...
     ```
- **Realistic Impact**:
  If anyone registers a Google account with `admin@littlestars.com` (or if email normalization/impersonation occurs), they automatically bypass every authorization boundary in the system, even in production, across all organizations and tenants.
- **Evidence**:
  Lines 98–103, 184, 198–200, 246–250 in `AppIdentityContext.cs`.
- **Recommended Fix**:
  Remove all hardcoded email addresses and static GUIDs from authorization logic. Permissions must be derived strictly from verified database role assignments and organization memberships.
- **Verification Test**:
  Authenticate as `admin@littlestars.com` without active role assignments in the database. Verify that permission checks return `false`.

---

### SEC-03: Live Payment Gateway Keys & Webhook Secrets in Git
- **Severity**: **CRITICAL**
- **Affected Files**:
  - [src/EnjoyEveryday.Daycare.Web/appsettings.json](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/appsettings.json#L15-L21)
  - [src/EnjoyEveryday.Admin.Web/appsettings.json](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/appsettings.json#L13-L19)
- **Vulnerable Data Flow**:
  1. Plaintext configuration contains:
     ```json
     "Razorpay": {
       "KeyId": "rzp_test_SWtZ26EZdULbQy",
       "KeySecret": "V0f3PBqMcmMEvBIRcx7LIdo7",
       "WebhookSecret": "whsec_enjoyeveryday_webhook",
       "DefaultCurrency": "INR",
       "MockMode": false
     }
     ```
  2. These credentials have been committed directly to the Git repository history.
- **Realistic Impact**:
  Anyone with repository access (or if the repository is ever pushed to a public remote or leaked) can forge webhook signatures, initiate fraudulent payment orders, or intercept merchant payment workflows.
- **Recommended Fix**:
  1. Immediately rotate the Razorpay Key Secret and Webhook Secret in the Razorpay Dashboard.
  2. Remove secrets from `appsettings.json`.
  3. Use .NET User Secrets (`dotnet user-secrets`) for local development and environment variables or Azure Key Vault / AWS Secrets Manager in production.
- **Verification Test**:
  Inspect `appsettings.json` to ensure `KeySecret` and `WebhookSecret` are empty strings and the application reads them from environment variables.

---

### SEC-04: Missing Page-Level Authorization and Unbounded Tenant Queries
- **Severity**: **HIGH**
- **Affected Files**:
  - [AccountBookPage.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/AccountBookPage.razor#L1-L10)
  - [AccountBookService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Commercial/AccountBookService.cs#L484-L490)
- **Vulnerable Data Flow**:
  1. `AccountBookPage.razor` has route `@page "/account-book"`.
  2. It contains no `@attribute [Authorize(Roles = "...")]` or `@attribute [Authorize(Permissions = "...")]`.
  3. When rendered, `OnInitializedAsync` invokes:
     `_overview = await AccountBookService.GetFinancialOverviewAsync();`
  4. In `AccountBookService.cs`:
     `var invoices = await _billingRepository.GetAllInvoicesAsync(null, null, null, ct);`
  5. In `BillingRepository.cs`, passing `tenantId = null` causes the query to execute without a `tenant_id` WHERE clause, loading financial data across all tenants.
- **Realistic Impact**:
  Any authenticated user (or unauthenticated visitor if routing allows) can view aggregated revenue, outstanding balances, and invoice records for every tenant on the platform.
- **Recommended Fix**:
  1. Add `@attribute [Authorize(Roles = "PlatformAdmin")]` to `AccountBookPage.razor`.
  2. Guard `AccountBookService` methods with `_authorizer.RequireOrganizationPermissionAsync(Permissions.PlatformAdminManage)`.
- **Verification Test**:
  Attempt to access `/account-book` as a Daycare Teacher or Parent. Verify access is denied (`403 Forbidden`).

---

### SEC-05: Presentation Component Executes Raw SQL Without Tenant Scoping
- **Severity**: **HIGH**
- **Affected File**: [ActiveSessionModal.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/ActiveSessionModal/ActiveSessionModal.razor#L253-L260)
- **Vulnerable Data Flow**:
  ```csharp
  using var conn = await Db.CreateConnectionAsync();
  var presentIds = await conn.QueryAsync<Guid>(
      "SELECT child_id FROM child_attendance WHERE classroom_id = @Id AND attendance_date = @Date AND status IN ('Present', 'Late')",
      new { Id = _session.ClassroomId, Date = todayDate });
  ```
- **Realistic Impact**:
  Presentation components should never execute SQL. Bypassing tenant filtering risks cross-tenant child data disclosure if classroom IDs are manipulated.
- **Recommended Fix**:
  Delegate data fetching to `IAttendanceService.GetPresentChildrenByClassroomAsync(classroomId, date, ct)` which enforces tenant isolation.

---

### SEC-06: Reflection-Based Sync-Over-Async Can Deadlock Blazor Circuits
- **Severity**: **HIGH**
- **Affected File**: [AppIdentityContext.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs#L57-L82)
- **Mechanism**:
  `ResolveUser()` searches loaded assemblies via reflection for `AuthenticationStateProvider`, reflects its `GetAuthenticationStateAsync()` method, and executes `task.GetAwaiter().GetResult()`.
- **Realistic Impact**:
  In Blazor Server, the SignalR circuit dispatcher thread is synchronous. Calling sync-over-async (`GetAwaiter().GetResult()`) inside a property getter can exhaust the .NET ThreadPool and permanently freeze interactive UI circuits for all connected teachers and administrators.
- **Recommended Fix**:
  In Blazor Server applications, inject `AuthenticationStateProvider` directly into components and pass `AuthenticationState` asynchronously to services, rather than using synchronous reflection in property getters.
