-- Function: public.fn_get_end_of_day_pending_events
-- Backup generated on 2026-10-25 20:33:16

CREATE OR REPLACE FUNCTION public.fn_get_end_of_day_pending_events(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid)  RETURNS TABLE(id uuid, name character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ev.id, ev.title::VARCHAR AS name     FROM events ev     WHERE ev.tenant_id = p_tenant_id AND ev.event_date = p_date       AND ev.status IN ('Draft', 'Planned', 'Approved', 'Published', 'InProgress')       AND (p_branch_id IS NULL OR ev.scope = 'Organization' OR ev.is_organization_wide = true OR ev.branch_id = p_branch_id); END; $function$ ;
