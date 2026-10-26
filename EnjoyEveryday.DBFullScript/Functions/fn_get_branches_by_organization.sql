-- Function: public.fn_get_branches_by_organization
-- Backup generated on 2026-10-25 20:33:06

CREATE OR REPLACE FUNCTION public.fn_get_branches_by_organization(p_tenant_id uuid, p_organization_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT b.id, b.tenant_id, b.organization_id, b.name, b.location, b.is_active, b.created_at, b.updated_at     FROM branches b     WHERE b.organization_id = p_organization_id AND b.tenant_id = p_tenant_id     ORDER BY b.name; END; $function$ ;
