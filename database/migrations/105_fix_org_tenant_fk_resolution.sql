-- Migration 105: Resolve canonical tenant_id from organization/classroom to prevent foreign key violations in multi-org scenarios
-- Fixes foreign key constraint user_branch_assignments_organization_id_tenant_id_fkey

CREATE OR REPLACE PROCEDURE public.sp_assign_user_branch(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_branch_id uuid,
    IN p_user_id uuid,
    IN p_assigned_by uuid DEFAULT NULL::uuid
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_canonical_tenant_id uuid;
BEGIN
    -- Resolve canonical tenant_id from the organization to guarantee FK integrity
    SELECT tenant_id INTO v_canonical_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    INSERT INTO user_branch_assignments (user_id, branch_id, organization_id, tenant_id, assigned_by, created_at, revoked_at)
    VALUES (p_user_id, p_branch_id, p_organization_id, v_canonical_tenant_id, p_assigned_by, NOW(), NULL)
    ON CONFLICT (user_id, branch_id) DO UPDATE
    SET revoked_at = NULL,
        tenant_id = v_canonical_tenant_id,
        organization_id = p_organization_id,
        assigned_by = COALESCE(p_assigned_by, user_branch_assignments.assigned_by);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_assign_organization_role(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_user_id uuid,
    IN p_role_name character varying
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_role_id uuid;
    v_template_role_id uuid;
    v_clean_name character varying;
    v_canonical_tenant_id uuid;
BEGIN
    -- Resolve canonical tenant_id from the organization
    SELECT tenant_id INTO v_canonical_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    -- 1. Ensure organization membership exists for this user and organization
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
    VALUES (p_user_id, p_organization_id, v_canonical_tenant_id, 'Active', NOW(), NOW())
    ON CONFLICT (user_id, organization_id) DO UPDATE
    SET status = 'Active', tenant_id = v_canonical_tenant_id, updated_at = NOW();

    -- Normalize role name
    v_clean_name := REPLACE(p_role_name, ' ', '');

    -- 2. Find matching role for this tenant
    SELECT id INTO v_role_id
    FROM roles
    WHERE tenant_id = v_canonical_tenant_id AND (name = p_role_name OR name = v_clean_name)
    LIMIT 1;

    -- If not found under this tenant, see if there's a template/global role or any role with this name, and copy it to this tenant
    IF v_role_id IS NULL THEN
        SELECT id INTO v_template_role_id
        FROM roles
        WHERE name = p_role_name OR name = v_clean_name
        LIMIT 1;

        IF v_template_role_id IS NOT NULL THEN
            INSERT INTO roles (id, tenant_id, name, description, scope_type, created_at)
            SELECT gen_random_uuid(), v_canonical_tenant_id, r.name, r.description, 'Organization', NOW()
            FROM roles r
            WHERE r.id = v_template_role_id
            RETURNING id INTO v_role_id;

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT v_role_id, rp.permission_id
            FROM role_permissions rp
            WHERE rp.role_id = v_template_role_id
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 3. Assign role to user in user_organization_roles
    IF v_role_id IS NOT NULL THEN
        INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
        VALUES (p_user_id, v_role_id, p_organization_id, v_canonical_tenant_id)
        ON CONFLICT (user_id, role_id, organization_id) DO UPDATE
        SET tenant_id = v_canonical_tenant_id;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_dispatch_teacher_to_classroom(
    IN p_tenant_id uuid,
    IN p_classroom_id uuid,
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_canonical_tenant_id uuid;
BEGIN
    SELECT tenant_id INTO v_canonical_tenant_id FROM classrooms WHERE id = p_classroom_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
    VALUES (gen_random_uuid(), v_canonical_tenant_id, p_classroom_id, p_user_id, FALSE)
    ON CONFLICT (classroom_id, user_id) DO UPDATE
    SET tenant_id = v_canonical_tenant_id;
END;
$$;

