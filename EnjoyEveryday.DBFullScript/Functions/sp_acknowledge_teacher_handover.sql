-- Function: public.sp_acknowledge_teacher_handover
-- Backup generated on 2026-10-25 20:33:47

CREATE OR REPLACE PROCEDURE public.sp_acknowledge_teacher_handover(IN p_tenant_id uuid, IN p_handover_id uuid, IN p_user_id uuid, IN p_user_name character varying, IN p_notes text)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE teacher_handovers     SET status = 'Acknowledged',         acknowledged_at = NOW(),         acknowledged_by_user_id = p_user_id,         acknowledged_by_name = p_user_name,         acknowledgement_notes = p_notes,         updated_at = NOW()     WHERE id = p_handover_id AND tenant_id = p_tenant_id; END; $procedure$ ;
