# EnjoyEveryday – DAST (Dynamic Application Security Testing) Report

**Project:** EnjoyEveryday Early Childhood Education Platform  
**Date of Assessment:** November 17, 2026  
**Assessment Tool / Engine:** Google Antigravity Native DAST Engine (PowerShell & HTTP Dynamic Probes)  
**Execution Mode:** Black-Box & Grey-Box Dynamic Verification  
**Overall Risk Posture:** Medium (Specific perimeter redirects and missing defense-in-depth headers)

---

## 1. Executive Summary

A comprehensive Dynamic Application Security Testing (DAST) assessment was conducted against running services and database procedures of EnjoyEveryday. Live runtime endpoints, authentication handshakes, multi-tenant database stored procedures, and HTTP headers were dynamically probed and validated.

### Summary of Dynamic Probes & Results

| Test Area | Probes Executed | Target / Protocol | Dynamic Result |
|:---|:---:|:---:|:---|
| **Anonymous Access to Protected Pages** | 5 | HTTP/HTTPS (Port 5075 / 7004) | Passed: HTTP 302 Redirect to `/login?ReturnUrl=...` enforced. |
| **Open Redirect Manipulation (`returnUrl`)** | 3 | HTTP/HTTPS (Auth Endpoints) | **Vulnerable**: Returned HTTP 302 with `Location: https://evil.com`. |
| **Cross-Tenant IDOR: Child Records** | 4 | PostgreSQL RPC (`fn_get_child_by_id`, `fn_get_all_children`) | Passed: 0 rows returned on mismatched tenant queries. |
| **Cross-Tenant IDOR: Financial Invoices** | 3 | PostgreSQL RPC (`fn_get_invoices_for_organization`) | Passed: 0 rows returned on cross-organization queries. |
| **Cross-Tenant IDOR: Marketplace Orders** | 3 | PostgreSQL RPC (`fn_get_material_orders_for_organization`) | Passed: Scoped strictly to requesting organization ID. |
| **Path Traversal on Media Endpoints** | 4 | Minimal API `/api/media/{id:guid}/{variant}` | Passed: Strongly typed GUID constraint rejects non-GUID traversals. |
| **HTTP Security Headers Inspection** | 1 | Kestrel HTTP Response | **Vulnerable**: Missing CSP, X-Frame-Options, X-Content-Type-Options. |
| **Authentication Cookie Security** | 1 | Set-Cookie Inspection | Partial: `HttpOnly` and `SameSite=Lax` present; `Secure` conditional. |

---

## 2. Dynamic Testing Methodology

The dynamic evaluation was executed strictly using native environment capabilities:
1. **Dynamic Service Invocations**:
   - Spun up test instances using `dotnet exec` with development profiles.
   - Probed live endpoints using .NET `HttpWebRequest` with `AllowAutoRedirect = $false` to inspect exact status codes, `Location` headers, and `Set-Cookie` directives.
2. **Direct Stored Procedure Dynamic Invocations**:
   - Used `scripts\db.cmd` to execute parameterized queries simulating cross-tenant requests between Organization A (`Little Stars`, `10000000-0000-0000-0000-000000000001`) and Organization B (`Sunshine Garden`, `10000000-0000-0000-0000-000000000002`).
3. **Boundary & Injection Probes**:
   - Injected external domain targets (`https://evil.com`) into `returnUrl` parameters.
   - Tested invalid variant inputs on `/api/media` endpoints.

---

## 3. Key Dynamic Assessment Findings

### 3.1 Open Redirect Confirmed Live (SEC-DAST-01)
- When issuing `GET /auth/dev-login?email=admin@littlestars.com&returnUrl=https://evil.com`, the response was an immediate `302 Found` with:
  ```http
  HTTP/1.1 302 Redirect
  Location: https://evil.com
  Set-Cookie: .AspNetCore.Cookies=...; path=/; samesite=lax; httponly
  ```
- This confirms that an attacker can use EnjoyEveryday's authentication pipeline as an open redirector to execute credential phishing.

### 3.2 Strong Multi-Tenant Database Isolation Confirmed
- Dynamically executed `SELECT * FROM fn_get_child_by_id('10000000-0000-0000-0000-000000000002', 'ce31a0c4-837e-4aee-a165-c281dfedc401');` (Tenant B requesting Child belonging to Tenant A).
- **Result:** Exactly `0 rows` returned. Database procedures enforce tenant boundaries regardless of query parameters.
- Executed `SELECT count(*) FROM fn_get_invoices_for_organization('10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', true);` (Tenant A requesting Invoices of Org B).
- **Result:** Exactly `0 rows` returned.

### 3.3 Perimeter Security Headers Missing (SEC-DAST-02)
- Dynamic HTTP header probing revealed that response headers do not include standard defense-in-depth protection headers (`Content-Security-Policy`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`).

---

## 4. Conclusion & Transition to Remediation

Dynamic testing confirmed that EnjoyEveryday's internal database tier and media storage engine exhibit strong tenant isolation and parameterization. However, perimeter authentication endpoints (`returnUrl`) and HTTP header configurations present confirmed vulnerabilities that require immediate remediation.

Refer to [dast-findings.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/docs/security/dast/dast-findings.md) for full reproduction evidence and remediation plans.

