-- Migration 104: Multi-Organization & Multi-Branch Teacher Management
-- 1. Updates fn_sync_external_login to route multi-org teachers to /select-organization
-- 2. Scopes fn_get_assigned_classroom_ids by organization and branch
-- 3. Adds lifecycle procedures for granular removal (classroom, branch, organization)

-- 1. Update fn_sync_external_login
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
    v_active_org_count INT := 0;
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

        IF v_user_id IS NOT NULL THEN
            IF NOT v_is_active THEN
                RETURN QUERY SELECT 'user_inactive'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
                RETURN;
            END IF;

            INSERT INTO user_external_logins (user_id, provider, provider_key, created_at)
            VALUES (v_user_id, p_provider, p_provider_key, NOW())
            ON CONFLICT (provider, provider_key) DO NOTHING;
        END IF;
    ELSE
        SELECT u.tenant_id, u.is_active, u.first_name, u.last_name, u.display_name, u.email
        INTO v_tenant_id, v_is_active, v_first_name, v_last_name, v_display_name, v_user_email
        FROM users u
        WHERE u.id = v_user_id;

        IF NOT v_is_active THEN
            RETURN QUERY SELECT 'user_inactive'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;
    END IF;

    -- 3. If user does not exist, auto-provision
    IF v_user_id IS NULL THEN
        SELECT t.id INTO v_tenant_id
        FROM tenants t
        WHERE t.is_active = TRUE
        ORDER BY t.created_at ASC
        LIMIT 1;

        IF v_tenant_id IS NULL THEN
            RETURN QUERY SELECT 'no_active_tenant'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;

        v_user_id := gen_random_uuid();
        v_first_name := COALESCE(p_first_name, split_part(p_email, '@', 1));
        v_last_name := COALESCE(p_last_name, '');
        v_display_name := COALESCE(p_display_name, TRIM(v_first_name || ' ' || v_last_name));
        v_user_email := p_email;

        INSERT INTO users (id, tenant_id, email, normalized_email, first_name, last_name, display_name, is_active, email_confirmed, security_stamp, concurrency_stamp, created_at, updated_at)
        VALUES (v_user_id, v_tenant_id, p_email, UPPER(p_email), v_first_name, v_last_name, v_display_name, TRUE, TRUE, gen_random_uuid()::text, gen_random_uuid()::text, NOW(), NOW());

        INSERT INTO user_external_logins (user_id, provider, provider_key, created_at)
        VALUES (v_user_id, p_provider, p_provider_key, NOW())
        ON CONFLICT (provider, provider_key) DO NOTHING;

        -- Default role for new users
        INSERT INTO user_roles (user_id, role_id)
        SELECT v_user_id, r.id
        FROM roles r
        WHERE r.name = 'Educator'
        LIMIT 1;
    END IF;

    -- 4. Portal-specific access check
    IF p_required_portal = 'Platform' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id AND r.name = 'PlatformAdmin'
        ) INTO v_has_access;

        IF NOT v_has_access THEN
            RETURN QUERY SELECT 'platform_access_denied'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;
    ELSIF p_required_portal = 'Parent' THEN
        SELECT EXISTS (
            SELECT 1 FROM child_guardians cg
            JOIN persons p ON cg.person_id = p.id
            WHERE p.user_id = v_user_id
        ) INTO v_has_access;

        IF NOT v_has_access THEN
            RETURN QUERY SELECT 'parent_access_denied'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;
    ELSIF p_required_portal = 'Daycare' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            WHERE uor.user_id = v_user_id
              AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher')
        )
        OR EXISTS (
            SELECT 1 FROM user_branch_assignments uba
            WHERE uba.user_id = v_user_id AND uba.revoked_at IS NULL
        )
        OR EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id
              AND r.name IN ('PlatformAdmin', 'OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'Teacher', 'LeadTeacher')
        ) INTO v_has_access;

        IF NOT v_has_access THEN
            RETURN QUERY SELECT 'daycare_access_denied'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;
    END IF;

    -- 5. Synchronize Google/OAuth profile name if provided
    IF p_display_name IS NOT NULL AND TRIM(p_display_name) <> '' AND LOWER(TRIM(p_display_name)) NOT IN ('teacher', 'educator', 'admin', 'user') THEN
        v_display_name := TRIM(p_display_name);
        v_first_name := COALESCE(NULLIF(TRIM(p_first_name), ''), v_first_name);
        v_last_name := COALESCE(NULLIF(TRIM(p_last_name), ''), v_last_name);

        UPDATE users 
        SET display_name = v_display_name,
            first_name = COALESCE(v_first_name, first_name),
            last_name = COALESCE(v_last_name, last_name),
            email_confirmed = TRUE,
            updated_at = NOW() 
        WHERE id = v_user_id;

        UPDATE teachers
        SET display_name = v_display_name,
            first_name = COALESCE(v_first_name, first_name),
            last_name = COALESCE(v_last_name, last_name),
            updated_at = NOW()
        WHERE user_id = v_user_id;
    ELSE
        UPDATE users SET email_confirmed = TRUE, updated_at = NOW() WHERE id = v_user_id;
    END IF;

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
    -- If user belongs to multiple active organizations, return NULL so they are routed to /select-organization
    SELECT COUNT(DISTINCT om.organization_id) INTO v_active_org_count
    FROM organization_memberships om
    WHERE om.user_id = v_user_id AND om.status = 'Active';

    IF v_active_org_count > 1 THEN
        v_active_org_id := NULL;
    ELSIF v_active_org_count = 1 THEN
        SELECT om.organization_id INTO v_active_org_id
        FROM organization_memberships om
        WHERE om.user_id = v_user_id AND om.status = 'Active'
        LIMIT 1;
    ELSE
        -- Fallback if memberships pending or in transition
        SELECT uor.organization_id INTO v_active_org_id
        FROM user_organization_roles uor
        JOIN roles r ON uor.role_id = r.id
        WHERE uor.user_id = v_user_id 
          AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher')
        LIMIT 1;

        IF v_active_org_id IS NULL THEN
            SELECT uba.organization_id INTO v_active_org_id
            FROM user_branch_assignments uba
            WHERE uba.user_id = v_user_id AND uba.revoked_at IS NULL
            LIMIT 1;
        END IF;
    END IF;

    -- 10. Effective tenant ID
    IF v_active_org_id IS NOT NULL THEN
        SELECT o.tenant_id INTO v_effective_tenant_id
        FROM organizations o
        WHERE o.id = v_active_org_id;
    END IF;
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

