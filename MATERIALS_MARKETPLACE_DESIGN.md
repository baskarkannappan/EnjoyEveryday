# EnjoyEveryday — Materials Marketplace, Warehouse Management & Central Account Book Architecture

**Document Version**: 2.0  
**Status**: Approved Architecture & Implementation Blueprint  
**Target Projects**: `EnjoyEveryday.Domain`, `EnjoyEveryday.Infrastructure`, `EnjoyEveryday.Application`, `EnjoyEveryday.Daycare.Web`, `EnjoyEveryday.Admin.Web`, `EnjoyEveryday.UI.Shared`

---

## 1. Confirmed Business Decisions (Mandatory)

### Rule 1: Product Pricing
Support two distinct pricing models:
1. **Standard Products — Fixed Price**:
   - The Platform Admin publishes products with fixed prices in the catalog.
   - Organization Admins order eligible, in-stock items at displayed prices.
   - The server calculates authoritative totals (items, quantities, taxes, delivery charges).
   - Price updates by Platform Admin never retroactively alter already confirmed orders.
2. **Special Requests — Quotation Required**:
   - Custom quantities, non-catalog requests, or large orders enter the Platform Admin review queue.
   - Platform Admin prepares an itemized quotation with item prices, delivery charges, taxes, rental fees, and deposits with an explicit validity period (`QuotationValidUntil`).
   - The Organization Admin explicitly accepts or declines the quotation.
   - Acceptance records the acting user, timestamp, and quotation version.
   - Only after explicit acceptance does the request transition to a confirmed order.

### Rule 2: Organization Pays Delivery Charges
- Delivery charges are paid by the purchasing/borrowing daycare organization.
- Delivery charges are calculated on the server and displayed separately from material prices.
- Stored authoritatively with the order and itemized on invoices and financial ledgers.
- Changes in delivery charges (due to address or volume adjustments) require explicit organization re-confirmation.

### Rule 3: Borrowing — Rental Fee Plus Optional Refundable Deposit
- Borrowed materials are reusable assets owned/controlled by the Platform Admin.
- Financial components are strictly separated:
  1. **Rental Fee**: Earned revenue for using the materials during the loan term.
  2. **Refundable Deposit**: Balance sheet liability (**money held temporarily**, never treated as earned revenue upon receipt).
  3. **Delivery Charge**: Organization's transportation expense.
- After return quality inspection:
  - Full refund approved when items meet agreed condition.
  - Authorized deduction requires documented inspection evidence (damaged/missing items), approver identity, and policy reference.
  - Deposit refund has its own distinct financial status, audit trail, and gateway/manual disbursement record.

---

## 2. Seven Core Operational Workflow Rules

