-- Migration 137: Fix ambiguous column reference "invoice_id" in fn_verify_submitted_payment
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
    FROM payments p
    WHERE p.id = p_payment_id
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
    FROM invoices inv
    WHERE inv.id = v_payment.invoice_id
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

    SELECT COALESCE(SUM(p.amount), 0) INTO v_current_verified
    FROM payments p
    WHERE p.invoice_id = v_invoice.id
      AND p.status = 'Completed'
      AND p.is_verified = TRUE
      AND p.is_reversed = FALSE;

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

