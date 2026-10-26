-- Function: public.fn_get_experience_versions
-- Backup generated on 2026-10-25 20:33:23

CREATE OR REPLACE FUNCTION public.fn_get_experience_versions(p_experience_id uuid)  RETURNS TABLE(id uuid, experience_id uuid, version_number integer, title character varying, description text, dna_payload jsonb, change_reason character varying, modified_by_user_id uuid, created_at timestamp with time zone)  LANGUAGE plpgsql  STABLE AS $function$ BEGIN     RETURN QUERY     SELECT ev.id, ev.experience_id, ev.version_number, ev.title::VARCHAR, ev.description, ev.dna_payload, ev.change_reason::VARCHAR, ev.modified_by_user_id, ev.created_at     FROM experience_versions ev     WHERE ev.experience_id = p_experience_id     ORDER BY ev.version_number DESC; END; $function$ ;
