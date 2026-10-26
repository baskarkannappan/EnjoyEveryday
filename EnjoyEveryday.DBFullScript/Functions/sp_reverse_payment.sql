-- Function: public.sp_reverse_payment
-- Backup generated on 2026-10-25 20:34:05

CREATE OR REPLACE PROCEDURE public.sp_reverse_payment(IN p_payment_id uuid, IN p_reversed_by character varying, IN p_reason text, IN p_reversal_reference character varying, IN p_reversed_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE payments     SET is_reversed = TRUE,         status = 'Reversed',         reversed_at = p_reversed_at,         reversed_by = p_reversed_by,         reversal_reason = p_reason,         reversal_reference = p_reversal_reference     WHERE id = p_payment_id; END; $procedure$ ;
