-- Function: public.fn_get_branch_enrolled_children_count
-- Backup generated on 2026-10-25 20:33:05

CREATE OR REPLACE FUNCTION public.fn_get_branch_enrolled_children_count(p_tenant_id uuid, p_branch_id uuid)  RETURNS integer  LANGUAGE sql  STABLE AS $function$     SELECT COUNT(*)::int     FROM children c     JOIN classrooms cl ON c.classroom_id = cl.id     WHERE c.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id AND c.is_active = TRUE; $function$ ;
