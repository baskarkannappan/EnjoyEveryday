-- Migration 131: Organization Fee Management, Tariff Plans, Flat-Rate Fees, Invoicing & Partial Payments
-- Extends existing billing, contracts, invoices, and payments tables with org/branch/child scoping,
-- tariff precedence resolution, flat-rate agreements, org bank/QR settings, installments, and manual verification.

-- 1. Extend branches with optional location_tier
ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS location_tier VARCHAR(60);

-- 2. Extend family_billing_contracts
ALTER TABLE family_billing_contracts
    ADD COLUMN IF NOT EXISTS organization_id UUID,
    ADD COLUMN IF NOT EXISTS fee_method VARCHAR(30) NOT NULL DEFAULT 'TariffBased',
    ADD COLUMN IF NOT EXISTS fee_plan_id UUID,
    ADD COLUMN IF NOT EXISTS tariff_rule_id UUID,
    ADD COLUMN IF NOT EXISTS fee_period VARCHAR(30) NOT NULL DEFAULT 'Monthly',
    ADD COLUMN IF NOT EXISTS payment_arrangement VARCHAR(30) NOT NULL DEFAULT 'Full',
    ADD COLUMN IF NOT EXISTS currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    ADD COLUMN IF NOT EXISTS remarks TEXT,
    ADD COLUMN IF NOT EXISTS allow_parent_cash_declaration BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS minimum_partial_payment NUMERIC(14,2);

-- 3. Extend invoices
ALTER TABLE invoices
    ADD COLUMN IF NOT EXISTS branch_id UUID,
    ADD COLUMN IF NOT EXISTS child_id UUID,
    ADD COLUMN IF NOT EXISTS contract_id UUID,
    ADD COLUMN IF NOT EXISTS fee_method VARCHAR(30),
    ADD COLUMN IF NOT EXISTS fee_plan_name VARCHAR(200),
    ADD COLUMN IF NOT EXISTS adjustment_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS pending_verification_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS payment_instructions_snapshot JSONB;

CREATE INDEX IF NOT EXISTS idx_invoices_org_branch_child
    ON invoices (tenant_id, organization_id, branch_id, child_id);

-- 4. Extend payments
ALTER TABLE payments
    ADD COLUMN IF NOT EXISTS branch_id UUID,
    ADD COLUMN IF NOT EXISTS child_id UUID,
    ADD COLUMN IF NOT EXISTS submission_source VARCHAR(30) NOT NULL DEFAULT 'Staff',
    ADD COLUMN IF NOT EXISTS proof_storage_key TEXT,
    ADD COLUMN IF NOT EXISTS rejection_reason TEXT,
    ADD COLUMN IF NOT EXISTS rejected_by VARCHAR(200),
    ADD COLUMN IF NOT EXISTS rejected_at TIMESTAMPTZ;

CREATE INDEX IF NOT EXISTS idx_payments_org_status
    ON payments (tenant_id, organization_id, status, is_verified, is_reversed);

-- 5. Organization Fee Plans
CREATE TABLE IF NOT EXISTS org_fee_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    fee_category VARCHAR(80) NOT NULL DEFAULT 'Tuition',
    billing_frequency VARCHAR(30) NOT NULL DEFAULT 'Monthly',
    currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    allow_partial_payment BOOLEAN NOT NULL DEFAULT TRUE,
    minimum_partial_payment NUMERIC(14,2),
    allow_parent_cash_declaration BOOLEAN NOT NULL DEFAULT FALSE,
    created_by VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_fee_plans_org
    ON org_fee_plans (tenant_id, organization_id, is_active);

-- 6. Organization Tariff Rules (Precedence: Child > Classroom > Branch > LocationTier > OrgDefault)
CREATE TABLE IF NOT EXISTS org_tariff_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    fee_plan_id UUID NOT NULL REFERENCES org_fee_plans(id) ON DELETE CASCADE,
    scope_type VARCHAR(30) NOT NULL, -- Child, Classroom, Branch, LocationTier, OrgDefault
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    location_tier VARCHAR(60),
    child_category VARCHAR(80),
    amount NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
    expiry_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    notes TEXT,
    created_by VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_tariff_rules_lookup
    ON org_tariff_rules (tenant_id, organization_id, fee_plan_id, scope_type, is_active);

