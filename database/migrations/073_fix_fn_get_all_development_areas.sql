-- =========================================================================
-- Migration: 073_fix_fn_get_all_development_areas.sql
-- Description: Fix public.fn_get_all_development_areas to accurately reflect
--              the development_areas schema (removes non-existent columns:
--              code, color_hex, created_at, updated_at).
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_all_development_areas();
DROP FUNCTION IF EXISTS public.fn_get_all_development_areas(UUID);

CREATE OR REPLACE FUNCTION public.fn_get_all_development_areas(
    p_tenant_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    description TEXT,
    icon VARCHAR,
    display_order INT,
    is_active BOOLEAN
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT da.id,
           da.tenant_id,
           da.name::VARCHAR,
           da.description,
           da.icon::VARCHAR,
           da.display_order,
           da.is_active
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id)
      AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$$;
