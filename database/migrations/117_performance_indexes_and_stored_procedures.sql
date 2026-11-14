-- Migration 117: Performance Indexes and Stored Procedures / Functions
-- Optimizes query performance, eliminates N+1 waterfalls, and replaces hardcoded SQL with compiled database routines.

-- ============================================================================
-- 1. HIGH-IMPACT FOREIGN KEY & COMPOSITE FILTER INDEXES
-- ============================================================================

-- Attendance Indexes
CREATE INDEX IF NOT EXISTS idx_child_attendance_room_date_status
    ON child_attendance (classroom_id, attendance_date, status);

CREATE INDEX IF NOT EXISTS idx_child_attendance_child_date
    ON child_attendance (child_id, attendance_date);

CREATE INDEX IF NOT EXISTS idx_child_attendance_recorded_by
    ON child_attendance (recorded_by_user_id) WHERE recorded_by_user_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_child_attendance_dep_verified
    ON child_attendance (departure_verified_by_user_id) WHERE departure_verified_by_user_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_teacher_attendance_branch_date_status
    ON teacher_attendance (tenant_id, branch_id, attendance_date, status);

CREATE INDEX IF NOT EXISTS idx_teacher_attendance_teacher_date
    ON teacher_attendance (teacher_id, attendance_date);

CREATE INDEX IF NOT EXISTS idx_teacher_attendance_branch_date
    ON teacher_attendance (branch_id, attendance_date);

-- Daily Care & Routine Indexes
CREATE INDEX IF NOT EXISTS idx_child_care_events_child_type_time
    ON child_care_events (child_id, event_type, event_time DESC);

CREATE INDEX IF NOT EXISTS idx_child_care_events_room_time
    ON child_care_events (classroom_id, event_time DESC) WHERE classroom_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_child_care_events_org_branch_time
    ON child_care_events (organization_id, branch_id, event_time DESC);

CREATE INDEX IF NOT EXISTS idx_child_nap_sessions_child_time
    ON child_nap_sessions (child_id, start_time DESC);

CREATE INDEX IF NOT EXISTS idx_child_nap_sessions_room_time
    ON child_nap_sessions (classroom_id, start_time DESC) WHERE classroom_id IS NOT NULL;

-- Children & Classrooms Core Indexes
CREATE INDEX IF NOT EXISTS idx_children_room_active
    ON children (classroom_id, is_active) WHERE classroom_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_children_tenant_active
    ON children (tenant_id, is_active) INCLUDE (id, classroom_id, first_name, last_name);

CREATE INDEX IF NOT EXISTS idx_classrooms_branch_active
    ON classrooms (branch_id, is_active);

CREATE INDEX IF NOT EXISTS idx_classrooms_tenant_active
    ON classrooms (tenant_id, is_active);

-- Staff Assignments & Roles Indexes
CREATE INDEX IF NOT EXISTS idx_classroom_teachers_user_room
    ON classroom_teachers (user_id, classroom_id);

CREATE INDEX IF NOT EXISTS idx_classroom_teachers_room_primary
    ON classroom_teachers (classroom_id, is_primary);

CREATE INDEX IF NOT EXISTS idx_user_branch_assignments_user_branch
    ON user_branch_assignments (user_id, branch_id) WHERE revoked_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_user_org_roles_user_org_role
    ON user_organization_roles (user_id, organization_id, role_id);

CREATE INDEX IF NOT EXISTS idx_roles_name
    ON roles (name);

-- Family & Safety / Pickups Indexes
CREATE INDEX IF NOT EXISTS idx_family_relationships_user_child
    ON family_relationships (user_id, child_id);

CREATE INDEX IF NOT EXISTS idx_authorized_pickups_child_active
    ON authorized_pickups (child_id, is_active);

CREATE INDEX IF NOT EXISTS idx_authorized_pickups_person_active
    ON authorized_pickups (person_id, is_active);

CREATE INDEX IF NOT EXISTS idx_emergency_contacts_child
    ON emergency_contacts (child_id);

CREATE INDEX IF NOT EXISTS idx_child_consents_child_type_granted
    ON child_consents (child_id, consent_type, granted);

