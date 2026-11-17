-- Migration 127: Multi-Organization Deactivation and Strict Account Isolation
-- Rules:
-- 1. If deactivated from 1 organization and user belongs to only 1 org: login blocked globally (users.is_active = false, status = user_inactive/no_active_organizations).
-- 2. If deactivated from 1 organization and user belongs to multiple orgs: user can log in, but cannot see/select the deactivated org, and is automatically scoped to remaining active orgs.
-- 3. If a child is deactivated (is_active = false, enrollment_status = 'Withdrawn'):
--    - Parent with only that child cannot log in to Parent Portal (parent_access_denied).
--    - Parent with other active children can log in, but deactivated child and their data are completely hidden.

-- 1. sp_remove_user_from_organization
CREATE OR REPLACE PROCEDURE public.sp_remove_user_from_organization(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_remaining_active_orgs INT := 0;
BEGIN
    -- 1. Inactivate membership in this organization (insert if not exists to record explicit Inactive status)
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
    VALUES (p_user_id, p_organization_id, p_tenant_id, 'Inactive', NOW(), NOW())
    ON CONFLICT (user_id, organization_id) DO UPDATE
    SET status = 'Inactive', updated_at = NOW();

    -- 2. Revoke / delete roles in this organization
    DELETE FROM user_organization_roles
    WHERE user_id = p_user_id
      AND organization_id = p_organization_id;

    -- 3. Revoke branch assignments in this organization
    UPDATE user_branch_assignments uba
    SET revoked_at = NOW()
    FROM branches b
    WHERE uba.branch_id = b.id
      AND b.organization_id = p_organization_id
      AND uba.user_id = p_user_id
      AND uba.revoked_at IS NULL;

    -- 4. Remove classroom assignments in this organization
    DELETE FROM classroom_teachers ct
    USING classrooms c, branches b
    WHERE ct.classroom_id = c.id
      AND c.branch_id = b.id
      AND b.organization_id = p_organization_id
      AND ct.user_id = p_user_id;

    -- 5. Count remaining active organization memberships
    SELECT COUNT(DISTINCT om.organization_id) INTO v_remaining_active_orgs
    FROM organization_memberships om
    JOIN organizations o ON om.organization_id = o.id
    WHERE om.user_id = p_user_id
      AND om.status = 'Active'
      AND o.is_active = TRUE;

    -- Also check if user has active assignments in other organizations without explicit membership record
    IF v_remaining_active_orgs = 0 THEN
        SELECT COUNT(DISTINCT o.id) INTO v_remaining_active_orgs
        FROM organizations o
        WHERE o.is_active = TRUE
          AND o.id <> p_organization_id
          AND NOT EXISTS (
              SELECT 1 FROM organization_memberships om_check
              WHERE om_check.user_id = p_user_id AND om_check.organization_id = o.id AND om_check.status = 'Inactive'
          )
          AND (
              EXISTS (SELECT 1 FROM user_organization_roles uor WHERE uor.user_id = p_user_id AND uor.organization_id = o.id)
              OR EXISTS (SELECT 1 FROM user_branch_assignments uba JOIN branches b ON uba.branch_id = b.id WHERE uba.user_id = p_user_id AND b.organization_id = o.id AND uba.revoked_at IS NULL)
              OR EXISTS (SELECT 1 FROM classroom_teachers ct JOIN classrooms c ON ct.classroom_id = c.id JOIN branches b ON c.branch_id = b.id WHERE ct.user_id = p_user_id AND b.organization_id = o.id)
          );
    END IF;

    -- 6. If user has no remaining active organizations, deactivate user account and teacher profile globally
    IF v_remaining_active_orgs = 0 THEN
        UPDATE users
        SET is_active = FALSE, updated_at = NOW()
        WHERE id = p_user_id;

        UPDATE teachers
        SET status = 'Inactive', updated_at = NOW()
        WHERE user_id = p_user_id;
    END IF;
END;
$procedure$;

-- 2. sp_delete_teacher
CREATE OR REPLACE PROCEDURE public.sp_delete_teacher(
    IN p_tenant_id uuid,
    IN p_id uuid,
    IN p_organization_id uuid DEFAULT NULL::uuid
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_user_id UUID;
    v_org_id UUID := p_organization_id;
BEGIN
    SELECT user_id INTO v_user_id FROM teachers WHERE id = p_id;

    IF v_org_id IS NULL THEN
        SELECT id INTO v_org_id FROM organizations WHERE tenant_id = p_tenant_id LIMIT 1;
    END IF;

    UPDATE teachers SET status = 'Inactive', updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;

    IF v_user_id IS NOT NULL AND v_org_id IS NOT NULL THEN
        CALL sp_remove_user_from_organization(p_tenant_id, v_org_id, v_user_id);
    ELSIF v_user_id IS NOT NULL THEN
        UPDATE users SET is_active = FALSE, updated_at = NOW() WHERE id = v_user_id;
    END IF;
END;
$procedure$;

-- 3. sp_delete_user
CREATE OR REPLACE PROCEDURE public.sp_delete_user(IN p_tenant_id uuid, IN p_id uuid)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_org_id UUID;
BEGIN
    SELECT id INTO v_org_id FROM organizations WHERE tenant_id = p_tenant_id LIMIT 1;

    IF v_org_id IS NOT NULL THEN
        CALL sp_remove_user_from_organization(p_tenant_id, v_org_id, p_id);
    ELSE
        UPDATE users SET is_active = FALSE, updated_at = NOW() WHERE id = p_id;
        UPDATE teachers SET status = 'Inactive', updated_at = NOW() WHERE user_id = p_id;
        UPDATE organization_memberships SET status = 'Inactive', updated_at = NOW() WHERE user_id = p_id;
        DELETE FROM user_organization_roles WHERE user_id = p_id;
        UPDATE user_branch_assignments SET revoked_at = NOW() WHERE user_id = p_id AND revoked_at IS NULL;
        DELETE FROM classroom_teachers WHERE user_id = p_id;
    END IF;
END;
$procedure$;

-- 4. sp_delete_child
CREATE OR REPLACE PROCEDURE public.sp_delete_child(IN p_tenant_id uuid, IN p_id uuid)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    UPDATE children
    SET is_active = FALSE,
        enrollment_status = 'Withdrawn',
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;

    UPDATE family_relationships
    SET status = 'Revoked',
        revoked_at = NOW()
    WHERE child_id = p_id;
END;
$procedure$;

-- 5. fn_get_organizations_by_user_id
CREATE OR REPLACE FUNCTION public.fn_get_organizations_by_user_id(p_user_id uuid)
RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
LANGUAGE plpgsql
SECURITY DEFINER
AS $function$
BEGIN
    -- If user is globally inactive, return nothing
    IF NOT EXISTS (SELECT 1 FROM users u WHERE u.id = p_user_id AND u.is_active = TRUE) THEN
        RETURN;
    END IF;

    RETURN QUERY
    WITH candidate_orgs AS (
        -- Direct active organization memberships
        SELECT om.organization_id
        FROM organization_memberships om
        WHERE om.user_id = p_user_id AND om.status = 'Active'

        UNION

        -- Roles assigned within an organization
        SELECT uor.organization_id
        FROM user_organization_roles uor
        WHERE uor.user_id = p_user_id AND uor.organization_id IS NOT NULL

        UNION

        -- Active branch assignments linked to organizations
        SELECT b.organization_id
        FROM user_branch_assignments uba
        JOIN branches b ON uba.branch_id = b.id
        WHERE uba.user_id = p_user_id AND uba.revoked_at IS NULL AND b.organization_id IS NOT NULL

        UNION

        -- Active classroom assignments linked to organizations
        SELECT b.organization_id
        FROM classroom_teachers ct
        JOIN classrooms c ON ct.classroom_id = c.id
        JOIN branches b ON c.branch_id = b.id
        WHERE ct.user_id = p_user_id AND b.organization_id IS NOT NULL

        UNION

        -- User primary tenant organization match (ONLY if user has no explicit inactive membership for it)
        SELECT o.id as organization_id
        FROM users u
        JOIN organizations o ON o.tenant_id = u.tenant_id
        WHERE u.id = p_user_id AND o.is_active = TRUE
          AND NOT EXISTS (
              SELECT 1 FROM organization_memberships om_check
              WHERE om_check.user_id = p_user_id AND om_check.organization_id = o.id AND om_check.status = 'Inactive'
          )
    )
    SELECT DISTINCT
        o.id,
        o.tenant_id,
        o.name,
        o.location,
        o.is_active,
        o.created_at,
        o.updated_at
    FROM candidate_orgs c
    JOIN organizations o ON c.organization_id = o.id
    WHERE o.is_active = TRUE
      -- STRICT EXCLUSION: If organization is explicitly marked Inactive in organization_memberships, DO NOT RETURN IT!
      AND NOT EXISTS (
          SELECT 1 FROM organization_memberships om_inact
          WHERE om_inact.user_id = p_user_id
            AND om_inact.organization_id = o.id
            AND om_inact.status = 'Inactive'
      )
    ORDER BY o.name;
END;
$function$;

-- 6. fn_validate_and_set_organization
CREATE OR REPLACE FUNCTION public.fn_validate_and_set_organization(
    p_user_id uuid,
    p_organization_id uuid DEFAULT NULL::uuid
)
RETURNS TABLE(is_valid boolean, tenant_id uuid)
LANGUAGE plpgsql
SECURITY DEFINER
AS $function$
DECLARE
    v_tenant_id UUID;
    v_is_user_active BOOLEAN;
BEGIN
    -- Check user active status
    SELECT is_active INTO v_is_user_active FROM users WHERE id = p_user_id;
    IF v_is_user_active IS NOT TRUE THEN
        RETURN QUERY SELECT FALSE, NULL::UUID;
        RETURN;
    END IF;

    IF p_organization_id IS NOT NULL THEN
        -- Check if user is explicitly deactivated from this organization
        IF EXISTS (
            SELECT 1 FROM organization_memberships om
            WHERE om.user_id = p_user_id AND om.organization_id = p_organization_id AND om.status = 'Inactive'
        ) THEN
            RETURN QUERY SELECT FALSE, NULL::UUID;
            RETURN;
        END IF;

        -- Check if organization is accessible via active memberships, roles, branch assignments or classroom assignments
        SELECT o.tenant_id INTO v_tenant_id
        FROM organizations o
        WHERE o.id = p_organization_id
          AND o.is_active = TRUE
          AND (
              EXISTS (
                  SELECT 1 FROM organization_memberships om
                  WHERE om.user_id = p_user_id AND om.organization_id = p_organization_id AND om.status = 'Active'
              )
              OR EXISTS (
                  SELECT 1 FROM user_organization_roles uor
                  WHERE uor.user_id = p_user_id AND uor.organization_id = p_organization_id
              )
              OR EXISTS (
                  SELECT 1 FROM user_branch_assignments uba
                  JOIN branches b ON uba.branch_id = b.id
                  WHERE uba.user_id = p_user_id AND b.organization_id = p_organization_id AND uba.revoked_at IS NULL
              )
              OR EXISTS (
                  SELECT 1 FROM classroom_teachers ct
                  JOIN classrooms c ON ct.classroom_id = c.id
                  JOIN branches b ON c.branch_id = b.id
                  WHERE ct.user_id = p_user_id AND b.organization_id = p_organization_id
              )
              OR EXISTS (
                  SELECT 1 FROM users u
                  WHERE u.id = p_user_id AND u.tenant_id = o.tenant_id
              )
          )
        LIMIT 1;

        IF v_tenant_id IS NOT NULL THEN
            -- Ensure an active organization_membership record exists ONLY IF not already Inactive
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
            VALUES (p_user_id, p_organization_id, v_tenant_id, 'Active', NOW(), NOW())
            ON CONFLICT (user_id, organization_id) DO NOTHING;

            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    ELSE
        -- Parent context fallback without organization
        IF EXISTS (SELECT 1 FROM family_relationships WHERE user_id = p_user_id AND status = 'Verified' AND revoked_at IS NULL) THEN
            SELECT u.tenant_id INTO v_tenant_id FROM users u WHERE u.id = p_user_id;
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    END IF;
END;
$function$;

-- 7. Update fn_sync_external_login to enforce strict multi-org isolation & deactivation
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
        -- Only allow parent access if parent has at least one active, enrolled child
        SELECT fn_has_active_onboarded_child(v_user_id) INTO v_has_access;

        IF NOT v_has_access THEN
            RETURN QUERY SELECT 'parent_access_denied'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
            RETURN;
        END IF;
    ELSIF p_required_portal = 'Daycare' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            JOIN organizations o ON uor.organization_id = o.id
            WHERE uor.user_id = v_user_id
              AND o.is_active = TRUE
              AND NOT EXISTS (
                  SELECT 1 FROM organization_memberships om_check
                  WHERE om_check.user_id = v_user_id AND om_check.organization_id = o.id AND om_check.status = 'Inactive'
              )
              AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher')
        )
        OR EXISTS (
            SELECT 1 FROM user_branch_assignments uba
            JOIN branches b ON uba.branch_id = b.id
            JOIN organizations o ON b.organization_id = o.id
            WHERE uba.user_id = v_user_id AND uba.revoked_at IS NULL
              AND o.is_active = TRUE
              AND NOT EXISTS (
                  SELECT 1 FROM organization_memberships om_check
                  WHERE om_check.user_id = v_user_id AND om_check.organization_id = o.id AND om_check.status = 'Inactive'
              )
        )
        OR EXISTS (
            SELECT 1 FROM organization_memberships om
            JOIN organizations o ON om.organization_id = o.id
            WHERE om.user_id = v_user_id AND om.status = 'Active' AND o.is_active = TRUE
        )
        OR EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id
              AND r.name = 'PlatformAdmin'
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

    -- 8. Ensure default organization membership ONLY for completely brand new users without any membership history
    IF NOT EXISTS (SELECT 1 FROM organization_memberships om WHERE om.user_id = v_user_id) THEN
        SELECT id INTO v_default_org_id FROM organizations WHERE tenant_id = v_tenant_id AND is_active = TRUE LIMIT 1;
        IF v_default_org_id IS NOT NULL THEN
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
            VALUES (v_user_id, v_default_org_id, v_tenant_id, 'Active', NOW(), NOW())
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 9. Resolve active organization ID strictly using accessible active organizations
    SELECT COUNT(*) INTO v_active_org_count
    FROM fn_get_organizations_by_user_id(v_user_id);

    IF p_required_portal = 'Daycare' AND v_active_org_count = 0 THEN
        -- User has no active organizations left (they were deactivated from their orgs)
        RETURN QUERY SELECT 'no_active_organizations'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
        RETURN;
    ELSIF v_active_org_count = 1 THEN
        -- Exactly 1 active organization: auto-select it
        SELECT id INTO v_active_org_id
        FROM fn_get_organizations_by_user_id(v_user_id)
        LIMIT 1;
    ELSE
        -- Multiple active organizations: route to /select-organization
        v_active_org_id := NULL;
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
