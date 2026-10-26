-- Function: public.sp_delete_consent_choices
-- Backup generated on 2026-10-25 20:33:56

CREATE OR REPLACE PROCEDURE public.sp_delete_consent_choices(IN p_tenant_id uuid, IN p_doc_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM consent_choices WHERE tenant_id = p_tenant_id AND consent_document_id = p_doc_id; END; $procedure$ ;
