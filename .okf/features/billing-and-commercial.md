---
okf_version: "0.2.0"
type: concept
title: "Billing, Subscriptions & Marketplace Orders"
description: "Organization invoices, rate cards, marketplace curriculum kits, and warehouse inventory."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - billing
  - invoices
  - commercial
  - subscriptions
  - marketplace
---

# Billing, Subscriptions & Marketplace Orders

## Core Modules & Routing

### 1. Daycare Billing (`Daycare.Web`)
* **Routes**: `/management/billing`, `/billing/invoices`
* **Access**: Organization Admin exclusive.
* **Entities**: `invoices`, `organization_billing_settings`, `payments`.
* **Services**: `BillingService` manages fee structures (full-time, part-time, sibling discounts) and automated monthly billing cycles.

### 2. Materials & Warehouse Marketplace (`Admin.Web`)
* **Routes**: `/warehouse`, `/catalog`, `/orders`
* **Entities**: `material_products`, `material_offerings`, `marketplace_orders`.
* **Functionality**: Daycare centers order curriculum material kits, craft packs, and sensory play resources fulfilled through central inventory.