-- 7. Child Fee & Flat-Rate Adjustments History
CREATE TABLE IF NOT EXISTS child_fee_adjustments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    branch_id UUID,
    child_id UUID NOT NULL,
    contract_id UUID,
    invoice_id UUID,
    adjustment_type VARCHAR(40) NOT NULL, -- FlatRateChange, Waiver, Credit, Proration, Discount
    previous_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    new_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    adjustment_delta NUMERIC(14,2) NOT NULL DEFAULT 0,
    reason TEXT NOT NULL,
    effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
    adjusted_by_user_id UUID,
    adjusted_by_email VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_child_fee_adjustments_child
    ON child_fee_adjustments (tenant_id, organization_id, child_id, created_at DESC);

-- 8. Planned Invoice Installment Schedules
CREATE TABLE IF NOT EXISTS invoice_installments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    installment_number INT NOT NULL,
    due_date DATE NOT NULL,
    amount_due NUMERIC(14,2) NOT NULL CHECK (amount_due > 0),
    allow_custom_amount BOOLEAN NOT NULL DEFAULT TRUE,
    allow_multiple_transactions BOOLEAN NOT NULL DEFAULT TRUE,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_invoice_installments_invoice
    ON invoice_installments (invoice_id, installment_number);

-- 9. Organization Bank Account Details
CREATE TABLE IF NOT EXISTS org_bank_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL UNIQUE,
    account_holder_name VARCHAR(200) NOT NULL DEFAULT '',
    bank_name VARCHAR(200) NOT NULL DEFAULT '',
    branch_name VARCHAR(200),
    account_number VARCHAR(100) NOT NULL DEFAULT '',
    ifsc_code VARCHAR(40),
    micr_code VARCHAR(40),
    account_type VARCHAR(40) DEFAULT 'Current',
    additional_instructions TEXT,
    allow_parent_cash_declaration BOOLEAN NOT NULL DEFAULT FALSE,
    require_payment_proof BOOLEAN NOT NULL DEFAULT FALSE,
    updated_by VARCHAR(200),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 10. Organization Payment QR Codes (up to 3 slots per organization)
CREATE TABLE IF NOT EXISTS org_payment_qr_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    slot_index INT NOT NULL CHECK (slot_index BETWEEN 1 AND 3),
    display_name VARCHAR(150) NOT NULL,
    payment_purpose VARCHAR(200),
    qr_image_data_uri TEXT NOT NULL,
    instructions TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    updated_by VARCHAR(200),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_org_qr_slot UNIQUE (organization_id, slot_index)
);

-- =====================================================================
-- STORED FUNCTIONS & PROCEDURES
-- =====================================================================

-- A. Recalculate invoice balance (only verified non-reversed Completed payments reduce balance)
CREATE OR REPLACE PROCEDURE public.sp_recalculate_invoice_balance(IN p_invoice_id UUID)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_total_paid NUMERIC(14,2);
    v_pending NUMERIC(14,2);
    v_total_amount NUMERIC(14,2);
    v_adjustment NUMERIC(14,2);
    v_effective_payable NUMERIC(14,2);
BEGIN
    SELECT COALESCE(SUM(amount), 0)
    INTO v_total_paid
    FROM payments
    WHERE invoice_id = p_invoice_id
      AND status = 'Completed'
      AND is_verified = TRUE
      AND is_reversed = FALSE;

    SELECT COALESCE(SUM(amount), 0)
    INTO v_pending
    FROM payments
    WHERE invoice_id = p_invoice_id
      AND status = 'PendingVerification'
      AND is_reversed = FALSE;

    SELECT total_amount, COALESCE(adjustment_amount, 0)
    INTO v_total_amount, v_adjustment
    FROM invoices
    WHERE id = p_invoice_id;

    v_effective_payable := GREATEST(0, COALESCE(v_total_amount, 0) - COALESCE(v_adjustment, 0));

    UPDATE invoices
    SET amount_paid = v_total_paid,
        pending_verification_amount = v_pending,
        paid_at = CASE
            WHEN (v_effective_payable - v_total_paid) <= 0 AND v_pending = 0 THEN COALESCE(paid_at, NOW())
            ELSE NULL
        END,
        status = CASE
            WHEN status = 'Void' THEN 'Void'
            WHEN status = 'Cancelled' THEN 'Cancelled'
            WHEN status = 'Draft' THEN 'Draft'
            WHEN (v_effective_payable - v_total_paid) <= 0 AND v_pending = 0 THEN 'Paid'
            WHEN v_total_paid > 0 THEN 'PartiallyPaid'
            WHEN COALESCE(v_adjustment, 0) > 0 AND status = 'Adjusted' THEN 'Adjusted'
            WHEN status = 'Issued' THEN 'Issued'
            ELSE 'Unpaid'
        END,
        updated_at = NOW()
    WHERE id = p_invoice_id;
