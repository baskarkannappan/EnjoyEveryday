-- Migration: 060_user_and_identity_procedures.sql
-- Description: Stored procedures and functions for User management, Access Scopes, and Tenant Settings.

-- 1. Get user by ID
CREATE OR REPLACE FUNCTION public.fn_get_user_by_id(
    p_tenant_id UUID,
    p_user_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    email VARCHAR,
    first_name VARCHAR,
    last_name VARCHAR,
    display_name VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.id = p_user_id AND u.tenant_id = p_tenant_id;
END;
$$;

-- 2. Get all users in tenant
CREATE OR REPLACE FUNCTION public.fn_get_all_users(
    p_tenant_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    email VARCHAR,
    first_name VARCHAR,
    last_name VARCHAR,
    display_name VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.tenant_id = p_tenant_id
    ORDER BY u.first_name, u.last_name;
END;
$$;

-- 3. Get users by role
CREATE OR REPLACE FUNCTION public.fn_get_users_by_role(
    p_tenant_id UUID,
    p_role_name VARCHAR,
    p_names VARCHAR[]
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    email VARCHAR,
    first_name VARCHAR,
    last_name VARCHAR,
    display_name VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.tenant_id = p_tenant_id
      AND (EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON ur.role_id = r.id
                   WHERE ur.user_id = u.id AND r.name = p_role_name)
           OR EXISTS (SELECT 1 FROM user_organization_roles uor JOIN roles r ON r.id = uor.role_id
                      WHERE uor.user_id = u.id AND uor.tenant_id = p_tenant_id AND r.name = ANY(p_names)))
    ORDER BY u.first_name, u.last_name;
END;
$$;

-- 4. Create user
CREATE OR REPLACE FUNCTION public.fn_create_user(
    p_id UUID,
    p_tenant_id UUID,
    p_email VARCHAR,
    p_normalized_email VARCHAR,
    p_password_hash VARCHAR,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_display_name VARCHAR,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    email VARCHAR,
    first_name VARCHAR,
    last_name VARCHAR,
    display_name VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO users (id, tenant_id, email, normalized_email, password_hash, first_name, last_name, display_name, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_email, p_normalized_email, p_password_hash, p_first_name, p_last_name, p_display_name, p_is_active, p_created_at, p_updated_at)
    RETURNING users.id, users.tenant_id, users.email, users.first_name, users.last_name, users.display_name, users.is_active, users.created_at, users.updated_at;
END;
$$;

-- 5. Update user
CREATE OR REPLACE PROCEDURE public.sp_update_user(
    p_id UUID,
    p_tenant_id UUID,
    p_email VARCHAR,
    p_normalized_email VARCHAR,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_display_name VARCHAR,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE users
    SET first_name = p_first_name,
        last_name = p_last_name,
        display_name = p_display_name,
        email = p_email,
        normalized_email = p_normalized_email,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- 6. Delete user
CREATE OR REPLACE PROCEDURE public.sp_delete_user(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM users WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- 7. Assign platform role
CREATE OR REPLACE PROCEDURE public.sp_assign_platform_role(
    p_user_id UUID,
    p_role_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO user_roles (user_id, role_id)
    SELECT p_user_id, id
    FROM roles
    WHERE name = p_role_name AND tenant_id IS NULL AND scope_type = 'Platform'
    ON CONFLICT DO NOTHING;
END;
$$;

-- 8. Assign organization role
CREATE OR REPLACE PROCEDURE public.sp_assign_organization_role(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_user_id UUID,
    p_role_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id)
    SELECT u.id, o.id, o.tenant_id
    FROM users u JOIN organizations o ON o.tenant_id = u.tenant_id
    WHERE u.id = p_user_id AND u.tenant_id = p_tenant_id AND o.id = p_organization_id
    ON CONFLICT DO NOTHING;

    INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
    SELECT p_user_id, r.id, p_organization_id, p_tenant_id
    FROM roles r
    WHERE r.tenant_id = p_tenant_id AND r.name = p_role_name AND r.scope_type = 'Organization'
      AND EXISTS (SELECT 1 FROM organization_memberships m
                  WHERE m.user_id = p_user_id AND m.organization_id = p_organization_id AND m.tenant_id = p_tenant_id)
    ON CONFLICT DO NOTHING;
END;
$$;

-- 9. Get users hierarchy with roles, classroom, and children count
DROP FUNCTION IF EXISTS public.fn_get_users_hierarchy(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_users_hierarchy(
    p_tenant_id UUID
)
RETURNS TABLE (
    user_id UUID,
    email VARCHAR,
    display_name VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    organization_name VARCHAR,
    classroom_name VARCHAR,
    associated_children_count INT,
    assigned_roles VARCHAR[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH user_role_agg AS (
        SELECT comb.user_id, array_agg(DISTINCT r.name::VARCHAR) as roles, MAX(o.name) as org_name
        FROM (
            SELECT ur.user_id, ur.role_id, NULL::UUID as org_id FROM user_roles ur
            UNION ALL
            SELECT uor.user_id, uor.role_id, uor.organization_id as org_id FROM user_organization_roles uor
        ) comb
        JOIN roles r ON comb.role_id = r.id
        LEFT JOIN organizations o ON o.id = comb.org_id
        GROUP BY comb.user_id
    ),
    classroom_agg AS (
        SELECT ct.user_id, MAX(c.name) as c_name
        FROM classroom_teachers ct
        JOIN classrooms c ON c.id = ct.classroom_id
        GROUP BY ct.user_id
    ),
    parent_agg AS (
        SELECT fr.user_id, COUNT(fr.child_id)::int as child_count
        FROM family_relationships fr
        GROUP BY fr.user_id
    )
    SELECT
        u.id as user_id,
        u.email::VARCHAR,
        COALESCE(NULLIF(TRIM(u.display_name), ''), NULLIF(TRIM(u.first_name || ' ' || u.last_name), ''), u.email)::VARCHAR as display_name,
        u.is_active,
        u.created_at,
        ura.org_name::VARCHAR as organization_name,
        cla.c_name::VARCHAR as classroom_name,
        COALESCE(pa.child_count, 0) as associated_children_count,
        COALESCE(ura.roles, ARRAY[]::VARCHAR[]) as assigned_roles
    FROM users u
    LEFT JOIN user_role_agg ura ON ura.user_id = u.id
    LEFT JOIN classroom_agg cla ON cla.user_id = u.id
    LEFT JOIN parent_agg pa ON pa.user_id = u.id
    WHERE p_tenant_id = '00000000-0000-0000-0000-000000000000'::uuid OR u.tenant_id = p_tenant_id
    ORDER BY u.first_name, u.last_name;
END;
$$;

-- 10. Get user profile security details
DROP FUNCTION IF EXISTS public.fn_get_user_profile_security_details(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_user_profile_security_details(
    p_user_id UUID
)
RETURNS TABLE (
    user_id UUID,
    display_name VARCHAR,
    first_name VARCHAR,
    last_name VARCHAR,
    assigned_role_names VARCHAR[],
    is_classroom_teacher BOOLEAN,
    is_parent BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        u.id as user_id,
        u.display_name::VARCHAR,
        u.first_name::VARCHAR,
        u.last_name::VARCHAR,
        COALESCE((
            SELECT array_agg(DISTINCT r.name::VARCHAR)
            FROM (
                SELECT ur.role_id FROM user_roles ur JOIN roles r ON ur.role_id = r.id WHERE ur.user_id = p_user_id AND r.scope_type = 'Platform' AND r.tenant_id IS NULL
                UNION
                SELECT uor.role_id FROM user_organization_roles uor JOIN roles r ON uor.role_id = r.id WHERE uor.user_id = p_user_id
            ) comb
            JOIN roles r ON r.id = comb.role_id
            WHERE r.name IS NOT NULL
        ), ARRAY[]::VARCHAR[]) as assigned_role_names,
        EXISTS(SELECT 1 FROM classroom_teachers ct WHERE ct.user_id = p_user_id) as is_classroom_teacher,
        EXISTS(SELECT 1 FROM family_relationships fr WHERE fr.user_id = p_user_id) as is_parent
    FROM users u
    WHERE u.id = p_user_id;
END;
$$;

-- 11. AccessScopeRepository functions
CREATE OR REPLACE FUNCTION public.fn_has_active_membership(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_user_id UUID
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 FROM organization_memberships m
        JOIN users u ON u.id = m.user_id
        WHERE m.tenant_id = p_tenant_id AND m.organization_id = p_organization_id AND m.user_id = p_user_id
          AND m.status = 'Active' AND u.is_active = true
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_organization_role_names(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_user_id UUID
)
RETURNS TABLE (role_name VARCHAR)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT r.name::VARCHAR
    FROM user_organization_roles uor
    JOIN roles r ON r.id = uor.role_id AND r.tenant_id = uor.tenant_id
    JOIN organization_memberships m ON m.user_id = uor.user_id AND m.organization_id = uor.organization_id AND m.tenant_id = uor.tenant_id
    WHERE uor.tenant_id = p_tenant_id AND uor.organization_id = p_organization_id AND uor.user_id = p_user_id
      AND m.status = 'Active';
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_assigned_branch_ids(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_user_id UUID
)
RETURNS TABLE (branch_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT uba.branch_id
    FROM user_branch_assignments uba
    WHERE uba.tenant_id = p_tenant_id AND uba.organization_id = p_organization_id AND uba.user_id = p_user_id AND uba.revoked_at IS NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_assigned_classroom_ids(
    p_tenant_id UUID,
    p_user_id UUID
)
RETURNS TABLE (classroom_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ct.classroom_id
    FROM classroom_teachers ct
    WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_ids_of_assigned_classrooms(
    p_tenant_id UUID,
    p_user_id UUID
)
RETURNS TABLE (branch_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT cl.branch_id
    FROM classroom_teachers ct
    JOIN classrooms cl ON cl.id = ct.classroom_id AND cl.tenant_id = ct.tenant_id
    WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_scope(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b.tenant_id, b.organization_id, b.id AS branch_id, NULL::uuid AS classroom_id
    FROM branches b WHERE b.id = p_branch_id AND b.tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_classroom_scope(
    p_tenant_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cl.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id
    FROM classrooms cl JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
    WHERE cl.id = p_classroom_id AND cl.tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_scope(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ch.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id
    FROM children ch
    LEFT JOIN classrooms cl ON cl.id = ch.classroom_id AND cl.tenant_id = ch.tenant_id
    LEFT JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
    WHERE ch.id = p_child_id AND ch.tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_has_verified_guardian_relationship(
    p_tenant_id UUID,
    p_user_id UUID,
    p_child_id UUID
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 FROM family_relationships fr
        JOIN children c ON c.id = fr.child_id AND c.tenant_id = fr.tenant_id
        JOIN users u ON u.id = fr.user_id AND u.tenant_id = fr.tenant_id
        WHERE fr.tenant_id = p_tenant_id AND fr.user_id = p_user_id AND fr.child_id = p_child_id
          AND fr.status = 'Verified' AND fr.revoked_at IS NULL
          AND c.is_active = true AND u.is_active = true
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_support_grant(
    p_tenant_id UUID,
    p_user_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    granted_to_user_id UUID,
    purpose VARCHAR,
    expires_at TIMESTAMPTZ,
    read_only BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT sag.id, sag.tenant_id, sag.granted_to_user_id, sag.purpose,
           sag.expires_at, sag.read_only
    FROM support_access_grants sag
    WHERE sag.tenant_id = p_tenant_id AND sag.granted_to_user_id = p_user_id
      AND sag.revoked_at IS NULL AND sag.expires_at > NOW()
    ORDER BY sag.expires_at DESC LIMIT 1;
END;
$$;

-- 12. TenantSettingsRepository functions
CREATE OR REPLACE FUNCTION public.fn_get_tenant_encrypted_ai_api_key(
    p_tenant_id UUID
)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
DECLARE
    v_key VARCHAR;
BEGIN
    SELECT encrypted_ai_api_key INTO v_key FROM tenants WHERE id = p_tenant_id;
    RETURN v_key;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_set_tenant_encrypted_ai_api_key(
    p_tenant_id UUID,
    p_encrypted_key VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE tenants SET encrypted_ai_api_key = p_encrypted_key WHERE id = p_tenant_id;
END;
$$;
