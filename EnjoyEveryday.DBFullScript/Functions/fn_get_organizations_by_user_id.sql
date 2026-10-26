-- Function: public.fn_get_organizations_by_user_id
-- Backup generated on 2026-10-25 20:33:34

CREATE OR REPLACE FUNCTION public.fn_get_organizations_by_user_id(p_user_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at     FROM organization_memberships om     JOIN organizations o ON om.organization_id = o.id     WHERE om.user_id = p_user_id AND om.status = 'Active'     ORDER BY o.name; END; $function$ ;
