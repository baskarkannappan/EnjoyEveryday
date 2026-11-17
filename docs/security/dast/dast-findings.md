# EnjoyEveryday – DAST Vulnerability Findings Catalog

**Assessment Date:** November 17, 2026  
**Scope:** Dynamic Probing of Live Web Endpoints and Database RPC Layer  

---

## Findings Matrix

| Finding ID | Severity | CVSS v3.1 | CWE | Title | Status |
|:---|:---:|:---:|:---:|:---|:---:|
| **SEC-DAST-01** | **High** | 7.4 | CWE-601 | Live Open Redirect Verified via `returnUrl` Parameter | Confirmed Live |
| **SEC-DAST-02** | **Medium** | 5.3 | CWE-1021 / CWE-693 | Missing Security Headers (`Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`) | Confirmed Live |
| **SEC-DAST-03** | **Medium** | 4.8 | CWE-614 | Sensitive Session Cookie Missing `Secure` Attribute over Non-HTTPS Connections | Confirmed Live |
| **SEC-DAST-04** | **Low** | 3.7 | CWE-200 | Server Implementation Disclosure via `Server: Kestrel` Response Header | Confirmed Live |

---

## Detailed Dynamic Findings & Reproduction

---

### SEC-DAST-01: Live Open Redirect Verified via `returnUrl` Parameter

- **Severity:** High
- **CVSS Score:** 7.4 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:C/C:L/I:L/A:N`)
- **CWE:** CWE-601 (URL Redirection to Untrusted Site)
- **Live Target:** `http://127.0.0.1:5075/auth/dev-login?email=admin@littlestars.com&returnUrl=https://evil.com`

#### Dynamic Probe Request:
```http
GET /auth/dev-login?email=admin%40littlestars.com&returnUrl=https://evil.com HTTP/1.1
Host: 127.0.0.1:5075
Connection: close
```

#### Actual Server Response:
```http
HTTP/1.1 302 Redirect
Date: Tue, 17 Nov 2026 16:45:42 GMT
Server: Kestrel
Location: https://evil.com
Set-Cookie: .AspNetCore.Cookies=...; expires=Tue, 01 Dec 2026 16:45:42 GMT; path=/; samesite=lax; httponly
Content-Length: 0
```

#### Evidence & Exploit Scenario:
1. Attacker sends a phishing email to a daycare administrator containing a link to the real EnjoyEveryday domain:  
   `https://daycare.enjoyeveryday.com/auth/login?returnUrl=https://attacker-credential-harvester.com/stealer`
2. The user trusts the domain name and logs in with their credentials.
3. Upon completing authentication, the application server blindly issues a `302 Redirect` to `https://attacker-credential-harvester.com/stealer`.
4. The user is delivered directly to the attacker's server without warning.

#### Remediation:
In all authentication handlers (`AuthEndpoints.cs`), validate that `returnUrl` begins with `/`, does not begin with `//` or `/\`, and parses as a relative URI using `Url.IsLocalUrl(...)` or helper `EnsureLocalUrl(returnUrl)`.

---

### SEC-DAST-02: Missing Security Headers

- **Severity:** Medium
- **CVSS Score:** 5.3 (`CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N`)
- **CWE:** CWE-1021 (Improper Restriction of Rendered UI Layers or Frames), CWE-693 (Protection Mechanism Failure)
- **Live Target:** `http://127.0.0.1:5075/`

#### Dynamic Probe Inspection:
```
Headers captured on root response:
- X-Correlation-Id = 6cd19b22-669d-4afb-9cbd-9525748a327b
- Content-Length = 0
- Date = Tue, 17 Nov 2026 16:44:47 GMT
- Location = http://127.0.0.1:5075/login?ReturnUrl=%2F
- Server = Kestrel
```

#### Missing Headers:
1. `Content-Security-Policy`: No restrictions on script or connect sources.
2. `X-Frame-Options: DENY`: Allows clickjacking embedding in malicious `<iframe>` tags.
3. `X-Content-Type-Options: nosniff`: Allows MIME-type sniffing by legacy browsers.
4. `Referrer-Policy: strict-origin-when-cross-origin`: Full URL query strings could be passed to external origins in referrer headers.

#### Remediation:
Add security header middleware in `Program.cs` across all web portals:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

---

### SEC-DAST-03: Sensitive Session Cookie Missing `Secure` Attribute

- **Severity:** Medium
- **CVSS Score:** 4.8 (`CVSS:3.1/AV:N/AC:H/PR:N/UI:N/S:U/C:L/I:L/A:N`)
- **CWE:** CWE-614 (Sensitive Cookie in HTTPS Session Without 'Secure' Attribute)
- **Live Target:** `http://127.0.0.1:5075/auth/dev-login`

#### Actual Cookie Header:
```http
Set-Cookie: .AspNetCore.Cookies=...; expires=...; path=/; samesite=lax; httponly
```

#### Vulnerability Description:
While the `httponly` and `samesite=lax` flags are correctly set, the `secure` flag is omitted when communicating over plain HTTP. In production environments behind reverse proxies (like nginx or AWS ALB) terminating SSL, if `CookieSecurePolicy` is left to default `SameAsRequest`, cookies may be transmitted unencrypted if the proxy forwards via HTTP.

#### Remediation:
In cookie configuration:
```csharp
options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest 
    : CookieSecurePolicy.Always;
```

---

### SEC-DAST-04: Server Implementation Disclosure via `Server: Kestrel` Response Header

- **Severity:** Low
- **CVSS Score:** 3.7 (`CVSS:3.1/AV:N/AC:H/PR:N/UI:N/S:U/C:L/I:N/A:N`)
- **CWE:** CWE-200 (Exposure of Sensitive Information)
- **Live Target:** All Kestrel endpoints

#### Actual Header:
```http
Server: Kestrel
```

#### Remediation:
Disable the Kestrel server header in `Program.cs` / `ConfigureWebHost`:
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});
```

