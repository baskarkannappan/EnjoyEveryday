-- Function: public.fn_get_all_development_areas
-- Backup generated on 2026-10-25 20:32:59

DROP FUNCTION IF EXISTS public.fn_get_all_development_areas();
DROP FUNCTION IF EXISTS public.fn_get_all_development_areas(UUID);

CREATE OR REPLACE FUNCTION public.fn_get_all_development_areas(p_tenant_id uuid DEFAULT NULL::uuid)
RETURNS TABLE(id uuid, tenant_id uuid, name character varying, description text, icon character varying, display_order integer, is_active boolean)
LANGUAGE plpgsql
STABLE
AS $function$
BEGIN
    RETURN QUERY
    SELECT da.id, da.tenant_id, da.name::VARCHAR, da.description, da.icon::VARCHAR,
           da.display_order, da.is_active
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$function$;
