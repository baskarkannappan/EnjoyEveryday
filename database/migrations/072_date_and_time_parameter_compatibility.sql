-- Migration: 072_date_and_time_parameter_compatibility.sql
-- Description: Align date and time parameters in stored procedures and functions with Npgsql/.NET bindings (TIMESTAMPTZ for DateTime, INTERVAL for TimeSpan).

-- =========================================================================
-- 1. EXPERIENCE SESSIONS & SCHEDULES
-- =========================================================================

DROP PROCEDURE IF EXISTS public.sp_create_experience_session(UUID, UUID, UUID, UUID, UUID, DATE, TIME, TIME, VARCHAR, UUID, TEXT);
DROP PROCEDURE IF EXISTS public.sp_create_experience_session(UUID, UUID, UUID, UUID, UUID, TIMESTAMPTZ, INTERVAL, INTERVAL, VARCHAR, UUID, TEXT);
CREATE OR REPLACE PROCEDURE public.sp_create_experience_session(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_id UUID,
    p_experience_schedule_id UUID,
    p_classroom_id UUID,
    p_session_date TIMESTAMPTZ,
    p_start_time INTERVAL,
    p_end_time INTERVAL,
    p_status VARCHAR,
    p_primary_teacher_id UUID,
    p_actual_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO experience_sessions (
        id, tenant_id, experience_id, experience_schedule_id, classroom_id, 
        session_date, start_time, end_time, status, primary_teacher_id, actual_notes
    ) VALUES (
        p_id, p_tenant_id, p_experience_id, p_experience_schedule_id, p_classroom_id, 
        p_session_date::DATE, p_start_time::time, p_end_time::time, p_status, p_primary_teacher_id, p_actual_notes
    );
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_update_experience_session(UUID, UUID, VARCHAR, TEXT, TIME, TIME);
DROP PROCEDURE IF EXISTS public.sp_update_experience_session(UUID, UUID, VARCHAR, TEXT, INTERVAL, INTERVAL);
CREATE OR REPLACE PROCEDURE public.sp_update_experience_session(
    p_id UUID,
    p_tenant_id UUID,
    p_status VARCHAR,
    p_actual_notes TEXT,
    p_start_time INTERVAL,
    p_end_time INTERVAL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_sessions SET
        status = p_status,
        actual_notes = p_actual_notes,
        start_time = p_start_time::time,
        end_time = p_end_time::time,
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_update_experience_schedule(UUID, UUID, DATE, VARCHAR, VARCHAR, UUID, TIME, TIME, TEXT, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_experience_schedule(UUID, UUID, TIMESTAMPTZ, VARCHAR, VARCHAR, UUID, INTERVAL, INTERVAL, TEXT, TIMESTAMPTZ);
CREATE OR REPLACE PROCEDURE public.sp_update_experience_schedule(
    p_id UUID,
    p_tenant_id UUID,
    p_scheduled_date TIMESTAMPTZ,
    p_time_of_day VARCHAR,
    p_status VARCHAR,
    p_primary_teacher_id UUID,
    p_planned_start_time INTERVAL,
    p_planned_end_time INTERVAL,
    p_preparation_notes TEXT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_schedules
    SET scheduled_date = p_scheduled_date::DATE,
        time_of_day = p_time_of_day,
        status = p_status,
        primary_teacher_id = p_primary_teacher_id,
        planned_start_time = p_planned_start_time::time,
        planned_end_time = p_planned_end_time::time,
        preparation_notes = p_preparation_notes,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_experience_schedule(UUID, UUID, UUID, UUID, DATE, VARCHAR, VARCHAR, UUID, TIME, TIME, TEXT, TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_experience_schedule(UUID, UUID, UUID, UUID, TIMESTAMPTZ, VARCHAR, VARCHAR, UUID, INTERVAL, INTERVAL, TEXT, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_experience_schedule(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_id UUID,
    p_classroom_id UUID,
    p_scheduled_date TIMESTAMPTZ,
    p_time_of_day VARCHAR,
    p_status VARCHAR,
    p_primary_teacher_id UUID,
    p_planned_start_time INTERVAL,
    p_planned_end_time INTERVAL,
    p_preparation_notes TEXT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    classroom_id UUID,
    scheduled_date DATE,
    time_of_day VARCHAR,
    status VARCHAR,
    primary_teacher_id UUID,
    planned_start_time TIME,
    planned_end_time TIME,
    preparation_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO experience_schedules (
        id, tenant_id, experience_id, classroom_id, scheduled_date, time_of_day, status,
        primary_teacher_id, planned_start_time, planned_end_time, preparation_notes, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_experience_id, p_classroom_id, p_scheduled_date::DATE, p_time_of_day, p_status,
        p_primary_teacher_id, p_planned_start_time::time, p_planned_end_time::time, p_preparation_notes, p_created_at, p_updated_at
    )
    RETURNING experience_schedules.id, experience_schedules.tenant_id, experience_schedules.experience_id,
              experience_schedules.classroom_id, experience_schedules.scheduled_date, experience_schedules.time_of_day,
              experience_schedules.status, experience_schedules.primary_teacher_id, experience_schedules.planned_start_time,
              experience_schedules.planned_end_time, experience_schedules.preparation_notes,
              experience_schedules.created_at, experience_schedules.updated_at;
END;
$$;

-- =========================================================================
-- 2. CHILD & TEACHER ATTENDANCE
-- =========================================================================

DROP PROCEDURE IF EXISTS public.sp_record_child_attendance(UUID, UUID, UUID, UUID, UUID, DATE, VARCHAR, TIME, TIME, VARCHAR, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, UUID, UUID);
DROP PROCEDURE IF EXISTS public.sp_record_child_attendance(UUID, UUID, UUID, UUID, UUID, TIMESTAMPTZ, VARCHAR, INTERVAL, INTERVAL, VARCHAR, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, UUID, UUID);
CREATE OR REPLACE PROCEDURE public.sp_record_child_attendance(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_attendance_date TIMESTAMPTZ,
    p_status VARCHAR,
    p_arrival_time INTERVAL,
    p_departure_time INTERVAL,
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
        p_id, p_tenant_id, p_branch_id, p_classroom_id, p_child_id, p_attendance_date::DATE, p_status, 
        p_arrival_time::time, p_departure_time::time, p_notes, p_recorded_by_user_id,
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

DROP FUNCTION IF EXISTS public.fn_record_teacher_attendance(UUID, UUID, UUID, UUID, DATE, VARCHAR, TIME, TIME, VARCHAR, UUID) CASCADE;
DROP FUNCTION IF EXISTS public.fn_record_teacher_attendance(UUID, UUID, UUID, UUID, DATE, VARCHAR, TIME, TIME, TEXT, UUID) CASCADE;
DROP FUNCTION IF EXISTS public.fn_record_teacher_attendance(UUID, UUID, UUID, UUID, TIMESTAMPTZ, VARCHAR, INTERVAL, INTERVAL, TEXT, UUID) CASCADE;
CREATE OR REPLACE FUNCTION public.fn_record_teacher_attendance(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_teacher_id UUID,
    p_attendance_date TIMESTAMPTZ,
    p_status VARCHAR,
    p_clock_in_time INTERVAL,
    p_clock_out_time INTERVAL,
    p_notes TEXT,
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
    notes TEXT,
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
    SELECT t.id INTO v_actual_teacher_id FROM teachers t WHERE t.id = p_teacher_id OR t.user_id = p_teacher_id LIMIT 1;
    
    IF v_actual_teacher_id IS NULL THEN
        SELECT u.id, u.tenant_id, u.first_name, u.last_name, u.display_name INTO v_user FROM users u WHERE u.id = p_teacher_id;
        IF v_user.id IS NOT NULL THEN
            v_actual_teacher_id := gen_random_uuid();
            INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
            VALUES (
                v_actual_teacher_id,
                p_tenant_id,
                v_user.id,
                COALESCE(v_user.first_name, 'Teacher'),
                COALESCE(v_user.last_name, ''),
                COALESCE(v_user.display_name, 'Teacher'),
                'Active',
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            );
        END IF;
    END IF;

    IF v_actual_teacher_id IS NULL THEN
        v_actual_teacher_id := p_teacher_id;
    END IF;

    SELECT ta.id INTO v_attendance_id
    FROM teacher_attendance ta
    WHERE ta.tenant_id = p_tenant_id
      AND ta.teacher_id = v_actual_teacher_id
      AND ta.attendance_date = p_attendance_date::DATE
    LIMIT 1;

    IF v_attendance_id IS NOT NULL THEN
        UPDATE teacher_attendance
        SET branch_id = p_branch_id,
            status = p_status,
            clock_in_time = COALESCE(p_clock_in_time::time, clock_in_time),
            clock_out_time = COALESCE(p_clock_out_time::time, clock_out_time),
            notes = COALESCE(p_notes, notes),
            recorded_by_user_id = p_recorded_by_user_id,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = v_attendance_id;
    ELSE
        v_attendance_id := COALESCE(p_id, gen_random_uuid());
        INSERT INTO teacher_attendance (
            id, tenant_id, branch_id, teacher_id, attendance_date, status,
            clock_in_time, clock_out_time, notes, recorded_by_user_id,
            created_at, updated_at
        )
        VALUES (
            v_attendance_id, p_tenant_id, p_branch_id, v_actual_teacher_id, p_attendance_date::DATE, p_status,
            p_clock_in_time::time, p_clock_out_time::time, p_notes, p_recorded_by_user_id,
            CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
        );
    END IF;

    RETURN QUERY
    SELECT ta.id, ta.tenant_id, ta.branch_id, ta.teacher_id, ta.attendance_date, ta.status,
           ta.clock_in_time, ta.clock_out_time, ta.notes, ta.recorded_by_user_id,
           ta.created_at, ta.updated_at
    FROM teacher_attendance ta
    WHERE ta.id = v_attendance_id;
END;
$$;

-- =========================================================================
-- 3. END OF DAY BATCH PROCEDURES
-- =========================================================================

DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_teachers(TIME, UUID[]);
DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_teachers(INTERVAL, UUID[]);
CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(
    p_clock_out INTERVAL,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teacher_attendance
    SET clock_out_time = p_clock_out::time,
        notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'
                     ELSE notes || ' · System auto-checkout at End of Day' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_children(TIME, UUID, UUID[]);
DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_children(INTERVAL, UUID, UUID[]);
CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_children(
    p_departure INTERVAL,
    p_user_id UUID,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_attendance
    SET departure_time = p_departure::time,
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

DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_activities(UUID, UUID[], TIME);
DROP PROCEDURE IF EXISTS public.sp_close_end_of_day_activities(UUID, UUID[], INTERVAL);
CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_activities(
    p_tenant_id UUID,
    p_schedule_ids UUID[],
    p_end_time INTERVAL
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
    SET status = 'Completed', end_time = p_end_time::time,
        actual_notes = CASE WHEN actual_notes IS NULL OR actual_notes = '' THEN 'System Auto-Closed at End of Day'
                            WHEN actual_notes LIKE '%System Auto-Closed%' THEN actual_notes
                            ELSE actual_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id
      AND experience_schedule_id = ANY(p_schedule_ids)
      AND status NOT IN ('Completed', 'Cancelled', 'Finished');
END;
$$;

-- =========================================================================
-- 4. EVENTS
-- =========================================================================

DROP PROCEDURE IF EXISTS public.sp_update_event(UUID, UUID, VARCHAR, TEXT, UUID, DATE, TIME, TIME, VARCHAR, UUID, VARCHAR, VARCHAR, UUID, BOOLEAN, TEXT[], TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_event(UUID, UUID, VARCHAR, TEXT, UUID, TIMESTAMPTZ, INTERVAL, INTERVAL, VARCHAR, UUID, VARCHAR, VARCHAR, UUID, BOOLEAN, TEXT[], TIMESTAMPTZ);
CREATE OR REPLACE PROCEDURE public.sp_update_event(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_event_type_id UUID,
    p_event_date TIMESTAMPTZ,
    p_start_time INTERVAL,
    p_end_time INTERVAL,
    p_location VARCHAR,
    p_organizer_id UUID,
    p_status VARCHAR,
    p_scope VARCHAR,
    p_branch_id UUID,
    p_is_organization_wide BOOLEAN,
    p_photo_urls TEXT[],
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE events 
    SET title = p_title, description = p_description, event_type_id = p_event_type_id, 
        event_date = p_event_date::DATE, start_time = p_start_time::time, end_time = p_end_time::time, 
        location = p_location, organizer_id = p_organizer_id, status = p_status, 
        scope = p_scope, branch_id = p_branch_id, is_organization_wide = p_is_organization_wide, 
        photo_urls = p_photo_urls, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_event(UUID, UUID, VARCHAR, TEXT, UUID, DATE, TIME, TIME, VARCHAR, UUID, VARCHAR, VARCHAR, UUID, BOOLEAN, TEXT[], TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_event(UUID, UUID, VARCHAR, TEXT, UUID, TIMESTAMPTZ, INTERVAL, INTERVAL, VARCHAR, UUID, VARCHAR, VARCHAR, UUID, BOOLEAN, TEXT[], TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_event(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_event_type_id UUID,
    p_event_date TIMESTAMPTZ,
    p_start_time INTERVAL,
    p_end_time INTERVAL,
    p_location VARCHAR,
    p_organizer_id UUID,
    p_status VARCHAR,
    p_scope VARCHAR,
    p_branch_id UUID,
    p_is_organization_wide BOOLEAN,
    p_photo_urls TEXT[],
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO events (
        id, tenant_id, title, description, event_type_id, event_date, start_time, end_time,
        location, organizer_id, status, scope, branch_id, is_organization_wide, photo_urls, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_title, p_description, p_event_type_id, p_event_date::DATE, p_start_time::time, p_end_time::time,
        p_location, p_organizer_id, p_status, p_scope, p_branch_id, p_is_organization_wide, p_photo_urls, p_created_at, p_updated_at
    );
    RETURN p_id;
END;
$$;

-- =========================================================================
-- 5. CHILDREN & TEACHERS (DateOfBirth Compatibility)
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_create_child(UUID, UUID, VARCHAR, VARCHAR, DATE, UUID, VARCHAR, INT, VARCHAR, JSONB, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_child(UUID, UUID, VARCHAR, VARCHAR, DATE, UUID, TEXT, INT, VARCHAR, TEXT, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_child(UUID, UUID, VARCHAR, VARCHAR, TIMESTAMPTZ, UUID, TEXT, INT, VARCHAR, TEXT, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_child(
    p_id UUID,
    p_tenant_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth TIMESTAMPTZ,
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
        p_id, p_tenant_id, p_first_name, p_last_name, p_date_of_birth::DATE, p_classroom_id, p_photo_url,
        p_profile_completion_percentage, p_enrollment_status, p_draft_data, p_is_active, p_created_at, p_updated_at
    )
    RETURNING children.id, children.tenant_id, children.first_name, children.last_name, children.date_of_birth,
              children.classroom_id, children.photo_url, children.profile_completion_percentage,
              children.enrollment_status, children.draft_data, children.is_active, children.created_at, children.updated_at;
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_update_child(UUID, UUID, VARCHAR, VARCHAR, DATE, UUID, VARCHAR, INT, VARCHAR, JSONB, BOOLEAN, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_child(UUID, UUID, VARCHAR, VARCHAR, DATE, UUID, TEXT, INT, VARCHAR, TEXT, BOOLEAN, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_child(UUID, UUID, VARCHAR, VARCHAR, TIMESTAMPTZ, UUID, TEXT, INT, VARCHAR, TEXT, BOOLEAN, TIMESTAMPTZ);
CREATE OR REPLACE PROCEDURE public.sp_update_child(
    p_id UUID,
    p_tenant_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth TIMESTAMPTZ,
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
        date_of_birth = p_date_of_birth::DATE,
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

DROP FUNCTION IF EXISTS public.fn_create_teacher(UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, DATE, VARCHAR, VARCHAR, TEXT, VARCHAR, TEXT, VARCHAR, JSONB, INT, TIMESTAMPTZ, TIMESTAMPTZ);
DROP FUNCTION IF EXISTS public.fn_create_teacher(UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, TIMESTAMPTZ, VARCHAR, VARCHAR, TEXT, VARCHAR, TEXT, VARCHAR, JSONB, INT, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_teacher(
    p_id UUID,
    p_tenant_id UUID,
    p_person_id UUID,
    p_user_id UUID,
    p_employee_number VARCHAR,
    p_first_name VARCHAR,
    p_middle_name VARCHAR,
    p_last_name VARCHAR,
    p_preferred_name VARCHAR,
    p_display_name VARCHAR,
    p_profile_photo_url TEXT,
    p_date_of_birth TIMESTAMPTZ,
    p_gender VARCHAR,
    p_preferred_language VARCHAR,
    p_other_languages TEXT,
    p_pronouns VARCHAR,
    p_bio TEXT,
    p_status VARCHAR,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    person_id UUID,
    user_id UUID,
    employee_number VARCHAR,
    first_name VARCHAR,
    middle_name VARCHAR,
    last_name VARCHAR,
    preferred_name VARCHAR,
    display_name VARCHAR,
    profile_photo_url TEXT,
    date_of_birth DATE,
    gender VARCHAR,
    preferred_language VARCHAR,
    other_languages TEXT,
    pronouns VARCHAR,
    bio TEXT,
    status VARCHAR,
    draft_data JSONB,
    profile_completion_percentage INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO teachers (
        id, tenant_id, person_id, user_id, employee_number, first_name, middle_name, last_name, 
        preferred_name, display_name, profile_photo_url, date_of_birth, gender, preferred_language, 
        other_languages, pronouns, bio, status, draft_data, profile_completion_percentage, 
        created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_person_id, p_user_id, p_employee_number, p_first_name, p_middle_name, p_last_name, 
        p_preferred_name, p_display_name, p_profile_photo_url, p_date_of_birth::DATE, p_gender, p_preferred_language, 
        p_other_languages, p_pronouns, p_bio, p_status, p_draft_data, p_profile_completion_percentage, 
        p_created_at, p_updated_at
    )
    RETURNING teachers.id, teachers.tenant_id, teachers.person_id, teachers.user_id, teachers.employee_number,
              teachers.first_name, teachers.middle_name, teachers.last_name, teachers.preferred_name,
              teachers.display_name, teachers.profile_photo_url, teachers.date_of_birth, teachers.gender,
              teachers.preferred_language, teachers.other_languages, teachers.pronouns, teachers.bio,
              teachers.status, teachers.draft_data, teachers.profile_completion_percentage,
              teachers.created_at, teachers.updated_at;
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_update_teacher(UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, DATE, VARCHAR, VARCHAR, TEXT, VARCHAR, TEXT, VARCHAR, JSONB, INT, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_update_teacher(UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, TIMESTAMPTZ, VARCHAR, VARCHAR, TEXT, VARCHAR, TEXT, VARCHAR, JSONB, INT, TIMESTAMPTZ);
CREATE OR REPLACE PROCEDURE public.sp_update_teacher(
    p_id UUID,
    p_tenant_id UUID,
    p_person_id UUID,
    p_user_id UUID,
    p_employee_number VARCHAR,
    p_first_name VARCHAR,
    p_middle_name VARCHAR,
    p_last_name VARCHAR,
    p_preferred_name VARCHAR,
    p_display_name VARCHAR,
    p_profile_photo_url TEXT,
    p_date_of_birth TIMESTAMPTZ,
    p_gender VARCHAR,
    p_preferred_language VARCHAR,
    p_other_languages TEXT,
    p_pronouns VARCHAR,
    p_bio TEXT,
    p_status VARCHAR,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teachers
    SET person_id = p_person_id,
        user_id = p_user_id,
        employee_number = p_employee_number,
        first_name = p_first_name,
        middle_name = p_middle_name,
        last_name = p_last_name,
        preferred_name = p_preferred_name,
        display_name = p_display_name,
        profile_photo_url = p_profile_photo_url,
        date_of_birth = p_date_of_birth::DATE,
        gender = p_gender,
        preferred_language = p_preferred_language,
        other_languages = p_other_languages,
        pronouns = p_pronouns,
        bio = p_bio,
        status = p_status,
        draft_data = p_draft_data,
        profile_completion_percentage = p_profile_completion_percentage,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;
