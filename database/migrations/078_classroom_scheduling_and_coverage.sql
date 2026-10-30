-- 078: Classroom scheduling conflicts + teacher coverage / reassignment
-- Rules (deterministic, no AI):
--   * A classroom can host only one non-cancelled event at any point in time.
--     Events without a start/end time occupy the whole day.
--   * Cancelling an event frees its slot immediately.
--   * Cover teachers: branch teachers not on approved leave and not leading another overlapping event.
--   * Reassignment is performed only by an authorized admin; availability is re-validated at write time.

-- ---------------------------------------------------------------------------
-- 1. Conflict lookup
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_find_classroom_conflicts(
    p_tenant_id uuid,
    p_classroom_id uuid,
    p_date date,
    p_start time,
    p_end time,
    p_exclude_event_id uuid DEFAULT NULL)
RETURNS TABLE(event_id uuid, title varchar, start_time time, end_time time, organizer_id uuid, organizer_name text)
LANGUAGE sql STABLE AS $$
    SELECT e.id, e.title, e.start_time, e.end_time, e.organizer_id,
           COALESCE(NULLIF(TRIM(COALESCE(u.first_name,'') || ' ' || COALESCE(u.last_name,'')), ''), u.email)::text
    FROM events e
    JOIN event_classrooms ec ON ec.event_id = e.id
    LEFT JOIN users u ON u.id = e.organizer_id
    WHERE e.tenant_id = p_tenant_id
      AND ec.classroom_id = p_classroom_id
      AND e.event_date = p_date
      AND e.status <> 'Cancelled'
      AND (p_exclude_event_id IS NULL OR e.id <> p_exclude_event_id)
      AND COALESCE(e.start_time, '00:00'::time) < COALESCE(p_end, '23:59:59.999999'::time)
      AND COALESCE(e.end_time, '23:59:59.999999'::time) > COALESCE(p_start, '00:00'::time)
    ORDER BY e.start_time NULLS FIRST;
$$;

CREATE OR REPLACE FUNCTION public.fn_assert_classroom_free(
    p_tenant_id uuid, p_classroom_id uuid, p_date date, p_start time, p_end time, p_event_id uuid)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    c record;
BEGIN
    -- Serialize writers for the same classroom so concurrent saves cannot both succeed.
    PERFORM pg_advisory_xact_lock(hashtextextended('classroom-slot:' || p_classroom_id::text, 0));

    SELECT * INTO c FROM public.fn_find_classroom_conflicts(p_tenant_id, p_classroom_id, p_date, p_start, p_end, p_event_id) LIMIT 1;
    IF FOUND THEN
        RAISE EXCEPTION 'CLASSROOM_CONFLICT: Classroom is already booked % by "%"%. Cancel or move that activity first.',
            CASE WHEN c.start_time IS NULL THEN 'all day'
                 ELSE to_char(c.start_time, 'HH24:MI') || '–' || COALESCE(to_char(c.end_time, 'HH24:MI'), 'end of day') END,
            c.title,
            CASE WHEN c.organizer_name IS NULL THEN '' ELSE ' (' || c.organizer_name || ')' END
            USING ERRCODE = 'EE409';
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- 2. Triggers enforcing the rule
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.trg_event_classrooms_no_overlap()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    e record;
BEGIN
    SELECT id, tenant_id, event_date, start_time, end_time, status INTO e FROM events WHERE id = NEW.event_id;
    IF FOUND AND e.status <> 'Cancelled' THEN
        PERFORM public.fn_assert_classroom_free(e.tenant_id, NEW.classroom_id, e.event_date, e.start_time, e.end_time, e.id);
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS event_classrooms_no_overlap ON event_classrooms;
CREATE TRIGGER event_classrooms_no_overlap
    BEFORE INSERT OR UPDATE ON event_classrooms
    FOR EACH ROW EXECUTE FUNCTION public.trg_event_classrooms_no_overlap();

