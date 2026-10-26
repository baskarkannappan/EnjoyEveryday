-- Function: public.fn_get_child_interests_by_child_id
-- Backup generated on 2026-10-25 20:33:09

CREATE OR REPLACE FUNCTION public.fn_get_child_interests_by_child_id(p_tenant_id uuid, p_child_id uuid)  RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, name character varying, created_at timestamp with time zone)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT ci.id, ci.tenant_id, ci.child_id, ci.name, ci.created_at     FROM child_interests ci     WHERE ci.child_id = p_child_id AND ci.tenant_id = p_tenant_id     ORDER BY ci.created_at DESC; END; $function$ ;
