-- Function: public.sp_update_person_contact
-- Backup generated on 2026-10-25 20:34:13

CREATE OR REPLACE PROCEDURE public.sp_update_person_contact(IN p_tenant_id uuid, IN p_person_id uuid, IN p_mobile_phone character varying, IN p_preferred_contact_method character varying, IN p_preferred_language character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     UPDATE persons     SET mobile_phone = p_mobile_phone,         preferred_contact_method = p_preferred_contact_method,         preferred_language = p_preferred_language,         updated_at = NOW()     WHERE id = p_person_id AND tenant_id = p_tenant_id; END; $procedure$ ;
