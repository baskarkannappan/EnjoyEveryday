# Organization Fee Management, Tariff Plans, Flat-Rate Fees, Invoicing and Partial Payments

## 1. Objective

Build a flexible fee-management and invoicing module for EnjoyEveryday.

Every daycare organization must be able to configure its own fee plans, tariff structures, pricing rules, and payment arrangements.

The system must support two fee-setting methods:

1. Tariff-based fee calculation.
2. Direct flat-rate fee assignment.

Parents must be able to pay invoices fully or partially using bank transfer, QR payment, or cash.

Daycare administrators and authorized staff must manually verify payments. The system must calculate outstanding balances automatically and maintain a complete financial history.

This requirement must extend the existing finance and billing functionality. Do not create a duplicate billing system.

---

## 2. Important Business Requirements

The system must support:

- Organization-specific fee plans and tariffs.
- Different fees for different branches.
- Location-based pricing, such as Tier 1 and Tier 3 cities.
- Classroom-specific and child-specific fees.
- Direct flat-rate fee assignment for individual admissions.
- Monthly, quarterly, half-yearly, yearly, and one-time payment arrangements.
- Full payment and partial payment.
- Multiple payments against the same invoice.
- Automatic outstanding balance calculation.
- Manual payment verification by authorized daycare staff.
- Organization and branch-level financial reports.
- Complete audit history.

All amounts, plans, rules, and payment arrangements must be configurable. Do not hardcode example prices or tariff values.

---

## 3. Two Fee Assignment Methods

When creating or managing a child's admission fee, the authorized administrator must be able to choose one of two methods.

### Method A: Tariff-Based Fee

The administrator selects an existing fee plan or tariff.

The system calculates the fee using the configured rules.

Example:

| Location category | Monthly fee |
|---|---:|
| Tier 1 | ₹1,500 |
| Tier 2 | Organization-defined |
| Tier 3 | ₹1,000 |

These are illustrative values only.

The organization can configure different tariffs based on:

- Location category or city tier.
- Organization.
- Branch.
- Classroom.
- Child-specific agreement.
- Billing period.
- Other supported fee categories.

### Method B: Direct Flat-Rate Fee

The administrator must also have an option called **Flat-Rate Fee**.

This option allows an Organization Admin or authorized Branch Admin to enter a directly agreed fee amount without selecting or calculating a tariff.

Example:

A parent negotiates an annual admission fee of ₹10,000 for a particular child.

The administrator enters:

- Child: Selected child.
- Fee method: Flat Rate.
- Agreed amount: ₹10,000.
- Currency: INR.
- Fee period: One year.
- Effective date: Admission start date.
- Payment arrangement: Full payment or partial payment.
- Remarks: Optional explanation of the agreed fee.

The system must accept this amount as the agreed fee for that admission.

It must not automatically replace the flat rate with a tariff calculation.

### Flat-Rate Business Rules

1. Flat-rate fees must be assignable to an individual child's admission.
2. Organization Admin can assign flat rates to children within the organization.
3. Branch Admin can assign flat rates only to children belonging to authorized branches.
4. The flat-rate amount must be independent of the organization's normal tariff calculation.
5. The administrator must specify the period covered by the flat rate.
6. The administrator can define whether the fee is one-time, annual, or for another supported period.
7. The system must retain the agreed amount even if the organization's tariffs change later.
8. Changes to an agreed flat rate must require an authorized adjustment process.
9. The system must preserve the previous amount, new amount, reason, effective date, and user who made the change.
10. Flat-rate assignments must be visible in the child's financial history.

**Important:** A flat-rate fee is an agreed amount, not necessarily a monthly tariff. The system must not automatically divide it into monthly charges unless the administrator explicitly chooses that billing arrangement.

---

## 4. Fee Plan and Tariff Configuration

Each organization must manage its own fee plans.

The configuration must support:

- Plan name and description.
- Location category or city tier.
- Branch.
- Classroom.
- Fee category.
- Billing frequency.
- Amount.
- Effective date.
- Expiry date.
- Active/inactive status.
- Applicable child categories, where required.

