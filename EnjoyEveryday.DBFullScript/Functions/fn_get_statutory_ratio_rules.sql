-- Function: public.fn_get_statutory_ratio_rules
-- Backup generated on 2026-10-25 20:33:41

CREATE OR REPLACE FUNCTION public.fn_get_statutory_ratio_rules(p_tenant_id uuid)  RETURNS TABLE(age_group character varying, max_ratio integer, description character varying)  LANGUAGE plpgsql AS $function$ BEGIN     RETURN QUERY     SELECT csr.age_group::VARCHAR, csr.max_children_per_educator::INT, csr.description::VARCHAR     FROM classroom_statutory_ratios csr     WHERE (csr.tenant_id IS NULL OR csr.tenant_id = p_tenant_id) AND csr.is_active = TRUE; END; $function$ ;
