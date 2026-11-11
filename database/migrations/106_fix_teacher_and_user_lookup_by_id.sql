-- Migration 106: Fix teacher and user lookup by ID for multi-organization users

-- 1. Support multi-organization membership in fn_get_user_by_id
CREATE OR REPLACE FUNCTION public.fn_get_user_by_id(p_tenant_id uuid, p_user_id uuid)
RETURNS TABLE(
    id uuid, 
    tenant_id uuid, 
    email character varying, 
    first_name character varying, 
    last_name character varying, 
    display_name character varying, 
    is_active boolean, 
    created_at timestamp with time zone, 
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.id = p_user_id 
      AND (
          p_tenant_id IS NULL 
          OR u.tenant_id = p_tenant_id 
          OR EXISTS (
              SELECT 1 FROM organization_memberships om 
              WHERE om.user_id = u.id AND om.tenant_id = p_tenant_id
          )
      );
END;
$$;

-- 2. Allow lookup by teacher.id OR teacher.user_id in fn_get_teacher_by_id
CREATE OR REPLACE FUNCTION public.fn_get_teacher_by_id(p_tenant_id uuid, p_id uuid)
RETURNS TABLE(
    id uuid, 
    tenant_id uuid, 
    person_id uuid, 
    user_id uuid, 
    employee_number character varying, 
    first_name character varying, 
    middle_name character varying, 
    last_name character varying, 
    preferred_name character varying, 
    display_name character varying, 
    profile_photo_url text, 
    date_of_birth date, 
    gender character varying, 
    preferred_language character varying, 
    other_languages text, 
    pronouns character varying, 
    bio text, 
    status character varying, 
    draft_data jsonb, 
    profile_completion_percentage integer, 
    created_at timestamp with time zone, 
    updated_at timestamp with time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT t.id, t.tenant_id, t.person_id, t.user_id, t.employee_number,
           t.first_name, t.middle_name, t.last_name, t.preferred_name,
           t.display_name, t.profile_photo_url, t.date_of_birth, t.gender,
           t.preferred_language, t.other_languages, t.pronouns, t.bio,
           t.status, t.draft_data, t.profile_completion_percentage,
           t.created_at, t.updated_at
    FROM teachers t
    WHERE (t.id = p_id OR t.user_id = p_id)
      AND (p_tenant_id IS NULL OR t.tenant_id = p_tenant_id)
    ORDER BY (CASE WHEN t.id = p_id THEN 0 ELSE 1 END), t.updated_at DESC
    LIMIT 1;
END;
$$;

