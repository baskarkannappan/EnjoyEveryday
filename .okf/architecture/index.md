---
okf_version: "0.2.0"
type: bundle_index
title: "Architecture & Domain Model"
description: "Core architectural boundaries, organizational structures, multi-tenancy isolation, UI design system, and domain aggregates."
parent: "../index.md"
status: active
generated: "2026-11-05"
tags:
  - architecture
  - multi-tenancy
  - domain-entities
  - ports
  - ai-rules
  - design-system
---

# Architecture & Domain Model

This bundle details the domain architecture of EnjoyEveryday, multi-tenant boundaries, and how organizations, branches, classrooms, staff, and children relate.

## Concepts in this Bundle

* **[domain-model.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/architecture/domain-model.md)**: Complete organizational entity hierarchy (`Organization` -> `Branch` -> `Classroom` -> `Teacher` & `Child`).
* **[multi-tenancy.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/architecture/multi-tenancy.md)**: Tenant isolation model, database tenancy column rules, and `TenantContext` resolution.
* **[ui-design-system.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/architecture/ui-design-system.md)**: Scandinavian-crafted aesthetic, sage green color tokens, typography, and standard card/button patterns.
* **[portals-and-services.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/architecture/portals-and-services.md)**: Network topology, hosting ports (7003, 7004, 7002, 5001), and project dependency boundaries.
* **[ai-agent-architecture.md](file:///c:/MyDrive/ProjectDrive/EnjoyEveryDay/.okf/architecture/ai-agent-architecture.md)**: Rules-first philosophy, bounded Google ADK integration, cost control, and deterministic fallbacks.
