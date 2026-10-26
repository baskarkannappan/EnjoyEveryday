-- Function: public.fn_get_portfolio_entry_evidence
-- Backup generated on 2026-10-25 20:33:40

CREATE OR REPLACE FUNCTION public.fn_get_portfolio_entry_evidence(p_tenant_id uuid, p_child_id uuid)  RETURNS TABLE(journey_entry_id uuid, text_content text)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT cje_ev.journey_entry_id, cje_ev.text_content     FROM child_journey_evidence cje_ev     WHERE cje_ev.journey_entry_id IN (         SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id     )     ORDER BY cje_ev.created_at DESC; END; $function$ ;
