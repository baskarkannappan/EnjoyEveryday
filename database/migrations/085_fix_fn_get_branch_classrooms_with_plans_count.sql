-- Migration 085: Fix fn_get_branch_classrooms_with_plans_count to only count active classrooms
CREATE OR REPLACE FUNCTION public.fn_get_branch_classrooms_with_plans_count(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(DISTINCT es.classroom_id)::int
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    WHERE es.tenant_id = p_tenant_id 
      AND cl.branch_id = p_branch_id
      AND cl.is_active = true
      AND COALESCE(cl.classroom_type, '') != 'Pool'
      AND COALESCE(cl.environment, '') != 'Virtual'
      AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days';
$$;
