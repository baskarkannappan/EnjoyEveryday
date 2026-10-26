-- Migration: 061_children_and_family_procedures.sql
-- Description: Stored procedures and functions for Children, Attendance, Daily Care, Secure Pickup, Parent Portal, and End of Day.

-- =========================================================================
-- 1. CHILD REPOSITORY FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_child_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_child_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    classroom_id UUID,
    photo_url TEXT,
    profile_completion_percentage INT,
    enrollment_status VARCHAR,
    draft_data TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id,
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data,
           c.is_active, c.created_at, c.updated_at
    FROM children c
    WHERE c.id = p_id AND c.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_children(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_children(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    classroom_id UUID,
    photo_url TEXT,
    profile_completion_percentage INT,
    enrollment_status VARCHAR,
    draft_data TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id, 
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data, 
           c.is_active, c.created_at, c.updated_at
    FROM children c
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id AND c.tenant_id = cl.tenant_id
    LEFT JOIN branches b ON cl.branch_id = b.id AND cl.tenant_id = b.tenant_id
    WHERE c.tenant_id = p_tenant_id AND c.is_active = true
      AND (p_organization_id IS NULL 
           OR b.organization_id = p_organization_id 
           OR EXISTS (
               SELECT 1 FROM enrollments e 
               JOIN branches eb ON e.branch_id = eb.id 
               WHERE e.child_id = c.id AND eb.organization_id = p_organization_id
           ))
    ORDER BY c.first_name, c.last_name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_children_by_classroom(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_children_by_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    classroom_id UUID,
    photo_url TEXT,
    profile_completion_percentage INT,
    enrollment_status VARCHAR,
    draft_data TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id,
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data,
           c.is_active, c.created_at, c.updated_at
    FROM children c
    WHERE c.classroom_id = p_classroom_id AND c.tenant_id = p_tenant_id AND c.is_active = true
    ORDER BY c.first_name, c.last_name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_child(UUID, UUID, VARCHAR, VARCHAR, DATE, UUID, VARCHAR, INT, VARCHAR, JSONB, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_child(
    p_id UUID,
    p_tenant_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth DATE,
    p_classroom_id UUID,
    p_photo_url TEXT,
    p_profile_completion_percentage INT,
    p_enrollment_status VARCHAR,
    p_draft_data TEXT,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    classroom_id UUID,
    photo_url TEXT,
    profile_completion_percentage INT,
    enrollment_status VARCHAR,
    draft_data TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO children (
        id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url,
        profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_first_name, p_last_name, p_date_of_birth, p_classroom_id, p_photo_url,
        p_profile_completion_percentage, p_enrollment_status, p_draft_data, p_is_active, p_created_at, p_updated_at
    )
    RETURNING children.id, children.tenant_id, children.first_name, children.last_name, children.date_of_birth,
              children.classroom_id, children.photo_url, children.profile_completion_percentage,
              children.enrollment_status, children.draft_data, children.is_active, children.created_at, children.updated_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_child(
    p_id UUID,
    p_tenant_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth DATE,
    p_classroom_id UUID,
    p_photo_url TEXT,
    p_profile_completion_percentage INT,
    p_enrollment_status VARCHAR,
    p_draft_data TEXT,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE children
    SET first_name = p_first_name,
        last_name = p_last_name,
        date_of_birth = p_date_of_birth,
        classroom_id = p_classroom_id,
        photo_url = p_photo_url,
        profile_completion_percentage = p_profile_completion_percentage,
        enrollment_status = p_enrollment_status,
        draft_data = p_draft_data,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_child(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE children SET is_active = false
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_sync_parent_guardian(
    p_tenant_id UUID,
    p_child_id UUID,
    p_parent_email VARCHAR,
    p_parent_name VARCHAR,
    p_relationship VARCHAR,
    p_phone VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_norm_email VARCHAR;
    v_trimmed_email VARCHAR;
    v_user_id UUID;
    v_role_id UUID;
    v_person_id UUID;
    v_first_name VARCHAR;
    v_last_name VARCHAR;
    v_rel VARCHAR;
    v_name_parts TEXT[];
BEGIN
    IF p_parent_email IS NULL OR TRIM(p_parent_email) = '' THEN
        RETURN;
    END IF;

    v_trimmed_email := TRIM(p_parent_email);
    v_norm_email := UPPER(v_trimmed_email);
    v_rel := COALESCE(NULLIF(TRIM(p_relationship), ''), 'Parent');

    -- Split name into first and last
    v_name_parts := regexp_split_to_array(COALESCE(NULLIF(TRIM(p_parent_name), ''), 'Parent Guardian'), '\s+');
    v_first_name := v_name_parts[1];
    IF array_length(v_name_parts, 1) > 1 THEN
        v_last_name := array_to_string(v_name_parts[2:], ' ');
    ELSE
        v_last_name := 'Guardian';
    END IF;

    -- 1. Find or create User
    SELECT id INTO v_user_id FROM users WHERE normalized_email = v_norm_email LIMIT 1;
    IF v_user_id IS NULL THEN
        v_user_id := gen_random_uuid();
        INSERT INTO users (
            id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
        ) VALUES (
            v_user_id, p_tenant_id, v_trimmed_email, v_norm_email, TRUE, v_first_name, v_last_name,
            COALESCE(NULLIF(TRIM(p_parent_name), ''), v_first_name || ' ' || v_last_name),
            '', md5(random()::text), TRUE, NOW(), NOW()
        ) ON CONFLICT (normalized_email) DO NOTHING;

        SELECT id INTO v_user_id FROM users WHERE normalized_email = v_norm_email LIMIT 1;

        SELECT id INTO v_role_id FROM roles WHERE name = 'Parent' AND (tenant_id = p_tenant_id OR tenant_id IS NULL) LIMIT 1;
        IF v_role_id IS NOT NULL THEN
            INSERT INTO user_roles (user_id, role_id) VALUES (v_user_id, v_role_id) ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 2. Find or create Person
    SELECT id INTO v_person_id FROM persons WHERE tenant_id = p_tenant_id AND user_id = v_user_id LIMIT 1;
    IF v_person_id IS NULL THEN
        v_person_id := gen_random_uuid();
        INSERT INTO persons (
            id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, has_portal_access, receives_daily_updates, receives_photos, created_at, updated_at
        ) VALUES (
            v_person_id, p_tenant_id, v_user_id, v_first_name, v_last_name, v_rel, v_trimmed_email, NULLIF(TRIM(p_phone), ''), TRUE, TRUE, TRUE, NOW(), NOW()
        );
    ELSE
        IF p_phone IS NOT NULL AND TRIM(p_phone) <> '' THEN
            UPDATE persons SET mobile_phone = TRIM(p_phone), updated_at = NOW() WHERE id = v_person_id;
        END IF;
    END IF;

    -- 3. Link child_guardians
    INSERT INTO child_guardians (
        id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_person_id, v_rel, TRUE, TRUE, TRUE, TRUE, TRUE
    ) ON CONFLICT (child_id, person_id) DO UPDATE SET 
        relationship = EXCLUDED.relationship, 
        can_pickup = TRUE;

    -- 4. Link family_relationships
    INSERT INTO family_relationships (
        id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_user_id, v_rel, TRUE, NOW(), 'Verified', NOW()
    ) ON CONFLICT (child_id, user_id) DO UPDATE SET 
        relationship = EXCLUDED.relationship,
        status = 'Verified',
        revoked_at = NULL,
        verified_at = NOW();

    -- 5. Link authorized_pickups
    INSERT INTO authorized_pickups (
        id, tenant_id, child_id, person_id, relationship, is_active, notes
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_person_id, v_rel, TRUE, 'Primary guardian on file.'
    ) ON CONFLICT DO NOTHING;
END;
$$;

-- =========================================================================
-- 2. ATTENDANCE REPOSITORY FUNCTIONS & PROCEDURES
-- =========================================================================

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date DATE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    attendance_date DATE,
    status VARCHAR,
    arrival_time TIME,
    departure_time TIME,
    notes VARCHAR,
    recorded_by_user_id UUID,
    pickup_person_name VARCHAR,
    pickup_person_relationship VARCHAR,
    pickup_verification_method VARCHAR,
    pickup_notes VARCHAR,
    authorized_pickup_id UUID,
    departure_verified_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ca.id, ca.tenant_id, ca.branch_id, ca.classroom_id, ca.child_id, ca.attendance_date, ca.status,
           ca.arrival_time, ca.departure_time, ca.notes, ca.recorded_by_user_id, ca.pickup_person_name,
           ca.pickup_person_relationship, ca.pickup_verification_method, ca.pickup_notes,
           ca.authorized_pickup_id, ca.departure_verified_by_user_id, ca.created_at, ca.updated_at
    FROM child_attendance ca
    WHERE ca.tenant_id = p_tenant_id AND ca.branch_id = p_branch_id AND ca.attendance_date = p_date;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date DATE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    teacher_id UUID,
    attendance_date DATE,
    status VARCHAR,
    clock_in_time TIME,
    clock_out_time TIME,
    notes VARCHAR,
    recorded_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ta.id, ta.tenant_id, ta.branch_id, ta.teacher_id, ta.attendance_date, ta.status,
           ta.clock_in_time, ta.clock_out_time, ta.notes, ta.recorded_by_user_id, ta.created_at, ta.updated_at
    FROM teacher_attendance ta
    WHERE ta.tenant_id = p_tenant_id AND ta.branch_id = p_branch_id AND ta.attendance_date = p_date;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_record_child_attendance(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_attendance_date DATE,
    p_status VARCHAR,
    p_arrival_time TIME,
    p_departure_time TIME,
    p_notes VARCHAR,
    p_recorded_by_user_id UUID,
    p_pickup_person_name VARCHAR,
    p_pickup_person_relationship VARCHAR,
    p_pickup_verification_method VARCHAR,
    p_pickup_notes VARCHAR,
    p_authorized_pickup_id UUID,
    p_departure_verified_by_user_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_attendance (
        id, tenant_id, branch_id, classroom_id, child_id, attendance_date, status, 
        arrival_time, departure_time, notes, recorded_by_user_id,
        pickup_person_name, pickup_person_relationship, pickup_verification_method, pickup_notes, authorized_pickup_id, departure_verified_by_user_id,
        created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_branch_id, p_classroom_id, p_child_id, p_attendance_date, p_status, 
        p_arrival_time, p_departure_time, p_notes, p_recorded_by_user_id,
        p_pickup_person_name, p_pickup_person_relationship, p_pickup_verification_method, p_pickup_notes, p_authorized_pickup_id, p_departure_verified_by_user_id,
        CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (id) DO UPDATE SET 
        status = EXCLUDED.status, 
        arrival_time = EXCLUDED.arrival_time, 
        departure_time = EXCLUDED.departure_time, 
        notes = EXCLUDED.notes,
        pickup_person_name = EXCLUDED.pickup_person_name,
        pickup_person_relationship = EXCLUDED.pickup_person_relationship,
        pickup_verification_method = EXCLUDED.pickup_verification_method,
        pickup_notes = EXCLUDED.pickup_notes,
        authorized_pickup_id = EXCLUDED.authorized_pickup_id,
        departure_verified_by_user_id = EXCLUDED.departure_verified_by_user_id,
        updated_at = CURRENT_TIMESTAMP;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_record_teacher_attendance(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_teacher_id UUID,
    p_attendance_date DATE,
    p_status VARCHAR,
    p_clock_in_time TIME,
    p_clock_out_time TIME,
    p_notes VARCHAR,
    p_recorded_by_user_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    teacher_id UUID,
    attendance_date DATE,
    status VARCHAR,
    clock_in_time TIME,
    clock_out_time TIME,
    notes VARCHAR,
    recorded_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_actual_teacher_id UUID;
    v_attendance_id UUID;
    v_user RECORD;
BEGIN
    -- Check if teacher_id is an actual teacher id or user id
    SELECT t.id INTO v_actual_teacher_id FROM teachers t WHERE t.id = p_teacher_id OR t.user_id = p_teacher_id LIMIT 1;
    
    IF v_actual_teacher_id IS NULL THEN
        SELECT u.id, u.tenant_id, u.first_name, u.last_name, u.display_name INTO v_user FROM users u WHERE u.id = p_teacher_id;
        IF v_user.id IS NOT NULL THEN
            v_actual_teacher_id := gen_random_uuid();
            INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
            VALUES (
                v_actual_teacher_id,
                COALESCE(NULLIF(p_tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), v_user.tenant_id),
                v_user.id,
                COALESCE(v_user.first_name, 'Teacher'),
                COALESCE(v_user.last_name, ''),
                COALESCE(v_user.display_name, v_user.first_name || ' ' || v_user.last_name),
                'Active', NOW(), NOW()
            ) ON CONFLICT (id) DO NOTHING;
        ELSE
            v_actual_teacher_id := p_teacher_id;
        END IF;
    END IF;

    v_attendance_id := p_id;
    IF v_attendance_id IS NULL OR v_attendance_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        SELECT ta.id INTO v_attendance_id
        FROM teacher_attendance ta
        WHERE ta.branch_id = p_branch_id AND ta.teacher_id = v_actual_teacher_id AND ta.attendance_date = p_attendance_date;
        
        IF v_attendance_id IS NULL THEN
            v_attendance_id := gen_random_uuid();
        END IF;
    END IF;

    INSERT INTO teacher_attendance (
        id, tenant_id, branch_id, teacher_id, attendance_date, status, clock_in_time, clock_out_time, notes, recorded_by_user_id, created_at, updated_at
    )
    VALUES (
        v_attendance_id, p_tenant_id, p_branch_id, v_actual_teacher_id, p_attendance_date, p_status, p_clock_in_time, p_clock_out_time, p_notes, p_recorded_by_user_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (id) DO UPDATE SET 
        status = EXCLUDED.status, clock_in_time = EXCLUDED.clock_in_time, clock_out_time = EXCLUDED.clock_out_time, notes = EXCLUDED.notes, updated_at = CURRENT_TIMESTAMP;

    RETURN QUERY
    SELECT ta.id, ta.tenant_id, ta.branch_id, ta.teacher_id, ta.attendance_date, ta.status,
           ta.clock_in_time, ta.clock_out_time, ta.notes, ta.recorded_by_user_id, ta.created_at, ta.updated_at
    FROM teacher_attendance ta
    WHERE ta.id = v_attendance_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_attendance_summary(
    p_tenant_id UUID,
    p_date DATE
)
RETURNS TABLE (
    expected_children INT,
    present_children INT,
    absent_children INT,
    late_children INT,
    expected_teachers INT,
    present_teachers INT,
    absent_teachers INT,
    late_teachers INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        (SELECT COUNT(*)::INT FROM children WHERE tenant_id = p_tenant_id AND is_active = true) AS expected_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Present') AS present_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Absent') AS absent_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Late') AS late_children,
        
        (SELECT COUNT(*)::INT FROM teachers WHERE tenant_id = p_tenant_id AND status != 'Inactive') AS expected_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Present') AS present_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Absent') AS absent_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Late') AS late_teachers;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_present_child_ids_by_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date DATE
)
RETURNS TABLE (child_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ca.child_id
    FROM child_attendance ca
    WHERE ca.tenant_id = p_tenant_id AND ca.classroom_id = p_classroom_id AND ca.attendance_date = p_date AND ca.status IN ('Present', 'Late');
END;
$$;

-- =========================================================================
-- 3. SECURE PICKUP REPOSITORY FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_child_pickup_header(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_child_pickup_header(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    photo_url TEXT,
    classroom_name VARCHAR,
    branch_name VARCHAR,
    classroom_id UUID,
    branch_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.first_name, c.last_name, c.photo_url,
           cr.name::VARCHAR AS classroom_name, b.name::VARCHAR AS branch_name, cr.id AS classroom_id, b.id AS branch_id
    FROM children c
    LEFT JOIN classrooms cr ON c.classroom_id = cr.id
    LEFT JOIN branches b ON cr.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id AND c.id = p_child_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_active_custody_restrictions(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_active_custody_restrictions(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    restriction_type VARCHAR,
    notes VARCHAR,
    effective_from TIMESTAMPTZ,
    effective_to TIMESTAMPTZ,
    person_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT r.id, r.restriction_type::VARCHAR, r.notes::VARCHAR, r.effective_from, r.effective_to,
           COALESCE(p.first_name || ' ' || p.last_name, 'Restricted Individual')::VARCHAR AS person_name
    FROM child_person_restrictions r
    LEFT JOIN persons p ON r.person_id = p.id
    WHERE r.tenant_id = p_tenant_id AND r.child_id = p_child_id
      AND (r.effective_to IS NULL OR r.effective_to >= NOW());
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_authorized_pickups(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_authorized_pickups(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    child_id UUID,
    person_id UUID,
    relationship VARCHAR,
    pickup_code VARCHAR,
    photo_url TEXT,
    is_active BOOLEAN,
    notes VARCHAR,
    full_name VARCHAR,
    phone VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ap.id, ap.child_id, ap.person_id, ap.relationship::VARCHAR, ap.pickup_code::VARCHAR,
           ap.photo_url::TEXT, ap.is_active, ap.notes::VARCHAR,
           COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), ap.relationship, 'Authorized Pickup')::VARCHAR AS full_name,
           COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS phone
    FROM authorized_pickups ap
    LEFT JOIN persons p ON ap.person_id = p.id
    WHERE ap.tenant_id = p_tenant_id AND ap.child_id = p_child_id AND ap.is_active = TRUE
    ORDER BY ap.is_active DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_family_pickup_relationships(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_family_pickup_relationships(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    child_id UUID,
    person_id UUID,
    relationship VARCHAR,
    is_primary BOOLEAN,
    is_emergency BOOLEAN,
    full_name VARCHAR,
    phone VARCHAR,
    photo_url TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH guardians AS (
        SELECT 
            fr.id AS g_id,
            fr.child_id AS g_child_id,
            COALESCE(p.id, u.id) AS g_person_id,
            COALESCE(fr.relationship, p.relationship, 'Legal Guardian')::VARCHAR AS g_relationship,
            fr.is_primary AS g_is_primary,
            FALSE AS g_is_emergency,
            COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), NULLIF(TRIM(u.first_name || ' ' || u.last_name), ''), u.display_name, 'Parent / Guardian')::VARCHAR AS g_full_name,
            COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS g_phone,
            NULL::TEXT AS g_photo_url
        FROM family_relationships fr
        JOIN users u ON fr.user_id = u.id
        LEFT JOIN persons p ON p.user_id = u.id
        WHERE fr.tenant_id = p_tenant_id AND fr.child_id = p_child_id
          AND (fr.status IS NULL OR fr.status = 'Verified')
          AND fr.revoked_at IS NULL

        UNION ALL

        SELECT 
            cg.id AS g_id,
            cg.child_id AS g_child_id,
            p.id AS g_person_id,
            COALESCE(cg.relationship, p.relationship, 'Legal Guardian')::VARCHAR AS g_relationship,
            cg.is_primary AS g_is_primary,
            FALSE AS g_is_emergency,
            COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), 'Parent / Guardian')::VARCHAR AS g_full_name,
            COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS g_phone,
            NULL::TEXT AS g_photo_url
        FROM child_guardians cg
        JOIN persons p ON cg.person_id = p.id
        WHERE cg.tenant_id = p_tenant_id AND cg.child_id = p_child_id
          AND (cg.can_pickup = TRUE OR cg.is_primary = TRUE)
    )
    SELECT DISTINCT ON (g_person_id)
           g_id, g_child_id, g_person_id, g_relationship, g_is_primary, g_is_emergency, g_full_name, g_phone, g_photo_url
    FROM guardians
    ORDER BY g_person_id, g_is_primary DESC;
END;
$$;

-- =========================================================================
-- 4. PARENT REPOSITORY FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_parent_authorized_children(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_parent_authorized_children(
    p_tenant_id UUID,
    p_user_id UUID
)
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
    is_primary BOOLEAN
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
        fr.is_primary
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

CREATE OR REPLACE FUNCTION public.fn_has_parent_child_access(
    p_tenant_id UUID,
    p_user_id UUID,
    p_child_id UUID
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 
        FROM family_relationships fr
        JOIN children c ON fr.child_id = c.id
        WHERE fr.user_id = p_user_id 
          AND fr.child_id = p_child_id
          AND fr.status = 'Verified' AND fr.revoked_at IS NULL
          AND c.is_active = true
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_person_by_user_id(
    p_tenant_id UUID,
    p_user_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    user_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    relationship VARCHAR,
    email VARCHAR,
    mobile_phone VARCHAR,
    alternate_phone VARCHAR,
    preferred_contact_method VARCHAR,
    preferred_language VARCHAR,
    has_portal_access BOOLEAN,
    receives_daily_updates BOOLEAN,
    receives_photos BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.tenant_id, p.user_id, p.first_name, p.last_name, p.relationship,
           p.email, p.mobile_phone, p.alternate_phone, p.preferred_contact_method,
           p.preferred_language, p.has_portal_access, p.receives_daily_updates,
           p.receives_photos, p.created_at, p.updated_at
    FROM persons p
    WHERE p.user_id = p_user_id
    ORDER BY (p.tenant_id = p_tenant_id) DESC
    LIMIT 1;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_person_contact(
    p_tenant_id UUID,
    p_person_id UUID,
    p_mobile_phone VARCHAR,
    p_preferred_contact_method VARCHAR,
    p_preferred_language VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE persons
    SET mobile_phone = p_mobile_phone,
        preferred_contact_method = p_preferred_contact_method,
        preferred_language = p_preferred_language,
        updated_at = NOW()
    WHERE id = p_person_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 5. END OF DAY REPOSITORY FUNCTIONS & PROCEDURES
-- =========================================================================

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_teachers(
    p_tenant_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ta.id,
           COALESCE(t.display_name, u.display_name, u.first_name || ' ' || u.last_name, 'Educator')::VARCHAR AS name
    FROM teacher_attendance ta
    LEFT JOIN teachers t ON ta.teacher_id = t.id
    LEFT JOIN users u ON t.user_id = u.id OR ta.teacher_id = u.id
    WHERE ta.tenant_id = p_tenant_id AND ta.attendance_date = p_date
      AND ta.status IN ('Present', 'Late') AND ta.clock_out_time IS NULL
      AND (p_branch_id IS NULL OR ta.branch_id = p_branch_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(
    p_clock_out TIME,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teacher_attendance
    SET clock_out_time = p_clock_out,
        notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'
                     ELSE notes || ' · System auto-checkout at End of Day' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_children(
    p_tenant_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ca.id, (c.first_name || ' ' || c.last_name)::VARCHAR AS name
    FROM child_attendance ca
    JOIN children c ON ca.child_id = c.id
    WHERE ca.tenant_id = p_tenant_id AND ca.attendance_date = p_date
      AND ca.status IN ('Present', 'Late') AND ca.departure_time IS NULL
      AND (p_branch_id IS NULL OR ca.branch_id = p_branch_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_children(
    p_departure TIME,
    p_user_id UUID,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_attendance
    SET departure_time = p_departure,
        pickup_person_name = 'System Auto-Closed',
        pickup_person_relationship = 'System Process',
        pickup_verification_method = 'System Auto-Closed',
        pickup_notes = CASE WHEN pickup_notes IS NULL OR pickup_notes = '' THEN 'System auto-checkout at End of Day'
                            ELSE pickup_notes || ' · System auto-checkout at End of Day' END,
        departure_verified_by_user_id = p_user_id,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_activities(
    p_tenant_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, COALESCE(e.title, 'Planned Experience')::VARCHAR AS name
    FROM experience_schedules es
    LEFT JOIN experiences e ON es.experience_id = e.id
    LEFT JOIN classrooms cr ON es.classroom_id = cr.id
    WHERE es.tenant_id = p_tenant_id AND es.scheduled_date = p_date
      AND es.status IN ('Planned', 'Scheduled', 'Confirmed', 'Started', 'Active', 'InProgress')
      AND (p_branch_id IS NULL OR cr.branch_id = p_branch_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_activities(
    p_tenant_id UUID,
    p_schedule_ids UUID[],
    p_end_time TIME
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_schedules
    SET status = 'Completed',
        preparation_notes = CASE WHEN preparation_notes IS NULL OR preparation_notes = '' THEN 'System Auto-Closed at End of Day'
                                 WHEN preparation_notes LIKE '%System Auto-Closed%' THEN preparation_notes
                                 ELSE preparation_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_schedule_ids);

    UPDATE experience_sessions
    SET status = 'Completed', end_time = p_end_time,
        actual_notes = CASE WHEN actual_notes IS NULL OR actual_notes = '' THEN 'System Auto-Closed at End of Day'
                            WHEN actual_notes LIKE '%System Auto-Closed%' THEN actual_notes
                            ELSE actual_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id
      AND experience_schedule_id = ANY(p_schedule_ids)
      AND status NOT IN ('Completed', 'Cancelled', 'Finished');
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_events(
    p_tenant_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.title::VARCHAR AS name
    FROM events ev
    WHERE ev.tenant_id = p_tenant_id AND ev.event_date = p_date
      AND ev.status IN ('Draft', 'Planned', 'Approved', 'Published', 'InProgress')
      AND (p_branch_id IS NULL OR ev.scope = 'Organization' OR ev.is_organization_wide = true OR ev.branch_id = p_branch_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_events(
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE events
    SET status = 'Completed',
        description = CASE WHEN description IS NULL OR description = '' THEN 'System Auto-Closed at End of Day'
                           WHEN description LIKE '%System Auto-Closed%' THEN description
                           ELSE description || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;
