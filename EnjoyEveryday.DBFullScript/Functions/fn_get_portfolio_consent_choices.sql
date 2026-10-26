-- Function: public.fn_get_portfolio_consent_choices
-- Backup generated on 2026-10-25 20:33:39

CREATE OR REPLACE FUNCTION public.fn_get_portfolio_consent_choices(p_tenant_id uuid, p_consent_document_id uuid)  RETURNS TABLE(choice character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cc.choice::VARCHAR     FROM consent_choices cc     WHERE cc.tenant_id = p_tenant_id AND cc.consent_document_id = p_consent_document_id       AND (cc.section_key ILIKE '%photo%' OR cc.section_key ILIKE '%media%'); END; $function$ ;
