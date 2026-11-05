---
okf_version: "0.2.0"
type: concept
title: "Portals, Services, and Port Topology"
description: "Network topology, hosting ports, process boundaries, and cross-project references."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - architecture
  - topology
  - ports
  - hosting
---

# Portals, Services, and Port Topology

## Solution Project Map

```
EnjoyEveryday.slnx
├── Presentation Layer (Blazor Interactive Server / Web)
│   ├── EnjoyEveryday.Daycare.Web   (Port 7003)        -> Staff, Teachers, Branch & Org Admins
│   ├── EnjoyEveryday.Admin.Web     (Port 7004)        -> Platform Admins, System Config, Warehouse
│   └── EnjoyEveryday.Parent.Web    (Port 7005)        -> Families, Guardians, Timeline, Messages
├── Backend Services Layer
│   ├── EnjoyEveryday.Api           (Port 5001)        -> REST APIs for mobile and integrations
│   └── EnjoyEveryday.AiAgent       (Port 8000 / ADK)  -> Python Google ADK Agent
├── Shared & Core Libraries
│   ├── EnjoyEveryday.UI.Shared     -> Reusable Razor components (Attendance, DailyCare, Modals)
│   ├── EnjoyEveryday.Application   -> CQRS / Business services & workflows
│   ├── EnjoyEveryday.Domain        -> Entities, repository interfaces, value objects
│   ├── EnjoyEveryday.Infrastructure-> Dapper, PostgreSQL connections, file storage
│   └── EnjoyEveryday.Shared        -> Tenancy, Auth definitions (RoleTemplates, Permissions)
```

## Running Port Map

| Application | Local Port | Primary Audience | Key Entry Routes |
|:---|:---|:---|:---|
| **Daycare Web** | `https://localhost:7003` | Teachers, Branch Admins, Org Admins | `/`, `/today`, `/management/attendance`, `/attendance/monthly`, `/today-activities`, `/planning` |
| **Platform Admin** | `https://localhost:7004` | Platform Admins & Operations | `/tenants`, `/warehouse`, `/catalog`, `/matrix` |
| **Parent Web** | `https://localhost:7005` | Parents & Guardians | `/`, `/today`, `/journey`, `/memories`, `/messages`, `/family` |
| **REST API** | `http://localhost:5001` | External integrations, mobile apps | `/api/v1/...` |
| **PostgreSQL 18** | `localhost:5433` | Database Server | Database: `enjoyeveryday`, User: `postgres` |

## Dependencies & UI Component Sharing
* All common daycare controls (Attendance tables, daily care sections, modals) live in `EnjoyEveryday.UI.Shared`.
* Both `Daycare.Web` and `Admin.Web` reference `UI.Shared`.
* `ee-foundation.css` in `UI.Shared/wwwroot/css/` enforces standard theme variables, typography (`--ee-font`), and standard zoom (`0.82`).

