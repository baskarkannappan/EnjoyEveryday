-- Function: public.fn_get_evidence_for_journey_entry
-- Backup generated on 2026-10-25 20:33:19

CREATE OR REPLACE FUNCTION public.fn_get_evidence_for_journey_entry(p_journey_entry_id uuid)  RETURNS TABLE(id uuid, journey_entry_id uuid, evidence_type character varying, media_id uuid, text_content text, captured_at timestamp with time zone, captured_by uuid, visibility character varying, created_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cje.id, cje.journey_entry_id, cje.evidence_type, cje.media_id,            cje.text_content, cje.captured_at, cje.captured_by, cje.visibility, cje.created_at     FROM child_journey_evidence cje     WHERE cje.journey_entry_id = p_journey_entry_id     ORDER BY cje.created_at; END; $function$ ;
