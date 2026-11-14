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

### 5. Platform Administration Commercial Workflows & Transactional Email Automation
* **Entities**: `organization_invitations`, `email_notification_logs`, `invoices`, `payments`, `subscription_plans`, `tariffs`.
* **Services**: `IPlatformAdminService`, `PlatformAdminService`, `IEmailNotificationService`, `AzureCommunicationEmailService`.
* **Organization Onboarding & 1-Tap Activation Workflow**:
  - When Platform Admin provisions a new daycare organization (`CreateOrganizationWorkflowAsync`) or clicks `✉️ Invite` in `OrganizationsPage.razor`, the system creates a 7-day cryptographically secure invitation token in `organization_invitations` via `fn_create_organization_invitation`.
  - Dispatches a branded transactional email (`SendOrgAdminInvitationAsync`) containing an activation link to `http://localhost:7003/auth/activate?token=...`.
  - The invited Org Admin clicks the link to review organization credentials, choose between 1-tap activation or Google SSO, binds their account to `OrganizationAdmin` role in `user_organization_roles`, and lands directly on their Daycare Operations workspace.
* **Subscription Plan & Tariff Broadcast Notifications**:
  - `NotifySubscribersOfPlanUpdateAsync` and `NotifySubscribersOfTariffUpdateAsync` locate all active subscribed daycare organizations.
  - Dispatches branded email updates (`SendPlanOrTariffUpdateNoticeAsync`) with details on revised base rates, classroom/child quotas, and effective billing cycle dates.
  - Managed via the interactive `✉️ Notify` broadcast modal in `PlansPage.razor`.
* **Automatic Plan & Tariff Assignment Email Triggers**:
  - When Platform Admin assigns or modifies an organization's subscription plan/tariff in `OrganizationsPage.razor` (`AssignPlanAndTariffToOrganizationAsync`), the system automatically compiles and dispatches a detailed confirmation email to the organization administrator (`org.AdminEmail`).
  - When Platform Admin saves updates to a plan definition or pricing in `PlansPage.razor` (`UpdatePlanAsync` / `UpdateTariffAsync`), the system automatically loads active subscribed organizations from PostgreSQL and dispatches the revised plan terms notice to their respective organization administrators.
* **Commercial Invoices & Verified Payment Receipts**:
  - Issuing an invoice (`IssueInvoiceWorkflowAsync`) automatically sends an itemized statement notice (`SendPlatformInvoiceNoticeAsync`) to the organization's administrator with due date, breakdown, and pay link.
  - Verifying a payment (`VerifyPaymentWorkflowAsync`) dispatches an official receipt confirmation (`SendPaymentReceiptConfirmationAsync`) with reconciled payment reference and ledger balance.
* **Transactional Email Delivery Auditing**:
  - Platform administrators have complete operational visibility via the **Transactional Email Delivery Logs** tab in `AuditLogsPage.razor`.
  - Audits recipient email addresses, notification types, delivery statuses (`Sent`, `Delivered`, `Failed`, `Skipped`), error messages, and UTC timestamps directly from `email_notification_logs`.

### 6. Subscription Plan Capacity Quota Enforcement
* **Entities**: `organizations` (`plan_id`, `plan_code`, `plan_name`, `max_branches`, `max_classrooms`, `max_children`, `includes_ai`), `OrganizationQuotaUsage`.
* **Exceptions**: `PlanQuotaExceededException` (`InvalidOperationException`).
* **Database Procedures & Functions**:
  - `sp_update_organization_plan_quota(p_org_id, p_plan_id, p_plan_code, p_plan_name, p_max_branches, p_max_classrooms, p_max_children, p_includes_ai)`: Synchronizes organization capacity limits whenever plan assignment changes in the platform admin console.
  - `fn_get_organization_quota_usage(p_org_id)`: Atomically calculates live usage vs allowed limits for branches, classrooms, and children directly in PostgreSQL.
* **Services**:
  - `IPlanQuotaService` / `PlanQuotaService`: Provides centralized enforcement contracts (`ValidateCanAddBranchAsync`, `ValidateCanAddClassroomAsync`, `ValidateCanAddChildAsync`).
  - `BranchService.CreateBranchAsync`: Validates branch quota before persisting new branch records.
  - `ClassroomService.CreateClassroomAsync`: Validates classroom quota across both single and bulk overloads.
  - `ChildService.CreateChildAsync`: Validates child quota before creating official admissions records.
* **UI Feedback & Guardrails**:
  - `CreateBranch.razor`: Previews remaining branch allowance; disables submission and renders alert banner if quota exceeded.
  - `ClassroomWizard.razor`: Previews classroom limit; catches `PlanQuotaExceededException` and presents actionable guidance.
  - `ChildEnrollmentWizard.razor`: Pre-validates child quota on wizard launch; blocks progression with informative error banners when maximum child limit is reached.
  - Consistent message pattern: `"You are exceeding your quota limit. Your current plan '{planName}' allows a maximum of {maxAllowed} {resourceType} (currently using {currentCount}/{maxAllowed}). Please upgrade your subscription plan to add more."`



