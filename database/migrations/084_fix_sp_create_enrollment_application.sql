-- ============================================================
-- Migration 084: Fix sp_create_enrollment_application and enrollment procedure parameter types
-- Updates DATE parameters to TIMESTAMPTZ so Npgsql / C# DateTime mappings match
-- PostgreSQL procedure signatures without type errors.
-- ============================================================

-- 1. Create enrollment application
CREATE OR REPLACE PROCEDURE sp_create_enrollment_application(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_child_first_name VARCHAR,
    p_child_last_name VARCHAR,
    p_date_of_birth TIMESTAMPTZ,
    p_gender VARCHAR,
    p_parent_guardian_name VARCHAR,
    p_parent_email VARCHAR,
    p_parent_phone VARCHAR,
    p_target_classroom_id UUID,
    p_desired_start_date TIMESTAMPTZ,
    p_schedule_type VARCHAR,
    p_pipeline_stage VARCHAR,
    p_tour_date TIMESTAMPTZ,
    p_tuition_monthly_rate NUMERIC,
    p_deposit_amount NUMERIC,
    p_sibling_discount_percent NUMERIC,
    p_registration_fee NUMERIC,
    p_internal_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO enrollment_pipeline_applications (
        id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
        parent_guardian_name, parent_email, parent_phone, target_classroom_id, desired_start_date,
        schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
        sibling_discount_percent, registration_fee, internal_notes
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_child_first_name, p_child_last_name, p_date_of_birth::DATE, p_gender,
        p_parent_guardian_name, p_parent_email, p_parent_phone, p_target_classroom_id, p_desired_start_date::DATE,
        p_schedule_type, p_pipeline_stage, p_tour_date, p_tuition_monthly_rate, p_deposit_amount,
        p_sibling_discount_percent, p_registration_fee, p_internal_notes
    );
END;
$$;

-- 2. Ensure child enrolled (accepts TIMESTAMPTZ for p_date_of_birth)
CREATE OR REPLACE PROCEDURE sp_ensure_child_enrolled(
    p_child_id UUID,
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth)
    VALUES (p_child_id, p_tenant_id, p_classroom_id, p_first_name, p_last_name, p_date_of_birth::DATE)
    ON CONFLICT (id) DO UPDATE
    SET classroom_id = EXCLUDED.classroom_id;
END;
$$;

-- 3. Create family billing contract (accepts TIMESTAMPTZ for p_start_date)
CREATE OR REPLACE PROCEDURE sp_create_family_billing_contract(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_application_id UUID,
    p_child_id UUID,
    p_parent_name VARCHAR,
    p_parent_email VARCHAR,
    p_schedule_type VARCHAR,
    p_billing_cycle VARCHAR,
    p_base_rate NUMERIC,
    p_discount_percent NUMERIC,
    p_effective_rate NUMERIC,
    p_deposit_held NUMERIC,
    p_start_date TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO family_billing_contracts (
        id, tenant_id, branch_id, application_id, child_id, parent_name, parent_email,
        schedule_type, billing_cycle, base_rate, discount_percent, effective_rate,
        deposit_held, start_date
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_application_id, p_child_id, p_parent_name, p_parent_email,
        p_schedule_type, p_billing_cycle, p_base_rate, p_discount_percent, p_effective_rate,
        p_deposit_held, p_start_date::DATE
    );
END;
$$;
