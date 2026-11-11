-- Function: public.fn_get_branch_brief
-- Backup generated on 2026-11-11 09:04:46

CREATE OR REPLACE FUNCTION public.fn_get_branch_brief(p_branch_id uuid)  RETURNS TABLE(id uuid, name character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT b.id, b.name::VARCHAR     FROM branches b     WHERE b.id = p_branch_id; END; $function$ ;
