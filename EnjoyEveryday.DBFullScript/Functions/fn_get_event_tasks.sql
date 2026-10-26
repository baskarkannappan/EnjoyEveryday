-- Function: public.fn_get_event_tasks
-- Backup generated on 2026-10-25 20:33:18

CREATE OR REPLACE FUNCTION public.fn_get_event_tasks(p_event_id uuid)  RETURNS TABLE(id uuid, event_id uuid, description character varying, owner_id uuid, due_date timestamp with time zone, is_completed boolean, completed_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT et.id, et.event_id, et.description, et.owner_id, et.due_date,            et.is_completed, et.completed_at, et.created_at, et.updated_at     FROM event_tasks et     WHERE et.event_id = p_event_id     ORDER BY et.created_at; END; $function$ ;
