-- Function: public.fn_get_invoice_line_items
-- Backup generated on 2026-11-11 09:05:13

CREATE OR REPLACE FUNCTION public.fn_get_invoice_line_items(p_invoice_id uuid)  RETURNS TABLE(id uuid, invoice_id uuid, line_number integer, description character varying, quantity numeric, unit_price numeric, line_total numeric, category character varying, created_at timestamp with time zone)  LANGUAGE sql  STABLE AS $function$     SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at     FROM invoice_line_items     WHERE invoice_id = p_invoice_id     ORDER BY line_number ASC; $function$ ;
