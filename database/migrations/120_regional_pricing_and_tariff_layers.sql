-- ============================================================================
-- Migration 120: Regional Pricing Zones, Tariff Layers & Location Matching
-- ============================================================================

-- 1. Create pricing_regions Table
CREATE TABLE IF NOT EXISTS pricing_regions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    description TEXT,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    matched_cities JSONB NOT NULL DEFAULT '[]'::jsonb,
    matched_states JSONB NOT NULL DEFAULT '[]'::jsonb,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pricing_regions_code ON pricing_regions(code);
CREATE INDEX IF NOT EXISTS idx_pricing_regions_default ON pricing_regions(is_default) WHERE is_default = TRUE;

-- 2. Create region_plan_tariffs Table (Tariff Layer Schedules per Region and Plan)
CREATE TABLE IF NOT EXISTS region_plan_tariffs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    region_id UUID NOT NULL REFERENCES pricing_regions(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL,
    plan_code VARCHAR(50) NOT NULL,
    plan_name VARCHAR(150) NOT NULL,
    name VARCHAR(150) NOT NULL,
    frequency VARCHAR(30) NOT NULL DEFAULT 'Monthly', -- Monthly, Quarterly, Annual
    price NUMERIC(12, 2) NOT NULL,
    discount_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_region_plan_tariff UNIQUE (region_id, plan_id, frequency)
);

CREATE INDEX IF NOT EXISTS idx_region_plan_tariffs_region ON region_plan_tariffs(region_id);
CREATE INDEX IF NOT EXISTS idx_region_plan_tariffs_plan ON region_plan_tariffs(plan_id);

-- 3. Extend organizations Table with Regional Tariff Metadata
ALTER TABLE organizations 
    ADD COLUMN IF NOT EXISTS pricing_region_id UUID NULL REFERENCES pricing_regions(id),
    ADD COLUMN IF NOT EXISTS pricing_region_code VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS pricing_region_name VARCHAR(150) NULL,
    ADD COLUMN IF NOT EXISTS tariff_id UUID NULL;

CREATE INDEX IF NOT EXISTS idx_organizations_pricing_region ON organizations(pricing_region_id);

-- 4. Stored Function: Resolve Pricing Region by Location String
CREATE OR REPLACE FUNCTION fn_resolve_pricing_region(p_location TEXT)
RETURNS TABLE (
    id UUID,
    code VARCHAR(50),
    name VARCHAR(150),
    description TEXT,
    currency VARCHAR(10),
    matched_cities JSONB,
    matched_states JSONB,
    is_default BOOLEAN,
    is_active BOOLEAN
) AS $$
DECLARE
    v_clean_loc TEXT;
    v_matched_id UUID;
BEGIN
    v_clean_loc := LOWER(COALESCE(TRIM(p_location), ''));

    IF v_clean_loc != '' THEN
        -- Match city or state in JSONB arrays (case-insensitive substring check)
        SELECT pr.id INTO v_matched_id
        FROM pricing_regions pr
        WHERE pr.is_active = TRUE
          AND (
            EXISTS (
                SELECT 1 
                FROM jsonb_array_elements_text(pr.matched_cities) c 
                WHERE v_clean_loc LIKE '%' || LOWER(TRIM(c)) || '%'
            )
            OR
            EXISTS (
                SELECT 1 
                FROM jsonb_array_elements_text(pr.matched_states) s 
                WHERE v_clean_loc LIKE '%' || LOWER(TRIM(s)) || '%'
            )
          )
        ORDER BY pr.display_order ASC, pr.created_at ASC
        LIMIT 1;
    END IF;

    -- If no region matched, fallback to default region
    IF v_matched_id IS NULL THEN
        SELECT pr.id INTO v_matched_id
        FROM pricing_regions pr
        WHERE pr.is_active = TRUE AND pr.is_default = TRUE
        LIMIT 1;
    END IF;

    -- If still no default found, take any active region
    IF v_matched_id IS NULL THEN
        SELECT pr.id INTO v_matched_id
        FROM pricing_regions pr
        WHERE pr.is_active = TRUE
        ORDER BY pr.display_order ASC
        LIMIT 1;
    END IF;

    RETURN QUERY
    SELECT pr.id, pr.code, pr.name, pr.description, pr.currency, pr.matched_cities, pr.matched_states, pr.is_default, pr.is_active
    FROM pricing_regions pr
    WHERE pr.id = v_matched_id;
END;
$$ LANGUAGE plpgsql STABLE;

-- 5. Seed Initial Regional Pricing Zones & Tariff Layers
DO $$
DECLARE
    v_r_metro UUID := '70000000-0000-0000-0000-000000000001';
    v_r_growth UUID := '70000000-0000-0000-0000-000000000002';
    v_r_regional UUID := '70000000-0000-0000-0000-000000000003';
    v_r_default UUID := '70000000-0000-0000-0000-000000000004';

    v_p_sprout UUID := '11111111-1111-1111-1111-111111111111';
    v_p_bloom UUID := '22222222-2222-2222-2222-222222222222';
    v_p_canopy UUID := '33333333-3333-3333-3333-333333333333';
