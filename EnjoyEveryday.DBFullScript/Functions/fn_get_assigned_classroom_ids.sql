-- Function: public.fn_get_assigned_classroom_ids
-- Backup generated on 2026-10-25 20:33:01

CREATE OR REPLACE FUNCTION public.fn_get_assigned_classroom_ids(p_tenant_id uuid, p_user_id uuid)  RETURNS TABLE(classroom_id uuid)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ct.classroom_id     FROM classroom_teachers ct     WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id; END; $function$ ;
