-- Function: public.sp_delete_experience_feedback
-- Backup generated on 2026-10-25 20:33:58

CREATE OR REPLACE PROCEDURE public.sp_delete_experience_feedback(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM experience_feedback WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;