END;
$procedure$;

-- B. Verify a parent-submitted payment atomically with row lock (prevents double verification & overpayment)
CREATE OR REPLACE FUNCTION public.fn_verify_submitted_payment(
    p_payment_id UUID,
    p_verified_by VARCHAR,
    p_remarks TEXT DEFAULT NULL
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    invoice_id UUID,
    new_amount_paid NUMERIC(14,2),
    new_balance_due NUMERIC(14,2)
)
LANGUAGE plpgsql
AS $function$
DECLARE
    v_payment payments%ROWTYPE;
    v_invoice invoices%ROWTYPE;
    v_effective_payable NUMERIC(14,2);
    v_current_verified NUMERIC(14,2);
BEGIN
    SELECT * INTO v_payment
    FROM payments
    WHERE id = p_payment_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Payment submission not found.', NULL::UUID, 0::NUMERIC(14,2), 0::NUMERIC(14,2);
        RETURN;
    END IF;

    IF v_payment.is_verified OR v_payment.status = 'Completed' THEN
        RETURN QUERY SELECT FALSE, 'This payment has already been verified.', v_payment.invoice_id, 0::NUMERIC(14,2), 0::NUMERIC(14,2);
        RETURN;
    END IF;

    IF v_payment.status <> 'PendingVerification' THEN
        RETURN QUERY SELECT FALSE, 'Only payments awaiting verification can be verified.', v_payment.invoice_id, 0::NUMERIC(14,2), 0::NUMERIC(14,2);
        RETURN;
    END IF;

    SELECT * INTO v_invoice
    FROM invoices
    WHERE id = v_payment.invoice_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Associated invoice not found.', v_payment.invoice_id, 0::NUMERIC(14,2), 0::NUMERIC(14,2);
        RETURN;
    END IF;

    IF v_invoice.status IN ('Void', 'Cancelled', 'Draft') THEN
        RETURN QUERY SELECT FALSE, 'Cannot verify payment against a Void, Cancelled, or Draft invoice.', v_invoice.id, v_invoice.amount_paid, 0::NUMERIC(14,2);
        RETURN;
    END IF;

    v_effective_payable := GREATEST(0, v_invoice.total_amount - COALESCE(v_invoice.adjustment_amount, 0));

    SELECT COALESCE(SUM(amount), 0) INTO v_current_verified
    FROM payments
    WHERE invoice_id = v_invoice.id
      AND status = 'Completed'
      AND is_verified = TRUE
      AND is_reversed = FALSE;

    IF (v_current_verified + v_payment.amount) > v_effective_payable THEN
        RETURN QUERY SELECT FALSE,
            format('Verified amount exceeds remaining outstanding balance (%s).', (v_effective_payable - v_current_verified)::TEXT),
            v_invoice.id, v_current_verified, GREATEST(0, v_effective_payable - v_current_verified);
        RETURN;
    END IF;

    UPDATE payments
    SET is_verified = TRUE,
        verified_by = p_verified_by,
        verified_at = NOW(),
        status = 'Completed',
        notes = CASE
            WHEN p_remarks IS NOT NULL AND LENGTH(TRIM(p_remarks)) > 0
                THEN COALESCE(notes || E'\nVerification: ', 'Verification: ') || p_remarks
            ELSE notes
        END
    WHERE id = p_payment_id;

    CALL public.sp_recalculate_invoice_balance(v_invoice.id);

    SELECT amount_paid INTO v_current_verified FROM invoices WHERE id = v_invoice.id;

    RETURN QUERY SELECT
        TRUE,
        'Payment verified successfully.',
        v_invoice.id,
        v_current_verified,
        GREATEST(0, v_effective_payable - v_current_verified);
END;
$function$;

-- C. Reject a parent-submitted payment atomically
CREATE OR REPLACE FUNCTION public.fn_reject_submitted_payment(
    p_payment_id UUID,
    p_rejected_by VARCHAR,
    p_reason TEXT
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    invoice_id UUID
)
LANGUAGE plpgsql
AS $function$
DECLARE
    v_payment payments%ROWTYPE;
