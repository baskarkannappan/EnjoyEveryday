-- Function: public.sp_create_journey_entry
-- Backup generated on 2026-10-25 20:33:53

CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id uuid,
    IN p_tenant_id uuid,
    IN p_child_id uuid,
    IN p_enrollment_id uuid,
    IN p_classroom_id uuid,
    IN p_teacher_id uuid,
    IN p_experience_id uuid,
    IN p_experience_schedule_id uuid,
    IN p_experience_session_id uuid,
    IN p_experience_participation_id uuid,
    IN p_little_moment_id uuid,
    IN p_milestone_id uuid,
    IN p_event_id uuid,
    IN p_journey_type character varying,
    IN p_title character varying,
    IN p_observation text,
    IN p_child_voice text,
    IN p_teacher_reflection text,
    IN p_observation_date timestamp with time zone,
    IN p_participation_type character varying,
    IN p_is_milestone boolean,
    IN p_is_parent_visible boolean,
    IN p_parent_visibility_status character varying,
    IN p_created_at timestamp with time zone,
    IN p_created_by uuid,
    IN p_updated_at timestamp with time zone,
    IN p_updated_by uuid,
    IN p_version integer
)
LANGUAGE plpgsql
AS $procedure$
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
$procedure$;

CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id uuid,
    IN p_tenant_id uuid,
    IN p_child_id uuid,
    IN p_enrollment_id uuid,
    IN p_classroom_id uuid,
    IN p_teacher_id uuid,
    IN p_experience_id uuid,
    IN p_experience_schedule_id uuid,
    IN p_experience_session_id uuid,
    IN p_experience_participation_id uuid,
    IN p_little_moment_id uuid,
    IN p_milestone_id uuid,
    IN p_event_id uuid,
    IN p_journey_type character varying,
    IN p_title character varying,
    IN p_observation text,
    IN p_child_voice text,
    IN p_teacher_reflection text,
    IN p_observation_date timestamp with time zone,
    IN p_participation_type character varying,
    IN p_is_milestone boolean,
    IN p_is_parent_visible boolean,
    IN p_parent_visibility_status character varying,
    IN p_created_at timestamp with time zone,
    IN p_created_by uuid,
    IN p_updated_at timestamp with time zone,
    IN p_updated_by uuid,
    IN p_version integer,
    IN p_photos text[]
)
LANGUAGE plpgsql
AS $procedure$
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
$procedure$;

CREATE OR REPLACE PROCEDURE public.sp_create_journey_entry(
    IN p_id uuid,
    IN p_tenant_id uuid,
    IN p_child_id uuid,
    IN p_enrollment_id uuid,
    IN p_classroom_id uuid,
    IN p_teacher_id uuid,
    IN p_experience_id uuid,
    IN p_experience_schedule_id uuid,
    IN p_experience_session_id uuid,
    IN p_experience_participation_id uuid,
    IN p_little_moment_id uuid,
    IN p_milestone_id uuid,
    IN p_journey_type character varying,
    IN p_title character varying,
    IN p_observation text,
    IN p_child_voice text,
    IN p_teacher_reflection text,
    IN p_observation_date timestamp with time zone,
    IN p_participation_type character varying,
    IN p_is_milestone boolean,
    IN p_is_parent_visible boolean,
    IN p_parent_visibility_status character varying,
    IN p_created_at timestamp with time zone,
    IN p_created_by uuid,
    IN p_updated_at timestamp with time zone,
    IN p_updated_by uuid,
    IN p_version integer
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    CALL public.sp_create_journey_entry(
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, NULL::uuid,
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$procedure$;
