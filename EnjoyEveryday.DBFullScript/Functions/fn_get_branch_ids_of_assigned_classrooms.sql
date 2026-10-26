-- Function: public.fn_get_branch_ids_of_assigned_classrooms
-- Backup generated on 2026-10-25 20:33:05

CREATE OR REPLACE FUNCTION public.fn_get_branch_ids_of_assigned_classrooms(p_tenant_id uuid, p_user_id uuid)  RETURNS TABLE(branch_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT DISTINCT cl.branch_id     FROM classroom_teachers ct     JOIN classrooms cl ON cl.id = ct.classroom_id AND cl.tenant_id = ct.tenant_id     WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id; END; $function$ ;
