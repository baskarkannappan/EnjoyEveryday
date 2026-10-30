-- Ensure fn_get_branches_for_benchmarking only returns active branches
CREATE OR REPLACE FUNCTION public.fn_get_branches_for_benchmarking(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    name VARCHAR,
    location VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    WITH org_branches AS (
        SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
        FROM branches b
        WHERE b.tenant_id = p_tenant_id 
          AND b.organization_id = p_organization_id
          AND b.is_active = true
        ORDER BY b.name
    )
    SELECT * FROM org_branches
    UNION ALL
    SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
    FROM branches b
    WHERE b.tenant_id = p_tenant_id
      AND b.is_active = true
      AND NOT EXISTS (SELECT 1 FROM org_branches)
    ORDER BY name;
$$;
