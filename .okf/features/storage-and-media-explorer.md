---
okf_version: "0.2.0"
type: concept
title: "Storage & Media Telemetry Explorer"
description: "Platform and Org Admin object storage telemetry, file registry, and branch/classroom volume breakdown."
parent: "index.md"
status: active
generated: "2026-11-14"
tags:
  - storage
  - media
  - blob
  - telemetry
  - admin
---

# Storage & Media Telemetry Explorer

The **Storage & Media Explorer** provides Platform Administrators and Organization Administrators with live visibility into physical and cloud storage consumption across organizations, branches, and classrooms without downloading full image payloads.

---

## 1. Access Control & Authorization

* **Permitted Roles**: `PlatformAdmin`, `Superadmin`, `OrgAdmin`, `Admin`, `DaycareAdmin`.
* **Tenant Scoping & Multi-Tenant Isolation**:
  * **Platform Admin**: Scopes across all organizations or filters globally via `/platform-admin/storage-explorer`. Can view and display media assets across all tenants.
  * **Organization Admin**: Strictly scoped to current `TenantContext.TenantId` or `UserContext.OrganizationId` via `/organization/storage-metrics`. Cannot access or see media assets from other organizations, but has full access to inspect and open assets belonging to their own organization.
  * **Media Endpoint Security (`/api/media/{id}/display`)**: Queries the media asset by primary key UUID directly (`IMediaRepository.GetByIdAsync`) and verifies tenant authorization: Platform Admins can access any asset, public facility branding assets are open, while private assets require tenant or organization identity match (`asset.TenantId == effectiveTenantId || asset.OrganizationId == effectiveTenantId`). Other orgs receive `404 Not Found`.
* **Unauthorized Access**: Renders a dedicated access restriction card when loaded by staff or parents.

---

## 2. Architecture & Components

| Layer | File / Component | Purpose |
|:---|:---|:---|
| **Shared View** | [StorageExplorerView.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.UI.Shared/Components/Storage/StorageExplorerView.razor) | Dual-tab UI rendering summary metrics, hierarchy rollups, and file directory table. |
| **Platform Page** | [StorageMetricsPage.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/StorageMetricsPage.razor) | Route `@page "/platform-admin/storage-explorer"` in Admin Portal. |
| **Org Admin Page** | [StorageMetricsPage.razor](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/StorageMetricsPage.razor) | Route `@page "/organization/storage-metrics"` in Daycare Portal. |
| **Service Layer** | [StorageExplorerService.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Infrastructure/Storage/StorageExplorerService.cs) | High-speed Dapper aggregation querying `media_assets` with branch/classroom joins (`classroom_teachers.user_id = m.entity_id`) in ~2 ms. |
| **Contracts & DTOs** | [StorageExplorerDtos.cs](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Application/Services/Storage/StorageExplorerDtos.cs) | Data models for summary, hierarchy rollups, and file records. |

---

## 3. Telemetry Tabs & Features

### A. Branch & Classroom Hierarchy Metrics
* Aggregates total storage used (MB) and file counts per branch.
* Displays individual classrooms within each branch with category counts (Child, Classroom, Teacher, Event) and total MB consumed.

### B. File & Link Explorer (Zero Download Bloat)
* Filterable by category and searchable by file key, branch, or classroom name.
* Displays storage key, content type, size in KB, upload date, and active storage engine badge (`Local Disk` vs `Azure Blob Storage`).
* **Links Only**: Each record provides an asset link (`🔗 Open Link`) pointing to `/api/media/{id}/display`, allowing administrators to open the file in a new tab without downloading images on page load.

---

## 4. 3-Month Photo Retention & Storage Purge Policy

To balance privacy, statutory limits, and storage resource consumption:
* **Retention Window**: 90 days (3 months) for student journey moments, classroom activities, and event photos (`Child`, `General`, `Event`, `ChildJourneyEvidence`). Facility profiles, staff photos, and marketplace catalog media remain protected.
* **Parent Notice**: Parents are advised upon portal login via an auto-fading 10-second banner to download photos before the 3-month expiration.
* **Administrative Purge Workflow**:
  - Available to Platform Admins and Organization Admins in `StorageExplorerView.razor`.
  - **Check Expired (90+ Days)**: Calls `GetRetentionCleanupPreviewAsync` to calculate the eligible photo count and reclaimable megabytes.
  - **Confirmation Dialog**: Requires explicit administrator confirmation prior to purge.
  - **Storage & Database Execution**: Invokes `ExecuteRetentionCleanupAsync` which removes the physical/cloud storage blobs (both display and thumbnail variants) via `IObjectStorage`, deletes database entries from `media_assets`, and writes an immutable entry to the audit log (`Media.RetentionPurge`).


