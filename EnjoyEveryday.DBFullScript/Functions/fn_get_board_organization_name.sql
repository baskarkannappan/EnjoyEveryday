-- Function: public.fn_get_board_organization_name
-- Backup generated on 2026-10-25 20:33:03

CREATE OR REPLACE FUNCTION public.fn_get_board_organization_name(p_tenant_id uuid, p_org_id uuid)  RETURNS character varying  LANGUAGE sql  STABLE AS $function$     SELECT name     FROM organizations     WHERE id = p_org_id AND tenant_id = p_tenant_id; $function$ ;
