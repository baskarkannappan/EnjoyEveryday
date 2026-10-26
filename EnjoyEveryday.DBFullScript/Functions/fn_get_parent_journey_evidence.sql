-- Function: public.fn_get_parent_journey_evidence
-- Backup generated on 2026-10-25 20:33:35

CREATE OR REPLACE FUNCTION public.fn_get_parent_journey_evidence(p_entry_ids uuid[])  RETURNS TABLE(journey_entry_id uuid, text_content text)  LANGUAGE sql  STABLE AS $function$     SELECT journey_entry_id, text_content     FROM child_journey_evidence     WHERE journey_entry_id = ANY(p_entry_ids)       AND evidence_type = 'Photo'       AND (visibility = 'Parent' OR visibility = 'Public')       AND text_content IS NOT NULL AND text_content != ''     ORDER BY created_at ASC; $function$ ;
