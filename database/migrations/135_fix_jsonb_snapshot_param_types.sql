-- Migration 135: Accept TEXT for JSONB snapshot parameters in tuition invoice routines and cast internally
DROP PROCEDURE IF EXISTS public.sp_insert_tuition_invoice(
    UUID, UUID, UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, NUMERIC, NUMERIC, NUMERIC, NUMERIC, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TIMESTAMPTZ, TIMESTAMPTZ, VARCHAR, TEXT, TEXT, TEXT, JSONB, VARCHAR
);

CREATE OR REPLACE PROCEDURE public.sp_insert_tuition_invoice(
    IN p_id UUID, IN p_tenant_id UUID, IN p_organization_id UUID,
    IN p_branch_id UUID, IN p_child_id UUID, IN p_contract_id UUID,
    IN p_invoice_number VARCHAR, IN p_billing_period VARCHAR,
    IN p_subtotal NUMERIC, IN p_discount_amount NUMERIC, IN p_tax_amount NUMERIC,
    IN p_total_amount NUMERIC, IN p_currency VARCHAR, IN p_status VARCHAR,
    IN p_fee_method VARCHAR, IN p_fee_plan_name VARCHAR,
    IN p_issue_date TIMESTAMPTZ, IN p_due_date TIMESTAMPTZ,
    IN p_created_by VARCHAR, IN p_description TEXT, IN p_notes TEXT,
    IN p_payment_instructions TEXT, IN p_payment_instructions_snapshot TEXT,
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
        p_payment_instructions,
        CASE WHEN p_payment_instructions_snapshot IS NOT NULL AND LENGTH(TRIM(p_payment_instructions_snapshot)) > 0 THEN p_payment_instructions_snapshot::jsonb ELSE NULL END,
        p_billing_contact_email,
        'OrgToParent', NOW(), NOW()
    );
END;
$procedure$;

DROP FUNCTION IF EXISTS public.fn_publish_tuition_invoice(UUID, VARCHAR, TEXT, JSONB);

CREATE OR REPLACE FUNCTION public.fn_publish_tuition_invoice(
    p_invoice_id UUID,
    p_published_by VARCHAR,
    p_payment_instructions TEXT,
    p_payment_instructions_snapshot TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $function$
BEGIN
    UPDATE invoices
    SET status = 'Issued',
        published_at = NOW(),
        published_by = p_published_by,
        payment_instructions = COALESCE(p_payment_instructions, payment_instructions),
        payment_instructions_snapshot = COALESCE(
            CASE WHEN p_payment_instructions_snapshot IS NOT NULL AND LENGTH(TRIM(p_payment_instructions_snapshot)) > 0 THEN p_payment_instructions_snapshot::jsonb ELSE NULL END,
            payment_instructions_snapshot
        ),
        updated_at = NOW()
    WHERE id = p_invoice_id
      AND status = 'Draft';
    RETURN FOUND;
END;
$function$;

