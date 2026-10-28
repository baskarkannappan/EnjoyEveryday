-- ============================================================
-- EnjoyEveryday — Migration 077: Organization Role Permissions Matrix
-- Allows Organization Admins to customize page and functionality access 
-- per role strictly scoped to their specific organization.
-- ============================================================

CREATE TABLE IF NOT EXISTS organization_role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    role_name VARCHAR(100) NOT NULL,
    feature_key VARCHAR(100) NOT NULL,
    is_allowed BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_org_role_feature UNIQUE (tenant_id, organization_id, role_name, feature_key)
);

CREATE INDEX IF NOT EXISTS idx_org_role_perms_lookup 
    ON organization_role_permissions(tenant_id, organization_id, role_name);

-- Helper function to fetch all permission overrides for an organization
CREATE OR REPLACE FUNCTION fn_get_organization_role_permissions(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    role_name VARCHAR(100),
    feature_key VARCHAR(100),
    is_allowed BOOLEAN,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        orp.role_name,
        orp.feature_key,
        orp.is_allowed,
        orp.updated_at
    FROM organization_role_permissions orp
    WHERE orp.tenant_id = p_tenant_id
      AND orp.organization_id = p_organization_id;
$$;

-- Helper procedure to upsert a role permission override for an organization
CREATE OR REPLACE PROCEDURE sp_upsert_organization_role_permission(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_role_name VARCHAR(100),
    p_feature_key VARCHAR(100),
    p_is_allowed BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO organization_role_permissions (
        tenant_id,
        organization_id,
        role_name,
        feature_key,
        is_allowed,
        updated_at
    )
    VALUES (
        p_tenant_id,
        p_organization_id,
        p_role_name,
        p_feature_key,
        p_is_allowed,
        NOW()
    )
    ON CONFLICT (tenant_id, organization_id, role_name, feature_key)
    DO UPDATE SET
        is_allowed = EXCLUDED.is_allowed,
        updated_at = NOW();
END;
$$;

-- Helper procedure to reset all customized permissions for an organization
CREATE OR REPLACE PROCEDURE sp_reset_organization_role_permissions(
    p_tenant_id UUID,
    p_organization_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM organization_role_permissions
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_organization_id;
END;
$$;
