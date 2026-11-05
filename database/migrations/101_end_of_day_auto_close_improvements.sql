-- Migration: 101_end_of_day_auto_close_improvements.sql
-- Description: Improve end-of-day auto-closing for activities and attendance

-- 1. sp_close_end_of_day_children: Ensure status is set to CheckedOut and departure_time is marked
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
        status = 'CheckedOut',
        pickup_person_name = 'System Auto-Closed',
        pickup_person_relationship = 'System Process',
        pickup_verification_method = 'System Auto-Closed',
        pickup_notes = CASE WHEN pickup_notes IS NULL OR pickup_notes = '' THEN 'System auto-checkout at End of Day'
                            ELSE pickup_notes || ' · System auto-checkout at End of Day' END,
        departure_verified_by_user_id = CASE WHEN p_user_id = '00000000-0000-0000-0000-000000000000'::uuid THEN NULL ELSE p_user_id END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
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
    CALL public.sp_close_end_of_day_children(p_departure::interval, p_user_id, p_ids);
END;
$$;

-- 2. sp_close_end_of_day_teachers: Ensure clock_out_time and status are updated
CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(
    p_clock_out INTERVAL,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teacher_attendance
    SET clock_out_time = p_clock_out::time,
        status = 'ClockedOut',
        notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'
                     ELSE notes || ' · System auto-checkout at End of Day' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_close_end_of_day_teachers(
    p_clock_out TIME,
    p_ids UUID[]
)
LANGUAGE plpgsql
AS $$
BEGIN
    CALL public.sp_close_end_of_day_teachers(p_clock_out::interval, p_ids);
END;
$$;

-- 3. sp_close_end_of_day_activities: Ensure all pending/active schedules and running sessions are closed
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
    SET status = 'Completed',
        end_time = p_end_time::time,
        actual_notes = CASE WHEN actual_notes IS NULL OR actual_notes = '' THEN 'System Auto-Closed at End of Day'
                            WHEN actual_notes LIKE '%System Auto-Closed%' THEN actual_notes
                            ELSE actual_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id
      AND (
          experience_schedule_id = ANY(p_schedule_ids)
          OR (status NOT IN ('Completed', 'Cancelled', 'Finished') AND session_date = CURRENT_DATE)
      );
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
    CALL public.sp_close_end_of_day_activities(p_tenant_id, p_schedule_ids, p_end_time::interval);
END;
$$;

