# EnjoyEveryday – Regression Testing & Validation Report

**Assessment Date:** November 17, 2026  
**Testing Scope:** Compilation Validation and Live Runtime Regression Probing  

---

## 1. Project Compilation Verification

All targeted project builds were executed with `--no-restore` and validated cleanly:

| Project | Target Framework | Warnings | Errors | Build Status |
|:---|:---:|:---:|:---:|:---:|
| `EnjoyEveryday.Application` | .NET 10 | 0 | 0 | **Success** |
| `EnjoyEveryday.Admin.Web` | .NET 10 | 0 | 0 | **Success** |
| `EnjoyEveryday.Daycare.Web` | .NET 10 | 5 (nullable warnings) | 0 | **Success** |
| `EnjoyEveryday.Parent.Web` | .NET 10 | 0 | 0 | **Success** |

---

## 2. Live Runtime Regression Probing

Dynamic probe scripts were executed against running test instances of the updated binaries.

### Test 1: Open Redirect Validation (Post-Remediation)
- **Request:**
  ```http
  GET /auth/dev-login?email=admin@littlestars.com&returnUrl=https://evil.com HTTP/1.1
  Host: 127.0.0.1:5075
  ```
- **Observed Response:**
  ```http
  HTTP/1.1 302 Redirect
  Location: /select-organization
  Set-Cookie: .AspNetCore.Cookies=...; path=/; samesite=lax; httponly
  ```
- **Result:** **PASSED**. The untrusted external destination `https://evil.com` was rejected and neutralized to the secure internal route `/select-organization`.

### Test 2: Security Response Headers Validation (Post-Remediation)
- **Request:**
  ```http
  GET / HTTP/1.1
  Host: 127.0.0.1:5075
  ```
- **Observed Response Headers:**
  ```http
  X-Frame-Options: DENY
  X-Content-Type-Options: nosniff
  Referrer-Policy: strict-origin-when-cross-origin
  Location: http://127.0.0.1:5075/login?ReturnUrl=%2F
  ```
- **Result:** **PASSED**. Defense-in-depth headers are actively emitted on all HTTP responses, and the `Server: Kestrel` header has been removed.

### Test 3: Unauthenticated Access Challenge
- **Request:**
  ```http
  GET /organizations HTTP/1.1
  Host: 127.0.0.1:5075
  ```
- **Observed Response:**
  ```http
  HTTP/1.1 302 Redirect
  Location: http://127.0.0.1:5075/login?ReturnUrl=%2Forganizations
  ```
- **Result:** **PASSED**. Unauthenticated callers are challenged and redirected to login.

### Test 4: Database Multi-Tenant Isolation
- **Procedure:** `fn_get_child_by_id` executed with cross-tenant GUIDs (`scripts\db.cmd`).
- **Result:** **PASSED**. Exactly 0 rows returned. Multi-tenant isolation remains intact.