BEGIN
    -- Region 1: Tier 1 Metros (High Cost of Living)
    INSERT INTO pricing_regions (id, code, name, description, currency, matched_cities, matched_states, is_default, display_order)
    VALUES (
        v_r_metro,
        'REG-TIER1-METRO',
        'Tier 1 Metros & Global Hubs',
        'Premium high-density metropolitan markets with maximum operational demand.',
        'USD',
        '["New York", "San Francisco", "Seattle", "Boston", "Los Angeles", "Chicago", "London", "Toronto", "Sydney", "Mumbai", "Singapore"]'::jsonb,
        '["NY", "CA", "WA", "MA", "IL", "NSW", "ON", "MH"]'::jsonb,
        FALSE,
        1
    ) ON CONFLICT (code) DO UPDATE SET 
        name = EXCLUDED.name, 
        matched_cities = EXCLUDED.matched_cities, 
        matched_states = EXCLUDED.matched_states;

    -- Region 2: Tier 2 Growth Hubs
    INSERT INTO pricing_regions (id, code, name, description, currency, matched_cities, matched_states, is_default, display_order)
    VALUES (
        v_r_growth,
        'REG-TIER2-GROWTH',
        'Tier 2 Cities & Growth Hubs',
        'Fast-growing urban and commercial tech hubs with medium-high center density.',
        'USD',
        '["Austin", "Denver", "Atlanta", "Dallas", "Houston", "Minneapolis", "Portland", "Charlotte", "Bengaluru", "Pune", "Hyderabad", "Melbourne"]'::jsonb,
        '["TX", "CO", "GA", "MN", "OR", "NC", "KA", "TS"]'::jsonb,
        FALSE,
        2
    ) ON CONFLICT (code) DO UPDATE SET 
        name = EXCLUDED.name, 
        matched_cities = EXCLUDED.matched_cities, 
        matched_states = EXCLUDED.matched_states;

    -- Region 3: Suburban & Regional Markets
    INSERT INTO pricing_regions (id, code, name, description, currency, matched_cities, matched_states, is_default, display_order)
    VALUES (
        v_r_regional,
        'REG-SUBURBAN-EMERGING',
        'Suburban, Regional & Small Towns',
        'Affordable community pricing tailored for suburban daycare centers and emerging districts.',
        'USD',
        '["Madison", "Boise", "Des Moines", "Spokane", "Knoxville", "Fort Collins", "Bhopal", "Indore", "Jaipur", "Geelong"]'::jsonb,
        '["WI", "ID", "IA", "TN", "MP", "RJ"]'::jsonb,
        FALSE,
        3
    ) ON CONFLICT (code) DO UPDATE SET 
        name = EXCLUDED.name, 
        matched_cities = EXCLUDED.matched_cities, 
        matched_states = EXCLUDED.matched_states;

    -- Region 4: Global Standard (Default Fallback)
    INSERT INTO pricing_regions (id, code, name, description, currency, matched_cities, matched_states, is_default, display_order)
    VALUES (
        v_r_default,
        'REG-GLOBAL-DEFAULT',
        'Global Standard (Default)',
        'Standard baseline SaaS tariff schedule applied when no specific municipal rule matches.',
        'USD',
        '[]'::jsonb,
        '[]'::jsonb,
        TRUE,
        4
    ) ON CONFLICT (code) DO UPDATE SET 
        name = EXCLUDED.name, 
        is_default = TRUE;

    -- -------------------------------------------------------------
    -- Tariff Layers per Region & Plan:
    -- -------------------------------------------------------------
    -- Region 1 (Tier 1 Metros):
    -- Sprout ($219/mo, $189/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Metro Starter Monthly', 'Monthly', 219.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Metro Starter Annual', 'Annual', 186.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Bloom ($399/mo, $339/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Metro Growth Monthly', 'Monthly', 399.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Metro Growth Annual', 'Annual', 339.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Canopy ($799/mo, $679/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Metro Enterprise Monthly', 'Monthly', 799.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_metro, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Metro Enterprise Annual', 'Annual', 679.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Region 2 (Tier 2 Growth):
    -- Sprout ($169/mo, $145/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Growth Hub Monthly', 'Monthly', 169.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Growth Hub Annual', 'Annual', 145.00, 14)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Bloom ($299/mo, $249/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Growth Hub Monthly', 'Monthly', 299.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Growth Hub Annual', 'Annual', 249.00, 17)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Canopy ($599/mo, $499/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Growth Hub Enterprise Monthly', 'Monthly', 599.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_growth, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Growth Hub Enterprise Annual', 'Annual', 499.00, 17)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Region 3 (Suburban & Regional):
    -- Sprout ($119/mo, $99/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Community Monthly', 'Monthly', 119.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Community Annual', 'Annual', 99.00, 17)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Bloom ($199/mo, $169/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Community Growth Monthly', 'Monthly', 199.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Community Growth Annual', 'Annual', 169.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Canopy ($399/mo, $339/mo Annual)
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Community Network Monthly', 'Monthly', 399.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_regional, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Community Network Annual', 'Annual', 339.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Region 4 (Global Standard Default):
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Standard Monthly', 'Monthly', 149.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_sprout, 'PLAN-SPROUT', 'Sprout (Starter)', 'Standard Annual', 'Annual', 126.00, 15)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Standard Growth Monthly', 'Monthly', 299.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_bloom, 'PLAN-BLOOM', 'Bloom (Growth)', 'Standard Growth Annual', 'Annual', 249.00, 17)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Standard Network Monthly', 'Monthly', 599.00, 0)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;
    INSERT INTO region_plan_tariffs (region_id, plan_id, plan_code, plan_name, name, frequency, price, discount_percentage)
    VALUES (v_r_default, v_p_canopy, 'PLAN-CANOPY', 'Canopy (Multi-Branch)', 'Standard Network Annual', 'Annual', 499.00, 17)
    ON CONFLICT (region_id, plan_id, frequency) DO UPDATE SET price = EXCLUDED.price;

    -- Update existing organizations with resolved region
    UPDATE organizations o
    SET 
        pricing_region_id = r.id,
        pricing_region_code = r.code,
        pricing_region_name = r.name
    FROM (
        SELECT id, code, name FROM fn_resolve_pricing_region('Minneapolis, MN')
    ) r
    WHERE o.pricing_region_id IS NULL;

END $$;

-- 6. Recreate fn_get_platform_organizations_overview with Regional & Quota columns
DROP FUNCTION IF EXISTS fn_get_platform_organizations_overview();

CREATE OR REPLACE FUNCTION fn_get_platform_organizations_overview()
RETURNS TABLE(
    id uuid, 
    tenant_id uuid, 
    name character varying, 
    location character varying, 
    is_active boolean, 
    account_status character varying, 
    suspension_reason text, 
    created_at timestamp with time zone, 
    branch_count integer, 
    admin_email character varying,
    plan_id uuid,
    plan_code character varying,
    plan_name character varying,
    max_branches integer,
    max_classrooms integer,
    max_children integer,
    includes_ai boolean,
    pricing_region_id uuid,
    pricing_region_code character varying,
    pricing_region_name character varying,
    tariff_id uuid
) AS $$
BEGIN
    RETURN QUERY
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active,
           COALESCE(o.account_status, CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END)::varchar AS account_status,
           o.suspension_reason,
           o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1)::varchar AS admin_email,
           o.plan_id,
           COALESCE(o.plan_code, 'PLAN-BLOOM')::varchar AS plan_code,
           COALESCE(o.plan_name, 'Bloom (Growth)')::varchar AS plan_name,
           COALESCE(o.max_branches, 2)::int AS max_branches,
           COALESCE(o.max_classrooms, 8)::int AS max_classrooms,
           COALESCE(o.max_children, 90)::int AS max_children,
           COALESCE(o.includes_ai, TRUE)::boolean AS includes_ai,
           o.pricing_region_id,
           o.pricing_region_code::varchar,
           o.pricing_region_name::varchar,
           o.tariff_id
    FROM organizations o
    ORDER BY o.name;
END;
$$ LANGUAGE plpgsql STABLE;

-- 7. Stored Procedure: Assign Regional Plan and Tariff to Organization
CREATE OR REPLACE PROCEDURE sp_assign_organization_regional_plan(
    p_org_id UUID,
    p_region_id UUID,
    p_plan_id UUID,
    p_tariff_id UUID DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_region_code VARCHAR(50);
    v_region_name VARCHAR(150);
    v_plan_code VARCHAR(50);
    v_plan_name VARCHAR(150);
    v_max_branches INT;
    v_max_classrooms INT;
    v_max_children INT;
    v_includes_ai BOOLEAN;
BEGIN
    -- Resolve Region
    SELECT code, name INTO v_region_code, v_region_name
    FROM pricing_regions
    WHERE id = p_region_id;

    -- Resolve Plan limits (fallback standard defaults if not already present)
    IF p_plan_id = '11111111-1111-1111-1111-111111111111'::UUID THEN
        v_plan_code := 'PLAN-SPROUT';
        v_plan_name := 'Sprout (Starter)';
        v_max_branches := 1;
        v_max_classrooms := 3;
        v_max_children := 35;
        v_includes_ai := FALSE;
    ELSIF p_plan_id = '33333333-3333-3333-3333-333333333333'::UUID THEN
        v_plan_code := 'PLAN-CANOPY';
        v_plan_name := 'Canopy (Multi-Branch)';
        v_max_branches := 6;
        v_max_classrooms := 24;
        v_max_children := 300;
        v_includes_ai := TRUE;
    ELSE
        v_plan_code := 'PLAN-BLOOM';
        v_plan_name := 'Bloom (Growth)';
        v_max_branches := 2;
        v_max_classrooms := 8;
        v_max_children := 90;
        v_includes_ai := TRUE;
    END IF;

    -- Update Organization with Region, Plan Quotas, and Tariff
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
        updated_at = NOW()
    WHERE id = p_org_id;

END;
$$;

