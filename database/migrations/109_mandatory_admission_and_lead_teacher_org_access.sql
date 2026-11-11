-- Migration 109: Mandatory Child Admission & Lead Teacher Organization/Branch Access
-- 1. Updates fn_get_organizations_by_user_id to discover organizations via memberships, roles, branch assignments, and classroom assignments.
-- 2. Updates fn_validate_and_set_organization to validate against all assigned scopes.
-- 3. Updates fn_get_user_assigned_branches to include branches linked via user_branch_assignments AND classroom_teachers.
-- 4. Updates sp_promote_teacher_to_lead & sp_update_teacher_classrooms to ensure active organization memberships and is_primary flag for Lead Teachers.
-- 5. Updates fn_sync_external_login to use multi-scope organization resolution.

-- 1. Comprehensive organization resolution by user ID
CREATE OR REPLACE FUNCTION public.fn_get_organizations_by_user_id(p_user_id uuid)
RETURNS TABLE(
    id uuid,
    tenant_id uuid,
    name character varying,
    location character varying,
    is_active boolean,
    created_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    RETURN QUERY
    WITH accessible_org_ids AS (
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

        -- Active classroom assignments linked to branches and organizations
        SELECT b.organization_id
        FROM classroom_teachers ct
        JOIN classrooms c ON ct.classroom_id = c.id
        JOIN branches b ON c.branch_id = b.id
        WHERE ct.user_id = p_user_id AND b.organization_id IS NOT NULL

        UNION

        -- User primary tenant organization match (if active)
        SELECT o.id as organization_id
        FROM users u
        JOIN organizations o ON o.tenant_id = u.tenant_id
        WHERE u.id = p_user_id AND o.is_active = TRUE
    )
    SELECT DISTINCT
        o.id,
        o.tenant_id,
        o.name,
        o.location,
        o.is_active,
        o.created_at,
        o.updated_at
    FROM accessible_org_ids a
    JOIN organizations o ON a.organization_id = o.id
    WHERE o.is_active = TRUE
    ORDER BY o.name;
END;
$$;

-- 2. Organization validation and tenant resolution
CREATE OR REPLACE FUNCTION public.fn_validate_and_set_organization(p_user_id uuid, p_organization_id uuid DEFAULT NULL::uuid)
RETURNS TABLE(is_valid boolean, tenant_id uuid)
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_tenant_id UUID;
BEGIN
    IF p_organization_id IS NOT NULL THEN
        -- Check if organization is accessible via memberships, roles, branch assignments or classroom assignments
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
            -- Ensure an active organization_membership record exists so future checks stay consistent
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
            VALUES (p_user_id, p_organization_id, v_tenant_id, 'Active', NOW(), NOW())
            ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active', updated_at = NOW();

            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    ELSE
        -- Parent context fallback without organization
        IF EXISTS (SELECT 1 FROM family_relationships WHERE user_id = p_user_id) THEN
            SELECT u.tenant_id INTO v_tenant_id FROM users u WHERE u.id = p_user_id;
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    END IF;
END;
$$;

-- 3. Branch resolution including classroom assignments
CREATE OR REPLACE FUNCTION public.fn_get_user_assigned_branches(p_user_id uuid, p_organization_id uuid)
RETURNS TABLE(id uuid, name character varying, location character varying, is_active boolean)
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    -- Check if user is Organization Admin or Owner
    IF EXISTS (
        SELECT 1 FROM user_organization_roles uor
        JOIN roles r ON uor.role_id = r.id
        WHERE uor.user_id = p_user_id
          AND uor.organization_id = p_organization_id
          AND r.name IN ('OrganizationAdmin', 'OrganizationOwner', 'PlatformAdmin', 'SuperAdmin')
    ) OR EXISTS (
        SELECT 1 FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE ur.user_id = p_user_id
          AND r.name IN ('PlatformAdmin', 'SuperAdmin')
    ) THEN
        -- Return all active branches for this organization
        RETURN QUERY
        SELECT b.id, b.name::VARCHAR, b.location::VARCHAR, b.is_active
        FROM branches b
        WHERE b.organization_id = p_organization_id AND b.is_active = TRUE
        ORDER BY b.name;
    ELSE
        -- Return branches assigned via user_branch_assignments OR classroom_teachers
        RETURN QUERY
        WITH user_branches AS (
            SELECT uba.branch_id
            FROM user_branch_assignments uba
            WHERE uba.user_id = p_user_id AND uba.revoked_at IS NULL

            UNION

            SELECT c.branch_id
            FROM classroom_teachers ct
            JOIN classrooms c ON ct.classroom_id = c.id
            WHERE ct.user_id = p_user_id
        )
        SELECT DISTINCT b.id, b.name, b.location, b.is_active
        FROM user_branches ub
        JOIN branches b ON ub.branch_id = b.id
        WHERE b.organization_id = p_organization_id AND b.is_active = TRUE
        ORDER BY b.name;
    END IF;
END;
$$;

-- 4. Update sp_promote_teacher_to_lead to ensure active organization membership and mark is_primary = TRUE for Lead Teachers
CREATE OR REPLACE FUNCTION public.sp_promote_teacher_to_lead(
    p_tenant_id uuid,
    p_organization_id uuid,
    p_branch_id uuid,
    p_user_id uuid,
    p_classroom_ids uuid[],
    p_promoted_by uuid
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_canonical_tenant_id uuid;
    v_lead_role_id uuid;
    v_template_role_id uuid;
    v_cid uuid;
    v_classroom_tenant_id uuid;
BEGIN
    -- 1. Resolve canonical tenant_id from organization
    SELECT tenant_id INTO v_canonical_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    -- 2. Ensure active organization membership
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
    VALUES (p_user_id, p_organization_id, v_canonical_tenant_id, 'Active', NOW(), NOW())
    ON CONFLICT (user_id, organization_id) DO UPDATE
    SET status = 'Active', updated_at = NOW();

    -- 3. Find or ensure LeadTeacher role exists for this tenant
    SELECT id INTO v_lead_role_id
    FROM roles
    WHERE tenant_id = v_canonical_tenant_id AND (name = 'LeadTeacher' OR name = 'Lead Teacher')
    LIMIT 1;

    IF v_lead_role_id IS NULL THEN
        SELECT id INTO v_template_role_id
        FROM roles
        WHERE (name = 'LeadTeacher' OR name = 'Lead Teacher')
        ORDER BY created_at ASC
        LIMIT 1;

        IF v_template_role_id IS NOT NULL THEN
            INSERT INTO roles (id, tenant_id, name, description, role_rank, is_system_role, scope_type, created_at, updated_at)
            SELECT
                gen_random_uuid(),
                v_canonical_tenant_id,
                'LeadTeacher',
                r.description,
                COALESCE(r.role_rank, 4),
                r.is_system_role,
                COALESCE(r.scope_type, 'Organization'),
                NOW(),
                NOW()
            FROM roles r
            WHERE r.id = v_template_role_id
            RETURNING id INTO v_lead_role_id;
        ELSE
            INSERT INTO roles (id, tenant_id, name, description, role_rank, is_system_role, scope_type, created_at, updated_at)
            VALUES (
                gen_random_uuid(),
                v_canonical_tenant_id,
                'LeadTeacher',
                'Lead Teacher coordinating classroom delivery and supporting educators',
                4,
                true,
                'Organization',
                NOW(),
                NOW()
            )
            RETURNING id INTO v_lead_role_id;
        END IF;
    END IF;

    -- 4. Assign Lead Teacher role in user_organization_roles
    INSERT INTO user_organization_roles (id, tenant_id, organization_id, user_id, role_id)
    VALUES (gen_random_uuid(), v_canonical_tenant_id, p_organization_id, p_user_id, v_lead_role_id)
    ON CONFLICT DO NOTHING;

    -- 5. Remove lower/generic educator roles in this organization context
    DELETE FROM user_organization_roles uor
    USING roles r
    WHERE uor.role_id = r.id
      AND uor.user_id = p_user_id
      AND uor.organization_id = p_organization_id
      AND r.name IN ('Teacher', 'DaycareTeacher', 'ClassroomTeacher', 'AssistantTeacher')
      AND uor.role_id <> v_lead_role_id;

    -- 6. Ensure active branch assignment
    INSERT INTO user_branch_assignments (user_id, branch_id, tenant_id, organization_id, assigned_by, created_at)
    VALUES (p_user_id, p_branch_id, v_canonical_tenant_id, p_organization_id, p_promoted_by, NOW())
    ON CONFLICT (user_id, branch_id) DO UPDATE
    SET revoked_at = NULL,
        tenant_id = v_canonical_tenant_id,
        organization_id = p_organization_id,
        assigned_by = COALESCE(p_promoted_by, user_branch_assignments.assigned_by);

    -- 7. Synchronize classroom assignments for this branch:
    -- Remove old classrooms in THIS branch that are not in p_classroom_ids
    DELETE FROM classroom_teachers ct
    USING classrooms c
    WHERE ct.classroom_id = c.id
      AND ct.user_id = p_user_id
      AND c.branch_id = p_branch_id
      AND (p_classroom_ids IS NULL OR NOT (c.id = ANY(p_classroom_ids)));

    -- Add/update all newly selected classrooms with is_primary = TRUE
    IF p_classroom_ids IS NOT NULL AND array_length(p_classroom_ids, 1) > 0 THEN
        FOREACH v_cid IN ARRAY p_classroom_ids
        LOOP
            SELECT tenant_id INTO v_classroom_tenant_id FROM classrooms WHERE id = v_cid;
            IF v_classroom_tenant_id IS NULL THEN
                v_classroom_tenant_id := v_canonical_tenant_id;
            END IF;

            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), v_classroom_tenant_id, v_cid, p_user_id, TRUE)
            ON CONFLICT (classroom_id, user_id) DO UPDATE
            SET tenant_id = v_classroom_tenant_id,
                is_primary = TRUE;
        END LOOP;
    END IF;
