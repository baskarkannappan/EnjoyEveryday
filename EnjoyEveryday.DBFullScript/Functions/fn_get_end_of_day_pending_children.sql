-- Function: public.fn_get_end_of_day_pending_children
-- Backup generated on 2026-10-25 20:33:16

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_children(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid)  RETURNS TABLE(id uuid, name character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ca.id, (c.first_name || ' ' || c.last_name)::VARCHAR AS name     FROM child_attendance ca     JOIN children c ON ca.child_id = c.id     WHERE ca.tenant_id = p_tenant_id AND ca.attendance_date = p_date       AND ca.status IN ('Present', 'Late') AND ca.departure_time IS NULL       AND (p_branch_id IS NULL OR ca.branch_id = p_branch_id); END; $function$ ;