BEGIN
    SELECT * INTO v_payment
    FROM payments
    WHERE id = p_payment_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Payment submission not found.', NULL::UUID;
        RETURN;
    END IF;

    IF v_payment.is_verified OR v_payment.status = 'Completed' THEN
        RETURN QUERY SELECT FALSE, 'Verified payments cannot be rejected; use reversal/adjustment instead.', v_payment.invoice_id;
        RETURN;
    END IF;

    IF v_payment.status <> 'PendingVerification' THEN
        RETURN QUERY SELECT FALSE, 'Payment is not awaiting verification.', v_payment.invoice_id;
        RETURN;
    END IF;

    UPDATE payments
    SET status = 'Rejected',
        is_verified = FALSE,
        rejection_reason = COALESCE(NULLIF(TRIM(p_reason), ''), 'Rejected by daycare staff'),
        rejected_by = p_rejected_by,
        rejected_at = NOW()
    WHERE id = p_payment_id;

    IF v_payment.invoice_id IS NOT NULL THEN
        CALL public.sp_recalculate_invoice_balance(v_payment.invoice_id);
    END IF;

    RETURN QUERY SELECT TRUE, 'Payment submission rejected.', v_payment.invoice_id;
END;
$function$;

-- D. Resolve applicable tariff rule using precedence:
-- 1. Child > 2. Classroom > 3. Branch > 4. LocationTier > 5. OrgDefault
CREATE OR REPLACE FUNCTION public.fn_resolve_child_tariff(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL,
    p_child_id UUID DEFAULT NULL,
    p_location_tier VARCHAR DEFAULT NULL,
    p_billing_frequency VARCHAR DEFAULT NULL,
    p_as_of_date DATE DEFAULT CURRENT_DATE
)
RETURNS TABLE (
    rule_id UUID,
    fee_plan_id UUID,
    plan_name VARCHAR,
    fee_category VARCHAR,
    billing_frequency VARCHAR,
    scope_type VARCHAR,
    precedence_rank INT,
    amount NUMERIC(14,2),
    currency VARCHAR,
    allow_partial_payment BOOLEAN,
    minimum_partial_payment NUMERIC(14,2),
    allow_parent_cash_declaration BOOLEAN,
    location_tier VARCHAR,
    notes TEXT
)
LANGUAGE sql
STABLE
AS $function$
    WITH resolved AS (
        SELECT
            r.id AS rule_id,
            r.fee_plan_id,
            p.name AS plan_name,
            p.fee_category,
            p.billing_frequency,
            r.scope_type,
            CASE r.scope_type
                WHEN 'Child' THEN 1
                WHEN 'Classroom' THEN 2
                WHEN 'Branch' THEN 3
                WHEN 'LocationTier' THEN 4
                WHEN 'OrgDefault' THEN 5
                ELSE 99
            END AS precedence_rank,
            r.amount,
            r.currency,
            p.allow_partial_payment,
            p.minimum_partial_payment,
            p.allow_parent_cash_declaration,
            r.location_tier,
            r.notes,
            r.effective_date
        FROM org_tariff_rules r
        INNER JOIN org_fee_plans p ON p.id = r.fee_plan_id
        WHERE r.tenant_id = p_tenant_id
          AND r.organization_id = p_organization_id
          AND r.is_active = TRUE
          AND p.is_active = TRUE
          AND r.effective_date <= p_as_of_date
          AND (r.expiry_date IS NULL OR r.expiry_date >= p_as_of_date)
          AND (p_billing_frequency IS NULL OR LOWER(p.billing_frequency) = LOWER(p_billing_frequency))
          AND (
              (r.scope_type = 'Child' AND p_child_id IS NOT NULL AND r.child_id = p_child_id)
              OR (r.scope_type = 'Classroom' AND p_classroom_id IS NOT NULL AND r.classroom_id = p_classroom_id)
              OR (r.scope_type = 'Branch' AND p_branch_id IS NOT NULL AND r.branch_id = p_branch_id)
              OR (r.scope_type = 'LocationTier' AND p_location_tier IS NOT NULL AND LOWER(r.location_tier) = LOWER(p_location_tier))
              OR (r.scope_type = 'OrgDefault')
          )
    )
    SELECT
        rule_id, fee_plan_id, plan_name, fee_category, billing_frequency,
        scope_type, precedence_rank, amount, currency,
        allow_partial_payment, minimum_partial_payment, allow_parent_cash_declaration,
        location_tier, notes
    FROM resolved
    ORDER BY precedence_rank ASC, effective_date DESC
    LIMIT 1;
