-- Function: public.fn_get_organization_by_id
-- Backup generated on 2026-10-25 20:33:33

CREATE OR REPLACE FUNCTION public.fn_get_organization_by_id(p_tenant_id uuid, p_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at     FROM organizations o     WHERE o.id = p_id AND o.tenant_id = p_tenant_id; END; $function$ ;
