---
okf_version: "0.2.0"
type: concept
title: "Facility Profile, About Us & Contact Us"
description: "Customizable About Us and Contact Us system for Organization Administrators and Branch Administrators, with viewer pages for Parents and Teachers, plus an interactive inquiry form."
parent: "index.md"
status: active
generated: "2026-11-14"
tags:
  - facility-profile
  - about-us
  - contact-us
  - branding
  - inquiries
  - parent-portal
  - daycare-web
---

# Facility Profile, About Us & Contact Us

The **Facility Profile & Contact Us** system provides a multi-level branding and communication framework that allows Organization Administrators and Branch Administrators to customize and publish their identity, educational philosophy, campus photos, location coordinates, hours, and direct phone lines.

---

## 1. Core Capabilities & Architectural Hierarchy

```
┌───────────────────────────────────────────────────────────────┐
│                 ORGANIZATION DEFAULT PROFILE                  │
│  - Configured by Organization Administrator                   │
│  - Organization Logo, Title, High-level Motto                 │
│  - Pedagogy Story & Core Values                               │
│  - HQ Contact Email & Phone                                   │
└───────────────────────────────┬───────────────────────────────┘
                                │ Fallback Inheritance
                                ▼
┌───────────────────────────────────────────────────────────────┐
│                    BRANCH-SPECIFIC PROFILE                    │
│  - Configured by Branch Administrator or Org Administrator     │
│  - Campus Name, Local Tagline, Campus Photos / Logo           │
│  - Local Direct Lines (Front Desk, Emergency Line)            │
│  - Physical Address, Google Maps link, Parking / Arrival notes│
│  - Campus Operating Hours (e.g. 7:00 AM – 6:00 PM)            │
└───────────────────────────────┬───────────────────────────────┘
                                │
               ┌────────────────┴────────────────┐
               ▼                                 ▼
┌──────────────────────────────┐ ┌──────────────────────────────┐
│        PARENT PORTAL         │ │        DAYCARE PORTAL        │
│    `/about-and-contact`      │ │ `/organization/branch-info` │
│ - 1-touch Call, Email, Maps  │ │ - Staff quick contact lookup │
│ - Center story & photo view  │ │ - Emergency lines & hours    │
│ - Interactive Contact Form   │ │ - Editor link for admins     │
└──────────────────────────────┘ └──────────────────────────────┘
```

1. **Platform Admin**: Completely excluded from this feature.
2. **Org Admin**: Can edit the organization default profile or drill into any branch under their organization. Can view the published page with a 1-click campus selector.
3. **Branch Admin**: Can edit and manage their assigned branch's profile and receive/resolve inquiries for that branch. Can switch to view their live published page.
4. **All Organization Roles (Teachers, Educators, Lead Teachers, Staff)**: Full access to the published Contact Us & About Us page via `/about-us`, `/contact-us`, or `/organization/branch-info`. Includes an interactive campus selector pill bar allowing staff to explore profiles across multiple physical campuses or the central organization overview.
5. **Parents**: View their enrolled branch's profile (or org fallback) in `EnjoyEveryday.Parent.Web` at `/about-and-contact` (and `/about-us`, `/contact-us`), with 1-click `tel:`, `mailto:`, Google Maps navigation, dynamic campus switching, and an interactive contact inquiry form.
6. **Campus Selector Bar**: Embedded directly at the top of `FacilityProfileView.razor`. Displays pills for "🏢 Organization Overview" plus every active physical campus in the organization. When an Admin views the page, an "✏️ Edit Profile" button appears right next to the campus selector.

---

## 2. Zero-Database-Bloat Media Architecture

All uploaded branding images (logos, cover hero banners, and campus photo galleries) are processed using:
- **`AvifImageUploader.razor`**: Converts uploaded photos to optimized AVIF/WebP binaries.
- **`IMediaService` & `IObjectStorageRegistry`**: Uploads binary payloads directly to physical storage (`storage/media/` or Azure Blob Container).
- **Lean PostgreSQL Storage**: The `facility_profiles` table stores only the lightweight 55-byte URI endpoint (`/api/media/{id}/display`) and JSON array of image URLs (`gallery_media_urls`), avoiding database bloat.

---

## 3. Database Schema

