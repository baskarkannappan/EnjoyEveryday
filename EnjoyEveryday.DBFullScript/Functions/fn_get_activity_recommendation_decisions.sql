-- Function: public.fn_get_activity_recommendation_decisions
-- Backup generated on 2026-10-25 20:32:58

CREATE OR REPLACE FUNCTION public.fn_get_activity_recommendation_decisions(p_tenant_id uuid, p_request_id uuid)  RETURNS TABLE(id uuid, request_id uuid, recommendation_item_id uuid, tenant_id uuid, decided_by_user_id uuid, decision character varying, rejection_reason text, resulting_schedule_id uuid, decided_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, request_id, recommendation_item_id, tenant_id, decided_by_user_id, decision, rejection_reason, resulting_schedule_id, decided_at     FROM activity_recommendation_decisions     WHERE request_id = p_request_id AND tenant_id = p_tenant_id     ORDER BY decided_at DESC; $function$ ;