END;
$$;

-- 5. Update sp_update_teacher_classrooms to ensure active organization membership and maintain is_primary flag for Lead Teachers
CREATE OR REPLACE FUNCTION public.sp_update_teacher_classrooms(
    p_tenant_id uuid,
    p_organization_id uuid,
    p_branch_id uuid,
    p_user_id uuid,
    p_classroom_ids uuid[],
    p_updated_by uuid
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_canonical_tenant_id uuid;
    v_cid uuid;
    v_classroom_tenant_id uuid;
    v_is_lead boolean := FALSE;
BEGIN
    SELECT tenant_id INTO v_canonical_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    -- Ensure active membership
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
    VALUES (p_user_id, p_organization_id, v_canonical_tenant_id, 'Active', NOW(), NOW())
    ON CONFLICT (user_id, organization_id) DO UPDATE
    SET status = 'Active', updated_at = NOW();

    -- Check if user is a Lead Teacher
    SELECT EXISTS (
        SELECT 1 FROM user_organization_roles uor
        JOIN roles r ON uor.role_id = r.id
        WHERE uor.user_id = p_user_id
          AND uor.organization_id = p_organization_id
          AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
    ) INTO v_is_lead;

    -- Ensure active branch assignment
    INSERT INTO user_branch_assignments (user_id, branch_id, tenant_id, organization_id, assigned_by, created_at)
    VALUES (p_user_id, p_branch_id, v_canonical_tenant_id, p_organization_id, p_updated_by, NOW())
    ON CONFLICT (user_id, branch_id) DO UPDATE
    SET revoked_at = NULL,
        tenant_id = v_canonical_tenant_id,
        organization_id = p_organization_id,
        assigned_by = COALESCE(p_updated_by, user_branch_assignments.assigned_by);

    -- Remove classrooms in this branch not in p_classroom_ids
    DELETE FROM classroom_teachers ct
    USING classrooms c
    WHERE ct.classroom_id = c.id
      AND ct.user_id = p_user_id
      AND c.branch_id = p_branch_id
      AND (p_classroom_ids IS NULL OR NOT (c.id = ANY(p_classroom_ids)));

    -- Insert or update selected classrooms
    IF p_classroom_ids IS NOT NULL AND array_length(p_classroom_ids, 1) > 0 THEN
        FOREACH v_cid IN ARRAY p_classroom_ids
        LOOP
            SELECT tenant_id INTO v_classroom_tenant_id FROM classrooms WHERE id = v_cid;
            IF v_classroom_tenant_id IS NULL THEN
                v_classroom_tenant_id := v_canonical_tenant_id;
            END IF;

            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), v_classroom_tenant_id, v_cid, p_user_id, v_is_lead)
            ON CONFLICT (classroom_id, user_id) DO UPDATE
            SET tenant_id = v_classroom_tenant_id,
                is_primary = CASE WHEN v_is_lead THEN TRUE ELSE classroom_teachers.is_primary END;
        END LOOP;
    END IF;
END;
$$;

-- 6. Synchronize existing Lead Teachers' is_primary flag in classroom_teachers
UPDATE classroom_teachers ct
SET is_primary = TRUE
FROM classrooms c, branches b, user_organization_roles uor, roles r
WHERE ct.classroom_id = c.id
  AND c.branch_id = b.id
  AND uor.user_id = ct.user_id
  AND uor.organization_id = b.organization_id
  AND uor.role_id = r.id
  AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
  AND ct.is_primary = FALSE;
