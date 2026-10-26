-- Function: public.sp_insert_child_story
-- Backup generated on 2026-10-25 20:34:01

CREATE OR REPLACE PROCEDURE public.sp_insert_child_story(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_teacher_id uuid, IN p_experience_schedule_id uuid, IN p_content text, IN p_status character varying, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO child_stories (id, tenant_id, child_id, teacher_id, experience_schedule_id, content, status, created_at, updated_at)     VALUES (p_id, p_tenant_id, p_child_id, p_teacher_id, p_experience_schedule_id, p_content, p_status, p_created_at, p_updated_at); END; $procedure$ ;
