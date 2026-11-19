-- Migration 130: Add fn_delete_platform_experience to allow discarding/deleting unpurchased platform experiences.
-- Experiences with existing marketplace_orders or organization_entitlements cannot be deleted and must be discontinued (Retired) instead.

CREATE OR REPLACE FUNCTION public.fn_delete_platform_experience(p_id uuid)
RETURNS TABLE(deleted boolean, blocking_reason text)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM platform_experiences WHERE id = p_id) THEN
        RETURN QUERY SELECT false, 'Platform experience not found.'::text;
        RETURN;
    END IF;

    IF EXISTS (SELECT 1 FROM marketplace_orders WHERE platform_experience_id = p_id)
       OR EXISTS (SELECT 1 FROM organization_entitlements WHERE platform_experience_id = p_id) THEN
        RETURN QUERY SELECT false, 'This experience has existing organization orders or entitlements and cannot be deleted. Use Discontinue instead.'::text;
        RETURN;
    END IF;

    DELETE FROM platform_experiences WHERE id = p_id;
    RETURN QUERY SELECT true, NULL::text;
END;
$$;

