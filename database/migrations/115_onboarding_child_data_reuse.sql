-- ============================================================================
-- Migration 115: Onboarding Child Data Reuse & Profile Persistence
-- Carry over and pre-populate onboarding/admissions data directly into child profiles,
-- emergency contacts, medical profiles, allergies, and dietary records.
-- ============================================================================

-- 1. Procedure: sp_finalize_enrolled_child_profile
DROP PROCEDURE IF EXISTS public.sp_finalize_enrolled_child_profile(UUID, UUID, UUID, VARCHAR, VARCHAR, DATE, TEXT, INTEGER, VARCHAR, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR);

CREATE OR REPLACE PROCEDURE public.sp_finalize_enrolled_child_profile(
    IN p_child_id UUID,
    IN p_tenant_id UUID,
    IN p_classroom_id UUID,
    IN p_first_name VARCHAR,
    IN p_last_name VARCHAR,
    IN p_date_of_birth TIMESTAMP,
    IN p_draft_data TEXT,
    IN p_profile_completion_percentage INTEGER,
    IN p_emergency_name VARCHAR DEFAULT NULL,
    IN p_emergency_phone VARCHAR DEFAULT NULL,
    IN p_emergency_email VARCHAR DEFAULT NULL,
    IN p_medical_notes TEXT DEFAULT NULL,
    IN p_allergies VARCHAR DEFAULT NULL,
    IN p_dietary_restrictions VARCHAR DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_first_name_clean VARCHAR;
    v_last_name_clean VARCHAR;
    v_parts TEXT[];
BEGIN
    -- 1. Ensure child record with full draft_data and enrollment status
    INSERT INTO children (
        id, tenant_id, classroom_id, first_name, last_name, date_of_birth,
        enrollment_status, is_active, draft_data, profile_completion_percentage, created_at, updated_at
    ) VALUES (
        p_child_id, p_tenant_id, p_classroom_id, p_first_name, p_last_name, p_date_of_birth::DATE,
        'Enrolled', TRUE, p_draft_data, COALESCE(p_profile_completion_percentage, 90), NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET
        classroom_id = EXCLUDED.classroom_id,
        first_name = EXCLUDED.first_name,
        last_name = EXCLUDED.last_name,
        date_of_birth = EXCLUDED.date_of_birth,
        enrollment_status = 'Enrolled',
        draft_data = COALESCE(EXCLUDED.draft_data, children.draft_data),
        profile_completion_percentage = GREATEST(COALESCE(EXCLUDED.profile_completion_percentage, 90), COALESCE(children.profile_completion_percentage, 0)),
        is_active = TRUE,
        updated_at = NOW();

    -- 2. Populate emergency contacts if guardian info provided
    IF p_emergency_name IS NOT NULL AND TRIM(p_emergency_name) <> '' THEN
        v_parts := regexp_split_to_array(TRIM(p_emergency_name), '\s+');
        v_first_name_clean := v_parts[1];
        IF array_length(v_parts, 1) > 1 THEN
            v_last_name_clean := array_to_string(v_parts[2:], ' ');
        ELSE
            v_last_name_clean := 'Guardian';
        END IF;

        IF NOT EXISTS (SELECT 1 FROM emergency_contacts WHERE tenant_id = p_tenant_id AND child_id = p_child_id) THEN
            INSERT INTO emergency_contacts (
                id, tenant_id, child_id, first_name, last_name, relationship,
                phone, email, priority, is_authorized_pickup, notes, is_active, created_at, updated_at
            ) VALUES (
                gen_random_uuid(), p_tenant_id, p_child_id,
                v_first_name_clean, v_last_name_clean, 'Parent / Guardian',
                COALESCE(p_emergency_phone, ''), COALESCE(p_emergency_email, ''),
                1, TRUE, 'Primary contact provided during admissions onboarding.', TRUE, NOW(), NOW()
            );
        END IF;
    END IF;

    -- 3. Populate medical profile if notes or allergies exist
    IF (p_medical_notes IS NOT NULL AND TRIM(p_medical_notes) <> '') OR (p_allergies IS NOT NULL AND TRIM(p_allergies) <> '') THEN
        INSERT INTO child_medical_profiles (
            child_id, tenant_id, medical_notes, special_medical_needs, medical_consent
        ) VALUES (
            p_child_id, p_tenant_id,
            COALESCE(p_medical_notes, ''),
            COALESCE(p_allergies, p_medical_notes, ''),
            TRUE
        ) ON CONFLICT (child_id) DO UPDATE SET
            medical_notes = CASE WHEN EXCLUDED.medical_notes <> '' THEN EXCLUDED.medical_notes ELSE child_medical_profiles.medical_notes END,
            special_medical_needs = CASE WHEN EXCLUDED.special_medical_needs <> '' THEN EXCLUDED.special_medical_needs ELSE child_medical_profiles.special_medical_needs END,
            medical_consent = TRUE;
    END IF;

    -- 4. Populate child allergies if allergy detected
    IF p_allergies IS NOT NULL AND TRIM(p_allergies) <> '' THEN
        IF NOT EXISTS (
            SELECT 1 FROM child_allergies 
            WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND LOWER(allergen) = LOWER(TRIM(p_allergies))
        ) THEN
            INSERT INTO child_allergies (
                id, tenant_id, child_id, allergen, severity, has_epi_pen, notes
            ) VALUES (
                gen_random_uuid(), p_tenant_id, p_child_id,
                TRIM(p_allergies), 'Moderate', FALSE, 'Noted during admissions onboarding.'
            );
        END IF;
    END IF;

    -- 5. Populate child dietary profile if dietary restriction detected
    IF p_dietary_restrictions IS NOT NULL AND TRIM(p_dietary_restrictions) <> '' THEN
        INSERT INTO child_dietary_profiles (
            child_id, tenant_id, diet_type, food_restrictions, special_instructions, self_feeds
        ) VALUES (
            p_child_id, p_tenant_id,
            TRIM(p_dietary_restrictions),
            TRIM(p_dietary_restrictions),
            'Noted during admissions onboarding.',
            TRUE
        ) ON CONFLICT (child_id) DO UPDATE SET
            diet_type = EXCLUDED.diet_type,
            food_restrictions = EXCLUDED.food_restrictions;
    END IF;
END;
$$;

-- 2. Function: fn_get_enrollment_application_by_child_id
CREATE OR REPLACE FUNCTION public.fn_get_enrollment_application_by_child_id(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    application_id UUID,
    child_id UUID,
    child_first_name VARCHAR,
    child_last_name VARCHAR,
    date_of_birth DATE,
    gender VARCHAR,
    parent_guardian_name VARCHAR,
    parent_email VARCHAR,
    parent_phone VARCHAR,
    target_classroom_id UUID,
    desired_start_date DATE,
    schedule_type VARCHAR,
    pipeline_stage VARCHAR,
    tuition_monthly_rate NUMERIC,
    deposit_amount NUMERIC,
    sibling_discount_percent NUMERIC,
    internal_notes TEXT
)
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        epa.id AS application_id,
        epa.child_id,
        epa.child_first_name::VARCHAR,
        epa.child_last_name::VARCHAR,
        epa.date_of_birth,
        epa.gender::VARCHAR,
        epa.parent_guardian_name::VARCHAR,
        epa.parent_email::VARCHAR,
        epa.parent_phone::VARCHAR,
        epa.target_classroom_id,
        epa.desired_start_date,
        epa.schedule_type::VARCHAR,
        epa.pipeline_stage::VARCHAR,
        epa.tuition_monthly_rate,
        epa.deposit_amount,
        epa.sibling_discount_percent,
        epa.internal_notes
    FROM enrollment_pipeline_applications epa
    WHERE epa.tenant_id = p_tenant_id
      AND epa.child_id = p_child_id
    ORDER BY epa.created_at DESC
    LIMIT 1;
END;
$$;

