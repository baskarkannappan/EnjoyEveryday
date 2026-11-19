-- Migration 134: Strict separation between Platform Billing and Organization-to-Parent Billing,
-- plus Organization-Owned Razorpay Gateway Settings and Webhook Idempotency.

-- 1. Billing Domain Isolation Columns on invoices & payments
ALTER TABLE invoices
    ADD COLUMN IF NOT EXISTS billing_domain VARCHAR(30) NOT NULL DEFAULT 'PlatformSubscription';

ALTER TABLE payments
    ADD COLUMN IF NOT EXISTS billing_domain VARCHAR(30) NOT NULL DEFAULT 'PlatformSubscription';

UPDATE invoices
SET billing_domain = 'OrgToParent'
WHERE child_id IS NOT NULL
  AND billing_domain <> 'OrgToParent';

UPDATE payments p
SET billing_domain = 'OrgToParent'
FROM invoices i
WHERE p.invoice_id = i.id
  AND i.child_id IS NOT NULL
  AND p.billing_domain <> 'OrgToParent';

CREATE INDEX IF NOT EXISTS idx_invoices_billing_domain
    ON invoices (billing_domain, tenant_id, organization_id);

CREATE INDEX IF NOT EXISTS idx_payments_billing_domain
    ON payments (billing_domain, tenant_id, organization_id);

-- 2. Extend razorpay_webhook_events with organization_id for per-org webhook isolation
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'razorpay_webhook_events') THEN
        ALTER TABLE razorpay_webhook_events ADD COLUMN IF NOT EXISTS organization_id UUID;
    END IF;
END $$;

