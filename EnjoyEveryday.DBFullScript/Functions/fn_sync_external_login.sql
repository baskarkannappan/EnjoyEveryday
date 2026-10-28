-- Function: public.fn_sync_external_login
-- Updated to include BranchAdmin and branch assignments for portal access

CREATE OR REPLACE FUNCTION public.fn_sync_external_login(
    p_provider VARCHAR,
    p_provider_key VARCHAR,
    p_email VARCHAR,
    p_first_name VARCHAR DEFAULT NULL,
    p_last_name VARCHAR DEFAULT NULL,
    p_display_name VARCHAR DEFAULT NULL,
    p_required_portal VARCHAR DEFAULT 'Daycare'
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
    v_user_id UUID;
    v_tenant_id UUID;
    v_effective_tenant_id UUID;
    v_is_active BOOLEAN;
    v_first_name VARCHAR;
    v_last_name VARCHAR;
    v_display_name VARCHAR;
    v_user_email VARCHAR;
    v_has_access BOOLEAN := FALSE;
    v_active_org_id UUID;
    v_default_org_id UUID;
BEGIN
    -- 1. Check if external login is already linked
    SELECT uel.user_id INTO v_user_id
    FROM user_external_logins uel
    WHERE uel.provider = p_provider AND uel.provider_key = p_provider_key
    LIMIT 1;

    -- 2. If not linked, check by normalized email
    IF v_user_id IS NULL THEN
        SELECT u.id, u.tenant_id, u.is_active, u.first_name, u.last_name, u.display_name, u.email
        INTO v_user_id, v_tenant_id, v_is_active, v_first_name, v_last_name, v_display_name, v_user_email
        FROM users u
        WHERE u.normalized_email = UPPER(p_email)
        LIMIT 1;

        IF v_user_id IS NULL THEN
            RETURN QUERY SELECT 'not_registered'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, p_email, NULL::VARCHAR;
            RETURN;
        END IF;
    ELSE
        SELECT u.id, u.tenant_id, u.is_active, u.first_name, u.last_name, u.display_name, u.email
        INTO v_user_id, v_tenant_id, v_is_active, v_first_name, v_last_name, v_display_name, v_user_email
        FROM users u
        WHERE u.id = v_user_id;
    END IF;

    -- 3. Check portal access
    IF p_required_portal = 'Daycare' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id AND r.name IN ('PlatformAdmin', 'SuperAdmin', 'OrganizationAdmin', 'OrganizationOwner', 'BranchAdmin')
            UNION
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            WHERE uor.user_id = v_user_id AND r.name IN ('DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher', 'AssistantTeacher', 'OrganizationAdmin', 'OrganizationOwner', 'BranchAdmin')
            UNION
            SELECT 1 FROM teachers t
            WHERE t.user_id = v_user_id
            UNION
            SELECT 1 FROM user_branch_assignments uba
            WHERE uba.user_id = v_user_id AND uba.revoked_at IS NULL
        ) INTO v_has_access;
    ELSIF p_required_portal = 'Admin' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id AND r.name IN ('PlatformAdmin', 'SuperAdmin')
            UNION
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            WHERE uor.user_id = v_user_id AND r.name IN ('OrganizationAdmin', 'OrganizationOwner', 'BranchAdmin')
        ) INTO v_has_access;
    ELSE
        v_has_access := TRUE;
    END IF;

    IF NOT v_has_access THEN
        RETURN QUERY SELECT 'insufficient_permissions'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, NULL::VARCHAR;
        RETURN;
    END IF;

    -- 4. Check if account is active
    IF v_is_active IS NOT TRUE THEN
        RETURN QUERY SELECT 'account_disabled'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, NULL::VARCHAR;
        RETURN;
    END IF;

    -- 5. Link Google external login and confirm email
    INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
    VALUES (p_provider, p_provider_key, v_user_id, NOW())
    ON CONFLICT (provider, provider_key) DO NOTHING;

    UPDATE users SET email_confirmed = TRUE, updated_at = NOW() WHERE id = v_user_id;

    -- 6. Resolve display name
    IF v_display_name IS NULL OR TRIM(v_display_name) = '' THEN
        v_display_name := NULLIF(TRIM(COALESCE(v_first_name, '') || ' ' || COALESCE(v_last_name, '')), '');
    END IF;
    IF v_display_name IS NULL OR TRIM(v_display_name) = '' THEN
        v_display_name := COALESCE(p_display_name, p_first_name, v_user_email, 'Teacher');
    END IF;

    -- 7. Ensure teacher profile exists if Daycare portal
    IF p_required_portal = 'Daycare' THEN
        INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
        SELECT gen_random_uuid(), v_tenant_id, v_user_id, COALESCE(p_first_name, v_first_name), COALESCE(p_last_name, v_last_name), v_display_name, 'Active', NOW(), NOW()
        WHERE NOT EXISTS (SELECT 1 FROM teachers tch WHERE tch.user_id = v_user_id);
    END IF;

    -- 8. Ensure organization membership
    IF NOT EXISTS (SELECT 1 FROM organization_memberships om WHERE om.user_id = v_user_id AND om.status = 'Active') THEN
        SELECT id INTO v_default_org_id FROM organizations WHERE tenant_id = v_tenant_id LIMIT 1;
        IF v_default_org_id IS NOT NULL THEN
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
            VALUES (v_user_id, v_default_org_id, v_tenant_id, 'Active', NOW(), NOW())
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 9. Resolve active organization ID
    SELECT uor.organization_id INTO v_active_org_id
    FROM user_organization_roles uor
    JOIN roles r ON uor.role_id = r.id
    JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
    WHERE uor.user_id = v_user_id 
      AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher')
    LIMIT 1;

    IF v_active_org_id IS NULL THEN
        SELECT uba.organization_id INTO v_active_org_id
        FROM user_branch_assignments uba
        JOIN organization_memberships om ON om.user_id = uba.user_id AND om.organization_id = uba.organization_id AND om.status = 'Active'
        WHERE uba.user_id = v_user_id AND uba.revoked_at IS NULL
        LIMIT 1;
    END IF;

    IF v_active_org_id IS NULL THEN
        SELECT om.organization_id INTO v_active_org_id
        FROM organization_memberships om
        WHERE om.user_id = v_user_id AND om.status = 'Active'
        LIMIT 1;
    END IF;

    IF v_active_org_id IS NULL THEN
        RETURN QUERY SELECT 'no_organizations'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, v_display_name;
        RETURN;
    END IF;

    -- 10. Resolve effective tenant ID
    SELECT o.tenant_id INTO v_effective_tenant_id
    FROM organizations o
    WHERE o.id = v_active_org_id;

    IF v_effective_tenant_id IS NULL THEN
        v_effective_tenant_id := v_tenant_id;
    END IF;

    RETURN QUERY SELECT 
        'success'::VARCHAR,
        v_user_id,
        v_tenant_id,
        v_effective_tenant_id,
        v_active_org_id,
        v_user_email,
        v_display_name;
END;
$$;
