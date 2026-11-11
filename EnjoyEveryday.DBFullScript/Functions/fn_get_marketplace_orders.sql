-- Function: public.fn_get_marketplace_orders
-- Backup generated on 2026-11-11 09:05:16

CREATE OR REPLACE FUNCTION public.fn_get_marketplace_orders(p_organization_id uuid DEFAULT NULL::uuid)  RETURNS SETOF marketplace_orders  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM marketplace_orders     WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)     ORDER BY purchased_at DESC; $function$ ;
