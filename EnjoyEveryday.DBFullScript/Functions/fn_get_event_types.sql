-- Function: public.fn_get_event_types
-- Backup generated on 2026-10-25 20:33:18

CREATE OR REPLACE FUNCTION public.fn_get_event_types(p_tenant_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, name character varying, description text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT et.id, et.tenant_id, et.name, et.description, et.is_active, et.created_at, et.updated_at     FROM event_types et     WHERE et.tenant_id = p_tenant_id AND et.is_active = true     ORDER BY et.name; END; $function$ ;
