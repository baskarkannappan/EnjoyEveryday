-- Migration 133: Stored function for publishing Draft tuition invoices (Org Admin & Branch Admin)
CREATE OR REPLACE FUNCTION public.fn_publish_tuition_invoice(
    p_invoice_id UUID,
    p_published_by VARCHAR,
    p_payment_instructions TEXT,
    p_payment_instructions_snapshot JSONB
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
        payment_instructions_snapshot = COALESCE(p_payment_instructions_snapshot, payment_instructions_snapshot),
        updated_at = NOW()
    WHERE id = p_invoice_id
      AND status = 'Draft';
    RETURN FOUND;
END;
$function$;

