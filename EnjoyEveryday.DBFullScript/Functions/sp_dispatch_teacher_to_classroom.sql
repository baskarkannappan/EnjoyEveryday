-- Function: public.sp_dispatch_teacher_to_classroom
-- Backup generated on 2026-10-25 20:34:00

CREATE OR REPLACE PROCEDURE public.sp_dispatch_teacher_to_classroom(IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_user_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)     VALUES (gen_random_uuid(), p_tenant_id, p_classroom_id, p_user_id, FALSE)     ON CONFLICT (classroom_id, user_id) DO NOTHING; END; $procedure$ ;
