-- ============================================================================
-- Migration 112: Phase 1 - Guardians Management & Razorpay Webhook Idempotency
-- ============================================================================

-- 1. Razorpay Webhook Events Idempotency & Audit Table
CREATE TABLE IF NOT EXISTS public.razorpay_webhook_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    invoice_id UUID,
    payment_id VARCHAR(100),
    amount_in_paise BIGINT,
    currency VARCHAR(10) DEFAULT 'INR',
    status VARCHAR(50) NOT NULL DEFAULT 'Processed',
    payload JSONB,
    received_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    error_message TEXT,
    CONSTRAINT uq_razorpay_webhook_event_id UNIQUE (event_id)
);

CREATE INDEX IF NOT EXISTS idx_razorpay_webhook_event_id ON public.razorpay_webhook_events (event_id);
CREATE INDEX IF NOT EXISTS idx_razorpay_webhook_invoice_id ON public.razorpay_webhook_events (invoice_id);

-- 2. Stored Function: fn_get_child_guardians_for_admin
CREATE OR REPLACE FUNCTION public.fn_get_child_guardians_for_admin(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    person_id UUID,
    user_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    email VARCHAR,
    phone VARCHAR,
    relationship VARCHAR,
    is_primary BOOLEAN,
    can_pickup BOOLEAN,
    has_portal_access BOOLEAN,
    status VARCHAR,
    verified_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ
)
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        p.id AS person_id,
        p.user_id,
        p.first_name::VARCHAR,
        p.last_name::VARCHAR,
        COALESCE(p.email, u.email, '')::VARCHAR AS email,
        COALESCE(p.mobile_phone, p.alternate_phone, '')::VARCHAR AS phone,
        cg.relationship::VARCHAR,
        cg.is_primary,
        cg.can_pickup,
        (fr.id IS NOT NULL AND fr.status = 'Verified' AND fr.revoked_at IS NULL) AS has_portal_access,
        COALESCE(fr.status, 'Pending')::VARCHAR AS status,
        fr.verified_at,
        fr.revoked_at
    FROM public.child_guardians cg
    JOIN public.persons p ON cg.person_id = p.id
    LEFT JOIN public.users u ON p.user_id = u.id
    LEFT JOIN public.family_relationships fr ON fr.child_id = cg.child_id AND fr.user_id = p.user_id
    WHERE cg.tenant_id = p_tenant_id 
      AND cg.child_id = p_child_id
    ORDER BY cg.is_primary DESC, p.first_name, p.last_name;
END;
$$;

-- 3. Stored Procedure: sp_revoke_child_guardian
CREATE OR REPLACE PROCEDURE public.sp_revoke_child_guardian(
    p_tenant_id UUID,
    p_child_id UUID,
    p_user_id UUID,
    p_revoked_by UUID DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_person_id UUID;
BEGIN
    SELECT id INTO v_person_id 
    FROM public.persons 
    WHERE tenant_id = p_tenant_id AND user_id = p_user_id 
    LIMIT 1;

    -- Revoke portal access in family_relationships
    UPDATE public.family_relationships
    SET status = 'Revoked',
        revoked_at = NOW(),
        revoked_by = p_revoked_by
    WHERE tenant_id = p_tenant_id 
      AND child_id = p_child_id 
      AND user_id = p_user_id;

    -- Deactivate pickup and communication permissions if person exists
    IF v_person_id IS NOT NULL THEN
        UPDATE public.child_guardians
        SET can_pickup = FALSE,
            can_receive_information = FALSE,
            can_authorize_medical_care = FALSE
        WHERE tenant_id = p_tenant_id 
          AND child_id = p_child_id 
          AND person_id = v_person_id;

        UPDATE public.authorized_pickups
        SET is_active = FALSE
        WHERE tenant_id = p_tenant_id 
          AND child_id = p_child_id 
          AND person_id = v_person_id;
    END IF;
END;
$$;

