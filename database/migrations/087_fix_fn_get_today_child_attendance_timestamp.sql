-- Migration 087: Add TIMESTAMP overload for fn_get_today_child_attendance

CREATE OR REPLACE FUNCTION public.fn_get_today_child_attendance(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMP WITHOUT TIME ZONE
)
RETURNS SETOF child_attendance
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM child_attendance
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND attendance_date = p_date::date
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_today_child_attendance(
    p_tenant_id UUID,
    p_child_id UUID,
    p_date TIMESTAMPTZ
)
RETURNS SETOF child_attendance
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM child_attendance
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND attendance_date = p_date::date
    LIMIT 1;
$$;
