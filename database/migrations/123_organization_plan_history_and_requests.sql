-- ============================================================================
-- Migration 123: Organization Plan History & Two-Way Plan Change Requests
-- 1. Tracks historical & current subscription plans per organization.
-- 2. Enables Org Admin -> Platform Admin -> Org Admin Acceptance workflow.
-- 3. Updates sp_assign_organization_regional_plan to automatically record history.
-- ============================================================================

-- 1. Organization Plan History Table
CREATE TABLE IF NOT EXISTS public.organization_plan_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    previous_plan_id UUID NULL,
    previous_plan_code VARCHAR(50) NULL,
    previous_plan_name VARCHAR(150) NULL,
    previous_tariff_id UUID NULL,
    previous_tariff_name VARCHAR(150) NULL,
    previous_rate NUMERIC(12,2) NULL,
    previous_currency VARCHAR(10) NULL,
    new_plan_id UUID NOT NULL,
    new_plan_code VARCHAR(50) NOT NULL,
    new_plan_name VARCHAR(150) NOT NULL,
    new_tariff_id UUID NULL,
    new_tariff_name VARCHAR(150) NULL,
    new_rate NUMERIC(12,2) NOT NULL DEFAULT 0,
    new_currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    max_branches INT NOT NULL DEFAULT 1,
    max_classrooms INT NOT NULL DEFAULT 3,
    max_children INT NOT NULL DEFAULT 35,
    includes_ai BOOLEAN NOT NULL DEFAULT FALSE,
    change_type VARCHAR(50) NOT NULL DEFAULT 'Initial',
    reason TEXT NULL,
    changed_by_user_id UUID NULL,
    changed_by_email VARCHAR(255) NULL,
    effective_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    effective_to TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_plan_history_org_date
    ON public.organization_plan_history (organization_id, effective_from DESC);

CREATE INDEX IF NOT EXISTS idx_org_plan_history_tenant
    ON public.organization_plan_history (tenant_id);