$function$;

-- E. Child-scoped invoices for Parent Portal (fixes multi-child/tenant isolation)
CREATE OR REPLACE FUNCTION public.fn_get_child_invoices(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    child_id UUID,
    contract_id UUID,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    adjustment_amount NUMERIC,
    pending_verification_amount NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    fee_method VARCHAR,
    fee_plan_name VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    payment_instructions_snapshot JSONB,
    billing_contact_email VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $function$
    SELECT
        i.id, i.tenant_id, i.organization_id, i.branch_id, i.child_id, i.contract_id,
        i.invoice_number, i.billing_period, i.subtotal, i.discount_amount, i.tax_amount,
        i.total_amount, i.amount_paid, COALESCE(i.adjustment_amount, 0),
        COALESCE(i.pending_verification_amount, 0), i.currency, i.status,
        i.fee_method, i.fee_plan_name, i.issue_date, i.due_date, i.paid_at,
        i.description, i.notes, i.payment_instructions, i.payment_instructions_snapshot,
        i.billing_contact_email, i.created_at, i.updated_at
    FROM invoices i
    WHERE i.tenant_id = p_tenant_id
      AND i.child_id = p_child_id
      AND i.status <> 'Draft'
    ORDER BY i.issue_date DESC;
$function$;

-- F. Org/Branch Tuition Invoices listing
CREATE OR REPLACE FUNCTION public.fn_get_org_tuition_invoices(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    child_id UUID,
    contract_id UUID,
    child_name TEXT,
    branch_name TEXT,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    adjustment_amount NUMERIC,
    pending_verification_amount NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    fee_method VARCHAR,
    fee_plan_name VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    payment_instructions_snapshot JSONB,
    billing_contact_email VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $function$
    SELECT
        i.id, i.tenant_id, i.organization_id, i.branch_id, i.child_id, i.contract_id,
        COALESCE(TRIM(CONCAT(c.first_name, ' ', c.last_name)), 'Child Record') AS child_name,
        COALESCE(b.name, 'Organization Wide') AS branch_name,
        i.invoice_number, i.billing_period, i.subtotal, i.discount_amount, i.tax_amount,
        i.total_amount, i.amount_paid, COALESCE(i.adjustment_amount, 0),
        COALESCE(i.pending_verification_amount, 0), i.currency, i.status,
        i.fee_method, i.fee_plan_name, i.issue_date, i.due_date, i.paid_at,
        i.description, i.notes, i.payment_instructions, i.payment_instructions_snapshot,
        i.billing_contact_email, i.created_at, i.updated_at
    FROM invoices i
    LEFT JOIN children c ON c.id = i.child_id
    LEFT JOIN branches b ON b.id = i.branch_id
    WHERE i.tenant_id = p_tenant_id
      AND i.organization_id = p_organization_id
      AND i.child_id IS NOT NULL
      AND (p_branch_id IS NULL OR i.branch_id = p_branch_id)
    ORDER BY i.issue_date DESC;
$function$;

-- G. Org/Branch Payment Submissions & History Queue
CREATE OR REPLACE FUNCTION public.fn_get_org_payments_queue(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    child_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    child_name TEXT,
    branch_name TEXT,
    parent_email VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    status VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    is_reversed BOOLEAN,
    transaction_id VARCHAR,
    receipt_number VARCHAR,
    submission_source VARCHAR,
    proof_storage_key TEXT,
    rejection_reason TEXT,
    rejected_by VARCHAR,
    rejected_at TIMESTAMPTZ,
    notes TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $function$
    SELECT
        p.id, p.tenant_id, p.organization_id,
        COALESCE(p.branch_id, i.branch_id) AS branch_id,
        COALESCE(p.child_id, i.child_id) AS child_id,
        p.invoice_id,
        COALESCE(p.invoice_number, i.invoice_number) AS invoice_number,
        COALESCE(TRIM(CONCAT(c.first_name, ' ', c.last_name)), 'Unknown Child') AS child_name,
        COALESCE(b.name, 'Organization') AS branch_name,
        COALESCE(i.billing_contact_email, p.recorded_by_email) AS parent_email,
        p.payment_reference, p.amount, p.currency, p.method, p.status,
        p.is_verified, p.verified_by, p.verified_at, p.is_reversed,
        p.transaction_id, p.receipt_number, p.submission_source,
        p.proof_storage_key, p.rejection_reason, p.rejected_by, p.rejected_at,
        p.notes, p.payment_date, p.created_at
    FROM payments p
    INNER JOIN invoices i ON i.id = p.invoice_id
    LEFT JOIN children c ON c.id = COALESCE(p.child_id, i.child_id)
    LEFT JOIN branches b ON b.id = COALESCE(p.branch_id, i.branch_id)
    WHERE p.tenant_id = p_tenant_id
      AND p.organization_id = p_organization_id
      AND i.child_id IS NOT NULL
      AND (p_branch_id IS NULL OR COALESCE(p.branch_id, i.branch_id) = p_branch_id)
      AND (p_status IS NULL OR p.status = p_status)
    ORDER BY
        CASE WHEN p.status = 'PendingVerification' THEN 0 ELSE 1 END,
        p.created_at DESC;
$function$;

-- H. Financial Dashboard Summary (Org or Branch scoped)
CREATE OR REPLACE FUNCTION public.fn_get_org_fee_dashboard(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    total_invoiced NUMERIC(14,2),
    total_verified_collections NUMERIC(14,2),
    total_outstanding NUMERIC(14,2),
    total_pending_verification NUMERIC(14,2),
    partially_paid_count BIGINT,
    overdue_count BIGINT,
    awaiting_verification_count BIGINT,
    flat_rate_invoiced NUMERIC(14,2),
    tariff_based_invoiced NUMERIC(14,2),
    cash_collected NUMERIC(14,2),
    qr_collected NUMERIC(14,2),
    bank_transfer_collected NUMERIC(14,2)
)
LANGUAGE sql
STABLE
AS $function$
    WITH inv AS (
        SELECT
            total_amount,
            amount_paid,
            COALESCE(adjustment_amount, 0) AS adj,
            COALESCE(pending_verification_amount, 0) AS pending_amt,
            status,
            due_date,
            fee_method
        FROM invoices
        WHERE tenant_id = p_tenant_id
          AND organization_id = p_organization_id
          AND child_id IS NOT NULL
          AND status NOT IN ('Draft', 'Void', 'Cancelled')
          AND (p_branch_id IS NULL OR branch_id = p_branch_id)
    ),
    pay AS (
        SELECT
            p.amount,
            p.method,
            p.status,
            p.is_verified,
            p.is_reversed
        FROM payments p
        INNER JOIN invoices i ON i.id = p.invoice_id
        WHERE p.tenant_id = p_tenant_id
          AND p.organization_id = p_organization_id
          AND i.child_id IS NOT NULL
          AND (p_branch_id IS NULL OR COALESCE(p.branch_id, i.branch_id) = p_branch_id)
    )
    SELECT
        COALESCE((SELECT SUM(GREATEST(0, total_amount - adj)) FROM inv), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(amount_paid) FROM inv), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(GREATEST(0, total_amount - adj - amount_paid)) FROM inv), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(pending_amt) FROM inv), 0)::NUMERIC(14,2),
        (SELECT COUNT(*) FROM inv WHERE status = 'PartiallyPaid'),
        (SELECT COUNT(*) FROM inv WHERE status <> 'Paid' AND GREATEST(0, total_amount - adj - amount_paid) > 0 AND due_date < NOW()),
        (SELECT COUNT(*) FROM pay WHERE status = 'PendingVerification' AND is_reversed = FALSE),
        COALESCE((SELECT SUM(GREATEST(0, total_amount - adj)) FROM inv WHERE fee_method = 'FlatRate'), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(GREATEST(0, total_amount - adj)) FROM inv WHERE COALESCE(fee_method, 'TariffBased') = 'TariffBased'), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(amount) FROM pay WHERE status = 'Completed' AND is_verified = TRUE AND is_reversed = FALSE AND method = 'Cash'), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(amount) FROM pay WHERE status = 'Completed' AND is_verified = TRUE AND is_reversed = FALSE AND method IN ('QrCode', 'QR')), 0)::NUMERIC(14,2),
        COALESCE((SELECT SUM(amount) FROM pay WHERE status = 'Completed' AND is_verified = TRUE AND is_reversed = FALSE AND method = 'BankTransfer'), 0)::NUMERIC(14,2);
$function$;

