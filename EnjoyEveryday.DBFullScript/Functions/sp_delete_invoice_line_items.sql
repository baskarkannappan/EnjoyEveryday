-- Function: public.sp_delete_invoice_line_items
-- Backup generated on 2026-10-25 20:33:58

CREATE OR REPLACE PROCEDURE public.sp_delete_invoice_line_items(IN p_invoice_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM invoice_line_items WHERE invoice_id = p_invoice_id; END; $procedure$ ;
