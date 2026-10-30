-- Fix fn_get_branch_room_stats to exclude inactive and virtual/pool classrooms
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
    WHERE tenant_id = p_tenant_id 
      AND branch_id = p_branch_id
      AND is_active = true
      AND COALESCE(classroom_type, '') != 'Pool'
      AND COALESCE(environment, '') != 'Virtual';
$$;
