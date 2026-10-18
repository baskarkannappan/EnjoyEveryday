-- 057_enrollment_pipeline.sql
-- Family Accounts & Enrollment Pipeline
-- Enables daycare operators to track inbound family interest, tour schedules, admissions review,
-- enrollment contracts, and tuition billing schedule linked directly to the child and family ledger.

CREATE TABLE IF NOT EXISTS enrollment_pipeline_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    branch_id UUID REFERENCES branches(id),
    child_first_name VARCHAR(100) NOT NULL,
    child_last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    gender VARCHAR(20),
    parent_guardian_name VARCHAR(150) NOT NULL,
    parent_email VARCHAR(150) NOT NULL,
    parent_phone VARCHAR(50) NOT NULL,
    target_classroom_id UUID REFERENCES classrooms(id),
    desired_start_date DATE,
    schedule_type VARCHAR(50) NOT NULL DEFAULT 'FullTime', -- FullTime, PartTime_MWF, PartTime_TTh, HalfDay_Morning
    pipeline_stage VARCHAR(50) NOT NULL DEFAULT 'Enquiry',  -- Enquiry, TourScheduled, ApplicationSubmitted, UnderReview, OfferSent, Enrolled, Waitlisted, Withdrawn
    tour_date TIMESTAMPTZ,
    tuition_monthly_rate NUMERIC(10,2) NOT NULL DEFAULT 850.00,
    deposit_amount NUMERIC(10,2) NOT NULL DEFAULT 200.00,
    sibling_discount_percent NUMERIC(5,2) DEFAULT 0,
    registration_fee NUMERIC(10,2) DEFAULT 50.00,
    internal_notes TEXT,
    child_id UUID REFERENCES children(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_enrollment_pipeline_tenant_stage ON enrollment_pipeline_applications(tenant_id, pipeline_stage);
CREATE INDEX IF NOT EXISTS idx_enrollment_pipeline_branch ON enrollment_pipeline_applications(branch_id);

CREATE TABLE IF NOT EXISTS family_billing_contracts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    branch_id UUID REFERENCES branches(id),
    application_id UUID REFERENCES enrollment_pipeline_applications(id) ON DELETE SET NULL,
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    parent_name VARCHAR(150) NOT NULL,
    parent_email VARCHAR(150) NOT NULL,
    schedule_type VARCHAR(50) NOT NULL DEFAULT 'FullTime',
    billing_cycle VARCHAR(50) NOT NULL DEFAULT 'Monthly', -- Monthly, BiWeekly, Weekly
    base_rate NUMERIC(10,2) NOT NULL,
    discount_percent NUMERIC(5,2) DEFAULT 0,
    effective_rate NUMERIC(10,2) NOT NULL,
    deposit_held NUMERIC(10,2) DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Paused, Terminated
    start_date DATE NOT NULL,
    end_date DATE,
    auto_invoice BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_family_contracts_tenant_child ON family_billing_contracts(tenant_id, child_id);

-- Seed Initial Realistic Pipeline Leads for standard branch/tenant
INSERT INTO enrollment_pipeline_applications (
    id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
    parent_guardian_name, parent_email, parent_phone, desired_start_date,
    schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
    sibling_discount_percent, registration_fee, internal_notes
)
SELECT 
    '11111111-e111-4000-8000-000000000001'::uuid,
    t.id,
    b.id,
    'Lucas', 'Vandermeer', CURRENT_DATE - INTERVAL '14 months', 'Male',
    'Elena Vandermeer', 'elena.v@example.com', '+1 (555) 234-8901', CURRENT_DATE + INTERVAL '14 days',
    'FullTime', 'TourScheduled', NOW() + INTERVAL '2 days', 950.00, 250.00,
    0, 50.00, 'Referred by Maya Peterson. Interested in nature-based toddler classroom.'
FROM tenants t
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO enrollment_pipeline_applications (
    id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
    parent_guardian_name, parent_email, parent_phone, desired_start_date,
    schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
    sibling_discount_percent, registration_fee, internal_notes
)
SELECT 
    '22222222-e222-4000-8000-000000000002'::uuid,
    t.id,
    b.id,
    'Sienna', 'Zhao', CURRENT_DATE - INTERVAL '26 months', 'Female',
    'Marcus Zhao', 'marcus.zhao@example.com', '+1 (555) 890-1234', CURRENT_DATE + INTERVAL '30 days',
    'PartTime_MWF', 'UnderReview', NOW() - INTERVAL '1 day', 620.00, 200.00,
    10.00, 50.00, 'Older sibling Oliver previously attended. 10% sibling discount applied.'
FROM tenants t
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO enrollment_pipeline_applications (
    id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
    parent_guardian_name, parent_email, parent_phone, desired_start_date,
    schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
    sibling_discount_percent, registration_fee, internal_notes
)
SELECT 
    '33333333-e333-4000-8000-000000000003'::uuid,
    t.id,
    b.id,
    'Noah', 'Kowalski', CURRENT_DATE - INTERVAL '8 months', 'Male',
    'Ania Kowalski', 'ania.k@example.com', '+1 (555) 432-5678', CURRENT_DATE + INTERVAL '45 days',
    'FullTime', 'OfferSent', NOW() - INTERVAL '4 days', 1100.00, 300.00,
    0, 50.00, 'Infant spot offered in Nestlings Room starting next month. Offer packet sent.'
FROM tenants t
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO enrollment_pipeline_applications (
    id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
    parent_guardian_name, parent_email, parent_phone, desired_start_date,
    schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
    sibling_discount_percent, registration_fee, internal_notes
)
SELECT 
    '44444444-e444-4000-8000-000000000004'::uuid,
    t.id,
    b.id,
    'Amara', 'Osei', CURRENT_DATE - INTERVAL '38 months', 'Female',
    'Kofi Osei', 'kofi.osei@example.com', '+1 (555) 777-3321', CURRENT_DATE + INTERVAL '10 days',
    'FullTime', 'Enquiry', NULL, 850.00, 200.00,
    0, 50.00, 'Web inquiry received via admissions contact form. Inquired about preschool art curriculum.'
FROM tenants t
JOIN branches b ON b.tenant_id = t.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;
