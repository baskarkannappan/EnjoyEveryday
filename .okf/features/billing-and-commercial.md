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

### 2. Materials & Warehouse Marketplace (`Admin.Web` & `Daycare.Web`)
* **Routes**: `/warehouse-inventory`, `/marketplace-operations`, `/materials-marketplace`
* **Entities**: `material_products`, `material_offerings`, `material_orders`, `material_order_items`, `material_loans`, `inventory_balances`, `inventory_ledger`.
* **Functionality**: Daycare centers order curriculum material kits, craft packs, and sensory play resources fulfilled through central inventory.
* **Dispatch & Reversal Engine**:
  - `DispatchOrderShipmentAsync`: Enforces strict idempotency checks preventing double dispatches; deducts allocated and on-hand stock atomically.
  - `sp_reverse_dispatched_inventory`: Safely restores warehouse on-hand inventory and writes reversal ledger entries upon order cancellation.
* **Modal Dialog Dropdown & Scrollbar Standards (`WarehouseInventoryPage.razor`)**:
  - `select.form-input` explicit height `42px` and `line-height: 1.4` to prevent Chromium Windows dropdown option clipping.
  - `.modal-body` custom scrollbar styling (`scrollbar-gutter: stable; scrollbar-width: thin; max-height: 72vh;`) ensuring smooth scrollability across dialog sizes.
* **Product Image Upload & AVIF Optimization (Max 3 Images)**:
  - Supports uploading up to 3 gallery photos per product.
  - Client-side optimization and AVIF conversion via `window.imageConverter.convertMultipleToAvif` using bounded canvas aspect ratios (`calculateBounds` max 1280x1280).
  - Stored in persistent blob/disk storage via `IMediaService.UploadMediaVariantsAsync`.
  - Persisted in PostgreSQL `material_products.image_urls text[]` and JSON-serialized `image_url text`.
* **Product Card Image Carousel (`MaterialProductCard.razor`)**:
  - Daycare marketplace cards at `/materials-marketplace` display product images with interactive left (`‹`) and right (`›`) navigation arrows and dot indicators cycling through up to 3 gallery images.
* **Marketplace Operations Grid Pagination & Sorting (`MarketplaceOperationsPage.razor`)**:
  - Column sorting toolbar: Date (Newest/Oldest), Amount (High-Low/Low-High), Order #, Organization, and Status with active direction indicators.
  - Full pagination: Page size selector (5, 10, 25, 50), item range counter ("Showing X to Y of Z orders"), and responsive page controls (`«`, `‹`, numbered pills, `›`, `»`).

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

### 10. Organization Subscription Visibility, Plan History & Two-Way Plan Change Workflow
* **Entities**: `organization_plan_history`, `organization_plan_change_requests`, `OrganizationSubscriptionOverview`.
* **Services**: `IOrganizationSubscriptionService` / `OrganizationSubscriptionService`, `IOrganizationSubscriptionRepository` / `OrganizationSubscriptionRepository`, `IEmailNotificationService`.
* **Org Admin Subscription Portal (`Daycare.Web`)**:
  - **Route**: `/management/subscription` (`OrganizationSubscription.razor`), linked from `NavMenu.razor` and `OrganizationBillingView.razor`.
  - **Capabilities**: Displays active plan, regional tariff rate/currency, live quota usage bars (Branches, Classrooms, Children, AI inclusion), available plan comparison cards, and chronological **Plan History** (past and current plans from `organization_plan_history`).
* **Two-Way Upgrade / Downgrade Approval State Machine**:
  1. **Request (`PendingPlatformReview`)**: Org Admin selects a target plan and submits an upgrade/downgrade request. Sends `PlanChangeRequested` email (`SendPlanChangeRequestedAsync`) to Platform Admin (CC Org Admin) and publishes a `PlanChangeRequested` event to the Platform Admin notification bell.
  2. **Proposal (`PendingOrgAcceptance`)**: Platform Admin reviews the request in `Admin.Web` (`/plan-requests`, `PlanChangeRequestsPage.razor`), configures the proposed plan, regional tariff, rate, and effective date, and sends it for Org Admin acceptance via `SendPlanChangeProposalForAcceptanceAsync`.
  3. **Acceptance (`Accepted` / `DeclinedByOrg`)**: Org Admin reviews the proposal banner on `/management/subscription` and clicks **Accept & Activate Plan** (or **Decline**). On acceptance, `sp_assign_organization_regional_plan` atomically updates `organizations` quotas, closes the previous `organization_plan_history` row (`effective_to`), inserts the new history row, and dispatches `SendPlanChangeDecisionConfirmationAsync` to both Org Admin and Platform Admin.

