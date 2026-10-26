-- Function: public.fn_get_parent_child_photo_consent
-- Backup generated on 2026-10-25 20:33:35

CREATE OR REPLACE FUNCTION public.fn_get_parent_child_photo_consent(p_child_id uuid)  RETURNS character varying  LANGUAGE sql  STABLE AS $function$     SELECT d.status     FROM child_consent_documents d     LEFT JOIN consent_templates t ON d.consent_template_id = t.id     WHERE d.child_id = p_child_id       AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')     ORDER BY d.created_at DESC     LIMIT 1; $function$ ;
