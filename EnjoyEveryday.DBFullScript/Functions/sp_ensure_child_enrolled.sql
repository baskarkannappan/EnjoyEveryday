-- Function: public.sp_ensure_child_enrolled
-- Backup generated on 2026-10-25 20:34:00

CREATE OR REPLACE PROCEDURE public.sp_ensure_child_enrolled(IN p_child_id uuid, IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_first_name character varying, IN p_last_name character varying, IN p_date_of_birth date)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth, enrollment_status, is_active, created_at, updated_at)     VALUES (p_child_id, p_tenant_id, p_classroom_id, p_first_name, p_last_name, p_date_of_birth, 'Enrolled', TRUE, NOW(), NOW())     ON CONFLICT (id) DO UPDATE SET         classroom_id = EXCLUDED.classroom_id,         enrollment_status = 'Enrolled',         updated_at = NOW(); END; $procedure$ ;