### 11. Platform Admin Bank Account & Payment QR Configuration
* **Entities & Services**: `platform_payment_settings` (`PlatformPaymentSettings`), `IPlatformPaymentSettingsService` / `PlatformPaymentSettingsService`, `QrCodeService`.
* **Platform Admin UI**: `💳 Payment & QR Settings` modal on `Admin.Web` (`/billing`, `BillingPage.razor`) allows Platform Admins to configure `upi_id`, `payee_name`, `bank_name`, `account_number`, `ifsc_code`, or upload a custom static bank QR image (`use_custom_qr_image`, `custom_qr_data_uri`) with live QR preview.
* **Invoice & Email Integration**: `QrCodeService.BuildInvoicePaymentPayload` and `InvoiceDetailsModal.razor` read the active settings dynamically so all generated invoice previews and email QR codes immediately reflect the Platform Admin's updated bank/UPI account.

### 12. Organization Fee Management, Tariff Plans, Flat-Rate Fees, Partial Payments & Manual Verification
* **Migrations**: `131_org_fee_management_invoicing_partial_payments.sql`, `132_org_fee_crud_routines.sql`, `133_publish_tuition_invoice.sql`, `137_fix_fn_verify_submitted_payment_ambiguous_column.sql`.
* **Entities & Repositories**: `OrgFeePlan`, `OrgTariffRule`, `ResolvedTariffResult`, `ChildFeeAdjustment`, `InvoiceInstallment`, `OrgBankAccount`, `OrgPaymentQrCode` (up to 3 slots), `OrgFeeDashboardSummary`; `IOrgFeeRepository` / `OrgFeeRepository`.
* **Service**: `IOrgFeeManagementService` / `OrgFeeManagementService` (registered in `ApplicationServiceRegistration.cs`) enforcing strict server-side `OrganizationAdmin` / `BranchAdmin` (assigned branch scope via `IAccessScopeRepository`) and `Parent` (linked child scope via `IParentRepository`) isolation. Only `OrganizationAdmin` can define tariff layer plans and prices; both `OrganizationAdmin` and authorized `BranchAdmin` can save Draft invoices and publish them (`PublishTuitionInvoiceAsync` -> `fn_publish_tuition_invoice`).
* **Fee Assignment Methods & Precedence**:
  - **Tariff-Based Fee**: Resolves via `fn_resolve_child_tariff` in precedence order `Child > Classroom > Branch > LocationTier > OrgDefault`.
  - **Flat-Rate Fee**: Directly agreed amount per child admission (`FeeMethods.FlatRate`) with explicit fee period (`Monthly`, `Quarterly`, `HalfYearly`, `Yearly`, `OneTime`), independent of tariffs (not auto-split monthly), with audited adjustments in `child_fee_adjustments`.
* **Invoicing, Snapshots & Partial Payments**:
  - `sp_insert_tuition_invoice` stamps `branch_id`, `child_id`, `contract_id`, `fee_method`, and `payment_instructions_snapshot` (bank + active QR codes at issue time).
  - Supports `Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Overdue`, `Cancelled`, `Adjusted`, `Void`, plus planned `invoice_installments` and unplanned partial payments.
  - `sp_recalculate_invoice_balance` credits **only** `status = 'Completed' AND is_verified = TRUE AND is_reversed = FALSE` payments; `PendingVerification` submissions are tracked separately in `pending_verification_amount`.
* **Parent Portal & Manual Verification**:
  - `ParentService.GetChildInvoicesAsync` and `InvoicesListModal.razor` strictly scope invoices to the selected child (`fn_get_child_invoices`), display snapshotted bank details & QR codes, and submit partial/full `QrCode`, `BankTransfer`, or permitted `Cash` payments as `PendingVerification`.
  - Cross-tenant Parent and Daycare staff invoice operations resolve invoices by global ID (`Guid.Empty` passed to `_billingRepository.GetInvoiceByIdAsync` / `fn_get_invoice_by_id`) before validating `invoice.ChildId == childId` (parent security check) or `invoice.OrganizationId == orgId` (staff security check) and retrieving payment instruction snapshots or executing payments.