CREATE INDEX IF NOT EXISTS idx_daycare_announcements_feed
    ON daycare_announcements (tenant_id, is_active, is_urgent DESC, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_experience_schedules_room_date
    ON experience_schedules (classroom_id, scheduled_date);

CREATE INDEX IF NOT EXISTS idx_child_stories_child_sched
    ON child_stories (child_id, experience_schedule_id);

CREATE INDEX IF NOT EXISTS idx_child_journey_entries_child_date
    ON child_journey_entries (child_id, observation_date DESC);

CREATE INDEX IF NOT EXISTS idx_child_milestones_child_status
    ON child_milestones (child_id, status);

CREATE INDEX IF NOT EXISTS idx_agent_tokens_tenant_user
    ON agent_personal_access_tokens (tenant_id, user_id, is_revoked);

CREATE INDEX IF NOT EXISTS idx_agent_webhooks_tenant_user
    ON agent_webhook_subscriptions (tenant_id, user_id, is_active);


-- ============================================================================
-- 2. STORED FUNCTIONS & PROCEDURES FOR HARDCODED REPOSITORIES
-- ============================================================================

-- A. Organization Hierarchy: Single-Roundtrip JSON Aggregation
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
    SELECT id, name, location INTO v_org
    FROM organizations
    WHERE id = p_org_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object('OrganizationId', p_org_id);
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
        'TotalBranches', jsonb_array_length(v_branches),
        'TotalStaff', COALESCE(v_total_staff, 0),
        'TotalStudents', v_total_students,
        'OrganizationAdmins', v_org_admins,
        'Branches', v_branches
    );
END;
$$;


-- B. Classroom Readiness: Consolidated Branch & Classroom Readiness Routine
CREATE OR REPLACE FUNCTION fn_get_branch_readiness(
    p_tenant_id uuid,
    p_branch_id uuid
)
RETURNS TABLE (
    branch_exists boolean,
    branch_name varchar,
    branch_is_active boolean,
    lead_teacher_count int,
    classroom_id uuid,
    classroom_name varchar,
    classroom_is_active boolean,
    classroom_status varchar,
    capacity int,
    current_enrolled int,
    assigned_teacher_count int
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_branch record;
    v_lead_count int := 0;
BEGIN
    SELECT id, name, is_active, organization_id INTO v_branch
    FROM branches
    WHERE tenant_id = p_tenant_id AND id = p_branch_id;

    IF NOT FOUND THEN
        RETURN QUERY SELECT false, ''::varchar, false, 0, NULL::uuid, ''::varchar, false, ''::varchar, 0, 0, 0 WHERE false;
        RETURN;
    END IF;

    -- Calculate lead teachers for the branch
    SELECT COUNT(DISTINCT u.id)::int INTO v_lead_count
    FROM users u
    JOIN (
        SELECT uor.user_id
        FROM user_organization_roles uor
        JOIN roles r ON uor.role_id = r.id AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
        WHERE uor.tenant_id = p_tenant_id OR uor.organization_id = v_branch.organization_id
        UNION
        SELECT ur.user_id
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id AND (r.name = 'LeadTeacher' OR r.name = 'Lead Teacher')
    ) lead_users ON lead_users.user_id = u.id
    LEFT JOIN teachers t ON t.user_id = u.id
    WHERE u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive')
      AND (
          EXISTS (
              SELECT 1 FROM user_branch_assignments uba
              WHERE uba.user_id = u.id AND uba.branch_id = p_branch_id AND uba.revoked_at IS NULL
          )
          OR EXISTS (
              SELECT 1 FROM classroom_teachers ct
              JOIN classrooms c ON ct.classroom_id = c.id
              WHERE ct.user_id = u.id AND c.branch_id = p_branch_id
          )
      );

    IF v_lead_count = 0 THEN
        SELECT COUNT(DISTINCT ct.user_id)::int INTO v_lead_count
        FROM classroom_teachers ct
        JOIN classrooms c ON ct.classroom_id = c.id
        JOIN users u ON ct.user_id = u.id
        LEFT JOIN teachers t ON t.user_id = u.id
        WHERE c.branch_id = p_branch_id AND ct.is_primary = true
          AND u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive');
    END IF;

    -- Return branch details along with all active classrooms, teachers, and enrolled children counts
    RETURN QUERY
    WITH classroom_teacher_counts AS (
        SELECT ct.classroom_id as c_id, COUNT(DISTINCT ct.user_id)::int as t_count
        FROM classroom_teachers ct
        JOIN classrooms c ON ct.classroom_id = c.id
        JOIN users u ON ct.user_id = u.id
        LEFT JOIN teachers t ON t.user_id = u.id
        WHERE c.branch_id = p_branch_id
          AND u.is_active = true AND (t.status IS NULL OR t.status != 'Inactive')
        GROUP BY ct.classroom_id
    ),
    classroom_enrollment_counts AS (
        SELECT ch.classroom_id as c_id, COUNT(*)::int as ch_count
        FROM children ch
        JOIN classrooms c ON ch.classroom_id = c.id
        WHERE c.branch_id = p_branch_id
          AND ch.is_active = true AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
        GROUP BY ch.classroom_id
    )
    SELECT 
        true as branch_exists,
        v_branch.name as branch_name,
        v_branch.is_active as branch_is_active,
        v_lead_count as lead_teacher_count,
        cl.id as classroom_id,
        cl.name as classroom_name,
        cl.is_active as classroom_is_active,
        cl.status as classroom_status,
        COALESCE(cl.capacity, 15) as capacity,
        COALESCE(cec.ch_count, 0) as current_enrolled,
        COALESCE(ctc.t_count, 0) as assigned_teacher_count
    FROM classrooms cl
    LEFT JOIN classroom_teacher_counts ctc ON ctc.c_id = cl.id
    LEFT JOIN classroom_enrollment_counts cec ON cec.c_id = cl.id
    WHERE cl.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id
      AND (cl.classroom_type IS NULL OR cl.classroom_type != 'Pool')
      AND (cl.code IS NULL OR cl.code != 'POOL-WAITING')
    ORDER BY cl.name;
END;
$$;


-- C. Privacy, Trust & Consent Procedures
CREATE OR REPLACE FUNCTION fn_is_privacy_and_trust_approved(
    p_tenant_id uuid,
    p_child_id uuid
)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM child_consents
        WHERE child_id = p_child_id
          AND consent_type = 'PrivacyAndTrust' AND granted = TRUE
          AND (tenant_id = p_tenant_id OR p_tenant_id IS NULL 
               OR p_tenant_id = '10000000-0000-0000-0000-000000000001' 
               OR tenant_id = (SELECT tenant_id FROM children WHERE id = p_child_id))
    );
