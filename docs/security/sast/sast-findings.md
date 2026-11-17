# EnjoyEveryday – SAST Vulnerability Findings Catalog

**Assessment Date:** November 17, 2026  
**Scope:** Static Analysis of Source Code and Configuration  

---

## Findings Matrix

| Finding ID | Severity | CVSS v3.1 | CWE | Title | Status |
|:---|:---:|:---:|:---:|:---|:---:|
| **SEC-SAST-01** | **Critical** | 9.1 | CWE-306 / CWE-1188 | Unauthenticated Development Context Grants Full Platform Admin Without Loopback Check | Confirmed |
| **SEC-SAST-02** | **High** | 8.8 | CWE-285 | Missing PlatformAdmin Role Enforcement on Admin Portal (`Admin.Web`) | Confirmed |
| **SEC-SAST-03** | **High** | 7.4 | CWE-601 | Open Redirect in Authentication Endpoints (`returnUrl` unvalidated) | Confirmed |
| **SEC-SAST-04** | **Medium** | 6.5 | CWE-285 | Missing Role / Scope Verification on Daycare Organization Financial Billing | Confirmed |
| **SEC-SAST-05** | **Medium** | 6.5 | CWE-942 | Permissive CORS Configuration (`*`) with Credentials Enabled in Python AI Agent | Confirmed |
| **SEC-SAST-06** | **Medium** | 5.3 | CWE-306 | Python AI Agent Service Authentication Disabled by Default | Confirmed |
| **SEC-SAST-07** | **Low** | 4.3 | CWE-200 / CWE-209 | Blazor Server Circuit `DetailedErrors = true` Hardcoded Unconditionally | Confirmed |
| **SEC-SAST-08** | **Low** | 3.1 | CWE-209 | Database Connection String `Include Error Detail=true` in Configuration | Confirmed |

---

## Detailed Vulnerability Analysis

---

### SEC-SAST-01: Unauthenticated Development Context Grants Full Platform Admin Without Loopback Check

- **Severity:** Critical
- **CVSS Score:** 9.1 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:N`)
- **CWE:** CWE-306 (Missing Authentication for Critical Function), CWE-1188 (Insecure Default Initialization of Resource)
- **Affected File:** `src/EnjoyEveryday.Application/Services/Auth/AppIdentityContext.cs` (Lines 87–112)

#### Vulnerability Description:
In `AppIdentityContext.cs`, the implementation checks `if (IsDevelopment())` and if the HTTP request is unauthenticated, it automatically constructs a mock identity:
```csharp
if (IsDevelopment())
{
    // Return dev context when running in development environment and no user logged in
    var devTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    var devOrgId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    ...
    return new AppIdentityContext
    {
        IsAuthenticated = true,
        UserId = devUserId,
        Roles = new[] { RoleTemplates.PlatformAdmin, RoleTemplates.OrganizationOwner, RoleTemplates.OrganizationAdmin, RoleTemplates.BranchAdmin },
        Permissions = Permissions.All,
        ...
    };
}
```
In contrast to `AuthEndpoints.IsDevLoginPermitted(HttpContext)`, which checks `context.Request.Host.Host == "localhost" || context.Connection.RemoteIpAddress.IsLoopback(...)`, `AppIdentityContext.cs` only checks `_environment.IsDevelopment()`. If an application instance is hosted in a staging, shared LAN, or Docker container with `ASPNETCORE_ENVIRONMENT=Development`, any remote caller over the network who hits any application service directly inherits full `PlatformAdmin` roles and `Permissions.All`.

#### Remediation:
Enforce that development fallback credentials only activate if the incoming HTTP request is confirmed to be on loopback (local machine) via `IPAddress.IsLoopback` or when running strictly in local testing with no remote network interface.

---

### SEC-SAST-02: Missing PlatformAdmin Role Enforcement on Admin Portal (`Admin.Web`)

- **Severity:** High
- **CVSS Score:** 8.8 (`CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:H/A:H`)
- **CWE:** CWE-285 (Improper Authorization)
- **Affected File:** `src/EnjoyEveryday.Admin.Web/Components/_Imports.razor` (Line 13)

#### Vulnerability Description:
In `src/EnjoyEveryday.Admin.Web/Components/_Imports.razor`, the global authorization attribute is declared as:
```razor
@attribute [Authorize]
```
No role constraint is specified. Except for `AccountBookPage.razor` which specifies `@attribute [Authorize(Roles = RoleTemplates.PlatformAdmin)]`, all other platform administration pages (`/organizations`, `/branches`, `/marketplace-operations`, `/warehouse-inventory`, `/tariffs`) inherit the bare `@attribute [Authorize]`.  
As a consequence, ANY authenticated user from any tenant (such as a Daycare Teacher, Daycare Branch Admin, or Parent) who visits `https://localhost:7004/organizations` or other admin pages can view and manipulate platform-wide organizations, branches, and marketplace catalog items.

