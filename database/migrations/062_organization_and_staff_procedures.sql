-- Migration: 062_organization_and_staff_procedures.sql
-- Description: Stored procedures and functions for Organizations, Branches, Classrooms, Teachers, Staff Rostering, and Operations Metrics.

-- =========================================================================
-- 1. ORGANIZATION FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_organization_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_organization_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id AND o.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_organization_by_id_global(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_organization_by_id_global(
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_organizations(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_organizations(
    p_tenant_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organizations o
    WHERE o.tenant_id = p_tenant_id
    ORDER BY o.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_organization(UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_organization(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO organizations (id, tenant_id, name, location, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_name, p_location, p_is_active, p_created_at, p_updated_at)
    RETURNING organizations.id, organizations.tenant_id, organizations.name, organizations.location, organizations.is_active, organizations.created_at, organizations.updated_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_organization(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE organizations
    SET name = p_name,
        location = p_location,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_organization(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM organizations
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_organizations_by_user_id(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_organizations_by_user_id(
    p_user_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organization_memberships om
    JOIN organizations o ON om.organization_id = o.id
    WHERE om.user_id = p_user_id AND om.status = 'Active'
    ORDER BY o.name;
END;
$$;

-- =========================================================================
-- 2. BRANCH FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_branch_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_branch_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.tenant_id, b.organization_id, b.name, b.location, b.is_active, b.created_at, b.updated_at
    FROM branches b
    WHERE b.id = p_id AND b.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_branches_by_organization(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_branches_by_organization(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.tenant_id, b.organization_id, b.name, b.location, b.is_active, b.created_at, b.updated_at
    FROM branches b
    WHERE b.organization_id = p_organization_id AND b.tenant_id = p_tenant_id
    ORDER BY b.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_branch(UUID, UUID, UUID, VARCHAR, VARCHAR, BOOLEAN, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_branch(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    location VARCHAR,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_organization_id, p_name, p_location, p_is_active, p_created_at, p_updated_at)
    RETURNING branches.id, branches.tenant_id, branches.organization_id, branches.name, branches.location, branches.is_active, branches.created_at, branches.updated_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_branch(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_location VARCHAR,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE branches
    SET name = p_name,
        location = p_location,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_branch(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM branches
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 3. CLASSROOM FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_classroom_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_classroom_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    name VARCHAR,
    display_name VARCHAR,
    short_name VARCHAR,
    code VARCHAR,
    description TEXT,
    classroom_type VARCHAR,
    status VARCHAR,
    age_group VARCHAR,
    min_age_months INT,
    max_age_months INT,
    capacity INT,
    current_enrollment INT,
    environment VARCHAR,
    photo_url TEXT,
    draft_data JSONB,
    profile_completion_percentage INT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cl.id, cl.tenant_id, cl.branch_id, cl.name, cl.display_name, cl.short_name,
           cl.code, cl.description, cl.classroom_type, cl.status, cl.age_group,
           cl.min_age_months, cl.max_age_months, cl.capacity, cl.current_enrollment,
           cl.environment, cl.photo_url, cl.draft_data, cl.profile_completion_percentage,
           cl.is_active, cl.created_at, cl.created_by, cl.updated_at, cl.updated_by
    FROM classrooms cl
    WHERE cl.id = p_id AND cl.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_classrooms_by_branch(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_classrooms_by_branch(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    name VARCHAR,
    display_name VARCHAR,
    short_name VARCHAR,
    code VARCHAR,
    description TEXT,
    classroom_type VARCHAR,
    status VARCHAR,
    age_group VARCHAR,
    min_age_months INT,
    max_age_months INT,
    capacity INT,
    current_enrollment INT,
    environment VARCHAR,
    photo_url TEXT,
    draft_data JSONB,
    profile_completion_percentage INT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cl.id, cl.tenant_id, cl.branch_id, cl.name, cl.display_name, cl.short_name,
           cl.code, cl.description, cl.classroom_type, cl.status, cl.age_group,
           cl.min_age_months, cl.max_age_months, cl.capacity, cl.current_enrollment,
           cl.environment, cl.photo_url, cl.draft_data, cl.profile_completion_percentage,
           cl.is_active, cl.created_at, cl.created_by, cl.updated_at, cl.updated_by
    FROM classrooms cl
    WHERE cl.branch_id = p_branch_id AND cl.tenant_id = p_tenant_id AND cl.is_active = true
    ORDER BY cl.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_classrooms(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_classrooms(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    name VARCHAR,
    display_name VARCHAR,
    short_name VARCHAR,
    code VARCHAR,
    description TEXT,
    classroom_type VARCHAR,
    status VARCHAR,
    age_group VARCHAR,
    min_age_months INT,
    max_age_months INT,
    capacity INT,
    current_enrollment INT,
    environment VARCHAR,
    photo_url TEXT,
    draft_data JSONB,
    profile_completion_percentage INT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cl.id, cl.tenant_id, cl.branch_id, cl.name, cl.display_name, cl.short_name,
           cl.code, cl.description, cl.classroom_type, cl.status, cl.age_group,
           cl.min_age_months, cl.max_age_months, cl.capacity, cl.current_enrollment,
           cl.environment, cl.photo_url, cl.draft_data, cl.profile_completion_percentage,
           cl.is_active, cl.created_at, cl.created_by, cl.updated_at, cl.updated_by
    FROM classrooms cl
    JOIN branches b ON cl.branch_id = b.id AND cl.tenant_id = b.tenant_id
    WHERE cl.tenant_id = p_tenant_id AND cl.is_active = true
      AND (p_organization_id IS NULL OR b.organization_id = p_organization_id)
    ORDER BY cl.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_classroom(UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR, VARCHAR, INT, INT, INT, INT, VARCHAR, TEXT, JSONB, INT, BOOLEAN, TIMESTAMPTZ, UUID, TIMESTAMPTZ, UUID);
CREATE OR REPLACE FUNCTION public.fn_create_classroom(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_name VARCHAR,
    p_display_name VARCHAR,
    p_short_name VARCHAR,
    p_code VARCHAR,
    p_description TEXT,
    p_classroom_type VARCHAR,
    p_status VARCHAR,
    p_age_group VARCHAR,
    p_min_age_months INT,
    p_max_age_months INT,
    p_capacity INT,
    p_current_enrollment INT,
    p_environment VARCHAR,
    p_photo_url TEXT,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_created_by UUID,
    p_updated_at TIMESTAMPTZ,
    p_updated_by UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    name VARCHAR,
    display_name VARCHAR,
    short_name VARCHAR,
    code VARCHAR,
    description TEXT,
    classroom_type VARCHAR,
    status VARCHAR,
    age_group VARCHAR,
    min_age_months INT,
    max_age_months INT,
    capacity INT,
    current_enrollment INT,
    environment VARCHAR,
    photo_url TEXT,
    draft_data JSONB,
    profile_completion_percentage INT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO classrooms (
        id, tenant_id, branch_id, name, display_name, short_name, code, description, 
        classroom_type, status, age_group, min_age_months, max_age_months, capacity, 
        current_enrollment, environment, photo_url, draft_data, profile_completion_percentage, 
        is_active, created_at, created_by, updated_at, updated_by
    )
    VALUES (
        p_id, p_tenant_id, p_branch_id, p_name, p_display_name, p_short_name, p_code, p_description, 
        p_classroom_type, p_status, p_age_group, p_min_age_months, p_max_age_months, p_capacity, 
        p_current_enrollment, p_environment, p_photo_url, p_draft_data, p_profile_completion_percentage, 
        p_is_active, p_created_at, p_created_by, p_updated_at, p_updated_by
    )
    RETURNING classrooms.id, classrooms.tenant_id, classrooms.branch_id, classrooms.name, classrooms.display_name,
              classrooms.short_name, classrooms.code, classrooms.description, classrooms.classroom_type,
              classrooms.status, classrooms.age_group, classrooms.min_age_months, classrooms.max_age_months,
              classrooms.capacity, classrooms.current_enrollment, classrooms.environment, classrooms.photo_url,
              classrooms.draft_data, classrooms.profile_completion_percentage, classrooms.is_active,
              classrooms.created_at, classrooms.created_by, classrooms.updated_at, classrooms.updated_by;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_classroom(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_display_name VARCHAR,
    p_short_name VARCHAR,
    p_code VARCHAR,
    p_description TEXT,
    p_classroom_type VARCHAR,
    p_status VARCHAR,
    p_age_group VARCHAR,
    p_min_age_months INT,
    p_max_age_months INT,
    p_capacity INT,
    p_current_enrollment INT,
    p_environment VARCHAR,
    p_photo_url TEXT,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ,
    p_updated_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE classrooms
    SET name = p_name,
        display_name = p_display_name,
        short_name = p_short_name,
        code = p_code,
        description = p_description,
        classroom_type = p_classroom_type,
        status = p_status,
        age_group = p_age_group,
        min_age_months = p_min_age_months,
        max_age_months = p_max_age_months,
        capacity = p_capacity,
        current_enrollment = p_current_enrollment,
        environment = p_environment,
        photo_url = p_photo_url,
        draft_data = p_draft_data,
        profile_completion_percentage = p_profile_completion_percentage,
        is_active = p_is_active,
        updated_at = p_updated_at,
        updated_by = p_updated_by
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_classroom(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE classrooms SET is_active = false
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

-- =========================================================================
-- 4. TEACHER FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_teacher_by_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_teacher_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    person_id UUID,
    user_id UUID,
    employee_number VARCHAR,
    first_name VARCHAR,
    middle_name VARCHAR,
    last_name VARCHAR,
    preferred_name VARCHAR,
    display_name VARCHAR,
    profile_photo_url TEXT,
    date_of_birth DATE,
    gender VARCHAR,
    preferred_language VARCHAR,
    other_languages TEXT,
    pronouns VARCHAR,
    bio TEXT,
    status VARCHAR,
    draft_data JSONB,
    profile_completion_percentage INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
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
    WHERE t.id = p_id AND t.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_all_teachers(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_all_teachers(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    person_id UUID,
    user_id UUID,
    employee_number VARCHAR,
    first_name VARCHAR,
    middle_name VARCHAR,
    last_name VARCHAR,
    preferred_name VARCHAR,
    display_name VARCHAR,
    profile_photo_url TEXT,
    date_of_birth DATE,
    gender VARCHAR,
    preferred_language VARCHAR,
    other_languages TEXT,
    pronouns VARCHAR,
    bio TEXT,
    status VARCHAR,
    draft_data JSONB,
    profile_completion_percentage INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
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
    WHERE t.tenant_id = p_tenant_id AND t.status != 'Inactive'
      AND (p_organization_id IS NULL OR (
          (t.user_id IS NOT NULL AND EXISTS (
              SELECT 1 FROM organization_memberships om 
              WHERE om.user_id = t.user_id AND om.organization_id = p_organization_id AND om.status = 'Active'
          ))
          OR EXISTS (
              SELECT 1 FROM teacher_employments te
              JOIN branches tb ON te.branch_id = tb.id
              WHERE te.teacher_id = t.id AND tb.organization_id = p_organization_id
          )
      ))
    ORDER BY t.first_name, t.last_name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_create_teacher(UUID, UUID, UUID, UUID, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, DATE, VARCHAR, VARCHAR, TEXT, VARCHAR, TEXT, VARCHAR, JSONB, INT, TIMESTAMPTZ, TIMESTAMPTZ);
CREATE OR REPLACE FUNCTION public.fn_create_teacher(
    p_id UUID,
    p_tenant_id UUID,
    p_person_id UUID,
    p_user_id UUID,
    p_employee_number VARCHAR,
    p_first_name VARCHAR,
    p_middle_name VARCHAR,
    p_last_name VARCHAR,
    p_preferred_name VARCHAR,
    p_display_name VARCHAR,
    p_profile_photo_url TEXT,
    p_date_of_birth DATE,
    p_gender VARCHAR,
    p_preferred_language VARCHAR,
    p_other_languages TEXT,
    p_pronouns VARCHAR,
    p_bio TEXT,
    p_status VARCHAR,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    person_id UUID,
    user_id UUID,
    employee_number VARCHAR,
    first_name VARCHAR,
    middle_name VARCHAR,
    last_name VARCHAR,
    preferred_name VARCHAR,
    display_name VARCHAR,
    profile_photo_url TEXT,
    date_of_birth DATE,
    gender VARCHAR,
    preferred_language VARCHAR,
    other_languages TEXT,
    pronouns VARCHAR,
    bio TEXT,
    status VARCHAR,
    draft_data JSONB,
    profile_completion_percentage INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO teachers (
        id, tenant_id, person_id, user_id, employee_number, first_name, middle_name, last_name, 
        preferred_name, display_name, profile_photo_url, date_of_birth, gender, preferred_language, 
        other_languages, pronouns, bio, status, draft_data, profile_completion_percentage, 
        created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_person_id, p_user_id, p_employee_number, p_first_name, p_middle_name, p_last_name, 
        p_preferred_name, p_display_name, p_profile_photo_url, p_date_of_birth, p_gender, p_preferred_language, 
        p_other_languages, p_pronouns, p_bio, p_status, p_draft_data, p_profile_completion_percentage, 
        p_created_at, p_updated_at
    )
    RETURNING teachers.id, teachers.tenant_id, teachers.person_id, teachers.user_id, teachers.employee_number,
              teachers.first_name, teachers.middle_name, teachers.last_name, teachers.preferred_name,
              teachers.display_name, teachers.profile_photo_url, teachers.date_of_birth, teachers.gender,
              teachers.preferred_language, teachers.other_languages, teachers.pronouns, teachers.bio,
              teachers.status, teachers.draft_data, teachers.profile_completion_percentage,
              teachers.created_at, teachers.updated_at;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_update_teacher(
    p_id UUID,
    p_tenant_id UUID,
    p_person_id UUID,
    p_user_id UUID,
    p_employee_number VARCHAR,
    p_first_name VARCHAR,
    p_middle_name VARCHAR,
    p_last_name VARCHAR,
    p_preferred_name VARCHAR,
    p_display_name VARCHAR,
    p_profile_photo_url TEXT,
    p_date_of_birth DATE,
    p_gender VARCHAR,
    p_preferred_language VARCHAR,
    p_other_languages TEXT,
    p_pronouns VARCHAR,
    p_bio TEXT,
    p_status VARCHAR,
    p_draft_data JSONB,
    p_profile_completion_percentage INT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teachers
    SET person_id = p_person_id,
        user_id = p_user_id,
        employee_number = p_employee_number,
        first_name = p_first_name,
        middle_name = p_middle_name,
        last_name = p_last_name,
        preferred_name = p_preferred_name,
        display_name = p_display_name,
        profile_photo_url = p_profile_photo_url,
        date_of_birth = p_date_of_birth,
        gender = p_gender,
        preferred_language = p_preferred_language,
        other_languages = p_other_languages,
        pronouns = p_pronouns,
        bio = p_bio,
        status = p_status,
        draft_data = p_draft_data,
        profile_completion_percentage = p_profile_completion_percentage,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_delete_teacher(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE teachers SET status = 'Inactive'
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_is_teacher_assigned_to_classroom(UUID, UUID, UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_is_teacher_assigned_to_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_teacher_id UUID,
    p_user_id UUID DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM classroom_teachers ct
        WHERE ct.tenant_id = p_tenant_id 
          AND ct.classroom_id = p_classroom_id 
          AND (ct.user_id = p_teacher_id OR (p_user_id IS NOT NULL AND ct.user_id = p_user_id))
    );
END;
$$;

-- =========================================================================
-- 5. STAFF ROSTER FUNCTIONS & PROCEDURES
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_branch_brief(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_branch_brief(
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.name::VARCHAR
    FROM branches b
    WHERE b.id = p_branch_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_statutory_ratio_rules(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_statutory_ratio_rules(
    p_tenant_id UUID
)
RETURNS TABLE (
    age_group VARCHAR,
    max_ratio INT,
    description VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT csr.age_group::VARCHAR, csr.max_children_per_educator::INT, csr.description::VARCHAR
    FROM classroom_statutory_ratios csr
    WHERE (csr.tenant_id IS NULL OR csr.tenant_id = p_tenant_id) AND csr.is_active = TRUE;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_branch_classrooms(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_branch_classrooms(
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    name VARCHAR,
    room_type VARCHAR,
    capacity INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT cl.id, cl.name::VARCHAR, COALESCE(cl.classroom_type, cl.age_group, 'Classroom')::VARCHAR AS room_type, cl.capacity
    FROM classrooms cl
    WHERE cl.branch_id = p_branch_id AND cl.is_active = true
    ORDER BY cl.name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_child_attendance_counts(UUID, UUID, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_counts(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date DATE
)
RETURNS TABLE (
    classroom_id UUID,
    present_count INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ca.classroom_id, COUNT(*)::INT AS present_count
    FROM child_attendance ca
    WHERE ca.tenant_id = p_tenant_id AND ca.branch_id = p_branch_id AND ca.attendance_date = p_date
      AND (ca.status = 'Present' OR ca.status = 'Late')
      AND ca.departure_time IS NULL
    GROUP BY ca.classroom_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_teacher_attendance_summary(UUID, UUID, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_teacher_attendance_summary(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_date DATE
)
RETURNS TABLE (
    teacher_id UUID,
    status VARCHAR,
    clock_in_time TIME,
    clock_out_time TIME,
    teacher_name VARCHAR,
    classroom_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT ta.teacher_id, ta.status::VARCHAR, ta.clock_in_time, ta.clock_out_time,
           (t.first_name || ' ' || t.last_name)::VARCHAR AS teacher_name,
           ct.classroom_id
    FROM teacher_attendance ta
    JOIN teachers t ON ta.teacher_id = t.id
    LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
    WHERE ta.tenant_id = p_tenant_id AND ta.branch_id = p_branch_id AND ta.attendance_date = p_date
      AND (ta.status = 'Present' OR ta.status = 'Late')
      AND ta.clock_out_time IS NULL;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_branch_teachers(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_branch_teachers(
    p_tenant_id UUID,
    p_branch_id UUID
)
RETURNS TABLE (
    id UUID,
    teacher_name VARCHAR,
    classroom_id UUID,
    classroom_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT t.id, (t.first_name || ' ' || t.last_name)::VARCHAR AS teacher_name,
           ct.classroom_id, cr.name::VARCHAR AS classroom_name
    FROM teachers t
    JOIN user_branch_assignments uba ON t.user_id = uba.user_id
    LEFT JOIN classroom_teachers ct ON t.user_id = ct.user_id
    LEFT JOIN classrooms cr ON ct.classroom_id = cr.id
    WHERE uba.branch_id = p_branch_id AND t.tenant_id = p_tenant_id
    ORDER BY t.first_name;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_active_staff_leaves(UUID, DATE);
CREATE OR REPLACE FUNCTION public.fn_get_active_staff_leaves(
    p_tenant_id UUID,
    p_date DATE
)
RETURNS TABLE (
    id UUID,
    teacher_id UUID,
    leave_type VARCHAR,
    start_date DATE,
    end_date DATE,
    reason VARCHAR,
    status VARCHAR,
    coverage_status VARCHAR,
    substitute_teacher_id UUID,
    teacher_name VARCHAR,
    substitute_teacher_name VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT sl.id, sl.teacher_id, sl.leave_type::VARCHAR, sl.start_date, sl.end_date, sl.reason::VARCHAR,
           sl.status::VARCHAR, sl.coverage_status::VARCHAR, sl.substitute_teacher_id,
           (t.first_name || ' ' || t.last_name)::VARCHAR AS teacher_name,
           (sub.first_name || ' ' || sub.last_name)::VARCHAR AS substitute_teacher_name
    FROM staff_leaves sl
    JOIN teachers t ON sl.teacher_id = t.id
    LEFT JOIN teachers sub ON sl.substitute_teacher_id = sub.id
    WHERE sl.tenant_id = p_tenant_id
      AND sl.start_date <= p_date AND sl.end_date >= p_date
      AND sl.status != 'Cancelled' AND sl.status != 'Rejected'
    ORDER BY sl.start_date DESC;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_insert_staff_leave(
    p_id UUID,
    p_tenant_id UUID,
    p_teacher_id UUID,
    p_leave_type VARCHAR,
    p_start_date DATE,
    p_end_date DATE,
    p_reason VARCHAR,
    p_status VARCHAR,
    p_coverage_status VARCHAR,
    p_substitute_teacher_id UUID,
    p_reviewed_by_user_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO staff_leaves (
        id, tenant_id, teacher_id, leave_type, start_date, end_date, reason, status, coverage_status, substitute_teacher_id, reviewed_by_user_id, created_at
    )
    VALUES (
        p_id, p_tenant_id, p_teacher_id, p_leave_type, p_start_date, p_end_date, p_reason, p_status, p_coverage_status, p_substitute_teacher_id, p_reviewed_by_user_id, NOW()
    );
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_assign_substitute_coverage(
    p_tenant_id UUID,
    p_leave_id UUID,
    p_substitute_teacher_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE staff_leaves
    SET substitute_teacher_id = p_substitute_teacher_id,
        coverage_status = 'Covered'
    WHERE tenant_id = p_tenant_id AND id = p_leave_id;
END;
$$;

CREATE OR REPLACE PROCEDURE public.sp_dispatch_teacher_to_classroom(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_user_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
    VALUES (gen_random_uuid(), p_tenant_id, p_classroom_id, p_user_id, FALSE)
    ON CONFLICT (classroom_id, user_id) DO NOTHING;
END;
$$;

-- =========================================================================
-- 6. DASHBOARD & OPERATIONS METRICS FUNCTIONS
-- =========================================================================

DROP FUNCTION IF EXISTS public.fn_get_dashboard_organization_metrics(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_dashboard_organization_metrics(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    organization_id UUID,
    organization_name VARCHAR,
    total_branches INT,
    active_branch_count INT,
    total_classrooms INT,
    total_children INT,
    total_teachers INT,
    children_experiencing_today INT,
    experiences_this_week INT,
    experiences_this_week_completed INT,
    experiences_this_week_upcoming INT,
    teacher_created_experiences INT,
    teacher_created_experiences_this_month INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        o.id AS organization_id,
        o.name::VARCHAR AS organization_name,
        (SELECT COUNT(*)::INT FROM branches br WHERE br.organization_id = p_organization_id AND br.tenant_id = p_tenant_id) AS total_branches,
        (SELECT COUNT(*)::INT FROM branches br WHERE br.organization_id = p_organization_id AND br.tenant_id = p_tenant_id AND br.is_active = true) AS active_branch_count,
        (SELECT COUNT(*)::INT FROM classrooms cl JOIN branches br ON cl.branch_id = br.id WHERE br.organization_id = p_organization_id AND cl.tenant_id = p_tenant_id) AS total_classrooms,
        (SELECT COUNT(*)::INT FROM children ch WHERE ch.tenant_id = p_tenant_id AND ch.is_active = true) AS total_children,
        (SELECT COUNT(*)::INT FROM teachers tc WHERE tc.tenant_id = p_tenant_id AND tc.status != 'Inactive') AS total_teachers,
        (SELECT COUNT(DISTINCT c.id)::INT FROM children c JOIN experience_schedules es ON c.classroom_id = es.classroom_id WHERE es.tenant_id = p_tenant_id AND es.scheduled_date = CURRENT_DATE) AS children_experiencing_today,
        (SELECT COUNT(*)::INT FROM experience_schedules es WHERE es.tenant_id = p_tenant_id AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days') AS experiences_this_week,
        (SELECT COUNT(*)::INT FROM experience_schedules es WHERE es.tenant_id = p_tenant_id AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days' AND es.status IN ('Completed', 'Finished')) AS experiences_this_week_completed,
        (SELECT COUNT(*)::INT FROM experience_schedules es WHERE es.tenant_id = p_tenant_id AND es.scheduled_date >= CURRENT_DATE AND es.status NOT IN ('Completed', 'Finished')) AS experiences_this_week_upcoming,
        (SELECT COUNT(*)::INT FROM experiences exp WHERE exp.tenant_id = p_tenant_id) AS teacher_created_experiences,
        (SELECT COUNT(*)::INT FROM experiences exp WHERE exp.tenant_id = p_tenant_id AND exp.created_at >= date_trunc('month', CURRENT_DATE)) AS teacher_created_experiences_this_month
    FROM organizations o
    WHERE o.id = p_organization_id AND o.tenant_id = p_tenant_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_teacher_user_id(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_teacher_user_id(
    p_tenant_id UUID,
    p_teacher_id UUID
)
RETURNS UUID
LANGUAGE plpgsql
AS $$
DECLARE
    v_user_id UUID;
BEGIN
    SELECT t.user_id INTO v_user_id
    FROM teachers t
    WHERE t.id = p_teacher_id AND t.tenant_id = p_tenant_id;
    RETURN v_user_id;
END;
$$;

DROP FUNCTION IF EXISTS public.fn_get_dashboard_branch_metrics(UUID, UUID);
CREATE OR REPLACE FUNCTION public.fn_get_dashboard_branch_metrics(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    branch_id UUID,
    branch_name VARCHAR,
    location VARCHAR,
    status TEXT,
    classrooms INT,
    children INT,
    experiences INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        b.id AS branch_id, 
        b.name::VARCHAR AS branch_name, 
        b.location::VARCHAR AS location, 
        CASE WHEN b.is_active THEN 'Active'::TEXT ELSE 'Inactive'::TEXT END AS status,
        (SELECT COUNT(*)::INT FROM classrooms c WHERE c.branch_id = b.id AND c.tenant_id = p_tenant_id) AS classrooms,
        (SELECT COUNT(*)::INT FROM children ch JOIN classrooms c ON ch.classroom_id = c.id WHERE c.branch_id = b.id AND ch.tenant_id = p_tenant_id) AS children,
        (SELECT COUNT(*)::INT FROM experience_schedules es JOIN classrooms c ON es.classroom_id = c.id WHERE c.branch_id = b.id AND es.tenant_id = p_tenant_id) AS experiences
    FROM branches b
    WHERE b.organization_id = p_organization_id AND b.tenant_id = p_tenant_id
    ORDER BY b.name;
END;
$$;
