-- Function: public.sp_get_child_journey_insight_items
-- Backup generated on 2026-10-25 20:34:00

CREATE OR REPLACE PROCEDURE public.sp_get_child_journey_insight_items(IN p_tenant_id uuid, IN p_request_id uuid, INOUT cur refcursor DEFAULT 'cur'::refcursor)  LANGUAGE plpgsql AS $procedure$ BEGIN     OPEN cur FOR     SELECT          id, request_id, tenant_id, category, classification,         statement, evidence_strength, supporting_evidence,         suggested_experience_id, suggested_experience_title, rank_order, created_at     FROM child_journey_insight_items     WHERE tenant_id = p_tenant_id AND request_id = p_request_id     ORDER BY rank_order ASC, created_at ASC; END; $procedure$ ;
