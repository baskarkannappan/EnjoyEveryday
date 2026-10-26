-- Function: public.fn_get_consent_audit_events_by_doc_id
-- Backup generated on 2026-10-25 20:33:12

CREATE OR REPLACE FUNCTION public.fn_get_consent_audit_events_by_doc_id(p_doc_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, consent_document_id uuid, actor_type character varying, actor_user_id uuid, actor_name character varying, event_type character varying, timestamp_utc timestamp with time zone, correlation_id character varying, safe_metadata text)  LANGUAGE sql  STABLE AS $function$     SELECT id, tenant_id, organization_id, consent_document_id, actor_type, actor_user_id, actor_name, event_type, timestamp_utc, correlation_id, safe_metadata     FROM consent_audit_events     WHERE consent_document_id = p_doc_id     ORDER BY timestamp_utc DESC; $function$ ;
