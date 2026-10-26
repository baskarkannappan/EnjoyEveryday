-- Function: public.fn_get_branch_classrooms
-- Backup generated on 2026-10-25 20:33:04

CREATE OR REPLACE FUNCTION public.fn_get_branch_classrooms(p_branch_id uuid)  RETURNS TABLE(id uuid, name character varying, room_type character varying, capacity integer)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cl.id, cl.name::VARCHAR, COALESCE(cl.classroom_type, cl.age_group, 'Classroom')::VARCHAR AS room_type, cl.capacity     FROM classrooms cl     WHERE cl.branch_id = p_branch_id AND cl.is_active = true     ORDER BY cl.name; END; $function$ ;
