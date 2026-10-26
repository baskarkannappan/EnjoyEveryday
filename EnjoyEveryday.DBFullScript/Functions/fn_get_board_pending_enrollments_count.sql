-- Function: public.fn_get_board_pending_enrollments_count
-- Backup generated on 2026-10-25 20:33:03

CREATE OR REPLACE FUNCTION public.fn_get_board_pending_enrollments_count(p_tenant_id uuid, p_org_id uuid DEFAULT NULL::uuid)  RETURNS integer  LANGUAGE sql  STABLE AS $function$     SELECT COUNT(1)::int     FROM children c     LEFT JOIN classrooms cl ON c.classroom_id = cl.id     LEFT JOIN branches b ON cl.branch_id = b.id     WHERE c.tenant_id = p_tenant_id       AND (p_org_id IS NULL OR b.organization_id = p_org_id OR c.classroom_id IS NULL)       AND (c.enrollment_status = 'Draft' OR c.profile_completion_percentage < 100); $function$ ;
