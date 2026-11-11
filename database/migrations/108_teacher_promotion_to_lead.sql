-- Migration 108: Teacher Promotion to Lead Teacher and Classroom Assignment Management
-- Supports promoting a Teacher to Lead Teacher scoped to an Organization and Branch,
-- and managing multiple classroom assignments atomically.

CREATE OR REPLACE PROCEDURE public.sp_promote_teacher_to_lead(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_branch_id uuid,
    IN p_user_id uuid,
    IN p_promoted_by uuid,
    IN p_classroom_ids uuid[]
)
LANGUAGE plpgsql
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

    -- 2. Validate active organization membership
    IF NOT EXISTS (
        SELECT 1 FROM organization_memberships 
        WHERE user_id = p_user_id AND organization_id = p_organization_id AND status = 'Active'
    ) THEN
        RAISE EXCEPTION 'User does not have an active membership in organization %', p_organization_id;
    END IF;

    -- 3. Find or ensure LeadTeacher role exists for this tenant
    SELECT id INTO v_lead_role_id
    FROM roles
    WHERE tenant_id = v_canonical_tenant_id AND (name = 'LeadTeacher' OR name = 'Lead Teacher')
    LIMIT 1;

    IF v_lead_role_id IS NULL THEN
        SELECT id INTO v_template_role_id
        FROM roles
        WHERE name = 'LeadTeacher' OR name = 'Lead Teacher'
        LIMIT 1;

        IF v_template_role_id IS NOT NULL THEN
            INSERT INTO roles (id, tenant_id, name, description, scope_type, created_at)
            SELECT gen_random_uuid(), v_canonical_tenant_id, r.name, r.description, COALESCE(r.scope_type, 'Organization'), NOW()
            FROM roles r
            WHERE r.id = v_template_role_id
            RETURNING id INTO v_lead_role_id;

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT v_lead_role_id, rp.permission_id
            FROM role_permissions rp
            WHERE rp.role_id = v_template_role_id
            ON CONFLICT DO NOTHING;
        ELSE
            -- Fallback: create role with standard LeadTeacher permissions
            INSERT INTO roles (id, tenant_id, name, description, scope_type, created_at)
            VALUES (gen_random_uuid(), v_canonical_tenant_id, 'LeadTeacher', 'Lead Educator with classroom coordination authority', 'Organization', NOW())
            RETURNING id INTO v_lead_role_id;
        END IF;
    END IF;

    -- 4. Remove previous lower teacher roles in this organization
    DELETE FROM user_organization_roles uor
    USING roles r
    WHERE uor.role_id = r.id
      AND uor.user_id = p_user_id
      AND uor.organization_id = p_organization_id
      AND r.name IN ('Teacher', 'ClassroomTeacher', 'DaycareTeacher', 'AssistantTeacher');

    -- 5. Assign LeadTeacher role for this organization
    IF v_lead_role_id IS NOT NULL THEN
        INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
        VALUES (p_user_id, v_lead_role_id, p_organization_id, v_canonical_tenant_id)
        ON CONFLICT (user_id, role_id, organization_id) DO UPDATE
        SET tenant_id = v_canonical_tenant_id;
    END IF;

    -- 6. Ensure user branch assignment is active for this branch
    IF p_branch_id IS NOT NULL THEN
        INSERT INTO user_branch_assignments (user_id, branch_id, organization_id, tenant_id, assigned_by, created_at, revoked_at)
        VALUES (p_user_id, p_branch_id, p_organization_id, v_canonical_tenant_id, p_promoted_by, NOW(), NULL)
        ON CONFLICT (user_id, branch_id) DO UPDATE
        SET revoked_at = NULL,
            tenant_id = v_canonical_tenant_id,
            organization_id = p_organization_id,
            assigned_by = COALESCE(p_promoted_by, user_branch_assignments.assigned_by);
    END IF;

    -- 7. Synchronize classroom assignments for this branch:
    -- Remove old classrooms in THIS branch that are not in p_classroom_ids
    DELETE FROM classroom_teachers ct
    USING classrooms c
    WHERE ct.classroom_id = c.id
      AND ct.user_id = p_user_id
      AND c.branch_id = p_branch_id
      AND (p_classroom_ids IS NULL OR NOT (c.id = ANY(p_classroom_ids)));

    -- Add all newly selected classrooms
    IF p_classroom_ids IS NOT NULL AND array_length(p_classroom_ids, 1) > 0 THEN
        FOREACH v_cid IN ARRAY p_classroom_ids
        LOOP
            SELECT tenant_id INTO v_classroom_tenant_id FROM classrooms WHERE id = v_cid;
            IF v_classroom_tenant_id IS NULL THEN
                v_classroom_tenant_id := v_canonical_tenant_id;
            END IF;

            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), v_classroom_tenant_id, v_cid, p_user_id, FALSE)
            ON CONFLICT (classroom_id, user_id) DO UPDATE
            SET tenant_id = v_classroom_tenant_id;
        END LOOP;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_teacher_classrooms(
    IN p_tenant_id uuid,
    IN p_organization_id uuid,
    IN p_branch_id uuid,
    IN p_user_id uuid,
    IN p_classroom_ids uuid[]
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_canonical_tenant_id uuid;
    v_cid uuid;
    v_classroom_tenant_id uuid;
BEGIN
    -- Resolve canonical tenant_id from organization
    SELECT tenant_id INTO v_canonical_tenant_id FROM organizations WHERE id = p_organization_id;
    IF v_canonical_tenant_id IS NULL THEN
        v_canonical_tenant_id := p_tenant_id;
    END IF;

    -- Remove classrooms in THIS branch that are not in p_classroom_ids
    DELETE FROM classroom_teachers ct
    USING classrooms c
    WHERE ct.classroom_id = c.id
      AND ct.user_id = p_user_id
      AND c.branch_id = p_branch_id
      AND (p_classroom_ids IS NULL OR NOT (c.id = ANY(p_classroom_ids)));

    -- Add/ensure all selected classrooms
    IF p_classroom_ids IS NOT NULL AND array_length(p_classroom_ids, 1) > 0 THEN
        FOREACH v_cid IN ARRAY p_classroom_ids
        LOOP
            SELECT tenant_id INTO v_classroom_tenant_id FROM classrooms WHERE id = v_cid;
            IF v_classroom_tenant_id IS NULL THEN
                v_classroom_tenant_id := v_canonical_tenant_id;
            END IF;

            INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
            VALUES (gen_random_uuid(), v_classroom_tenant_id, v_cid, p_user_id, FALSE)
            ON CONFLICT (classroom_id, user_id) DO UPDATE
            SET tenant_id = v_classroom_tenant_id;
        END LOOP;
    END IF;
END;
$$;

