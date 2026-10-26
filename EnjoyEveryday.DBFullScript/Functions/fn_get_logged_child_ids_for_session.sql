-- Function: public.fn_get_logged_child_ids_for_session
-- Backup generated on 2026-10-25 20:33:29

CREATE OR REPLACE FUNCTION public.fn_get_logged_child_ids_for_session(p_tenant_id uuid, p_session_id uuid)  RETURNS TABLE(child_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cje.child_id     FROM child_journey_entries cje     WHERE cje.tenant_id = p_tenant_id AND cje.experience_session_id = p_session_id; END; $function$ ;
