-- Function: public.fn_create_event_type
-- Backup generated on 2026-10-25 20:32:54

CREATE OR REPLACE FUNCTION public.fn_create_event_type(p_id uuid, p_tenant_id uuid, p_name character varying, p_description text, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone)  RETURNS uuid  LANGUAGE plpgsql AS $function$ BEGIN     INSERT INTO event_types (id, tenant_id, name, description, is_active, created_at, updated_at)     VALUES (p_id, p_tenant_id, p_name, p_description, p_is_active, p_created_at, p_updated_at);     RETURN p_id; END; $function$ ;
