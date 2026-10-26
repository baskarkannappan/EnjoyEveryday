-- =========================================================================
-- Migration: 071_comprehensive_routine_fixes.sql
-- Description: Comprehensive fix for PostgreSQL stored routines:
--   1. Fix column type mismatches in experience_feedback and experience_versions
--   2. Add TIMESTAMP and TIMESTAMPTZ overloads for all routines taking DATE parameters
-- =========================================================================

-- -------------------------------------------------------------------------
-- 1. EXPERIENCE FEEDBACK ROUTINES (Fix rating type from INT to VARCHAR)
-- -------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_get_experience_feedback_by_experience_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_feedback_by_experience_id(
    p_tenant_id UUID,
    p_experience_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_schedule_id UUID,
    experience_id UUID,
    teacher_id UUID,
    stars INT,
    rating VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating::VARCHAR, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.experience_id = p_experience_id AND ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_experience_feedbacks(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_experience_feedbacks(
    p_tenant_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_schedule_id UUID,
    experience_id UUID,
    teacher_id UUID,
    stars INT,
    rating VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating::VARCHAR, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_insert_experience_feedback(UUID, UUID, UUID, UUID, UUID, INT, INT, TEXT, TIMESTAMPTZ);
DROP PROCEDURE IF EXISTS public.sp_insert_experience_feedback(UUID, UUID, UUID, UUID, UUID, INT, VARCHAR, TEXT, TIMESTAMPTZ);
CREATE OR REPLACE PROCEDURE public.sp_insert_experience_feedback(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_schedule_id UUID,
    p_experience_id UUID,
    p_teacher_id UUID,
    p_stars INT,
    p_rating VARCHAR,
    p_notes TEXT,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO experience_feedback (id, tenant_id, experience_schedule_id, experience_id, teacher_id, stars, rating, notes, created_at)
    VALUES (p_id, p_tenant_id, p_experience_schedule_id, p_experience_id, p_teacher_id, p_stars, p_rating, p_notes, p_created_at);
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_update_experience_feedback(UUID, UUID, INT, INT, TEXT);
DROP PROCEDURE IF EXISTS public.sp_update_experience_feedback(UUID, UUID, INT, VARCHAR, TEXT);
CREATE OR REPLACE PROCEDURE public.sp_update_experience_feedback(
    p_id UUID,
    p_tenant_id UUID,
    p_stars INT,
    p_rating VARCHAR,
    p_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_feedback
    SET stars = p_stars,
        rating = p_rating,
        notes = p_notes
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- -------------------------------------------------------------------------
-- 2. EXPERIENCE VERSIONS ROUTINE (Fix change_reason type to VARCHAR)
-- -------------------------------------------------------------------------
DROP FUNCTION IF EXISTS public.fn_get_experience_versions(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_versions(
    p_experience_id UUID
)
RETURNS TABLE (
    id UUID,
    experience_id UUID,
    version_number INT,
    title VARCHAR,
    description TEXT,
    dna_payload JSONB,
    change_reason VARCHAR,
    modified_by_user_id UUID,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.experience_id, ev.version_number, ev.title::VARCHAR, ev.description, ev.dna_payload, ev.change_reason::VARCHAR, ev.modified_by_user_id, ev.created_at
    FROM experience_versions ev
    WHERE ev.experience_id = p_experience_id
    ORDER BY ev.version_number DESC;
END;
$$;

-- -------------------------------------------------------------------------
-- 3. CHILD CARE EVENT ROUTINES (Date overloads)
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id, p_classroom_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id, p_classroom_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_branch_and_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_branch_and_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_branch_and_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_branch_and_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    event_type VARCHAR,
    event_time TIMESTAMPTZ,
    diaper_condition VARCHAR,
    skin_observation VARCHAR,
    meal_type VARCHAR,
    food_offered VARCHAR,
    amount_consumed VARCHAR,
    fluid_oz NUMERIC,
    rest_status VARCHAR,
    toileting_activity VARCHAR,
    hygiene_type VARCHAR,
    comfort_mood VARCHAR,
    notes TEXT,
    parent_visible BOOLEAN,
    recorded_by_user_id UUID,
    recorded_by_name VARCHAR,
    correction_reason TEXT,
    is_corrected BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_care_events_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_naps_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    start_time TIMESTAMPTZ,
    end_time TIMESTAMPTZ,
    duration_minutes INT,
    status VARCHAR,
    outcome VARCHAR,
    notes TEXT,
    started_by_user_id UUID,
    started_by_name VARCHAR,
    ended_by_user_id UUID,
    ended_by_name VARCHAR,
    parent_visible BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_naps_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_naps_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    start_time TIMESTAMPTZ,
    end_time TIMESTAMPTZ,
    duration_minutes INT,
    status VARCHAR,
    outcome VARCHAR,
    notes TEXT,
    started_by_user_id UUID,
    started_by_name VARCHAR,
    ended_by_user_id UUID,
    ended_by_name VARCHAR,
    parent_visible BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_naps_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;

-- -------------------------------------------------------------------------
-- 4. STAFF ROSTER ROUTINES (Date overloads)
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_counts(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    classroom_id UUID,
    present_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_attendance_counts(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_counts(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    classroom_id UUID,
    present_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_attendance_counts(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_summary(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    teacher_id UUID,
    status VARCHAR,
    clock_in_time TIME,
    clock_out_time TIME,
    teacher_name VARCHAR,
    classroom_id UUID
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_teacher_attendance_summary(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_summary(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    teacher_id UUID,
    status VARCHAR,
    clock_in_time TIME,
    clock_out_time TIME,
    teacher_name VARCHAR,
    classroom_id UUID
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_teacher_attendance_summary(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_staff_leaves(
    p_tenant_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    teacher_id UUID,
    leave_type VARCHAR,
    start_date DATE,
    end_date DATE,
    reason VARCHAR,
    status VARCHAR,
    coverage_status VARCHAR,
    substitute_teacher_id UUID,
    teacher_name VARCHAR,
    substitute_teacher_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_active_staff_leaves(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_staff_leaves(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    teacher_id UUID,
    leave_type VARCHAR,
    start_date DATE,
    end_date DATE,
    reason VARCHAR,
    status VARCHAR,
    coverage_status VARCHAR,
    substitute_teacher_id UUID,
    teacher_name VARCHAR,
    substitute_teacher_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_active_staff_leaves(p_tenant_id, p_date::DATE);
$$;

-- -------------------------------------------------------------------------
-- 5. EXPERIENCE SCHEDULES AND EVENTS ROUTINES (Date overloads)
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMP,
    p_end_date TIMESTAMP
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_date(
    p_tenant_id UUID,
    p_date TIMESTAMP
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_date(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_date(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_date(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_filtered_experience_schedules(
    p_tenant_id UUID,
    p_date TIMESTAMP,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    classroom_id UUID,
    scheduled_date DATE,
    time_of_day VARCHAR,
    status VARCHAR,
    planned_start_time TIME,
    planned_end_time TIME,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_filtered_experience_schedules(p_tenant_id, p_date::DATE, p_organization_id, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_filtered_experience_schedules(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    classroom_id UUID,
    scheduled_date DATE,
    time_of_day VARCHAR,
    status VARCHAR,
    planned_start_time TIME,
    planned_end_time TIME,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_filtered_experience_schedules(p_tenant_id, p_date::DATE, p_organization_id, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_events_for_date(
    p_tenant_id UUID,
    p_date TIMESTAMP,
    p_branch_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    title VARCHAR,
    description TEXT,
    event_type_id UUID,
    event_date DATE,
    start_time TIME,
    end_time TIME,
    location VARCHAR,
    organizer_id UUID,
    status VARCHAR,
    scope VARCHAR,
    branch_id UUID,
    is_organization_wide BOOLEAN,
    photo_urls TEXT[],
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_events_for_date(p_tenant_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_events_for_date(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ,
    p_branch_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    title VARCHAR,
    description TEXT,
    event_type_id UUID,
    event_date DATE,
    start_time TIME,
    end_time TIME,
    location VARCHAR,
    organizer_id UUID,
    status VARCHAR,
    scope VARCHAR,
    branch_id UUID,
    is_organization_wide BOOLEAN,
    photo_urls TEXT[],
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_events_for_date(p_tenant_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

-- -------------------------------------------------------------------------
-- 6. DAYCARE OPERATIONS BOARD ROUTINES (Date overloads)
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_board_classrooms(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMP,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    age_group VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    capacity INT,
    enrolled_children INT,
    present_children INT,
    expected_children INT,
    present_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_classrooms(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_classrooms(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    age_group VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    capacity INT,
    enrolled_children INT,
    present_children INT,
    expected_children INT,
    present_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_classrooms(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_child_attendance_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMP,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    checked_in INT,
    checked_out INT,
    absent INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_child_attendance_summary(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_child_attendance_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    checked_in INT,
    checked_out INT,
    absent INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_child_attendance_summary(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_staff_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    expected_staff INT,
    present_staff INT,
    absent_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_staff_summary(p_tenant_id, p_org_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_staff_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    expected_staff INT,
    present_staff INT,
    absent_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_staff_summary(p_tenant_id, p_org_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_experience_schedules(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMP,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display VARCHAR,
    scope_display VARCHAR,
    status VARCHAR,
    classroom_id UUID,
    classroom_name VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_experience_schedules(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_experience_schedules(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display VARCHAR,
    scope_display VARCHAR,
    status VARCHAR,
    classroom_id UUID,
    classroom_name VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_experience_schedules(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_events(
    p_tenant_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display TEXT,
    scope_display TEXT,
    status VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_events(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_events(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display TEXT,
    scope_display TEXT,
    status VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_events(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_teacher_children_care_status(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMP,
    p_start_of_day TIMESTAMPTZ,
    p_end_of_day TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    child_id UUID,
    full_name TEXT,
    photo_url TEXT,
    is_checked_in BOOLEAN,
    arrival_time TIME,
    care_events_count INT,
    last_recorded_care_type VARCHAR,
    last_recorded_time TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_teacher_children_care_status(
        p_tenant_id, p_org_id, p_date::DATE, p_start_of_day, p_end_of_day, p_branch_id, p_classroom_id
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_teacher_children_care_status(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date TIMESTAMPTZ,
    p_start_of_day TIMESTAMPTZ,
    p_end_of_day TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    child_id UUID,
    full_name TEXT,
    photo_url TEXT,
    is_checked_in BOOLEAN,
    arrival_time TIME,
    care_events_count INT,
    last_recorded_care_type VARCHAR,
    last_recorded_time TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_board_teacher_children_care_status(
        p_tenant_id, p_org_id, p_date::DATE, p_start_of_day, p_end_of_day, p_branch_id, p_classroom_id
    );
$$;

-- -------------------------------------------------------------------------
-- 7. ORGANIZATION INTELLIGENCE ROUTINES (Date overloads)
-- -------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_intelligence_curriculum_variety(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMP,
    p_end_date TIMESTAMP
)
RETURNS TABLE (
    title VARCHAR,
    usage_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_curriculum_variety(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_curriculum_variety(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ
)
RETURNS TABLE (
    title VARCHAR,
    usage_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_curriculum_variety(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_planning_gaps(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today TIMESTAMP,
    p_seven_days_out TIMESTAMP
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    branch_name VARCHAR,
    upcoming_plans_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_planning_gaps(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE, p_seven_days_out::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_planning_gaps(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today TIMESTAMPTZ,
    p_seven_days_out TIMESTAMPTZ
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    branch_name VARCHAR,
    upcoming_plans_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_planning_gaps(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE, p_seven_days_out::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_overdue_plans_count(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today TIMESTAMP
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT public.fn_get_intelligence_overdue_plans_count(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_overdue_plans_count(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today TIMESTAMPTZ
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT public.fn_get_intelligence_overdue_plans_count(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_data_quality(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMP,
    p_end_date TIMESTAMP
)
RETURNS TABLE (
    missing_departure_times_count INT,
    drafts_awaiting_review_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_data_quality(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_data_quality(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ
)
RETURNS TABLE (
    missing_departure_times_count INT,
    drafts_awaiting_review_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_intelligence_data_quality(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;