### 13. Strict Separation of Platform Billing vs. Organization-to-Parent Billing & Org-Owned Razorpay
* **Migration**: `134_separate_platform_and_org_billing_razorpay.sql`.
* **Data & Process Separation**:
  - `invoices.billing_domain` and `payments.billing_domain` enforce `'PlatformSubscription'` (for Platform-to-Org SaaS billing via `fn_get_all_invoices`, `fn_get_invoices_for_organization`, `fn_get_payments_for_organization`) vs. `'OrgToParent'` (for Org-to-Parent childcare billing via `sp_insert_tuition_invoice`, `sp_submit_parent_payment`, `fn_get_child_invoices`, `fn_get_org_tuition_invoices`, `fn_get_org_payments_queue`, `fn_get_org_fee_dashboard`).
  - **Daycare Routes & Permissions**:
    - `/management/billing` (`OrganizationBilling.razor` -> `<OrgFeeManagementBoard />`): Exclusively Organization-to-Parent Billing (`OrganizationAdmin`, `OrganizationOwner`, `BranchAdmin` scoped to authorized branches).
    - `/management/platform-billing` (`PlatformSubscriptionBilling.razor` -> `<OrganizationBillingView />`): Exclusively Platform Subscription Billing (`PlatformAdmin`, `OrganizationOwner`, `OrganizationAdmin` — Branch Admins excluded).
    - `/management/payment-gateway` (`OrgPaymentGatewaySettings.razor`): Org Admin-only configuration for organization-owned Razorpay credentials.