- **Migration**: `119_facility_profiles_and_inquiries.sql`
- **Table `facility_profiles`**:
  - `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`
  - `tenant_id UUID NOT NULL REFERENCES tenants(id)`
  - `organization_id UUID NOT NULL REFERENCES organizations(id)`
  - `branch_id UUID NULL REFERENCES branches(id)` (NULL for org default)
  - `title VARCHAR(200) NOT NULL`, `motto VARCHAR(300)`, `about_story TEXT`
  - `logo_media_url VARCHAR(500)`, `cover_media_url VARCHAR(500)`, `gallery_media_urls JSONB`
  - `phone VARCHAR(50)`, `secondary_phone VARCHAR(50)`, `emergency_phone VARCHAR(50)`
  - `email VARCHAR(150)`, `inquiry_email VARCHAR(150)`, `operating_hours VARCHAR(250)`
  - `street_address VARCHAR(250)`, `city VARCHAR(100)`, `state VARCHAR(100)`, `postal_code VARCHAR(30)`, `country VARCHAR(100)`
  - `map_url TEXT`, `directions_notes TEXT`, `core_values JSONB`
  - Partial Unique Indexes:
    - `uk_facility_profiles_org` on `(tenant_id, organization_id) WHERE branch_id IS NULL`
    - `uk_facility_profiles_branch` on `(tenant_id, organization_id, branch_id) WHERE branch_id IS NOT NULL`
- **Table `contact_inquiries`**:
  - `id UUID PRIMARY KEY DEFAULT gen_random_uuid()`
  - `tenant_id`, `organization_id`, `branch_id`
  - `sender_user_id UUID NULL`, `sender_name`, `sender_email`, `sender_phone`
  - `category VARCHAR(50)` (`General`, `Tour Request`, `Enrollment`, `Absence`, `Feedback`)
  - `subject`, `message`, `status VARCHAR(30)` (`New`, `InReview`, `Resolved`)
  - `admin_notes TEXT`, `created_at`, `resolved_at`, `resolved_by_user_id`

---

## 4. Public Inquiry Routing & Multi-Role Visibility

When a parent, teacher, or visitor submits a message through the public Contact Us form, inquiries are routed and visible according to the administrative hierarchy:

1. **Branch Administrator**:
   - Accesses `/organization/inquiries` (or `/organization/profile-customizer` -> "📬 Received Inquiries" tab).
   - Scoped strictly to inquiries submitted for their assigned branch campus.
   - Can filter by status (`All`, `New`, `InReview`, `Resolved`) and update statuses as parent inquiries are addressed.

2. **Organization Administrator**:
   - Accesses `/organization/inquiries` (or `/organization/profile-customizer` -> "📬 Received Inquiries" tab).
   - Can view inquiries consolidated across all branches or filter by a specific campus using the scope dropdown.
   - Full authority to resolve, re-open, or annotate inquiries across the organization.

3. **Platform Administrator**:
   - Accesses the centralized Public Inquiries Desk in `Admin.Web` at `/operations/inquiries` (also linked in the main sidebar under **Platform Operations**).
   - Cross-tenant visibility across all registered daycare organizations and branches.
   - Can filter by Organization, Status (`New`, `InReview`, `Resolved`), or Topic (`General`, `Tour Request`, `Enrollment`, `Absence`, `Feedback`), review full message contents, and manage resolution statuses.

---

## 5. Clean Transparent Icon Standard (Zero Tinted Boxes)

In strict accordance with repository UI standards:
- Dashboard snapshot icons (`.snapshot-icon`) and action shortcut icons (`.action-card-icon`, `.quick-icon-box`) must **never** feature tinted pastel background squares (e.g., light blue, peach, or light teal boxes).
- Icons render directly and transparently against card surfaces with clean glyphs, matching the Scandinavian warm design tokens (`--surface`, `--surface-soft`, `--text`, `--accent-dark`).

---

## 6. Key Components and Services

| Component / File | Purpose |
|:---|:---|
| `FacilityProfileDtos.cs` | DTOs for facility profiles, save requests, inquiries, and branch selector options. |
| `IFacilityProfileService.cs` | Service contract defining profile retrieval (with fallback), saving, inquiry submission, and cross-tenant platform queries. |
| `FacilityProfileService.cs` | Dapper implementation respecting multi-tenant and role-scoping constraints. |
| `FacilityProfileView.razor` | Shared read-only Scandinavian card view with light warm hero surface (`.facility-hero-clean`), high-contrast dark forest slate typography, clean light action pills, story, gallery lightbox, location, and inquiry form. |
| `FacilityProfileEditorView.razor` | Shared multi-tab editor for Org and Branch Admins (customizer, received inquiries workflow, live parent preview). |
| `FacilityProfileCustomizerPage.razor` | Daycare Web Admin page at `/organization/profile-customizer` and `/organization/inquiries`. |
| `BranchAboutViewPage.razor` | Daycare Web Teacher page at `/organization/branch-info` for quick campus contact lookup. |
| `AboutAndContactPage.razor` | Parent Portal page at `/about-and-contact` with direct phone links, directions, and inquiry submission. |
| `PlatformInquiriesPage.razor` | Admin.Web Platform Admin centralized public inquiries desk at `/operations/inquiries`. |

