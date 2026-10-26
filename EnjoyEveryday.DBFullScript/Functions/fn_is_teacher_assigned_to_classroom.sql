-- Function: public.fn_is_teacher_assigned_to_classroom
-- Backup generated on 2026-10-25 20:33:46

CREATE OR REPLACE FUNCTION public.fn_is_teacher_assigned_to_classroom(p_tenant_id uuid, p_classroom_id uuid, p_teacher_id uuid, p_user_id uuid DEFAULT NULL::uuid)  RETURNS boolean  LANGUAGE plpgsql AS $function$ BEGIN     RETURN EXISTS (         SELECT 1         FROM classroom_teachers ct         WHERE ct.tenant_id = p_tenant_id            AND ct.classroom_id = p_classroom_id            AND (ct.user_id = p_teacher_id OR (p_user_id IS NOT NULL AND ct.user_id = p_user_id))     ); END; $function$ ;
