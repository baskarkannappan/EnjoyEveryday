-- Function: public.sp_update_enrollment_application_stage
-- Backup generated on 2026-10-25 20:34:08

CREATE OR REPLACE PROCEDURE public.sp_update_enrollment_application_stage(IN p_tenant_id uuid, IN p_application_id uuid, IN p_new_stage character varying, IN p_notes text, IN p_tour_date timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE enrollment_pipeline_applications     SET pipeline_stage = p_new_stage,         tour_date = COALESCE(p_tour_date, tour_date),         internal_notes = CASE WHEN p_notes IS NOT NULL THEN p_notes ELSE internal_notes END,         updated_at = NOW()     WHERE tenant_id = p_tenant_id AND id = p_application_id; END; $procedure$ ;
