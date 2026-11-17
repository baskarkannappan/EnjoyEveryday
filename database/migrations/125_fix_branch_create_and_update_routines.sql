-- Fix public.fn_create_branch and public.sp_update_branch to match the 10-property Branch entity
-- (which includes operational_status and active_closure_id added to public.branches).

DROP FUNCTION IF EXISTS public.fn_create_branch(UUID, UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_branch(UUID, UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, VARCHAR, UUID, TIMESTAMPTZ, TIMESTAMPTZ);

CREATE OR REPLACE FUNCTION public.fn_create_branch(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_is_active BOOLEAN,
    p_operational_status VARCHAR,
    p_active_closure_id UUID,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE(
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    operational_status VARCHAR,
    active_closure_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    INSERT INTO branches (
        id,
        tenant_id,
        organization_id,
        name,
        location,
        is_active,
        operational_status,
        active_closure_id,
        created_at,
        updated_at
    )
    VALUES (
        p_id,
        p_tenant_id,
        p_organization_id,
        p_name,
        p_location,
        p_is_active,
        COALESCE(NULLIF(p_operational_status, ''), 'Open'),
        p_active_closure_id,
        p_created_at,
        p_updated_at
    )
    RETURNING
        branches.id,
        branches.tenant_id,
        branches.organization_id,
        branches.name,
        branches.location,
        branches.is_active,
        branches.operational_status,
        branches.active_closure_id,
        branches.created_at,
        branches.updated_at;
END;
$function$;

DROP PROCEDURE IF EXISTS public.sp_update_branch(UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_branch(UUID, UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, VARCHAR, UUID, TIMESTAMPTZ, TIMESTAMPTZ);

CREATE OR REPLACE PROCEDURE public.sp_update_branch(
    IN p_id UUID,
    IN p_tenant_id UUID,
    IN p_organization_id UUID,
    IN p_name VARCHAR,
    IN p_location VARCHAR,
    IN p_is_active BOOLEAN,
    IN p_operational_status VARCHAR,
    IN p_active_closure_id UUID,
    IN p_created_at TIMESTAMPTZ,
    IN p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    UPDATE branches
    SET name = p_name,
        location = p_location,
        is_active = p_is_active,
        operational_status = COALESCE(NULLIF(p_operational_status, ''), operational_status),
        active_closure_id = p_active_closure_id,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$procedure$;

