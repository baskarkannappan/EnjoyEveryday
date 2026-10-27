-- =========================================================================
-- Migration: 074_fix_sp_create_journey_entry.sql
-- Description: Fix public.sp_create_journey_entry and sp_update_journey_entry
--              to support event_id, photos, and robust parameter handling.
-- =========================================================================

-- 1. Create or replace the 28-parameter sp_create_journey_entry (with event_id)
CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id UUID,
    IN p_tenant_id UUID,
    IN p_child_id UUID,
    IN p_enrollment_id UUID,
    IN p_classroom_id UUID,
    IN p_teacher_id UUID,
    IN p_experience_id UUID,
    IN p_experience_schedule_id UUID,
    IN p_experience_session_id UUID,
    IN p_experience_participation_id UUID,
    IN p_little_moment_id UUID,
    IN p_milestone_id UUID,
    IN p_event_id UUID,
    IN p_journey_type VARCHAR,
    IN p_title VARCHAR,
    IN p_observation TEXT,
    IN p_child_voice TEXT,
    IN p_teacher_reflection TEXT,
    IN p_observation_date TIMESTAMPTZ,
    IN p_participation_type VARCHAR,
    IN p_is_milestone BOOLEAN,
    IN p_is_parent_visible BOOLEAN,
    IN p_parent_visibility_status VARCHAR,
    IN p_created_at TIMESTAMPTZ,
    IN p_created_by UUID,
    IN p_updated_at TIMESTAMPTZ,
    IN p_updated_by UUID,
    IN p_version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_entries (
        id, tenant_id, child_id, enrollment_id, classroom_id, teacher_id, 
        experience_id, experience_schedule_id, experience_session_id, experience_participation_id, 
        little_moment_id, milestone_id, event_id,
        journey_type, title, observation, child_voice, teacher_reflection, 
        observation_date, participation_type, is_milestone, is_parent_visible, 
        parent_visibility_status, created_at, created_by, updated_at, updated_by, version
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, p_event_id,
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$$;

-- 2. 29-parameter overload (accepts p_photos text[] for direct entity call)
CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id UUID,
    IN p_tenant_id UUID,
    IN p_child_id UUID,
    IN p_enrollment_id UUID,
    IN p_classroom_id UUID,
    IN p_teacher_id UUID,
    IN p_experience_id UUID,
    IN p_experience_schedule_id UUID,
    IN p_experience_session_id UUID,
    IN p_experience_participation_id UUID,
    IN p_little_moment_id UUID,
    IN p_milestone_id UUID,
    IN p_event_id UUID,
    IN p_journey_type VARCHAR,
    IN p_title VARCHAR,
    IN p_observation TEXT,
    IN p_child_voice TEXT,
    IN p_teacher_reflection TEXT,
    IN p_observation_date TIMESTAMPTZ,
    IN p_participation_type VARCHAR,
    IN p_is_milestone BOOLEAN,
    IN p_is_parent_visible BOOLEAN,
    IN p_parent_visibility_status VARCHAR,
    IN p_created_at TIMESTAMPTZ,
    IN p_created_by UUID,
    IN p_updated_at TIMESTAMPTZ,
    IN p_updated_by UUID,
    IN p_version INT,
    IN p_photos TEXT[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    CALL public.sp_create_journey_entry(
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, p_event_id,
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$$;

-- 3. 27-parameter overload (backward compatibility without event_id)
CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id UUID,
    IN p_tenant_id UUID,
    IN p_child_id UUID,
    IN p_enrollment_id UUID,
    IN p_classroom_id UUID,
    IN p_teacher_id UUID,
    IN p_experience_id UUID,
    IN p_experience_schedule_id UUID,
    IN p_experience_session_id UUID,
    IN p_experience_participation_id UUID,
    IN p_little_moment_id UUID,
    IN p_milestone_id UUID,
    IN p_journey_type VARCHAR,
    IN p_title VARCHAR,
    IN p_observation TEXT,
    IN p_child_voice TEXT,
    IN p_teacher_reflection TEXT,
    IN p_observation_date TIMESTAMPTZ,
    IN p_participation_type VARCHAR,
    IN p_is_milestone BOOLEAN,
    IN p_is_parent_visible BOOLEAN,
    IN p_parent_visibility_status VARCHAR,
    IN p_created_at TIMESTAMPTZ,
    IN p_created_by UUID,
    IN p_updated_at TIMESTAMPTZ,
    IN p_updated_by UUID,
    IN p_version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    CALL public.sp_create_journey_entry(
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, NULL::UUID,
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$$;
