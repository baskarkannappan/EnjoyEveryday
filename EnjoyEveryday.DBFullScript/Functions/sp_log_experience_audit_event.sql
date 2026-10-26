-- Function: public.sp_log_experience_audit_event
-- Backup generated on 2026-10-25 20:34:03

CREATE OR REPLACE PROCEDURE public.sp_log_experience_audit_event(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_platform_experience_id uuid, IN p_action character varying, IN p_performed_by_user_id uuid, IN p_details jsonb, IN p_created_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO experience_audit_events (         id, tenant_id, organization_id, platform_experience_id, action, performed_by_user_id, details, created_at     ) VALUES (         p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_action, p_performed_by_user_id, p_details, p_created_at     ); END; $procedure$ ;
