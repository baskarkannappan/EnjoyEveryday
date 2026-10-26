-- Function: public.fn_get_inventory_locations
-- Backup generated on 2026-10-25 20:33:27

CREATE OR REPLACE FUNCTION public.fn_get_inventory_locations()  RETURNS TABLE(id uuid, code character varying, name character varying, address text, contact_phone character varying, is_central_warehouse boolean, is_active boolean, created_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at     FROM inventory_locations      WHERE is_active = TRUE      ORDER BY is_central_warehouse DESC, name ASC; $function$ ;
