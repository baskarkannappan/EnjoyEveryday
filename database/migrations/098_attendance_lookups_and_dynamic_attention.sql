-- Migration 098: Attendance Master Lookups & Dynamic Attention Query

-- 1. Seed Attendance Master Lookups
INSERT INTO public.master_lookups (category, code, display_name, display_order, is_active, metadata_json)
VALUES
    -- Attendance Statuses
    ('AttendanceStatus', 'Present', 'Present', 1, true, '{"badgeClass": "green", "countsAsPresent": true}'::jsonb),
    ('AttendanceStatus', 'Late', 'Late', 2, true, '{"badgeClass": "amber", "countsAsPresent": true}'::jsonb),
    ('AttendanceStatus', 'Absent', 'Absent', 3, true, '{"badgeClass": "red", "countsAsPresent": false}'::jsonb),
    ('AttendanceStatus', 'Excused', 'Excused', 4, true, '{"badgeClass": "blue", "countsAsPresent": false}'::jsonb),
    ('AttendanceStatus', 'NotMarked', 'Not marked', 5, true, '{"badgeClass": "gray", "countsAsPresent": false}'::jsonb),

    -- Classroom Attendance Statuses
    ('ClassroomAttendanceStatus', 'Marked', 'Marked', 1, true, '{"badgeClass": "green", "tab": "all"}'::jsonb),
    ('ClassroomAttendanceStatus', 'NeedsMarking', 'Needs marking', 2, true, '{"badgeClass": "amber", "tab": "pending"}'::jsonb),
    ('ClassroomAttendanceStatus', 'StaffingAttention', 'Staffing attention', 3, true, '{"badgeClass": "red", "tab": "attention"}'::jsonb),

    -- Pickup Verification Methods
    ('PickupVerificationMethod', 'DirectHandover', 'Parent / Guardian Direct Handover', 1, true, '{"requiresId": false}'::jsonb),
    ('PickupVerificationMethod', 'IdVerified', 'Authorized Collector (Photo ID Verified)', 2, true, '{"requiresId": true}'::jsonb),
    ('PickupVerificationMethod', 'PhoneAuthorized', 'Emergency Phone-Authorized Handover', 3, true, '{"requiresId": true, "requiresStaffOverride": true}'::jsonb),
    ('PickupVerificationMethod', 'SystemAutoClosed', 'System Auto-Closed at End of Day', 4, true, '{"systemGenerated": true}'::jsonb)
ON CONFLICT (category, code) DO UPDATE
SET display_name = EXCLUDED.display_name,
    display_order = EXCLUDED.display_order,
    metadata_json = EXCLUDED.metadata_json,
    updated_at = now();

-- 2. Stored Function for Real Attendance Needs Attention
DROP FUNCTION IF EXISTS public.fn_get_attendance_needs_attention(UUID, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_attendance_needs_attention(
    p_tenant_id UUID,
    p_date DATE DEFAULT CURRENT_DATE
)
RETURNS TABLE (
    alert_type VARCHAR,
    icon VARCHAR,
    title VARCHAR,
    description TEXT,
    branch_name VARCHAR,
    classroom_name VARCHAR,
    action_text VARCHAR,
    action_url VARCHAR,
    severity VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- 1. Unmarked children in classrooms
    RETURN QUERY
    WITH room_unmarked AS (
        SELECT 
            b.name AS b_name,
            c.name AS c_name,
            COUNT(ca.id) AS unmarked_count
        FROM public.child_attendance ca
        JOIN public.classrooms c ON ca.classroom_id = c.id
        JOIN public.branches b ON c.branch_id = b.id
        WHERE ca.tenant_id = p_tenant_id
          AND ca.attendance_date = p_date
          AND ca.status = 'Not marked'
        GROUP BY b.name, c.name
        HAVING COUNT(ca.id) > 0
    )
    SELECT 
        'UnmarkedAttendance'::VARCHAR AS alert_type,
        '◷'::VARCHAR AS icon,
        'Attendance not completed'::VARCHAR AS title,
        (ru.b_name || ' · ' || ru.c_name || ' has ' || ru.unmarked_count || ' children not marked.')::TEXT AS description,
        ru.b_name::VARCHAR AS branch_name,
        ru.c_name::VARCHAR AS classroom_name,
        'Review classroom'::VARCHAR AS action_text,
        ('/management/attendance/classrooms/' || ru.c_name || '?branch=' || ru.b_name)::VARCHAR AS action_url,
        'amber'::VARCHAR AS severity
    FROM room_unmarked ru
    LIMIT 5;

    -- 2. Missing child checkouts (Checked in / Present, but departure_time IS NULL)
    RETURN QUERY
    WITH missing_checkouts AS (
        SELECT 
            b.name AS b_name,
            c.name AS c_name,
            COUNT(ca.id) AS missing_count
        FROM public.child_attendance ca
        JOIN public.classrooms c ON ca.classroom_id = c.id
        JOIN public.branches b ON c.branch_id = b.id
        WHERE ca.tenant_id = p_tenant_id
          AND ca.attendance_date = p_date
          AND ca.status IN ('Present', 'Late')
          AND ca.departure_time IS NULL
        GROUP BY b.name, c.name
        HAVING COUNT(ca.id) > 0
    )
    SELECT 
        'MissingCheckout'::VARCHAR AS alert_type,
        '↗'::VARCHAR AS icon,
        'Child checkout missing'::VARCHAR AS title,
        (mc.b_name || ' · ' || mc.c_name || ' has ' || mc.missing_count || ' children checked in without recorded departure.')::TEXT AS description,
        mc.b_name::VARCHAR AS branch_name,
        mc.c_name::VARCHAR AS classroom_name,
        'View records'::VARCHAR AS action_text,
        ('/management/attendance/classrooms/' || mc.c_name || '?branch=' || mc.b_name)::VARCHAR AS action_url,
        'red'::VARCHAR AS severity
    FROM missing_checkouts mc
    LIMIT 5;

    -- 3. Teacher staffing review (branches where teacher attendance count < 2 on a weekday)
    RETURN QUERY
    WITH branch_teachers AS (
        SELECT 
            b.id AS b_id,
            b.name AS b_name,
            COUNT(ta.id) FILTER (WHERE ta.status IN ('Present', 'Late')) AS present_teachers
        FROM public.branches b
        LEFT JOIN public.teacher_attendance ta 
            ON ta.branch_id = b.id 
            AND ta.attendance_date = p_date 
            AND ta.tenant_id = p_tenant_id
        WHERE b.tenant_id = p_tenant_id
          AND b.is_active = true
        GROUP BY b.id, b.name
        HAVING COUNT(ta.id) FILTER (WHERE ta.status IN ('Present', 'Late')) < 2
    )
    SELECT 
        'StaffingReview'::VARCHAR AS alert_type,
        '♙'::VARCHAR AS icon,
        'Teacher coverage needs review'::VARCHAR AS title,
        (bt.b_name || ' branch has ' || bt.present_teachers || ' teachers marked present today.')::TEXT AS description,
        bt.b_name::VARCHAR AS branch_name,
        ''::VARCHAR AS classroom_name,
        'Review branch'::VARCHAR AS action_text,
        ('/management/attendance/branches/' || bt.b_name)::VARCHAR AS action_url,
        'amber'::VARCHAR AS severity
    FROM branch_teachers bt
    LIMIT 3;
END;
$$;
