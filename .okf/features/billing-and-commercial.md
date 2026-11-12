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
* **Entities**: `invoices`, `invoice_line_items`, `organization_billing_settings`, `payments`, `razorpay_webhook_events`.
* **Services**: `BillingService` manages fee structures (full-time, part-time, sibling discounts) and automated monthly billing cycles.
* **Online Payment Gateway & Reconciliation**:
  - `IRazorpayGateway` / `RazorpayGateway`: Generates gateway orders and cryptographically verifies webhook signatures (`X-Razorpay-Signature`).
  - `ProcessRazorpayWebhookAsync`: Idempotently reconciles incoming webhook events (`payment.captured`, `order.paid`). Deduplicates against `razorpay_webhook_events` to prevent double-crediting invoices.
  - Updates `invoices` balance, logs `payments` record, and marks status as `Paid` or `PartiallyPaid`.

### 2. Materials & Warehouse Marketplace (`Admin.Web`)
* **Routes**: `/warehouse`, `/catalog`, `/orders`
* **Entities**: `material_products`, `material_offerings`, `marketplace_orders`, `material_inventory_items`, `material_inventory_transactions`.
* **Functionality**: Daycare centers order curriculum material kits, craft packs, and sensory play resources fulfilled through central inventory.
* **Dispatch & Reversal Engine**:
  - `DispatchOrderShipmentAsync`: Enforces strict idempotency checks preventing double dispatches; deducts allocated and on-hand stock atomically.
  - `sp_reverse_dispatched_inventory`: Safely restores warehouse on-hand inventory and writes reversal ledger entries upon order cancellation.

### 3. Automated Monthly Recurring Tuition Billing
* **Background Service**: `MonthlyTuitionBillingBackgroundService` in `Daycare.Web` executes on the 1st of each month (off-peak 02:00–04:00 AM window).
* **Manual Trigger**: `⚡ Run Tuition Billing` in `Admin.Web/Components/Pages/Billing/BillingPage.razor`.
* **Contract Resolution**: Queries `fn_get_active_family_contracts(p_tenant_id, p_branch_id)` for active contracts with `auto_invoice = true`.
* **Idempotency Guard**: Deduplicates by contract ID and billing period (`Contract:{id}|Period:{yyyy-MM}` in invoice metadata) ensuring no family is double-billed.
* **Proration Mechanics**: Automatically calculates mid-month active day ratios if enrollment start date or termination date falls mid-month.
* **Notifications**: Dispatches branded parent invoice emails via `IEmailNotificationService` with links to the Parent Portal.

### 4. Cross-Application Business Events & Platform Admin Notifications
* **Entities**: `platform_business_events`, `platform_admin_notifications`, `email_notification_logs`.
* **Event Service**: `IPlatformBusinessEventService` / `PlatformBusinessEventService` provides centralized event publication and notification tracking across all portals.
* **Tracked Commercial Events**:
  - `MaterialOrderCreated`, `MaterialOrderDispatched`, `MaterialOrderCancelled`: Generated in `MaterialMarketplaceService`.
  - `ExperienceAcquired`, `ExperiencePurchased`: Generated in `ExperienceMarketplaceService.AcquireEntitlementAsync`.
  - `SubscriptionInvoicePayment`: Generated in `BillingService` for manual, cash, online Razorpay, and webhook payments.
* **Platform Admin Notification Bell**: Integrated into `MainLayout.razor` with live unread count pill badge, dropdown list, "Mark read", "Mark all read", and direct action links.
* **Platform Dashboard Activity Feed**: Real-time cross-organization commercial feed in `PlatformDashboard.razor` displaying transactions, organizations, amounts, and direct drill-downs.
* **Marketplace Operations Filtering**: `MarketplaceOperationsPage.razor` defaults to `All Orders` with full text search and organization dropdown filters.
* **Parent Onboarding & Consent Mandatory Org Admin CC**: All parent onboarding stage emails (`SendOnboardingStageNotificationAsync`), consent invitations (`SendConsentInvitationAsync`, `SendConsentReminderAsync`), and signature confirmations (`SendConsentSubmittedConfirmationAsync`) automatically resolve and CC the organization's primary administrator via `fn_get_org_admin_email_for_org`.

