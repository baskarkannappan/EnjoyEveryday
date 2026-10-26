-- Function: public.fn_create_event_task
-- Backup generated on 2026-10-25 20:32:54

CREATE OR REPLACE FUNCTION public.fn_create_event_task(p_id uuid, p_event_id uuid, p_description character varying, p_owner_id uuid, p_due_date timestamp with time zone, p_is_completed boolean, p_completed_at timestamp with time zone, p_created_at timestamp with time zone, p_updated_at timestamp with time zone)  RETURNS uuid  LANGUAGE plpgsql AS $function$ BEGIN     INSERT INTO event_tasks (id, event_id, description, owner_id, due_date, is_completed, completed_at, created_at, updated_at)     VALUES (p_id, p_event_id, p_description, p_owner_id, p_due_date, p_is_completed, p_completed_at, p_created_at, p_updated_at);     RETURN p_id; END; $function$ ;
