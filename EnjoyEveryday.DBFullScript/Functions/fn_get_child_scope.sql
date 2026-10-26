-- Function: public.fn_get_child_scope
-- Backup generated on 2026-10-25 20:33:10

CREATE OR REPLACE FUNCTION public.fn_get_child_scope(p_tenant_id uuid, p_child_id uuid)  RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ch.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id     FROM children ch     LEFT JOIN classrooms cl ON cl.id = ch.classroom_id AND cl.tenant_id = ch.tenant_id     LEFT JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id     WHERE ch.id = p_child_id AND ch.tenant_id = p_tenant_id; END; $function$ ;