-- 3. Enforce Platform-only scope in fn_get_all_invoices, fn_get_invoices_for_organization, fn_get_payments_for_organization
CREATE OR REPLACE FUNCTION public.fn_get_all_invoices(
    p_tenant_id UUID DEFAULT NULL,
    p_organization_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE(
    id UUID, tenant_id UUID, organization_id UUID, invoice_number VARCHAR, billing_period VARCHAR,
    subtotal NUMERIC, discount_amount NUMERIC, tax_amount NUMERIC, total_amount NUMERIC, amount_paid NUMERIC,
    currency VARCHAR, status VARCHAR, issue_date TIMESTAMPTZ, due_date TIMESTAMPTZ, paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ, published_by VARCHAR, voided_at TIMESTAMPTZ, voided_by VARCHAR, void_reason TEXT,
    created_by VARCHAR, description TEXT, notes TEXT, payment_instructions TEXT, billing_contact_email VARCHAR,
    document_storage_key VARCHAR, created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE billing_domain = 'PlatformSubscription'
      AND child_id IS NULL
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id)
      AND (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_status IS NULL OR status = p_status)
    ORDER BY issue_date DESC, created_at DESC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_invoices_for_organization(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_include_drafts BOOLEAN DEFAULT TRUE
)
RETURNS TABLE(
    id UUID, tenant_id UUID, organization_id UUID, invoice_number VARCHAR, billing_period VARCHAR,
    subtotal NUMERIC, discount_amount NUMERIC, tax_amount NUMERIC, total_amount NUMERIC, amount_paid NUMERIC,
    currency VARCHAR, status VARCHAR, issue_date TIMESTAMPTZ, due_date TIMESTAMPTZ, paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ, published_by VARCHAR, voided_at TIMESTAMPTZ, voided_by VARCHAR, void_reason TEXT,
    created_by VARCHAR, description TEXT, notes TEXT, payment_instructions TEXT, billing_contact_email VARCHAR,
    document_storage_key VARCHAR, created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE billing_domain = 'PlatformSubscription'
      AND child_id IS NULL
      AND organization_id = p_organization_id
      AND tenant_id = p_tenant_id
      AND (p_include_drafts OR status != 'Draft')
    ORDER BY issue_date DESC, created_at DESC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_payments_for_organization(p_organization_id UUID)
RETURNS TABLE(
    id UUID, tenant_id UUID, organization_id UUID, invoice_id UUID, invoice_number VARCHAR,
    payment_reference VARCHAR, amount NUMERIC, currency VARCHAR, method VARCHAR,
    is_verified BOOLEAN, verified_by VARCHAR, verified_at TIMESTAMPTZ, status VARCHAR,
    transaction_id VARCHAR, notes TEXT, provider_order_id VARCHAR, provider_payment_id VARCHAR,
    provider_signature VARCHAR, receipt_number VARCHAR, recorded_by_user_id UUID, recorded_by_email VARCHAR,
    is_reversed BOOLEAN, reversed_at TIMESTAMPTZ, reversed_by VARCHAR, reversal_reason TEXT,
    reversal_reference VARCHAR, audit_metadata TEXT, payment_date TIMESTAMPTZ, created_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT
        id, tenant_id, organization_id,
        invoice_id, invoice_number,
        payment_reference, amount, currency, method,
        is_verified, verified_by, verified_at,
        status, transaction_id, notes,
        provider_order_id, provider_payment_id,
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE billing_domain = 'PlatformSubscription'
      AND child_id IS NULL
      AND organization_id = p_organization_id
    ORDER BY payment_date DESC, created_at DESC;
$function$;

-- 4. Stamp billing_domain = 'OrgToParent' on tuition invoice & parent payment procedures
CREATE OR REPLACE PROCEDURE public.sp_insert_tuition_invoice(
    IN p_id UUID, IN p_tenant_id UUID, IN p_organization_id UUID,
    IN p_branch_id UUID, IN p_child_id UUID, IN p_contract_id UUID,
    IN p_invoice_number VARCHAR, IN p_billing_period VARCHAR,
    IN p_subtotal NUMERIC, IN p_discount_amount NUMERIC, IN p_tax_amount NUMERIC,
    IN p_total_amount NUMERIC, IN p_currency VARCHAR, IN p_status VARCHAR,
    IN p_fee_method VARCHAR, IN p_fee_plan_name VARCHAR,
    IN p_issue_date TIMESTAMPTZ, IN p_due_date TIMESTAMPTZ,
    IN p_created_by VARCHAR, IN p_description TEXT, IN p_notes TEXT,
    IN p_payment_instructions TEXT, IN p_payment_instructions_snapshot JSONB,
    IN p_billing_contact_email VARCHAR
)
LANGUAGE plpgsql AS $procedure$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, branch_id, child_id, contract_id,
        invoice_number, billing_period, subtotal, discount_amount, tax_amount,
        total_amount, amount_paid, adjustment_amount, pending_verification_amount,
        currency, status, fee_method, fee_plan_name, issue_date, due_date,
        published_at, published_by, created_by, description, notes,
        payment_instructions, payment_instructions_snapshot, billing_contact_email,
        billing_domain, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_contract_id,
        p_invoice_number, p_billing_period, p_subtotal, p_discount_amount, p_tax_amount,
        p_total_amount, 0, 0, 0,
        p_currency, p_status, p_fee_method, p_fee_plan_name, p_issue_date, p_due_date,
        CASE WHEN p_status <> 'Draft' THEN NOW() ELSE NULL END,
        CASE WHEN p_status <> 'Draft' THEN p_created_by ELSE NULL END,
        p_created_by, p_description, p_notes,
        p_payment_instructions, p_payment_instructions_snapshot, p_billing_contact_email,
        'OrgToParent', NOW(), NOW()
    );
END;
$procedure$;

CREATE OR REPLACE PROCEDURE public.sp_submit_parent_payment(
    IN p_id UUID, IN p_tenant_id UUID, IN p_organization_id UUID,
    IN p_branch_id UUID, IN p_child_id UUID, IN p_invoice_id UUID,
    IN p_invoice_number VARCHAR, IN p_payment_reference VARCHAR,
    IN p_amount NUMERIC, IN p_currency VARCHAR, IN p_method VARCHAR,
    IN p_transaction_id VARCHAR, IN p_notes TEXT,
    IN p_submission_source VARCHAR, IN p_proof_storage_key TEXT,
    IN p_recorded_by_user_id UUID, IN p_recorded_by_email VARCHAR,
    IN p_payment_date TIMESTAMPTZ
)
LANGUAGE plpgsql AS $procedure$
BEGIN
    INSERT INTO payments (
        id, tenant_id, organization_id, branch_id, child_id, invoice_id, invoice_number,
        payment_reference, amount, currency, method, is_verified, status, transaction_id,
        notes, submission_source, proof_storage_key, recorded_by_user_id, recorded_by_email,
        billing_domain, payment_date, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_invoice_id, p_invoice_number,
        p_payment_reference, p_amount, p_currency, p_method, FALSE, 'PendingVerification', p_transaction_id,
        p_notes, p_submission_source, p_proof_storage_key, p_recorded_by_user_id, p_recorded_by_email,
        'OrgToParent', p_payment_date, NOW()
    );
    CALL public.sp_recalculate_invoice_balance(p_invoice_id);
END;
$procedure$;

-- 5. Organization-Owned Razorpay Gateway Settings
CREATE TABLE IF NOT EXISTS org_payment_gateway_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL UNIQUE,
    provider VARCHAR(40) NOT NULL DEFAULT 'Razorpay',
    has_razorpay_account BOOLEAN NOT NULL DEFAULT FALSE,
    key_id VARCHAR(120),
    encrypted_key_secret TEXT,
    encrypted_webhook_secret TEXT,
    environment VARCHAR(20) NOT NULL DEFAULT 'Test', -- Test | Live
    status VARCHAR(40) NOT NULL DEFAULT 'NotConfigured', -- NotConfigured, ConfigurationPending, TestActive, LiveActive, ValidationFailed, Disabled
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    last_validated_at TIMESTAMPTZ,
    validation_message TEXT,
    configured_by VARCHAR(200),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE OR REPLACE FUNCTION public.fn_get_org_payment_gateway_settings(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS SETOF org_payment_gateway_settings
LANGUAGE sql STABLE AS $function$
    SELECT * FROM org_payment_gateway_settings
    WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_org_payment_gateway_settings(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_has_razorpay_account BOOLEAN,
    p_key_id VARCHAR,
    p_encrypted_key_secret TEXT,
    p_encrypted_webhook_secret TEXT,
    p_environment VARCHAR,
    p_status VARCHAR,
    p_is_active BOOLEAN,
    p_last_validated_at TIMESTAMPTZ,
    p_validation_message TEXT,
    p_configured_by VARCHAR
)
RETURNS SETOF org_payment_gateway_settings
LANGUAGE sql AS $function$
    INSERT INTO org_payment_gateway_settings (
        id, tenant_id, organization_id, provider, has_razorpay_account, key_id,
        encrypted_key_secret, encrypted_webhook_secret, environment, status, is_active,
        last_validated_at, validation_message, configured_by, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, 'Razorpay', p_has_razorpay_account, p_key_id,
        p_encrypted_key_secret, p_encrypted_webhook_secret, p_environment, p_status, p_is_active,
        p_last_validated_at, p_validation_message, p_configured_by, NOW()
    )
    ON CONFLICT (organization_id) DO UPDATE SET
        has_razorpay_account = EXCLUDED.has_razorpay_account,
        key_id = EXCLUDED.key_id,
        encrypted_key_secret = COALESCE(NULLIF(EXCLUDED.encrypted_key_secret, ''), org_payment_gateway_settings.encrypted_key_secret),
        encrypted_webhook_secret = COALESCE(NULLIF(EXCLUDED.encrypted_webhook_secret, ''), org_payment_gateway_settings.encrypted_webhook_secret),
        environment = EXCLUDED.environment,
        status = EXCLUDED.status,
        is_active = EXCLUDED.is_active,
        last_validated_at = EXCLUDED.last_validated_at,
        validation_message = EXCLUDED.validation_message,
        configured_by = EXCLUDED.configured_by,
        updated_at = NOW()
    WHERE org_payment_gateway_settings.tenant_id = p_tenant_id
    RETURNING *;
$function$;

-- 6. Atomic Org Razorpay Verified Payment Recording (idempotent by provider_payment_id)
CREATE OR REPLACE FUNCTION public.fn_record_org_razorpay_payment(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_child_id UUID,
    p_invoice_id UUID,
    p_invoice_number VARCHAR,
    p_amount NUMERIC,
    p_currency VARCHAR,
    p_provider_order_id VARCHAR,
    p_provider_payment_id VARCHAR,
    p_provider_signature VARCHAR,
    p_recorded_by_user_id UUID,
    p_recorded_by_email VARCHAR,
    p_notes TEXT
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    payment_id UUID
)
LANGUAGE plpgsql AS $function$
DECLARE
    v_existing UUID;
    v_invoice invoices%ROWTYPE;
    v_effective_payable NUMERIC(14,2);
    v_remaining NUMERIC(14,2);
BEGIN
    SELECT id INTO v_existing
    FROM payments
    WHERE provider_payment_id = p_provider_payment_id
      AND is_reversed = FALSE
    LIMIT 1;

    IF v_existing IS NOT NULL THEN
        RETURN QUERY SELECT TRUE, 'Payment already recorded.', v_existing;
        RETURN;
    END IF;

    SELECT * INTO v_invoice
    FROM invoices
    WHERE id = p_invoice_id
      AND organization_id = p_organization_id
      AND billing_domain = 'OrgToParent'
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Parent invoice not found for this organization.', NULL::UUID;
        RETURN;
    END IF;

    v_effective_payable := GREATEST(0, v_invoice.total_amount - COALESCE(v_invoice.adjustment_amount, 0));
    v_remaining := GREATEST(0, v_effective_payable - v_invoice.amount_paid);

    IF p_amount <= 0 OR p_amount > v_remaining THEN
        RETURN QUERY SELECT FALSE, format('Payment amount (%s) exceeds remaining balance (%s).', p_amount, v_remaining), NULL::UUID;
        RETURN;
    END IF;

    INSERT INTO payments (
        id, tenant_id, organization_id, branch_id, child_id, invoice_id, invoice_number,
        payment_reference, amount, currency, method, is_verified, verified_by, verified_at,
        status, transaction_id, notes, provider_order_id, provider_payment_id, provider_signature,
        submission_source, recorded_by_user_id, recorded_by_email, billing_domain, payment_date, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_invoice_id, p_invoice_number,
        p_provider_payment_id, p_amount, p_currency, 'Razorpay', TRUE, 'Organization Razorpay Gateway', NOW(),
        'Completed', p_provider_payment_id, p_notes, p_provider_order_id, p_provider_payment_id, p_provider_signature,
        'ParentPortalRazorpay', p_recorded_by_user_id, p_recorded_by_email, 'OrgToParent', NOW(), NOW()
    );

    CALL public.sp_recalculate_invoice_balance(p_invoice_id);

    RETURN QUERY SELECT TRUE, 'Razorpay payment verified and applied.', p_id;
END;
$function$;

