-- Function: public.fn_get_teacher_user_id
-- Backup generated on 2026-10-25 20:33:43

CREATE OR REPLACE FUNCTION public.fn_get_teacher_user_id(p_tenant_id uuid, p_teacher_id uuid)  RETURNS uuid  LANGUAGE plpgsql AS $function$ DECLARE     v_user_id UUID; BEGIN     SELECT t.user_id INTO v_user_id     FROM teachers t     WHERE t.id = p_teacher_id AND t.tenant_id = p_tenant_id;     RETURN v_user_id; END; $function$ ;
