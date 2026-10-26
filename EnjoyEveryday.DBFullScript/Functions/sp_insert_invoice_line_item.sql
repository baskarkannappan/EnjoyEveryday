-- Function: public.sp_insert_invoice_line_item
-- Backup generated on 2026-10-25 20:34:02

CREATE OR REPLACE PROCEDURE public.sp_insert_invoice_line_item(IN p_id uuid, IN p_invoice_id uuid, IN p_line_number integer, IN p_description character varying, IN p_quantity numeric, IN p_unit_price numeric, IN p_line_total numeric, IN p_category character varying, IN p_created_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO invoice_line_items (         id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at     ) VALUES (         p_id, p_invoice_id, p_line_number, p_description, p_quantity, p_unit_price, p_line_total, p_category, p_created_at     ); END; $procedure$ ;
