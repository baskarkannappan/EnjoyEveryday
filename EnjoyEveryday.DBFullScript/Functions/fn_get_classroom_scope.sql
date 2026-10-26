-- Function: public.fn_get_classroom_scope
-- Backup generated on 2026-10-25 20:33:11

CREATE OR REPLACE FUNCTION public.fn_get_classroom_scope(p_tenant_id uuid, p_classroom_id uuid)  RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cl.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id     FROM classrooms cl JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id     WHERE cl.id = p_classroom_id AND cl.tenant_id = p_tenant_id; END; $function$ ;
