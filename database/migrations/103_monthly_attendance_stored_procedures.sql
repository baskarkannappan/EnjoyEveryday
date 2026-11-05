-- ============================================================================
-- Migration: 103_monthly_attendance_stored_procedures.sql
-- Description: Stored procedures for monthly child and teacher attendance registers
--             with branch/location context and role-based branch scoping.
-- ============================================================================

-- 1. Branch and Location Context for Monthly Attendance Header
CREATE OR REPLACE PROCEDURE public.sp_get_branch_location_context(
    p_tenant_id uuid,
    p_branch_id uuid,
    INOUT cur refcursor = 'cur_branch_context'
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    OPEN cur FOR
    SELECT 
        o.id AS organization_id,
        o.name::VARCHAR(200) AS organization_name,
        b.id AS branch_id,
        b.name::VARCHAR(200) AS branch_name,
        COALESCE(b.location, 'Main Campus')::VARCHAR(300) AS location
    FROM branches b
    JOIN organizations o ON b.organization_id = o.id
    WHERE b.id = p_branch_id
      AND b.tenant_id = p_tenant_id;
END;
$procedure$;

-- 2. Accessible Branches for Current Logged-in User
CREATE OR REPLACE PROCEDURE public.sp_get_user_accessible_branches(
    p_tenant_id uuid,
    p_user_id uuid,
    p_is_org_admin boolean,
    INOUT cur refcursor = 'cur_accessible_branches'
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    OPEN cur FOR
    WITH allowed_branches AS (
        SELECT b.id AS branch_id
        FROM branches b
        JOIN organizations o ON b.organization_id = o.id
        WHERE b.tenant_id = p_tenant_id
          AND b.is_active = true
          AND (
              -- Org Admin can access all branches in tenant
              p_is_org_admin = true
              -- Branch Admin / Staff explicitly assigned
              OR EXISTS (
                  SELECT 1 FROM user_branch_assignments uba
                  WHERE uba.user_id = p_user_id
                    AND uba.branch_id = b.id
                    AND uba.tenant_id = p_tenant_id
                    AND uba.revoked_at IS NULL
              )
              -- Teachers assigned to classrooms in this branch
              OR EXISTS (
                  SELECT 1 FROM classroom_teachers ct
                  JOIN classrooms cr ON ct.classroom_id = cr.id
                  WHERE ct.user_id = p_user_id
                    AND cr.branch_id = b.id
                    AND cr.tenant_id = p_tenant_id
              )
              -- Fallback: Staff with employment in this branch
              OR EXISTS (
                  SELECT 1 FROM teachers t
                  JOIN teacher_employments te ON t.id = te.teacher_id
                  WHERE t.user_id = p_user_id
                    AND te.branch_id = b.id
                    AND t.tenant_id = p_tenant_id
              )
          )
    )
    SELECT 
        o.id AS organization_id,
        o.name::VARCHAR(200) AS organization_name,
        b.id AS branch_id,
        b.name::VARCHAR(200) AS branch_name,
        COALESCE(b.location, 'Main Campus')::VARCHAR(300) AS location,
        EXISTS (
            SELECT 1 FROM user_branch_assignments uba 
            WHERE uba.user_id = p_user_id AND uba.branch_id = b.id
        ) AS is_primary
    FROM branches b
    JOIN organizations o ON b.organization_id = o.id
    JOIN allowed_branches ab ON b.id = ab.branch_id
    ORDER BY b.name;
END;
$procedure$;

-- 3. Monthly Child Attendance Register Data
CREATE OR REPLACE PROCEDURE public.sp_get_monthly_child_attendance(
    p_tenant_id uuid,
    p_branch_id uuid,
    p_year integer,
    p_month integer,
    p_classroom_id uuid DEFAULT NULL,
    INOUT cur refcursor = 'cur_monthly_child_attendance'
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_start_date date := make_date(p_year, p_month, 1);
    v_end_date date := (make_date(p_year, p_month, 1) + interval '1 month - 1 day')::date;
BEGIN
    OPEN cur FOR
    WITH eligible_children AS (
        SELECT DISTINCT c.id AS child_id,
               c.first_name::VARCHAR(100) AS first_name,
               c.last_name::VARCHAR(100) AS last_name,
               TRIM(c.first_name || ' ' || COALESCE(c.last_name, ''))::VARCHAR(200) AS full_name,
               c.photo_url,
               COALESCE(
                   c.classroom_id,
                   (SELECT ca2.classroom_id FROM child_attendance ca2 
                    WHERE ca2.child_id = c.id AND ca2.branch_id = p_branch_id AND ca2.classroom_id IS NOT NULL 
                    ORDER BY ca2.attendance_date DESC LIMIT 1),
                   (SELECT e.classroom_id FROM enrollments e 
                    WHERE e.child_id = c.id AND e.branch_id = p_branch_id AND e.classroom_id IS NOT NULL 
                    ORDER BY e.created_at DESC LIMIT 1)
               ) AS default_classroom_id
        FROM children c
        WHERE c.tenant_id = p_tenant_id
          AND (
              c.classroom_id IN (SELECT id FROM classrooms WHERE branch_id = p_branch_id)
              OR EXISTS (
                  SELECT 1 FROM child_attendance ca 
                  WHERE ca.child_id = c.id 
                    AND ca.branch_id = p_branch_id 
                    AND (p_classroom_id IS NULL OR ca.classroom_id = p_classroom_id)
              )
              OR EXISTS (
                  SELECT 1 FROM enrollments e 
                  WHERE e.child_id = c.id 
                    AND e.branch_id = p_branch_id 
                    AND (p_classroom_id IS NULL OR e.classroom_id = p_classroom_id)
              )
              OR EXISTS (
                  SELECT 1 FROM child_care_plans ccp 
                  WHERE ccp.child_id = c.id 
                    AND ccp.branch_id = p_branch_id
              )
          )
    ),
    deduped_attendance AS (
        SELECT DISTINCT ON (ca.child_id, ca.attendance_date)
            ca.id AS attendance_id,
            ca.child_id,
            ca.branch_id,
            ca.classroom_id,
            ca.attendance_date,
            EXTRACT(DAY FROM ca.attendance_date)::INTEGER AS day_of_month,
            ca.status::VARCHAR(50) AS status,
            ca.arrival_time,
            ca.departure_time,
            ca.notes
        FROM child_attendance ca
        WHERE ca.tenant_id = p_tenant_id
          AND ca.branch_id = p_branch_id
          AND ca.attendance_date >= v_start_date
          AND ca.attendance_date <= v_end_date
          AND (p_classroom_id IS NULL OR ca.classroom_id = p_classroom_id)
        ORDER BY ca.child_id, ca.attendance_date, ca.updated_at DESC, ca.created_at DESC
    )
    SELECT 
        ec.child_id,
        ec.first_name,
        ec.last_name,
        ec.full_name,
        ec.photo_url,
        COALESCE(da.classroom_id, ec.default_classroom_id) AS classroom_id,
        COALESCE(cr.name, 'General')::VARCHAR(150) AS classroom_name,
        da.attendance_id,
        da.attendance_date,
        da.day_of_month,
        da.status,
        da.arrival_time,
        da.departure_time,
        da.notes
    FROM eligible_children ec
    LEFT JOIN deduped_attendance da ON ec.child_id = da.child_id
    LEFT JOIN classrooms cr ON cr.id = COALESCE(da.classroom_id, ec.default_classroom_id)
    WHERE (p_classroom_id IS NULL OR COALESCE(da.classroom_id, ec.default_classroom_id) = p_classroom_id)
    ORDER BY ec.full_name, da.day_of_month NULLS LAST;
END;
$procedure$;

-- 4. Monthly Teacher Attendance Register Data
CREATE OR REPLACE PROCEDURE public.sp_get_monthly_teacher_attendance(
    p_tenant_id uuid,
    p_branch_id uuid,
    p_year integer,
    p_month integer,
    INOUT cur refcursor = 'cur_monthly_teacher_attendance'
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_start_date date := make_date(p_year, p_month, 1);
    v_end_date date := (make_date(p_year, p_month, 1) + interval '1 month - 1 day')::date;
BEGIN
    OPEN cur FOR
    WITH eligible_teachers AS (
        SELECT DISTINCT t.id AS teacher_id,
               t.employee_number::VARCHAR(50) AS employee_number,
               t.first_name::VARCHAR(100) AS first_name,
               t.last_name::VARCHAR(100) AS last_name,
               TRIM(t.first_name || ' ' || COALESCE(t.last_name, ''))::VARCHAR(200) AS full_name,
               t.profile_photo_url,
               COALESCE(te.job_title, 'Educator')::VARCHAR(100) AS job_title
        FROM teachers t
        LEFT JOIN user_branch_assignments uba ON t.user_id = uba.user_id AND uba.revoked_at IS NULL
        LEFT JOIN teacher_employments te ON t.id = te.teacher_id
        WHERE t.tenant_id = p_tenant_id
          AND (
              uba.branch_id = p_branch_id
              OR te.branch_id = p_branch_id
              OR EXISTS (
                  SELECT 1 FROM teacher_attendance ta 
                  WHERE ta.teacher_id = t.id AND ta.branch_id = p_branch_id
              )
              OR EXISTS (
                  SELECT 1 FROM classroom_teachers ct 
                  JOIN classrooms cr ON ct.classroom_id = cr.id 
                  WHERE ct.user_id = t.user_id AND cr.branch_id = p_branch_id
              )
          )
    ),
    deduped_attendance AS (
        SELECT DISTINCT ON (ta.teacher_id, ta.attendance_date)
            ta.id AS attendance_id,
            ta.teacher_id,
            ta.branch_id,
            ta.attendance_date,
            EXTRACT(DAY FROM ta.attendance_date)::INTEGER AS day_of_month,
            ta.status::VARCHAR(50) AS status,
            ta.clock_in_time,
            ta.clock_out_time,
            ta.notes
        FROM teacher_attendance ta
        WHERE ta.tenant_id = p_tenant_id
          AND ta.branch_id = p_branch_id
          AND ta.attendance_date >= v_start_date
          AND ta.attendance_date <= v_end_date
        ORDER BY ta.teacher_id, ta.attendance_date, ta.updated_at DESC, ta.created_at DESC
    )
    SELECT 
        et.teacher_id,
        et.employee_number,
        et.first_name,
        et.last_name,
        et.full_name,
        et.job_title,
        et.profile_photo_url,
        da.attendance_id,
        da.attendance_date,
        da.day_of_month,
        da.status,
        da.clock_in_time,
        da.clock_out_time,
        da.notes
    FROM eligible_teachers et
    LEFT JOIN deduped_attendance da ON et.teacher_id = da.teacher_id
    ORDER BY et.full_name, da.day_of_month NULLS LAST;
END;
$procedure$;
