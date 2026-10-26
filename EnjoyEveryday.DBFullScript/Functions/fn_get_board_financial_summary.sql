-- Function: public.fn_get_board_financial_summary
-- Backup generated on 2026-10-25 20:33:03

CREATE OR REPLACE FUNCTION public.fn_get_board_financial_summary(p_tenant_id uuid, p_org_id uuid)  RETURNS TABLE(outstanding numeric, overdue_count integer)  LANGUAGE sql  STABLE AS $function$     SELECT          COALESCE(SUM(total_amount - amount_paid) FILTER (WHERE status != 'Paid' AND status != 'Void'), 0)::numeric AS outstanding,         COUNT(1) FILTER (WHERE status != 'Paid' AND status != 'Void' AND due_date < NOW())::int AS overdue_count     FROM invoices     WHERE tenant_id = p_tenant_id       AND organization_id = p_org_id; $function$ ;