CREATE OR REPLACE FUNCTION public.trg_events_no_overlap()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    r record;
BEGIN
    IF NEW.status = 'Cancelled' THEN
        RETURN NEW;
    END IF;
    IF NEW.event_date IS DISTINCT FROM OLD.event_date
       OR NEW.start_time IS DISTINCT FROM OLD.start_time
       OR NEW.end_time IS DISTINCT FROM OLD.end_time
       OR (OLD.status = 'Cancelled') THEN
        FOR r IN SELECT classroom_id FROM event_classrooms WHERE event_id = NEW.id ORDER BY classroom_id LOOP
            PERFORM public.fn_assert_classroom_free(NEW.tenant_id, r.classroom_id, NEW.event_date, NEW.start_time, NEW.end_time, NEW.id);
        END LOOP;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS events_no_overlap ON events;
CREATE TRIGGER events_no_overlap
    BEFORE UPDATE ON events
    FOR EACH ROW EXECUTE FUNCTION public.trg_events_no_overlap();

-- ---------------------------------------------------------------------------
-- 3. Teacher availability for cover
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_available_cover_teachers(
    p_tenant_id uuid,
    p_branch_id uuid,
    p_classroom_id uuid,
    p_date date,
    p_start time,
    p_end time,
    p_exclude_event_id uuid DEFAULT NULL)
RETURNS TABLE(
    user_id uuid,
    display_name text,
    is_same_classroom boolean,
    is_on_leave boolean,
    busy_with text,
    covers_this_week integer,
    is_available boolean,
    reason text)
LANGUAGE sql STABLE AS $$
    WITH branch_teachers AS (
        SELECT DISTINCT ct.user_id
        FROM classroom_teachers ct
        JOIN classrooms c ON c.id = ct.classroom_id
        WHERE ct.tenant_id = p_tenant_id AND c.branch_id = p_branch_id
        UNION
        SELECT uba.user_id
        FROM user_branch_assignments uba
        JOIN teachers t ON t.user_id = uba.user_id AND t.tenant_id = p_tenant_id
        WHERE uba.tenant_id = p_tenant_id AND uba.branch_id = p_branch_id AND uba.revoked_at IS NULL
    ),
    info AS (
        SELECT bt.user_id,
               COALESCE(NULLIF(TRIM(COALESCE(u.first_name,'') || ' ' || COALESCE(u.last_name,'')), ''), u.email)::text AS display_name,
               EXISTS (SELECT 1 FROM classroom_teachers x WHERE x.user_id = bt.user_id AND x.classroom_id = p_classroom_id) AS is_same_classroom,
               EXISTS (SELECT 1 FROM staff_leaves sl JOIN teachers t ON t.id = sl.teacher_id
                       WHERE t.user_id = bt.user_id AND sl.tenant_id = p_tenant_id
                         AND sl.status = 'Approved' AND p_date BETWEEN sl.start_date AND sl.end_date) AS is_on_leave,
               (SELECT e.title || ' ' || COALESCE(to_char(e.start_time,'HH24:MI'),'all day')
                FROM events e
                WHERE e.tenant_id = p_tenant_id AND e.organizer_id = bt.user_id AND e.event_date = p_date
                  AND e.status <> 'Cancelled'
                  AND (p_exclude_event_id IS NULL OR e.id <> p_exclude_event_id)
                  AND COALESCE(e.start_time, '00:00'::time) < COALESCE(p_end, '23:59:59.999999'::time)
                  AND COALESCE(e.end_time, '23:59:59.999999'::time) > COALESCE(p_start, '00:00'::time)
                LIMIT 1)::text AS busy_with,
               (SELECT COUNT(*)::int FROM staff_leaves sl JOIN teachers t ON t.id = sl.substitute_teacher_id
                WHERE t.user_id = bt.user_id AND sl.tenant_id = p_tenant_id
                  AND sl.start_date <= p_date + 6 AND sl.end_date >= p_date - 6) AS covers_this_week
        FROM branch_teachers bt
        JOIN users u ON u.id = bt.user_id
        WHERE u.tenant_id = p_tenant_id
    )
    SELECT i.user_id, i.display_name, i.is_same_classroom, i.is_on_leave, i.busy_with, i.covers_this_week,
           (NOT i.is_on_leave AND i.busy_with IS NULL) AS is_available,
           CASE
               WHEN i.is_on_leave THEN 'On approved leave'
               WHEN i.busy_with IS NOT NULL THEN 'Leading: ' || i.busy_with
               WHEN i.is_same_classroom THEN 'Co-teacher in this classroom — children know them'
               ELSE 'Free branch teacher'
           END
    FROM info i
    ORDER BY (NOT i.is_on_leave AND i.busy_with IS NULL) DESC, i.is_same_classroom DESC, i.covers_this_week ASC, i.display_name;