#### Remediation:
Update `src/EnjoyEveryday.Admin.Web/Components/_Imports.razor` to enforce platform administrator roles globally across the entire admin portal:
```razor
@attribute [Authorize(Roles = $"{RoleTemplates.PlatformAdmin},Superadmin")]
```
Pages requiring more specific permissions can still add additional constraints.

---

### SEC-SAST-03: Open Redirect in Authentication Endpoints (`returnUrl` unvalidated)

- **Severity:** High
- **CVSS Score:** 7.4 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:C/C:L/I:L/A:N`)
- **CWE:** CWE-601 (URL Redirection to Untrusted Site / Open Redirect)
- **Affected Files:**
  - `src/EnjoyEveryday.Admin.Web/Endpoints/AuthEndpoints.cs` (Lines 91, 164, 219)
  - `src/EnjoyEveryday.Daycare.Web/Endpoints/AuthEndpoints.cs` (Lines 345, 407, 420, 432)
  - `src/EnjoyEveryday.Parent.Web/Endpoints/AuthEndpoints.cs` (Lines 100, 193)

#### Vulnerability Description:
Across all three web portals, the authentication endpoints accept a `returnUrl` query parameter:
```csharp
var returnUrl = context.Request.Query["returnUrl"].FirstOrDefault() ?? "/";
...
return Results.LocalRedirect(returnUrl); // or Results.Redirect(returnUrl)
```
In multiple redirect paths, `Results.Redirect(returnUrl)` is called directly without verifying whether `returnUrl` is a local URL. For example:
```csharp
return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
```
An attacker can craft a phishing link such as `https://localhost:7003/auth/login?returnUrl=https://attacker-phishing.com/login` which lures a legitimate user to sign in and then immediately redirects their browser to a malicious spoofed site.

#### Remediation:
Sanitize and validate `returnUrl` using an explicit local URL validator helper:
```csharp
private static string EnsureLocalUrl(string? returnUrl, string fallback = "/")
{
    if (string.IsNullOrWhiteSpace(returnUrl)) return fallback;
    if (returnUrl.StartsWith("//") || returnUrl.StartsWith("/\\")) return fallback;
    if (Uri.TryCreate(returnUrl, UriKind.Relative, out _)) return returnUrl;
    return fallback;
}
```

---

### SEC-SAST-04: Missing Role / Scope Verification on Daycare Organization Financial Billing

- **Severity:** Medium
- **CVSS Score:** 6.5 (`CVSS:3.1/AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:N/A:N`)
- **CWE:** CWE-285 (Improper Authorization)
- **Affected Files:**
  - `src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationBilling.razor`
  - `src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationBillingView.razor`

#### Vulnerability Description:
`OrganizationBilling.razor` exposes organization-wide billing statements, invoices, and payment histories. The page only declares `@attribute [Authorize]`. It lacks `@attribute [Authorize(Roles = ...)]` and does not call `EnsureAuthorizedAsync` for billing permissions. A standard staff member or teacher logged into the Daycare portal can navigate directly to `/management/billing` and view organization financial invoices.

#### Remediation:
Add role enforcement to `OrganizationBilling.razor`:
```razor
@attribute [Authorize(Roles = $"{RoleTemplates.OrganizationOwner},{RoleTemplates.OrganizationAdmin}")]
```

