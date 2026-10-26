-- Migration: 063_learning_and_experiences_procedures.sql
-- Description: Stored procedures and functions for Learning, Experiences, Curriculum, Memories, and Events.

-- =========================================================================
-- 1. EXPERIENCES & VERSIONS
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_experience_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    title VARCHAR,
    description TEXT,
    status VARCHAR,
    dna_payload JSONB,
    created_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    average_stars NUMERIC
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload,
           e.created_by_user_id, e.created_at, e.updated_at,
           COALESCE(AVG(ef.stars), 0)::NUMERIC AS average_stars
    FROM experiences e
    LEFT JOIN experience_schedules es ON e.id = es.experience_id
    LEFT JOIN experience_feedback ef ON es.id = ef.experience_schedule_id
    WHERE e.id = p_id AND e.tenant_id = p_tenant_id
    GROUP BY e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_experiences(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_experiences(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    title VARCHAR,
    description TEXT,
    status VARCHAR,
    dna_payload JSONB,
    created_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    average_stars NUMERIC
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload,
           e.created_by_user_id, e.created_at, e.updated_at,
           COALESCE(AVG(ef.stars), 0)::NUMERIC AS average_stars
    FROM experiences e
    LEFT JOIN experience_schedules es ON e.id = es.experience_id
    LEFT JOIN experience_feedback ef ON es.id = ef.experience_schedule_id
    WHERE e.tenant_id = p_tenant_id AND e.status != 'Archived'
      AND (p_organization_id IS NULL OR e.organization_id = p_organization_id)
    GROUP BY e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at
    ORDER BY e.created_at DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_experience(UUID, UUID, UUID, VARCHAR, TEXT, VARCHAR, JSONB, UUID, TIMESTAMPTZ, TIMESTAMPTZ, UUID);
CREATE OR REPLACE FUNCTION public.fn_create_experience(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_status VARCHAR,
    p_dna_payload JSONB,
    p_created_by_user_id UUID,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ,
    p_version_id UUID DEFAULT gen_random_uuid()
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    title VARCHAR,
    description TEXT,
    status VARCHAR,
    dna_payload JSONB,
    created_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO experiences (id, tenant_id, organization_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_organization_id, p_title, p_description, p_status, p_dna_payload, p_created_by_user_id, p_created_at, p_updated_at);

    INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
    VALUES (p_version_id, p_id, 1, p_title, p_description, p_dna_payload, 'Initial Creation', p_created_by_user_id, p_created_at);

    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at
    FROM experiences e
    WHERE e.id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_experience(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_status VARCHAR,
    p_dna_payload JSONB,
    p_updated_at TIMESTAMPTZ,
    p_version_id UUID,
    p_change_reason TEXT,
    p_modified_by_user_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_next_version INT;
BEGIN
    SELECT COALESCE(MAX(version_number), 0) + 1 INTO v_next_version
    FROM experience_versions
    WHERE experience_id = p_id;

    UPDATE experiences
    SET title = p_title,
        description = p_description,
        status = p_status,
        dna_payload = p_dna_payload,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;

    INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
    VALUES (p_version_id, p_id, v_next_version, p_title, p_description, p_dna_payload, p_change_reason, p_modified_by_user_id, p_updated_at);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_experience(
    p_tenant_id UUID,
    p_id UUID,
    p_status VARCHAR,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experiences
    SET status = p_status, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

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
    change_reason TEXT,
    modified_by_user_id UUID,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.experience_id, ev.version_number, ev.title, ev.description, ev.dna_payload, ev.change_reason, ev.modified_by_user_id, ev.created_at
    FROM experience_versions ev
    WHERE ev.experience_id = p_experience_id
    ORDER BY ev.version_number DESC;
END;
$$;

-- =========================================================================
-- 2. EXPERIENCE SCHEDULES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_experience_schedules_by_classroom_and_date_range(UUID, UUID, DATE, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_start_date DATE,
    p_end_date DATE
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
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.tenant_id = p_tenant_id AND es.classroom_id = p_classroom_id
      AND es.scheduled_date >= p_start_date AND es.scheduled_date <= p_end_date
    ORDER BY es.scheduled_date ASC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_schedules_by_date(UUID, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_experience_schedules_by_date(
    p_tenant_id UUID,
    p_date DATE
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
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.tenant_id = p_tenant_id AND es.scheduled_date = p_date
    ORDER BY es.planned_start_time ASC, es.time_of_day ASC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_filtered_experience_schedules(UUID, DATE, UUID, UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_filtered_experience_schedules(
    p_tenant_id UUID,
    p_date DATE,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id, s.tenant_id, s.experience_id, 
           s.classroom_id, s.scheduled_date, 
           s.time_of_day, s.status, s.planned_start_time, 
           s.planned_end_time, s.preparation_notes
    FROM experience_schedules s
    JOIN classrooms c ON s.classroom_id = c.id
    JOIN branches b ON c.branch_id = b.id
    WHERE s.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR b.organization_id = p_organization_id)
      AND s.scheduled_date = p_date
      AND (p_classroom_id IS NULL OR s.classroom_id = p_classroom_id)
      AND (p_branch_id IS NULL OR c.branch_id = p_branch_id)
    ORDER BY s.planned_start_time ASC NULLS LAST, s.time_of_day ASC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_schedule_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_schedule_by_id(
    p_tenant_id UUID,
    p_id UUID
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
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.id = p_id AND es.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_experience_schedule(UUID, UUID, UUID, UUID, DATE, VARCHAR, VARCHAR, UUID, TIME, TIME, TEXT, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_experience_schedule(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_id UUID,
    p_classroom_id UUID,
    p_scheduled_date DATE,
    p_time_of_day VARCHAR,
    p_status VARCHAR,
    p_primary_teacher_id UUID,
    p_planned_start_time TIME,
    p_planned_end_time TIME,
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
        p_id, p_tenant_id, p_experience_id, p_classroom_id, p_scheduled_date, p_time_of_day, p_status,
        p_primary_teacher_id, p_planned_start_time, p_planned_end_time, p_preparation_notes, p_created_at, p_updated_at
    )
    RETURNING experience_schedules.id, experience_schedules.tenant_id, experience_schedules.experience_id,
              experience_schedules.classroom_id, experience_schedules.scheduled_date, experience_schedules.time_of_day,
              experience_schedules.status, experience_schedules.primary_teacher_id, experience_schedules.planned_start_time,
              experience_schedules.planned_end_time, experience_schedules.preparation_notes,
              experience_schedules.created_at, experience_schedules.updated_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_experience_schedule(
    p_id UUID,
    p_tenant_id UUID,
    p_scheduled_date DATE,
    p_time_of_day VARCHAR,
    p_status VARCHAR,
    p_primary_teacher_id UUID,
    p_planned_start_time TIME,
    p_planned_end_time TIME,
    p_preparation_notes TEXT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_schedules
    SET scheduled_date = p_scheduled_date,
        time_of_day = p_time_of_day,
        status = p_status,
        primary_teacher_id = p_primary_teacher_id,
        planned_start_time = p_planned_start_time,
        planned_end_time = p_planned_end_time,
        preparation_notes = p_preparation_notes,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_experience_schedule(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM experience_schedules WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 3. EXPERIENCE SESSIONS & PARTICIPATIONS
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_experience_session_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_session_by_id(
    p_tenant_id UUID,
    p_session_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    classroom_id UUID,
    session_date DATE,
    start_time TIME,
    end_time TIME,
    status VARCHAR,
    primary_teacher_id UUID,
    actual_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.id = p_session_id AND es.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_sessions_for_classroom(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_sessions_for_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    classroom_id UUID,
    session_date DATE,
    start_time TIME,
    end_time TIME,
    status VARCHAR,
    primary_teacher_id UUID,
    actual_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.classroom_id = p_classroom_id AND es.tenant_id = p_tenant_id
    ORDER BY es.session_date DESC, es.start_time DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_session_by_schedule_id(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_session_by_schedule_id(
    p_schedule_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    classroom_id UUID,
    session_date DATE,
    start_time TIME,
    end_time TIME,
    status VARCHAR,
    primary_teacher_id UUID,
    actual_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.experience_schedule_id = p_schedule_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_create_experience_session(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_id UUID,
    p_experience_schedule_id UUID,
    p_classroom_id UUID,
    p_session_date DATE,
    p_start_time TIME,
    p_end_time TIME,
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
        p_session_date, p_start_time, p_end_time, p_status, p_primary_teacher_id, p_actual_notes
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_experience_session(
    p_id UUID,
    p_tenant_id UUID,
    p_status VARCHAR,
    p_actual_notes TEXT,
    p_start_time TIME,
    p_end_time TIME
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_sessions SET
        status = p_status,
        actual_notes = p_actual_notes,
        start_time = p_start_time,
        end_time = p_end_time,
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_participations_for_session(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_participations_for_session(
    p_session_id UUID
)
RETURNS TABLE (
    id UUID,
    experience_session_id UUID,
    child_id UUID,
    participation_status VARCHAR,
    participation_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ep.id, ep.experience_session_id, 
           ep.child_id, ep.participation_status, 
           ep.participation_notes, 
           ep.created_at, ep.updated_at
    FROM experience_participations ep
    WHERE ep.experience_session_id = p_session_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_participations_for_session(
    p_session_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM experience_participations WHERE experience_session_id = p_session_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_experience_participation(
    p_id UUID,
    p_experience_session_id UUID,
    p_child_id UUID,
    p_participation_status VARCHAR,
    p_participation_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO experience_participations (
        id, experience_session_id, child_id, participation_status, participation_notes
    ) VALUES (
        p_id, p_experience_session_id, p_child_id, p_participation_status, p_participation_notes
    );
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_logged_child_ids_for_session(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_logged_child_ids_for_session(
    p_tenant_id UUID,
    p_session_id UUID
)
RETURNS TABLE (
    child_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cje.child_id
    FROM child_journey_entries cje
    WHERE cje.tenant_id = p_tenant_id AND cje.experience_session_id = p_session_id;
END;
$$;

-- =========================================================================
-- 4. HARVEST & FEEDBACK
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_harvest_monthly_metrics(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_harvest_monthly_metrics(
    p_tenant_id UUID
)
RETURNS TABLE (
    total_experiences INT,
    total_feedbacks INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(*)::INT 
         FROM experience_schedules es 
         WHERE es.tenant_id = p_tenant_id 
           AND EXTRACT(MONTH FROM es.scheduled_date) = EXTRACT(MONTH FROM CURRENT_DATE) 
           AND EXTRACT(YEAR FROM es.scheduled_date) = EXTRACT(YEAR FROM CURRENT_DATE)) AS total_experiences,
        (SELECT COUNT(*)::INT 
         FROM experience_feedback f
         JOIN experience_schedules s ON f.experience_schedule_id = s.id
         WHERE f.tenant_id = p_tenant_id 
           AND EXTRACT(MONTH FROM s.scheduled_date) = EXTRACT(MONTH FROM CURRENT_DATE) 
           AND EXTRACT(YEAR FROM s.scheduled_date) = EXTRACT(YEAR FROM CURRENT_DATE)) AS total_feedbacks;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_harvest_performance_items(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_harvest_performance_items(
    p_tenant_id UUID
)
RETURNS TABLE (
    experience_id UUID,
    title VARCHAR,
    subtitle TEXT,
    uses INT,
    response TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.id AS experience_id,
        e.title,
        ('Created by ' || COALESCE((SELECT u.first_name FROM users u WHERE u.id = e.created_by_user_id), 'Educator'))::TEXT AS subtitle,
        (SELECT COUNT(*)::INT FROM experience_schedules es WHERE es.experience_id = e.id) AS uses,
        ('💬 ' || (SELECT COUNT(1) FROM experience_feedback f JOIN experience_schedules s ON f.experience_schedule_id = s.id WHERE s.experience_id = e.id))::TEXT AS response
    FROM experiences e
    WHERE e.tenant_id = p_tenant_id AND e.status NOT IN ('Idea', 'Draft', 'Archived')
    ORDER BY uses DESC
    LIMIT 10;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_feedback_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_feedback_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_schedule_id UUID,
    experience_id UUID,
    teacher_id UUID,
    stars INT,
    rating INT,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.id = p_id AND ef.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_feedback_by_schedule_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_experience_feedback_by_schedule_id(
    p_tenant_id UUID,
    p_schedule_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    experience_schedule_id UUID,
    experience_id UUID,
    teacher_id UUID,
    stars INT,
    rating INT,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.experience_schedule_id = p_schedule_id AND ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;

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
    rating INT,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
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
    rating INT,
    notes TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_experience_feedback(
    p_id UUID,
    p_tenant_id UUID,
    p_experience_schedule_id UUID,
    p_experience_id UUID,
    p_teacher_id UUID,
    p_stars INT,
    p_rating INT,
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

CREATE OR REPLACE PROCEDURE public.sp_update_experience_feedback(
    p_id UUID,
    p_tenant_id UUID,
    p_stars INT,
    p_rating INT,
    p_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_feedback
    SET stars = p_stars, rating = p_rating, notes = p_notes
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_experience_feedback(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM experience_feedback WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 5. CHILD STORIES & INTERESTS
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_child_stories_by_child_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_child_stories_by_child_id(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    child_id UUID,
    teacher_id UUID,
    experience_schedule_id UUID,
    content TEXT,
    status VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cs.id, cs.tenant_id, cs.child_id, cs.teacher_id,
           cs.experience_schedule_id, cs.content,
           cs.status, cs.created_at, cs.updated_at
    FROM child_stories cs
    WHERE cs.child_id = p_child_id AND cs.tenant_id = p_tenant_id
    ORDER BY cs.created_at DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_child_interests_by_child_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_child_interests_by_child_id(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    child_id UUID,
    name VARCHAR,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ci.id, ci.tenant_id, ci.child_id, ci.name, ci.created_at
    FROM child_interests ci
    WHERE ci.child_id = p_child_id AND ci.tenant_id = p_tenant_id
    ORDER BY ci.created_at DESC;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_child_story(
    p_id UUID,
    p_tenant_id UUID,
    p_child_id UUID,
    p_teacher_id UUID,
    p_experience_schedule_id UUID,
    p_content TEXT,
    p_status VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_stories (id, tenant_id, child_id, teacher_id, experience_schedule_id, content, status, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_child_id, p_teacher_id, p_experience_schedule_id, p_content, p_status, p_created_at, p_updated_at);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_child_interest(
    p_id UUID,
    p_tenant_id UUID,
    p_child_id UUID,
    p_name VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_interests (id, tenant_id, child_id, name, created_at)
    VALUES (p_id, p_tenant_id, p_child_id, p_name, p_created_at);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_child_story_status(
    p_id UUID,
    p_tenant_id UUID,
    p_status VARCHAR,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_stories
    SET status = p_status, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 6. CHILD JOURNEY, DEVELOPMENTS, EVIDENCE & MILESTONES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_journey_entry_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_journey_entry_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    child_id UUID,
    enrollment_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    experience_session_id UUID,
    experience_participation_id UUID,
    little_moment_id UUID,
    milestone_id UUID,
    journey_type VARCHAR,
    title VARCHAR,
    observation TEXT,
    child_voice TEXT,
    teacher_reflection TEXT,
    observation_date TIMESTAMPTZ,
    participation_type VARCHAR,
    is_milestone BOOLEAN,
    is_parent_visible BOOLEAN,
    parent_visibility_status VARCHAR,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.tenant_id, cje.child_id, cje.enrollment_id, cje.classroom_id,
           cje.teacher_id, cje.experience_id, cje.experience_schedule_id,
           cje.experience_session_id, cje.experience_participation_id,
           cje.little_moment_id, cje.milestone_id, cje.journey_type,
           cje.title, cje.observation, cje.child_voice, cje.teacher_reflection,
           cje.observation_date, cje.participation_type, cje.is_milestone,
           cje.is_parent_visible, cje.parent_visibility_status,
           cje.created_at, cje.created_by, cje.updated_at, cje.updated_by, cje.version
    FROM child_journey_entries cje
    WHERE cje.id = p_id AND cje.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_journey_entries_for_child(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_journey_entries_for_child(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    child_id UUID,
    enrollment_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    experience_session_id UUID,
    experience_participation_id UUID,
    little_moment_id UUID,
    milestone_id UUID,
    journey_type VARCHAR,
    title VARCHAR,
    observation TEXT,
    child_voice TEXT,
    teacher_reflection TEXT,
    observation_date TIMESTAMPTZ,
    participation_type VARCHAR,
    is_milestone BOOLEAN,
    is_parent_visible BOOLEAN,
    parent_visibility_status VARCHAR,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.tenant_id, cje.child_id, cje.enrollment_id, cje.classroom_id,
           cje.teacher_id, cje.experience_id, cje.experience_schedule_id,
           cje.experience_session_id, cje.experience_participation_id,
           cje.little_moment_id, cje.milestone_id, cje.journey_type,
           cje.title, cje.observation, cje.child_voice, cje.teacher_reflection,
           cje.observation_date, cje.participation_type, cje.is_milestone,
           cje.is_parent_visible, cje.parent_visibility_status,
           cje.created_at, cje.created_by, cje.updated_at, cje.updated_by, cje.version
    FROM child_journey_entries cje
    WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    ORDER BY cje.observation_date DESC;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    p_id UUID,
    p_tenant_id UUID,
    p_child_id UUID,
    p_enrollment_id UUID,
    p_classroom_id UUID,
    p_teacher_id UUID,
    p_experience_id UUID,
    p_experience_schedule_id UUID,
    p_experience_session_id UUID,
    p_experience_participation_id UUID,
    p_little_moment_id UUID,
    p_milestone_id UUID,
    p_journey_type VARCHAR,
    p_title VARCHAR,
    p_observation TEXT,
    p_child_voice TEXT,
    p_teacher_reflection TEXT,
    p_observation_date TIMESTAMPTZ,
    p_participation_type VARCHAR,
    p_is_milestone BOOLEAN,
    p_is_parent_visible BOOLEAN,
    p_parent_visibility_status VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_created_by UUID,
    p_updated_at TIMESTAMPTZ,
    p_updated_by UUID,
    p_version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_entries (
        id, tenant_id, child_id, enrollment_id, classroom_id, teacher_id, 
        experience_id, experience_schedule_id, experience_session_id, experience_participation_id, 
        little_moment_id, milestone_id, 
        journey_type, title, observation, child_voice, teacher_reflection, 
        observation_date, participation_type, is_milestone, is_parent_visible, 
        parent_visibility_status, created_at, created_by, updated_at, updated_by, version
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, 
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_journey_entry(
    p_id UUID,
    p_tenant_id UUID,
    p_journey_type VARCHAR,
    p_title VARCHAR,
    p_observation TEXT,
    p_child_voice TEXT,
    p_teacher_reflection TEXT,
    p_observation_date TIMESTAMPTZ,
    p_participation_type VARCHAR,
    p_is_milestone BOOLEAN,
    p_is_parent_visible BOOLEAN,
    p_parent_visibility_status VARCHAR,
    p_updated_at TIMESTAMPTZ,
    p_updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_journey_entries SET
        journey_type = p_journey_type,
        title = p_title,
        observation = p_observation,
        child_voice = p_child_voice,
        teacher_reflection = p_teacher_reflection,
        observation_date = p_observation_date,
        participation_type = p_participation_type,
        is_milestone = p_is_milestone,
        is_parent_visible = p_is_parent_visible,
        parent_visibility_status = p_parent_visibility_status,
        updated_at = p_updated_at,
        updated_by = p_updated_by,
        version = version + 1
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_journey_entry(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM child_journey_entries WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_development_areas(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_development_areas(
    p_tenant_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    code VARCHAR,
    description TEXT,
    icon VARCHAR,
    color_hex VARCHAR,
    display_order INT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT da.id, da.tenant_id, da.name, da.code, da.description, da.icon, da.color_hex,
           da.display_order, da.is_active, da.created_at, da.updated_at
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_journey_developments(
    p_journey_entry_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM child_journey_developments WHERE journey_entry_id = p_journey_entry_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_journey_development(
    p_journey_entry_id UUID,
    p_development_area_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_developments (journey_entry_id, development_area_id) 
    VALUES (p_journey_entry_id, p_development_area_id);
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_development_areas_for_journey_entry(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_development_areas_for_journey_entry(
    p_journey_entry_id UUID
)
RETURNS TABLE (
    development_area_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cjd.development_area_id
    FROM child_journey_developments cjd
    WHERE cjd.journey_entry_id = p_journey_entry_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_create_journey_evidence(
    p_id UUID,
    p_journey_entry_id UUID,
    p_evidence_type VARCHAR,
    p_media_id UUID,
    p_text_content TEXT,
    p_captured_at TIMESTAMPTZ,
    p_captured_by UUID,
    p_visibility VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_evidence (
        id, journey_entry_id, evidence_type, media_id, text_content, 
        captured_at, captured_by, visibility, created_at
    ) VALUES (
        p_id, p_journey_entry_id, p_evidence_type, p_media_id, p_text_content, 
        p_captured_at, p_captured_by, p_visibility, p_created_at
    );
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_evidence_for_journey_entry(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_evidence_for_journey_entry(
    p_journey_entry_id UUID
)
RETURNS TABLE (
    id UUID,
    journey_entry_id UUID,
    evidence_type VARCHAR,
    media_id UUID,
    text_content TEXT,
    captured_at TIMESTAMPTZ,
    captured_by UUID,
    visibility VARCHAR,
    created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.journey_entry_id, cje.evidence_type, cje.media_id,
           cje.text_content, cje.captured_at, cje.captured_by, cje.visibility, cje.created_at
    FROM child_journey_evidence cje
    WHERE cje.journey_entry_id = p_journey_entry_id
    ORDER BY cje.created_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_create_milestone(
    p_id UUID,
    p_tenant_id UUID,
    p_child_id UUID,
    p_classroom_id UUID,
    p_teacher_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_milestone_date TIMESTAMPTZ,
    p_development_area_id UUID,
    p_evidence_summary TEXT,
    p_journey_entry_id UUID,
    p_parent_visible BOOLEAN,
    p_status VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_created_by UUID,
    p_updated_at TIMESTAMPTZ,
    p_updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_milestones (
        id, tenant_id, child_id, classroom_id, teacher_id, title, description, 
        milestone_date, development_area_id, evidence_summary, journey_entry_id, 
        parent_visible, status, created_at, created_by, updated_at, updated_by
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_classroom_id, p_teacher_id, p_title, p_description, 
        p_milestone_date, p_development_area_id, p_evidence_summary, p_journey_entry_id, 
        p_parent_visible, p_status, p_created_at, p_created_by, p_updated_at, p_updated_by
    );
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_milestones_for_child(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_milestones_for_child(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    child_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    title VARCHAR,
    description TEXT,
    milestone_date TIMESTAMPTZ,
    development_area_id UUID,
    evidence_summary TEXT,
    journey_entry_id UUID,
    parent_visible BOOLEAN,
    status VARCHAR,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cm.id, cm.tenant_id, cm.child_id, cm.classroom_id, cm.teacher_id,
           cm.title, cm.description, cm.milestone_date, cm.development_area_id,
           cm.evidence_summary, cm.journey_entry_id, cm.parent_visible, cm.status,
           cm.created_at, cm.created_by, cm.updated_at, cm.updated_by
    FROM child_milestones cm
    WHERE cm.tenant_id = p_tenant_id AND cm.child_id = p_child_id
    ORDER BY cm.milestone_date DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_resolve_teacher_id(UUID, UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_resolve_teacher_id(
    p_tenant_id UUID,
    p_teacher_or_user_id UUID,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
    v_found_id UUID;
    v_new_teacher_id UUID;
BEGIN
    -- 1. Direct teacher ID
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.id = p_teacher_or_user_id AND t.tenant_id = p_tenant_id
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 2. User ID linked to teacher
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.user_id = p_teacher_or_user_id AND t.tenant_id = p_tenant_id
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 3. Classroom teacher
    IF p_classroom_id IS NOT NULL THEN
        SELECT t.id INTO v_found_id
        FROM classroom_teachers ct
        JOIN teachers t ON ct.user_id = t.user_id AND ct.tenant_id = t.tenant_id
        WHERE ct.classroom_id = p_classroom_id AND ct.tenant_id = p_tenant_id
        ORDER BY ct.is_primary DESC
        LIMIT 1;

        IF v_found_id IS NOT NULL THEN
            RETURN v_found_id;
        END IF;
    END IF;

    -- 4. Any active teacher
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.tenant_id = p_tenant_id AND t.status != 'Inactive'
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 5. Auto-create if user exists
    v_new_teacher_id := gen_random_uuid();
    INSERT INTO teachers (
        id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at
    )
    SELECT v_new_teacher_id, p_tenant_id, u.id, COALESCE(u.first_name, 'Teacher'), COALESCE(u.last_name, 'Staff'), COALESCE(u.display_name, 'Lead Educator'), 'Active', NOW(), NOW()
    FROM users u
    WHERE u.id = p_teacher_or_user_id AND u.tenant_id = p_tenant_id
    ON CONFLICT (id) DO NOTHING;

    IF FOUND THEN
        RETURN v_new_teacher_id;
    END IF;

    RETURN p_teacher_or_user_id;
END;
$$;

-- =========================================================================
-- 7. CHILD DEVELOPMENT PORTFOLIO
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_latest_media_consent_document(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_latest_media_consent_document(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    status VARCHAR,
    document_title VARCHAR,
    signed_at TIMESTAMPTZ,
    withdrawn_at TIMESTAMPTZ,
    declined_at TIMESTAMPTZ,
    document_type VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.status::VARCHAR, d.document_title::VARCHAR,
           d.signed_at, d.withdrawn_at, d.declined_at,
           COALESCE(t.document_type, '')::VARCHAR AS document_type
    FROM child_consent_documents d
    LEFT JOIN consent_templates t ON d.consent_template_id = t.id
    WHERE d.tenant_id = p_tenant_id AND d.child_id = p_child_id
      AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
    ORDER BY d.created_at DESC
    LIMIT 1;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_consent_choices(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_consent_choices(
    p_tenant_id UUID,
    p_consent_document_id UUID
)
RETURNS TABLE (
    choice VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cc.choice::VARCHAR
    FROM consent_choices cc
    WHERE cc.tenant_id = p_tenant_id AND cc.consent_document_id = p_consent_document_id
      AND (cc.section_key ILIKE '%photo%' OR cc.section_key ILIKE '%media%');
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_child_profile(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_child_profile(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    photo_url TEXT,
    date_of_birth DATE,
    classroom_name VARCHAR,
    branch_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.first_name::VARCHAR, c.last_name::VARCHAR,
           c.photo_url, c.date_of_birth,
           cr.name::VARCHAR AS classroom_name, b.name::VARCHAR AS branch_name
    FROM children c
    LEFT JOIN classrooms cr ON c.classroom_id = cr.id
    LEFT JOIN branches b ON cr.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id AND c.id = p_child_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_development_areas(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_development_areas(
    p_tenant_id UUID
)
RETURNS TABLE (
    id UUID,
    name VARCHAR,
    icon VARCHAR,
    display_order INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT da.id, da.name::VARCHAR, da.icon::VARCHAR, da.display_order
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_journey_entries(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_journey_entries(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    observation_date TIMESTAMPTZ,
    journey_type VARCHAR,
    title VARCHAR,
    observation TEXT,
    child_voice TEXT,
    teacher_reflection TEXT,
    is_milestone BOOLEAN,
    is_parent_visible BOOLEAN,
    parent_visibility_status VARCHAR,
    teacher_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT je.id, je.observation_date, je.journey_type::VARCHAR,
           je.title::VARCHAR, je.observation, je.child_voice,
           je.teacher_reflection, je.is_milestone,
           je.is_parent_visible, je.parent_visibility_status::VARCHAR,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM child_journey_entries je
    LEFT JOIN teachers t ON je.teacher_id = t.id
    WHERE je.tenant_id = p_tenant_id AND je.child_id = p_child_id
    ORDER BY je.observation_date DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_entry_development_areas(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_entry_development_areas(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    journey_entry_id UUID,
    development_area_id UUID,
    name VARCHAR,
    icon VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT d.journey_entry_id, d.development_area_id,
           a.name::VARCHAR, a.icon::VARCHAR
    FROM child_journey_developments d
    JOIN development_areas a ON d.development_area_id = a.id
    WHERE d.journey_entry_id IN (
        SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    );
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_entry_evidence(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_entry_evidence(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    journey_entry_id UUID,
    text_content TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cje_ev.journey_entry_id, cje_ev.text_content
    FROM child_journey_evidence cje_ev
    WHERE cje_ev.journey_entry_id IN (
        SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    )
    ORDER BY cje_ev.created_at DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_milestones(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_milestones(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    milestone_date TIMESTAMPTZ,
    title VARCHAR,
    description TEXT,
    evidence_summary TEXT,
    parent_visible BOOLEAN,
    status VARCHAR,
    development_area_id UUID,
    teacher_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT m.id, m.milestone_date, m.title::VARCHAR,
           m.description, m.evidence_summary,
           m.parent_visible, m.status::VARCHAR,
           m.development_area_id,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM child_milestones m
    LEFT JOIN teachers t ON m.teacher_id = t.id
    WHERE m.tenant_id = p_tenant_id AND m.child_id = p_child_id
    ORDER BY m.milestone_date DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_portfolio_experience_participations(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_portfolio_experience_participations(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    session_date TIMESTAMPTZ,
    experience_title VARCHAR,
    notes TEXT,
    teacher_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT p.id,
           (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS session_date,
           e.title::VARCHAR AS experience_title,
           COALESCE(p.participation_notes, s.actual_notes) AS notes,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM experience_participations p
    JOIN experience_sessions s ON p.experience_session_id = s.id
    JOIN experiences e ON s.experience_id = e.id
    LEFT JOIN teachers t ON s.primary_teacher_id = t.id
    WHERE s.tenant_id = p_tenant_id AND p.child_id = p_child_id AND p.participation_status = 'Participated'
    ORDER BY s.session_date DESC;
END;
$$;

-- =========================================================================
-- 8. EVENTS & RSVPS
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_event_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_by_id(
    p_tenant_id UUID,
    p_id UUID
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    WHERE ev.id = p_id AND ev.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_events_for_tenant(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_events_for_tenant(
    p_tenant_id UUID
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    WHERE ev.tenant_id = p_tenant_id
    ORDER BY ev.event_date DESC;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_events_by_branch(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_events_by_branch(
    p_tenant_id UUID,
    p_branch_id UUID
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = ec.classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND (ev.scope = 'Organization' 
           OR ev.is_organization_wide = true 
           OR (ev.scope = 'Branch' AND ev.branch_id = p_branch_id)
           OR (c.branch_id = p_branch_id))
    ORDER BY ev.event_date DESC, ev.start_time ASC NULLS LAST;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_events_by_classroom(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_events_by_classroom(
    p_tenant_id UUID,
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = p_classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND (ev.scope = 'Organization' 
           OR ev.is_organization_wide = true 
           OR (ev.scope = 'Branch' AND ev.branch_id = c.branch_id)
           OR (ec.classroom_id = p_classroom_id))
    ORDER BY ev.event_date DESC, ev.start_time ASC NULLS LAST;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_events_for_date(UUID, DATE, UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_events_for_date(
    p_tenant_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
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
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = p_classroom_id
    LEFT JOIN classrooms ec_c ON ec_c.id = ec.classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND ev.event_date = p_date
      AND (
          (p_classroom_id IS NOT NULL AND (
              ev.scope = 'Organization' 
              OR ev.is_organization_wide = true 
              OR (ev.scope = 'Branch' AND ev.branch_id = c.branch_id) 
              OR (ec.classroom_id = p_classroom_id)
          ))
          OR
          (p_classroom_id IS NULL AND p_branch_id IS NOT NULL AND (
              ev.scope = 'Organization' 
              OR ev.is_organization_wide = true 
              OR (ev.scope = 'Branch' AND ev.branch_id = p_branch_id) 
              OR (ec_c.branch_id = p_branch_id)
          ))
          OR
          (p_classroom_id IS NULL AND p_branch_id IS NULL)
      )
    ORDER BY ev.start_time ASC NULLS LAST;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_event(UUID, UUID, VARCHAR, TEXT, UUID, DATE, TIME, TIME, VARCHAR, UUID, VARCHAR, VARCHAR, UUID, BOOLEAN, TEXT[], TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_event(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_event_type_id UUID,
    p_event_date DATE,
    p_start_time TIME,
    p_end_time TIME,
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
        p_id, p_tenant_id, p_title, p_description, p_event_type_id, p_event_date, p_start_time, p_end_time,
        p_location, p_organizer_id, p_status, p_scope, p_branch_id, p_is_organization_wide, p_photo_urls, p_created_at, p_updated_at
    );
    RETURN p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_event(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_event_type_id UUID,
    p_event_date DATE,
    p_start_time TIME,
    p_end_time TIME,
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
        event_date = p_event_date, start_time = p_start_time, end_time = p_end_time, 
        location = p_location, organizer_id = p_organizer_id, status = p_status, 
        scope = p_scope, branch_id = p_branch_id, is_organization_wide = p_is_organization_wide, 
        photo_urls = p_photo_urls, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_event_photos(
    p_id UUID,
    p_tenant_id UUID,
    p_photo_urls TEXT[],
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE events 
    SET photo_urls = p_photo_urls, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_event(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM events WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_types(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_types(
    p_tenant_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    description TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT et.id, et.tenant_id, et.name, et.description, et.is_active, et.created_at, et.updated_at
    FROM event_types et
    WHERE et.tenant_id = p_tenant_id AND et.is_active = true
    ORDER BY et.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_event_type(UUID, UUID, VARCHAR, TEXT, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_event_type(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_description TEXT,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO event_types (id, tenant_id, name, description, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_name, p_description, p_is_active, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_add_event_classroom(
    p_event_id UUID,
    p_classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO event_classrooms (event_id, classroom_id) 
    VALUES (p_event_id, p_classroom_id)
    ON CONFLICT DO NOTHING;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_remove_event_classroom(
    p_event_id UUID,
    p_classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM event_classrooms WHERE event_id = p_event_id AND classroom_id = p_classroom_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_classrooms(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_classrooms(
    p_event_id UUID
)
RETURNS TABLE (
    classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ec.classroom_id FROM event_classrooms ec WHERE ec.event_id = p_event_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_add_event_experience(
    p_id UUID,
    p_event_id UUID,
    p_experience_id UUID,
    p_experience_schedule_id UUID,
    p_experience_session_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO event_experiences (id, event_id, experience_id, experience_schedule_id, experience_session_id)
    VALUES (p_id, p_event_id, p_experience_id, p_experience_schedule_id, p_experience_session_id);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_remove_event_experience(
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM event_experiences WHERE id = p_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_experiences(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_experiences(
    p_event_id UUID
)
RETURNS TABLE (
    id UUID,
    event_id UUID,
    experience_id UUID,
    experience_schedule_id UUID,
    experience_session_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ee.id, ee.event_id, ee.experience_id, ee.experience_schedule_id, ee.experience_session_id
    FROM event_experiences ee
    WHERE ee.event_id = p_event_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_event_task(UUID, UUID, VARCHAR, UUID, TIMESTAMPTZ, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_event_task(
    p_id UUID,
    p_event_id UUID,
    p_description VARCHAR,
    p_owner_id UUID,
    p_due_date TIMESTAMPTZ,
    p_is_completed BOOLEAN,
    p_completed_at TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO event_tasks (id, event_id, description, owner_id, due_date, is_completed, completed_at, created_at, updated_at)
    VALUES (p_id, p_event_id, p_description, p_owner_id, p_due_date, p_is_completed, p_completed_at, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_event_task(
    p_id UUID,
    p_description VARCHAR,
    p_owner_id UUID,
    p_due_date TIMESTAMPTZ,
    p_is_completed BOOLEAN,
    p_completed_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE event_tasks 
    SET description = p_description, owner_id = p_owner_id, due_date = p_due_date, 
        is_completed = p_is_completed, completed_at = p_completed_at, updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_event_task(
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM event_tasks WHERE id = p_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_tasks(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_tasks(
    p_event_id UUID
)
RETURNS TABLE (
    id UUID,
    event_id UUID,
    description VARCHAR,
    owner_id UUID,
    due_date TIMESTAMPTZ,
    is_completed BOOLEAN,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT et.id, et.event_id, et.description, et.owner_id, et.due_date,
           et.is_completed, et.completed_at, et.created_at, et.updated_at
    FROM event_tasks et
    WHERE et.event_id = p_event_id
    ORDER BY et.created_at;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_event_rsvp(UUID, UUID, UUID, UUID, VARCHAR, TEXT, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_event_rsvp(
    p_id UUID,
    p_event_id UUID,
    p_child_id UUID,
    p_guardian_id UUID,
    p_status VARCHAR,
    p_notes TEXT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO event_rsvps (id, event_id, child_id, guardian_id, status, notes, created_at, updated_at)
    VALUES (p_id, p_event_id, p_child_id, p_guardian_id, p_status, p_notes, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_event_rsvp(
    p_id UUID,
    p_status VARCHAR,
    p_notes TEXT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE event_rsvps 
    SET status = p_status, notes = p_notes, updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_rsvps(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_rsvps(
    p_event_id UUID
)
RETURNS TABLE (
    id UUID,
    event_id UUID,
    child_id UUID,
    guardian_id UUID,
    status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT er.id, er.event_id, er.child_id, er.guardian_id, er.status, er.notes, er.created_at, er.updated_at
    FROM event_rsvps er
    WHERE er.event_id = p_event_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_event_rsvp(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_rsvp(
    p_event_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    event_id UUID,
    child_id UUID,
    guardian_id UUID,
    status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT er.id, er.event_id, er.child_id, er.guardian_id, er.status, er.notes, er.created_at, er.updated_at
    FROM event_rsvps er
    WHERE er.event_id = p_event_id AND er.child_id = p_child_id;
END;
$$;

-- =========================================================================
-- 9. MEMORIES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_child_memories(UUID, UUID, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ, VARCHAR, VARCHAR, INT, INT);
CREATE OR REPLACE FUNCTION public.fn_get_child_memories(
    p_tenant_id UUID,
    p_child_id UUID,
    p_is_parent BOOLEAN,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_category VARCHAR DEFAULT NULL,
    p_search_query VARCHAR DEFAULT NULL,
    p_limit INT DEFAULT 100,
    p_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    event_date TIMESTAMPTZ,
    event_type VARCHAR,
    category VARCHAR,
    title VARCHAR,
    description TEXT,
    source_id UUID,
    source_type VARCHAR,
    child_id UUID,
    child_name VARCHAR,
    classroom_id UUID,
    classroom_name VARCHAR,
    teacher_id UUID,
    teacher_name VARCHAR,
    experience_id UUID,
    experience_title VARCHAR,
    media_url TEXT,
    is_parent_visible BOOLEAN,
    status VARCHAR,
    category_icon VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        -- 1. Child Stories
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Observations'::VARCHAR AS category,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            NULL::UUID AS classroom_id,
            NULL::VARCHAR AS classroom_name,
            cs.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            es.experience_id,
            e.title::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            cs.status::VARCHAR,
            'bi-journal-text'::VARCHAR AS category_icon
        FROM child_stories cs
        JOIN children c ON cs.child_id = c.id
        LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        LEFT JOIN experiences e ON es.experience_id = e.id
        LEFT JOIN teachers t ON cs.teacher_id = t.id
        WHERE cs.tenant_id = p_tenant_id AND cs.child_id = p_child_id
          AND (p_is_parent = FALSE OR cs.status = 'Approved')

        UNION ALL
        
        -- 2. Child Journey Entries
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            (CASE WHEN je.is_milestone THEN 'Milestone' ELSE 'Observation' END)::VARCHAR AS event_type,
            (CASE WHEN je.is_milestone THEN 'Milestones' ELSE 'Observations' END)::VARCHAR AS category,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            je.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            je.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            je.experience_id,
            e.title::VARCHAR AS experience_title,
            ev.text_content AS media_url,
            je.is_parent_visible,
            je.parent_visibility_status::VARCHAR AS status,
            (CASE WHEN je.is_milestone THEN 'bi-trophy' ELSE 'bi-compass' END)::VARCHAR AS category_icon
        FROM child_journey_entries je
        JOIN children c ON je.child_id = c.id
        LEFT JOIN classrooms cr ON je.classroom_id = cr.id
        LEFT JOIN experiences e ON je.experience_id = e.id
        LEFT JOIN teachers t ON je.teacher_id = t.id
        LEFT JOIN LATERAL (
            SELECT cje_ev.text_content 
            FROM child_journey_evidence cje_ev
            WHERE cje_ev.journey_entry_id = je.id 
            ORDER BY cje_ev.created_at DESC 
            LIMIT 1
        ) ev ON TRUE
        WHERE je.tenant_id = p_tenant_id AND je.child_id = p_child_id
          AND (p_is_parent = FALSE OR (je.is_parent_visible = TRUE AND (je.parent_visibility_status = 'Approved' OR je.parent_visibility_status IS NULL)))

        UNION ALL

        -- 3. Experience Participations
        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Learning Experiences'::VARCHAR AS category,
            COALESCE(e.title, 'Experience Session')::VARCHAR AS title,
            COALESCE(p.participation_notes, s.actual_notes, 'Child participated in this learning experience.') AS description,
            p.id AS source_id,
            'experience_participations'::VARCHAR AS source_type,
            p.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            s.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            s.primary_teacher_id AS teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            s.experience_id,
            e.title::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            p.participation_status::VARCHAR AS status,
            'bi-palette'::VARCHAR AS category_icon
        FROM experience_participations p
        JOIN experience_sessions s ON p.experience_session_id = s.id
        JOIN children c ON p.child_id = c.id
        LEFT JOIN classrooms cr ON s.classroom_id = cr.id
        LEFT JOIN experiences e ON s.experience_id = e.id
        LEFT JOIN teachers t ON s.primary_teacher_id = t.id
        WHERE s.tenant_id = p_tenant_id 
          AND p.child_id = p_child_id 
          AND p.participation_status = 'Participated'

        UNION ALL

        -- 4. Child Milestones
        SELECT
            m.id, m.tenant_id, m.milestone_date AS event_date,
            'Milestone'::VARCHAR AS event_type,
            'Milestones'::VARCHAR AS category,
            m.title::VARCHAR,
            COALESCE(m.description, m.evidence_summary, 'Developmental milestone achieved.') AS description,
            m.id AS source_id,
            'child_milestones'::VARCHAR AS source_type,
            m.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            m.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            m.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            m.parent_visible AS is_parent_visible,
            m.status::VARCHAR,
            'bi-star-fill'::VARCHAR AS category_icon
        FROM child_milestones m
        JOIN children c ON m.child_id = c.id
        LEFT JOIN classrooms cr ON m.classroom_id = cr.id
        LEFT JOIN teachers t ON m.teacher_id = t.id
        WHERE m.tenant_id = p_tenant_id AND m.child_id = p_child_id
          AND (p_is_parent = FALSE OR m.parent_visible = TRUE)

        UNION ALL

        -- 5. Celebrations & Events
        SELECT
            r.id, ev.tenant_id,
            (ev.event_date + COALESCE(ev.start_time, '09:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Celebration'::VARCHAR AS event_type,
            'Celebrations'::VARCHAR AS category,
            ev.title::VARCHAR,
            COALESCE(ev.description, r.notes, 'Celebration & Daycare Event') AS description,
            ev.id AS source_id,
            'events'::VARCHAR AS source_type,
            r.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            NULL::UUID AS classroom_id,
            NULL::VARCHAR AS classroom_name,
            NULL::UUID AS teacher_id,
            'Daycare Staff'::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            r.status::VARCHAR,
            'bi-balloon-heart'::VARCHAR AS category_icon
        FROM event_rsvps r
        JOIN events ev ON r.event_id = ev.id
        JOIN children c ON r.child_id = c.id
        WHERE ev.tenant_id = p_tenant_id 
          AND r.child_id = p_child_id
          AND ev.status = 'Published'
          AND r.status IN ('Accepted', 'Attended', 'Confirmed')

        UNION ALL

        -- 6. Child Daily Care & Wellbeing
        SELECT
            ce.id, ce.tenant_id, ce.event_time AS event_date,
            'DailyCare'::VARCHAR AS event_type,
            'Daily Care'::VARCHAR AS category,
            ('Wellbeing Highlight: ' || ce.comfort_mood)::VARCHAR AS title,
            COALESCE(ce.notes, 'Positive daily care & comfort milestone.') AS description,
            ce.id AS source_id,
            'child_care_events'::VARCHAR AS source_type,
            ce.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            ce.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            ce.recorded_by_user_id AS teacher_id,
            ce.recorded_by_name::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            ce.parent_visible AS is_parent_visible,
            'Recorded'::VARCHAR AS status,
            'bi-sun'::VARCHAR AS category_icon
        FROM child_care_events ce
        JOIN children c ON ce.child_id = c.id
        LEFT JOIN classrooms cr ON ce.classroom_id = cr.id
        WHERE ce.tenant_id = p_tenant_id 
          AND ce.child_id = p_child_id
          AND ce.event_type = 'Comfort'
          AND ce.comfort_mood IN ('Happy', 'Settled')
          AND (p_is_parent = FALSE OR ce.parent_visible = TRUE)
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
      AND (p_category IS NULL OR p_category = 'All' OR combined.category = p_category)
      AND (p_search_query IS NULL OR p_search_query = '' OR combined.title ILIKE '%' || p_search_query || '%' OR combined.description ILIKE '%' || p_search_query || '%')
      AND (p_is_parent = FALSE OR combined.is_parent_visible = TRUE)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_classroom_memories(UUID, UUID, TIMESTAMPTZ, TIMESTAMPTZ, INT, INT);
CREATE OR REPLACE FUNCTION public.fn_get_classroom_memories(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_limit INT DEFAULT 100,
    p_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    event_date TIMESTAMPTZ,
    event_type VARCHAR,
    title VARCHAR,
    description TEXT,
    source_id UUID,
    source_type VARCHAR,
    child_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    experience_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.classroom_id = p_classroom_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.classroom_id = p_classroom_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND s.classroom_id = p_classroom_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_teacher_memories(UUID, UUID, TIMESTAMPTZ, TIMESTAMPTZ, INT, INT);
CREATE OR REPLACE FUNCTION public.fn_get_teacher_memories(
    p_tenant_id UUID,
    p_teacher_id UUID,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_limit INT DEFAULT 100,
    p_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    event_date TIMESTAMPTZ,
    event_type VARCHAR,
    title VARCHAR,
    description TEXT,
    source_id UUID,
    source_type VARCHAR,
    child_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    experience_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            NULL::UUID AS classroom_id,
            cs.teacher_id,
            es.experience_id
        FROM child_stories cs
        LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        WHERE cs.tenant_id = p_tenant_id AND cs.teacher_id = p_teacher_id

        UNION ALL
        
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.teacher_id = p_teacher_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.primary_teacher_id = p_teacher_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND f.teacher_id = p_teacher_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_experience_memories(UUID, UUID, TIMESTAMPTZ, TIMESTAMPTZ, INT, INT);
CREATE OR REPLACE FUNCTION public.fn_get_experience_memories(
    p_tenant_id UUID,
    p_experience_id UUID,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_limit INT DEFAULT 100,
    p_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    event_date TIMESTAMPTZ,
    event_type VARCHAR,
    title VARCHAR,
    description TEXT,
    source_id UUID,
    source_type VARCHAR,
    child_id UUID,
    classroom_id UUID,
    teacher_id UUID,
    experience_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            NULL::UUID AS classroom_id,
            cs.teacher_id,
            es.experience_id
        FROM child_stories cs
        JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        WHERE cs.tenant_id = p_tenant_id AND es.experience_id = p_experience_id

        UNION ALL
        
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.experience_id = p_experience_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.experience_id = p_experience_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND s.experience_id = p_experience_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;
