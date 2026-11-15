-- ============================================================================
-- Migration 121: Organization Currency Configuration & Multi-Tenant Propagation
-- Allows organizations to configure a specific operating currency (Default: INR).
-- Cascades currency down through Organization Hierarchy, Daycare Web, and Parent Portal.
-- ============================================================================

-- 1. Add currency column to organizations table (Default: INR)
ALTER TABLE organizations 
ADD COLUMN IF NOT EXISTS currency VARCHAR(10) NOT NULL DEFAULT 'INR';

-- Ensure all existing organizations default to INR if unset
UPDATE organizations 
SET currency = 'INR' 
WHERE currency IS NULL OR TRIM(currency) = '';

-- Ensure all tenant preferences have INR as default if unset
UPDATE tenant_preferences
SET currency = 'INR'
WHERE currency IS NULL OR TRIM(currency) = '';

-- 2. Update procedure sp_assign_organization_regional_plan to accept optional currency parameter
CREATE OR REPLACE PROCEDURE sp_assign_organization_regional_plan(
    p_org_id UUID,
    p_region_id UUID,
    p_plan_id UUID,
    p_tariff_id UUID,
    p_currency VARCHAR(10) DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_region_code VARCHAR(50);
    v_region_name VARCHAR(150);
    v_region_currency VARCHAR(10);
    v_plan_code VARCHAR(50);
    v_plan_name VARCHAR(150);
    v_tariff_currency VARCHAR(10);
    v_resolved_currency VARCHAR(10);
    v_max_branches INT;
    v_max_classrooms INT;
    v_max_children INT;
    v_includes_ai BOOLEAN;
    v_tenant_id UUID;
BEGIN
    -- Resolve Region & Region Currency
    SELECT code, name, currency INTO v_region_code, v_region_name, v_region_currency
    FROM pricing_regions
    WHERE id = p_region_id;

    -- Resolve Tariff Currency if tariff is specified
    IF p_tariff_id IS NOT NULL THEN
        SELECT currency INTO v_tariff_currency
        FROM region_plan_tariffs
        WHERE id = p_tariff_id;
    END IF;

    -- Determine Effective Currency: Explicit parameter > Tariff Currency > Region Currency > Existing Org Currency > 'INR'
    SELECT currency, tenant_id INTO v_resolved_currency, v_tenant_id
    FROM organizations
    WHERE id = p_org_id;

    v_resolved_currency := COALESCE(
        NULLIF(TRIM(p_currency), ''),
        NULLIF(TRIM(v_tariff_currency), ''),
        NULLIF(TRIM(v_region_currency), ''),
        NULLIF(TRIM(v_resolved_currency), ''),
        'INR'
    );

    -- Resolve Plan limits (fallback standard defaults if not already present)
    IF p_plan_id = '11111111-1111-1111-1111-111111111111'::UUID THEN
        v_plan_code := 'PLAN-SPROUT';
        v_plan_name := 'Sprout (Starter)';
        v_max_branches := 1;
        v_max_classrooms := 3;
        v_max_children := 35;
        v_includes_ai := FALSE;
    ELSIF p_plan_id = '33333333-3333-3333-3333-333333333333'::UUID THEN
        v_plan_code := 'PLAN-CANOPY';
        v_plan_name := 'Canopy (Multi-Branch)';
        v_max_branches := 6;
        v_max_classrooms := 24;
        v_max_children := 300;
        v_includes_ai := TRUE;
    ELSE
        v_plan_code := 'PLAN-BLOOM';
        v_plan_name := 'Bloom (Growth)';
        v_max_branches := 2;
        v_max_classrooms := 8;
        v_max_children := 90;
        v_includes_ai := TRUE;
    END IF;

    -- Update Organization with Region, Plan Quotas, Tariff, and Currency
    UPDATE organizations
    SET
        pricing_region_id = p_region_id,
        pricing_region_code = v_region_code,
        pricing_region_name = v_region_name,
        plan_id = p_plan_id,
        plan_code = v_plan_code,
        plan_name = v_plan_name,
        max_branches = v_max_branches,
        max_classrooms = v_max_classrooms,
        max_children = v_max_children,
        includes_ai = v_includes_ai,
        tariff_id = p_tariff_id,
        currency = v_resolved_currency,
        updated_at = NOW()
    WHERE id = p_org_id;

    -- Propagate Currency to tenant_preferences
    IF v_tenant_id IS NOT NULL THEN
        UPDATE tenant_preferences
        SET currency = v_resolved_currency, updated_at = NOW()
        WHERE tenant_id = v_tenant_id;
    END IF;

END;
$$;

-- 3. Update fn_get_platform_organizations_overview to include currency
DROP FUNCTION IF EXISTS fn_get_platform_organizations_overview();

CREATE OR REPLACE FUNCTION fn_get_platform_organizations_overview()
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    account_status VARCHAR,
    suspension_reason TEXT,
    created_at TIMESTAMPTZ,
    branch_count INT,
    admin_email VARCHAR,
    plan_id UUID,
    plan_code VARCHAR,
    plan_name VARCHAR,
    max_branches INT,
    max_classrooms INT,
    max_children INT,
    includes_ai BOOLEAN,
    pricing_region_id UUID,
    pricing_region_code VARCHAR,
    pricing_region_name VARCHAR,
    tariff_id UUID,
    currency VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar AS account_status,
           o.suspension_reason,
           o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1)::varchar AS admin_email,
           o.plan_id,
           COALESCE(o.plan_code, 'PLAN-BLOOM')::varchar AS plan_code,
           COALESCE(o.plan_name, 'Bloom (Growth)')::varchar AS plan_name,
           COALESCE(o.max_branches, 2)::int AS max_branches,
           COALESCE(o.max_classrooms, 8)::int AS max_classrooms,
           COALESCE(o.max_children, 90)::int AS max_children,
           COALESCE(o.includes_ai, TRUE)::boolean AS includes_ai,
           o.pricing_region_id,
           o.pricing_region_code::varchar,
           o.pricing_region_name::varchar,
           o.tariff_id,
           COALESCE(o.currency, 'INR')::varchar AS currency
    FROM organizations o
    ORDER BY o.name;
