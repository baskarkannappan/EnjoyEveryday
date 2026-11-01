-- Migration 088: Platform Admin Creation Procedure & Directory Integration

-- 1. Procedure to create or promote an administrator (PlatformAdmin or Daycare Org Admin/Owner)
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

-- 2. Update fn_get_platform_organization_admins to list Platform Admins globally alongside Daycare Organization Admins
CREATE OR REPLACE FUNCTION public.fn_get_platform_organization_admins()
 RETURNS TABLE(user_id uuid, admin_name text, admin_email character varying, organization_id uuid, organization_name character varying, role_name character varying, account_status text)
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
        CASE WHEN u.is_active THEN 'Active' ELSE 'Suspended' END AS account_status
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
        CASE WHEN o.is_active THEN 'Active' ELSE 'Suspended' END AS account_status
    FROM organizations o
    LEFT JOIN organization_memberships om ON om.organization_id = o.id AND om.status = 'Active'
    LEFT JOIN users u ON u.id = om.user_id
    LEFT JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = o.id
    LEFT JOIN roles r ON r.id = uor.role_id
    WHERE r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin') OR r.name IS NULL
    ORDER BY 4, 1;
$function$;
