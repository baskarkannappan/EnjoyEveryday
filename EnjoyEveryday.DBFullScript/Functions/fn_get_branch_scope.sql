-- Function: public.fn_get_branch_scope
-- Backup generated on 2026-10-25 20:33:06

CREATE OR REPLACE FUNCTION public.fn_get_branch_scope(p_tenant_id uuid, p_branch_id uuid)  RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT b.tenant_id, b.organization_id, b.id AS branch_id, NULL::uuid AS classroom_id     FROM branches b WHERE b.id = p_branch_id AND b.tenant_id = p_tenant_id; END; $function$ ;