-- 2. Scoped fn_get_assigned_classroom_ids
CREATE OR REPLACE FUNCTION public.fn_get_assigned_classroom_ids(
    p_tenant_id uuid,
    p_user_id uuid,
    p_organization_id uuid DEFAULT NULL,
    p_branch_id uuid DEFAULT NULL
)
RETURNS TABLE(classroom_id uuid)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ct.classroom_id
    FROM classroom_teachers ct
    JOIN classrooms c ON c.id = ct.classroom_id
    JOIN branches b ON b.id = c.branch_id
    WHERE ct.tenant_id = p_tenant_id 
      AND ct.user_id = p_user_id
      AND (p_organization_id IS NULL OR b.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR c.branch_id = p_branch_id);
END;
$$;

-- 3. Procedure: sp_revoke_user_branch
CREATE OR REPLACE PROCEDURE public.sp_revoke_user_branch(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_branch_id uuid,
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE user_branch_assignments
    SET revoked_at = NOW()
    WHERE user_id = p_user_id
      AND branch_id = p_branch_id
      AND organization_id = p_organization_id
      AND tenant_id = p_tenant_id;
END;
$$;

-- 4. Procedure: sp_remove_teacher_from_classroom
CREATE OR REPLACE PROCEDURE public.sp_remove_teacher_from_classroom(
    IN p_tenant_id uuid,
    IN p_classroom_id uuid,
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM classroom_teachers
    WHERE classroom_id = p_classroom_id
      AND user_id = p_user_id
      AND tenant_id = p_tenant_id;
END;
$$;

-- 5. Procedure: sp_remove_user_from_organization
CREATE OR REPLACE PROCEDURE public.sp_remove_user_from_organization(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- Inactivate membership in this organization ONLY
    UPDATE organization_memberships
    SET status = 'Inactive', updated_at = NOW()
    WHERE user_id = p_user_id
      AND organization_id = p_organization_id
      AND tenant_id = p_tenant_id;

    -- Revoke branch assignments in this organization ONLY
    UPDATE user_branch_assignments
    SET revoked_at = NOW()
    WHERE user_id = p_user_id
      AND organization_id = p_organization_id
      AND tenant_id = p_tenant_id;

    -- Remove classroom assignments belonging to this organization
    DELETE FROM classroom_teachers ct
    USING classrooms c, branches b
    WHERE ct.classroom_id = c.id
      AND c.branch_id = b.id
      AND b.organization_id = p_organization_id
      AND ct.user_id = p_user_id
      AND ct.tenant_id = p_tenant_id;
END;
$$;

