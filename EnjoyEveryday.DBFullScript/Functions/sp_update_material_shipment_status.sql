-- Function: public.sp_update_material_shipment_status
-- Backup generated on 2026-10-25 20:34:12

CREATE OR REPLACE PROCEDURE public.sp_update_material_shipment_status(IN p_shipment_id uuid, IN p_delivery_status character varying, IN p_actual_delivery_date timestamp with time zone, IN p_proof_notes text)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE material_shipments SET         delivery_status = p_delivery_status,         actual_delivery_date = COALESCE(p_actual_delivery_date, actual_delivery_date),         proof_of_delivery_notes = COALESCE(p_proof_notes, proof_of_delivery_notes),         updated_at = NOW()     WHERE id = p_shipment_id; END; $procedure$ ;
