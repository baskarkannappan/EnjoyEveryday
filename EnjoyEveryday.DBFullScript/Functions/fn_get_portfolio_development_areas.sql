-- Function: public.fn_get_portfolio_development_areas
-- Backup generated on 2026-10-25 20:33:39

CREATE OR REPLACE FUNCTION public.fn_get_portfolio_development_areas(p_tenant_id uuid)  RETURNS TABLE(id uuid, name character varying, icon character varying, display_order integer)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT da.id, da.name::VARCHAR, da.icon::VARCHAR, da.display_order     FROM development_areas da     WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE     ORDER BY da.display_order; END; $function$ ;
