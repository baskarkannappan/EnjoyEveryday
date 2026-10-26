-- Function: public.sp_delete_emergency_contact
-- Backup generated on 2026-10-25 20:33:57

CREATE OR REPLACE PROCEDURE public.sp_delete_emergency_contact(IN p_tenant_id uuid, IN p_id uuid)  LANGUAGE plpgsql AS $procedure$ BEGIN     DELETE FROM emergency_contacts WHERE tenant_id = p_tenant_id AND id = p_id; END; $procedure$ ;
