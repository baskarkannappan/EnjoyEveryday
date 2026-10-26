-- Function: public.fn_get_board_default_organization_id
-- Backup generated on 2026-10-25 20:33:02

CREATE OR REPLACE FUNCTION public.fn_get_board_default_organization_id(p_tenant_id uuid)  RETURNS uuid  LANGUAGE sql  STABLE AS $function$     SELECT id     FROM organizations     WHERE tenant_id = p_tenant_id AND is_active = true     ORDER BY name     LIMIT 1; $function$ ;
