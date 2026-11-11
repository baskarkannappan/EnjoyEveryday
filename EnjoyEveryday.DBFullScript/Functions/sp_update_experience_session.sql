-- Function: public.sp_update_experience_session
-- Backup generated on 2026-11-11 09:06:05

CREATE OR REPLACE PROCEDURE public.sp_update_experience_session(IN p_id uuid, IN p_tenant_id uuid, IN p_status character varying, IN p_actual_notes text, IN p_start_time interval, IN p_end_time interval)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE experience_sessions SET         status = p_status,         actual_notes = p_actual_notes,         start_time = p_start_time::time,         end_time = p_end_time::time,         updated_at = NOW()     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;
