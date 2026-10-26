-- Function: public.sp_verify_emergency_contact
-- Backup generated on 2026-10-25 20:34:14

CREATE OR REPLACE PROCEDURE public.sp_verify_emergency_contact(IN p_tenant_id uuid, IN p_id uuid, IN p_verified_by character varying, IN p_verified_at timestamp with time zone, IN p_review_due_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE emergency_contacts SET         verification_status = 'Verified',         verified_by = p_verified_by,         verified_at = p_verified_at,         review_due_at = p_review_due_at,         updated_at = CURRENT_TIMESTAMP     WHERE tenant_id = p_tenant_id AND id = p_id; END; $procedure$ ;
