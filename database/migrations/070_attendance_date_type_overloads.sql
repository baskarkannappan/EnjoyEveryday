-- Migration: 070_attendance_date_type_overloads.sql
-- Description: Provide correct column return types (TEXT for notes and pickup_notes) and TIMESTAMP / TIMESTAMPTZ overloads.

DROP FUNCTION IF EXISTS public.fn_get_child_attendance_by_date(UUID, UUID, DATE) CASCADE;
DROP FUNCTION IF EXISTS public.fn_get_child_attendance_by_date(UUID, UUID, TIMESTAMP) CASCADE;
DROP FUNCTION IF EXISTS public.fn_get_child_attendance_by_date(UUID, UUID, TIMESTAMPTZ) CASCADE;
DROP FUNCTION IF EXISTS public.fn_get_teacher_attendance_by_date(UUID, UUID, DATE) CASCADE;
DROP FUNCTION IF EXISTS public.fn_get_teacher_attendance_by_date(UUID, UUID, TIMESTAMP) CASCADE;
DROP FUNCTION IF EXISTS public.fn_get_teacher_attendance_by_date(UUID, UUID, TIMESTAMPTZ) CASCADE;
DROP FUNCTION IF EXISTS public.fn_record_teacher_attendance(UUID, UUID, UUID, UUID, DATE, VARCHAR, TIME, TIME, VARCHAR, UUID) CASCADE;
DROP FUNCTION IF EXISTS public.fn_record_teacher_attendance(UUID, UUID, UUID, UUID, DATE, VARCHAR, TIME, TIME, TEXT, UUID) CASCADE;

-- 1. fn_get_child_attendance_by_date
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
    notes TEXT,
    recorded_by_user_id UUID,
    pickup_person_name VARCHAR,
    pickup_person_relationship VARCHAR,
    pickup_verification_method VARCHAR,
    pickup_notes TEXT,
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

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMP
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
    notes TEXT,
    recorded_by_user_id UUID,
    pickup_person_name VARCHAR,
    pickup_person_relationship VARCHAR,
    pickup_verification_method VARCHAR,
    pickup_notes TEXT,
    authorized_pickup_id UUID,
    departure_verified_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMPTZ
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
    notes TEXT,
    recorded_by_user_id UUID,
    pickup_person_name VARCHAR,
    pickup_person_relationship VARCHAR,
    pickup_verification_method VARCHAR,
    pickup_notes TEXT,
    authorized_pickup_id UUID,
    departure_verified_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_child_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

-- 2. fn_get_teacher_attendance_by_date
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
    notes TEXT,
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

CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMP
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_teacher_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_by_date(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date TIMESTAMPTZ
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_teacher_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;

-- 3. fn_record_teacher_attendance
CREATE OR REPLACE FUNCTION public.fn_record_teacher_attendance(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_teacher_id UUID,
    p_attendance_date DATE,
    p_status VARCHAR,
    p_clock_in_time TIME,
    p_clock_out_time TIME,
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

-- 4. fn_get_attendance_summary overloads
CREATE OR REPLACE FUNCTION public.fn_get_attendance_summary(
    p_tenant_id UUID,
    p_date TIMESTAMP
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_attendance_summary(p_tenant_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_attendance_summary(
    p_tenant_id UUID,
    p_date TIMESTAMPTZ
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
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_attendance_summary(p_tenant_id, p_date::DATE);
$$;

-- 5. fn_get_present_child_ids_by_classroom overloads
CREATE OR REPLACE FUNCTION public.fn_get_present_child_ids_by_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date TIMESTAMP
)
RETURNS TABLE (child_id UUID)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_present_child_ids_by_classroom(p_tenant_id, p_classroom_id, p_date::DATE);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_present_child_ids_by_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS TABLE (child_id UUID)
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.fn_get_present_child_ids_by_classroom(p_tenant_id, p_classroom_id, p_date::DATE);
$$;
