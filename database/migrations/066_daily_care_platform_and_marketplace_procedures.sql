-- ============================================================
-- Migration 066: Daily Care, Platform Administration, Organization Intelligence,
-- Benchmarking, and Experience Marketplace Routines
-- Replaces remaining inline queries in:
--   - SecurePickupRepository
--   - ChildDailyCareRepository
--   - PlatformAdminRepository
--   - OrganizationIntelligenceRepository
--   - BranchBenchmarkingRepository
--   - ExperienceMarketplaceRepository
-- ============================================================

-- ------------------------------------------------------------
-- 1. SECURE PICKUP ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_today_child_attendance(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date DATE
)
RETURNS SETOF child_attendance
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM child_attendance
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND attendance_date = p_date
    LIMIT 1;
$$;

CREATE OR REPLACE PROCEDURE public.sp_add_authorized_pickup(
    p_id UUID,
    p_tenant_id UUID,
    p_child_id UUID,
    p_person_id UUID,
    p_relationship VARCHAR,
    p_pickup_code VARCHAR,
    p_photo_url TEXT,
    p_valid_from TIMESTAMPTZ,
    p_valid_to TIMESTAMPTZ,
    p_is_active BOOLEAN,
    p_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO authorized_pickups (
        id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, valid_from, valid_to, is_active, notes
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_person_id, p_relationship, p_pickup_code, p_photo_url, p_valid_from, p_valid_to, p_is_active, p_notes
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_authorized_pickup_exists(
    p_tenant_id UUID,
    p_pickup_id UUID
)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS(SELECT 1 FROM authorized_pickups WHERE tenant_id = p_tenant_id AND id = p_pickup_id);
$$;

-- ------------------------------------------------------------
-- 2. CHILD DAILY CARE ROUTINES (Events, Naps, Plans, Supplies, Handovers)
-- ------------------------------------------------------------

CREATE OR REPLACE PROCEDURE public.sp_add_child_care_event(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_event_type VARCHAR,
    p_event_time TIMESTAMPTZ,
    p_diaper_condition VARCHAR,
    p_skin_observation VARCHAR,
    p_meal_type VARCHAR,
    p_food_offered VARCHAR,
    p_amount_consumed VARCHAR,
    p_fluid_oz NUMERIC,
    p_rest_status VARCHAR,
    p_toileting_activity VARCHAR,
    p_hygiene_type VARCHAR,
    p_comfort_mood VARCHAR,
    p_notes TEXT,
    p_parent_visible BOOLEAN,
    p_recorded_by_user_id UUID,
    p_recorded_by_name VARCHAR,
    p_correction_reason TEXT,
    p_is_corrected BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_care_events (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        event_type, event_time, diaper_condition, skin_observation,
        meal_type, food_offered, amount_consumed, fluid_oz,
        rest_status, toileting_activity, hygiene_type, comfort_mood,
        notes, parent_visible, recorded_by_user_id, recorded_by_name,
        correction_reason, is_corrected, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_event_type, p_event_time, p_diaper_condition, p_skin_observation,
        p_meal_type, p_food_offered, p_amount_consumed, p_fluid_oz,
        p_rest_status, p_toileting_activity, p_hygiene_type, p_comfort_mood,
        p_notes, p_parent_visible, p_recorded_by_user_id, p_recorded_by_name,
        p_correction_reason, p_is_corrected, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_child_care_event(
    p_id UUID,
    p_tenant_id UUID,
    p_event_time TIMESTAMPTZ,
    p_diaper_condition VARCHAR,
    p_skin_observation VARCHAR,
    p_meal_type VARCHAR,
    p_food_offered VARCHAR,
    p_amount_consumed VARCHAR,
    p_fluid_oz NUMERIC,
    p_rest_status VARCHAR,
    p_toileting_activity VARCHAR,
    p_hygiene_type VARCHAR,
    p_comfort_mood VARCHAR,
    p_notes TEXT,
    p_parent_visible BOOLEAN,
    p_correction_reason TEXT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_care_events
    SET event_time = p_event_time,
        diaper_condition = p_diaper_condition,
        skin_observation = p_skin_observation,
        meal_type = p_meal_type,
        food_offered = p_food_offered,
        amount_consumed = p_amount_consumed,
        fluid_oz = p_fluid_oz,
        rest_status = p_rest_status,
        toileting_activity = p_toileting_activity,
        hygiene_type = p_hygiene_type,
        comfort_mood = p_comfort_mood,
        notes = p_notes,
        parent_visible = p_parent_visible,
        correction_reason = p_correction_reason,
        is_corrected = TRUE,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_event_by_id(
    p_tenant_id UUID,
    p_event_id UUID
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
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id AND e.id = p_event_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date DATE
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
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.child_id = p_child_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time ASC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_child(
    p_tenant_id UUID,
    p_child_id UUID
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
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.child_id = p_child_id
    ORDER BY e.event_time DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date DATE
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
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.classroom_id = p_classroom_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_events_by_branch_and_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date DATE
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
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.branch_id = p_branch_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time DESC;
$$;

-- Nap Sessions
CREATE OR REPLACE PROCEDURE public.sp_start_child_nap_session(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_start_time TIMESTAMPTZ,
    p_notes TEXT,
    p_started_by_user_id UUID,
    p_started_by_name VARCHAR,
    p_parent_visible BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_nap_sessions (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        start_time, status, notes, started_by_user_id, started_by_name,
        parent_visible, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_start_time, 'Active', p_notes, p_started_by_user_id, p_started_by_name,
        p_parent_visible, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_end_child_nap_session(
    p_tenant_id UUID,
    p_session_id UUID,
    p_end_time TIMESTAMPTZ,
    p_duration_minutes INT,
    p_outcome VARCHAR,
    p_notes TEXT,
    p_ended_by_user_id UUID,
    p_ended_by_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_nap_sessions
    SET end_time = p_end_time,
        duration_minutes = p_duration_minutes,
        status = 'Completed',
        outcome = p_outcome,
        notes = COALESCE(p_notes, notes),
        ended_by_user_id = p_ended_by_user_id,
        ended_by_name = p_ended_by_name,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_session_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_child_nap_session(
    p_tenant_id UUID,
    p_child_id UUID
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
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.child_id = p_child_id 
      AND s.status = 'Active'
    ORDER BY s.start_time DESC
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_child_naps_by_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID
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
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.classroom_id = p_classroom_id 
      AND s.status = 'Active'
    ORDER BY s.start_time ASC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_naps_by_child_and_date(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date DATE
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
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.child_id = p_child_id
      AND s.start_time::date = p_date
    ORDER BY s.start_time ASC;
$$;

-- Care Plans
CREATE OR REPLACE FUNCTION public.fn_get_active_child_care_plan(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    child_id UUID,
    version INT,
    diaper_routine_guidance TEXT,
    feeding_instructions TEXT,
    bottle_schedule_guidance TEXT,
    sleep_routine_guidance TEXT,
    toileting_support_plan TEXT,
    comfort_calming_preferences TEXT,
    required_supplies_note TEXT,
    dietary_allergies_summary TEXT,
    effective_date DATE,
    review_date DATE,
    status VARCHAR,
    created_by_user_id UUID,
    approved_by_user_id UUID,
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT p.id, p.tenant_id, p.organization_id, p.branch_id, p.child_id, p.version,
           p.diaper_routine_guidance, p.feeding_instructions, p.bottle_schedule_guidance,
           p.sleep_routine_guidance, p.toileting_support_plan, p.comfort_calming_preferences,
           p.required_supplies_note, p.dietary_allergies_summary, p.effective_date, p.review_date,
           p.status, p.created_by_user_id, p.approved_by_user_id, p.approved_at, p.created_at, p.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_care_plans p
    JOIN children c ON p.child_id = c.id
    WHERE p.tenant_id = p_tenant_id 
      AND p.child_id = p_child_id 
      AND p.status = 'Active'
    ORDER BY p.version DESC
    LIMIT 1;
$$;

CREATE OR REPLACE PROCEDURE public.sp_save_child_care_plan(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_child_id UUID,
    p_version INT,
    p_diaper_routine_guidance TEXT,
    p_feeding_instructions TEXT,
    p_bottle_schedule_guidance TEXT,
    p_sleep_routine_guidance TEXT,
    p_toileting_support_plan TEXT,
    p_comfort_calming_preferences TEXT,
    p_required_supplies_note TEXT,
    p_dietary_allergies_summary TEXT,
    p_effective_date DATE,
    p_review_date DATE,
    p_status VARCHAR,
    p_created_by_user_id UUID,
    p_approved_by_user_id UUID,
    p_approved_at TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_care_plans (
        id, tenant_id, organization_id, branch_id, child_id, version,
        diaper_routine_guidance, feeding_instructions, bottle_schedule_guidance,
        sleep_routine_guidance, toileting_support_plan, comfort_calming_preferences,
        required_supplies_note, dietary_allergies_summary, effective_date, review_date,
        status, created_by_user_id, approved_by_user_id, approved_at, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_version,
        p_diaper_routine_guidance, p_feeding_instructions, p_bottle_schedule_guidance,
        p_sleep_routine_guidance, p_toileting_support_plan, p_comfort_calming_preferences,
        p_required_supplies_note, p_dietary_allergies_summary, p_effective_date, p_review_date,
        p_status, p_created_by_user_id, p_approved_by_user_id, p_approved_at, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_care_plans_by_branch(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    child_id UUID,
    version INT,
    diaper_routine_guidance TEXT,
    feeding_instructions TEXT,
    bottle_schedule_guidance TEXT,
    sleep_routine_guidance TEXT,
    toileting_support_plan TEXT,
    comfort_calming_preferences TEXT,
    required_supplies_note TEXT,
    dietary_allergies_summary TEXT,
    effective_date DATE,
    review_date DATE,
    status VARCHAR,
    created_by_user_id UUID,
    approved_by_user_id UUID,
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT p.id, p.tenant_id, p.organization_id, p.branch_id, p.child_id, p.version,
           p.diaper_routine_guidance, p.feeding_instructions, p.bottle_schedule_guidance,
           p.sleep_routine_guidance, p.toileting_support_plan, p.comfort_calming_preferences,
           p.required_supplies_note, p.dietary_allergies_summary, p.effective_date, p.review_date,
           p.status, p.created_by_user_id, p.approved_by_user_id, p.approved_at, p.created_at, p.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_care_plans p
    JOIN children c ON p.child_id = c.id
    WHERE p.tenant_id = p_tenant_id 
      AND p.branch_id = p_branch_id 
      AND p.status = 'Active'
    ORDER BY c.first_name ASC;
$$;

-- Care Supplies
CREATE OR REPLACE FUNCTION public.fn_get_child_care_supplies_by_child(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    supply_type VARCHAR,
    status VARCHAR,
    notes TEXT,
    threshold_note VARCHAR,
    last_reported_by_user_id UUID,
    last_reported_by_name VARCHAR,
    parent_reminder_sent BOOLEAN,
    last_reminder_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.supply_type, s.status, s.notes, s.threshold_note, s.last_reported_by_user_id,
           s.last_reported_by_name, s.parent_reminder_sent, s.last_reminder_at, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_supplies s
    JOIN children c ON s.child_id = c.id
    LEFT JOIN classrooms cl ON s.classroom_id = cl.id
    WHERE s.tenant_id = p_tenant_id AND s.child_id = p_child_id
    ORDER BY s.supply_type ASC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_low_child_care_supplies_by_branch(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    supply_type VARCHAR,
    status VARCHAR,
    notes TEXT,
    threshold_note VARCHAR,
    last_reported_by_user_id UUID,
    last_reported_by_name VARCHAR,
    parent_reminder_sent BOOLEAN,
    last_reminder_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.supply_type, s.status, s.notes, s.threshold_note, s.last_reported_by_user_id,
           s.last_reported_by_name, s.parent_reminder_sent, s.last_reminder_at, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_supplies s
    JOIN children c ON s.child_id = c.id
    LEFT JOIN classrooms cl ON s.classroom_id = cl.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.branch_id = p_branch_id 
      AND s.status IN ('Low', 'Depleted')
    ORDER BY s.updated_at DESC;
$$;

CREATE OR REPLACE PROCEDURE public.sp_save_child_care_supply_status(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_supply_type VARCHAR,
    p_status VARCHAR,
    p_notes TEXT,
    p_threshold_note VARCHAR,
    p_last_reported_by_user_id UUID,
    p_last_reported_by_name VARCHAR,
    p_parent_reminder_sent BOOLEAN,
    p_last_reminder_at TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_care_supplies (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        supply_type, status, notes, threshold_note, last_reported_by_user_id,
        last_reported_by_name, parent_reminder_sent, last_reminder_at, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_supply_type, p_status, p_notes, p_threshold_note, p_last_reported_by_user_id,
        p_last_reported_by_name, p_parent_reminder_sent, p_last_reminder_at, p_created_at, p_updated_at
    )
    ON CONFLICT (id) DO UPDATE
    SET status = EXCLUDED.status,
        notes = EXCLUDED.notes,
        threshold_note = EXCLUDED.threshold_note,
        last_reported_by_user_id = EXCLUDED.last_reported_by_user_id,
        last_reported_by_name = EXCLUDED.last_reported_by_name,
        parent_reminder_sent = EXCLUDED.parent_reminder_sent,
        last_reminder_at = EXCLUDED.last_reminder_at,
        updated_at = EXCLUDED.updated_at;
END;
$$;

-- Teacher Handovers
CREATE OR REPLACE PROCEDURE public.sp_create_teacher_handover(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_handover_date DATE,
    p_shift_type VARCHAR,
    p_outgoing_teacher_user_id UUID,
    p_outgoing_teacher_name VARCHAR,
    p_incoming_teacher_user_id UUID,
    p_incoming_teacher_name VARCHAR,
    p_summary_notes TEXT,
    p_child_specific_notes TEXT,
    p_outstanding_care_tasks TEXT,
    p_pending_followups TEXT,
    p_status VARCHAR,
    p_is_critical BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO teacher_handovers (
        id, tenant_id, organization_id, branch_id, classroom_id, handover_date,
        shift_type, outgoing_teacher_user_id, outgoing_teacher_name,
        incoming_teacher_user_id, incoming_teacher_name, summary_notes,
        child_specific_notes, outstanding_care_tasks, pending_followups,
        status, is_critical, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_handover_date,
        p_shift_type, p_outgoing_teacher_user_id, p_outgoing_teacher_name,
        p_incoming_teacher_user_id, p_incoming_teacher_name, p_summary_notes,
        p_child_specific_notes, p_outstanding_care_tasks, p_pending_followups,
        p_status, p_is_critical, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_acknowledge_teacher_handover(
    p_tenant_id UUID,
    p_handover_id UUID,
    p_user_id UUID,
    p_user_name VARCHAR,
    p_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teacher_handovers
    SET status = 'Acknowledged',
        acknowledged_at = NOW(),
        acknowledged_by_user_id = p_user_id,
        acknowledged_by_name = p_user_name,
        acknowledgement_notes = p_notes,
        updated_at = NOW()
    WHERE id = p_handover_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_handover_by_id(
    p_tenant_id UUID,
    p_handover_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    handover_date DATE,
    shift_type VARCHAR,
    outgoing_teacher_user_id UUID,
    outgoing_teacher_name VARCHAR,
    incoming_teacher_user_id UUID,
    incoming_teacher_name VARCHAR,
    summary_notes TEXT,
    child_specific_notes TEXT,
    outstanding_care_tasks TEXT,
    pending_followups TEXT,
    status VARCHAR,
    is_critical BOOLEAN,
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by_user_id UUID,
    acknowledged_by_name VARCHAR,
    acknowledgement_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    classroom_name VARCHAR,
    branch_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.id = p_handover_id AND th.tenant_id = p_tenant_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_handovers_by_classroom_and_date(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date DATE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    handover_date DATE,
    shift_type VARCHAR,
    outgoing_teacher_user_id UUID,
    outgoing_teacher_name VARCHAR,
    incoming_teacher_user_id UUID,
    incoming_teacher_name VARCHAR,
    summary_notes TEXT,
    child_specific_notes TEXT,
    outstanding_care_tasks TEXT,
    pending_followups TEXT,
    status VARCHAR,
    is_critical BOOLEAN,
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by_user_id UUID,
    acknowledged_by_name VARCHAR,
    acknowledgement_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    classroom_name VARCHAR,
    branch_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.tenant_id = p_tenant_id 
      AND th.classroom_id = p_classroom_id 
      AND th.handover_date = p_date
    ORDER BY th.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_pending_teacher_handovers_by_branch(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    handover_date DATE,
    shift_type VARCHAR,
    outgoing_teacher_user_id UUID,
    outgoing_teacher_name VARCHAR,
    incoming_teacher_user_id UUID,
    incoming_teacher_name VARCHAR,
    summary_notes TEXT,
    child_specific_notes TEXT,
    outstanding_care_tasks TEXT,
    pending_followups TEXT,
    status VARCHAR,
    is_critical BOOLEAN,
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by_user_id UUID,
    acknowledged_by_name VARCHAR,
    acknowledgement_notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    classroom_name VARCHAR,
    branch_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.tenant_id = p_tenant_id 
      AND th.branch_id = p_branch_id 
      AND th.status = 'PendingAcknowledgement'
    ORDER BY th.is_critical DESC, th.created_at DESC;
$$;

-- ------------------------------------------------------------
-- 3. PLATFORM ADMIN ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_platform_organizations_overview()
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    branch_count INT,
    admin_email VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active, o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1) AS admin_email
    FROM organizations o
    ORDER BY o.name;
$$;

CREATE OR REPLACE PROCEDURE public.sp_provision_platform_organization(
    p_org_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_admin_email VARCHAR,
    p_branch_count INT DEFAULT 1
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_raw_slug VARCHAR;
    v_slug VARCHAR;
    v_slug_count INT;
    v_existing_user_id UUID;
    v_existing_tenant_id UUID;
    v_admin_user_id UUID;
    v_derived_name VARCHAR;
    v_branches_to_create INT;
    v_i INT;
    v_branch_id UUID;
    v_branch_name VARCHAR;
    v_branch_loc VARCHAR;
    v_role_id UUID;
BEGIN
    v_raw_slug := trim(both '-' from regexp_replace(lower(p_name), '[^a-z0-9]+', '-', 'g'));
    IF v_raw_slug IS NULL OR v_raw_slug = '' THEN
        v_slug := 'org-' || substring(p_org_id::text, 1, 8);
    ELSE
        v_slug := v_raw_slug;
    END IF;

    SELECT COUNT(1) INTO v_slug_count FROM tenants WHERE slug = v_slug;
    IF v_slug_count > 0 THEN
        v_slug := v_slug || '-' || substring(p_org_id::text, 1, 4);
    END IF;

    INSERT INTO tenants (id, name, slug) 
    VALUES (p_org_id, p_name, v_slug) 
    ON CONFLICT (id) DO NOTHING;

    SELECT id, tenant_id INTO v_existing_user_id, v_existing_tenant_id 
    FROM users 
    WHERE LOWER(email) = LOWER(trim(p_admin_email)) OR normalized_email = UPPER(trim(p_admin_email)) 
    LIMIT 1;

    IF v_existing_user_id IS NOT NULL THEN
        v_admin_user_id := v_existing_user_id;
        IF v_existing_tenant_id = '10000000-0000-0000-0000-000000000001'::uuid THEN
            UPDATE users SET tenant_id = p_org_id WHERE id = v_admin_user_id;
        END IF;
    ELSE
        v_admin_user_id := gen_random_uuid();
        v_derived_name := initcap(split_part(split_part(trim(p_admin_email), '@', 1), '.', 1));
        IF v_derived_name IS NULL OR v_derived_name = '' THEN
            v_derived_name := 'Daycare Director';
        END IF;

        INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
        VALUES (v_admin_user_id, p_org_id, trim(p_admin_email), UPPER(trim(p_admin_email)), TRUE, 'Admin', v_derived_name, v_derived_name, TRUE)
        ON CONFLICT (normalized_email) DO NOTHING;
    END IF;

    INSERT INTO organizations (id, tenant_id, name, location, is_active)
    VALUES (p_org_id, p_org_id, p_name, p_location, TRUE)
    ON CONFLICT (id) DO NOTHING;

    v_branches_to_create := GREATEST(1, p_branch_count);
    FOR v_i IN 1..v_branches_to_create LOOP
        v_branch_id := gen_random_uuid();
        IF v_i = 1 THEN
            v_branch_name := 'Main Campus';
        ELSIF v_i = 2 THEN
            v_branch_name := 'Annex Campus';
        ELSE
            v_branch_name := 'Campus ' || v_i;
        END IF;

        IF p_location IS NOT NULL AND trim(p_location) <> '' THEN
            v_branch_loc := p_location;
        ELSE
            v_branch_loc := 'Main Branch';
        END IF;

        INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
        VALUES (v_branch_id, p_org_id, p_org_id, v_branch_name, v_branch_loc, TRUE)
        ON CONFLICT (id) DO NOTHING;
    END LOOP;

    IF v_admin_user_id IS NOT NULL THEN
        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
        VALUES (v_admin_user_id, p_org_id, p_org_id, 'Active')
        ON CONFLICT (user_id, organization_id) DO NOTHING;

        SELECT id INTO v_role_id 
        FROM roles 
        WHERE name IN ('OrganizationAdmin', 'OrganizationOwner') 
          AND (tenant_id = p_org_id OR tenant_id IS NULL) 
        ORDER BY tenant_id NULLS LAST 
        LIMIT 1;

        IF v_role_id IS NOT NULL THEN
            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
            VALUES (v_admin_user_id, v_role_id, p_org_id, p_org_id)
            ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;
        END IF;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_platform_organization_details(
    p_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_admin_email VARCHAR,
    p_branch_count INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_tenant_id UUID;
    v_current_branches_count INT;
    v_i INT;
    v_branch_name VARCHAR;
    v_trimmed_email VARCHAR;
    v_derived_name VARCHAR;
    v_existing_user_id UUID;
    v_admin_user_id UUID;
    v_current_user_id UUID;
BEGIN
    UPDATE organizations 
    SET name = p_name, location = p_location, updated_at = NOW() 
    WHERE id = p_id;

    UPDATE tenants 
    SET name = p_name, updated_at = NOW() 
    WHERE id = p_id;

    SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_id;
    IF v_tenant_id IS NULL THEN
        v_tenant_id := p_id;
    END IF;

    SELECT COUNT(*) INTO v_current_branches_count FROM branches WHERE organization_id = p_id;
    IF v_current_branches_count = 0 THEN
        FOR v_i IN 1..GREATEST(1, p_branch_count) LOOP
            IF v_i = 1 THEN
                v_branch_name := 'Main Campus';
            ELSIF v_i = 2 THEN
                v_branch_name := 'Annex Campus';
            ELSE
                v_branch_name := 'Campus ' || v_i;
            END IF;
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (gen_random_uuid(), v_tenant_id, p_id, v_branch_name, COALESCE(p_location, 'Branch Location'), TRUE);
        END LOOP;
    ELSIF v_current_branches_count < p_branch_count THEN
        FOR v_i IN (v_current_branches_count + 1)..p_branch_count LOOP
            IF v_i = 2 THEN
                v_branch_name := 'Annex Campus';
            ELSE
                v_branch_name := 'Campus ' || v_i;
            END IF;
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (gen_random_uuid(), v_tenant_id, p_id, v_branch_name, COALESCE(p_location, 'Branch Location'), TRUE);
        END LOOP;
    END IF;

    IF p_admin_email IS NOT NULL AND trim(p_admin_email) <> '' THEN
        v_trimmed_email := trim(p_admin_email);
        v_derived_name := initcap(split_part(split_part(v_trimmed_email, '@', 1), '.', 1));
        IF v_derived_name IS NULL OR v_derived_name = '' THEN
            v_derived_name := 'Daycare Director';
        END IF;

        SELECT id INTO v_existing_user_id 
        FROM users 
        WHERE normalized_email = UPPER(v_trimmed_email) 
        LIMIT 1;

        IF v_existing_user_id IS NOT NULL THEN
            v_admin_user_id := v_existing_user_id;
            UPDATE users 
            SET is_active = TRUE,
                display_name = COALESCE(NULLIF(TRIM(display_name), ''), v_derived_name),
                updated_at = NOW() 
            WHERE id = v_admin_user_id;
        ELSE
            SELECT om.user_id INTO v_current_user_id
            FROM organization_memberships om
            JOIN users u ON u.id = om.user_id
            WHERE om.organization_id = p_id AND om.status = 'Active'
            ORDER BY om.created_at
            LIMIT 1;

            IF v_current_user_id IS NOT NULL THEN
                v_admin_user_id := v_current_user_id;
                UPDATE users 
                SET email = v_trimmed_email, 
                    normalized_email = UPPER(v_trimmed_email), 
                    display_name = v_derived_name,
                    updated_at = NOW() 
                WHERE id = v_admin_user_id;
            ELSE
                v_admin_user_id := gen_random_uuid();
                INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                VALUES (v_admin_user_id, v_tenant_id, v_trimmed_email, UPPER(v_trimmed_email), TRUE, 'Admin', v_derived_name, v_derived_name, TRUE)
                ON CONFLICT (normalized_email) DO UPDATE SET updated_at = NOW();
            END IF;
        END IF;

        DELETE FROM user_organization_roles 
        WHERE organization_id = p_id AND user_id != v_admin_user_id;
        
        DELETE FROM organization_memberships 
        WHERE organization_id = p_id AND user_id != v_admin_user_id;

        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
        VALUES (v_admin_user_id, p_id, v_tenant_id, 'Active')
        ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active', tenant_id = v_tenant_id;

        INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
        SELECT v_admin_user_id, r.id, p_id, v_tenant_id
        FROM roles r
        WHERE r.tenant_id = v_tenant_id AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
        ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_platform_all_branches()
RETURNS TABLE (
    id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    organization_name VARCHAR,
    effective_location TEXT,
    classrooms_count INT,
    children_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        b.id AS id,
        b.organization_id AS organization_id,
        b.name AS name,
        b.location AS location,
        b.is_active AS is_active,
        b.created_at AS created_at,
        o.name AS organization_name,
        COALESCE(b.location, o.location, 'Location unspecified') AS effective_location,
        COALESCE((SELECT COUNT(*) FROM classrooms c WHERE c.branch_id = b.id), 0)::int AS classrooms_count,
        COALESCE((SELECT COUNT(*) FROM children ch JOIN classrooms c ON ch.classroom_id = c.id WHERE c.branch_id = b.id), 0)::int AS children_count
    FROM branches b
    JOIN organizations o ON b.organization_id = o.id
    ORDER BY o.name, b.name;
$$;

CREATE OR REPLACE FUNCTION public.fn_add_platform_branch(
    p_branch_id UUID,
    p_organization_id UUID,
    p_name VARCHAR,
    p_location VARCHAR
)
RETURNS TABLE (
    id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    organization_name VARCHAR,
    effective_location TEXT,
    classrooms_count INT,
    children_count INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_tenant_id UUID;
    v_org_name VARCHAR;
BEGIN
    SELECT tenant_id, o.name INTO v_tenant_id, v_org_name 
    FROM organizations o 
    WHERE o.id = p_organization_id;

    IF v_tenant_id IS NULL THEN
        v_tenant_id := p_organization_id;
    END IF;

    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
    VALUES (p_branch_id, v_tenant_id, p_organization_id, p_name, p_location, TRUE);

    RETURN QUERY
    SELECT 
        p_branch_id,
        p_organization_id,
        p_name,
        p_location,
        TRUE,
        NOW(),
        COALESCE(v_org_name, 'Organization'),
        COALESCE(p_location, 'Location unspecified'),
        0::int,
        0::int;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_platform_organization_admins()
RETURNS TABLE (
    user_id UUID,
    admin_name TEXT,
    admin_email VARCHAR,
    organization_id UUID,
    organization_name VARCHAR,
    role_name VARCHAR,
    account_status TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT DISTINCT ON (o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid))
        COALESCE(u.id, gen_random_uuid()) AS user_id,
        CASE 
            WHEN u.display_name IS NOT NULL AND TRIM(u.display_name) != '' AND TRIM(u.display_name) != o.name THEN TRIM(u.display_name)
            WHEN TRIM(CONCAT(u.first_name, ' ', u.last_name)) != '' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != 'Admin' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != o.name THEN TRIM(CONCAT(u.first_name, ' ', u.last_name))
            WHEN u.email IS NOT NULL AND u.email != '' THEN INITCAP(SPLIT_PART(SPLIT_PART(u.email, '@', 1), '.', 1))
            ELSE 'Daycare Director'
        END AS admin_name,
        COALESCE(u.email, 'admin@daycare.org') AS admin_email,
        o.id AS organization_id,
        o.name AS organization_name,
        COALESCE(r.name, 'Organization Admin') AS role_name,
        CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END AS account_status
    FROM organizations o
    LEFT JOIN organization_memberships om ON om.organization_id = o.id AND om.status = 'Active'
    LEFT JOIN users u ON u.id = om.user_id
    LEFT JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = o.id
    LEFT JOIN roles r ON r.id = uor.role_id
    ORDER BY o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid), 
             CASE WHEN r.name = 'OrganizationOwner' THEN 1 WHEN r.name = 'OrganizationAdmin' THEN 2 ELSE 3 END;
$$;

-- ------------------------------------------------------------
-- 4. ORGANIZATION INTELLIGENCE ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_operational_overview(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ
)
RETURNS TABLE (
    total_attendance INT,
    present_count INT,
    absent_count INT,
    late_count INT,
    total_classrooms INT,
    planned_classrooms INT,
    planned_experiences INT,
    completed_experiences INT,
    upcoming_experiences INT,
    moments_count INT,
    care_count INT
)
LANGUAGE sql
STABLE
AS $$
    WITH att AS (
        SELECT 
            COUNT(*)::int AS total_att,
            COUNT(*) FILTER (WHERE ca.status = 'Present')::int AS present_cnt,
            COUNT(*) FILTER (WHERE ca.status = 'Absent')::int AS absent_cnt,
            COUNT(*) FILTER (WHERE ca.status = 'Late')::int AS late_cnt
        FROM child_attendance ca
        JOIN classrooms cl ON ca.classroom_id = cl.id
        JOIN branches b ON cl.branch_id = b.id
        WHERE ca.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND ca.attendance_date BETWEEN p_start_date::date AND p_end_date::date
    ),
    cls AS (
        SELECT 
            COUNT(DISTINCT cl.id)::int AS total_cls,
            COUNT(DISTINCT es.classroom_id)::int AS planned_cls
        FROM classrooms cl
        JOIN branches b ON cl.branch_id = b.id
        LEFT JOIN experience_schedules es ON cl.id = es.classroom_id 
             AND es.tenant_id = p_tenant_id
             AND es.scheduled_date BETWEEN p_start_date::date AND p_end_date::date
        WHERE cl.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND cl.is_active = true
    ),
    exp AS (
        SELECT 
            COUNT(*)::int AS planned_exp,
            COUNT(*) FILTER (WHERE es.status IN ('Completed', 'Finished'))::int AS completed_exp,
            COUNT(*) FILTER (WHERE es.status IN ('Confirmed', 'Scheduled'))::int AS upcoming_exp
        FROM experience_schedules es
        JOIN classrooms cl ON es.classroom_id = cl.id
        JOIN branches b ON cl.branch_id = b.id
        WHERE es.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND es.scheduled_date BETWEEN p_start_date::date AND p_end_date::date
    ),
    act AS (
        SELECT 
            (SELECT COUNT(*)::int 
             FROM child_journey_entries cje
             JOIN classrooms cl ON cje.classroom_id = cl.id
             JOIN branches b ON cl.branch_id = b.id
             WHERE cje.tenant_id = p_tenant_id
               AND b.organization_id = p_org_id
               AND (p_branch_id IS NULL OR b.id = p_branch_id)
               AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
               AND cje.observation_date >= p_start_date::date 
               AND cje.observation_date <= p_end_date::date) AS moments_cnt,
            (SELECT COUNT(*)::int
             FROM child_care_events cce
             JOIN classrooms cl ON cce.classroom_id = cl.id
             JOIN branches b ON cl.branch_id = b.id
             WHERE cce.tenant_id = p_tenant_id
               AND b.organization_id = p_org_id
               AND (p_branch_id IS NULL OR b.id = p_branch_id)
               AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
               AND cce.event_time >= p_start_date 
               AND cce.event_time <= p_end_date) AS care_cnt
    )
    SELECT 
        att.total_att, att.present_cnt, att.absent_cnt, att.late_cnt,
        cls.total_cls, cls.planned_cls,
        exp.planned_exp, exp.completed_exp, exp.upcoming_exp,
        act.moments_cnt, act.care_cnt
    FROM att, cls, exp, act;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_curriculum_variety(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date DATE,
    p_end_date DATE
)
RETURNS TABLE (
    title VARCHAR,
    usage_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        e.title AS title,
        COUNT(es.id) AS usage_count
    FROM experience_schedules es
    JOIN experiences e ON es.experience_id = e.id
    JOIN classrooms cl ON es.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE es.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND es.scheduled_date BETWEEN p_start_date AND p_end_date
    GROUP BY e.title
    ORDER BY usage_count DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_planning_gaps(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today DATE,
    p_seven_days_out DATE
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
    SELECT 
        cl.id AS classroom_id,
        cl.name AS classroom_name,
        b.name AS branch_name,
        COUNT(es.id) AS upcoming_plans_count
    FROM classrooms cl
    JOIN branches b ON cl.branch_id = b.id
    LEFT JOIN experience_schedules es ON cl.id = es.classroom_id
         AND es.tenant_id = p_tenant_id
         AND es.scheduled_date BETWEEN p_today AND p_seven_days_out
         AND es.status NOT IN ('Cancelled')
    WHERE cl.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND cl.is_active = true
    GROUP BY cl.id, cl.name, b.name
    HAVING COUNT(es.id) < 2
    ORDER BY upcoming_plans_count ASC, cl.name;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_overdue_plans_count(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_today DATE
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::int
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE es.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND es.scheduled_date < p_today
      AND es.status IN ('Confirmed', 'Scheduled');
$$;

CREATE OR REPLACE FUNCTION public.fn_get_intelligence_data_quality(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_start_date DATE,
    p_end_date DATE
)
RETURNS TABLE (
    missing_departure_times_count INT,
    drafts_awaiting_review_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        (SELECT COUNT(*)::int 
         FROM child_attendance ca
         JOIN classrooms cl ON ca.classroom_id = cl.id
         JOIN branches b ON cl.branch_id = b.id
         WHERE ca.tenant_id = p_tenant_id
           AND b.organization_id = p_org_id
           AND (p_branch_id IS NULL OR b.id = p_branch_id)
           AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
           AND ca.status = 'Present'
           AND ca.departure_time IS NULL
           AND ca.attendance_date < CURRENT_DATE
           AND ca.attendance_date BETWEEN p_start_date AND p_end_date) AS missing_departure_times_count,
        (SELECT COUNT(*)::int 
         FROM parent_communications pc
         JOIN classrooms cl ON pc.classroom_id = cl.id
         JOIN branches b ON cl.branch_id = b.id
         WHERE pc.tenant_id = p_tenant_id
           AND b.organization_id = p_org_id
           AND (p_branch_id IS NULL OR b.id = p_branch_id)
           AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
           AND pc.status IN ('Draft', 'PendingReview')) AS drafts_awaiting_review_count;
$$;

CREATE OR REPLACE FUNCTION public.sp_save_organization_intelligence_insight(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_scope_type VARCHAR,
    p_reporting_period VARCHAR,
    p_category VARCHAR,
    p_title VARCHAR,
    p_observation TEXT,
    p_supporting_metrics_json JSONB,
    p_possible_explanation TEXT,
    p_suggested_next_step TEXT,
    p_evidence_strength VARCHAR,
    p_data_limitations TEXT,
    p_source_type VARCHAR,
    p_is_ai_generated BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS SETOF organization_intelligence_insights
LANGUAGE sql
AS $$
    INSERT INTO organization_intelligence_insights (
        id, tenant_id, organization_id, branch_id, classroom_id,
        scope_type, reporting_period, category, title, observation,
        supporting_metrics_json, possible_explanation, suggested_next_step,
        evidence_strength, data_limitations, source_type, is_ai_generated,
        created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        p_scope_type, p_reporting_period, p_category, p_title, p_observation,
        COALESCE(p_supporting_metrics_json, '{}'::jsonb), p_possible_explanation, p_suggested_next_step,
        p_evidence_strength, p_data_limitations, p_source_type, p_is_ai_generated,
        p_created_at, p_updated_at
    )
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_recent_organization_intelligence_insights(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_reporting_period VARCHAR
)
RETURNS SETOF organization_intelligence_insights
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM organization_intelligence_insights
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_organization_id
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR classroom_id = p_classroom_id)
      AND reporting_period = p_reporting_period
    ORDER BY created_at DESC
    LIMIT 10;
$$;

-- ------------------------------------------------------------
-- 5. BRANCH BENCHMARKING ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_branches_for_benchmarking(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    name VARCHAR,
    location VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    WITH org_branches AS (
        SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
        FROM branches b
        WHERE b.tenant_id = p_tenant_id AND b.organization_id = p_organization_id
        ORDER BY b.name
    )
    SELECT * FROM org_branches
    UNION ALL
    SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
    FROM branches b
    WHERE b.tenant_id = p_tenant_id
      AND NOT EXISTS (SELECT 1 FROM org_branches)
    ORDER BY name;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_room_stats(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    room_count INT,
    total_cap INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COUNT(*)::int AS room_count,
        COALESCE(SUM(capacity), 0)::int AS total_cap
    FROM classrooms
    WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_enrolled_children_count(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::int
    FROM children c
    JOIN classrooms cl ON c.classroom_id = cl.id
    WHERE c.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id AND c.is_active = TRUE;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_attendance_stats(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    total_attendance_records INT,
    present_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COUNT(*)::int AS total_attendance_records,
        COUNT(CASE WHEN status = 'Present' THEN 1 END)::int AS present_count
    FROM child_attendance
    WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id
      AND attendance_date >= CURRENT_DATE - INTERVAL '30 days';
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_classrooms_with_plans_count(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(DISTINCT es.classroom_id)::int
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    WHERE es.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id
      AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days';
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_billing_stats(
    p_tenant_id UUID
)
RETURNS TABLE (
    total_billed NUMERIC,
    total_paid NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COALESCE(SUM(total_amount), 0)::numeric AS total_billed,
        COALESCE(SUM(amount_paid), 0)::numeric AS total_paid
    FROM invoices
    WHERE tenant_id = p_tenant_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_branch_operational_goals(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    branch_name VARCHAR,
    title VARCHAR,
    category VARCHAR,
    target_metric VARCHAR,
    current_value VARCHAR,
    target_value VARCHAR,
    status VARCHAR,
    target_date DATE,
    owner_name VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        g.id,
        g.tenant_id AS tenant_id,
        g.organization_id AS organization_id,
        g.branch_id AS branch_id,
        b.name AS branch_name,
        g.title AS title,
        g.category AS category,
        g.target_metric AS target_metric,
        g.current_value AS current_value,
        g.target_value AS target_value,
        g.status AS status,
        g.target_date AS target_date,
        g.owner_name AS owner_name,
        g.created_at AS created_at,
        g.updated_at AS updated_at
    FROM branch_operational_goals g
    LEFT JOIN branches b ON g.branch_id = b.id
    WHERE g.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR g.branch_id = p_branch_id)
    ORDER BY g.created_at DESC;
$$;

CREATE OR REPLACE PROCEDURE public.sp_create_branch_operational_goal(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_title VARCHAR,
    p_category VARCHAR,
    p_target_metric VARCHAR,
    p_current_value VARCHAR,
    p_target_value VARCHAR,
    p_status VARCHAR,
    p_target_date DATE,
    p_owner_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO branch_operational_goals (
        id, tenant_id, organization_id, branch_id, title, category,
        target_metric, current_value, target_value, status, target_date, owner_name
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_title, p_category,
        p_target_metric, p_current_value, p_target_value, p_status, p_target_date, p_owner_name
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_branch_operational_goal_status(
    p_tenant_id UUID,
    p_goal_id UUID,
    p_status VARCHAR,
    p_current_value VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE branch_operational_goals
    SET status = p_status,
        current_value = COALESCE(p_current_value, current_value),
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_goal_id;
END;
$$;

-- ------------------------------------------------------------
-- 6. EXPERIENCE MARKETPLACE & HARVESTING ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_create_experience_license(
    p_id UUID,
    p_organization_id UUID,
    p_experience_id UUID,
    p_license_type VARCHAR,
    p_grant_scope VARCHAR,
    p_granted_by_user_id UUID,
    p_granted_at TIMESTAMPTZ,
    p_expires_at TIMESTAMPTZ,
    p_terms_document_hash VARCHAR,
    p_is_active BOOLEAN
)
RETURNS SETOF experience_licenses
LANGUAGE sql
AS $$
    INSERT INTO experience_licenses (
        id, organization_id, experience_id, license_type, grant_scope, granted_by_user_id, granted_at, expires_at, terms_document_hash, is_active
    ) VALUES (
        p_id, p_organization_id, p_experience_id, p_license_type, p_grant_scope, p_granted_by_user_id, p_granted_at, p_expires_at, p_terms_document_hash, p_is_active
    )
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_active_experience_license(
    p_experience_id UUID
)
RETURNS SETOF experience_licenses
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM experience_licenses
    WHERE experience_id = p_experience_id AND is_active = TRUE
      AND (expires_at IS NULL OR expires_at > NOW())
    ORDER BY granted_at DESC
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_submit_experience_harvest_request(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_source_experience_id UUID,
    p_source_version_number INT,
    p_license_id UUID,
    p_submitted_by_user_id UUID,
    p_submitted_at TIMESTAMPTZ,
    p_status VARCHAR,
    p_review_notes TEXT,
    p_reviewed_by_user_id UUID,
    p_reviewed_at TIMESTAMPTZ,
    p_safety_suitability_verified BOOLEAN,
    p_learning_objectives_verified BOOLEAN,
    p_instructions_verified BOOLEAN,
    p_materials_verified BOOLEAN,
    p_media_rights_verified BOOLEAN,
    p_commercial_rights_verified BOOLEAN
)
RETURNS SETOF experience_harvest_requests
LANGUAGE sql
AS $$
    INSERT INTO experience_harvest_requests (
        id, tenant_id, organization_id, source_experience_id, source_version_number, license_id,
        submitted_by_user_id, submitted_at, status, review_notes, reviewed_by_user_id, reviewed_at,
        safety_suitability_verified, learning_objectives_verified, instructions_verified,
        materials_verified, media_rights_verified, commercial_rights_verified
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_source_experience_id, p_source_version_number, p_license_id,
        p_submitted_by_user_id, p_submitted_at, p_status, p_review_notes, p_reviewed_by_user_id, p_reviewed_at,
        p_safety_suitability_verified, p_learning_objectives_verified, p_instructions_verified,
        p_materials_verified, p_media_rights_verified, p_commercial_rights_verified
    )
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_harvest_request_by_id(
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    source_experience_id UUID,
    source_version_number INT,
    license_id UUID,
    submitted_by_user_id UUID,
    submitted_at TIMESTAMPTZ,
    status VARCHAR,
    review_notes TEXT,
    reviewed_by_user_id UUID,
    reviewed_at TIMESTAMPTZ,
    safety_suitability_verified BOOLEAN,
    learning_objectives_verified BOOLEAN,
    instructions_verified BOOLEAN,
    materials_verified BOOLEAN,
    media_rights_verified BOOLEAN,
    commercial_rights_verified BOOLEAN,
    source_experience_title VARCHAR,
    organization_name VARCHAR,
    submitter_email VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT r.id AS id, r.tenant_id AS tenant_id, r.organization_id AS organization_id, r.source_experience_id AS source_experience_id,
           r.source_version_number AS source_version_number, r.license_id AS license_id, r.submitted_by_user_id AS submitted_by_user_id,
           r.submitted_at AS submitted_at, r.status AS status, r.review_notes AS review_notes, r.reviewed_by_user_id AS reviewed_by_user_id,
           r.reviewed_at AS reviewed_at, r.safety_suitability_verified AS safety_suitability_verified,
           r.learning_objectives_verified AS learning_objectives_verified, r.instructions_verified AS instructions_verified,
           r.materials_verified AS materials_verified, r.media_rights_verified AS media_rights_verified,
           r.commercial_rights_verified AS commercial_rights_verified,
           e.title AS source_experience_title, o.name AS organization_name, u.email AS submitter_email
    FROM experience_harvest_requests r
    LEFT JOIN experiences e ON r.source_experience_id = e.id
    LEFT JOIN organizations o ON r.organization_id = o.id
    LEFT JOIN users u ON r.submitted_by_user_id = u.id
    WHERE r.id = p_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_harvest_requests(
    p_organization_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    source_experience_id UUID,
    source_version_number INT,
    license_id UUID,
    submitted_by_user_id UUID,
    submitted_at TIMESTAMPTZ,
    status VARCHAR,
    review_notes TEXT,
    reviewed_by_user_id UUID,
    reviewed_at TIMESTAMPTZ,
    safety_suitability_verified BOOLEAN,
    learning_objectives_verified BOOLEAN,
    instructions_verified BOOLEAN,
    materials_verified BOOLEAN,
    media_rights_verified BOOLEAN,
    commercial_rights_verified BOOLEAN,
    source_experience_title VARCHAR,
    organization_name VARCHAR,
    submitter_email VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT r.id AS id, r.tenant_id AS tenant_id, r.organization_id AS organization_id, r.source_experience_id AS source_experience_id,
           r.source_version_number AS source_version_number, r.license_id AS license_id, r.submitted_by_user_id AS submitted_by_user_id,
           r.submitted_at AS submitted_at, r.status AS status, r.review_notes AS review_notes, r.reviewed_by_user_id AS reviewed_by_user_id,
           r.reviewed_at AS reviewed_at, r.safety_suitability_verified AS safety_suitability_verified,
           r.learning_objectives_verified AS learning_objectives_verified, r.instructions_verified AS instructions_verified,
           r.materials_verified AS materials_verified, r.media_rights_verified AS media_rights_verified,
           r.commercial_rights_verified AS commercial_rights_verified,
           e.title AS source_experience_title, o.name AS organization_name, u.email AS submitter_email
    FROM experience_harvest_requests r
    LEFT JOIN experiences e ON r.source_experience_id = e.id
    LEFT JOIN organizations o ON r.organization_id = o.id
    LEFT JOIN users u ON r.submitted_by_user_id = u.id
    WHERE (p_organization_id IS NULL OR r.organization_id = p_organization_id)
      AND (p_status IS NULL OR r.status = p_status)
    ORDER BY r.submitted_at DESC;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_experience_harvest_request(
    p_id UUID,
    p_status VARCHAR,
    p_review_notes TEXT,
    p_reviewed_by_user_id UUID,
    p_reviewed_at TIMESTAMPTZ,
    p_safety_suitability_verified BOOLEAN,
    p_learning_objectives_verified BOOLEAN,
    p_instructions_verified BOOLEAN,
    p_materials_verified BOOLEAN,
    p_media_rights_verified BOOLEAN,
    p_commercial_rights_verified BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE experience_harvest_requests SET
        status = p_status,
        review_notes = p_review_notes,
        reviewed_by_user_id = p_reviewed_by_user_id,
        reviewed_at = p_reviewed_at,
        safety_suitability_verified = p_safety_suitability_verified,
        learning_objectives_verified = p_learning_objectives_verified,
        instructions_verified = p_instructions_verified,
        materials_verified = p_materials_verified,
        media_rights_verified = p_media_rights_verified,
        commercial_rights_verified = p_commercial_rights_verified
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_create_platform_experience(
    p_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_category VARCHAR,
    p_age_range_min_months INT,
    p_age_range_max_months INT,
    p_duration_minutes INT,
    p_learning_objectives JSONB,
    p_required_materials JSONB,
    p_instructions TEXT,
    p_safety_notes TEXT,
    p_dna_payload TEXT,
    p_pricing_model VARCHAR,
    p_price_amount NUMERIC,
    p_currency VARCHAR,
    p_publication_status VARCHAR,
    p_ownership_type VARCHAR,
    p_current_version INT,
    p_license_id UUID,
    p_source_harvest_request_id UUID,
    p_source_experience_id UUID,
    p_source_organization_id UUID,
    p_creator_attribution VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS SETOF platform_experiences
LANGUAGE sql
AS $$
    INSERT INTO platform_experiences (
        id, title, description, category, age_range_min_months, age_range_max_months,
        duration_minutes, learning_objectives, required_materials, instructions, safety_notes,
        dna_payload, pricing_model, price_amount, currency, publication_status, ownership_type,
        current_version, license_id, source_harvest_request_id, source_experience_id, source_organization_id,
        creator_attribution, created_at, updated_at
    ) VALUES (
        p_id, p_title, p_description, p_category, p_age_range_min_months, p_age_range_max_months,
        p_duration_minutes, p_learning_objectives, p_required_materials, p_instructions, p_safety_notes,
        p_dna_payload, p_pricing_model, p_price_amount, p_currency, p_publication_status, p_ownership_type,
        p_current_version, p_license_id, p_source_harvest_request_id, p_source_experience_id, p_source_organization_id,
        p_creator_attribution, p_created_at, p_updated_at
    )
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_platform_experience_by_id(
    p_id UUID
)
RETURNS SETOF platform_experiences
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM platform_experiences
    WHERE id = p_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_platform_marketplace_catalog(
    p_category VARCHAR DEFAULT NULL,
    p_pricing_model VARCHAR DEFAULT NULL,
    p_search_query VARCHAR DEFAULT NULL
)
RETURNS SETOF platform_experiences
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM platform_experiences
    WHERE publication_status = 'Published'
      AND (p_category IS NULL OR category = p_category)
      AND (p_pricing_model IS NULL OR pricing_model = p_pricing_model)
      AND (p_search_query IS NULL OR title ILIKE ('%' || trim(p_search_query) || '%') OR description ILIKE ('%' || trim(p_search_query) || '%'))
    ORDER BY created_at DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_all_platform_experiences()
RETURNS SETOF platform_experiences
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM platform_experiences
    ORDER BY created_at DESC;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_platform_experience(
    p_id UUID,
    p_title VARCHAR,
    p_description TEXT,
    p_category VARCHAR,
    p_age_range_min_months INT,
    p_age_range_max_months INT,
    p_duration_minutes INT,
    p_instructions TEXT,
    p_safety_notes TEXT,
    p_dna_payload TEXT,
    p_pricing_model VARCHAR,
    p_price_amount NUMERIC,
    p_currency VARCHAR,
    p_publication_status VARCHAR,
    p_ownership_type VARCHAR,
    p_current_version INT,
    p_creator_attribution VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE platform_experiences SET
        title = p_title,
        description = p_description,
        category = p_category,
        age_range_min_months = p_age_range_min_months,
        age_range_max_months = p_age_range_max_months,
        duration_minutes = p_duration_minutes,
        instructions = p_instructions,
        safety_notes = p_safety_notes,
        dna_payload = p_dna_payload,
        pricing_model = p_pricing_model,
        price_amount = p_price_amount,
        currency = p_currency,
        publication_status = p_publication_status,
        ownership_type = p_ownership_type,
        current_version = p_current_version,
        creator_attribution = p_creator_attribution,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_add_platform_experience_version(
    p_id UUID,
    p_platform_experience_id UUID,
    p_version_number INT,
    p_title VARCHAR,
    p_description TEXT,
    p_dna_payload TEXT,
    p_change_summary TEXT,
    p_created_by_user_id UUID,
    p_created_at TIMESTAMPTZ,
    p_is_active BOOLEAN
)
RETURNS SETOF platform_experience_versions
LANGUAGE sql
AS $$
    INSERT INTO platform_experience_versions (
        id, platform_experience_id, version_number, title, description, dna_payload, change_summary, created_by_user_id, created_at, is_active
    ) VALUES (
        p_id, p_platform_experience_id, p_version_number, p_title, p_description, p_dna_payload, p_change_summary, p_created_by_user_id, p_created_at, p_is_active
    )
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_platform_experience_versions(
    p_platform_experience_id UUID
)
RETURNS SETOF platform_experience_versions
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM platform_experience_versions
    WHERE platform_experience_id = p_platform_experience_id
    ORDER BY version_number DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_create_marketplace_order(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_platform_experience_id UUID,
    p_purchased_by_user_id UUID,
    p_pricing_model VARCHAR,
    p_amount_paid NUMERIC,
    p_currency VARCHAR,
    p_payment_status VARCHAR,
    p_transaction_reference VARCHAR,
    p_purchased_at TIMESTAMPTZ,
    p_refunded_at TIMESTAMPTZ,
    p_refund_reason TEXT
)
RETURNS SETOF marketplace_orders
LANGUAGE sql
AS $$
    INSERT INTO marketplace_orders (
        id, tenant_id, organization_id, platform_experience_id, purchased_by_user_id,
        pricing_model, amount_paid, currency, payment_status, transaction_reference, purchased_at, refunded_at, refund_reason
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_purchased_by_user_id,
        p_pricing_model, p_amount_paid, p_currency, p_payment_status, p_transaction_reference, p_purchased_at, p_refunded_at, p_refund_reason
    )
    ON CONFLICT (organization_id, platform_experience_id, transaction_reference) DO UPDATE
    SET payment_status = EXCLUDED.payment_status
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_marketplace_order_by_transaction_ref(
    p_organization_id UUID,
    p_transaction_reference VARCHAR
)
RETURNS SETOF marketplace_orders
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM marketplace_orders
    WHERE organization_id = p_organization_id AND transaction_reference = p_transaction_reference;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_marketplace_orders(
    p_organization_id UUID DEFAULT NULL
)
RETURNS SETOF marketplace_orders
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM marketplace_orders
    WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)
    ORDER BY purchased_at DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_create_or_update_organization_entitlement(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_platform_experience_id UUID,
    p_order_id UUID,
    p_acquired_version INT,
    p_entitlement_type VARCHAR,
    p_status VARCHAR,
    p_granted_at TIMESTAMPTZ,
    p_expires_at TIMESTAMPTZ,
    p_scope VARCHAR
)
RETURNS SETOF organization_entitlements
LANGUAGE sql
AS $$
    INSERT INTO organization_entitlements (
        id, tenant_id, organization_id, platform_experience_id, order_id,
        acquired_version, entitlement_type, status, granted_at, expires_at, scope
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_order_id,
        p_acquired_version, p_entitlement_type, p_status, p_granted_at, p_expires_at, p_scope
    )
    ON CONFLICT (organization_id, platform_experience_id) DO UPDATE
    SET acquired_version = EXCLUDED.acquired_version,
        status = EXCLUDED.status,
        expires_at = EXCLUDED.expires_at
    RETURNING *;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_organization_entitlement(
    p_organization_id UUID,
    p_platform_experience_id UUID
)
RETURNS SETOF organization_entitlements
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM organization_entitlements
    WHERE organization_id = p_organization_id AND platform_experience_id = p_platform_experience_id
      AND status = 'Active' AND (expires_at IS NULL OR expires_at > NOW());
$$;

CREATE OR REPLACE FUNCTION public.fn_get_organization_entitlements(
    p_organization_id UUID
)
RETURNS SETOF organization_entitlements
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM organization_entitlements
    WHERE organization_id = p_organization_id AND status = 'Active'
      AND (expires_at IS NULL OR expires_at > NOW())
    ORDER BY granted_at DESC;
$$;

CREATE OR REPLACE PROCEDURE public.sp_log_experience_audit_event(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_platform_experience_id UUID,
    p_action VARCHAR,
    p_performed_by_user_id UUID,
    p_details JSONB,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO experience_audit_events (
        id, tenant_id, organization_id, platform_experience_id, action, performed_by_user_id, details, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_action, p_performed_by_user_id, p_details, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_experience_audit_events(
    p_organization_id UUID DEFAULT NULL,
    p_platform_experience_id UUID DEFAULT NULL
)
RETURNS SETOF experience_audit_events
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM experience_audit_events
    WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_platform_experience_id IS NULL OR platform_experience_id = p_platform_experience_id)
    ORDER BY created_at DESC
    LIMIT 200;
$$;