Supported billing frequencies:

- Monthly.
- Quarterly.
- Half-yearly.
- Yearly.
- One-time or full-period payment.

Organizations must be able to create, update, activate, deactivate, and manage their own plans.

Different organizations must not share or access each other's financial configuration.

---

## 5. Fee Selection During Child Admission

During child onboarding, the system must allow the authorized administrator to configure the child's fee arrangement.

The administrator must be able to:

1. Select the organization.
2. Select the branch.
3. Select the classroom.
4. Select the child.
5. Choose Tariff-Based Fee or Flat-Rate Fee.
6. Configure the amount and billing period.
7. Select the payment arrangement.
8. Review the calculated or agreed fee.
9. Confirm the fee arrangement.

For tariff-based fees, the system calculates the amount using the configured tariff rules.

For flat-rate fees, the administrator directly enters the agreed amount.

The selected fee arrangement must be associated with the child's admission and retained as financial history.

### Fee Precedence

When calculating a tariff-based fee, use the following proposed precedence:

1. Child-specific agreed tariff.
2. Classroom-specific tariff.
3. Branch-specific tariff.
4. Location-category tariff.
5. Organization default tariff.

The system must identify the applicable rule and show the administrator how the final amount was calculated.

A direct flat-rate assignment is a separate fee-setting method and takes precedence over tariff calculation for that admission.

---

## 6. Partial Payment and Installment Management

Partial payment is a mandatory feature.

Parents may not always be able to pay the entire fee at once. The system must allow them to pay any permitted amount against an outstanding invoice.

Example:

A child's annual flat-rate fee is ₹10,000.

The parent makes the following payments:

| Payment | Amount |
|---|---:|
| First payment | ₹3,000 |
| Second payment | ₹2,000 |
| Third payment | ₹1,000 |
| Fourth payment | ₹4,000 |
| Total paid | ₹10,000 |

The system must calculate the balance after every verified payment.

### Required Calculation

**Outstanding Balance = Invoice Total − Total Verified Payments − Applicable Credits or Adjustments**

Example:

| Description | Amount |
|---|---:|
| Original agreed fee | ₹10,000 |
| First verified payment | ₹3,000 |
| Second verified payment | ₹2,000 |
| Total verified | ₹5,000 |
| Outstanding balance | ₹5,000 |

The system must automatically update the outstanding balance.

### Partial Payment Rules

1. A parent can submit a payment smaller than the outstanding balance.
2. Multiple payments must be supported against the same invoice.
3. Each payment must have its own reference number and payment record.
4. The system must maintain the original invoice amount.
5. A payment submission must not automatically reduce the confirmed outstanding balance before verification.
6. Once staff verifies a payment, the system must update the paid amount and outstanding balance.
7. The invoice must become Partially Paid when some amount has been verified but a balance remains.
8. The invoice must become Paid only when the full payable amount is settled.
9. The system must prevent duplicate payment verification.
10. Payments must not exceed the outstanding amount unless an authorized credit or advance-payment workflow is explicitly supported.
11. Any adjustment, waiver, or credit must be recorded separately with authorization and an audit trail.
12. Parents must be able to see the total fee, verified payments, pending verification amounts, and remaining balance.

### Payment Arrangement

The administrator must be able to choose:

- Full payment.
- Partial payment allowed.
- Installment arrangement.

For installment arrangements, the administrator may define:

- Number of installments.
- Installment amount.
- Due date for each installment.
- Whether the parent can pay a different amount.
- Whether an installment can be paid in multiple transactions.

The system must support both planned installments and unplanned partial payments, subject to the organization's payment rules.

The organization must be able to configure whether a minimum payment amount or specific installment schedule is required.

Do not assume that every flat-rate fee must be divided into equal monthly payments.

---

## 7. Invoice Management

The system must support invoice creation by:

- Organization Admin.
- Authorized Branch Admin.

Organization Admin can generate invoices for authorized branches within the organization.