-- 2. Organization Plan Change Requests Table
CREATE TABLE IF NOT EXISTS public.organization_plan_change_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    organization_name VARCHAR(200) NOT NULL DEFAULT '',
    current_plan_id UUID NOT NULL,
    current_plan_name VARCHAR(150) NOT NULL DEFAULT '',
    current_tariff_id UUID NULL,
    current_rate NUMERIC(12,2) NOT NULL DEFAULT 0,
    current_currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    requested_plan_id UUID NOT NULL,
    requested_plan_name VARCHAR(150) NOT NULL DEFAULT '',
    requested_tariff_id UUID NULL,
    request_type VARCHAR(30) NOT NULL DEFAULT 'Upgrade',
    org_admin_notes TEXT NULL,
    proposed_plan_id UUID NULL,
    proposed_plan_name VARCHAR(150) NULL,
    proposed_tariff_id UUID NULL,
    proposed_tariff_name VARCHAR(150) NULL,
    proposed_rate NUMERIC(12,2) NULL,
    proposed_currency VARCHAR(10) NULL,
    proposed_effective_date TIMESTAMPTZ NULL,
    platform_admin_notes TEXT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'PendingPlatformReview',
    requested_by_user_id UUID NULL,
    requested_by_email VARCHAR(255) NULL,
    requested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reviewed_by_user_id UUID NULL,
    reviewed_by_email VARCHAR(255) NULL,
    reviewed_at TIMESTAMPTZ NULL,
    responded_by_user_id UUID NULL,
    responded_by_email VARCHAR(255) NULL,
    responded_at TIMESTAMPTZ NULL,
    org_response_notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_plan_change_req_org
    ON public.organization_plan_change_requests (organization_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_org_plan_change_req_status
    ON public.organization_plan_change_requests (status, created_at DESC);

-- 3. Backfill initial history for existing organizations that do not yet have any history
INSERT INTO public.organization_plan_history (
    tenant_id,
    organization_id,
    new_plan_id,
    new_plan_code,
    new_plan_name,
    new_tariff_id,
    new_tariff_name,
    new_rate,
    new_currency,
    max_branches,
    max_classrooms,
    max_children,
    includes_ai,
    change_type,
    reason,
    changed_by_email,
    effective_from
)
SELECT
    o.tenant_id,
    o.id,
    COALESCE(o.plan_id, '22222222-2222-2222-2222-222222222222'::UUID),
    COALESCE(NULLIF(o.plan_code, ''), 'PLAN-BLOOM'),
    COALESCE(NULLIF(o.plan_name, ''), 'Bloom (Growth)'),
    o.tariff_id,
    COALESCE(rpt.name, 'Monthly Standard'),
    COALESCE(rpt.price, 0),
    COALESCE(NULLIF(o.currency, ''), 'INR'),
    COALESCE(o.max_branches, 2),
    COALESCE(o.max_classrooms, 8),
    COALESCE(o.max_children, 90),
    COALESCE(o.includes_ai, TRUE),
    'Initial',
    'Initial organization subscription',
    'platform-admin@enjoyeveryday.com',
    COALESCE(o.created_at, NOW())
FROM public.organizations o
LEFT JOIN public.region_plan_tariffs rpt ON rpt.id = o.tariff_id
WHERE NOT EXISTS (
    SELECT 1 FROM public.organization_plan_history h WHERE h.organization_id = o.id
);

-- 4. Update sp_assign_organization_regional_plan to automatically maintain organization_plan_history
CREATE OR REPLACE PROCEDURE sp_assign_organization_regional_plan(
    p_org_id UUID,
    p_region_id UUID,
    p_plan_id UUID,
    p_tariff_id UUID,
    p_currency VARCHAR(10) DEFAULT NULL,
    p_change_type VARCHAR(50) DEFAULT 'PlatformAdjustment',
    p_reason TEXT DEFAULT 'Subscription plan updated',
    p_changed_by_user_id UUID DEFAULT NULL,
    p_changed_by_email VARCHAR(255) DEFAULT 'platform-admin@enjoyeveryday.com'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_region_code VARCHAR(50);
    v_region_name VARCHAR(150);
    v_region_currency VARCHAR(10);
    v_plan_code VARCHAR(50);
    v_plan_name VARCHAR(150);
    v_tariff_currency VARCHAR(10);
    v_tariff_name VARCHAR(150);
    v_tariff_price NUMERIC(12,2);
    v_resolved_currency VARCHAR(10);
    v_max_branches INT;
    v_max_classrooms INT;
    v_max_children INT;
    v_includes_ai BOOLEAN;
    v_tenant_id UUID;
    v_prev_plan_id UUID;
    v_prev_plan_code VARCHAR(50);
    v_prev_plan_name VARCHAR(150);
    v_prev_tariff_id UUID;
    v_prev_tariff_name VARCHAR(150);
    v_prev_rate NUMERIC(12,2);
    v_prev_currency VARCHAR(10);
    v_has_history BOOLEAN;
BEGIN
    -- Resolve Region & Region Currency
    SELECT code, name, currency INTO v_region_code, v_region_name, v_region_currency
    FROM pricing_regions
    WHERE id = p_region_id;

    -- Resolve Tariff details if tariff is specified
    IF p_tariff_id IS NOT NULL THEN
        SELECT currency, name, price INTO v_tariff_currency, v_tariff_name, v_tariff_price
        FROM region_plan_tariffs
        WHERE id = p_tariff_id;
    END IF;

    -- Snapshot previous state from organizations
    SELECT
        currency, tenant_id, plan_id, plan_code, plan_name, tariff_id
    INTO
        v_resolved_currency, v_tenant_id, v_prev_plan_id, v_prev_plan_code, v_prev_plan_name, v_prev_tariff_id
    FROM organizations
    WHERE id = p_org_id;

    IF v_prev_tariff_id IS NOT NULL THEN
        SELECT name, price INTO v_prev_tariff_name, v_prev_rate
        FROM region_plan_tariffs
        WHERE id = v_prev_tariff_id;
    END IF;
    v_prev_currency := v_resolved_currency;

    v_resolved_currency := COALESCE(
        NULLIF(TRIM(p_currency), ''),
        NULLIF(TRIM(v_tariff_currency), ''),
        NULLIF(TRIM(v_region_currency), ''),
        NULLIF(TRIM(v_resolved_currency), ''),
        'INR'
    );

    -- Resolve Plan limits
    IF p_plan_id = '11111111-1111-1111-1111-111111111111'::UUID THEN
        v_plan_code := 'PLAN-SPROUT';
        v_plan_name := 'Sprout (Starter)';
        v_max_branches := 1;
        v_max_classrooms := 3;
        v_max_children := 35;
        v_includes_ai := FALSE;
        v_tariff_price := COALESCE(v_tariff_price, 1499);
    ELSIF p_plan_id = '33333333-3333-3333-3333-333333333333'::UUID THEN
        v_plan_code := 'PLAN-CANOPY';
        v_plan_name := 'Canopy (Multi-Branch)';
        v_max_branches := 6;
        v_max_classrooms := 24;
        v_max_children := 300;
        v_includes_ai := TRUE;
        v_tariff_price := COALESCE(v_tariff_price, 6999);
    ELSE
        v_plan_code := 'PLAN-BLOOM';
        v_plan_name := 'Bloom (Growth)';
        v_max_branches := 2;
        v_max_classrooms := 8;
        v_max_children := 90;
        v_includes_ai := TRUE;
        v_tariff_price := COALESCE(v_tariff_price, 2999);
    END IF;

    -- Update Organization with Region, Plan Quotas, Tariff, and Currency
    UPDATE organizations
    SET
        pricing_region_id = p_region_id,
        pricing_region_code = v_region_code,
        pricing_region_name = v_region_name,
        plan_id = p_plan_id,
        plan_code = v_plan_code,
        plan_name = v_plan_name,
        max_branches = v_max_branches,
        max_classrooms = v_max_classrooms,
        max_children = v_max_children,
        includes_ai = v_includes_ai,
        tariff_id = p_tariff_id,
        currency = v_resolved_currency,
        updated_at = NOW()
    WHERE id = p_org_id;

    -- Propagate Currency to tenant_preferences
    IF v_tenant_id IS NOT NULL THEN
        UPDATE tenant_preferences
        SET currency = v_resolved_currency, updated_at = NOW()
        WHERE tenant_id = v_tenant_id;
    END IF;

    -- Record plan history if plan/tariff changed or no history exists yet
    SELECT EXISTS(SELECT 1 FROM organization_plan_history WHERE organization_id = p_org_id) INTO v_has_history;

    IF NOT v_has_history OR v_prev_plan_id IS DISTINCT FROM p_plan_id OR v_prev_tariff_id IS DISTINCT FROM p_tariff_id THEN
        UPDATE organization_plan_history
        SET effective_to = NOW()
        WHERE organization_id = p_org_id AND effective_to IS NULL;

        INSERT INTO organization_plan_history (
            tenant_id,
            organization_id,
            previous_plan_id,
            previous_plan_code,
            previous_plan_name,
            previous_tariff_id,
            previous_tariff_name,
            previous_rate,
            previous_currency,
            new_plan_id,
            new_plan_code,
            new_plan_name,
            new_tariff_id,
            new_tariff_name,
            new_rate,
            new_currency,
            max_branches,
            max_classrooms,
            max_children,
            includes_ai,
            change_type,
            reason,
            changed_by_user_id,
            changed_by_email,
            effective_from
        ) VALUES (
            COALESCE(v_tenant_id, '10000000-0000-0000-0000-000000000001'::UUID),
            p_org_id,
            CASE WHEN v_has_history THEN v_prev_plan_id ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_plan_code ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_plan_name ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_tariff_id ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_tariff_name ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_rate ELSE NULL END,
            CASE WHEN v_has_history THEN v_prev_currency ELSE NULL END,
            p_plan_id,
            v_plan_code,
            v_plan_name,
            p_tariff_id,
            COALESCE(v_tariff_name, 'Monthly Standard'),
            COALESCE(v_tariff_price, 0),
            v_resolved_currency,
            v_max_branches,
            v_max_classrooms,
            v_max_children,
            v_includes_ai,
            CASE WHEN NOT v_has_history THEN 'Initial' ELSE COALESCE(NULLIF(p_change_type, ''), 'PlatformAdjustment') END,
            p_reason,
            p_changed_by_user_id,
            p_changed_by_email,
            NOW()
        );
    END IF;
END;
$$;