* **Organization-Owned Razorpay Gateway**:
  - Table `org_payment_gateway_settings` stores encrypted `KeySecret` and `WebhookSecret` via `ICredentialProtector` (`DataProtectionCredentialProtector` / ASP.NET Core `IDataProtectionProvider`). Secrets are never returned to browsers or logged in audit entries.
  - Statuses: `NotConfigured`, `ConfigurationPending`, `TestActive`, `LiveActive`, `ValidationFailed`, `Disabled`.
  - `IRazorpayGateway` / `RazorpayGateway` supports per-org credential execution (`CreateOrderWithCredentialsAsync`, `VerifyPaymentSignatureWithSecret`, `VerifyWebhookSignatureWithSecret`).
  - **Parent Portal Integration**:
    - Dedicated `/billing` (`My Billing & Invoices` in `EnjoyEveryday.Parent.Web`) provides child selection, summary cards, monthly billing history, payment history, and **Pay Now** (uses the invoice-owning organization's active Razorpay credentials via `CreateParentRazorpayOrderAsync` / `VerifyParentRazorpayPaymentAsync`, or falls back to manual QR / Bank Transfer / Cash when Razorpay is not configured/disabled).
    - **Installment Schedule Visibility & Selection**:
      - `GetParentChildInvoiceInstallmentsAsync` validates parent-child ownership before returning planned installment schedules (`fn_get_invoice_installments`).
      - Invoices with scheduled installments display an installment indicator in the Monthly Billing History table showing installment count and next due amount/date.
      - Expanding an invoice renders the **Planned Installment Schedule** grid detailing installment number, due date, amount, cumulative paid tracking, and status badges (`Paid`, `Current Due`, `Upcoming`, `Overdue`).
      - When installments exist, default payment amount automatically aligns to the **Next Due Installment** rather than forcing full balance. Quick payment pills allow parents to switch between specific installments, full balance, or custom amounts across both online Razorpay and manual submission.
    - Org-scoped Razorpay webhook endpoint: `POST /api/billing/razorpay/org/{orgId:guid}/webhook` in `Daycare.Web/Endpoints/BillingEndpoints.cs` with idempotent `razorpay_webhook_events` tracking.

### 14. Fee Settlement & Payment Status Reporting Ledger
* **Migration**: `138_fn_get_org_fee_settlement_report.sql`.
* **Stored Function**: `fn_get_org_fee_settlement_report(p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id, p_billing_period, p_status, p_search)` returns child-level settlement aggregates, status badges, and nested `invoices_json` and `payments_json`.
* **Entities & DTOs**: `ChildFeeSettlementReportItem`, `FeeReportInvoiceDto`, `FeeReportInstallmentDto`, `FeeReportPaymentDto`, `FeeSettlementReportSummary`, `SettlementStatuses`.
  - Properties explicitly decorated with `[JsonPropertyName("...")]` to map Postgres snake_case attributes (`invoice_number`, `billing_period`, `effective_payable`, `amount_paid`, `balance_due`, `due_date`, `issue_date`, `payment_date`, `transaction_id`, `payment_reference`, `verified_by`) into C# DTOs.
  - Deserialized using `JsonSerializerOptions` with `PropertyNameCaseInsensitive = true` and `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`.
* **Routes & UI Components**:
  - `/management/fee-reports` (`FeeSettlementReportPage.razor` & `<FeeSettlementReportComponent />`).
  - Embedded inside `/management/billing` under `Overview & Reports` tab (`OrgFeeManagementBoard.razor`).
  - NavMenu link under `Business & Settings`: `Fee & Payment Reports`.
* **Security & Scoping**:
  - Enforces strict tenant and organization boundary (`organization_id = @OrganizationId AND billing_domain = 'OrgToParent'`).
  - Branch Admins are strictly scoped to `allowedBranches` from `IAccessScopeRepository` / `GetCurrentFinancialScopeAsync()`. Org Admins have full organization visibility.
* **Hierarchical Nested Grid Features**:
  - **Dynamic KPI Summary**: Total Invoiced, Total Collected, Outstanding Balance, Pending Verification Amount & collection rate with tabular number alignment.
  - **Filter Bar**: Cascading Branch -> Classroom dropdowns, Child search, Billing Period selector (defaults to `All Periods (Cumulative)` to prevent period filtering mismatches while supporting individual month drill-downs), Status pills (`All`, `Paid`, `Partially Paid`, `Unpaid`, `Overdue`, `Pending Verification`, `Not Invoiced`), and CSV Export.
  - **Expandable Master Row**: Displays child avatar, classroom/branch, invoiced, paid, balance, and settlement badge.
  - **Nested Sub-Tables**:
    - **Invoices**: Breakdown of invoice numbers (`#INV-FEE-...`), billing period, fee method, formatted issue/due dates, total, paid, balance, status, and installment schedules.
    - **Payments**: Transaction timeline with payment method, amount, reference/transaction ID, verification status badge, notes/verified-by details, and direct inline `Verify` / `Reject` action buttons for pending submissions.
  - **Theme & Design System Alignment**:
    - Adheres to EnjoyEveryday design tokens (`--ee-font`, `--surface`, `--surface-soft`, `--border`, `--text`, `--muted`, `--accent`, `--accent-light`, `--accent-dark`, `--shadow`, `--radius: 18px`).
    - Tabular numeric alignment with `font-variant-numeric: tabular-nums` and `CurrencyService.Format(..., forceDecimals: true)`.
    - Date safeguarding (`FormatDate`) preventing default `0001-01-01` displays.
  - **Mobile Responsive Design**:
    - **KPI Metrics**: Stacks dynamically into a 2x2 grid (`repeat(2, 1fr)`) on mobile viewports ($\le 768\text{px}$) with compact 18px font and reduced padding to prevent vertical stretching.
    - **Filter Bar & Status Pills**: `.ee-filter-grid` wraps neatly to single-column inputs with full-width search; `.ee-pill-group` enables smooth horizontal touch scrolling (`overflow-x: auto; -webkit-overflow-scrolling: touch; scrollbar-width: none`).
    - **Nested Grids & Sub-Tables**: Tables enforce minimum safe widths (`min-width: 680px` / `580px`) inside horizontal scrolling containers, preventing crushed text and column overlapping on mobile phones.
    - **Parent Portal Responsive Billing (`Billing.razor`)**: Summary cards collapse into a 2-column grid (`repeat(2, 1fr)`), installment schedule cards expand to 1-column layout, and payment choice pills adapt to 100% width on narrow screens.







