-- ============================================================
-- Migration 067: Parent Portal & Daycare Operations Board Procedures
-- Replaces remaining inline queries in:
--   - ParentRepository
--   - DaycareOperationsBoardRepository
-- ============================================================

-- ------------------------------------------------------------
-- 1. PARENT PORTAL ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_child_attendance_for_date(
    p_child_id UUID,
    p_date DATE
)
RETURNS SETOF child_attendance
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM child_attendance
    WHERE child_id = p_child_id AND attendance_date = p_date
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_parent_approved_journey_entries(
    p_child_id UUID
)
RETURNS SETOF child_journey_entries
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM child_journey_entries
    WHERE child_id = p_child_id 
      AND is_parent_visible = true
    ORDER BY observation_date DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_parent_child_photo_consent(
    p_child_id UUID
)
RETURNS VARCHAR
LANGUAGE sql
STABLE
AS $$
    SELECT d.status
    FROM child_consent_documents d
    LEFT JOIN consent_templates t ON d.consent_template_id = t.id
    WHERE d.child_id = p_child_id
      AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
    ORDER BY d.created_at DESC
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_parent_journey_evidence(
    p_entry_ids UUID[]
)
RETURNS TABLE (
    journey_entry_id UUID,
    text_content TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT journey_entry_id, text_content
    FROM child_journey_evidence
    WHERE journey_entry_id = ANY(p_entry_ids)
      AND evidence_type = 'Photo'
      AND (visibility = 'Parent' OR visibility = 'Public')
      AND text_content IS NOT NULL AND text_content != ''
    ORDER BY created_at ASC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_parent_approved_milestones(
    p_child_id UUID
)
RETURNS SETOF child_milestones
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM child_milestones
    WHERE child_id = p_child_id 
      AND parent_visible = true
    ORDER BY milestone_date DESC;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_parent_approved_events_for_child(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS SETOF events
LANGUAGE sql
STABLE
AS $$
    SELECT DISTINCT e.*
    FROM events e
    JOIN children c ON c.id = p_child_id
    LEFT JOIN event_classrooms ec ON e.id = ec.event_id
    WHERE (e.tenant_id = c.tenant_id OR e.tenant_id = p_tenant_id)
      AND e.status IN ('Published', 'InProgress', 'Completed')
      AND (
          e.is_organization_wide = true
          OR ec.classroom_id = c.classroom_id
          OR e.branch_id IN (SELECT cl.branch_id FROM classrooms cl WHERE cl.id = c.classroom_id)
      )
    ORDER BY e.event_date DESC, e.start_time;
$$;

-- ------------------------------------------------------------
-- 2. DAYCARE OPERATIONS BOARD ROUTINES
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.fn_get_board_default_organization_id(
    p_tenant_id UUID
)
RETURNS UUID
LANGUAGE sql
STABLE
AS $$
    SELECT id
    FROM organizations
    WHERE tenant_id = p_tenant_id AND is_active = true
    ORDER BY name
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_organization_name(
    p_tenant_id UUID,
    p_org_id UUID
)
RETURNS VARCHAR
LANGUAGE sql
STABLE
AS $$
    SELECT name
    FROM organizations
    WHERE id = p_org_id AND tenant_id = p_tenant_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_classrooms(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    age_group VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    capacity INT,
    enrolled_children INT,
    present_children INT,
    expected_children INT,
    present_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        cl.id AS classroom_id,
        cl.name AS classroom_name,
        cl.age_group AS age_group,
        cl.branch_id AS branch_id,
        b.name AS branch_name,
        cl.capacity AS capacity,
        cl.current_enrollment AS enrolled_children,
        COALESCE((
            SELECT COUNT(1)::int FROM child_attendance ca
            WHERE ca.classroom_id = cl.id AND ca.attendance_date = p_date 
              AND ca.status IN ('Present', 'Late') AND ca.tenant_id = p_tenant_id
        ), 0) AS present_children,
        COALESCE((
            SELECT COUNT(1)::int FROM children ch
            WHERE ch.classroom_id = cl.id AND ch.is_active = true AND ch.tenant_id = p_tenant_id
        ), 0) AS expected_children,
        COALESCE((
            SELECT COUNT(1)::int FROM teacher_attendance ta
            WHERE ta.branch_id = cl.branch_id AND ta.attendance_date = p_date
              AND ta.status = 'Present' AND ta.tenant_id = p_tenant_id
        ), 0) AS present_staff
    FROM classrooms cl
    JOIN branches b ON cl.branch_id = b.id
    WHERE cl.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND cl.is_active = true
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
    ORDER BY b.name, cl.name;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_child_attendance_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    checked_in INT,
    checked_out INT,
    absent INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COUNT(1) FILTER (WHERE ca.status IN ('Present', 'Late'))::int AS checked_in,
        COUNT(1) FILTER (WHERE ca.departure_time IS NOT NULL OR ca.status = 'Departed')::int AS checked_out,
        COUNT(1) FILTER (WHERE ca.status = 'Absent')::int AS absent
    FROM child_attendance ca
    JOIN classrooms cl ON ca.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE ca.tenant_id = p_tenant_id 
      AND b.organization_id = p_org_id
      AND ca.attendance_date = p_date
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_staff_summary(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date DATE
)
RETURNS TABLE (
    expected_staff INT,
    present_staff INT,
    absent_staff INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COUNT(1)::int AS expected_staff,
        COUNT(1) FILTER (WHERE ta.status = 'Present')::int AS present_staff,
        COUNT(1) FILTER (WHERE ta.status = 'Absent')::int AS absent_staff
    FROM teachers t
    LEFT JOIN teacher_attendance ta ON ta.teacher_id = t.id AND ta.attendance_date = p_date AND ta.tenant_id = p_tenant_id
    WHERE t.tenant_id = p_tenant_id AND t.status != 'Inactive'
      AND EXISTS (
          SELECT 1 FROM organization_memberships om
          WHERE om.user_id = t.user_id AND om.organization_id = p_org_id AND om.status = 'Active'
      );
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_experience_schedules(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date DATE,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display VARCHAR,
    scope_display VARCHAR,
    status VARCHAR,
    classroom_id UUID,
    classroom_name VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        es.id,
        false AS is_celebration,
        COALESCE(e.title, 'Classroom Activity')::varchar AS title,
        COALESCE(es.time_of_day, 'Morning')::varchar AS time_display,
        cl.name AS scope_display,
        es.status AS status,
        es.classroom_id AS classroom_id,
        cl.name AS classroom_name,
        es.preparation_notes AS preparation_notes
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    LEFT JOIN experiences e ON es.experience_id = e.id
    WHERE es.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND es.scheduled_date = p_date
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
    ORDER BY es.planned_start_time NULLS LAST, es.time_of_day;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_events(
    p_tenant_id UUID,
    p_date DATE
)
RETURNS TABLE (
    id UUID,
    is_celebration BOOLEAN,
    title VARCHAR,
    time_display TEXT,
    scope_display TEXT,
    status VARCHAR,
    preparation_notes TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        ev.id,
        true AS is_celebration,
        ev.title AS title,
        COALESCE(TO_CHAR(ev.start_time, 'HH24:MI'), 'All Day') AS time_display,
        CASE WHEN ev.is_organization_wide THEN 'All Campus' ELSE 'Classroom Event' END AS scope_display,
        ev.status AS status,
        ev.description AS preparation_notes
    FROM events ev
    WHERE ev.tenant_id = p_tenant_id
      AND ev.event_date = p_date
    ORDER BY ev.start_time NULLS LAST;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_teacher_children_care_status(
    p_tenant_id UUID,
    p_org_id UUID,
    p_date DATE,
    p_start_of_day TIMESTAMPTZ,
    p_end_of_day TIMESTAMPTZ,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    child_id UUID,
    full_name TEXT,
    photo_url TEXT,
    is_checked_in BOOLEAN,
    arrival_time TIME,
    care_events_count INT,
    last_recorded_care_type VARCHAR,
    last_recorded_time TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        c.id AS child_id,
        c.first_name || ' ' || c.last_name AS full_name,
        c.photo_url AS photo_url,
        true AS is_checked_in,
        ca.arrival_time AS arrival_time,
        COALESCE((
            SELECT COUNT(1)::int FROM child_care_events cce 
            WHERE cce.child_id = c.id AND cce.tenant_id = p_tenant_id 
              AND cce.event_time >= p_start_of_day AND cce.event_time <= p_end_of_day
        ), 0) AS care_events_count,
        (
            SELECT cce.event_type FROM child_care_events cce
            WHERE cce.child_id = c.id AND cce.tenant_id = p_tenant_id 
              AND cce.event_time >= p_start_of_day AND cce.event_time <= p_end_of_day
            ORDER BY cce.event_time DESC LIMIT 1
        ) AS last_recorded_care_type,
        (
            SELECT cce.event_time FROM child_care_events cce
            WHERE cce.child_id = c.id AND cce.tenant_id = p_tenant_id 
              AND cce.event_time >= p_start_of_day AND cce.event_time <= p_end_of_day
            ORDER BY cce.event_time DESC LIMIT 1
        ) AS last_recorded_time
    FROM children c
    JOIN child_attendance ca ON c.id = ca.child_id AND ca.attendance_date = p_date AND ca.status IN ('Present', 'Late')
    JOIN classrooms cl ON c.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_pending_enrollments_count(
    p_tenant_id UUID,
    p_org_id UUID DEFAULT NULL
)
RETURNS INT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(1)::int
    FROM children c
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id
      AND (p_org_id IS NULL OR b.organization_id = p_org_id OR c.classroom_id IS NULL)
      AND (c.enrollment_status = 'Draft' OR c.profile_completion_percentage < 100);
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_financial_summary(
    p_tenant_id UUID,
    p_org_id UUID
)
RETURNS TABLE (
    outstanding NUMERIC,
    overdue_count INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        COALESCE(SUM(total_amount - amount_paid) FILTER (WHERE status != 'Paid' AND status != 'Void'), 0)::numeric AS outstanding,
        COUNT(1) FILTER (WHERE status != 'Paid' AND status != 'Void' AND due_date < NOW())::int AS overdue_count
    FROM invoices
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_org_id;
$$;

CREATE OR REPLACE FUNCTION public.fn_get_board_pending_teacher_handovers(
    p_tenant_id UUID,
    p_org_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    classroom_id UUID,
    classroom_name VARCHAR,
    shift_type VARCHAR,
    outgoing_teacher_name VARCHAR,
    is_critical BOOLEAN,
    summary_notes TEXT,
    outstanding_tasks TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        th.id, th.classroom_id AS classroom_id, cl.name AS classroom_name,
        th.shift_type AS shift_type, th.outgoing_teacher_name AS outgoing_teacher_name,
        th.is_critical AS is_critical, th.summary_notes AS summary_notes,
        th.outstanding_care_tasks AS outstanding_tasks,
        th.created_at AS created_at
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND th.status = 'PendingAcknowledgement'
      AND (p_branch_id IS NULL OR th.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR th.classroom_id = p_classroom_id);
$$;
