-- Function: public.sp_update_material_loan_status
-- Backup generated on 2026-10-25 20:34:11

CREATE OR REPLACE PROCEDURE public.sp_update_material_loan_status(IN p_loan_id uuid, IN p_return_status character varying, IN p_actual_return_date timestamp with time zone DEFAULT NULL::timestamp with time zone, IN p_notes text DEFAULT NULL::text, IN p_deposit_status character varying DEFAULT NULL::character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE material_loans SET         return_status = p_return_status,         actual_return_date = COALESCE(p_actual_return_date, actual_return_date),         notes = COALESCE(p_notes, notes),         deposit_status = COALESCE(p_deposit_status, deposit_status),         updated_at = NOW()     WHERE id = p_loan_id; END; $procedure$ ;
