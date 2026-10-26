-- Function: public.fn_get_organization_role_names
-- Backup generated on 2026-10-25 20:33:33

CREATE OR REPLACE FUNCTION public.fn_get_organization_role_names(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid)  RETURNS TABLE(role_name character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT r.name::VARCHAR     FROM user_organization_roles uor     JOIN roles r ON r.id = uor.role_id AND r.tenant_id = uor.tenant_id     JOIN organization_memberships m ON m.user_id = uor.user_id AND m.organization_id = uor.organization_id AND m.tenant_id = uor.tenant_id     WHERE uor.tenant_id = p_tenant_id AND uor.organization_id = p_organization_id AND uor.user_id = p_user_id       AND m.status = 'Active'; END; $function$ ;