Branch Admin can generate invoices only for the branch or branches assigned to them.

Invoices must include:

- Invoice number.
- Organization details.
- Branch details.
- Child details.
- Parent or guardian details.
- Fee method: Tariff-Based or Flat Rate.
- Fee plan or flat-rate description.
- Billing period.
- Invoice date.
- Due date.
- Original fee amount.
- Adjustments or discounts, where applicable.
- Total payable amount.
- Total verified amount paid.
- Pending payment submissions, where applicable.
- Outstanding balance.
- Payment instructions.
- Invoice status.

### Invoice Status

Support the following statuses:

- Draft.
- Issued.
- Partially Paid.
- Paid.
- Overdue.
- Cancelled.
- Adjusted.

The status must be calculated using the invoice lifecycle and verified financial transactions.

An invoice with a pending payment submission must not be marked Paid.

### Historical Invoice Protection

Once an invoice is issued, its financial details must be preserved as a snapshot.

Changing the organization's tariff, bank details, QR codes, or future fee configuration must not silently change an existing invoice.

Any correction must use an authorized adjustment, cancellation, or replacement process with a complete audit trail.

---

## 8. Organization Bank Details and QR Code Configuration

Each organization must be able to configure its own payment details.

Supported bank information:

- Account holder name.
- Bank name.
- Branch name.
- Account number.
- IFSC code.
- MICR code.
- Account type, where applicable.
- Additional payment instructions.

Each organization must be able to upload and manage up to three QR codes.

QR code configuration must include:

- QR code image.
- Display name or payment purpose.
- Active/inactive status.
- Optional instructions.

The system must not expose one organization's bank details or QR codes to another organization.

Invoices must display the relevant organization's payment instructions.

Existing issued invoices must retain the payment details that were displayed when they were issued.

---

## 9. Parent Payment Submission

Parents must be able to view their children's invoices through the Parent Portal.

For each invoice, the parent must see:

- Original invoice amount.
- Amount already verified.
- Amount awaiting verification.
- Outstanding balance.
- Due date.
- Payment instructions.
- Available QR codes.
- Bank transfer details.
- Payment history.

Parents can pay using:

1. QR code payment.
2. Bank transfer.
3. Cash, where the organization allows parent cash declarations.

### Payment Submission Screen

The parent must be able to enter:

- Invoice reference.
- Amount paid.
- Payment method.
- Transaction ID or reference number.
- Payment date.
- Optional remarks.
- Screenshot or payment proof.

The screenshot or proof must be optional where the organization permits it.

The parent must be able to submit multiple payments against the same invoice.

After submission, show a clear status:

**Payment Submitted – Waiting for Daycare Verification.**

Do not display the submitted amount as confirmed paid until staff verifies it.

---

## 10. Manual Payment Verification

No payment gateway integration is required.

Authorized daycare staff must manually verify payments.

The verification screen must show:

- Parent details.
- Child details.
- Invoice details.
- Submitted amount.
- Payment method.
- Transaction ID.
- Payment date.
- Uploaded proof.
- Previous verified payments.
- Current outstanding balance.

Authorized staff can:

- Verify payment.
- Reject payment.
- Record a cash payment directly.
- Add verification remarks.
- View previous payment submissions.

### Verification Rules

1. Only authorized staff can verify or reject payments.
2. Staff must not verify the same payment twice.
3. Rejected submissions must not reduce the outstanding balance.
4. Verified payments must update the invoice balance automatically.
5. Every verification must record the staff member, date, time, amount, and remarks.
6. Cash payments entered directly by staff must also create a payment record and audit entry.
7. A parent-submitted cash declaration must remain unverified until staff confirms receipt.
8. Payment proof must be accessible only to authorized users within the relevant organization.
9. Any correction to a verified payment must follow an authorized reversal or adjustment process. Do not silently edit historical financial transactions.

---

## 11. Branch Transfers and Fee Changes

If a child moves from one branch to another:

- The system must preserve the previous branch's invoices and payments.
- The new branch's fee arrangement must be evaluated from its effective date.
- The administrator must review and approve the new fee arrangement.
- Existing invoices must not be rewritten.
- Any prorated adjustment must be explicitly calculated and recorded.

If an organization changes a tariff or flat-rate agreement:

- New rules apply only from their effective date.
- Existing issued invoices remain unchanged.
- Historical fee arrangements remain available for audit.

---

## 12. Financial Dashboard and Reports

Provide financial views at organization and branch levels.

### Organization Admin

The Organization Admin must see:

- Total invoiced amount.
- Total verified collections.
- Outstanding amount.
- Partially paid invoices.
- Overdue invoices.
- Payments awaiting verification.
- Branch-wise collections.
- Fee-plan-wise collections.
- Flat-rate versus tariff-based fee summary.
- Cash, QR, and bank-transfer payment summaries.

### Branch Admin

The Branch Admin must see the same relevant financial information, limited to authorized branches.

### Parent Portal

Parents must see:

- Invoice history.
- Total fees.
- Verified payments.
- Pending verification.
- Outstanding balances.
- Payment due dates.
- Payment submission history.

Reports must use verified payments for confirmed collection totals. Pending submissions must be shown separately.

---

## 13. Security, Tenant Isolation and Audit

All financial information must follow the existing multi-tenant authorization model.

Rules:

- Organization Admin can manage financial settings only for their organization.
- Branch Admin can manage invoices and payments only for authorized branches.
- Parents can access only invoices and payment records belonging to their linked children.
- One organization must never access another organization's tariffs, fee plans, bank details, QR codes, invoices, or payment records.
- Payment proofs must be protected from unauthorized access.
- All financial actions must be audited.

Audit events must include:

- Fee plan creation and modification.
- Flat-rate assignment.
- Flat-rate changes.
- Invoice generation.
- Invoice cancellation and adjustment.
- Payment submission.
- Payment verification and rejection.
- Cash payment recording.
- Payment reversal or correction.
- Bank detail and QR code changes.

Record the actor, organization, branch, affected record, timestamp, action, and relevant before/after values.

---

## 14. Data and Architecture Requirements

Before introducing new entities, inspect the existing finance and billing implementation.

Potential logical data concepts include:

- Organization Fee Plans.
- Tariff Rules.
- Child Fee Assignments.
- Flat-Rate Fee Agreements.
- Invoices.
- Invoice Line Items.
- Installment Schedules.
- Payment Transactions.
- Payment Submissions.
- Payment Verification Records.
- Organization Bank Accounts.
- Organization QR Codes.
- Financial Adjustments.
- Financial Audit History.

These are logical concepts, not instructions to create duplicate tables.

Reuse existing tables, services, APIs, invoice models, payment records, parent portal functionality, notification abstractions, and audit infrastructure wherever possible.

Technical requirements:

- Follow the existing .NET architecture.
- Use existing Dapper and PostgreSQL conventions.
- Follow the project's database function/stored-procedure approach.
- Do not introduce inline SQL where the project requires database functions or stored procedures.
- Keep fee calculation, invoice management, payment submission, payment verification, and reporting responsibilities modular.
- Use database transactions for financial operations that must succeed or fail together.
- Prevent duplicate payment processing and race conditions.
- Use decimal-safe monetary calculations. Do not use floating-point types for currency.
- Apply server-side authorization and tenant/branch validation to every financial API.
- Do not trust amounts, invoice IDs, child IDs, or organization IDs supplied by the browser without server-side validation.

---

## 15. Scope Exclusions

The initial implementation must NOT include:

- Razorpay or any other payment gateway.
- Automatic UPI verification.
- Bank API integration.
- Automatic bank reconciliation.
- Automatic payment confirmation from QR scans.
- Automatic refunds.
- Cross-organization financial sharing.

All payments must be submitted or recorded and then manually verified by authorized staff.

The existing email notification service must be reused through its current abstraction. Do not implement a new email provider as part of this requirement.