$$;

-- ---------------------------------------------------------------------------
-- 4. Coverage needs: events whose organizer is on approved leave
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_get_coverage_needs(
    p_tenant_id uuid, p_branch_id uuid, p_from date, p_to date)
RETURNS TABLE(
    event_id uuid, title varchar, event_date date, start_time time, end_time time, status varchar,
    classroom_id uuid, classroom_name varchar,
    organizer_id uuid, organizer_name text,
    leave_id uuid, leave_type varchar)
LANGUAGE sql STABLE AS $$
    SELECT DISTINCT ON (e.id)
           e.id, e.title, e.event_date, e.start_time, e.end_time, e.status,
           c.id, c.name,
           e.organizer_id,
           COALESCE(NULLIF(TRIM(COALESCE(u.first_name,'') || ' ' || COALESCE(u.last_name,'')), ''), u.email)::text,
           sl.id, sl.leave_type
    FROM events e
    JOIN event_classrooms ec ON ec.event_id = e.id
    JOIN classrooms c ON c.id = ec.classroom_id AND c.branch_id = p_branch_id
    JOIN users u ON u.id = e.organizer_id
    JOIN teachers t ON t.user_id = e.organizer_id AND t.tenant_id = p_tenant_id
    JOIN staff_leaves sl ON sl.teacher_id = t.id AND sl.status = 'Approved'
                        AND e.event_date BETWEEN sl.start_date AND sl.end_date
    WHERE e.tenant_id = p_tenant_id
      AND e.event_date BETWEEN p_from AND p_to
      AND e.status NOT IN ('Cancelled', 'Completed')
    ORDER BY e.id, e.event_date;
$$;

-- ---------------------------------------------------------------------------
-- 5. Reassign (validated at write time, audited)
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_reassign_event_organizer(
    p_tenant_id uuid, p_event_id uuid, p_new_user_id uuid, p_reassigned_by uuid, p_branch_id uuid)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    e record;
    v_classroom uuid;
    v_ok boolean;
    v_reason text;
    v_old_teacher uuid;
    v_new_teacher uuid;
