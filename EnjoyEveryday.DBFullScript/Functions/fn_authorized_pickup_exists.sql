-- Function: public.fn_authorized_pickup_exists
-- Backup generated on 2026-11-11 09:04:32

CREATE OR REPLACE FUNCTION public.fn_authorized_pickup_exists(p_tenant_id uuid, p_pickup_id uuid)  RETURNS boolean  LANGUAGE sql  STABLE AS $function$     SELECT EXISTS(SELECT 1 FROM authorized_pickups WHERE tenant_id = p_tenant_id AND id = p_pickup_id); $function$ ;
