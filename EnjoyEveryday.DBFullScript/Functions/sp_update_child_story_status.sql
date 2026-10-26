-- Function: public.sp_update_child_story_status
-- Backup generated on 2026-10-25 20:34:08

CREATE OR REPLACE PROCEDURE public.sp_update_child_story_status(IN p_id uuid, IN p_tenant_id uuid, IN p_status character varying, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE child_stories     SET status = p_status, updated_at = p_updated_at     WHERE id = p_id AND tenant_id = p_tenant_id; END; $procedure$ ;
