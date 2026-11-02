-- Migration 094: Enforce Platform Administrator Limit of 3 & Admin CRUD Procedures

-- 1. Helper Function: Get Current Platform Admin Count
CREATE OR REPLACE FUNCTION public.fn_get_platform_admin_count()
RETURNS integer
LANGUAGE sql
STABLE
AS $function$
    SELECT COUNT(DISTINCT ur.user_id)::integer
    FROM user_roles ur
    JOIN roles r ON ur.role_id = r.id
    WHERE r.name IN ('PlatformAdmin', 'SuperAdmin');
$function$;

-- 2. Procedure: sp_create_admin_user with Max 3 Platform Admin Enforcement
CREATE OR REPLACE PROCEDURE public.sp_create_admin_user(
    IN p_email character varying,
    IN p_first_name character varying,
    IN p_last_name character varying,
    IN p_password character varying,
    IN p_role_name character varying,
    IN p_org_id uuid DEFAULT NULL
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_user_id uuid;
    v_role_id uuid;
    v_tenant_id uuid;
    v_clean_email character varying;
    v_platform_admin_count integer;
BEGIN
    v_clean_email := lower(trim(p_email));
    
    -- Resolve tenant_id (if org provided, use org tenant_id; else default to primary platform tenant)
    IF p_org_id IS NOT NULL THEN
        SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_org_id;
        IF v_tenant_id IS NULL THEN v_tenant_id := p_org_id; END IF;
    END IF;

    IF v_tenant_id IS NULL THEN
        v_tenant_id := '10000000-0000-0000-0000-000000000001'::uuid;
    END IF;

    SELECT id INTO v_user_id FROM users WHERE lower(email) = v_clean_email;
    
    IF v_user_id IS NULL THEN
        v_user_id := gen_random_uuid();
        INSERT INTO users (
            id, tenant_id, email, normalized_email, first_name, last_name, display_name, 
            password_hash, is_active, created_at, updated_at
        )
        VALUES (
            v_user_id, 
            v_tenant_id,
            v_clean_email, 
            upper(v_clean_email), 
            p_first_name, 
            p_last_name, 
            trim(concat(p_first_name, ' ', p_last_name)),
            COALESCE(NULLIF(p_password, ''), 'TEMP_HASH'),
            TRUE, 
            NOW(), 
            NOW()
        );
    ELSE
        UPDATE users 
        SET first_name = COALESCE(NULLIF(p_first_name, ''), first_name),
            last_name = COALESCE(NULLIF(p_last_name, ''), last_name),
            display_name = trim(concat(COALESCE(NULLIF(p_first_name, ''), first_name), ' ', COALESCE(NULLIF(p_last_name, ''), last_name))),
            is_active = TRUE,
            updated_at = NOW()
        WHERE id = v_user_id;
    END IF;

    IF p_role_name IN ('PlatformAdmin', 'SuperAdmin') THEN
        -- Check count of platform administrators (excluding this user if already assigned)
        SELECT COUNT(DISTINCT ur.user_id) INTO v_platform_admin_count
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE r.name IN ('PlatformAdmin', 'SuperAdmin')
          AND ur.user_id != v_user_id;

        IF v_platform_admin_count >= 3 THEN
            RAISE EXCEPTION 'A maximum of 3 platform administrators can exist at any point in time. Please remove an existing platform administrator before creating a new one.';
        END IF;

        SELECT id INTO v_role_id FROM roles WHERE name = 'PlatformAdmin' LIMIT 1;
        IF v_role_id IS NOT NULL THEN
            INSERT INTO user_roles (user_id, role_id)
            VALUES (v_user_id, v_role_id)
            ON CONFLICT DO NOTHING;
        END IF;
    ELSE
        -- Organization Admin or Organization Owner
        SELECT id INTO v_role_id 
        FROM roles 
        WHERE name = p_role_name 
          AND (tenant_id = p_org_id OR tenant_id IS NULL)
        ORDER BY tenant_id NULLS LAST 
        LIMIT 1;

        IF p_org_id IS NOT NULL THEN
            SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_org_id;
            IF v_tenant_id IS NULL THEN v_tenant_id := p_org_id; END IF;

            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
            VALUES (v_user_id, p_org_id, v_tenant_id, 'Active')
            ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active';

            IF v_role_id IS NOT NULL THEN
                INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                VALUES (v_user_id, v_role_id, p_org_id, v_tenant_id)
                ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;

                INSERT INTO user_roles (user_id, role_id)
                VALUES (v_user_id, v_role_id)
                ON CONFLICT DO NOTHING;
            END IF;
        END IF;
    END IF;
END;
$procedure$;

-- 3. Procedure: sp_update_admin_user to Update Administrator Details and Role
CREATE OR REPLACE PROCEDURE public.sp_update_admin_user(
    IN p_user_id uuid,
    IN p_email character varying,
    IN p_first_name character varying,
    IN p_last_name character varying,
    IN p_password character varying,
    IN p_role_name character varying,
    IN p_account_status character varying,
    IN p_org_id uuid DEFAULT NULL
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_clean_email character varying;
    v_old_is_platform_admin boolean;
    v_new_is_platform_admin boolean;
    v_platform_admin_count integer;
    v_platform_role_id uuid;
    v_target_role_id uuid;
    v_tenant_id uuid;
    v_is_active boolean;
BEGIN
    v_clean_email := lower(trim(p_email));
    v_is_active := (lower(trim(p_account_status)) = 'active');
    
    SELECT id INTO v_platform_role_id FROM roles WHERE name = 'PlatformAdmin' LIMIT 1;

    -- Check if user currently has platform admin role
    SELECT EXISTS (
        SELECT 1 FROM user_roles 
        WHERE user_id = p_user_id 
          AND role_id IN (SELECT id FROM roles WHERE name IN ('PlatformAdmin', 'SuperAdmin'))
    ) INTO v_old_is_platform_admin;

    v_new_is_platform_admin := (p_role_name IN ('PlatformAdmin', 'SuperAdmin'));

    -- If promoting to platform admin from a non-platform admin role, check limit of 3
    IF v_new_is_platform_admin AND NOT v_old_is_platform_admin THEN
        SELECT COUNT(DISTINCT ur.user_id) INTO v_platform_admin_count
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE r.name IN ('PlatformAdmin', 'SuperAdmin');

        IF v_platform_admin_count >= 3 THEN
            RAISE EXCEPTION 'A maximum of 3 platform administrators can exist at any point in time. Please remove an existing platform administrator before promoting a new one.';
        END IF;
    END IF;

    -- If demoting from platform admin to an org role, ensure not the last one
    IF v_old_is_platform_admin AND NOT v_new_is_platform_admin THEN
        SELECT COUNT(DISTINCT ur.user_id) INTO v_platform_admin_count
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE r.name IN ('PlatformAdmin', 'SuperAdmin');

        IF v_platform_admin_count <= 1 THEN
            RAISE EXCEPTION 'Cannot demote the only remaining platform administrator. At least one platform administrator must exist.';
        END IF;
    END IF;

    -- Update users table
    UPDATE users 
    SET email = v_clean_email,
        normalized_email = upper(v_clean_email),
        first_name = p_first_name,
        last_name = p_last_name,
        display_name = trim(concat(p_first_name, ' ', p_last_name)),
        is_active = v_is_active,
        password_hash = CASE WHEN p_password IS NOT NULL AND trim(p_password) != '' THEN p_password ELSE password_hash END,
        updated_at = NOW()
    WHERE id = p_user_id;

    -- Handle role transitions
    IF v_new_is_platform_admin THEN
        IF v_platform_role_id IS NOT NULL THEN
            INSERT INTO user_roles (user_id, role_id)
            VALUES (p_user_id, v_platform_role_id)
            ON CONFLICT DO NOTHING;
        END IF;
    ELSE
        -- Remove platform admin role if previously held
        IF v_platform_role_id IS NOT NULL THEN
            DELETE FROM user_roles WHERE user_id = p_user_id AND role_id = v_platform_role_id;
        END IF;

        -- Assign target organization role
        IF p_org_id IS NOT NULL THEN
            SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_org_id;
            IF v_tenant_id IS NULL THEN v_tenant_id := p_org_id; END IF;

            SELECT id INTO v_target_role_id 
            FROM roles 
            WHERE name = p_role_name 
              AND (tenant_id = p_org_id OR tenant_id IS NULL)
            ORDER BY tenant_id NULLS LAST 
            LIMIT 1;

            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
            VALUES (p_user_id, p_org_id, v_tenant_id, p_account_status)
            ON CONFLICT (user_id, organization_id) DO UPDATE SET status = p_account_status;

            IF v_target_role_id IS NOT NULL THEN
                INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                VALUES (p_user_id, v_target_role_id, p_org_id, v_tenant_id)
                ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;

                INSERT INTO user_roles (user_id, role_id)
                VALUES (p_user_id, v_target_role_id)
                ON CONFLICT DO NOTHING;
            END IF;
        END IF;
    END IF;
END;
$procedure$;

-- 4. Procedure: sp_delete_admin_user to Delete / Revoke Administrator Access
CREATE OR REPLACE PROCEDURE public.sp_delete_admin_user(
    IN p_user_id uuid
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_is_platform_admin boolean;
    v_platform_admin_count integer;
BEGIN
    -- Check if user is a platform admin
    SELECT EXISTS (
        SELECT 1 FROM user_roles 
        WHERE user_id = p_user_id 
          AND role_id IN (SELECT id FROM roles WHERE name IN ('PlatformAdmin', 'SuperAdmin'))
    ) INTO v_is_platform_admin;

    IF v_is_platform_admin THEN
        SELECT COUNT(DISTINCT ur.user_id) INTO v_platform_admin_count
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE r.name IN ('PlatformAdmin', 'SuperAdmin');

        IF v_platform_admin_count <= 1 THEN
            RAISE EXCEPTION 'Cannot delete the only remaining platform administrator. At least one platform administrator must exist.';
        END IF;

        -- Remove Platform Admin role from user_roles
        DELETE FROM user_roles 
        WHERE user_id = p_user_id 
          AND role_id IN (SELECT id FROM roles WHERE name IN ('PlatformAdmin', 'SuperAdmin'));
    END IF;

    -- Remove organization roles, memberships, and external logins
    DELETE FROM user_organization_roles WHERE user_id = p_user_id;
    DELETE FROM organization_memberships WHERE user_id = p_user_id;
    DELETE FROM user_external_logins WHERE user_id = p_user_id;
    DELETE FROM user_roles WHERE user_id = p_user_id;

    -- Attempt deletion from users table; fallback to deactivation if referenced by operational foreign keys
    BEGIN
        DELETE FROM users WHERE id = p_user_id;
    EXCEPTION WHEN foreign_key_violation THEN
        UPDATE users SET is_active = FALSE, updated_at = NOW() WHERE id = p_user_id;
    END;
END;
$procedure$;

-- 5. Update fn_get_platform_organization_admins to return first_name and last_name
DROP FUNCTION IF EXISTS public.fn_get_platform_organization_admins();
CREATE OR REPLACE FUNCTION public.fn_get_platform_organization_admins()
 RETURNS TABLE(
     user_id uuid, 
     admin_name text, 
     admin_email character varying, 
     organization_id uuid, 
     organization_name character varying, 
     role_name character varying, 
     account_status text,
     first_name character varying,
     last_name character varying
 )
 LANGUAGE sql
 STABLE
AS $function$
    -- 1. All users holding PlatformAdmin or SuperAdmin role globally
    SELECT DISTINCT ON (u.id)
        u.id AS user_id,
        CASE
            WHEN u.display_name IS NOT NULL AND TRIM(u.display_name) != '' THEN TRIM(u.display_name)
            WHEN TRIM(CONCAT(u.first_name, ' ', u.last_name)) != '' THEN TRIM(CONCAT(u.first_name, ' ', u.last_name))
            WHEN u.email IS NOT NULL AND u.email != '' THEN INITCAP(SPLIT_PART(SPLIT_PART(u.email, '@', 1), '.', 1))
            ELSE 'Platform Administrator'
        END AS admin_name,
        u.email AS admin_email,
        '00000000-0000-0000-0000-000000000000'::uuid AS organization_id,
        'EnjoyEveryday Platform (Global)'::character varying AS organization_name,
        'Platform Administrator'::character varying AS role_name,
        CASE WHEN u.is_active THEN 'Active' ELSE 'Suspended' END AS account_status,
        COALESCE(u.first_name, '')::character varying AS first_name,
        COALESCE(u.last_name, '')::character varying AS last_name
    FROM users u
    JOIN user_roles ur ON ur.user_id = u.id
    JOIN roles r ON ur.role_id = r.id
    WHERE r.name IN ('PlatformAdmin', 'SuperAdmin')

    UNION ALL

    -- 2. Organization Owners, Admins, and Branch Admins
    SELECT DISTINCT ON (o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid))
        COALESCE(u.id, gen_random_uuid()) AS user_id,
        CASE
            WHEN u.display_name IS NOT NULL AND TRIM(u.display_name) != '' AND TRIM(u.display_name) != o.name THEN TRIM(u.display_name)
            WHEN TRIM(CONCAT(u.first_name, ' ', u.last_name)) != '' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != 'Admin' AND TRIM(CONCAT(u.first_name, ' ', u.last_name)) != o.name THEN TRIM(CONCAT(u.first_name, ' ', u.last_name))
            WHEN u.email IS NOT NULL AND u.email != '' THEN INITCAP(SPLIT_PART(SPLIT_PART(u.email, '@', 1), '.', 1))
            ELSE 'Daycare Director'
        END AS admin_name,
        COALESCE(u.email, 'admin@daycare.org') AS admin_email,
        o.id AS organization_id,
        o.name AS organization_name,
        COALESCE(r.name, 'Organization Admin') AS role_name,
        CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END AS account_status,
        COALESCE(u.first_name, '')::character varying AS first_name,
        COALESCE(u.last_name, '')::character varying AS last_name
    FROM organizations o
    LEFT JOIN organization_memberships om ON om.organization_id = o.id AND om.status = 'Active'
    LEFT JOIN users u ON u.id = om.user_id
    LEFT JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = o.id
    LEFT JOIN roles r ON r.id = uor.role_id
    WHERE r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin') OR r.name IS NULL
    ORDER BY 4, 1;
$function$;
