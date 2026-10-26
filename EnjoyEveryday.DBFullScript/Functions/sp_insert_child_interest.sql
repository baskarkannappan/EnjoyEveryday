-- Function: public.sp_insert_child_interest
-- Backup generated on 2026-10-25 20:34:01

CREATE OR REPLACE PROCEDURE public.sp_insert_child_interest(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_name character varying, IN p_created_at timestamp with time zone)  LANGUAGE plpgsql AS $procedure$ BEGIN     INSERT INTO child_interests (id, tenant_id, child_id, name, created_at)     VALUES (p_id, p_tenant_id, p_child_id, p_name, p_created_at); END; $procedure$ ;
