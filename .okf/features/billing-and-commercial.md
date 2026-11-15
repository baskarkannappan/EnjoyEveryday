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

### 8. Multi-Currency Organization Operations & Invoice Lifecycle
* **Entities**: `organizations.currency`, `tenant_preferences.currency`, `invoices.currency`, `organization_currency_history`.
* **Currency Architecture**:
  - Each organization operates in its assigned currency (`INR` default, `AUD`, `USD`, `JPY`, `CAD`, `EUR`, `GBP`, `SGD`).
  - **New Invoices**: When invoices (manual platform fees, automated recurring subscription invoices, or monthly tuition billing) are generated, the invoice stamps the organization's current active currency into `invoices.currency`.
  - **Historical Invoices**: Old invoices permanently preserve their original issued currency (`inv.Currency`) and are rendered via `CurrencyService.Format(inv.TotalAmount, inv.Currency)` across all invoice tables, statements, and parent portals, preventing retro-active currency distortion.
  - **Currency Change History**: Managed via `public.organization_currency_history` and `sp_assign_organization_regional_plan(p_org_id, p_region_id, p_plan_id, p_tariff_id, p_currency)`. Every time an organization switches currency, an audit record is stored with `previous_currency`, `new_currency`, `changed_by`, `reason`, and `effective_from` timestamp, visible directly in the commercial plan modal in `OrganizationsPage.razor`.

  - `AzureCommunicationEmailService.cs`: Automatically compiles and embeds high-contrast PNG QR codes directly into invoice notice emails (`SendPlatformInvoiceNoticeAsync`).
  - `InvoiceDetailsModal.razor`: Renders scannable QR Code remittance block on printable PDF invoice statements and live preview modal in Daycare Portal (`OrganizationBillingView.razor`) and Platform Admin (`BillingPage.razor`).
* **Payload Encoding**:
  - Automatically formats NPCI UPI payment payloads (`upi://pay?pa=billing@enjoyeveryday&pn=...&am=...&cu=INR&tn=Invoice%20...&tr=...`) for instant settlement via Google Pay, PhonePe, Paytm, or BHIM when currency is INR.
  - Formats universal mobile checkout URLs for international card payments, Apple Pay, and Google Pay.
* **Cost & Performance Architecture**:
  - $0/month cost: 100% in-process execution with zero external 3rd-party QR APIs, zero cloud egress overhead, and full offline resilience.

### 8. Location-Based Regional Pricing Zones & Tariff Layers
* **Architecture Pattern**: Decoupled **Base Plan + Regional Tariff Overlay**.
  - **Base Plans (`SubscriptionPlan`)**: Maintain quota allowances (Max Branches, Max Classrooms, Max Children, AI Features) and global standard base rates once across the entire platform.
  - **Pricing Regions (`pricing_regions`)**: Define geographic pricing layers (e.g. `REG-TIER1-METRO`, `REG-TIER2-GROWTH`, `REG-SUBURBAN-EMERGING`, `REG-GLOBAL-DEFAULT`) with assigned currency (`USD`, `INR`, `EUR`) and JSON arrays of matched municipal cities/states.
  - **Tariff Layer Pricing Matrix (`region_plan_tariffs`)**: Overlays customized localized rates (e.g., Bloom Plan is $399 in Tier 1 Metros, $299 in Tier 2 Growth Hubs, $199 in Suburban/Regional, and $299 Global Standard).
* **Database Stored Function & Procedure**:
  - `fn_resolve_pricing_region(p_location)`: Resolves an organization's freeform location string (e.g., "Austin, TX", "New York, NY", or "Mumbai") against `matched_cities` and `matched_states`, automatically falling back to `is_default = true`.
  - `sp_assign_organization_regional_plan(p_org_id, p_region_id, p_plan_id, p_tariff_id)`: Atomically updates an organization's assigned region, plan quotas, and tariff schedule in PostgreSQL.
* **Platform Admin UI & Experience**:
  - **`PlansPage.razor`**: Includes an interactive **"Regional Tariff Layers"** tab side-by-side with Base Subscription Plans, displaying all active pricing zones, matched cities/states tags, and full localized tariff schedules.
  - **`OrganizationsPage.razor`**:
    - **Live Location Matcher**: Real-time detection banner auto-selects the matching pricing zone as soon as the administrator enters or edits the organization location.
    - **Tariff Zone Override**: Administrators can manually pick or override the assigned region from a dropdown.
    - **Dynamic Regional Rates**: The plan selection and billing tariff schedules dynamically recalculate and preview localized prices for the selected region.
    - **Directory Visibility**: Organization directory table displays the 📍 region badge alongside the active plan and schedule.

### 9. Global Base Currency Standardization to INR & Multi-Currency Hierarchy
* **Architecture Pattern**: **Global Base Currency INR (`₹`) with Historical Invoice Immutability & Audit Trail**.
  - **Standard Global Base Currency**: Standardized base operating currency across all pricing regions (`pricing_regions`), regional tariff schedules (`region_plan_tariffs`), subscription plans, tenant preferences, and daycare organizations is **INR** (`₹`).
  - **Single Source of Truth (`organizations.currency`) with Cascading Inheritance**:
    - Centralized in `sp_assign_organization_regional_plan(p_org_id, p_region_id, p_plan_id, p_tariff_id, p_currency)`.
    - Automatically syncs to `tenant_preferences.currency` and is returned in `fn_get_organization_hierarchy`, `fn_get_parent_authorized_children`, `fn_get_all_organizations`, and `fn_get_platform_organizations_overview`.
    - All subsequent child entities (campuses, branches, classrooms, teacher workspaces, parent portal tuition invoices, and receipt vouchers) strictly inherit and format in the organization's specific currency.
  - **Historical Invoice Currency Immutability**:
    - Every generated invoice (tuition, subscription, manual draft) is stamped with the active organization currency at invoice creation time.
    - Historical invoices permanently preserve their issued currency (`inv.Currency`) and format accordingly via `@inv.TotalAmount.ToCurrency(inv.Currency)`, even if the organization later changes its base currency.
  - **Currency Change Audit Trail (`organization_currency_history`)**:
    - Every change to an organization's operating currency is atomically recorded in `public.organization_currency_history` with `previous_currency`, `new_currency`, `changed_by`, `reason`, and `effective_from`.
    - Displayed directly inside the Assign Plan & Tariff modal in `OrganizationsPage.razor`.
  - **Unified Currency Formatter (`ICurrencyService` / `CurrencyService`)**:
    - Located in `src/EnjoyEveryday.UI.Shared/Services/`.
    - Supports culture-specific zero-decimal rules (e.g., INR `₹12,500` with Indian lakh grouping, JPY `¥10,000`, USD `$150.00`).
    - Registered as a singleton in `UiSharedServiceRegistration.cs` across all portals.






