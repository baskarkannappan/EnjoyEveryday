-- Function: public.fn_get_branch_classrooms_with_plans_count
-- Backup generated on 2026-11-11 09:04:46

CREATE OR REPLACE FUNCTION public.fn_get_branch_classrooms_with_plans_count(p_tenant_id uuid, p_branch_id uuid)  RETURNS integer  LANGUAGE sql  STABLE AS $function$     SELECT COUNT(DISTINCT es.classroom_id)::int     FROM experience_schedules es     JOIN classrooms cl ON es.classroom_id = cl.id     WHERE es.tenant_id = p_tenant_id        AND cl.branch_id = p_branch_id       AND cl.is_active = true       AND COALESCE(cl.classroom_type, '') != 'Pool'       AND COALESCE(cl.environment, '') != 'Virtual'       AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days'; $function$ ;
