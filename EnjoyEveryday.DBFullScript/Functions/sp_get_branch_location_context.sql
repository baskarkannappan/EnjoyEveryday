-- Function: public.sp_get_branch_location_context
-- Backup generated on 2026-11-11 09:05:51

CREATE OR REPLACE PROCEDURE public.sp_get_branch_location_context(IN p_tenant_id uuid, IN p_branch_id uuid, INOUT cur refcursor DEFAULT 'cur_branch_context'::refcursor)  LANGUAGE plpgsql AS $procedure$ BEGIN     OPEN cur FOR     SELECT          o.id AS organization_id,         o.name::VARCHAR(200) AS organization_name,         b.id AS branch_id,         b.name::VARCHAR(200) AS branch_name,         COALESCE(b.location, 'Main Campus')::VARCHAR(300) AS location     FROM branches b     JOIN organizations o ON b.organization_id = o.id     WHERE b.id = p_branch_id       AND b.tenant_id = p_tenant_id; END; $procedure$ ;
