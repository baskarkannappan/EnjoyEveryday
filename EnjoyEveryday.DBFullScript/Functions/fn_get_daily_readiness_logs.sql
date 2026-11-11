-- Function: public.fn_get_daily_readiness_logs
-- Backup generated on 2026-11-11 09:04:59

CREATE OR REPLACE FUNCTION public.fn_get_daily_readiness_logs(p_tenant_id uuid, p_organization_id uuid, p_check_date date)  RETURNS TABLE(item_id character varying, is_completed boolean, updated_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT item_id, is_completed, updated_at     FROM public.daily_readiness_checklist_logs     WHERE tenant_id = p_tenant_id       AND organization_id = p_organization_id       AND check_date = p_check_date; $function$ ;