END;
$$;

-- 4. Update fn_get_organization_by_id and fn_get_all_organizations
DROP FUNCTION IF EXISTS fn_get_organization_by_id(UUID, UUID);

CREATE OR REPLACE FUNCTION fn_get_organization_by_id(p_tenant_id UUID, p_id UUID)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    account_status VARCHAR,
    suspension_reason TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    currency VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar,
           o.suspension_reason, o.created_at, o.updated_at,
           COALESCE(o.currency, 'INR')::varchar AS currency
    FROM organizations o
    WHERE o.tenant_id = p_tenant_id AND o.id = p_id;
END;
$$;

DROP FUNCTION IF EXISTS fn_get_all_organizations(UUID);

CREATE OR REPLACE FUNCTION fn_get_all_organizations(p_tenant_id UUID)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    currency VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at,
           COALESCE(o.currency, 'INR')::varchar AS currency
    FROM organizations o
    WHERE o.tenant_id = p_tenant_id
    ORDER BY o.name;
END;
$$;

-- 5. Update fn_get_parent_authorized_children to include organization currency
DROP FUNCTION IF EXISTS fn_get_parent_authorized_children(UUID, UUID);

CREATE OR REPLACE FUNCTION fn_get_parent_authorized_children(p_tenant_id UUID, p_user_id UUID)
RETURNS TABLE (
    child_id UUID,
    tenant_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    classroom_id UUID,
    classroom_name VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    organization_id UUID,
    organization_name VARCHAR,
    photo_url TEXT,
    relationship VARCHAR,
    is_primary BOOLEAN,
    currency VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.id AS child_id,
        c.tenant_id,
        c.first_name,
        c.last_name,
        c.date_of_birth,
        c.classroom_id,
        cl.name::VARCHAR AS classroom_name,
        cl.branch_id,
        b.name::VARCHAR AS branch_name,
        COALESCE(b.organization_id, o.id, (SELECT o2.id FROM organizations o2 WHERE o2.tenant_id = c.tenant_id ORDER BY o2.created_at LIMIT 1)) AS organization_id,
        o.name::VARCHAR AS organization_name,
        c.photo_url,
        fr.relationship::VARCHAR,
        fr.is_primary,
        COALESCE(o.currency, 'INR')::VARCHAR AS currency
    FROM family_relationships fr
    JOIN children c ON fr.child_id = c.id
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    LEFT JOIN organizations o ON b.organization_id = o.id
    WHERE fr.user_id = p_user_id
      AND fr.status = 'Verified' AND fr.revoked_at IS NULL
      AND c.is_active = true
    ORDER BY c.first_name, c.last_name;
END;
$$;

-- 6. Update fn_get_organization_hierarchy(p_tenant_id, p_org_id, p_requesting_user_id, p_is_org_admin) to include Currency
CREATE OR REPLACE FUNCTION fn_get_organization_hierarchy(
    p_tenant_id uuid,
    p_org_id uuid,
    p_requesting_user_id uuid,
    p_is_org_admin boolean
)
RETURNS jsonb
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_org record;
    v_org_admins jsonb;
    v_branches jsonb;
    v_total_staff int := 0;
    v_total_students int := 0;
BEGIN
    -- 1. Fetch organization details
    SELECT id, name, location, COALESCE(currency, 'INR') AS currency INTO v_org
    FROM organizations
    WHERE id = p_org_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('OrganizationId', p_org_id, 'Currency', 'INR');
    END IF;

    -- 2. Fetch Organization Admins
    SELECT COALESCE(jsonb_agg(
        jsonb_build_object(
            'UserId', u.id,
            'Name', COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email),
            'Email', u.email,
            'RoleName', 'Organization Admin'
        ) ORDER BY COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email)
    ), '[]'::jsonb)
    INTO v_org_admins
    FROM users u
    JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = p_org_id
    JOIN roles r ON uor.role_id = r.id
    WHERE r.name IN ('OrganizationAdmin', 'OrganizationOwner', 'SuperAdmin');

    -- 3. Fetch Authorized Branches and their hierarchy
    WITH authorized_branches AS (
        SELECT b.id, b.name, b.location
        FROM branches b
        WHERE b.organization_id = p_org_id
          AND b.is_active = true
          AND (
              p_is_org_admin = true
              OR EXISTS (
                  SELECT 1 FROM user_branch_assignments uba
                  WHERE uba.branch_id = b.id
                    AND uba.user_id = p_requesting_user_id
                    AND uba.revoked_at IS NULL
              )
          )
        ORDER BY b.name
    ),
    branch_admins_cte AS (
        SELECT DISTINCT ON (uba.branch_id, u.id)
            uba.branch_id,
            u.id as user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
            u.email
        FROM user_branch_assignments uba
        JOIN users u ON uba.user_id = u.id
        JOIN user_organization_roles uor ON uor.user_id = uba.user_id AND uor.organization_id = uba.organization_id
        JOIN roles r ON uor.role_id = r.id AND r.name = 'BranchAdmin'
        WHERE uba.branch_id IN (SELECT id FROM authorized_branches)
          AND uba.revoked_at IS NULL
        ORDER BY uba.branch_id, u.id, name
    ),
    branch_classrooms_cte AS (
        SELECT 
            c.id, c.branch_id, c.name, c.code as room_number, COALESCE(c.capacity, 0) as capacity
        FROM classrooms c
        WHERE c.branch_id IN (SELECT id FROM authorized_branches)
          AND c.is_active = true
        ORDER BY c.name
    ),
    classroom_teachers_cte AS (
        SELECT 
            ct.classroom_id,
            u.id as user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
            u.email,
            (ct.is_primary OR EXISTS (
                SELECT 1 FROM user_organization_roles uor
                JOIN roles r ON uor.role_id = r.id
                WHERE uor.user_id = u.id
                  AND (uor.organization_id = p_org_id OR uor.tenant_id = p_tenant_id)
                  AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
            )) as is_lead,
            CASE 
                WHEN (ct.is_primary OR EXISTS (
                    SELECT 1 FROM user_organization_roles uor
                    JOIN roles r ON uor.role_id = r.id
                    WHERE uor.user_id = u.id
                      AND (uor.organization_id = p_org_id OR uor.tenant_id = p_tenant_id)
                      AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
                )) THEN 'Lead Teacher' 
                ELSE 'Educator' 
            END as role_name
        FROM classroom_teachers ct
        JOIN users u ON ct.user_id = u.id
        WHERE ct.classroom_id IN (SELECT id FROM branch_classrooms_cte)
        ORDER BY is_lead DESC, name
    ),
    classroom_children_cte AS (
        SELECT 
            ch.id as child_id,
            ch.classroom_id,
            TRIM(ch.first_name || ' ' || ch.last_name) as full_name,
            ch.photo_url,
            c.name as classroom_name
        FROM children ch
        JOIN classrooms c ON ch.classroom_id = c.id
        WHERE ch.classroom_id IN (SELECT id FROM branch_classrooms_cte)
          AND ch.is_active = true
          AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
        ORDER BY ch.first_name, ch.last_name
    ),
    unassigned_branch_staff_cte AS (
        SELECT DISTINCT ON (uba.branch_id, u.id)
            uba.branch_id,
            u.id as user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
            u.email,
            CASE 
                WHEN EXISTS (
                    SELECT 1 FROM user_organization_roles uor 
                    JOIN roles r ON uor.role_id = r.id
                    WHERE uor.user_id = u.id AND (uor.organization_id = p_org_id OR uor.tenant_id = p_tenant_id)
                      AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
                ) THEN 'Lead Teacher'
                ELSE 'Educator'
            END as role_name
        FROM user_branch_assignments uba
        JOIN users u ON uba.user_id = u.id
        WHERE uba.branch_id IN (SELECT id FROM authorized_branches)
          AND uba.revoked_at IS NULL
          AND NOT EXISTS (
              SELECT 1 FROM classroom_teachers ct
              JOIN classrooms c ON ct.classroom_id = c.id
              WHERE ct.user_id = u.id AND c.branch_id = uba.branch_id
          )
          AND NOT EXISTS (
              SELECT 1 FROM branch_admins_cte ba
              WHERE ba.branch_id = uba.branch_id AND ba.user_id = u.id
          )
        ORDER BY uba.branch_id, u.id, name
    ),
    aggregated_classrooms AS (
        SELECT 
            bc.branch_id,
            bc.id as classroom_id,
            bc.name as classroom_name,
            bc.room_number,
            bc.capacity,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'UserId', ct.user_id,
                        'Name', ct.name,
                        'Email', ct.email,
                        'RoleName', ct.role_name,
                        'AssignedClassroomName', bc.name
                    ) ORDER BY ct.is_lead DESC, ct.name
                ) FROM classroom_teachers_cte ct WHERE ct.classroom_id = bc.id AND ct.is_lead = true), '[]'::jsonb
            ) as lead_teachers,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'UserId', ct.user_id,
                        'Name', ct.name,
                        'Email', ct.email,
                        'RoleName', ct.role_name,
                        'AssignedClassroomName', bc.name
                    ) ORDER BY ct.name
                ) FROM classroom_teachers_cte ct WHERE ct.classroom_id = bc.id AND ct.is_lead = false), '[]'::jsonb
            ) as teachers,
            (SELECT count(*)::int FROM classroom_children_cte cc WHERE cc.classroom_id = bc.id) as student_count,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'ChildId', cc.child_id,
                        'FullName', cc.full_name,
                        'PhotoUrl', cc.photo_url,
                        'ClassroomName', bc.name,
                        'AssignedTeacherName', (SELECT name FROM classroom_teachers_cte ct WHERE ct.classroom_id = bc.id LIMIT 1)
                    ) ORDER BY cc.full_name
                ) FROM classroom_children_cte cc WHERE cc.classroom_id = bc.id), '[]'::jsonb
            ) as students
        FROM branch_classrooms_cte bc
    ),
    aggregated_branches AS (
        SELECT 
            ab.id as branch_id,
            ab.name as branch_name,
            ab.location,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'UserId', ba.user_id,
                        'Name', ba.name,
                        'Email', ba.email,
                        'RoleName', 'Campus Branch Director'
                    ) ORDER BY ba.name
                ) FROM branch_admins_cte ba WHERE ba.branch_id = ab.id), '[]'::jsonb
            ) as branch_admins,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'ClassroomId', ac.classroom_id,
                        'ClassroomName', ac.classroom_name,
                        'RoomNumber', ac.room_number,
                        'Capacity', ac.capacity,
                        'LeadTeachers', ac.lead_teachers,
                        'Teachers', ac.teachers,
                        'StudentCount', ac.student_count,
                        'ChildrenLoaded', true,
                        'Students', ac.students
                    ) ORDER BY ac.classroom_name
                ) FROM aggregated_classrooms ac WHERE ac.branch_id = ab.id), '[]'::jsonb
            ) as classrooms,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'UserId', ubs.user_id,
                        'Name', ubs.name,
                        'Email', ubs.email,
                        'RoleName', ubs.role_name,
                        'AssignedClassroomName', 'Unassigned Classroom'
                    ) ORDER BY ubs.name
                ) FROM unassigned_branch_staff_cte ubs WHERE ubs.branch_id = ab.id), '[]'::jsonb
            ) as unassigned_staff,
            (SELECT count(*)::int FROM aggregated_classrooms ac WHERE ac.branch_id = ab.id) as total_classrooms,
            (SELECT COALESCE(sum(ac.student_count), 0)::int FROM aggregated_classrooms ac WHERE ac.branch_id = ab.id) as total_students,
            (SELECT count(DISTINCT ct.user_id)::int FROM classroom_teachers_cte ct 
             JOIN aggregated_classrooms ac ON ct.classroom_id = ac.classroom_id WHERE ac.branch_id = ab.id AND ct.is_lead = true) as lead_teacher_count,
            (SELECT count(DISTINCT ct.user_id)::int FROM classroom_teachers_cte ct 
             JOIN aggregated_classrooms ac ON ct.classroom_id = ac.classroom_id WHERE ac.branch_id = ab.id AND ct.is_lead = false) as teacher_count
        FROM authorized_branches ab
    )
    SELECT COALESCE(jsonb_agg(
        jsonb_build_object(
            'BranchId', br.branch_id,
            'BranchName', br.branch_name,
            'Location', br.location,
            'BranchAdmins', br.branch_admins,
            'Classrooms', br.classrooms,
            'UnassignedStaff', br.unassigned_staff,
            'StudentCount', br.total_students,
            'LeadTeacherCount', br.lead_teacher_count,
            'TeacherCount', br.teacher_count
        ) ORDER BY br.branch_name
    ), '[]'::jsonb)
    INTO v_branches
    FROM aggregated_branches br;

    -- 4. Calculate total unique staff and total students across entire org
    WITH all_staff AS (
        SELECT (admin->>'UserId')::uuid as staff_id FROM jsonb_array_elements(v_org_admins) admin
        UNION
        SELECT (admin->>'UserId')::uuid as staff_id FROM jsonb_array_elements(v_branches) b, jsonb_array_elements(b->'BranchAdmins') admin
        UNION
        SELECT (teacher->>'UserId')::uuid as staff_id FROM jsonb_array_elements(v_branches) b, jsonb_array_elements(b->'Classrooms') c, jsonb_array_elements(c->'LeadTeachers') teacher
        UNION
        SELECT (teacher->>'UserId')::uuid as staff_id FROM jsonb_array_elements(v_branches) b, jsonb_array_elements(b->'Classrooms') c, jsonb_array_elements(c->'Teachers') teacher
        UNION
        SELECT (staff->>'UserId')::uuid as staff_id FROM jsonb_array_elements(v_branches) b, jsonb_array_elements(b->'UnassignedStaff') staff
    )
    SELECT count(DISTINCT staff_id)::int INTO v_total_staff FROM all_staff WHERE staff_id IS NOT NULL;

    SELECT COALESCE(sum((b->>'StudentCount')::int), 0)::int INTO v_total_students FROM jsonb_array_elements(v_branches) b;

    RETURN jsonb_build_object(
        'OrganizationId', v_org.id,
        'OrganizationName', v_org.name,
        'Location', v_org.location,
        'Currency', v_org.currency,
        'TotalBranches', jsonb_array_length(v_branches),
        'TotalStaff', COALESCE(v_total_staff, 0),
        'TotalStudents', v_total_students,
        'OrganizationAdmins', v_org_admins,
        'Branches', v_branches
    );
END;
$$;