BEGIN
    SELECT * INTO e FROM events WHERE id = p_event_id AND tenant_id = p_tenant_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Event not found' USING ERRCODE = 'EE404';
    END IF;

    SELECT ec.classroom_id INTO v_classroom
    FROM event_classrooms ec JOIN classrooms c ON c.id = ec.classroom_id
    WHERE ec.event_id = p_event_id AND c.branch_id = p_branch_id LIMIT 1;
    IF v_classroom IS NULL THEN
        RAISE EXCEPTION 'Event does not belong to this branch' USING ERRCODE = 'EE403';
    END IF;

    -- Serialize per teacher so two admins cannot double-book the same person.
    PERFORM pg_advisory_xact_lock(hashtextextended('teacher-slot:' || p_new_user_id::text, 0));

    SELECT a.is_available, a.reason INTO v_ok, v_reason
    FROM public.fn_get_available_cover_teachers(p_tenant_id, p_branch_id, v_classroom, e.event_date, e.start_time, e.end_time, p_event_id) a
    WHERE a.user_id = p_new_user_id;

    IF v_ok IS NULL THEN
        RAISE EXCEPTION 'Selected teacher is not assigned to this branch' USING ERRCODE = 'EE409';
    ELSIF NOT v_ok THEN
        RAISE EXCEPTION 'Selected teacher is not available: %', v_reason USING ERRCODE = 'EE409';
    END IF;

    UPDATE events SET organizer_id = p_new_user_id, updated_at = now() WHERE id = p_event_id;
    UPDATE event_tasks SET owner_id = p_new_user_id, updated_at = now()
     WHERE event_id = p_event_id AND is_completed = false AND (owner_id = e.organizer_id OR owner_id IS NULL);

    SELECT id INTO v_old_teacher FROM teachers WHERE user_id = e.organizer_id AND tenant_id = p_tenant_id LIMIT 1;
    SELECT id INTO v_new_teacher FROM teachers WHERE user_id = p_new_user_id AND tenant_id = p_tenant_id LIMIT 1;

    IF v_old_teacher IS NOT NULL THEN
        UPDATE staff_leaves sl
           SET substitute_teacher_id = COALESCE(sl.substitute_teacher_id, v_new_teacher),
               reviewed_by_user_id = p_reassigned_by,
               coverage_status = CASE WHEN EXISTS (
                    SELECT 1 FROM events x JOIN event_classrooms xc ON xc.event_id = x.id
                    WHERE x.tenant_id = p_tenant_id AND x.organizer_id = e.organizer_id
                      AND x.status NOT IN ('Cancelled','Completed')
                      AND x.event_date BETWEEN sl.start_date AND sl.end_date)
                  THEN 'Uncovered' ELSE 'Covered' END
         WHERE sl.teacher_id = v_old_teacher AND sl.tenant_id = p_tenant_id AND sl.status = 'Approved'
           AND e.event_date BETWEEN sl.start_date AND sl.end_date;
    END IF;

    INSERT INTO audit_log (tenant_id, user_id, action, entity_type, entity_id, details)
    VALUES (p_tenant_id, p_reassigned_by, 'EventOrganizerReassigned', 'Event', p_event_id,
            jsonb_build_object('from_user_id', e.organizer_id, 'to_user_id', p_new_user_id,
                               'event_date', e.event_date, 'classroom_id', v_classroom));
END;
$$;
DROP FUNCTION IF EXISTS public.fn_get_coverage_needs(uuid, uuid, timestamp, timestamp);
CREATE FUNCTION public.fn_get_coverage_needs(p_tenant_id uuid, p_branch_id uuid, p_from timestamp, p_to timestamp)
RETURNS TABLE(event_id uuid, title varchar, event_date date, start_time time, end_time time, status varchar,
              classroom_id uuid, classroom_name varchar, organizer_id uuid, organizer_name text, leave_id uuid, leave_type varchar)
LANGUAGE sql STABLE AS $$ SELECT * FROM public.fn_get_coverage_needs(p_tenant_id, p_branch_id, p_from::date, p_to::date) $$;

CREATE OR REPLACE FUNCTION public.fn_get_cover_candidates_for_event(p_tenant_id uuid, p_branch_id uuid, p_event_id uuid)
RETURNS TABLE(user_id uuid, display_name text, is_same_classroom boolean, is_on_leave boolean, busy_with text,
              covers_this_week integer, is_available boolean, reason text)
LANGUAGE sql STABLE AS $$
    SELECT a.* FROM events e
    CROSS JOIN LATERAL (
        SELECT ec.classroom_id FROM event_classrooms ec JOIN classrooms c ON c.id = ec.classroom_id
        WHERE ec.event_id = e.id AND c.branch_id = p_branch_id LIMIT 1) cls
    CROSS JOIN LATERAL public.fn_get_available_cover_teachers(p_tenant_id, p_branch_id, cls.classroom_id, e.event_date, e.start_time, e.end_time, e.id) a
    WHERE e.id = p_event_id AND e.tenant_id = p_tenant_id AND a.user_id IS DISTINCT FROM e.organizer_id;
$$;
