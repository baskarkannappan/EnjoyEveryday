-- Function: public.fn_get_central_warehouse_location
-- Backup generated on 2026-11-11 09:04:48

CREATE OR REPLACE FUNCTION public.fn_get_central_warehouse_location()  RETURNS TABLE(id uuid, code character varying, name character varying, address text, contact_phone character varying, is_central_warehouse boolean, is_active boolean, created_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at     FROM inventory_locations      WHERE is_central_warehouse = TRUE AND is_active = TRUE      LIMIT 1; $function$ ;
