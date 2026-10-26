-- Function: public.sp_log_audit_entry
-- Backup generated on 2026-10-25 20:34:03

CREATE OR REPLACE PROCEDURE public.sp_log_audit_entry(IN p_tenant_id uuid, IN p_user_id uuid, IN p_action character varying, IN p_entity_type character varying, IN p_entity_id uuid, IN p_details jsonb, IN p_ip_address character varying)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO audit_log (tenant_id, user_id, action, entity_type, entity_id, details, ip_address)     VALUES (         p_tenant_id,          CASE WHEN EXISTS (SELECT 1 FROM users WHERE id = p_user_id) THEN p_user_id ELSE NULL END,          p_action, p_entity_type, p_entity_id, p_details, p_ip_address     ); END; $procedure$ ;
