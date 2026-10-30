CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_for_date(
    p_child_id UUID,
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
    arrival_time TIME WITHOUT TIME ZONE,
    departure_time TIME WITHOUT TIME ZONE,
    notes TEXT,
    recorded_by_user_id UUID,
    created_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE,
    pickup_person_name VARCHAR,
    pickup_person_relationship VARCHAR,
    pickup_verification_method VARCHAR,
    pickup_notes TEXT,
    authorized_pickup_id UUID,
    departure_verified_by_user_id UUID
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        ca.id,
        ca.tenant_id,
        ca.branch_id,
        ca.classroom_id,
        ca.child_id,
        ca.attendance_date,
        ca.status,
        ca.arrival_time,
        ca.departure_time,
        ca.notes,
        ca.recorded_by_user_id,
        ca.created_at,
        ca.updated_at,
        ca.pickup_person_name,
        ca.pickup_person_relationship,
        ca.pickup_verification_method,
        ca.pickup_notes,
        ca.authorized_pickup_id,
        ca.departure_verified_by_user_id
    FROM child_attendance ca
    WHERE ca.child_id = p_child_id AND ca.attendance_date = p_date
    LIMIT 1;
$$;
