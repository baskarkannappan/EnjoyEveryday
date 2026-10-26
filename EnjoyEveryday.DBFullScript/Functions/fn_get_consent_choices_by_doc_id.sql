-- Function: public.fn_get_consent_choices_by_doc_id
-- Backup generated on 2026-10-25 20:33:12

CREATE OR REPLACE FUNCTION public.fn_get_consent_choices_by_doc_id(p_doc_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, consent_signer_id uuid, section_key character varying, section_title character varying, is_mandatory boolean, choice character varying, recorded_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, tenant_id, consent_document_id, consent_signer_id, section_key, section_title, is_mandatory, choice, recorded_at     FROM consent_choices     WHERE consent_document_id = p_doc_id; $function$ ;
