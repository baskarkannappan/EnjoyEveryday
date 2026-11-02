-- Migration 097: Dynamic Database-Backed Settings, Tenant Preferences, and Master Lookups

-- 1. Master Lookups Table
CREATE TABLE IF NOT EXISTS public.master_lookups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category VARCHAR(50) NOT NULL,
    code VARCHAR(50) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata_json JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_master_lookups_category_code UNIQUE (category, code)
);

CREATE INDEX IF NOT EXISTS idx_master_lookups_category ON public.master_lookups (category, is_active, display_order);

-- 2. Tenant Preferences Table
CREATE TABLE IF NOT EXISTS public.tenant_preferences (
    tenant_id UUID PRIMARY KEY REFERENCES public.tenants(id) ON DELETE CASCADE,
    currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    time_zone VARCHAR(100) NOT NULL DEFAULT 'Asia/Kolkata',
    operating_hours_json JSONB NOT NULL DEFAULT '{"openTime": "08:00", "closeTime": "18:00", "workingDays": ["Monday","Tuesday","Wednesday","Thursday","Friday"]}'::jsonb,
    statutory_ratios_json JSONB NOT NULL DEFAULT '{"Infant": 4, "Toddler": 7, "Preschool": 10, "Kindergarten": 15}'::jsonb,
    billing_rules_json JSONB NOT NULL DEFAULT '{"siblingDiscountPercent": 10.0, "latePickupFeePerMinute": 1.0, "gracePeriodMinutes": 15, "invoiceDueDays": 15}'::jsonb,
    safety_policies_json JSONB NOT NULL DEFAULT '{"napCheckIntervalMinutes": 15, "requireDualMedicationSignoff": false, "minEmergencyContacts": 2}'::jsonb,
    custom_settings_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 3. Stored Functions for Fast Dapper / Query Access
DROP FUNCTION IF EXISTS public.fn_get_active_master_lookups(VARCHAR);
CREATE OR REPLACE FUNCTION public.fn_get_active_master_lookups(
    p_category VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    category VARCHAR,
    code VARCHAR,
    display_name VARCHAR,
    display_order INT,
    is_active BOOLEAN,
    metadata_json JSONB
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ml.id, ml.category, ml.code, ml.display_name, ml.display_order, ml.is_active, ml.metadata_json
    FROM public.master_lookups ml
    WHERE ml.is_active = true
      AND (p_category IS NULL OR ml.category = p_category)
    ORDER BY ml.category, ml.display_order, ml.display_name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_tenant_preferences(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_tenant_preferences(
    p_tenant_id UUID
)
RETURNS TABLE (
    tenant_id UUID,
    currency VARCHAR,
    time_zone VARCHAR,
    operating_hours_json JSONB,
    statutory_ratios_json JSONB,
    billing_rules_json JSONB,
    safety_policies_json JSONB,
    custom_settings_json JSONB,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT tp.tenant_id, tp.currency, tp.time_zone,
           tp.operating_hours_json, tp.statutory_ratios_json,
           tp.billing_rules_json, tp.safety_policies_json,
           tp.custom_settings_json, tp.updated_at
    FROM public.tenant_preferences tp
    WHERE tp.tenant_id = p_tenant_id;
END;
$$;

DROP PROCEDURE IF EXISTS public.sp_upsert_tenant_preferences(UUID, VARCHAR, VARCHAR, JSONB, JSONB, JSONB, JSONB, JSONB);
CREATE OR REPLACE PROCEDURE public.sp_upsert_tenant_preferences(
    p_tenant_id UUID,
    p_currency VARCHAR,
    p_time_zone VARCHAR,
    p_operating_hours_json JSONB,
    p_statutory_ratios_json JSONB,
    p_billing_rules_json JSONB,
    p_safety_policies_json JSONB,
    p_custom_settings_json JSONB
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.tenant_preferences (
        tenant_id, currency, time_zone,
        operating_hours_json, statutory_ratios_json,
        billing_rules_json, safety_policies_json,
        custom_settings_json, updated_at
    ) VALUES (
        p_tenant_id, p_currency, p_time_zone,
        COALESCE(p_operating_hours_json, '{"openTime": "08:00", "closeTime": "18:00", "workingDays": ["Monday","Tuesday","Wednesday","Thursday","Friday"]}'::jsonb),
        COALESCE(p_statutory_ratios_json, '{"Infant": 4, "Toddler": 7, "Preschool": 10, "Kindergarten": 15}'::jsonb),
        COALESCE(p_billing_rules_json, '{"siblingDiscountPercent": 10.0, "latePickupFeePerMinute": 1.0, "gracePeriodMinutes": 15, "invoiceDueDays": 15}'::jsonb),
        COALESCE(p_safety_policies_json, '{"napCheckIntervalMinutes": 15, "requireDualMedicationSignoff": false, "minEmergencyContacts": 2}'::jsonb),
        COALESCE(p_custom_settings_json, '{}'::jsonb),
        now()
    )
    ON CONFLICT (tenant_id) DO UPDATE SET
        currency = EXCLUDED.currency,
        time_zone = EXCLUDED.time_zone,
        operating_hours_json = EXCLUDED.operating_hours_json,
        statutory_ratios_json = EXCLUDED.statutory_ratios_json,
        billing_rules_json = EXCLUDED.billing_rules_json,
        safety_policies_json = EXCLUDED.safety_policies_json,
        custom_settings_json = EXCLUDED.custom_settings_json,
        updated_at = now();
END;
$$;

-- 4. Pre-Seed Lookups
INSERT INTO public.master_lookups (category, code, display_name, display_order)
VALUES
    -- Development Domains
    ('DevelopmentDomain', 'Cognitive', 'Cognitive Exploration', 1),
    ('DevelopmentDomain', 'Physical', 'Gross & Fine Motor', 2),
    ('DevelopmentDomain', 'Emotional', 'Social & Emotional Wellbeing', 3),
    ('DevelopmentDomain', 'Language', 'Language & Early Literacy', 4),
    ('DevelopmentDomain', 'Sensory', 'Sensory & Nature Awakening', 5),

    -- Allergens
    ('Allergen', 'Peanuts', 'Peanuts', 1),
    ('Allergen', 'TreeNuts', 'Tree Nuts (Almonds, Walnuts, Cashews)', 2),
    ('Allergen', 'Dairy', 'Cow Milk & Dairy', 3),
    ('Allergen', 'Eggs', 'Eggs', 4),
    ('Allergen', 'Gluten', 'Gluten & Wheat', 5),
    ('Allergen', 'Soy', 'Soy', 6),
    ('Allergen', 'Shellfish', 'Crustaceans & Shellfish', 7),
    ('Allergen', 'Fish', 'Fish', 8),

    -- Event Categories
    ('EventCategory', 'FieldTrip', 'Field Trip & Excursion', 1),
    ('EventCategory', 'Celebration', 'Seasonal & Cultural Celebration', 2),
    ('EventCategory', 'ParentWorkshop', 'Family & Parent Workshop', 3),
    ('EventCategory', 'ParentTeacherMeeting', 'Pedagogy & Milestone Conference', 4),
    ('EventCategory', 'OpenHouse', 'Open House & Orientation', 5),
    ('EventCategory', 'SportsDay', 'Nature & Physical Play Day', 6),

    -- Incident Severity
    ('IncidentSeverity', 'Minor', 'Minor (First Aid on-site)', 1),
    ('IncidentSeverity', 'Moderate', 'Moderate (Requires Physician / Clinic)', 2),
    ('IncidentSeverity', 'Reportable', 'Statutory Reportable Incident', 3)
ON CONFLICT (category, code) DO NOTHING;

-- 5. Seed Preferences for all existing tenants
INSERT INTO public.tenant_preferences (tenant_id)
SELECT id FROM public.tenants
ON CONFLICT (tenant_id) DO NOTHING;
