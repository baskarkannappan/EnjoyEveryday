-- Migration: 102_stored_procedures_auth_and_parent.sql
-- Description: Stored procedures for dev environment seeding and parent auth synchronization

-- 1. sp_seed_dev_parent_environment: Sets up mock parent, child, family relationship, and observations
CREATE OR REPLACE PROCEDURE public.sp_seed_dev_parent_environment(
    p_user_id UUID,
    p_tenant_id UUID,
    p_email VARCHAR,
    p_child_id UUID,
    p_security_stamp VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_teacher_id UUID;
BEGIN
    -- 1. Ensure Sunita Patel exists in users
    INSERT INTO users (
        id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active
    ) VALUES (
        p_user_id, p_tenant_id, p_email, UPPER(p_email), TRUE, 'Sunita', 'Patel', 'Sunita Patel', '', p_security_stamp, TRUE
    ) ON CONFLICT (id) DO UPDATE SET is_active = TRUE;

    -- 2. Ensure mock child Aarav Patel exists
    INSERT INTO children (id, tenant_id, first_name, last_name, date_of_birth, classroom_id, is_active)
    VALUES (p_child_id, p_tenant_id, 'Aarav', 'Patel', '2023-01-15', '40000000-0000-0000-0000-000000000001', TRUE)
    ON CONFLICT (id) DO UPDATE SET is_active = TRUE;

    -- 3. Ensure family relationship links Sunita Patel to Aarav Patel
    INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
    VALUES ('f0000000-0000-0000-0000-000000000001', p_tenant_id, p_child_id, p_user_id, 'Mother', TRUE, NOW(), 'Verified', NOW())
    ON CONFLICT (child_id, user_id) DO NOTHING;

    -- 4. Ensure person record exists for Family Profile
    INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, preferred_contact_method, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
    VALUES ('70000000-0000-0000-0000-000000000001', p_tenant_id, p_user_id, 'Sunita', 'Patel', 'Mother', p_email, '416-555-0199', 'Mobile', 'English', TRUE, TRUE, TRUE)
    ON CONFLICT (id) DO NOTHING;

    -- 5. Ensure child guardian record exists
    INSERT INTO child_guardians (id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care)
    VALUES ('80000000-0000-0000-0000-000000000001', p_tenant_id, p_child_id, '70000000-0000-0000-0000-000000000001', 'Mother', TRUE, TRUE, TRUE, TRUE, TRUE)
    ON CONFLICT (child_id, person_id) DO NOTHING;

    -- 6. Ensure sample journey entries and milestones exist
    SELECT id INTO v_teacher_id FROM teachers WHERE tenant_id = p_tenant_id LIMIT 1;
    IF v_teacher_id IS NOT NULL THEN
        INSERT INTO child_journey_entries (id, tenant_id, child_id, teacher_id, journey_type, title, observation, child_voice, observation_date, is_parent_visible, parent_visibility_status)
        VALUES ('90000000-0000-0000-0000-000000000001', p_tenant_id, p_child_id, v_teacher_id, 'Art & Sensory', 'Exploring Watercolor Rainbows', 'Aarav mixed colors and identified secondary hues.', 'Look, I made green!', NOW(), TRUE, 'Approved')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO child_milestones (id, tenant_id, child_id, teacher_id, title, description, milestone_date, parent_visible, status)
        VALUES ('91000000-0000-0000-0000-000000000001', p_tenant_id, p_child_id, v_teacher_id, 'Collaborative Tower Building', 'Constructed a stable 12-block tower with peers demonstrating turn-taking.', NOW(), TRUE, 'Active')
        ON CONFLICT (id) DO NOTHING;
    END IF;
END;
$$;


-- 2. sp_sync_parent_relationships: Auto-links children and creates person records for a verified parent
CREATE OR REPLACE PROCEDURE public.sp_sync_parent_relationships(
    p_user_id UUID,
    p_email VARCHAR,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_tenant_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- 1. Automatically link any active children whose enrollment draft_data matches this parent's email
    INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
    SELECT gen_random_uuid(), c.tenant_id, c.id, p_user_id, 
           COALESCE(NULLIF(TRIM((c.draft_data::jsonb)->>'parentRelationship'), ''), 'Parent'), 
           TRUE, NOW(), 'Verified', NOW()
    FROM children c
    WHERE c.is_active = TRUE
      AND (c.draft_data::jsonb)->>'parentEmail' ILIKE p_email
    ON CONFLICT (child_id, user_id) DO UPDATE 
    SET status = 'Verified', revoked_at = NULL, verified_at = NOW();

    -- 2. Also ensure person record exists for this parent
    INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
    SELECT gen_random_uuid(), c.tenant_id, p_user_id, p_first_name, p_last_name, 'Parent', p_email, 'English', TRUE, TRUE, TRUE
    FROM children c
    WHERE c.is_active = TRUE AND (c.draft_data::jsonb)->>'parentEmail' ILIKE p_email
      AND NOT EXISTS (SELECT 1 FROM persons WHERE tenant_id = c.tenant_id AND user_id = p_user_id);

    -- 3. In development / demo, automatically connect to sample child if no relationships exist
    IF NOT EXISTS (SELECT 1 FROM family_relationships WHERE user_id = p_user_id AND status = 'Verified' AND revoked_at IS NULL) THEN
        INSERT INTO family_relationships (id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at)
        SELECT gen_random_uuid(), p_tenant_id, c.id, p_user_id, 'Parent', TRUE, NOW(), 'Verified', NOW()
        FROM children c
        WHERE c.tenant_id = p_tenant_id
        LIMIT 1
        ON CONFLICT DO NOTHING;
    END IF;
END;
$$;


-- 3. sp_sync_parent_external_login: Handles user creation/sync and external login registration for parent portal
CREATE OR REPLACE PROCEDURE public.sp_sync_parent_external_login(
    IN p_provider VARCHAR,
    IN p_provider_key VARCHAR,
    IN p_email VARCHAR,
    IN p_first_name VARCHAR,
    IN p_last_name VARCHAR,
    IN p_display_name VARCHAR,
    IN p_default_tenant_id UUID,
    IN p_default_org_id UUID,
    INOUT io_user_id UUID DEFAULT NULL,
    INOUT io_tenant_id UUID DEFAULT NULL,
    INOUT io_is_active BOOLEAN DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_existing_user_id UUID;
    v_existing_tenant_id UUID;
    v_existing_is_active BOOLEAN;
    v_parent_role_id UUID;
BEGIN
    -- Check if provider key already linked
    SELECT user_id INTO v_existing_user_id
    FROM user_external_logins
    WHERE provider = p_provider AND provider_key = p_provider_key;

    IF v_existing_user_id IS NOT NULL THEN
        SELECT id, tenant_id, is_active
        INTO io_user_id, io_tenant_id, io_is_active
        FROM users
        WHERE id = v_existing_user_id;
        RETURN;
    END IF;

    -- Check if user exists by normalized email
    SELECT id, tenant_id, is_active
    INTO v_existing_user_id, v_existing_tenant_id, v_existing_is_active
    FROM users
    WHERE normalized_email = UPPER(p_email)
    LIMIT 1;

    IF v_existing_user_id IS NOT NULL THEN
        -- Reactivate if previously inactive or has active enrolled child
        UPDATE users
        SET is_active = TRUE, email_confirmed = TRUE, updated_at = NOW()
        WHERE id = v_existing_user_id;

        INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
        VALUES (p_provider, p_provider_key, v_existing_user_id, NOW())
        ON CONFLICT (provider, provider_key) DO NOTHING;

        io_user_id := v_existing_user_id;
        io_tenant_id := v_existing_tenant_id;
        io_is_active := TRUE;
    ELSE
        -- Auto-create new parent user account
        io_user_id := gen_random_uuid();
        io_tenant_id := p_default_tenant_id;
        io_is_active := TRUE;

        INSERT INTO users (
            id, tenant_id, email, normalized_email, email_confirmed,
            first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
        ) VALUES (
            io_user_id, io_tenant_id, p_email, UPPER(p_email), TRUE,
            p_first_name, p_last_name, p_display_name, '', gen_random_uuid()::varchar, TRUE, NOW(), NOW()
        );

        INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
        VALUES (p_provider, p_provider_key, io_user_id, NOW())
        ON CONFLICT (provider, provider_key) DO NOTHING;

        -- Ensure organization membership
        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
        VALUES (io_user_id, p_default_org_id, io_tenant_id, 'Active', NOW(), NOW())
        ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active';

        -- Assign Parent role
        SELECT id INTO v_parent_role_id
        FROM roles
        WHERE name = 'Parent' AND tenant_id = io_tenant_id
        LIMIT 1;

        IF v_parent_role_id IS NOT NULL THEN
            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
            VALUES (io_user_id, v_parent_role_id, p_default_org_id, io_tenant_id)
            ON CONFLICT DO NOTHING;
        END IF;

        -- Ensure person record
        INSERT INTO persons (id, tenant_id, user_id, first_name, last_name, relationship, email, preferred_language, has_portal_access, receives_daily_updates, receives_photos)
        SELECT gen_random_uuid(), io_tenant_id, io_user_id, p_first_name, p_last_name, 'Parent', p_email, 'English', TRUE, TRUE, TRUE
        WHERE NOT EXISTS (SELECT 1 FROM persons WHERE user_id = io_user_id);
    END IF;
END;
$$;

