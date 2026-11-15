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
* **Tenant Scoping**:
  * **Platform Admin**: Scopes across all organizations or filters globally via `/platform-admin/storage-explorer`.
  * **Organization Admin**: Strictly scoped to current `TenantContext.TenantId` via `/organization/storage-metrics`.
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

