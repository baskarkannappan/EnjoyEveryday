-- Function: public.sp_get_child_journey_insight_request_by_id
-- Backup generated on 2026-10-25 20:34:01

CREATE OR REPLACE PROCEDURE public.sp_get_child_journey_insight_request_by_id(IN p_tenant_id uuid, IN p_request_id uuid, INOUT cur refcursor DEFAULT 'cur'::refcursor)  LANGUAGE plpgsql AS $procedure$ BEGIN     OPEN cur FOR     SELECT          id, tenant_id, organization_id, classroom_id, child_id,         requested_by_user_id, period_type, period_start_date, period_end_date,         focus_area, teacher_question, evidence_count, overall_summary,         evidence_gaps, created_at     FROM child_journey_insight_requests     WHERE tenant_id = p_tenant_id AND id = p_request_id; END; $procedure$ ;