$$;

CREATE OR REPLACE PROCEDURE sp_approve_privacy_and_trust(
    p_tenant_id uuid,
    p_child_id uuid,
    p_approved_by_name text
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_resolved_tenant_id uuid;
BEGIN
    SELECT COALESCE((SELECT tenant_id FROM children WHERE id = p_child_id), p_tenant_id)
    INTO v_resolved_tenant_id;

    INSERT INTO child_consents (id, tenant_id, child_id, consent_type, granted, granted_by, granted_at, version)
    VALUES (gen_random_uuid(), v_resolved_tenant_id, p_child_id, 'PrivacyAndTrust', TRUE, p_approved_by_name, NOW(), '1.0')
    ON CONFLICT (child_id, consent_type)
    DO UPDATE SET granted = TRUE, granted_by = EXCLUDED.granted_by, granted_at = NOW(), version = '1.0';
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_privacy_and_trust_approval_date(
    p_tenant_id uuid,
    p_child_id uuid
)
RETURNS timestamptz
LANGUAGE sql
STABLE
AS $$
    SELECT granted_at
    FROM child_consents
    WHERE child_id = p_child_id
      AND consent_type = 'PrivacyAndTrust' AND granted = TRUE
      AND (tenant_id = p_tenant_id OR p_tenant_id IS NULL 
           OR p_tenant_id = '10000000-0000-0000-0000-000000000001' 
           OR tenant_id = (SELECT tenant_id FROM children WHERE id = p_child_id))
    ORDER BY granted_at DESC
    LIMIT 1;
$$;


-- D. Secure Pickup: Atomic Person & Pickup Management
CREATE OR REPLACE PROCEDURE sp_save_authorized_pickup_with_person(
    p_tenant_id uuid,
    p_child_id uuid,
    p_first_name text,
    p_last_name text,
    p_relationship text,
    p_phone text,
    p_pickup_code text,
    p_notes text,
    INOUT p_pickup_id uuid,
    INOUT p_person_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_person_id IS NULL OR p_person_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        p_person_id := gen_random_uuid();
        INSERT INTO persons (id, tenant_id, first_name, last_name, relationship, mobile_phone, created_at, updated_at)
        VALUES (p_person_id, p_tenant_id, p_first_name, p_last_name, COALESCE(p_relationship, 'Authorized Pickup'), p_phone, NOW(), NOW());
    ELSE
        UPDATE persons
        SET first_name = p_first_name, last_name = p_last_name, mobile_phone = p_phone, 
            relationship = COALESCE(p_relationship, 'Authorized Pickup'), updated_at = NOW()
        WHERE id = p_person_id AND (tenant_id = p_tenant_id OR tenant_id = '10000000-0000-0000-0000-000000000001');
    END IF;

    IF p_pickup_id IS NULL OR p_pickup_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        p_pickup_id := gen_random_uuid();
    END IF;

    INSERT INTO authorized_pickups (id, tenant_id, child_id, person_id, relationship, pickup_code, notes, is_active)
    VALUES (p_pickup_id, p_tenant_id, p_child_id, p_person_id, COALESCE(p_relationship, 'Authorized Pickup'), p_pickup_code, p_notes, true)
    ON CONFLICT (id) DO UPDATE
    SET person_id = EXCLUDED.person_id,
        relationship = EXCLUDED.relationship,
        pickup_code = EXCLUDED.pickup_code,
        notes = EXCLUDED.notes,
        is_active = true;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_authorized_pickup(
    p_tenant_id uuid,
    p_child_id uuid,
    p_pickup_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE authorized_pickups
    SET is_active = false
    WHERE child_id = p_child_id AND id = p_pickup_id
      AND (tenant_id = p_tenant_id OR tenant_id = '10000000-0000-0000-0000-000000000001' OR p_tenant_id IS NULL);
END;
$$;


-- E. Daycare Announcements: Stored Procedures & Functions
CREATE OR REPLACE FUNCTION fn_get_daycare_announcements(
    p_tenant_id uuid,
    p_organization_id uuid DEFAULT NULL,
    p_branch_id uuid DEFAULT NULL,
    p_active_only boolean DEFAULT true,
    p_limit int DEFAULT 20
)
RETURNS TABLE (
    id uuid,
    tenant_id uuid,
    organization_id uuid,
    branch_id uuid,
    title varchar,
    content text,
    tag varchar,
    is_urgent boolean,
    author_name varchar,
    author_role varchar,
    is_active boolean,
    created_at timestamptz,
    updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
    SELECT da.id, da.tenant_id, da.organization_id, da.branch_id,
           da.title, da.content, da.tag, da.is_urgent, da.author_name, da.author_role,
           da.is_active, da.created_at, da.updated_at
    FROM daycare_announcements da
    WHERE (da.tenant_id = p_tenant_id OR da.tenant_id = '10000000-0000-0000-0000-000000000001' 
           OR (p_organization_id IS NOT NULL AND da.organization_id = p_organization_id))
      AND (p_branch_id IS NULL OR da.branch_id IS NULL OR da.branch_id = p_branch_id)
      AND (p_active_only = FALSE OR da.is_active = TRUE)
    ORDER BY da.is_urgent DESC, da.created_at DESC
    LIMIT p_limit;
$$;

CREATE OR REPLACE PROCEDURE sp_create_daycare_announcement(
    p_id uuid,
    p_tenant_id uuid,
    p_organization_id uuid,
    p_branch_id uuid,
    p_title varchar,
    p_content text,
    p_tag varchar,
    p_is_urgent boolean,
    p_author_name varchar,
    p_author_role varchar,
    p_is_active boolean,
    p_created_at timestamptz,
    p_updated_at timestamptz
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO daycare_announcements (
        id, tenant_id, organization_id, branch_id, title, content, tag, is_urgent,
        author_name, author_role, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_title, p_content, p_tag, p_is_urgent,
        p_author_name, p_author_role, p_is_active, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_set_daycare_announcement_status(
    p_id uuid,
    p_tenant_id uuid,
    p_is_active boolean
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE daycare_announcements
    SET is_active = p_is_active, updated_at = NOW()
    WHERE id = p_id
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id OR tenant_id = '10000000-0000-0000-0000-000000000001');
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_daycare_announcement(
    p_id uuid,
    p_tenant_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM daycare_announcements
    WHERE id = p_id
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id OR tenant_id = '10000000-0000-0000-0000-000000000001');
END;
$$;


-- F. Agent PAT & Webhook Subscriptions Procedures
CREATE OR REPLACE FUNCTION fn_get_agent_tokens_by_user(
    p_tenant_id uuid,
    p_user_id uuid
)
RETURNS TABLE (
    id uuid,
    tenant_id uuid,
    user_id uuid,
    token_name varchar,
    token_prefix varchar,
    role varchar,
    scopes text[],
    is_revoked boolean,
    expires_at timestamptz,
    last_used_at timestamptz,
    created_at timestamptz,
    updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
    SELECT t.id, t.tenant_id, t.user_id, t.token_name, t.token_prefix, t.role, t.scopes,
           t.is_revoked, t.expires_at, t.last_used_at, t.created_at, t.updated_at
    FROM agent_personal_access_tokens t
    WHERE t.tenant_id = p_tenant_id AND t.user_id = p_user_id
    ORDER BY t.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_agent_token_by_hash(
    p_token_hash varchar
)
RETURNS TABLE (
    id uuid,
    tenant_id uuid,
    user_id uuid,
    token_name varchar,
    token_prefix varchar,
    token_hash varchar,
    role varchar,
    scopes text[],
    is_revoked boolean,
    expires_at timestamptz,
    last_used_at timestamptz,
    created_at timestamptz,
    updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
    SELECT t.id, t.tenant_id, t.user_id, t.token_name, t.token_prefix, t.token_hash, t.role, t.scopes,
           t.is_revoked, t.expires_at, t.last_used_at, t.created_at, t.updated_at
    FROM agent_personal_access_tokens t
    WHERE t.token_hash = p_token_hash AND t.is_revoked = FALSE;
$$;

CREATE OR REPLACE PROCEDURE sp_create_agent_token(
    p_id uuid,
    p_tenant_id uuid,
    p_user_id uuid,
    p_token_name varchar,
    p_token_prefix varchar,
    p_token_hash varchar,
    p_role varchar,
    p_scopes text[],
    p_expires_at timestamptz
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO agent_personal_access_tokens (
        id, tenant_id, user_id, token_name, token_prefix, token_hash, 
        role, scopes, is_revoked, expires_at, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_user_id, p_token_name, p_token_prefix, p_token_hash,
        p_role, p_scopes, FALSE, p_expires_at, NOW(), NOW()
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_revoke_agent_token(
    p_tenant_id uuid,
    p_token_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE agent_personal_access_tokens
    SET is_revoked = TRUE, updated_at = NOW()
    WHERE id = p_token_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_agent_token_last_used(
    p_token_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE agent_personal_access_tokens
    SET last_used_at = NOW()
    WHERE id = p_token_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_agent_webhooks(
    p_tenant_id uuid
)
RETURNS TABLE (
    id uuid,
    tenant_id uuid,
    user_id uuid,
    name varchar,
    webhook_url text,
    secret_key varchar,
    subscribed_events text[],
    is_active boolean,
    last_triggered_at timestamptz,
    failure_count int,
    created_at timestamptz,
    updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
    SELECT w.id, w.tenant_id, w.user_id, w.name, w.webhook_url, w.secret_key,
           w.subscribed_events, w.is_active, w.last_triggered_at, w.failure_count,
           w.created_at, w.updated_at
    FROM agent_webhook_subscriptions w
    WHERE w.tenant_id = p_tenant_id
    ORDER BY w.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_active_agent_webhooks_for_event(
    p_tenant_id uuid,
    p_event_type varchar
)
RETURNS TABLE (
    id uuid,
    tenant_id uuid,
    user_id uuid,
    name varchar,
    webhook_url text,
    secret_key varchar,
    subscribed_events text[],
    is_active boolean,
    last_triggered_at timestamptz,
    failure_count int,
    created_at timestamptz,
    updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
    SELECT w.id, w.tenant_id, w.user_id, w.name, w.webhook_url, w.secret_key,
           w.subscribed_events, w.is_active, w.last_triggered_at, w.failure_count,
           w.created_at, w.updated_at
    FROM agent_webhook_subscriptions w
    WHERE w.tenant_id = p_tenant_id 
      AND w.is_active = TRUE
      AND (p_event_type = ANY(w.subscribed_events) OR '*' = ANY(w.subscribed_events));
$$;

CREATE OR REPLACE PROCEDURE sp_create_agent_webhook(
    p_id uuid,
    p_tenant_id uuid,
    p_user_id uuid,
    p_name varchar,
    p_webhook_url text,
    p_secret_key varchar,
    p_subscribed_events text[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO agent_webhook_subscriptions (
        id, tenant_id, user_id, name, webhook_url, secret_key, 
        subscribed_events, is_active, failure_count, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_user_id, p_name, p_webhook_url, p_secret_key, 
        p_subscribed_events, TRUE, 0, NOW(), NOW()
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_agent_webhook(
    p_tenant_id uuid,
    p_subscription_id uuid
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM agent_webhook_subscriptions
    WHERE id = p_subscription_id AND tenant_id = p_tenant_id;
END;
$$;
