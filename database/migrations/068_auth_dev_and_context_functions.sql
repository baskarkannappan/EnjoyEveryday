-- Migration: 068_auth_dev_and_context_functions.sql
-- Description: Stored functions for dev login lookup, organization switching validation, and modular auth claims.

CREATE OR REPLACE FUNCTION public.fn_dev_login(
    p_email VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    out_status VARCHAR,
    out_user_id UUID,
    out_tenant_id UUID,
    out_effective_tenant_id UUID,
    out_active_org_id UUID,
    out_email VARCHAR,
    out_display_name VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_target_email VARCHAR;
BEGIN
    IF p_email IS NOT NULL AND TRIM(p_email) <> '' THEN
        SELECT email INTO v_target_email 
        FROM users 
        WHERE LOWER(email) = LOWER(TRIM(p_email)) OR normalized_email = UPPER(TRIM(p_email)) 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        SELECT email INTO v_target_email 
        FROM users 
        WHERE normalized_email = 'PRIYA@LITTLESTARS.COM' 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        SELECT email INTO v_target_email 
        FROM users 
        ORDER BY created_at ASC 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        RETURN QUERY SELECT 'no_users'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
        RETURN;
    END IF;

    RETURN QUERY 
    SELECT * FROM fn_sync_external_login('DevLogin', v_target_email, v_target_email, 'Dev', 'User', NULL, 'Daycare');
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_validate_and_set_organization(
    p_user_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    is_valid BOOLEAN,
    tenant_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_tenant_id UUID;
BEGIN
    IF p_organization_id IS NOT NULL THEN
        SELECT o.tenant_id INTO v_tenant_id
        FROM organization_memberships om
        JOIN organizations o ON om.organization_id = o.id
        WHERE om.user_id = p_user_id
          AND om.organization_id = p_organization_id
          AND om.status = 'Active'
        LIMIT 1;

        IF v_tenant_id IS NOT NULL THEN
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    ELSE
        IF EXISTS (SELECT 1 FROM family_relationships WHERE user_id = p_user_id) THEN
            SELECT u.tenant_id INTO v_tenant_id FROM users u WHERE u.id = p_user_id;
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    END IF;
END;
$$;
