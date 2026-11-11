-- Function: public.fn_get_organization_role_permissions
-- Backup generated on 2026-11-11 09:05:22

CREATE OR REPLACE FUNCTION public.fn_get_organization_role_permissions(p_tenant_id uuid, p_organization_id uuid)  RETURNS TABLE(role_name character varying, feature_key character varying, is_allowed boolean, updated_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT          orp.role_name,         orp.feature_key,         orp.is_allowed,         orp.updated_at     FROM organization_role_permissions orp     WHERE orp.tenant_id = p_tenant_id       AND orp.organization_id = p_organization_id; $function$ ;