---

### SEC-SAST-05: Permissive CORS Configuration (`*`) with Credentials Enabled in Python AI Agent

- **Severity:** Medium
- **CVSS Score:** 6.5 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:U/C:H/I:N/A:N`)
- **CWE:** CWE-942 (Permissive Cross-Origin Resource Sharing Policy)
- **Affected File:** `src/EnjoyEveryday.AiAgent/app/main.py` (Lines 16–22)

#### Vulnerability Description:
The FastAPI application configures `CORSMiddleware` with wildcard origins:
```python
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
```
In standard web security, `allow_origins=["*"]` combined with `allow_credentials=True` allows malicious web pages visited by a browser to make credentialed background requests to the local/internal AI service endpoint.

#### Remediation:
Restrict `allow_origins` to known trusted origins (e.g. `http://localhost:7003`, `https://localhost:7003`, `http://localhost:7004`, `https://localhost:7004`, `http://localhost:7005`, `https://localhost:7005`) or configure via an explicit environment setting `CORS_ORIGINS`.

---

### SEC-SAST-06: Python AI Agent Service Authentication Disabled by Default

- **Severity:** Medium
- **CVSS Score:** 5.3 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:L/A:N`)
- **CWE:** CWE-306 (Missing Authentication for Critical Function)
- **Affected File:** `src/EnjoyEveryday.AiAgent/app/config.py` (Line 18)

#### Vulnerability Description:
In `config.py`:
```python
REQUIRE_SERVICE_AUTH = os.getenv("REQUIRE_SERVICE_AUTH", "false").lower() == "true"
```
Because `REQUIRE_SERVICE_AUTH` defaults to `false`, the AI Agent microservice accepts requests on port 8000 using fallback dummy keys (`DEFAULT_AI_KEY`). Any client on the network can proxy prompts directly to the backend Gemini model using the server's API quota without authenticating.

#### Remediation:
Enforce `REQUIRE_SERVICE_AUTH = True` in production configurations, and validate the `X-Tenant-Api-Key` or service shared secret on all `/api/v1/` routes.

---

### SEC-SAST-07: Blazor Server Circuit `DetailedErrors = true` Hardcoded Unconditionally

- **Severity:** Low
- **CVSS Score:** 4.3 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N`)
- **CWE:** CWE-200 / CWE-209 (Information Disclosure)
- **Affected Files:**
  - `src/EnjoyEveryday.Admin.Web/Program.cs` (Line 20)
  - `src/EnjoyEveryday.Parent.Web/Program.cs` (Line 19)

#### Vulnerability Description:
In `Admin.Web/Program.cs` and `Parent.Web/Program.cs`:
```csharp
builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
{
    options.DetailedErrors = true;
});
```
`DetailedErrors = true` causes detailed server exception traces and internal stack frames to be sent across the SignalR WebSocket connection to the client browser whenever an unhandled exception occurs in a Blazor component. In production, this discloses internal directory paths, query details, and stack traces.

#### Remediation:
Wrap the setting with an environment check:
```csharp
builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});
```

---

### SEC-SAST-08: Database Connection String `Include Error Detail=true` in Configuration

- **Severity:** Low
- **CVSS Score:** 3.1 (`CVSS:3.1/AV:N/AC:H/PR:N/UI:N/S:U/C:L/I:N/A:N`)
- **CWE:** CWE-209 (Generation of Error Message Containing Sensitive Information)
- **Affected File:** `src/EnjoyEveryday.Daycare.Web/appsettings.json` (Line 9)

#### Vulnerability Description:
The connection string in `Daycare.Web/appsettings.json` specifies:
```json
"DefaultConnection": "Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres;Include Error Detail=true;"
```
When `Include Error Detail=true` is enabled in Npgsql, SQL parameter values and database column data are included in exception messages, increasing the risk of sensitive data leaking into application logs or client exception responses.

#### Remediation:
Remove `Include Error Detail=true;` from base `appsettings.json`, reserving it exclusively for `appsettings.Development.json`.

