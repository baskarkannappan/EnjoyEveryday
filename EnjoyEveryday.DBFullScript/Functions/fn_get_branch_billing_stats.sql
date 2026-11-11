-- Function: public.fn_get_branch_billing_stats
-- Backup generated on 2026-11-11 09:04:46

CREATE OR REPLACE FUNCTION public.fn_get_branch_billing_stats(p_tenant_id uuid)  RETURNS TABLE(total_billed numeric, total_paid numeric)  LANGUAGE sql  STABLE AS $function$     SELECT          COALESCE(SUM(total_amount), 0)::numeric AS total_billed,         COALESCE(SUM(amount_paid), 0)::numeric AS total_paid     FROM invoices     WHERE tenant_id = p_tenant_id; $function$ ;
