-- Migration: Add account_status and suspension_reason to organizations table, and add sp_update_organization_account_status

-- 1. Add columns to organizations
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS account_status VARCHAR(50) NOT NULL DEFAULT 'Active';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS suspension_reason TEXT;

-- 2. Create procedure to update organization account status
CREATE OR REPLACE PROCEDURE public.sp_update_organization_account_status(
    p_org_id UUID,
    p_status VARCHAR,
    p_reason TEXT DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE organizations
    SET account_status = p_status,
        is_active = (CASE WHEN p_status IN ('Active', 'Trial', 'Onboarding', 'PendingApproval') THEN TRUE ELSE FALSE END),
        suspension_reason = p_reason,
        updated_at = NOW()
    WHERE id = p_org_id;
END;
$$;

-- 3. Update fn_get_platform_organizations_overview to return account_status and suspension_reason
DROP FUNCTION IF EXISTS public.fn_get_platform_organizations_overview();
CREATE OR REPLACE FUNCTION public.fn_get_platform_organizations_overview()
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    name character varying,
    location character varying,
    is_active boolean,
    account_status character varying,
    suspension_reason text,
    created_at timestamp with time zone,
    branch_count integer,
    admin_email character varying
)
LANGUAGE sql
STABLE
AS $function$
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar AS account_status,
           o.suspension_reason,
           o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1) AS admin_email
    FROM organizations o
    ORDER BY o.name;
$function$;

-- 4. Update fn_get_organization_by_id to include account_status and suspension_reason
DROP FUNCTION IF EXISTS public.fn_get_organization_by_id(uuid, uuid);
CREATE OR REPLACE FUNCTION public.fn_get_organization_by_id(p_tenant_id uuid, p_id uuid)
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    name character varying,
    location character varying,
    is_active boolean,
    account_status character varying,
    suspension_reason text,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar,
           o.suspension_reason,
           o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id AND o.tenant_id = p_tenant_id;
END;
$function$;

-- 5. Update fn_get_organization_by_id_global to include account_status and suspension_reason
DROP FUNCTION IF EXISTS public.fn_get_organization_by_id_global(uuid);
CREATE OR REPLACE FUNCTION public.fn_get_organization_by_id_global(p_id uuid)
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    name character varying,
    location character varying,
    is_active boolean,
    account_status character varying,
    suspension_reason text,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar,
           o.suspension_reason,
           o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id;
END;
$function$;

-- 6. Synchronize NEW ORG status to Suspended if present
UPDATE organizations
SET account_status = 'Suspended',
    is_active = FALSE,
    suspension_reason = 'Organization suspended by Platform Administrator.',
    updated_at = NOW()
WHERE name ILIKE '%NEW ORG%';

