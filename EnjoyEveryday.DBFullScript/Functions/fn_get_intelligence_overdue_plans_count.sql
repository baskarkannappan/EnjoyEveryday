-- Function: public.fn_get_intelligence_overdue_plans_count
-- Backup generated on 2026-10-25 20:33:26

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_overdue_plans_count(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today date)  RETURNS integer  LANGUAGE sql  STABLE AS $function$     SELECT COUNT(*)::int     FROM experience_schedules es     JOIN classrooms cl ON es.classroom_id = cl.id     JOIN branches b ON cl.branch_id = b.id     WHERE es.tenant_id = p_tenant_id       AND b.organization_id = p_org_id       AND (p_branch_id IS NULL OR b.id = p_branch_id)       AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)       AND es.scheduled_date < p_today       AND es.status IN ('Confirmed', 'Scheduled'); $function$ ;
