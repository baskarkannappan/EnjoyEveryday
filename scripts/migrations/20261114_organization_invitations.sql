-- 20261114_organization_invitations.sql
-- Organization Invitations & Magic Activation Workflow

CREATE TABLE IF NOT EXISTS organization_invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    admin_email VARCHAR(256) NOT NULL,
    token VARCHAR(128) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    expires_at TIMESTAMPTZ NOT NULL,
    accepted_at TIMESTAMPTZ,
    accepted_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_org_invitations_token ON organization_invitations(token);
CREATE INDEX IF NOT EXISTS idx_org_invitations_org ON organization_invitations(organization_id);
CREATE INDEX IF NOT EXISTS idx_org_invitations_email ON organization_invitations(admin_email);

-- Create or replace invitation dispatch
CREATE OR REPLACE FUNCTION fn_create_organization_invitation(
    p_organization_id UUID,
    p_admin_email VARCHAR,
    p_token VARCHAR,
    p_expires_days INT DEFAULT 7
)
RETURNS TABLE (
    out_id UUID,
    out_token VARCHAR,
    out_expires_at TIMESTAMPTZ,
    out_status VARCHAR
)
LANGUAGE plpgsql
AS $func$
DECLARE
    v_tenant_id UUID;
    v_invitation_id UUID := gen_random_uuid();
    v_expires_at TIMESTAMPTZ := now() + (p_expires_days || ' days')::INTERVAL;
BEGIN
    SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_tenant_id IS NULL THEN
        v_tenant_id := p_organization_id;
    END IF;

    -- Revoke any prior pending invitations for this org & email
    UPDATE organization_invitations
    SET status = 'Revoked', updated_at = now()
    WHERE organization_id = p_organization_id 
      AND admin_email = p_admin_email
      AND status = 'Pending';

    INSERT INTO organization_invitations (id, organization_id, tenant_id, admin_email, token, status, expires_at, created_at, updated_at)
    VALUES (v_invitation_id, p_organization_id, v_tenant_id, p_admin_email, p_token, 'Pending', v_expires_at, now(), now());

    RETURN QUERY
    SELECT v_invitation_id, p_token, v_expires_at, 'Pending'::VARCHAR;
END;
$func$;

-- Latest invitations per organization
CREATE OR REPLACE FUNCTION fn_get_organization_latest_invitations()
RETURNS TABLE (
    out_organization_id UUID,
    out_admin_email VARCHAR,
    out_token VARCHAR,
    out_status VARCHAR,
    out_expires_at TIMESTAMPTZ,
    out_created_at TIMESTAMPTZ,
    out_accepted_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $func$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ON (i.organization_id)
        i.organization_id,
        i.admin_email,
        i.token,
        i.status::VARCHAR,
        i.expires_at,
        i.created_at,
        i.accepted_at
    FROM organization_invitations i
    ORDER BY i.organization_id, i.created_at DESC;
END;
$func$;

-- Validate invitation token
CREATE OR REPLACE FUNCTION fn_validate_organization_invitation(p_token VARCHAR)
RETURNS TABLE (
    out_valid BOOLEAN,
    out_invitation_id UUID,
    out_organization_id UUID,
    out_tenant_id UUID,
    out_organization_name VARCHAR,
    out_admin_email VARCHAR,
    out_expires_at TIMESTAMPTZ,
    out_status VARCHAR
)
LANGUAGE plpgsql
AS $func$
DECLARE
    r_inv RECORD;
    v_org_name VARCHAR;
BEGIN
    SELECT * INTO r_inv
    FROM organization_invitations
    WHERE token = p_token
    LIMIT 1;

    IF r_inv IS NULL THEN
        RETURN QUERY SELECT FALSE, NULL::UUID, NULL::UUID, NULL::UUID, ''::VARCHAR, ''::VARCHAR, NULL::TIMESTAMPTZ, 'NotFound'::VARCHAR;
        RETURN;
    END IF;

    IF r_inv.status <> 'Pending' THEN
        RETURN QUERY SELECT FALSE, r_inv.id, r_inv.organization_id, r_inv.tenant_id, ''::VARCHAR, r_inv.admin_email, r_inv.expires_at, r_inv.status::VARCHAR;
        RETURN;
    END IF;

    IF r_inv.expires_at < now() THEN
        UPDATE organization_invitations SET status = 'Expired', updated_at = now() WHERE id = r_inv.id;
        RETURN QUERY SELECT FALSE, r_inv.id, r_inv.organization_id, r_inv.tenant_id, ''::VARCHAR, r_inv.admin_email, r_inv.expires_at, 'Expired'::VARCHAR;
        RETURN;
    END IF;

    SELECT name INTO v_org_name FROM organizations WHERE id = r_inv.organization_id;

    RETURN QUERY SELECT TRUE, r_inv.id, r_inv.organization_id, r_inv.tenant_id, COALESCE(v_org_name, 'Daycare Organization')::VARCHAR, r_inv.admin_email, r_inv.expires_at, 'Pending'::VARCHAR;
END;
$func$;

-- Accept invitation
CREATE OR REPLACE FUNCTION fn_accept_organization_invitation(
    p_token VARCHAR,
    p_user_id UUID
)
RETURNS TABLE (
    out_success BOOLEAN,
    out_organization_id UUID,
    out_tenant_id UUID,
    out_role_name VARCHAR
)
LANGUAGE plpgsql
AS $func$
DECLARE
    r_inv RECORD;
    v_role_id UUID;
BEGIN
    SELECT * INTO r_inv
    FROM organization_invitations
    WHERE token = p_token AND status = 'Pending' AND expires_at > now()
    LIMIT 1;

    IF r_inv IS NULL THEN
        RETURN QUERY SELECT FALSE, NULL::UUID, NULL::UUID, ''::VARCHAR;
        RETURN;
    END IF;

    -- 1. Mark accepted
    UPDATE organization_invitations
    SET status = 'Accepted',
        accepted_at = now(),
        accepted_by_user_id = p_user_id,
        updated_at = now()
    WHERE id = r_inv.id;

    -- 2. Ensure membership in organization_memberships
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
    VALUES (p_user_id, r_inv.organization_id, r_inv.tenant_id, 'Active')
    ON CONFLICT (user_id, organization_id)
    DO UPDATE SET status = 'Active', updated_at = now();

    -- 3. Resolve OrganizationAdmin role
    SELECT id INTO v_role_id
    FROM roles
    WHERE name IN ('OrganizationAdmin', 'OrganizationOwner')
      AND (tenant_id = r_inv.tenant_id OR tenant_id IS NULL)
    ORDER BY tenant_id NULLS LAST
    LIMIT 1;

    IF v_role_id IS NOT NULL THEN
        INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
        VALUES (p_user_id, v_role_id, r_inv.organization_id, r_inv.tenant_id)
        ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;
    END IF;

    RETURN QUERY SELECT TRUE, r_inv.organization_id, r_inv.tenant_id, 'OrganizationAdmin'::VARCHAR;
END;
$func$;

