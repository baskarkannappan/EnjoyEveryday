-- Function: public.fn_get_assigned_branch_ids
-- Backup generated on 2026-10-25 20:33:01

CREATE OR REPLACE FUNCTION public.fn_get_assigned_branch_ids(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid)  RETURNS TABLE(branch_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT uba.branch_id     FROM user_branch_assignments uba     WHERE uba.tenant_id = p_tenant_id AND uba.organization_id = p_organization_id AND uba.user_id = p_user_id AND uba.revoked_at IS NULL; END; $function$ ;