1. **Independent Lifecycles**: Do not conflate order, payment, delivery, and loan statuses into one field. An order can be `Dispatched`, payment `Paid`, shipment `InTransit`, and loan `ReturnNotYetDue`.
2. **Mixed Orders**: An order may contain both purchases (craft paper) and loans (reusable sorting trays). Track purchase vs borrow rules per line item.
3. **Delivery Date as Anchor**: Organizations select the planned activity and required date. The system validates stock availability and delivery feasibility against that date.
4. **Reusable Kit Tracking**: Track kits individually with unique identifiers, condition history, and cleaning status.
5. **Cash & Manual Payments as First-Class Records**: Support controlled manual receipt recording (amount, date, method, receiver, receipt #, and verification) alongside Razorpay gateway webhooks.
6. **Predictable Recurring Orders**: Show upcoming scheduled orders, estimated amounts, and stock readiness. Background jobs are idempotent and never double-charge.
7. **First-Class Exception Handling**: Built-in workflows for stock shortages, late deliveries, damaged returns, missing items, payment discrepancies, and cancellations.

---

## 3. Central Account Book & Financial Investigation Architecture

### Vision & Objective
Provide the Platform Admin with a single financial control center (`/account-book`) to audit, reconcile, and investigate money across both **Platform Subscription Fees** and the **Materials Marketplace** without digging through individual orders.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        FINANCIAL OVERVIEW · CURRENT PERIOD                             │
├────────────────────┬────────────────────┬────────────────────┬─────────────────────────┤
│   Money Received   │    Outstanding     │Needs Investigation │     Refunds Pending     │
│     ₹4,82,500      │      ₹38,400       │         7          │         ₹12,600         │
│ Confirmed Receipts │  Unpaid Invoices   │ Exceptions Review  │   Awaiting Completion   │
└────────────────────┴────────────────────┴────────────────────┴─────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────────────┐
│  Account Book                                                                          │
│  [All Transactions]   [Needs Attention (7)]   [Unreconciled]                           │
│  Filter by Category: [All Categories ▼]   Filter by Status: [All Statuses ▼]           │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ • Platform fee       +₹12,500  [Paid]            PAY-10482 · Bluebird Daycare          │
│ • Materials          +₹8,600   [Payment Pending] MAT-00821 · Little Steps Daycare      │
│ • Rental deposit     +₹3,000   [Held]            DEP-00194 · Bluebird Daycare          │
│ • Refund             -₹2,400   [Failed]          REF-00217 · Happy Nest Daycare        │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Investigation Workspace Checklist
Selecting any transaction opens an interactive investigation drawer displaying:
1. **Invoice & Order Matching**: Verifies whether an invoice, payment attempt, and receipt exist.
2. **Amount Discrepancy Check**: Compares expected amount vs actual received amount.
3. **Gateway / Bank Evidence**: Inspects Razorpay Payment ID, signature, webhook payload, or cash receipt number.
4. **Duplicate & Retry Detection**: Inspects idempotency keys and retry timestamps.
5. **Timeline & State Transitions**: Chronological log of state changes, actors, and error codes.
6. **Tenant & Allocation Check**: Verifies payment was allocated to the correct organization and account.
7. **Resolution Actions**:
   - Verify & Reconcile Payment
   - Record Manual Cash Receipt
   - Release / Deduct Refundable Deposit
   - Process / Retry Refund
   - Add Internal Investigation Note

---

## 4. System Vision & Operating Model

EnjoyEveryday Materials Marketplace connects daycare educational activities directly with physical materials supply.
- **Seller & Warehouse Operator**: EnjoyEveryday Platform Admin team.
- **Buyers & Borrowers**: Daycare Organizations (represented by Organization Admins, Branch Admins, and Teachers).
- **Core Value Proposition**: Daycares do not have to purchase every expensive resource. They can:
  1. **Buy** consumable supplies (paper, crayons, glue, paints, sensory liquids) to keep.
  2. **Borrow** reusable activity kits, STEM sets, sensory trays, and educational equipment for a set period and return them.
  3. **Schedule Recurring Supplies or Rentals** for automated replenishment or recurring rental cycles.
  4. **Check Material Readiness in Experience Planning** to ensure every planned classroom activity has its materials on hand before execution.

---

## 2. Shared Order Workflow & Role-Specific Views

Both Platform Admin and Daycare Organization Admin track the **exact same order** via a shared state machine and persistent event timeline, but each role has distinct, authorized actions:

```
[ Daycare Organization Admin ]                      [ Platform Admin / Warehouse ]
             │                                                     │
             ▼                                                     │
    1. Submit Request / Order                                      │
       (Standard Catalog or Quotation Path)                        │
             │                                                     ▼
             │                                            2. Review Request
             │                                               • Approve / Reject
             │                                               • Set Quotation / Loan Terms
             ▼                                                     │
    3. Accept Quotation & Pay                                      │
       (Razorpay or Authorized Terms)                              │
             │                                                     ▼
             │                                            4. Stock Allocation & Packing
             │                                               • Allocate warehouse stock
             │                                               • Pick & pack
             │                                               • Dispatch with carrier & tracking
             ▼                                                     │
    5. Track Shipment & Confirm Receipt                            │
       • View carrier & tracking                                   ▼
       • Confirm delivery or report issue                 6. Delivery Confirmed
             │                                                     │
   ┌─────────┴─────────────────────────┐                           │
   │                                   │                           │
[ Purchased Items ]            [ Borrowed Items ]                  │
   • Consumed locally             • In Use                         │
   • Order Completed              • Return Due                     ▼
                                  • Return In Transit ──► 7. Receive Return at Warehouse
                                                                   │
                                                                   ▼
                                                          8. Return Quality Inspection
                                                             • Good / Clean ──► Restored to Available Stock
                                                             • Needs Cleaning ──► Under Cleaning
                                                             • Damaged / Missing ──► Resolution & Deposit Review
```

---

## 3. Database Schema Deliverables (`052_materials_marketplace_and_warehouse.sql`)

### A. Catalog & Pricing
1. **`material_products`**:
   - `id UUID PRIMARY KEY`, `sku VARCHAR(64) UNIQUE`, `name VARCHAR(200)`, `description TEXT`, `category VARCHAR(100)`, `image_url VARCHAR(500)`
   - `material_type VARCHAR(50)` (`Consumable`, `Reusable`, `ActivityKit`)
   - `safety_instructions TEXT`, `age_min_months INT`, `age_max_months INT`, `usage_restrictions TEXT`
   - `cleaning_instructions TEXT`, `return_policy TEXT`, `is_active BOOLEAN DEFAULT TRUE`
2. **`material_offerings`**:
   - `id UUID PRIMARY KEY`, `material_product_id UUID REFERENCES material_products(id)`
   - `allow_purchase BOOLEAN`, `purchase_price NUMERIC(10,2)`
   - `allow_borrow BOOLEAN`, `rental_fee NUMERIC(10,2)`, `refundable_deposit NUMERIC(10,2)`
   - `default_loan_duration_days INT`, `max_loan_duration_days INT`, `currency VARCHAR(10) DEFAULT 'INR'`

### B. Warehouse & Inventory Ledger
3. **`inventory_locations`**:
   - `id UUID PRIMARY KEY`, `code VARCHAR(50) UNIQUE`, `name VARCHAR(150)`, `address TEXT`, `is_central_warehouse BOOLEAN DEFAULT TRUE`
4. **`inventory_balances`**:
   - Multi-status stock: `available_stock`, `reserved_stock`, `dispatched_stock`, `on_loan_stock`, `under_inspection_stock`, `under_cleaning_stock`, `damaged_stock`, `missing_stock`, `retired_stock`, `total_stock`
   - Row-level locking & constraints ensure: Total = sum(Status Balances) and Available >= 0.
5. **`inventory_ledger`**:
   - Append-only audit log of every stock movement: `transaction_type` (`Receipt`, `Reservation`, `ReservationRelease`, `DispatchPurchase`, `DispatchLoan`, `ReturnReceived`, `InspectionApproved`, `InspectionDamaged`, `InspectionMissing`, `CleaningComplete`, `Adjustment`), quantity deltas, actor metadata, notes.

### C. Shared Order Workflow
6. **`material_orders`**:
   - `order_number VARCHAR(50) UNIQUE`, `tenant_id`, `organization_id`, `branch_id`, `classroom_id`, `planned_experience_id`
   - `workflow_status`: `RequestSubmitted` -> `UnderReview` -> `QuotationOffered` -> `AwaitingConfirmation` -> `Confirmed` -> `PaymentPending` -> `PaymentConfirmed` -> `StockAllocated` -> `PickingPacking` -> `Dispatched` -> `InTransit` -> `Delivered` -> `ReceiptConfirmed` -> `Completed` (or `Rejected`, `Cancelled`, `DeliveryException`)
   - `payment_status`: `Pending`, `Completed`, `Partial`, `Waived`, `Refunded`
   - `fulfilment_status`: `Unfulfilled`, `Allocated`, `Dispatched`, `Delivered`, `ReceiptConfirmed`
   - Pricing snapshots: `total_purchase_amount`, `total_rental_fee`, `total_deposit`, `shipping_fee`, `tax_amount`, `total_amount`, `currency` (`INR`)
   - Delivery details: `delivery_address`, `contact_name`, `contact_phone`, `required_delivery_date`
7. **`material_order_items`**:
   - Line-item breakdown with `fulfilment_mode` (`Purchase` vs `Borrow`), `quantity`, unit prices, deposit, and return requirements.
8. **`material_order_history`**:
   - Event timeline table: `event_type`, `previous_status`, `new_status`, `actor_user_id`, `actor_email`, `actor_role` (`DaycareOrganization` / `PlatformAdmin` / `System`), `description`, `notes`, `is_internal_note` (filtered from daycare).

### D. Shipments, Loans, Inspections & Recurring Schedules
9. **`material_shipments`**:
   - `carrier_name`, `tracking_reference`, `dispatch_date`, `estimated_delivery_date`, `actual_delivery_date`, `delivery_status`, `proof_of_delivery_notes`.
10. **`material_loans`**:
    - Reusable kit lifecycle: `quantity_issued`, `due_date`, `return_status` (`Reserved`, `Dispatched`, `Delivered`, `InUse`, `ReturnDue`, `ReturnInTransit`, `Received`, `Inspected`, `AvailableAgain`, `Damaged`, `Missing`), `deposit_amount`, `deposit_status`.
11. **`material_return_inspections`**:
    - `returned_quantity`, `inspected_by_user_id`, `condition` (`Excellent`, `Good`, `RequiresCleaning`, `Damaged`, `MissingParts`), `quantity_approved_for_stock`, `quantity_requiring_cleaning`, `quantity_damaged`, `quantity_missing`, `deposit_disposition`, `deposit_deduction_amount`.
12. **`material_recurring_schedules`**:
    - `schedule_type` (`RecurringPurchase` vs `RecurringLoan`), `frequency` (`Weekly`, `BiWeekly`, `Monthly`), `next_run_date`, `status` (`Active`, `Paused`, `Cancelled`).

---

## 4. Experience Planning Integration Flow

1. Teacher or Admin opens **Experience Planning** (`/planning`).
2. Teacher clicks an experience or opens the planning drawer/modal.
3. The system parses required materials from `ExperienceDna.Materials`.
4. The system executes a **Material Readiness Check**:
   - Highlights materials already in classroom stock.
   - Identifies missing consumables and reusable kits.
   - Provides a 1-click **"Order via Marketplace"** button that pre-populates a Material Order linked to the specific planned activity and classroom.
5. In Experience Planning, the schedule item visually displays a badge showing its material readiness:
   - Ready for Activity
   - Materials Dispatched (In Transit)
   - Materials Confirmed / Packing
   - Materials Requested
   - Return Due After Activity

---

## 5. Security, Multi-Tenant Isolation & Role Authorization

- **Server-Side Enforcement**: All operations validate `TenantId` and `OrganizationId` against the authenticated caller context (`IResourceAuthorizer` and `AppIdentityContext`).
- **Platform Admin Boundaries**: Only `PlatformAdmin` users can manage central warehouse stock, issue quotations, dispatch shipments, verify manual payments, and record return inspections.
- **Organization Boundaries**: Daycare users cannot modify product prices, view other daycares' orders, inspect other daycares' returns, or alter ledger balances.
- **Data Protection**: No child personal data is exposed in shipping manifests or payment metadata.

---

## 6. Phase Execution Roadmap

| Phase | Scope | Status |
|:---|:---|:---|
| **Phase 1** | Repository Inspection & Architecture Design | **DONE** |
| **Phase 2** | Database Migration, Domain Entities, Repository & Ledger Services | **NEXT** |
| **Phase 3** | Shared Order Workflow Engine, Order Placement (Paths A & B), Timeline Event Store | Pending |
| **Phase 4** | Platform Admin Operations: Warehouse, Catalog, Review Queue, Dispatch, Inspections | Pending |
| **Phase 5** | Daycare Organization Admin Marketplace: Catalog Browse, Cart, Orders, Tracking, Returns | Pending |
| **Phase 6** | Payments & Cash Receipts Integration (Razorpay + Manual Receipts) | Pending |
| **Phase 7** | Recurring Supplies & Scheduled Rentals Engine | Pending |
| **Phase 8** | Experience Planning Integration: Material Readiness Check & Activity Order Linking | Pending |
| **Phase 9** | Security Verification, Solution Build & Final Handover Report | Pending |
