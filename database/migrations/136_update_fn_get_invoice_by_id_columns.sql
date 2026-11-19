-- Migration 136: Include child/branch/fee/snapshot columns in fn_get_invoice_by_id
DROP FUNCTION IF EXISTS public.fn_get_invoice_by_id(UUID, UUID);

CREATE OR REPLACE FUNCTION public.fn_get_invoice_by_id(
    p_tenant_id UUID DEFAULT NULL,
    p_invoice_id UUID DEFAULT NULL
)
RETURNS TABLE(
    id UUID, tenant_id UUID, organization_id UUID, branch_id UUID, child_id UUID, contract_id UUID,
    invoice_number VARCHAR, billing_period VARCHAR, subtotal NUMERIC, discount_amount NUMERIC,
    tax_amount NUMERIC, total_amount NUMERIC, adjustment_amount NUMERIC, amount_paid NUMERIC,
    pending_verification_amount NUMERIC, currency VARCHAR, status VARCHAR,
    fee_method VARCHAR, fee_plan_name VARCHAR,
    issue_date TIMESTAMPTZ, due_date TIMESTAMPTZ, paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ, published_by VARCHAR, voided_at TIMESTAMPTZ, voided_by VARCHAR,
    void_reason TEXT, created_by VARCHAR, description TEXT, notes TEXT,
    payment_instructions TEXT, payment_instructions_snapshot TEXT,
    billing_contact_email VARCHAR, document_storage_key VARCHAR,
    created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT
        id, tenant_id, organization_id, branch_id, child_id, contract_id,
        invoice_number, billing_period, subtotal, discount_amount,
        tax_amount, total_amount, COALESCE(adjustment_amount, 0), amount_paid,
        COALESCE(pending_verification_amount, 0), currency, status,
        fee_method, fee_plan_name,
        issue_date, due_date, paid_at,
        published_at, published_by, voided_at, voided_by,
        void_reason, created_by, description, notes,
        payment_instructions, payment_instructions_snapshot::text,
        billing_contact_email, document_storage_key,
        created_at, updated_at
    FROM invoices
    WHERE id = p_invoice_id
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id);
$function$;

