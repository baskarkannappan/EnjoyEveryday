-- Function: public.fn_get_marketplace_order_by_transaction_ref
-- Backup generated on 2026-10-25 20:33:30

CREATE OR REPLACE FUNCTION public.fn_get_marketplace_order_by_transaction_ref(p_organization_id uuid, p_transaction_reference character varying)  RETURNS SETOF marketplace_orders  LANGUAGE sql  STABLE AS $function$     SELECT *     FROM marketplace_orders     WHERE organization_id = p_organization_id AND transaction_reference = p_transaction_reference; $function$ ;
