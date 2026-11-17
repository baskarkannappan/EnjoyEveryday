-- 122_query_performance_optimizations.sql
-- Optimize fn_get_organization_hierarchy (top #1 slowest query in pg_stat_statements)
-- Eliminates repeated CTE re-evaluation, duplicate correlated role subqueries,
-- per-student teacher lookups, and 5x JSONB re-parsing in Step 4.

CREATE OR REPLACE FUNCTION public.fn_get_organization_hierarchy(
    p_tenant_id uuid,
    p_org_id uuid,
    p_requesting_user_id uuid,
    p_is_org_admin boolean
)
RETURNS jsonb
LANGUAGE plpgsql
STABLE
AS $function$
DECLARE
    v_org record;
    v_org_admins jsonb;
    v_org_admin_ids uuid[];
    v_branches jsonb;
    v_total_staff int := 0;
    v_total_students int := 0;
BEGIN
    -- 1. Fetch organization details
    SELECT id, name, location, COALESCE(currency, 'INR') AS currency
    INTO v_org
    FROM organizations
    WHERE id = p_org_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('OrganizationId', p_org_id, 'Currency', 'INR');
    END IF;

    -- 2. Fetch Organization Admins (and capture their IDs once for staff count)
    SELECT
        COALESCE(jsonb_agg(
            jsonb_build_object(
                'UserId', oa.id,
                'Name', oa.display_name,
                'Email', oa.email,
                'RoleName', 'Organization Admin'
            ) ORDER BY oa.display_name
        ), '[]'::jsonb),
        COALESCE(array_agg(oa.id), ARRAY[]::uuid[])
    INTO v_org_admins, v_org_admin_ids
    FROM (
        SELECT DISTINCT ON (u.id)
            u.id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) AS display_name,
            u.email
        FROM users u
        JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = p_org_id
        JOIN roles r ON uor.role_id = r.id
        WHERE r.name IN ('OrganizationAdmin', 'OrganizationOwner', 'SuperAdmin')
    ) oa;

    -- 3. Fetch Authorized Branches, their hierarchy, and totals in a single materialized pass
    WITH authorized_branches AS MATERIALIZED (
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
    lead_teacher_users AS MATERIALIZED (
        SELECT DISTINCT uor.user_id
        FROM user_organization_roles uor
        JOIN roles r ON uor.role_id = r.id
        WHERE (uor.organization_id = p_org_id OR uor.tenant_id = p_tenant_id)
          AND r.name IN ('LeadTeacher', 'Lead Teacher')
    ),
    branch_admins_cte AS MATERIALIZED (
        SELECT DISTINCT ON (uba.branch_id, u.id)
            uba.branch_id,
            u.id AS user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) AS name,
            u.email
        FROM user_branch_assignments uba
        JOIN users u ON uba.user_id = u.id
        JOIN user_organization_roles uor ON uor.user_id = uba.user_id AND uor.organization_id = uba.organization_id
        JOIN roles r ON uor.role_id = r.id AND r.name = 'BranchAdmin'
        WHERE uba.branch_id IN (SELECT id FROM authorized_branches)
          AND uba.revoked_at IS NULL
        ORDER BY uba.branch_id, u.id, name
    ),
    branch_classrooms_cte AS MATERIALIZED (
        SELECT
            c.id, c.branch_id, c.name, c.code AS room_number, COALESCE(c.capacity, 0) AS capacity
        FROM classrooms c
        WHERE c.branch_id IN (SELECT id FROM authorized_branches)
          AND c.is_active = true
        ORDER BY c.name
    ),
    classroom_teachers_cte AS MATERIALIZED (
        SELECT
            ct.classroom_id,
            u.id AS user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) AS name,
            u.email,
            (ct.is_primary OR ltu.user_id IS NOT NULL) AS is_lead,
            CASE
                WHEN (ct.is_primary OR ltu.user_id IS NOT NULL) THEN 'Lead Teacher'
                ELSE 'Educator'
            END AS role_name
        FROM classroom_teachers ct
        JOIN users u ON ct.user_id = u.id
        LEFT JOIN lead_teacher_users ltu ON ltu.user_id = u.id
        WHERE ct.classroom_id IN (SELECT id FROM branch_classrooms_cte)
        ORDER BY is_lead DESC, name
    ),
    first_classroom_teacher AS MATERIALIZED (
        SELECT DISTINCT ON (classroom_id)
            classroom_id,
            name AS assigned_teacher_name
        FROM classroom_teachers_cte
        ORDER BY classroom_id, is_lead DESC, name
    ),
    classroom_children_cte AS MATERIALIZED (
        SELECT
            ch.id AS child_id,
            ch.classroom_id,
            TRIM(ch.first_name || ' ' || ch.last_name) AS full_name,
            ch.photo_url
        FROM children ch
        WHERE ch.classroom_id IN (SELECT id FROM branch_classrooms_cte)
          AND ch.is_active = true
          AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
        ORDER BY ch.first_name, ch.last_name
    ),
    unassigned_branch_staff_cte AS MATERIALIZED (
        SELECT DISTINCT ON (uba.branch_id, u.id)
            uba.branch_id,
            u.id AS user_id,
            COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) AS name,
            u.email,
            CASE
                WHEN ltu.user_id IS NOT NULL THEN 'Lead Teacher'
                ELSE 'Educator'
            END AS role_name
        FROM user_branch_assignments uba
        JOIN users u ON uba.user_id = u.id
        LEFT JOIN lead_teacher_users ltu ON ltu.user_id = u.id
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
    aggregated_classrooms AS MATERIALIZED (
        SELECT
            bc.branch_id,
            bc.id AS classroom_id,
            bc.name AS classroom_name,
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
            ) AS lead_teachers,
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
            ) AS teachers,
            (SELECT count(*)::int FROM classroom_children_cte cc WHERE cc.classroom_id = bc.id) AS student_count,
            COALESCE(
                (SELECT jsonb_agg(
                    jsonb_build_object(
                        'ChildId', cc.child_id,
                        'FullName', cc.full_name,
                        'PhotoUrl', cc.photo_url,
                        'ClassroomName', bc.name,
                        'AssignedTeacherName', fct.assigned_teacher_name
                    ) ORDER BY cc.full_name
                ) FROM classroom_children_cte cc WHERE cc.classroom_id = bc.id), '[]'::jsonb
            ) AS students
        FROM branch_classrooms_cte bc
        LEFT JOIN first_classroom_teacher fct ON fct.classroom_id = bc.id
    ),
    aggregated_branches AS (
        SELECT
            ab.id AS branch_id,
            ab.name AS branch_name,
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
            ) AS branch_admins,
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
            ) AS classrooms,
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
            ) AS unassigned_staff,
            (SELECT COALESCE(sum(ac.student_count), 0)::int FROM aggregated_classrooms ac WHERE ac.branch_id = ab.id) AS total_students,
            (SELECT count(DISTINCT ct.user_id)::int FROM classroom_teachers_cte ct
             JOIN aggregated_classrooms ac ON ct.classroom_id = ac.classroom_id WHERE ac.branch_id = ab.id AND ct.is_lead = true) AS lead_teacher_count,
            (SELECT count(DISTINCT ct.user_id)::int FROM classroom_teachers_cte ct
             JOIN aggregated_classrooms ac ON ct.classroom_id = ac.classroom_id WHERE ac.branch_id = ab.id AND ct.is_lead = false) AS teacher_count
        FROM authorized_branches ab
    ),
    all_staff_ids AS (
        SELECT unnest(v_org_admin_ids) AS staff_id
        UNION
        SELECT user_id AS staff_id FROM branch_admins_cte
        UNION
        SELECT user_id AS staff_id FROM classroom_teachers_cte
        UNION
        SELECT user_id AS staff_id FROM unassigned_branch_staff_cte
    )
    SELECT
        COALESCE((
            SELECT jsonb_agg(
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
            )
            FROM aggregated_branches br
        ), '[]'::jsonb),
        COALESCE((SELECT sum(total_students)::int FROM aggregated_branches), 0),
        COALESCE((SELECT count(*)::int FROM all_staff_ids WHERE staff_id IS NOT NULL), 0)
    INTO v_branches, v_total_students, v_total_staff;

    RETURN jsonb_build_object(
        'OrganizationId', v_org.id,
        'OrganizationName', v_org.name,
        'Location', v_org.location,
        'Currency', v_org.currency,
        'TotalBranches', jsonb_array_length(v_branches),
        'TotalStaff', v_total_staff,
        'TotalStudents', v_total_students,
        'OrganizationAdmins', v_org_admins,
        'Branches', v_branches
    );
END;
$function$;