---

## 16. Implementation Phases

### Phase 0 – Existing System Inspection and Gap Analysis

Before writing or changing code:

1. Inspect the existing finance and billing modules.
2. Inspect existing fee plans, tariffs, invoices, payment records, and database functions.
3. Inspect child admission and onboarding workflows.
4. Inspect Parent Portal invoice and payment screens.
5. Inspect organization and branch authorization.
6. Inspect existing file upload and audit mechanisms.
7. Identify existing functionality that can be reused.
8. Identify missing functionality.
9. Identify any conflicts with the current architecture.

Produce a gap-analysis report with:

- Existing implementation.
- Missing capabilities.
- Existing database objects to reuse.
- Proposed database changes, if necessary.
- Proposed API and service changes.
- Proposed UI changes.
- Security considerations.
- Risks and migration considerations.

**Do not modify the existing application during this phase. Submit the report and wait for approval.**

### Phase 1 – Fee Configuration

Implement organization-specific fee plans, tariff rules, and flat-rate fee assignment after approval.

### Phase 2 – Invoice Generation

Implement invoice generation using tariff-based or flat-rate fee arrangements.

### Phase 3 – Partial Payments and Installments

Implement partial payment submission, installment schedules, outstanding balance calculations, and payment history.

### Phase 4 – Parent Payment Experience

Implement invoice viewing, QR and bank instructions, payment submission, and proof upload.

### Phase 5 – Manual Verification

Implement staff verification, rejection, cash recording, and payment correction workflows.

### Phase 6 – Reports, Audit and Security

Implement financial dashboards, reports, audit history, tenant isolation tests, and regression testing.

---

## 17. Acceptance Criteria

The requirement is complete only when the following scenarios work:

1. An Organization Admin can create a tariff plan for the organization.
2. Different branches can have different tariff amounts.
3. A Branch Admin can manage fees only for authorized branches.
4. An administrator can assign a flat rate of ₹10,000 for one child's annual admission.
5. The flat-rate assignment does not trigger tariff calculation.
6. The system preserves the agreed flat rate when future tariffs change.
7. An invoice can be generated from either fee-setting method.
8. A parent can submit ₹3,000 against a ₹10,000 invoice.
9. The submitted payment remains pending until staff verification.
10. After verifying ₹3,000, the invoice shows ₹3,000 paid and ₹7,000 outstanding.
11. The parent can submit another ₹2,000 payment against the same invoice.
12. After verification, the invoice shows ₹5,000 paid and ₹5,000 outstanding.
13. After the remaining ₹5,000 is verified, the invoice becomes Paid.
14. A rejected payment does not reduce the outstanding balance.
15. Staff can record cash payments directly with an audit record.
16. Multiple payments and their verification history remain visible.
17. Issued invoices are not silently changed when tariffs or bank details change.
18. Parents can access only their linked children's financial records.
19. Branch Admins cannot access unauthorized branches.
20. One organization cannot access another organization's financial information.
21. Organization and branch reports show accurate verified collections and outstanding balances.
22. Financial changes and corrections are auditable.

---

## 18. Final Instruction to Google Antigravity

Treat this as an extension of the existing EnjoyEveryday application.

Do not rebuild the finance module or replace existing billing functionality.

First inspect the repository and understand the existing implementation. Reuse existing entities, services, APIs, database functions, UI components, identity, authorization, notifications, and audit mechanisms.

Pay special attention to the distinction between:

- Tariff-based fee calculation.
- Direct flat-rate fee assignment.
- Original invoice amount.
- Planned installments.
- Parent-submitted payments.
- Verified payments.
- Pending verification amounts.
- Outstanding balance.

These must be separate concepts in the business logic.

A parent submitting payment proof is not the same as the daycare receiving and verifying the money.

Do not mark an invoice as Paid until the verified payments and approved adjustments settle the complete payable amount.

Start with Phase 0 only. Produce the inspection and gap-analysis report, explain the proposed implementation, and wait for approval before making code, database, or architectural changes.

