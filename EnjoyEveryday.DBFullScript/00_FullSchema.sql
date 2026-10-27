--
-- PostgreSQL database dump
--

\restrict LTCoGMcAl9xbIfsxlldfDnXsY5a86BPKYuYCUohJdHNeWXGbqKJ4xtuTweb05pz

-- Dumped from database version 15.19
-- Dumped by pg_dump version 15.19

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- Name: audit_log_append_only(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.audit_log_append_only() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    RAISE EXCEPTION 'audit_log is append-only (% not permitted)', TG_OP USING ERRCODE = 'insufficient_privilege';
END
$$;


--
-- Name: audit_org_role_change(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.audit_org_role_change() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)
        VALUES (NEW.tenant_id, 'Role.Granted.Db', 'User', NEW.user_id,
                jsonb_build_object('role_id', NEW.role_id, 'organization_id', NEW.organization_id));
        RETURN NEW;
    END IF;
    INSERT INTO audit_log (tenant_id, action, entity_type, entity_id, details)
    VALUES (OLD.tenant_id, 'Role.Revoked.Db', 'User', OLD.user_id,
            jsonb_build_object('role_id', OLD.role_id, 'organization_id', OLD.organization_id));
    RETURN OLD;
END
$$;


--
-- Name: enjoy_role_template(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.enjoy_role_template() RETURNS TABLE(role_name text, permission_name text)
    LANGUAGE sql IMMUTABLE
    AS $$
    SELECT t.role_name::text, t.permission_name::text FROM (VALUES
        ('PlatformAdmin', 'platform.configure'),
        ('PlatformAdmin', 'platform.health'),
        ('PlatformAdmin', 'platform.plans'),
        ('PlatformAdmin', 'platform.roles.manage'),
        ('PlatformAdmin', 'platform.security.view'),
        ('PlatformAdmin', 'tenant.manage'),
        ('OrganizationOwner', 'ai.use'),
        ('OrganizationOwner', 'attendance.approve'),
        ('OrganizationOwner', 'attendance.correct'),
        ('OrganizationOwner', 'attendance.record'),
        ('OrganizationOwner', 'attendance.view'),
        ('OrganizationOwner', 'audit.view'),
        ('OrganizationOwner', 'branch.manage'),
        ('OrganizationOwner', 'care.plan.manage'),
        ('OrganizationOwner', 'care.view'),
        ('OrganizationOwner', 'child.enrollment.manage'),
        ('OrganizationOwner', 'child.manage'),
        ('OrganizationOwner', 'child.observe'),
        ('OrganizationOwner', 'child.view'),
        ('OrganizationOwner', 'classroom.manage'),
        ('OrganizationOwner', 'comms.approve'),
        ('OrganizationOwner', 'comms.draft'),
        ('OrganizationOwner', 'comms.send'),
        ('OrganizationOwner', 'comms.view'),
        ('OrganizationOwner', 'consent.issue'),
        ('OrganizationOwner', 'consent.manage'),
        ('OrganizationOwner', 'consent.review'),
        ('OrganizationOwner', 'consent.view'),
        ('OrganizationOwner', 'emergency.contacts.manage'),
        ('OrganizationOwner', 'event.approved'),
        ('OrganizationOwner', 'event.cancelled'),
        ('OrganizationOwner', 'event.create'),
        ('OrganizationOwner', 'event.manage'),
        ('OrganizationOwner', 'event.published'),
        ('OrganizationOwner', 'experience.approve'),
        ('OrganizationOwner', 'experience.create'),
        ('OrganizationOwner', 'experience.execute'),
        ('OrganizationOwner', 'experience.plan.approve'),
        ('OrganizationOwner', 'experience.plan.submit'),
        ('OrganizationOwner', 'experience.schedule'),
        ('OrganizationOwner', 'experience.view'),
        ('OrganizationOwner', 'export.child'),
        ('OrganizationOwner', 'export.org'),
        ('OrganizationOwner', 'family.manage'),
        ('OrganizationOwner', 'family.verify'),
        ('OrganizationOwner', 'insights.branch.view'),
        ('OrganizationOwner', 'insights.classroom.view'),
        ('OrganizationOwner', 'insights.org.generate'),
        ('OrganizationOwner', 'insights.org.view'),
        ('OrganizationOwner', 'journey.publish'),
        ('OrganizationOwner', 'journey.view.internal'),
        ('OrganizationOwner', 'observation.approve'),
        ('OrganizationOwner', 'organization.manage'),
        ('OrganizationOwner', 'role.assign'),
        ('OrganizationOwner', 'safety.manage'),
        ('OrganizationOwner', 'safety.map.manage'),
        ('OrganizationOwner', 'safety.map.publish'),
        ('OrganizationOwner', 'safety.map.view'),
        ('OrganizationOwner', 'safety.report'),
        ('OrganizationOwner', 'user.manage'),
        ('OrganizationAdmin', 'ai.use'),
        ('OrganizationAdmin', 'attendance.approve'),
        ('OrganizationAdmin', 'attendance.correct'),
        ('OrganizationAdmin', 'attendance.record'),
        ('OrganizationAdmin', 'attendance.view'),
        ('OrganizationAdmin', 'audit.view'),
        ('OrganizationAdmin', 'branch.manage'),
        ('OrganizationAdmin', 'care.plan.manage'),
        ('OrganizationAdmin', 'care.view'),
        ('OrganizationAdmin', 'child.enrollment.manage'),
        ('OrganizationAdmin', 'child.manage'),
        ('OrganizationAdmin', 'child.observe'),
        ('OrganizationAdmin', 'child.view'),
        ('OrganizationAdmin', 'classroom.manage'),
        ('OrganizationAdmin', 'comms.approve'),
        ('OrganizationAdmin', 'comms.draft'),
        ('OrganizationAdmin', 'comms.send'),
        ('OrganizationAdmin', 'comms.view'),
        ('OrganizationAdmin', 'consent.issue'),
        ('OrganizationAdmin', 'consent.manage'),
        ('OrganizationAdmin', 'consent.review'),
        ('OrganizationAdmin', 'consent.view'),
        ('OrganizationAdmin', 'emergency.contacts.manage'),
        ('OrganizationAdmin', 'event.approved'),
        ('OrganizationAdmin', 'event.cancelled'),
        ('OrganizationAdmin', 'event.create'),
        ('OrganizationAdmin', 'event.manage'),
        ('OrganizationAdmin', 'event.published'),
        ('OrganizationAdmin', 'experience.approve'),
        ('OrganizationAdmin', 'experience.create'),
        ('OrganizationAdmin', 'experience.execute'),
        ('OrganizationAdmin', 'experience.plan.approve'),
        ('OrganizationAdmin', 'experience.plan.submit'),
        ('OrganizationAdmin', 'experience.schedule'),
        ('OrganizationAdmin', 'experience.view'),
        ('OrganizationAdmin', 'export.child'),
        ('OrganizationAdmin', 'export.org'),
        ('OrganizationAdmin', 'family.manage'),
        ('OrganizationAdmin', 'family.verify'),
        ('OrganizationAdmin', 'insights.branch.view'),
        ('OrganizationAdmin', 'insights.classroom.view'),
        ('OrganizationAdmin', 'insights.org.generate'),
        ('OrganizationAdmin', 'insights.org.view'),
        ('OrganizationAdmin', 'journey.publish'),
        ('OrganizationAdmin', 'journey.view.internal'),
        ('OrganizationAdmin', 'observation.approve'),
        ('OrganizationAdmin', 'organization.manage'),
        ('OrganizationAdmin', 'role.assign'),
        ('OrganizationAdmin', 'safety.manage'),
        ('OrganizationAdmin', 'safety.map.manage'),
        ('OrganizationAdmin', 'safety.map.publish'),
        ('OrganizationAdmin', 'safety.map.view'),
        ('OrganizationAdmin', 'safety.report'),
        ('OrganizationAdmin', 'user.manage'),
        ('BranchAdmin', 'ai.use'),
        ('BranchAdmin', 'attendance.approve'),
        ('BranchAdmin', 'attendance.correct'),
        ('BranchAdmin', 'attendance.record'),
        ('BranchAdmin', 'attendance.view'),
        ('BranchAdmin', 'audit.view'),
        ('BranchAdmin', 'branch.manage'),
        ('BranchAdmin', 'care.plan.manage'),
        ('BranchAdmin', 'care.view'),
        ('BranchAdmin', 'child.enrollment.manage'),
        ('BranchAdmin', 'child.manage'),
        ('BranchAdmin', 'child.observe'),
        ('BranchAdmin', 'child.view'),
        ('BranchAdmin', 'classroom.manage'),
        ('BranchAdmin', 'comms.approve'),
        ('BranchAdmin', 'comms.draft'),
        ('BranchAdmin', 'comms.send'),
        ('BranchAdmin', 'comms.view'),
        ('BranchAdmin', 'consent.issue'),
        ('BranchAdmin', 'consent.review'),
        ('BranchAdmin', 'consent.view'),
        ('BranchAdmin', 'emergency.contacts.manage'),
        ('BranchAdmin', 'event.approved'),
        ('BranchAdmin', 'event.cancelled'),
        ('BranchAdmin', 'event.create'),
        ('BranchAdmin', 'event.manage'),
        ('BranchAdmin', 'event.published'),
        ('BranchAdmin', 'experience.execute'),
        ('BranchAdmin', 'experience.plan.approve'),
        ('BranchAdmin', 'experience.plan.submit'),
        ('BranchAdmin', 'experience.schedule'),
        ('BranchAdmin', 'experience.view'),
        ('BranchAdmin', 'family.manage'),
        ('BranchAdmin', 'family.verify'),
        ('BranchAdmin', 'insights.branch.view'),
        ('BranchAdmin', 'insights.classroom.view'),
        ('BranchAdmin', 'journey.publish'),
        ('BranchAdmin', 'journey.view.internal'),
        ('BranchAdmin', 'observation.approve'),
        ('BranchAdmin', 'safety.map.manage'),
        ('BranchAdmin', 'safety.map.publish'),
        ('BranchAdmin', 'safety.map.view'),
        ('BranchAdmin', 'safety.report'),
        ('BranchAdmin', 'user.manage'),
        ('DaycareTeacher', 'ai.use'),
        ('DaycareTeacher', 'attendance.record'),
        ('DaycareTeacher', 'attendance.view'),
        ('DaycareTeacher', 'care.record'),
        ('DaycareTeacher', 'care.view'),
        ('DaycareTeacher', 'child.observe'),
        ('DaycareTeacher', 'child.view'),
        ('DaycareTeacher', 'comms.draft'),
        ('DaycareTeacher', 'comms.view'),
        ('DaycareTeacher', 'consent.view'),
        ('DaycareTeacher', 'experience.execute'),
        ('DaycareTeacher', 'experience.plan.submit'),
        ('DaycareTeacher', 'experience.schedule'),
        ('DaycareTeacher', 'experience.view'),
        ('DaycareTeacher', 'safety.map.view'),
        ('DaycareTeacher', 'safety.report'),
        ('ClassroomTeacher', 'ai.use'),
        ('ClassroomTeacher', 'attendance.record'),
        ('ClassroomTeacher', 'attendance.view'),
        ('ClassroomTeacher', 'care.record'),
        ('ClassroomTeacher', 'care.view'),
        ('ClassroomTeacher', 'child.observe'),
        ('ClassroomTeacher', 'child.view'),
        ('ClassroomTeacher', 'comms.draft'),
        ('ClassroomTeacher', 'comms.view'),
        ('ClassroomTeacher', 'consent.view'),
        ('ClassroomTeacher', 'experience.execute'),
        ('ClassroomTeacher', 'experience.plan.submit'),
        ('ClassroomTeacher', 'experience.schedule'),
        ('ClassroomTeacher', 'experience.view'),
        ('ClassroomTeacher', 'safety.map.view'),
        ('ClassroomTeacher', 'safety.report')
    ) AS t(role_name, permission_name)
$$;


--
-- Name: ensure_tenant_roles(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.ensure_tenant_roles(p_tenant uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO roles (tenant_id, name, description, scope_type)
    SELECT p_tenant, v.n, v.n, 'Organization'
    FROM (VALUES ('OrganizationOwner'), ('OrganizationAdmin'), ('BranchAdmin'), ('DaycareTeacher'), ('ClassroomTeacher')) AS v(n)
    WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.tenant_id = p_tenant AND r.name = v.n);

    UPDATE roles SET scope_type = 'Organization'
    WHERE tenant_id = p_tenant AND name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher');

    DELETE FROM role_permissions rp USING roles r
    WHERE rp.role_id = r.id AND r.tenant_id = p_tenant
      AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher');

    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM enjoy_role_template() t
    JOIN roles r ON r.tenant_id = p_tenant AND r.name = t.role_name
    JOIN permissions p ON p.name = t.permission_name
    ON CONFLICT DO NOTHING;
END
$$;


--
-- Name: fn_add_platform_branch(uuid, uuid, character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_add_platform_branch(p_branch_id uuid, p_organization_id uuid, p_name character varying, p_location character varying) RETURNS TABLE(id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, organization_name character varying, effective_location text, classrooms_count integer, children_count integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_tenant_id UUID;
    v_org_name VARCHAR;
BEGIN
    SELECT tenant_id, o.name INTO v_tenant_id, v_org_name 
    FROM organizations o 
    WHERE o.id = p_organization_id;

    IF v_tenant_id IS NULL THEN
        v_tenant_id := p_organization_id;
    END IF;

    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
    VALUES (p_branch_id, v_tenant_id, p_organization_id, p_name, p_location, TRUE);

    RETURN QUERY
    SELECT 
        p_branch_id,
        p_organization_id,
        p_name,
        p_location,
        TRUE,
        NOW(),
        COALESCE(v_org_name, 'Organization'),
        COALESCE(p_location, 'Location unspecified'),
        0::int,
        0::int;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: platform_experience_versions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.platform_experience_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    platform_experience_id uuid NOT NULL,
    version_number integer NOT NULL,
    title character varying(200) NOT NULL,
    description text NOT NULL,
    dna_payload text NOT NULL,
    change_summary text,
    created_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: fn_add_platform_experience_version(uuid, uuid, integer, character varying, text, text, text, uuid, timestamp with time zone, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_add_platform_experience_version(p_id uuid, p_platform_experience_id uuid, p_version_number integer, p_title character varying, p_description text, p_dna_payload text, p_change_summary text, p_created_by_user_id uuid, p_created_at timestamp with time zone, p_is_active boolean) RETURNS SETOF public.platform_experience_versions
    LANGUAGE sql
    AS $$
    INSERT INTO platform_experience_versions (
        id, platform_experience_id, version_number, title, description, dna_payload, change_summary, created_by_user_id, created_at, is_active
    ) VALUES (
        p_id, p_platform_experience_id, p_version_number, p_title, p_description, p_dna_payload, p_change_summary, p_created_by_user_id, p_created_at, p_is_active
    )
    RETURNING *;
$$;


--
-- Name: fn_authorized_pickup_exists(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_authorized_pickup_exists(p_tenant_id uuid, p_pickup_id uuid) RETURNS boolean
    LANGUAGE sql STABLE
    AS $$
    SELECT EXISTS(SELECT 1 FROM authorized_pickups WHERE tenant_id = p_tenant_id AND id = p_pickup_id);
$$;


--
-- Name: fn_create_branch(uuid, uuid, uuid, character varying, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_branch(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_name character varying, p_location character varying, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_organization_id, p_name, p_location, p_is_active, p_created_at, p_updated_at)
    RETURNING branches.id, branches.tenant_id, branches.organization_id, branches.name, branches.location, branches.is_active, branches.created_at, branches.updated_at;
END;
$$;


--
-- Name: fn_create_child(uuid, uuid, character varying, character varying, date, uuid, text, integer, character varying, text, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_child(p_id uuid, p_tenant_id uuid, p_first_name character varying, p_last_name character varying, p_date_of_birth date, p_classroom_id uuid, p_photo_url text, p_profile_completion_percentage integer, p_enrollment_status character varying, p_draft_data text, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, first_name character varying, last_name character varying, date_of_birth date, classroom_id uuid, photo_url text, profile_completion_percentage integer, enrollment_status character varying, draft_data text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    INSERT INTO children (
        id, tenant_id, first_name, last_name, date_of_birth, classroom_id, photo_url,
        profile_completion_percentage, enrollment_status, draft_data, is_active, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_first_name, p_last_name, p_date_of_birth, p_classroom_id, p_photo_url,
        p_profile_completion_percentage, p_enrollment_status, p_draft_data, p_is_active, p_created_at, p_updated_at
    )
    RETURNING children.id, children.tenant_id, children.first_name, children.last_name, children.date_of_birth,
              children.classroom_id, children.photo_url, children.profile_completion_percentage,
              children.enrollment_status, children.draft_data, children.is_active, children.created_at, children.updated_at;
END;
$$;


--
-- Name: fn_create_classroom(uuid, uuid, uuid, character varying, character varying, character varying, character varying, text, character varying, character varying, character varying, integer, integer, integer, integer, character varying, text, jsonb, integer, boolean, timestamp with time zone, uuid, timestamp with time zone, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_classroom(p_id uuid, p_tenant_id uuid, p_branch_id uuid, p_name character varying, p_display_name character varying, p_short_name character varying, p_code character varying, p_description text, p_classroom_type character varying, p_status character varying, p_age_group character varying, p_min_age_months integer, p_max_age_months integer, p_capacity integer, p_current_enrollment integer, p_environment character varying, p_photo_url text, p_draft_data jsonb, p_profile_completion_percentage integer, p_is_active boolean, p_created_at timestamp with time zone, p_created_by uuid, p_updated_at timestamp with time zone, p_updated_by uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, name character varying, display_name character varying, short_name character varying, code character varying, description text, classroom_type character varying, status character varying, age_group character varying, min_age_months integer, max_age_months integer, capacity integer, current_enrollment integer, environment character varying, photo_url text, draft_data jsonb, profile_completion_percentage integer, is_active boolean, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid)
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


--
-- Name: fn_create_event(uuid, uuid, character varying, text, uuid, date, time without time zone, time without time zone, character varying, uuid, character varying, character varying, uuid, boolean, text[], timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_event(p_id uuid, p_tenant_id uuid, p_title character varying, p_description text, p_event_type_id uuid, p_event_date date, p_start_time time without time zone, p_end_time time without time zone, p_location character varying, p_organizer_id uuid, p_status character varying, p_scope character varying, p_branch_id uuid, p_is_organization_wide boolean, p_photo_urls text[], p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO events (
        id, tenant_id, title, description, event_type_id, event_date, start_time, end_time,
        location, organizer_id, status, scope, branch_id, is_organization_wide, photo_urls, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_title, p_description, p_event_type_id, p_event_date, p_start_time, p_end_time,
        p_location, p_organizer_id, p_status, p_scope, p_branch_id, p_is_organization_wide, p_photo_urls, p_created_at, p_updated_at
    );
    RETURN p_id;
END;
$$;


--
-- Name: fn_create_event_rsvp(uuid, uuid, uuid, uuid, character varying, text, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_event_rsvp(p_id uuid, p_event_id uuid, p_child_id uuid, p_guardian_id uuid, p_status character varying, p_notes text, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO event_rsvps (id, event_id, child_id, guardian_id, status, notes, created_at, updated_at)
    VALUES (p_id, p_event_id, p_child_id, p_guardian_id, p_status, p_notes, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;


--
-- Name: fn_create_event_task(uuid, uuid, character varying, uuid, timestamp with time zone, boolean, timestamp with time zone, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_event_task(p_id uuid, p_event_id uuid, p_description character varying, p_owner_id uuid, p_due_date timestamp with time zone, p_is_completed boolean, p_completed_at timestamp with time zone, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO event_tasks (id, event_id, description, owner_id, due_date, is_completed, completed_at, created_at, updated_at)
    VALUES (p_id, p_event_id, p_description, p_owner_id, p_due_date, p_is_completed, p_completed_at, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;


--
-- Name: fn_create_event_type(uuid, uuid, character varying, text, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_event_type(p_id uuid, p_tenant_id uuid, p_name character varying, p_description text, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO event_types (id, tenant_id, name, description, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_name, p_description, p_is_active, p_created_at, p_updated_at);
    RETURN p_id;
END;
$$;


--
-- Name: fn_create_experience(uuid, uuid, uuid, character varying, text, character varying, jsonb, uuid, timestamp with time zone, timestamp with time zone, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_experience(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_title character varying, p_description text, p_status character varying, p_dna_payload jsonb, p_created_by_user_id uuid, p_created_at timestamp with time zone, p_updated_at timestamp with time zone, p_version_id uuid DEFAULT gen_random_uuid()) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, title character varying, description text, status character varying, dna_payload jsonb, created_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO experiences (id, tenant_id, organization_id, title, description, status, dna_payload, created_by_user_id, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_organization_id, p_title, p_description, p_status, p_dna_payload, p_created_by_user_id, p_created_at, p_updated_at);

    INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
    VALUES (p_version_id, p_id, 1, p_title, p_description, p_dna_payload, 'Initial Creation', p_created_by_user_id, p_created_at);

    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at
    FROM experiences e
    WHERE e.id = p_id;
END;
$$;


--
-- Name: experience_licenses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_licenses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    organization_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    license_type character varying(50) DEFAULT 'CommercialRoyalty'::character varying NOT NULL,
    grant_scope character varying(150) DEFAULT 'Curate, Reproduce, Distribute, Modify and Monetize'::character varying NOT NULL,
    granted_by_user_id uuid NOT NULL,
    granted_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone,
    terms_document_hash character varying(128),
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: fn_create_experience_license(uuid, uuid, uuid, character varying, character varying, uuid, timestamp with time zone, timestamp with time zone, character varying, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_experience_license(p_id uuid, p_organization_id uuid, p_experience_id uuid, p_license_type character varying, p_grant_scope character varying, p_granted_by_user_id uuid, p_granted_at timestamp with time zone, p_expires_at timestamp with time zone, p_terms_document_hash character varying, p_is_active boolean) RETURNS SETOF public.experience_licenses
    LANGUAGE sql
    AS $$
    INSERT INTO experience_licenses (
        id, organization_id, experience_id, license_type, grant_scope, granted_by_user_id, granted_at, expires_at, terms_document_hash, is_active
    ) VALUES (
        p_id, p_organization_id, p_experience_id, p_license_type, p_grant_scope, p_granted_by_user_id, p_granted_at, p_expires_at, p_terms_document_hash, p_is_active
    )
    RETURNING *;
$$;


--
-- Name: fn_create_experience_schedule(uuid, uuid, uuid, uuid, date, character varying, character varying, uuid, time without time zone, time without time zone, text, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_experience_schedule(p_id uuid, p_tenant_id uuid, p_experience_id uuid, p_classroom_id uuid, p_scheduled_date date, p_time_of_day character varying, p_status character varying, p_primary_teacher_id uuid, p_planned_start_time time without time zone, p_planned_end_time time without time zone, p_preparation_notes text, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    INSERT INTO experience_schedules (
        id, tenant_id, experience_id, classroom_id, scheduled_date, time_of_day, status,
        primary_teacher_id, planned_start_time, planned_end_time, preparation_notes, created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_experience_id, p_classroom_id, p_scheduled_date, p_time_of_day, p_status,
        p_primary_teacher_id, p_planned_start_time, p_planned_end_time, p_preparation_notes, p_created_at, p_updated_at
    )
    RETURNING experience_schedules.id, experience_schedules.tenant_id, experience_schedules.experience_id,
              experience_schedules.classroom_id, experience_schedules.scheduled_date, experience_schedules.time_of_day,
              experience_schedules.status, experience_schedules.primary_teacher_id, experience_schedules.planned_start_time,
              experience_schedules.planned_end_time, experience_schedules.preparation_notes,
              experience_schedules.created_at, experience_schedules.updated_at;
END;
$$;


--
-- Name: marketplace_orders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.marketplace_orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    platform_experience_id uuid NOT NULL,
    purchased_by_user_id uuid NOT NULL,
    pricing_model character varying(50) NOT NULL,
    amount_paid numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    payment_status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    transaction_reference character varying(100) NOT NULL,
    purchased_at timestamp with time zone DEFAULT now() NOT NULL,
    refunded_at timestamp with time zone,
    refund_reason text
);


--
-- Name: fn_create_marketplace_order(uuid, uuid, uuid, uuid, uuid, character varying, numeric, character varying, character varying, character varying, timestamp with time zone, timestamp with time zone, text); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_marketplace_order(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_platform_experience_id uuid, p_purchased_by_user_id uuid, p_pricing_model character varying, p_amount_paid numeric, p_currency character varying, p_payment_status character varying, p_transaction_reference character varying, p_purchased_at timestamp with time zone, p_refunded_at timestamp with time zone, p_refund_reason text) RETURNS SETOF public.marketplace_orders
    LANGUAGE sql
    AS $$
    INSERT INTO marketplace_orders (
        id, tenant_id, organization_id, platform_experience_id, purchased_by_user_id,
        pricing_model, amount_paid, currency, payment_status, transaction_reference, purchased_at, refunded_at, refund_reason
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_purchased_by_user_id,
        p_pricing_model, p_amount_paid, p_currency, p_payment_status, p_transaction_reference, p_purchased_at, p_refunded_at, p_refund_reason
    )
    ON CONFLICT (organization_id, platform_experience_id, transaction_reference) DO UPDATE
    SET payment_status = EXCLUDED.payment_status
    RETURNING *;
$$;


--
-- Name: organization_entitlements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_entitlements (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    platform_experience_id uuid NOT NULL,
    order_id uuid,
    acquired_version integer DEFAULT 1 NOT NULL,
    entitlement_type character varying(50) DEFAULT 'OneTimePurchase'::character varying NOT NULL,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    granted_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone,
    scope character varying(50) DEFAULT 'OrganizationWide'::character varying NOT NULL
);


--
-- Name: fn_create_or_update_organization_entitlement(uuid, uuid, uuid, uuid, uuid, integer, character varying, character varying, timestamp with time zone, timestamp with time zone, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_or_update_organization_entitlement(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_platform_experience_id uuid, p_order_id uuid, p_acquired_version integer, p_entitlement_type character varying, p_status character varying, p_granted_at timestamp with time zone, p_expires_at timestamp with time zone, p_scope character varying) RETURNS SETOF public.organization_entitlements
    LANGUAGE sql
    AS $$
    INSERT INTO organization_entitlements (
        id, tenant_id, organization_id, platform_experience_id, order_id,
        acquired_version, entitlement_type, status, granted_at, expires_at, scope
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_order_id,
        p_acquired_version, p_entitlement_type, p_status, p_granted_at, p_expires_at, p_scope
    )
    ON CONFLICT (organization_id, platform_experience_id) DO UPDATE
    SET acquired_version = EXCLUDED.acquired_version,
        status = EXCLUDED.status,
        expires_at = EXCLUDED.expires_at
    RETURNING *;
$$;


--
-- Name: fn_create_organization(uuid, uuid, character varying, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_organization(p_id uuid, p_tenant_id uuid, p_name character varying, p_location character varying, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    INSERT INTO organizations (id, tenant_id, name, location, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_name, p_location, p_is_active, p_created_at, p_updated_at)
    RETURNING organizations.id, organizations.tenant_id, organizations.name, organizations.location, organizations.is_active, organizations.created_at, organizations.updated_at;
END;
$$;


--
-- Name: platform_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.platform_experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    title character varying(200) NOT NULL,
    description text NOT NULL,
    category character varying(100) DEFAULT 'General'::character varying NOT NULL,
    age_range_min_months integer,
    age_range_max_months integer,
    duration_minutes integer,
    learning_objectives jsonb,
    required_materials jsonb,
    instructions text,
    safety_notes text,
    dna_payload text NOT NULL,
    pricing_model character varying(50) DEFAULT 'IncludedInSubscription'::character varying NOT NULL,
    price_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    publication_status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    ownership_type character varying(50) DEFAULT 'CuratedLicensed'::character varying NOT NULL,
    current_version integer DEFAULT 1 NOT NULL,
    license_id uuid,
    source_harvest_request_id uuid,
    source_experience_id uuid,
    source_organization_id uuid,
    creator_attribution character varying(200),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: fn_create_platform_experience(uuid, character varying, text, character varying, integer, integer, integer, jsonb, jsonb, text, text, text, character varying, numeric, character varying, character varying, character varying, integer, uuid, uuid, uuid, uuid, character varying, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_platform_experience(p_id uuid, p_title character varying, p_description text, p_category character varying, p_age_range_min_months integer, p_age_range_max_months integer, p_duration_minutes integer, p_learning_objectives jsonb, p_required_materials jsonb, p_instructions text, p_safety_notes text, p_dna_payload text, p_pricing_model character varying, p_price_amount numeric, p_currency character varying, p_publication_status character varying, p_ownership_type character varying, p_current_version integer, p_license_id uuid, p_source_harvest_request_id uuid, p_source_experience_id uuid, p_source_organization_id uuid, p_creator_attribution character varying, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS SETOF public.platform_experiences
    LANGUAGE sql
    AS $$
    INSERT INTO platform_experiences (
        id, title, description, category, age_range_min_months, age_range_max_months,
        duration_minutes, learning_objectives, required_materials, instructions, safety_notes,
        dna_payload, pricing_model, price_amount, currency, publication_status, ownership_type,
        current_version, license_id, source_harvest_request_id, source_experience_id, source_organization_id,
        creator_attribution, created_at, updated_at
    ) VALUES (
        p_id, p_title, p_description, p_category, p_age_range_min_months, p_age_range_max_months,
        p_duration_minutes, p_learning_objectives, p_required_materials, p_instructions, p_safety_notes,
        p_dna_payload, p_pricing_model, p_price_amount, p_currency, p_publication_status, p_ownership_type,
        p_current_version, p_license_id, p_source_harvest_request_id, p_source_experience_id, p_source_organization_id,
        p_creator_attribution, p_created_at, p_updated_at
    )
    RETURNING *;
$$;


--
-- Name: fn_create_teacher(uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, text, date, character varying, character varying, text, character varying, text, character varying, jsonb, integer, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_teacher(p_id uuid, p_tenant_id uuid, p_person_id uuid, p_user_id uuid, p_employee_number character varying, p_first_name character varying, p_middle_name character varying, p_last_name character varying, p_preferred_name character varying, p_display_name character varying, p_profile_photo_url text, p_date_of_birth date, p_gender character varying, p_preferred_language character varying, p_other_languages text, p_pronouns character varying, p_bio text, p_status character varying, p_draft_data jsonb, p_profile_completion_percentage integer, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, person_id uuid, user_id uuid, employee_number character varying, first_name character varying, middle_name character varying, last_name character varying, preferred_name character varying, display_name character varying, profile_photo_url text, date_of_birth date, gender character varying, preferred_language character varying, other_languages text, pronouns character varying, bio text, status character varying, draft_data jsonb, profile_completion_percentage integer, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_create_user(uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_create_user(p_id uuid, p_tenant_id uuid, p_email character varying, p_normalized_email character varying, p_password_hash character varying, p_first_name character varying, p_last_name character varying, p_display_name character varying, p_is_active boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    INSERT INTO users (id, tenant_id, email, normalized_email, password_hash, first_name, last_name, display_name, is_active, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_email, p_normalized_email, p_password_hash, p_first_name, p_last_name, p_display_name, p_is_active, p_created_at, p_updated_at)
    RETURNING users.id, users.tenant_id, users.email, users.first_name, users.last_name, users.display_name, users.is_active, users.created_at, users.updated_at;
END;
$$;


--
-- Name: fn_dev_login(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_dev_login(p_email character varying DEFAULT NULL::character varying) RETURNS TABLE(out_status character varying, out_user_id uuid, out_tenant_id uuid, out_effective_tenant_id uuid, out_active_org_id uuid, out_email character varying, out_display_name character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_target_email VARCHAR;
BEGIN
    IF p_email IS NOT NULL AND TRIM(p_email) <> '' THEN
        SELECT email INTO v_target_email 
        FROM users 
        WHERE LOWER(email) = LOWER(TRIM(p_email)) OR normalized_email = UPPER(TRIM(p_email)) 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        SELECT email INTO v_target_email 
        FROM users 
        WHERE normalized_email = 'PRIYA@LITTLESTARS.COM' 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        SELECT email INTO v_target_email 
        FROM users 
        ORDER BY created_at ASC 
        LIMIT 1;
    END IF;

    IF v_target_email IS NULL THEN
        RETURN QUERY SELECT 'no_users'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR;
        RETURN;
    END IF;

    RETURN QUERY 
    SELECT * FROM fn_sync_external_login('DevLogin', v_target_email, v_target_email, 'Dev', 'User', NULL, 'Daycare');
END;
$$;


--
-- Name: fn_get_active_child_care_plan(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_child_care_plan(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, child_id uuid, version integer, diaper_routine_guidance text, feeding_instructions text, bottle_schedule_guidance text, sleep_routine_guidance text, toileting_support_plan text, comfort_calming_preferences text, required_supplies_note text, dietary_allergies_summary text, effective_date date, review_date date, status character varying, created_by_user_id uuid, approved_by_user_id uuid, approved_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT p.id, p.tenant_id, p.organization_id, p.branch_id, p.child_id, p.version,
           p.diaper_routine_guidance, p.feeding_instructions, p.bottle_schedule_guidance,
           p.sleep_routine_guidance, p.toileting_support_plan, p.comfort_calming_preferences,
           p.required_supplies_note, p.dietary_allergies_summary, p.effective_date, p.review_date,
           p.status, p.created_by_user_id, p.approved_by_user_id, p.approved_at, p.created_at, p.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_care_plans p
    JOIN children c ON p.child_id = c.id
    WHERE p.tenant_id = p_tenant_id 
      AND p.child_id = p_child_id 
      AND p.status = 'Active'
    ORDER BY p.version DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_active_child_nap_session(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_child_nap_session(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, start_time timestamp with time zone, end_time timestamp with time zone, duration_minutes integer, status character varying, outcome character varying, notes text, started_by_user_id uuid, started_by_name character varying, ended_by_user_id uuid, ended_by_name character varying, parent_visible boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.child_id = p_child_id 
      AND s.status = 'Active'
    ORDER BY s.start_time DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_active_child_naps_by_classroom(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_child_naps_by_classroom(p_tenant_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, start_time timestamp with time zone, end_time timestamp with time zone, duration_minutes integer, status character varying, outcome character varying, notes text, started_by_user_id uuid, started_by_name character varying, ended_by_user_id uuid, ended_by_name character varying, parent_visible boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.classroom_id = p_classroom_id 
      AND s.status = 'Active'
    ORDER BY s.start_time ASC;
$$;


--
-- Name: fn_get_active_custody_restrictions(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_custody_restrictions(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, restriction_type character varying, notes character varying, effective_from timestamp with time zone, effective_to timestamp with time zone, person_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT r.id, r.restriction_type::VARCHAR, r.notes::VARCHAR, r.effective_from, r.effective_to,
           COALESCE(p.first_name || ' ' || p.last_name, 'Restricted Individual')::VARCHAR AS person_name
    FROM child_person_restrictions r
    LEFT JOIN persons p ON r.person_id = p.id
    WHERE r.tenant_id = p_tenant_id AND r.child_id = p_child_id
      AND (r.effective_to IS NULL OR r.effective_to >= NOW());
END;
$$;


--
-- Name: fn_get_active_experience_license(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_experience_license(p_experience_id uuid) RETURNS SETOF public.experience_licenses
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM experience_licenses
    WHERE experience_id = p_experience_id AND is_active = TRUE
      AND (expires_at IS NULL OR expires_at > NOW())
    ORDER BY granted_at DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_active_family_contracts(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_family_contracts(p_tenant_id uuid, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, application_id uuid, child_id uuid, parent_name character varying, parent_email character varying, schedule_type character varying, billing_cycle character varying, base_rate numeric, discount_percent numeric, effective_rate numeric, deposit_held numeric, status character varying, start_date date, end_date date, auto_invoice boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, branch_id, application_id,
        child_id, parent_name, parent_email,
        schedule_type, billing_cycle,
        base_rate, discount_percent, effective_rate,
        deposit_held, status, start_date, end_date,
        auto_invoice, created_at, updated_at
    FROM family_billing_contracts
    WHERE tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
    ORDER BY created_at DESC;
$$;


--
-- Name: fn_get_active_material_loans_for_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_material_loans_for_organization(p_organization_id uuid) RETURNS TABLE(id uuid, order_id uuid, order_item_id uuid, material_product_id uuid, organization_id uuid, classroom_id uuid, quantity_issued integer, issued_at timestamp with time zone, due_date timestamp with time zone, actual_return_date timestamp with time zone, return_status character varying, deposit_amount numeric, deposit_status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying, order_number character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.organization_id = p_organization_id
    ORDER BY l.due_date ASC;
$$;


--
-- Name: fn_get_active_staff_leaves(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_staff_leaves(p_tenant_id uuid, p_date date) RETURNS TABLE(id uuid, teacher_id uuid, leave_type character varying, start_date date, end_date date, reason character varying, status character varying, coverage_status character varying, substitute_teacher_id uuid, teacher_name character varying, substitute_teacher_name character varying)
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


--
-- Name: fn_get_active_staff_leaves(uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_staff_leaves(p_tenant_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, teacher_id uuid, leave_type character varying, start_date date, end_date date, reason character varying, status character varying, coverage_status character varying, substitute_teacher_id uuid, teacher_name character varying, substitute_teacher_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_active_staff_leaves(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_active_staff_leaves(uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_staff_leaves(p_tenant_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, teacher_id uuid, leave_type character varying, start_date date, end_date date, reason character varying, status character varying, coverage_status character varying, substitute_teacher_id uuid, teacher_name character varying, substitute_teacher_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_active_staff_leaves(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_active_support_grant(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_active_support_grant(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, granted_to_user_id uuid, purpose character varying, expires_at timestamp with time zone, read_only boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT sag.id, sag.tenant_id, sag.granted_to_user_id, sag.purpose,
           sag.expires_at, sag.read_only
    FROM support_access_grants sag
    WHERE sag.tenant_id = p_tenant_id AND sag.granted_to_user_id = p_user_id
      AND sag.revoked_at IS NULL AND sag.expires_at > NOW()
    ORDER BY sag.expires_at DESC LIMIT 1;
END;
$$;


--
-- Name: fn_get_activity_recommendation_decisions(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_activity_recommendation_decisions(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, request_id uuid, recommendation_item_id uuid, tenant_id uuid, decided_by_user_id uuid, decision character varying, rejection_reason text, resulting_schedule_id uuid, decided_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, request_id, recommendation_item_id, tenant_id, decided_by_user_id, decision, rejection_reason, resulting_schedule_id, decided_at
    FROM activity_recommendation_decisions
    WHERE request_id = p_request_id AND tenant_id = p_tenant_id
    ORDER BY decided_at DESC;
$$;


--
-- Name: fn_get_activity_recommendation_items(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_activity_recommendation_items(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, request_id uuid, tenant_id uuid, experience_id uuid, experience_title character varying, suggested_format character varying, estimated_duration_minutes integer, rationale text, supporting_evidence jsonb, materials jsonb, rank_order integer, is_evidence_limited boolean, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, request_id, tenant_id, experience_id, experience_title, suggested_format, estimated_duration_minutes, rationale, supporting_evidence, materials, rank_order, is_evidence_limited, created_at
    FROM activity_recommendation_items
    WHERE request_id = p_request_id AND tenant_id = p_tenant_id
    ORDER BY rank_order ASC;
$$;


--
-- Name: fn_get_activity_recommendation_request_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_activity_recommendation_request_by_id(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, requested_by_user_id uuid, request_timestamp timestamp with time zone, scope character varying, context_preferences jsonb, evidence_count integer, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, tenant_id, organization_id, classroom_id, child_id, requested_by_user_id, request_timestamp, scope, context_preferences, evidence_count, created_at
    FROM activity_recommendation_requests
    WHERE id = p_request_id AND tenant_id = p_tenant_id;
$$;


--
-- Name: fn_get_all_children(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_children(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, first_name character varying, last_name character varying, date_of_birth date, classroom_id uuid, photo_url text, profile_completion_percentage integer, enrollment_status character varying, draft_data text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id, 
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data, 
           c.is_active, c.created_at, c.updated_at
    FROM children c
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id AND c.tenant_id = cl.tenant_id
    LEFT JOIN branches b ON cl.branch_id = b.id AND cl.tenant_id = b.tenant_id
    WHERE c.tenant_id = p_tenant_id AND c.is_active = true
      AND (p_organization_id IS NULL 
           OR b.organization_id = p_organization_id 
           OR EXISTS (
               SELECT 1 FROM enrollments e 
               JOIN branches eb ON e.branch_id = eb.id 
               WHERE e.child_id = c.id AND eb.organization_id = p_organization_id
           ))
    ORDER BY c.first_name, c.last_name;
END;
$$;


--
-- Name: fn_get_all_classrooms(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_classrooms(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, name character varying, display_name character varying, short_name character varying, code character varying, description text, classroom_type character varying, status character varying, age_group character varying, min_age_months integer, max_age_months integer, capacity integer, current_enrollment integer, environment character varying, photo_url text, draft_data jsonb, profile_completion_percentage integer, is_active boolean, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid)
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


--
-- Name: fn_get_all_development_areas(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_development_areas(p_tenant_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, description text, icon character varying, display_order integer, is_active boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT da.id, da.tenant_id, da.name::VARCHAR, da.description, da.icon::VARCHAR,
           da.display_order, da.is_active
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$$;


--
-- Name: fn_get_all_events_for_tenant(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_events_for_tenant(p_tenant_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    WHERE ev.tenant_id = p_tenant_id
    ORDER BY ev.event_date DESC;
END;
$$;


--
-- Name: fn_get_all_experience_feedbacks(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_experience_feedbacks(p_tenant_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating character varying, notes text, created_at timestamp with time zone)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating::VARCHAR, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;


--
-- Name: fn_get_all_experiences(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_experiences(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, title character varying, description text, status character varying, dna_payload jsonb, created_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone, average_stars numeric)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload,
           e.created_by_user_id, e.created_at, e.updated_at,
           COALESCE(AVG(ef.stars), 0)::NUMERIC AS average_stars
    FROM experiences e
    LEFT JOIN experience_schedules es ON e.id = es.experience_id
    LEFT JOIN experience_feedback ef ON es.id = ef.experience_schedule_id
    WHERE e.tenant_id = p_tenant_id AND e.status != 'Archived'
      AND (p_organization_id IS NULL OR e.organization_id = p_organization_id)
    GROUP BY e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at
    ORDER BY e.created_at DESC;
END;
$$;


--
-- Name: fn_get_all_invoices(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_invoices(p_tenant_id uuid DEFAULT NULL::uuid, p_organization_id uuid DEFAULT NULL::uuid, p_status character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_number character varying, billing_period character varying, subtotal numeric, discount_amount numeric, tax_amount numeric, total_amount numeric, amount_paid numeric, currency character varying, status character varying, issue_date timestamp with time zone, due_date timestamp with time zone, paid_at timestamp with time zone, published_at timestamp with time zone, published_by character varying, voided_at timestamp with time zone, voided_by character varying, void_reason text, created_by character varying, description text, notes text, payment_instructions text, billing_contact_email character varying, document_storage_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE (p_tenant_id IS NULL OR tenant_id = p_tenant_id)
      AND (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_status IS NULL OR status = p_status)
    ORDER BY issue_date DESC, created_at DESC;
$$;


--
-- Name: fn_get_all_organizations(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_organizations(p_tenant_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_get_all_platform_experiences(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_platform_experiences() RETURNS SETOF public.platform_experiences
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM platform_experiences
    ORDER BY created_at DESC;
$$;


--
-- Name: fn_get_all_teachers(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_teachers(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, person_id uuid, user_id uuid, employee_number character varying, first_name character varying, middle_name character varying, last_name character varying, preferred_name character varying, display_name character varying, profile_photo_url text, date_of_birth date, gender character varying, preferred_language character varying, other_languages text, pronouns character varying, bio text, status character varying, draft_data jsonb, profile_completion_percentage integer, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_get_all_users(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_all_users(p_tenant_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.tenant_id = p_tenant_id
    ORDER BY u.first_name, u.last_name;
END;
$$;


--
-- Name: fn_get_assigned_branch_ids(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_assigned_branch_ids(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid) RETURNS TABLE(branch_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT uba.branch_id
    FROM user_branch_assignments uba
    WHERE uba.tenant_id = p_tenant_id AND uba.organization_id = p_organization_id AND uba.user_id = p_user_id AND uba.revoked_at IS NULL;
END;
$$;


--
-- Name: fn_get_assigned_classroom_ids(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_assigned_classroom_ids(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ct.classroom_id
    FROM classroom_teachers ct
    WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id;
END;
$$;


--
-- Name: fn_get_attendance_summary(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_attendance_summary(p_tenant_id uuid, p_date date) RETURNS TABLE(expected_children integer, present_children integer, absent_children integer, late_children integer, expected_teachers integer, present_teachers integer, absent_teachers integer, late_teachers integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        (SELECT COUNT(*)::INT FROM children WHERE tenant_id = p_tenant_id AND is_active = true) AS expected_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Present') AS present_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Absent') AS absent_children,
        (SELECT COUNT(*)::INT FROM child_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Late') AS late_children,
        
        (SELECT COUNT(*)::INT FROM teachers WHERE tenant_id = p_tenant_id AND status != 'Inactive') AS expected_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Present') AS present_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Absent') AS absent_teachers,
        (SELECT COUNT(*)::INT FROM teacher_attendance WHERE tenant_id = p_tenant_id AND attendance_date = p_date AND status = 'Late') AS late_teachers;
END;
$$;


--
-- Name: fn_get_attendance_summary(uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_attendance_summary(p_tenant_id uuid, p_date timestamp without time zone) RETURNS TABLE(expected_children integer, present_children integer, absent_children integer, late_children integer, expected_teachers integer, present_teachers integer, absent_teachers integer, late_teachers integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_attendance_summary(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_attendance_summary(uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_attendance_summary(p_tenant_id uuid, p_date timestamp with time zone) RETURNS TABLE(expected_children integer, present_children integer, absent_children integer, late_children integer, expected_teachers integer, present_teachers integer, absent_teachers integer, late_teachers integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_attendance_summary(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_authorized_pickups(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_authorized_pickups(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, child_id uuid, person_id uuid, relationship character varying, pickup_code character varying, photo_url text, is_active boolean, notes character varying, full_name character varying, phone character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ap.id, ap.child_id, ap.person_id, ap.relationship::VARCHAR, ap.pickup_code::VARCHAR,
           ap.photo_url::TEXT, ap.is_active, ap.notes::VARCHAR,
           COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), ap.relationship, 'Authorized Pickup')::VARCHAR AS full_name,
           COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS phone
    FROM authorized_pickups ap
    LEFT JOIN persons p ON ap.person_id = p.id
    WHERE ap.tenant_id = p_tenant_id AND ap.child_id = p_child_id AND ap.is_active = TRUE
    ORDER BY ap.is_active DESC;
END;
$$;


--
-- Name: fn_get_board_child_attendance_summary(uuid, uuid, date, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_child_attendance_summary(p_tenant_id uuid, p_org_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(checked_in integer, checked_out integer, absent integer)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_child_attendance_summary(uuid, uuid, timestamp without time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_child_attendance_summary(p_tenant_id uuid, p_org_id uuid, p_date timestamp without time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(checked_in integer, checked_out integer, absent integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_child_attendance_summary(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_child_attendance_summary(uuid, uuid, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_child_attendance_summary(p_tenant_id uuid, p_org_id uuid, p_date timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(checked_in integer, checked_out integer, absent integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_child_attendance_summary(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_classrooms(uuid, uuid, date, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_classrooms(p_tenant_id uuid, p_org_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(classroom_id uuid, classroom_name character varying, age_group character varying, branch_id uuid, branch_name character varying, capacity integer, enrolled_children integer, present_children integer, expected_children integer, present_staff integer)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_classrooms(uuid, uuid, timestamp without time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_classrooms(p_tenant_id uuid, p_org_id uuid, p_date timestamp without time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(classroom_id uuid, classroom_name character varying, age_group character varying, branch_id uuid, branch_name character varying, capacity integer, enrolled_children integer, present_children integer, expected_children integer, present_staff integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_classrooms(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_classrooms(uuid, uuid, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_classrooms(p_tenant_id uuid, p_org_id uuid, p_date timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(classroom_id uuid, classroom_name character varying, age_group character varying, branch_id uuid, branch_name character varying, capacity integer, enrolled_children integer, present_children integer, expected_children integer, present_staff integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_classrooms(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_default_organization_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_default_organization_id(p_tenant_id uuid) RETURNS uuid
    LANGUAGE sql STABLE
    AS $$
    SELECT id
    FROM organizations
    WHERE tenant_id = p_tenant_id AND is_active = true
    ORDER BY name
    LIMIT 1;
$$;


--
-- Name: fn_get_board_events(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_events(p_tenant_id uuid, p_date date) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display text, scope_display text, status character varying, preparation_notes text)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_events(uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_events(p_tenant_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display text, scope_display text, status character varying, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_events(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_board_events(uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_events(p_tenant_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display text, scope_display text, status character varying, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_events(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_board_experience_schedules(uuid, uuid, date, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_experience_schedules(p_tenant_id uuid, p_org_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display character varying, scope_display character varying, status character varying, classroom_id uuid, classroom_name character varying, preparation_notes text)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_experience_schedules(uuid, uuid, timestamp without time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_experience_schedules(p_tenant_id uuid, p_org_id uuid, p_date timestamp without time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display character varying, scope_display character varying, status character varying, classroom_id uuid, classroom_name character varying, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_experience_schedules(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_experience_schedules(uuid, uuid, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_experience_schedules(p_tenant_id uuid, p_org_id uuid, p_date timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, is_celebration boolean, title character varying, time_display character varying, scope_display character varying, status character varying, classroom_id uuid, classroom_name character varying, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_experience_schedules(p_tenant_id, p_org_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_board_financial_summary(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_financial_summary(p_tenant_id uuid, p_org_id uuid) RETURNS TABLE(outstanding numeric, overdue_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        COALESCE(SUM(total_amount - amount_paid) FILTER (WHERE status != 'Paid' AND status != 'Void'), 0)::numeric AS outstanding,
        COUNT(1) FILTER (WHERE status != 'Paid' AND status != 'Void' AND due_date < NOW())::int AS overdue_count
    FROM invoices
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_org_id;
$$;


--
-- Name: fn_get_board_organization_name(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_organization_name(p_tenant_id uuid, p_org_id uuid) RETURNS character varying
    LANGUAGE sql STABLE
    AS $$
    SELECT name
    FROM organizations
    WHERE id = p_org_id AND tenant_id = p_tenant_id;
$$;


--
-- Name: fn_get_board_pending_enrollments_count(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_pending_enrollments_count(p_tenant_id uuid, p_org_id uuid DEFAULT NULL::uuid) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT COUNT(1)::int
    FROM children c
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id
      AND (p_org_id IS NULL OR b.organization_id = p_org_id OR c.classroom_id IS NULL)
      AND (c.enrollment_status = 'Draft' OR c.profile_completion_percentage < 100);
$$;


--
-- Name: fn_get_board_pending_teacher_handovers(uuid, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_pending_teacher_handovers(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, classroom_id uuid, classroom_name character varying, shift_type character varying, outgoing_teacher_name character varying, is_critical boolean, summary_notes text, outstanding_tasks text, created_at timestamp with time zone)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_staff_summary(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_staff_summary(p_tenant_id uuid, p_org_id uuid, p_date date) RETURNS TABLE(expected_staff integer, present_staff integer, absent_staff integer)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_staff_summary(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_staff_summary(p_tenant_id uuid, p_org_id uuid, p_date timestamp without time zone) RETURNS TABLE(expected_staff integer, present_staff integer, absent_staff integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_staff_summary(p_tenant_id, p_org_id, p_date::DATE);
$$;


--
-- Name: fn_get_board_staff_summary(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_staff_summary(p_tenant_id uuid, p_org_id uuid, p_date timestamp with time zone) RETURNS TABLE(expected_staff integer, present_staff integer, absent_staff integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_staff_summary(p_tenant_id, p_org_id, p_date::DATE);
$$;


--
-- Name: fn_get_board_teacher_children_care_status(uuid, uuid, date, timestamp with time zone, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_teacher_children_care_status(p_tenant_id uuid, p_org_id uuid, p_date date, p_start_of_day timestamp with time zone, p_end_of_day timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(child_id uuid, full_name text, photo_url text, is_checked_in boolean, arrival_time time without time zone, care_events_count integer, last_recorded_care_type character varying, last_recorded_time timestamp with time zone)
    LANGUAGE sql STABLE
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


--
-- Name: fn_get_board_teacher_children_care_status(uuid, uuid, timestamp without time zone, timestamp with time zone, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_teacher_children_care_status(p_tenant_id uuid, p_org_id uuid, p_date timestamp without time zone, p_start_of_day timestamp with time zone, p_end_of_day timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(child_id uuid, full_name text, photo_url text, is_checked_in boolean, arrival_time time without time zone, care_events_count integer, last_recorded_care_type character varying, last_recorded_time timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_teacher_children_care_status(
        p_tenant_id, p_org_id, p_date::DATE, p_start_of_day, p_end_of_day, p_branch_id, p_classroom_id
    );
$$;


--
-- Name: fn_get_board_teacher_children_care_status(uuid, uuid, timestamp with time zone, timestamp with time zone, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_board_teacher_children_care_status(p_tenant_id uuid, p_org_id uuid, p_date timestamp with time zone, p_start_of_day timestamp with time zone, p_end_of_day timestamp with time zone, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(child_id uuid, full_name text, photo_url text, is_checked_in boolean, arrival_time time without time zone, care_events_count integer, last_recorded_care_type character varying, last_recorded_time timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_board_teacher_children_care_status(
        p_tenant_id, p_org_id, p_date::DATE, p_start_of_day, p_end_of_day, p_branch_id, p_classroom_id
    );
$$;


--
-- Name: fn_get_branch_attendance_stats(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_attendance_stats(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(total_attendance_records integer, present_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        COUNT(*)::int AS total_attendance_records,
        COUNT(CASE WHEN status = 'Present' THEN 1 END)::int AS present_count
    FROM child_attendance
    WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id
      AND attendance_date >= CURRENT_DATE - INTERVAL '30 days';
$$;


--
-- Name: fn_get_branch_billing_stats(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_billing_stats(p_tenant_id uuid) RETURNS TABLE(total_billed numeric, total_paid numeric)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        COALESCE(SUM(total_amount), 0)::numeric AS total_billed,
        COALESCE(SUM(amount_paid), 0)::numeric AS total_paid
    FROM invoices
    WHERE tenant_id = p_tenant_id;
$$;


--
-- Name: fn_get_branch_brief(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_brief(p_branch_id uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.name::VARCHAR
    FROM branches b
    WHERE b.id = p_branch_id;
END;
$$;


--
-- Name: fn_get_branch_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.tenant_id, b.organization_id, b.name, b.location, b.is_active, b.created_at, b.updated_at
    FROM branches b
    WHERE b.id = p_id AND b.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_branch_classrooms(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_classrooms(p_branch_id uuid) RETURNS TABLE(id uuid, name character varying, room_type character varying, capacity integer)
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


--
-- Name: fn_get_branch_classrooms_with_plans_count(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_classrooms_with_plans_count(p_tenant_id uuid, p_branch_id uuid) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT COUNT(DISTINCT es.classroom_id)::int
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    WHERE es.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id
      AND es.scheduled_date >= CURRENT_DATE - INTERVAL '7 days';
$$;


--
-- Name: fn_get_branch_enrolled_children_count(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_enrolled_children_count(p_tenant_id uuid, p_branch_id uuid) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT COUNT(*)::int
    FROM children c
    JOIN classrooms cl ON c.classroom_id = cl.id
    WHERE c.tenant_id = p_tenant_id AND cl.branch_id = p_branch_id AND c.is_active = TRUE;
$$;


--
-- Name: fn_get_branch_floor_map_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_floor_map_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, name character varying, building_area character varying, floor_level character varying, description text, file_storage_key character varying, file_type character varying, file_size bigint, content_hash character varying, version integer, status character varying, review_due_at timestamp with time zone, created_by character varying, created_at timestamp with time zone, published_by character varying, published_at timestamp with time zone, supersedes_map_id uuid, row_version integer, emergency_instructions text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;


--
-- Name: fn_get_branch_floor_maps(uuid, uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_floor_maps(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_status character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, name character varying, building_area character varying, floor_level character varying, description text, file_storage_key character varying, file_type character varying, file_size bigint, content_hash character varying, version integer, status character varying, review_due_at timestamp with time zone, created_by character varying, created_at timestamp with time zone, published_by character varying, published_at timestamp with time zone, supersedes_map_id uuid, row_version integer, emergency_instructions text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
      AND (p_status IS NULL OR status = p_status)
    ORDER BY floor_level ASC, name ASC, version DESC;
$$;


--
-- Name: fn_get_branch_ids_of_assigned_classrooms(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_ids_of_assigned_classrooms(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(branch_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT cl.branch_id
    FROM classroom_teachers ct
    JOIN classrooms cl ON cl.id = ct.classroom_id AND cl.tenant_id = ct.tenant_id
    WHERE ct.tenant_id = p_tenant_id AND ct.user_id = p_user_id;
END;
$$;


--
-- Name: fn_get_branch_operational_goals(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_operational_goals(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, branch_name character varying, title character varying, category character varying, target_metric character varying, current_value character varying, target_value character varying, status character varying, target_date date, owner_name character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        g.id,
        g.tenant_id AS tenant_id,
        g.organization_id AS organization_id,
        g.branch_id AS branch_id,
        b.name AS branch_name,
        g.title AS title,
        g.category AS category,
        g.target_metric AS target_metric,
        g.current_value AS current_value,
        g.target_value AS target_value,
        g.status AS status,
        g.target_date AS target_date,
        g.owner_name AS owner_name,
        g.created_at AS created_at,
        g.updated_at AS updated_at
    FROM branch_operational_goals g
    LEFT JOIN branches b ON g.branch_id = b.id
    WHERE g.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR g.branch_id = p_branch_id)
    ORDER BY g.created_at DESC;
$$;


--
-- Name: fn_get_branch_room_stats(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_room_stats(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(room_count integer, total_cap integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        COUNT(*)::int AS room_count,
        COALESCE(SUM(capacity), 0)::int AS total_cap
    FROM classrooms
    WHERE tenant_id = p_tenant_id AND branch_id = p_branch_id;
$$;


--
-- Name: fn_get_branch_scope(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_scope(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT b.tenant_id, b.organization_id, b.id AS branch_id, NULL::uuid AS classroom_id
    FROM branches b WHERE b.id = p_branch_id AND b.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_branch_teachers(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branch_teachers(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, teacher_name character varying, classroom_id uuid, classroom_name character varying)
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


--
-- Name: fn_get_branches_by_organization(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branches_by_organization(p_tenant_id uuid, p_organization_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_get_branches_for_benchmarking(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_branches_for_benchmarking(p_tenant_id uuid, p_organization_id uuid) RETURNS TABLE(id uuid, name character varying, location character varying)
    LANGUAGE sql STABLE
    AS $$
    WITH org_branches AS (
        SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
        FROM branches b
        WHERE b.tenant_id = p_tenant_id AND b.organization_id = p_organization_id
        ORDER BY b.name
    )
    SELECT * FROM org_branches
    UNION ALL
    SELECT b.id, b.name, COALESCE(b.location, 'Main Campus')::varchar AS location
    FROM branches b
    WHERE b.tenant_id = p_tenant_id
      AND NOT EXISTS (SELECT 1 FROM org_branches)
    ORDER BY name;
$$;


--
-- Name: fn_get_central_warehouse_location(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_central_warehouse_location() RETURNS TABLE(id uuid, code character varying, name character varying, address text, contact_phone character varying, is_central_warehouse boolean, is_active boolean, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at
    FROM inventory_locations 
    WHERE is_central_warehouse = TRUE AND is_active = TRUE 
    LIMIT 1;
$$;


--
-- Name: fn_get_child_attendance_by_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, attendance_date date, status character varying, arrival_time time without time zone, departure_time time without time zone, notes text, recorded_by_user_id uuid, pickup_person_name character varying, pickup_person_relationship character varying, pickup_verification_method character varying, pickup_notes text, authorized_pickup_id uuid, departure_verified_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ca.id, ca.tenant_id, ca.branch_id, ca.classroom_id, ca.child_id, ca.attendance_date, ca.status,
           ca.arrival_time, ca.departure_time, ca.notes, ca.recorded_by_user_id, ca.pickup_person_name,
           ca.pickup_person_relationship, ca.pickup_verification_method, ca.pickup_notes,
           ca.authorized_pickup_id, ca.departure_verified_by_user_id, ca.created_at, ca.updated_at
    FROM child_attendance ca
    WHERE ca.tenant_id = p_tenant_id AND ca.branch_id = p_branch_id AND ca.attendance_date = p_date;
END;
$$;


--
-- Name: fn_get_child_attendance_by_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, attendance_date date, status character varying, arrival_time time without time zone, departure_time time without time zone, notes text, recorded_by_user_id uuid, pickup_person_name character varying, pickup_person_relationship character varying, pickup_verification_method character varying, pickup_notes text, authorized_pickup_id uuid, departure_verified_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_attendance_by_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, attendance_date date, status character varying, arrival_time time without time zone, departure_time time without time zone, notes text, recorded_by_user_id uuid, pickup_person_name character varying, pickup_person_relationship character varying, pickup_verification_method character varying, pickup_notes text, authorized_pickup_id uuid, departure_verified_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_attendance_counts(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_counts(p_tenant_id uuid, p_branch_id uuid, p_date date) RETURNS TABLE(classroom_id uuid, present_count integer)
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


--
-- Name: fn_get_child_attendance_counts(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_counts(p_tenant_id uuid, p_branch_id uuid, p_date timestamp without time zone) RETURNS TABLE(classroom_id uuid, present_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_attendance_counts(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_attendance_counts(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_counts(p_tenant_id uuid, p_branch_id uuid, p_date timestamp with time zone) RETURNS TABLE(classroom_id uuid, present_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_attendance_counts(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: child_attendance; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_attendance (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid NOT NULL,
    attendance_date date NOT NULL,
    status character varying(50) NOT NULL,
    arrival_time time without time zone,
    departure_time time without time zone,
    notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    pickup_person_name character varying(255),
    pickup_person_relationship character varying(100),
    pickup_verification_method character varying(100),
    pickup_notes text,
    authorized_pickup_id uuid,
    departure_verified_by_user_id uuid
);


--
-- Name: fn_get_child_attendance_for_date(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_attendance_for_date(p_child_id uuid, p_date date) RETURNS SETOF public.child_attendance
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM child_attendance
    WHERE child_id = p_child_id AND attendance_date = p_date
    LIMIT 1;
$$;


--
-- Name: fn_get_child_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, first_name character varying, last_name character varying, date_of_birth date, classroom_id uuid, photo_url text, profile_completion_percentage integer, enrollment_status character varying, draft_data text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id,
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data,
           c.is_active, c.created_at, c.updated_at
    FROM children c
    WHERE c.id = p_id AND c.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_child_care_event_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_event_by_id(p_tenant_id uuid, p_event_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id AND e.id = p_event_id;
$$;


--
-- Name: fn_get_child_care_events_by_branch_and_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_branch_and_date(p_tenant_id uuid, p_branch_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.branch_id = p_branch_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time DESC;
$$;


--
-- Name: fn_get_child_care_events_by_branch_and_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_branch_and_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_branch_and_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_events_by_branch_and_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_branch_and_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_branch_and_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_events_by_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_child(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.child_id = p_child_id
    ORDER BY e.event_time DESC;
$$;


--
-- Name: fn_get_child_care_events_by_child_and_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.child_id = p_child_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time ASC;
$$;


--
-- Name: fn_get_child_care_events_by_child_and_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_events_by_child_and_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_events_by_classroom_and_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id uuid, p_classroom_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT e.id, e.tenant_id, e.organization_id, e.branch_id, e.classroom_id, e.child_id,
           e.event_type, e.event_time, e.diaper_condition, e.skin_observation,
           e.meal_type, e.food_offered, e.amount_consumed, e.fluid_oz,
           e.rest_status, e.toileting_activity, e.hygiene_type, e.comfort_mood,
           e.notes, e.parent_visible, e.recorded_by_user_id, e.recorded_by_name,
           e.correction_reason, e.is_corrected, e.created_at, e.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_events e
    JOIN children c ON e.child_id = c.id
    LEFT JOIN classrooms cl ON e.classroom_id = cl.id
    WHERE e.tenant_id = p_tenant_id 
      AND e.classroom_id = p_classroom_id
      AND (e.event_time::date = p_date OR (e.event_time AT TIME ZONE 'America/New_York')::date = p_date)
    ORDER BY e.event_time DESC;
$$;


--
-- Name: fn_get_child_care_events_by_classroom_and_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id uuid, p_classroom_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id, p_classroom_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_events_by_classroom_and_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id uuid, p_classroom_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, event_type character varying, event_time timestamp with time zone, diaper_condition character varying, skin_observation character varying, meal_type character varying, food_offered character varying, amount_consumed character varying, fluid_oz numeric, rest_status character varying, toileting_activity character varying, hygiene_type character varying, comfort_mood character varying, notes text, parent_visible boolean, recorded_by_user_id uuid, recorded_by_name character varying, correction_reason text, is_corrected boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_care_events_by_classroom_and_date(p_tenant_id, p_classroom_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_care_plans_by_branch(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_plans_by_branch(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, child_id uuid, version integer, diaper_routine_guidance text, feeding_instructions text, bottle_schedule_guidance text, sleep_routine_guidance text, toileting_support_plan text, comfort_calming_preferences text, required_supplies_note text, dietary_allergies_summary text, effective_date date, review_date date, status character varying, created_by_user_id uuid, approved_by_user_id uuid, approved_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT p.id, p.tenant_id, p.organization_id, p.branch_id, p.child_id, p.version,
           p.diaper_routine_guidance, p.feeding_instructions, p.bottle_schedule_guidance,
           p.sleep_routine_guidance, p.toileting_support_plan, p.comfort_calming_preferences,
           p.required_supplies_note, p.dietary_allergies_summary, p.effective_date, p.review_date,
           p.status, p.created_by_user_id, p.approved_by_user_id, p.approved_at, p.created_at, p.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_care_plans p
    JOIN children c ON p.child_id = c.id
    WHERE p.tenant_id = p_tenant_id 
      AND p.branch_id = p_branch_id 
      AND p.status = 'Active'
    ORDER BY c.first_name ASC;
$$;


--
-- Name: fn_get_child_care_supplies_by_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_care_supplies_by_child(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, supply_type character varying, status character varying, notes text, threshold_note character varying, last_reported_by_user_id uuid, last_reported_by_name character varying, parent_reminder_sent boolean, last_reminder_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.supply_type, s.status, s.notes, s.threshold_note, s.last_reported_by_user_id,
           s.last_reported_by_name, s.parent_reminder_sent, s.last_reminder_at, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_supplies s
    JOIN children c ON s.child_id = c.id
    LEFT JOIN classrooms cl ON s.classroom_id = cl.id
    WHERE s.tenant_id = p_tenant_id AND s.child_id = p_child_id
    ORDER BY s.supply_type ASC;
$$;


--
-- Name: fn_get_child_consent_document_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_consent_document_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, consent_template_id uuid, template_version character varying, document_version integer, document_title character varying, document_snapshot text, status character varying, signing_deadline timestamp with time zone, created_by uuid, created_at timestamp with time zone, admin_reviewed_by uuid, admin_reviewed_at timestamp with time zone, admin_review_notes text, approved_at timestamp with time zone, sent_at timestamp with time zone, signed_at timestamp with time zone, declined_at timestamp with time zone, withdrawn_at timestamp with time zone, signed_document_storage_key character varying, signed_document_checksum character varying, supersedes_document_id uuid, row_version integer, child_name text, branch_name character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        d.id, d.tenant_id, d.organization_id, d.branch_id, d.classroom_id, d.child_id,
        d.consent_template_id, d.template_version, d.document_version, d.document_title,
        d.document_snapshot, d.status, d.signing_deadline, d.created_by, d.created_at,
        d.admin_reviewed_by, d.admin_reviewed_at, d.admin_review_notes, d.approved_at,
        d.sent_at, d.signed_at, d.declined_at, d.withdrawn_at,
        d.signed_document_storage_key, d.signed_document_checksum,
        d.supersedes_document_id, d.row_version,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        b.name AS branch_name,
        cl.name AS classroom_name
    FROM child_consent_documents d
    JOIN children c ON d.child_id = c.id
    LEFT JOIN branches b ON d.branch_id = b.id
    LEFT JOIN classrooms cl ON d.classroom_id = cl.id
    WHERE d.tenant_id = p_tenant_id AND d.id = p_id;
$$;


--
-- Name: fn_get_child_consent_documents(uuid, uuid, uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_consent_documents(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_child_id uuid DEFAULT NULL::uuid, p_status character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, consent_template_id uuid, template_version character varying, document_version integer, document_title character varying, document_snapshot text, status character varying, signing_deadline timestamp with time zone, created_by uuid, created_at timestamp with time zone, admin_reviewed_by uuid, admin_reviewed_at timestamp with time zone, admin_review_notes text, approved_at timestamp with time zone, sent_at timestamp with time zone, signed_at timestamp with time zone, declined_at timestamp with time zone, withdrawn_at timestamp with time zone, signed_document_storage_key character varying, signed_document_checksum character varying, supersedes_document_id uuid, row_version integer, child_name text, branch_name character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        d.id, d.tenant_id, d.organization_id, d.branch_id, d.classroom_id, d.child_id,
        d.consent_template_id, d.template_version, d.document_version, d.document_title,
        d.document_snapshot, d.status, d.signing_deadline, d.created_by, d.created_at,
        d.admin_reviewed_by, d.admin_reviewed_at, d.admin_review_notes, d.approved_at,
        d.sent_at, d.signed_at, d.declined_at, d.withdrawn_at,
        d.signed_document_storage_key, d.signed_document_checksum,
        d.supersedes_document_id, d.row_version,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        b.name AS branch_name,
        cl.name AS classroom_name
    FROM child_consent_documents d
    JOIN children c ON d.child_id = c.id
    LEFT JOIN branches b ON d.branch_id = b.id
    LEFT JOIN classrooms cl ON d.classroom_id = cl.id
    WHERE d.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR d.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR d.branch_id = p_branch_id)
      AND (p_child_id IS NULL OR d.child_id = p_child_id)
      AND (p_status IS NULL OR d.status = p_status)
    ORDER BY d.created_at DESC;
$$;


--
-- Name: fn_get_child_interests_by_child_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_interests_by_child_id(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, name character varying, created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ci.id, ci.tenant_id, ci.child_id, ci.name, ci.created_at
    FROM child_interests ci
    WHERE ci.child_id = p_child_id AND ci.tenant_id = p_tenant_id
    ORDER BY ci.created_at DESC;
END;
$$;


--
-- Name: fn_get_child_journey_insight_items(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_journey_insight_items(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, request_id uuid, tenant_id uuid, category character varying, classification character varying, statement text, evidence_strength character varying, supporting_evidence text, suggested_experience_id uuid, suggested_experience_title character varying, rank_order integer, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, request_id, tenant_id, category, classification,
        statement, evidence_strength, supporting_evidence::text,
        suggested_experience_id, suggested_experience_title,
        rank_order, created_at
    FROM child_journey_insight_items
    WHERE tenant_id = p_tenant_id AND request_id = p_request_id
    ORDER BY rank_order ASC;
$$;


--
-- Name: fn_get_child_journey_insight_request_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_journey_insight_request_by_id(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, requested_by_user_id uuid, period_type character varying, period_start_date date, period_end_date date, focus_area character varying, teacher_question text, evidence_count integer, overall_summary text, evidence_gaps text, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND id = p_request_id;
$$;


--
-- Name: fn_get_child_memories(uuid, uuid, boolean, timestamp with time zone, timestamp with time zone, character varying, character varying, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_memories(p_tenant_id uuid, p_child_id uuid, p_is_parent boolean, p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_category character varying DEFAULT NULL::character varying, p_search_query character varying DEFAULT NULL::character varying, p_limit integer DEFAULT 100, p_offset integer DEFAULT 0) RETURNS TABLE(id uuid, tenant_id uuid, event_date timestamp with time zone, event_type character varying, category character varying, title character varying, description text, source_id uuid, source_type character varying, child_id uuid, child_name character varying, classroom_id uuid, classroom_name character varying, teacher_id uuid, teacher_name character varying, experience_id uuid, experience_title character varying, media_url text, is_parent_visible boolean, status character varying, category_icon character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        -- 1. Child Stories
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Observations'::VARCHAR AS category,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            NULL::UUID AS classroom_id,
            NULL::VARCHAR AS classroom_name,
            cs.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            es.experience_id,
            e.title::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            cs.status::VARCHAR,
            'bi-journal-text'::VARCHAR AS category_icon
        FROM child_stories cs
        JOIN children c ON cs.child_id = c.id
        LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        LEFT JOIN experiences e ON es.experience_id = e.id
        LEFT JOIN teachers t ON cs.teacher_id = t.id
        WHERE cs.tenant_id = p_tenant_id AND cs.child_id = p_child_id
          AND (p_is_parent = FALSE OR cs.status = 'Approved')

        UNION ALL
        
        -- 2. Child Journey Entries
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            (CASE WHEN je.is_milestone THEN 'Milestone' ELSE 'Observation' END)::VARCHAR AS event_type,
            (CASE WHEN je.is_milestone THEN 'Milestones' ELSE 'Observations' END)::VARCHAR AS category,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            je.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            je.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            je.experience_id,
            e.title::VARCHAR AS experience_title,
            ev.text_content AS media_url,
            je.is_parent_visible,
            je.parent_visibility_status::VARCHAR AS status,
            (CASE WHEN je.is_milestone THEN 'bi-trophy' ELSE 'bi-compass' END)::VARCHAR AS category_icon
        FROM child_journey_entries je
        JOIN children c ON je.child_id = c.id
        LEFT JOIN classrooms cr ON je.classroom_id = cr.id
        LEFT JOIN experiences e ON je.experience_id = e.id
        LEFT JOIN teachers t ON je.teacher_id = t.id
        LEFT JOIN LATERAL (
            SELECT cje_ev.text_content 
            FROM child_journey_evidence cje_ev
            WHERE cje_ev.journey_entry_id = je.id 
            ORDER BY cje_ev.created_at DESC 
            LIMIT 1
        ) ev ON TRUE
        WHERE je.tenant_id = p_tenant_id AND je.child_id = p_child_id
          AND (p_is_parent = FALSE OR (je.is_parent_visible = TRUE AND (je.parent_visibility_status = 'Approved' OR je.parent_visibility_status IS NULL)))

        UNION ALL

        -- 3. Experience Participations
        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Learning Experiences'::VARCHAR AS category,
            COALESCE(e.title, 'Experience Session')::VARCHAR AS title,
            COALESCE(p.participation_notes, s.actual_notes, 'Child participated in this learning experience.') AS description,
            p.id AS source_id,
            'experience_participations'::VARCHAR AS source_type,
            p.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            s.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            s.primary_teacher_id AS teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            s.experience_id,
            e.title::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            p.participation_status::VARCHAR AS status,
            'bi-palette'::VARCHAR AS category_icon
        FROM experience_participations p
        JOIN experience_sessions s ON p.experience_session_id = s.id
        JOIN children c ON p.child_id = c.id
        LEFT JOIN classrooms cr ON s.classroom_id = cr.id
        LEFT JOIN experiences e ON s.experience_id = e.id
        LEFT JOIN teachers t ON s.primary_teacher_id = t.id
        WHERE s.tenant_id = p_tenant_id 
          AND p.child_id = p_child_id 
          AND p.participation_status = 'Participated'

        UNION ALL

        -- 4. Child Milestones
        SELECT
            m.id, m.tenant_id, m.milestone_date AS event_date,
            'Milestone'::VARCHAR AS event_type,
            'Milestones'::VARCHAR AS category,
            m.title::VARCHAR,
            COALESCE(m.description, m.evidence_summary, 'Developmental milestone achieved.') AS description,
            m.id AS source_id,
            'child_milestones'::VARCHAR AS source_type,
            m.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            m.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            m.teacher_id,
            COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            m.parent_visible AS is_parent_visible,
            m.status::VARCHAR,
            'bi-star-fill'::VARCHAR AS category_icon
        FROM child_milestones m
        JOIN children c ON m.child_id = c.id
        LEFT JOIN classrooms cr ON m.classroom_id = cr.id
        LEFT JOIN teachers t ON m.teacher_id = t.id
        WHERE m.tenant_id = p_tenant_id AND m.child_id = p_child_id
          AND (p_is_parent = FALSE OR m.parent_visible = TRUE)

        UNION ALL

        -- 5. Celebrations & Events
        SELECT
            r.id, ev.tenant_id,
            (ev.event_date + COALESCE(ev.start_time, '09:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Celebration'::VARCHAR AS event_type,
            'Celebrations'::VARCHAR AS category,
            ev.title::VARCHAR,
            COALESCE(ev.description, r.notes, 'Celebration & Daycare Event') AS description,
            ev.id AS source_id,
            'events'::VARCHAR AS source_type,
            r.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            NULL::UUID AS classroom_id,
            NULL::VARCHAR AS classroom_name,
            NULL::UUID AS teacher_id,
            'Daycare Staff'::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            TRUE AS is_parent_visible,
            r.status::VARCHAR,
            'bi-balloon-heart'::VARCHAR AS category_icon
        FROM event_rsvps r
        JOIN events ev ON r.event_id = ev.id
        JOIN children c ON r.child_id = c.id
        WHERE ev.tenant_id = p_tenant_id 
          AND r.child_id = p_child_id
          AND ev.status = 'Published'
          AND r.status IN ('Accepted', 'Attended', 'Confirmed')

        UNION ALL

        -- 6. Child Daily Care & Wellbeing
        SELECT
            ce.id, ce.tenant_id, ce.event_time AS event_date,
            'DailyCare'::VARCHAR AS event_type,
            'Daily Care'::VARCHAR AS category,
            ('Wellbeing Highlight: ' || ce.comfort_mood)::VARCHAR AS title,
            COALESCE(ce.notes, 'Positive daily care & comfort milestone.') AS description,
            ce.id AS source_id,
            'child_care_events'::VARCHAR AS source_type,
            ce.child_id,
            (c.first_name || ' ' || c.last_name)::VARCHAR AS child_name,
            ce.classroom_id,
            cr.name::VARCHAR AS classroom_name,
            ce.recorded_by_user_id AS teacher_id,
            ce.recorded_by_name::VARCHAR AS teacher_name,
            NULL::UUID AS experience_id,
            NULL::VARCHAR AS experience_title,
            NULL::TEXT AS media_url,
            ce.parent_visible AS is_parent_visible,
            'Recorded'::VARCHAR AS status,
            'bi-sun'::VARCHAR AS category_icon
        FROM child_care_events ce
        JOIN children c ON ce.child_id = c.id
        LEFT JOIN classrooms cr ON ce.classroom_id = cr.id
        WHERE ce.tenant_id = p_tenant_id 
          AND ce.child_id = p_child_id
          AND ce.event_type = 'Comfort'
          AND ce.comfort_mood IN ('Happy', 'Settled')
          AND (p_is_parent = FALSE OR ce.parent_visible = TRUE)
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
      AND (p_category IS NULL OR p_category = 'All' OR combined.category = p_category)
      AND (p_search_query IS NULL OR p_search_query = '' OR combined.title ILIKE '%' || p_search_query || '%' OR combined.description ILIKE '%' || p_search_query || '%')
      AND (p_is_parent = FALSE OR combined.is_parent_visible = TRUE)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


--
-- Name: fn_get_child_naps_by_child_and_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_naps_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, start_time timestamp with time zone, end_time timestamp with time zone, duration_minutes integer, status character varying, outcome character varying, notes text, started_by_user_id uuid, started_by_name character varying, ended_by_user_id uuid, ended_by_name character varying, parent_visible boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.start_time, s.end_time, s.duration_minutes, s.status, s.outcome, s.notes,
           s.started_by_user_id, s.started_by_name, s.ended_by_user_id, s.ended_by_name,
           s.parent_visible, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name
    FROM child_nap_sessions s
    JOIN children c ON s.child_id = c.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.child_id = p_child_id
      AND s.start_time::date = p_date
    ORDER BY s.start_time ASC;
$$;


--
-- Name: fn_get_child_naps_by_child_and_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_naps_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, start_time timestamp with time zone, end_time timestamp with time zone, duration_minutes integer, status character varying, outcome character varying, notes text, started_by_user_id uuid, started_by_name character varying, ended_by_user_id uuid, ended_by_name character varying, parent_visible boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_naps_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_naps_by_child_and_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_naps_by_child_and_date(p_tenant_id uuid, p_child_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, start_time timestamp with time zone, end_time timestamp with time zone, duration_minutes integer, status character varying, outcome character varying, notes text, started_by_user_id uuid, started_by_name character varying, ended_by_user_id uuid, ended_by_name character varying, parent_visible boolean, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_child_naps_by_child_and_date(p_tenant_id, p_child_id, p_date::DATE);
$$;


--
-- Name: fn_get_child_pickup_header(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_pickup_header(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, first_name character varying, last_name character varying, photo_url text, classroom_name character varying, branch_name character varying, classroom_id uuid, branch_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.first_name, c.last_name, c.photo_url,
           cr.name::VARCHAR AS classroom_name, b.name::VARCHAR AS branch_name, cr.id AS classroom_id, b.id AS branch_id
    FROM children c
    LEFT JOIN classrooms cr ON c.classroom_id = cr.id
    LEFT JOIN branches b ON cr.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id AND c.id = p_child_id;
END;
$$;


--
-- Name: fn_get_child_scope(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_scope(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ch.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id
    FROM children ch
    LEFT JOIN classrooms cl ON cl.id = ch.classroom_id AND cl.tenant_id = ch.tenant_id
    LEFT JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
    WHERE ch.id = p_child_id AND ch.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_child_stories_by_child_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_child_stories_by_child_id(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, teacher_id uuid, experience_schedule_id uuid, content text, status character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cs.id, cs.tenant_id, cs.child_id, cs.teacher_id,
           cs.experience_schedule_id, cs.content,
           cs.status, cs.created_at, cs.updated_at
    FROM child_stories cs
    WHERE cs.child_id = p_child_id AND cs.tenant_id = p_tenant_id
    ORDER BY cs.created_at DESC;
END;
$$;


--
-- Name: fn_get_children_by_classroom(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_children_by_classroom(p_tenant_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, first_name character varying, last_name character varying, date_of_birth date, classroom_id uuid, photo_url text, profile_completion_percentage integer, enrollment_status character varying, draft_data text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.tenant_id, c.first_name, c.last_name, c.date_of_birth, c.classroom_id,
           c.photo_url, c.profile_completion_percentage, c.enrollment_status, c.draft_data,
           c.is_active, c.created_at, c.updated_at
    FROM children c
    WHERE c.classroom_id = p_classroom_id AND c.tenant_id = p_tenant_id AND c.is_active = true
    ORDER BY c.first_name, c.last_name;
END;
$$;


--
-- Name: fn_get_classroom_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_classroom_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, name character varying, display_name character varying, short_name character varying, code character varying, description text, classroom_type character varying, status character varying, age_group character varying, min_age_months integer, max_age_months integer, capacity integer, current_enrollment integer, environment character varying, photo_url text, draft_data jsonb, profile_completion_percentage integer, is_active boolean, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid)
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


--
-- Name: fn_get_classroom_memories(uuid, uuid, timestamp with time zone, timestamp with time zone, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_classroom_memories(p_tenant_id uuid, p_classroom_id uuid, p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_limit integer DEFAULT 100, p_offset integer DEFAULT 0) RETURNS TABLE(id uuid, tenant_id uuid, event_date timestamp with time zone, event_type character varying, title character varying, description text, source_id uuid, source_type character varying, child_id uuid, classroom_id uuid, teacher_id uuid, experience_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.classroom_id = p_classroom_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.classroom_id = p_classroom_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND s.classroom_id = p_classroom_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


--
-- Name: fn_get_classroom_scope(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_classroom_scope(p_tenant_id uuid, p_classroom_id uuid) RETURNS TABLE(tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cl.tenant_id, b.organization_id, b.id AS branch_id, cl.id AS classroom_id
    FROM classrooms cl JOIN branches b ON b.id = cl.branch_id AND b.tenant_id = cl.tenant_id
    WHERE cl.id = p_classroom_id AND cl.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_classroom_vacancies(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_classroom_vacancies(p_tenant_id uuid, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(classroom_id uuid, classroom_name character varying, age_group character varying, capacity integer, current_enrolled bigint, pending_offers bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        c.id AS classroom_id,
        c.name AS classroom_name,
        COALESCE(c.age_group, c.classroom_type, 'Toddler')::VARCHAR AS age_group,
        COALESCE(c.capacity, 15) AS capacity,
        (SELECT COUNT(*) FROM children ch WHERE ch.classroom_id = c.id AND ch.is_active = TRUE) AS current_enrolled,
        (SELECT COUNT(*) FROM enrollment_pipeline_applications a WHERE a.target_classroom_id = c.id AND a.pipeline_stage IN ('UnderReview', 'OfferSent')) AS pending_offers
    FROM classrooms c
    WHERE c.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR c.branch_id = p_branch_id)
    ORDER BY c.name;
$$;


--
-- Name: fn_get_classrooms_by_branch(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_classrooms_by_branch(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, name character varying, display_name character varying, short_name character varying, code character varying, description text, classroom_type character varying, status character varying, age_group character varying, min_age_months integer, max_age_months integer, capacity integer, current_enrollment integer, environment character varying, photo_url text, draft_data jsonb, profile_completion_percentage integer, is_active boolean, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid)
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


--
-- Name: fn_get_consent_audit_events_by_doc_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_audit_events_by_doc_id(p_doc_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, consent_document_id uuid, actor_type character varying, actor_user_id uuid, actor_name character varying, event_type character varying, timestamp_utc timestamp with time zone, correlation_id character varying, safe_metadata text)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, tenant_id, organization_id, consent_document_id, actor_type, actor_user_id, actor_name, event_type, timestamp_utc, correlation_id, safe_metadata
    FROM consent_audit_events
    WHERE consent_document_id = p_doc_id
    ORDER BY timestamp_utc DESC;
$$;


--
-- Name: fn_get_consent_choices_by_doc_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_choices_by_doc_id(p_doc_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, consent_signer_id uuid, section_key character varying, section_title character varying, is_mandatory boolean, choice character varying, recorded_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, tenant_id, consent_document_id, consent_signer_id, section_key, section_title, is_mandatory, choice, recorded_at
    FROM consent_choices
    WHERE consent_document_id = p_doc_id;
$$;


--
-- Name: fn_get_consent_signer_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_signer_by_id(p_tenant_id uuid, p_signer_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, family_relationship_id uuid, person_id uuid, intended_signer_user_id uuid, signer_name_snapshot character varying, signer_email_snapshot character varying, signer_relationship character varying, is_required boolean, signing_status character varying, token_hash character varying, token_expires_at timestamp with time zone, invitation_sent_count integer, last_invitation_sent_at timestamp with time zone, last_opened_at timestamp with time zone, signed_at timestamp with time zone, signature_legal_name character varying, signature_intent_text text, signature_ip_address character varying, signature_user_agent text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE tenant_id = p_tenant_id AND id = p_signer_id;
$$;


--
-- Name: fn_get_consent_signer_by_token_hash(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_signer_by_token_hash(p_token_hash character varying) RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, family_relationship_id uuid, person_id uuid, intended_signer_user_id uuid, signer_name_snapshot character varying, signer_email_snapshot character varying, signer_relationship character varying, is_required boolean, signing_status character varying, token_hash character varying, token_expires_at timestamp with time zone, invitation_sent_count integer, last_invitation_sent_at timestamp with time zone, last_opened_at timestamp with time zone, signed_at timestamp with time zone, signature_legal_name character varying, signature_intent_text text, signature_ip_address character varying, signature_user_agent text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE token_hash = p_token_hash;
$$;


--
-- Name: fn_get_consent_signers_by_doc_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_signers_by_doc_id(p_doc_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, family_relationship_id uuid, person_id uuid, intended_signer_user_id uuid, signer_name_snapshot character varying, signer_email_snapshot character varying, signer_relationship character varying, is_required boolean, signing_status character varying, token_hash character varying, token_expires_at timestamp with time zone, invitation_sent_count integer, last_invitation_sent_at timestamp with time zone, last_opened_at timestamp with time zone, signed_at timestamp with time zone, signature_legal_name character varying, signature_intent_text text, signature_ip_address character varying, signature_user_agent text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE consent_document_id = p_doc_id;
$$;


--
-- Name: fn_get_consent_signers_by_doc_ids(uuid[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_signers_by_doc_ids(p_doc_ids uuid[]) RETURNS TABLE(id uuid, tenant_id uuid, consent_document_id uuid, family_relationship_id uuid, person_id uuid, intended_signer_user_id uuid, signer_name_snapshot character varying, signer_email_snapshot character varying, signer_relationship character varying, is_required boolean, signing_status character varying, token_hash character varying, token_expires_at timestamp with time zone, invitation_sent_count integer, last_invitation_sent_at timestamp with time zone, last_opened_at timestamp with time zone, signed_at timestamp with time zone, signature_legal_name character varying, signature_intent_text text, signature_ip_address character varying, signature_user_agent text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE consent_document_id = ANY(p_doc_ids);
$$;


--
-- Name: fn_get_consent_template_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_template_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, description text, document_type character varying, version character varying, template_content text, required_for_enrollment boolean, is_active boolean, created_by uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, name, description, document_type,
        version, template_content, required_for_enrollment, is_active,
        created_by, created_at, updated_at
    FROM consent_templates
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;


--
-- Name: fn_get_consent_templates(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_consent_templates(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, name character varying, description text, document_type character varying, version character varying, template_content text, required_for_enrollment boolean, is_active boolean, created_by uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, name, description, document_type,
        version, template_content, required_for_enrollment, is_active,
        created_by, created_at, updated_at
    FROM consent_templates
    WHERE tenant_id = p_tenant_id
      AND is_active = TRUE
      AND (p_organization_id IS NULL OR organization_id IS NULL OR organization_id = p_organization_id)
    ORDER BY required_for_enrollment DESC, name ASC;
$$;


--
-- Name: fn_get_dashboard_branch_metrics(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_dashboard_branch_metrics(p_tenant_id uuid, p_organization_id uuid) RETURNS TABLE(branch_id uuid, branch_name character varying, location character varying, status text, classrooms integer, children integer, experiences integer)
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


--
-- Name: fn_get_dashboard_organization_metrics(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_dashboard_organization_metrics(p_tenant_id uuid, p_organization_id uuid) RETURNS TABLE(organization_id uuid, organization_name character varying, total_branches integer, active_branch_count integer, total_classrooms integer, total_children integer, total_teachers integer, children_experiencing_today integer, experiences_this_week integer, experiences_this_week_completed integer, experiences_this_week_upcoming integer, teacher_created_experiences integer, teacher_created_experiences_this_month integer)
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


--
-- Name: fn_get_development_areas_for_journey_entry(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_development_areas_for_journey_entry(p_journey_entry_id uuid) RETURNS TABLE(development_area_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cjd.development_area_id
    FROM child_journey_developments cjd
    WHERE cjd.journey_entry_id = p_journey_entry_id;
END;
$$;


--
-- Name: fn_get_due_material_recurring_schedules(timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_due_material_recurring_schedules(p_as_of_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, schedule_type character varying, frequency character varying, material_product_id uuid, offering_id uuid, quantity integer, loan_duration_days integer, delivery_address text, next_run_date timestamp with time zone, last_run_date timestamp with time zone, status character varying, notes text, created_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.schedule_type,
        s.frequency, s.material_product_id, s.offering_id, s.quantity, s.loan_duration_days,
        s.delivery_address, s.next_run_date, s.last_run_date, s.status, s.notes,
        s.created_by_user_id, s.created_at, s.updated_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_recurring_schedules s
    JOIN material_products p ON s.material_product_id = p.id
    WHERE s.status = 'Active' AND s.next_run_date <= p_as_of_date
    ORDER BY s.next_run_date ASC;
$$;


--
-- Name: fn_get_emergency_contact_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_emergency_contact_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, scope character varying, category character varying, name character varying, phone_number character varying, alternate_phone_number character varying, country_region character varying, address text, instructions text, verification_status character varying, verified_by character varying, verified_at timestamp with time zone, review_due_at timestamp with time zone, is_active boolean, display_order integer, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    FROM emergency_contacts
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;


--
-- Name: fn_get_emergency_contacts(uuid, uuid, uuid, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_emergency_contacts(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_active_only boolean DEFAULT false) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, scope character varying, category character varying, name character varying, phone_number character varying, alternate_phone_number character varying, country_region character varying, address text, instructions text, verification_status character varying, verified_by character varying, verified_at timestamp with time zone, review_due_at timestamp with time zone, is_active boolean, display_order integer, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    FROM emergency_contacts
    WHERE tenant_id = p_tenant_id
      AND (NOT p_active_only OR is_active = TRUE)
      AND (
          scope = 'Jurisdiction'
          OR (scope = 'Organization' AND (p_organization_id IS NULL OR organization_id IS NULL OR organization_id = p_organization_id))
          OR (scope = 'Branch' AND (p_branch_id IS NULL OR branch_id IS NULL OR branch_id = p_branch_id))
      )
    ORDER BY display_order ASC, name ASC;
$$;


--
-- Name: fn_get_end_of_day_pending_activities(uuid, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_activities(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, COALESCE(e.title, 'Planned Experience')::VARCHAR AS name
    FROM experience_schedules es
    LEFT JOIN experiences e ON es.experience_id = e.id
    LEFT JOIN classrooms cr ON es.classroom_id = cr.id
    WHERE es.tenant_id = p_tenant_id AND es.scheduled_date = p_date
      AND es.status IN ('Planned', 'Scheduled', 'Confirmed', 'Started', 'Active', 'InProgress')
      AND (p_branch_id IS NULL OR cr.branch_id = p_branch_id);
END;
$$;


--
-- Name: fn_get_end_of_day_pending_children(uuid, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_children(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ca.id, (c.first_name || ' ' || c.last_name)::VARCHAR AS name
    FROM child_attendance ca
    JOIN children c ON ca.child_id = c.id
    WHERE ca.tenant_id = p_tenant_id AND ca.attendance_date = p_date
      AND ca.status IN ('Present', 'Late') AND ca.departure_time IS NULL
      AND (p_branch_id IS NULL OR ca.branch_id = p_branch_id);
END;
$$;


--
-- Name: fn_get_end_of_day_pending_children(uuid, timestamp without time zone, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_children(p_tenant_id uuid, p_date timestamp without time zone, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_end_of_day_pending_children(p_tenant_id, p_date::DATE, p_branch_id);
$$;


--
-- Name: fn_get_end_of_day_pending_events(uuid, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_events(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.title::VARCHAR AS name
    FROM events ev
    WHERE ev.tenant_id = p_tenant_id AND ev.event_date = p_date
      AND ev.status IN ('Draft', 'Planned', 'Approved', 'Published', 'InProgress')
      AND (p_branch_id IS NULL OR ev.scope = 'Organization' OR ev.is_organization_wide = true OR ev.branch_id = p_branch_id);
END;
$$;


--
-- Name: fn_get_end_of_day_pending_teachers(uuid, date, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_teachers(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ta.id,
           COALESCE(t.display_name, u.display_name, u.first_name || ' ' || u.last_name, 'Educator')::VARCHAR AS name
    FROM teacher_attendance ta
    LEFT JOIN teachers t ON ta.teacher_id = t.id
    LEFT JOIN users u ON t.user_id = u.id OR ta.teacher_id = u.id
    WHERE ta.tenant_id = p_tenant_id AND ta.attendance_date = p_date
      AND ta.status IN ('Present', 'Late') AND ta.clock_out_time IS NULL
      AND (p_branch_id IS NULL OR ta.branch_id = p_branch_id);
END;
$$;


--
-- Name: fn_get_end_of_day_pending_teachers(uuid, timestamp without time zone, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_end_of_day_pending_teachers(p_tenant_id uuid, p_date timestamp without time zone, p_branch_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_end_of_day_pending_teachers(p_tenant_id, p_date::DATE, p_branch_id);
$$;


--
-- Name: fn_get_enrollment_application_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_enrollment_application_by_id(p_tenant_id uuid, p_application_id uuid) RETURNS TABLE(id uuid, branch_id uuid, branch_name character varying, child_first_name character varying, child_last_name character varying, date_of_birth date, gender character varying, parent_guardian_name character varying, parent_email character varying, parent_phone character varying, target_classroom_id uuid, target_classroom_name character varying, desired_start_date date, schedule_type character varying, pipeline_stage character varying, tour_date timestamp with time zone, tuition_monthly_rate numeric, deposit_amount numeric, sibling_discount_percent numeric, registration_fee numeric, internal_notes text, child_id uuid, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        a.id,
        a.branch_id,
        b.name AS branch_name,
        a.child_first_name,
        a.child_last_name,
        a.date_of_birth,
        a.gender,
        a.parent_guardian_name,
        a.parent_email,
        a.parent_phone,
        a.target_classroom_id,
        c.name AS target_classroom_name,
        a.desired_start_date,
        a.schedule_type,
        a.pipeline_stage,
        a.tour_date,
        a.tuition_monthly_rate,
        a.deposit_amount,
        a.sibling_discount_percent,
        a.registration_fee,
        a.internal_notes,
        a.child_id,
        a.created_at
    FROM enrollment_pipeline_applications a
    LEFT JOIN branches b ON a.branch_id = b.id
    LEFT JOIN classrooms c ON a.target_classroom_id = c.id
    WHERE a.tenant_id = p_tenant_id AND a.id = p_application_id;
$$;


--
-- Name: fn_get_enrollment_applications(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_enrollment_applications(p_tenant_id uuid, p_branch_id uuid DEFAULT NULL::uuid, p_stage character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, branch_id uuid, branch_name character varying, child_first_name character varying, child_last_name character varying, date_of_birth date, gender character varying, parent_guardian_name character varying, parent_email character varying, parent_phone character varying, target_classroom_id uuid, target_classroom_name character varying, desired_start_date date, schedule_type character varying, pipeline_stage character varying, tour_date timestamp with time zone, tuition_monthly_rate numeric, deposit_amount numeric, sibling_discount_percent numeric, registration_fee numeric, internal_notes text, child_id uuid, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        a.id,
        a.branch_id,
        b.name AS branch_name,
        a.child_first_name,
        a.child_last_name,
        a.date_of_birth,
        a.gender,
        a.parent_guardian_name,
        a.parent_email,
        a.parent_phone,
        a.target_classroom_id,
        c.name AS target_classroom_name,
        a.desired_start_date,
        a.schedule_type,
        a.pipeline_stage,
        a.tour_date,
        a.tuition_monthly_rate,
        a.deposit_amount,
        a.sibling_discount_percent,
        a.registration_fee,
        a.internal_notes,
        a.child_id,
        a.created_at
    FROM enrollment_pipeline_applications a
    LEFT JOIN branches b ON a.branch_id = b.id
    LEFT JOIN classrooms c ON a.target_classroom_id = c.id
    WHERE a.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR a.branch_id = p_branch_id)
      AND (p_stage IS NULL OR a.pipeline_stage = p_stage)
    ORDER BY a.created_at DESC;
$$;


--
-- Name: fn_get_event_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    WHERE ev.id = p_id AND ev.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_event_classrooms(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_classrooms(p_event_id uuid) RETURNS TABLE(classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ec.classroom_id FROM event_classrooms ec WHERE ec.event_id = p_event_id;
END;
$$;


--
-- Name: fn_get_event_experiences(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_experiences(p_event_id uuid) RETURNS TABLE(id uuid, event_id uuid, experience_id uuid, experience_schedule_id uuid, experience_session_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ee.id, ee.event_id, ee.experience_id, ee.experience_schedule_id, ee.experience_session_id
    FROM event_experiences ee
    WHERE ee.event_id = p_event_id;
END;
$$;


--
-- Name: fn_get_event_rsvp(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_rsvp(p_event_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, event_id uuid, child_id uuid, guardian_id uuid, status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT er.id, er.event_id, er.child_id, er.guardian_id, er.status, er.notes, er.created_at, er.updated_at
    FROM event_rsvps er
    WHERE er.event_id = p_event_id AND er.child_id = p_child_id;
END;
$$;


--
-- Name: fn_get_event_rsvps(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_rsvps(p_event_id uuid) RETURNS TABLE(id uuid, event_id uuid, child_id uuid, guardian_id uuid, status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT er.id, er.event_id, er.child_id, er.guardian_id, er.status, er.notes, er.created_at, er.updated_at
    FROM event_rsvps er
    WHERE er.event_id = p_event_id;
END;
$$;


--
-- Name: fn_get_event_tasks(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_tasks(p_event_id uuid) RETURNS TABLE(id uuid, event_id uuid, description character varying, owner_id uuid, due_date timestamp with time zone, is_completed boolean, completed_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT et.id, et.event_id, et.description, et.owner_id, et.due_date,
           et.is_completed, et.completed_at, et.created_at, et.updated_at
    FROM event_tasks et
    WHERE et.event_id = p_event_id
    ORDER BY et.created_at;
END;
$$;


--
-- Name: fn_get_event_types(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_event_types(p_tenant_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, description text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT et.id, et.tenant_id, et.name, et.description, et.is_active, et.created_at, et.updated_at
    FROM event_types et
    WHERE et.tenant_id = p_tenant_id AND et.is_active = true
    ORDER BY et.name;
END;
$$;


--
-- Name: fn_get_events_by_branch(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_events_by_branch(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = ec.classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND (ev.scope = 'Organization' 
           OR ev.is_organization_wide = true 
           OR (ev.scope = 'Branch' AND ev.branch_id = p_branch_id)
           OR (c.branch_id = p_branch_id))
    ORDER BY ev.event_date DESC, ev.start_time ASC NULLS LAST;
END;
$$;


--
-- Name: fn_get_events_by_classroom(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_events_by_classroom(p_tenant_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = p_classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND (ev.scope = 'Organization' 
           OR ev.is_organization_wide = true 
           OR (ev.scope = 'Branch' AND ev.branch_id = c.branch_id)
           OR (ec.classroom_id = p_classroom_id))
    ORDER BY ev.event_date DESC, ev.start_time ASC NULLS LAST;
END;
$$;


--
-- Name: fn_get_events_for_date(uuid, date, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_events_for_date(p_tenant_id uuid, p_date date, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT ev.id, ev.tenant_id, ev.title, ev.description, ev.event_type_id,
           ev.event_date, ev.start_time, ev.end_time, ev.location, ev.organizer_id,
           ev.status, ev.scope, ev.branch_id, ev.is_organization_wide,
           ev.photo_urls, ev.created_at, ev.updated_at
    FROM events ev
    LEFT JOIN event_classrooms ec ON ev.id = ec.event_id
    LEFT JOIN classrooms c ON c.id = p_classroom_id
    LEFT JOIN classrooms ec_c ON ec_c.id = ec.classroom_id
    WHERE ev.tenant_id = p_tenant_id 
      AND ev.event_date = p_date
      AND (
          (p_classroom_id IS NOT NULL AND (
              ev.scope = 'Organization' 
              OR ev.is_organization_wide = true 
              OR (ev.scope = 'Branch' AND ev.branch_id = c.branch_id) 
              OR (ec.classroom_id = p_classroom_id)
          ))
          OR
          (p_classroom_id IS NULL AND p_branch_id IS NOT NULL AND (
              ev.scope = 'Organization' 
              OR ev.is_organization_wide = true 
              OR (ev.scope = 'Branch' AND ev.branch_id = p_branch_id) 
              OR (ec_c.branch_id = p_branch_id)
          ))
          OR
          (p_classroom_id IS NULL AND p_branch_id IS NULL)
      )
    ORDER BY ev.start_time ASC NULLS LAST;
END;
$$;


--
-- Name: fn_get_events_for_date(uuid, timestamp without time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_events_for_date(p_tenant_id uuid, p_date timestamp without time zone, p_branch_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_events_for_date(p_tenant_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_events_for_date(uuid, timestamp with time zone, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_events_for_date(p_tenant_id uuid, p_date timestamp with time zone, p_branch_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, title character varying, description text, event_type_id uuid, event_date date, start_time time without time zone, end_time time without time zone, location character varying, organizer_id uuid, status character varying, scope character varying, branch_id uuid, is_organization_wide boolean, photo_urls text[], created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_events_for_date(p_tenant_id, p_date::DATE, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_evidence_for_journey_entry(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_evidence_for_journey_entry(p_journey_entry_id uuid) RETURNS TABLE(id uuid, journey_entry_id uuid, evidence_type character varying, media_id uuid, text_content text, captured_at timestamp with time zone, captured_by uuid, visibility character varying, created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.journey_entry_id, cje.evidence_type, cje.media_id,
           cje.text_content, cje.captured_at, cje.captured_by, cje.visibility, cje.created_at
    FROM child_journey_evidence cje
    WHERE cje.journey_entry_id = p_journey_entry_id
    ORDER BY cje.created_at;
END;
$$;


--
-- Name: experience_audit_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_audit_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    organization_id uuid,
    platform_experience_id uuid,
    action character varying(100) NOT NULL,
    performed_by_user_id uuid NOT NULL,
    details jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: fn_get_experience_audit_events(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_audit_events(p_organization_id uuid DEFAULT NULL::uuid, p_platform_experience_id uuid DEFAULT NULL::uuid) RETURNS SETOF public.experience_audit_events
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM experience_audit_events
    WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_platform_experience_id IS NULL OR platform_experience_id = p_platform_experience_id)
    ORDER BY created_at DESC
    LIMIT 200;
$$;


--
-- Name: fn_get_experience_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, title character varying, description text, status character varying, dna_payload jsonb, created_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone, average_stars numeric)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload,
           e.created_by_user_id, e.created_at, e.updated_at,
           COALESCE(AVG(ef.stars), 0)::NUMERIC AS average_stars
    FROM experiences e
    LEFT JOIN experience_schedules es ON e.id = es.experience_id
    LEFT JOIN experience_feedback ef ON es.id = ef.experience_schedule_id
    WHERE e.id = p_id AND e.tenant_id = p_tenant_id
    GROUP BY e.id, e.tenant_id, e.organization_id, e.title, e.description, e.status, e.dna_payload, e.created_by_user_id, e.created_at, e.updated_at;
END;
$$;


--
-- Name: fn_get_experience_feedback_by_experience_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_feedback_by_experience_id(p_tenant_id uuid, p_experience_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating character varying, notes text, created_at timestamp with time zone)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating::VARCHAR, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.experience_id = p_experience_id AND ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;


--
-- Name: fn_get_experience_feedback_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_feedback_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating integer, notes text, created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.id = p_id AND ef.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_experience_feedback_by_schedule_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_feedback_by_schedule_id(p_tenant_id uuid, p_schedule_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_schedule_id uuid, experience_id uuid, teacher_id uuid, stars integer, rating integer, notes text, created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ef.id, ef.tenant_id, ef.experience_schedule_id, ef.experience_id,
           ef.teacher_id, ef.stars, ef.rating, ef.notes, ef.created_at
    FROM experience_feedback ef
    WHERE ef.experience_schedule_id = p_schedule_id AND ef.tenant_id = p_tenant_id
    ORDER BY ef.created_at DESC;
END;
$$;


--
-- Name: fn_get_experience_harvest_request_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_harvest_request_by_id(p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, source_experience_id uuid, source_version_number integer, license_id uuid, submitted_by_user_id uuid, submitted_at timestamp with time zone, status character varying, review_notes text, reviewed_by_user_id uuid, reviewed_at timestamp with time zone, safety_suitability_verified boolean, learning_objectives_verified boolean, instructions_verified boolean, materials_verified boolean, media_rights_verified boolean, commercial_rights_verified boolean, source_experience_title character varying, organization_name character varying, submitter_email character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT r.id AS id, r.tenant_id AS tenant_id, r.organization_id AS organization_id, r.source_experience_id AS source_experience_id,
           r.source_version_number AS source_version_number, r.license_id AS license_id, r.submitted_by_user_id AS submitted_by_user_id,
           r.submitted_at AS submitted_at, r.status AS status, r.review_notes AS review_notes, r.reviewed_by_user_id AS reviewed_by_user_id,
           r.reviewed_at AS reviewed_at, r.safety_suitability_verified AS safety_suitability_verified,
           r.learning_objectives_verified AS learning_objectives_verified, r.instructions_verified AS instructions_verified,
           r.materials_verified AS materials_verified, r.media_rights_verified AS media_rights_verified,
           r.commercial_rights_verified AS commercial_rights_verified,
           e.title AS source_experience_title, o.name AS organization_name, u.email AS submitter_email
    FROM experience_harvest_requests r
    LEFT JOIN experiences e ON r.source_experience_id = e.id
    LEFT JOIN organizations o ON r.organization_id = o.id
    LEFT JOIN users u ON r.submitted_by_user_id = u.id
    WHERE r.id = p_id;
$$;


--
-- Name: fn_get_experience_harvest_requests(uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_harvest_requests(p_organization_id uuid DEFAULT NULL::uuid, p_status character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, source_experience_id uuid, source_version_number integer, license_id uuid, submitted_by_user_id uuid, submitted_at timestamp with time zone, status character varying, review_notes text, reviewed_by_user_id uuid, reviewed_at timestamp with time zone, safety_suitability_verified boolean, learning_objectives_verified boolean, instructions_verified boolean, materials_verified boolean, media_rights_verified boolean, commercial_rights_verified boolean, source_experience_title character varying, organization_name character varying, submitter_email character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT r.id AS id, r.tenant_id AS tenant_id, r.organization_id AS organization_id, r.source_experience_id AS source_experience_id,
           r.source_version_number AS source_version_number, r.license_id AS license_id, r.submitted_by_user_id AS submitted_by_user_id,
           r.submitted_at AS submitted_at, r.status AS status, r.review_notes AS review_notes, r.reviewed_by_user_id AS reviewed_by_user_id,
           r.reviewed_at AS reviewed_at, r.safety_suitability_verified AS safety_suitability_verified,
           r.learning_objectives_verified AS learning_objectives_verified, r.instructions_verified AS instructions_verified,
           r.materials_verified AS materials_verified, r.media_rights_verified AS media_rights_verified,
           r.commercial_rights_verified AS commercial_rights_verified,
           e.title AS source_experience_title, o.name AS organization_name, u.email AS submitter_email
    FROM experience_harvest_requests r
    LEFT JOIN experiences e ON r.source_experience_id = e.id
    LEFT JOIN organizations o ON r.organization_id = o.id
    LEFT JOIN users u ON r.submitted_by_user_id = u.id
    WHERE (p_organization_id IS NULL OR r.organization_id = p_organization_id)
      AND (p_status IS NULL OR r.status = p_status)
    ORDER BY r.submitted_at DESC;
$$;


--
-- Name: fn_get_experience_memories(uuid, uuid, timestamp with time zone, timestamp with time zone, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_memories(p_tenant_id uuid, p_experience_id uuid, p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_limit integer DEFAULT 100, p_offset integer DEFAULT 0) RETURNS TABLE(id uuid, tenant_id uuid, event_date timestamp with time zone, event_type character varying, title character varying, description text, source_id uuid, source_type character varying, child_id uuid, classroom_id uuid, teacher_id uuid, experience_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            NULL::UUID AS classroom_id,
            cs.teacher_id,
            es.experience_id
        FROM child_stories cs
        JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        WHERE cs.tenant_id = p_tenant_id AND es.experience_id = p_experience_id

        UNION ALL
        
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.experience_id = p_experience_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.experience_id = p_experience_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND s.experience_id = p_experience_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


--
-- Name: fn_get_experience_schedule_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedule_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.id = p_id AND es.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_experience_schedules_by_classroom_and_date_range(uuid, uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id uuid, p_classroom_id uuid, p_start_date date, p_end_date date) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.tenant_id = p_tenant_id AND es.classroom_id = p_classroom_id
      AND es.scheduled_date >= p_start_date AND es.scheduled_date <= p_end_date
    ORDER BY es.scheduled_date ASC;
END;
$$;


--
-- Name: fn_get_experience_schedules_by_classroom_and_date_range(uuid, uuid, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id uuid, p_classroom_id uuid, p_start_date timestamp without time zone, p_end_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE);
$$;


--
-- Name: fn_get_experience_schedules_by_classroom_and_date_range(uuid, uuid, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id uuid, p_classroom_id uuid, p_start_date timestamp with time zone, p_end_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_classroom_and_date_range(p_tenant_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE);
$$;


--
-- Name: fn_get_experience_schedules_by_date(uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_date(p_tenant_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, es.classroom_id,
           es.scheduled_date, es.time_of_day, es.status,
           es.primary_teacher_id, es.planned_start_time,
           es.planned_end_time, es.preparation_notes,
           es.created_at, es.updated_at
    FROM experience_schedules es
    WHERE es.tenant_id = p_tenant_id AND es.scheduled_date = p_date
    ORDER BY es.planned_start_time ASC, es.time_of_day ASC;
END;
$$;


--
-- Name: fn_get_experience_schedules_by_date(uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_date(p_tenant_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_date(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_experience_schedules_by_date(uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_schedules_by_date(p_tenant_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, primary_teacher_id uuid, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_experience_schedules_by_date(p_tenant_id, p_date::DATE);
$$;


--
-- Name: fn_get_experience_session_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_session_by_id(p_tenant_id uuid, p_session_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, experience_schedule_id uuid, classroom_id uuid, session_date date, start_time time without time zone, end_time time without time zone, status character varying, primary_teacher_id uuid, actual_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.id = p_session_id AND es.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_experience_session_by_schedule_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_session_by_schedule_id(p_schedule_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, experience_schedule_id uuid, classroom_id uuid, session_date date, start_time time without time zone, end_time time without time zone, status character varying, primary_teacher_id uuid, actual_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.experience_schedule_id = p_schedule_id;
END;
$$;


--
-- Name: fn_get_experience_sessions_for_classroom(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_sessions_for_classroom(p_tenant_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, experience_schedule_id uuid, classroom_id uuid, session_date date, start_time time without time zone, end_time time without time zone, status character varying, primary_teacher_id uuid, actual_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT es.id, es.tenant_id, es.experience_id, 
           es.experience_schedule_id, es.classroom_id, 
           es.session_date, es.start_time, es.end_time, 
           es.status, es.primary_teacher_id, es.actual_notes, 
           es.created_at, es.updated_at
    FROM experience_sessions es
    WHERE es.classroom_id = p_classroom_id AND es.tenant_id = p_tenant_id
    ORDER BY es.session_date DESC, es.start_time DESC;
END;
$$;


--
-- Name: fn_get_experience_versions(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_experience_versions(p_experience_id uuid) RETURNS TABLE(id uuid, experience_id uuid, version_number integer, title character varying, description text, dna_payload jsonb, change_reason character varying, modified_by_user_id uuid, created_at timestamp with time zone)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    SELECT ev.id, ev.experience_id, ev.version_number, ev.title::VARCHAR, ev.description, ev.dna_payload, ev.change_reason::VARCHAR, ev.modified_by_user_id, ev.created_at
    FROM experience_versions ev
    WHERE ev.experience_id = p_experience_id
    ORDER BY ev.version_number DESC;
END;
$$;


--
-- Name: fn_get_family_pickup_relationships(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_family_pickup_relationships(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, child_id uuid, person_id uuid, relationship character varying, is_primary boolean, is_emergency boolean, full_name character varying, phone character varying, photo_url text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH guardians AS (
        SELECT 
            fr.id AS g_id,
            fr.child_id AS g_child_id,
            COALESCE(p.id, u.id) AS g_person_id,
            COALESCE(fr.relationship, p.relationship, 'Legal Guardian')::VARCHAR AS g_relationship,
            fr.is_primary AS g_is_primary,
            FALSE AS g_is_emergency,
            COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), NULLIF(TRIM(u.first_name || ' ' || u.last_name), ''), u.display_name, 'Parent / Guardian')::VARCHAR AS g_full_name,
            COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS g_phone,
            NULL::TEXT AS g_photo_url
        FROM family_relationships fr
        JOIN users u ON fr.user_id = u.id
        LEFT JOIN persons p ON p.user_id = u.id
        WHERE fr.tenant_id = p_tenant_id AND fr.child_id = p_child_id
          AND (fr.status IS NULL OR fr.status = 'Verified')
          AND fr.revoked_at IS NULL

        UNION ALL

        SELECT 
            cg.id AS g_id,
            cg.child_id AS g_child_id,
            p.id AS g_person_id,
            COALESCE(cg.relationship, p.relationship, 'Legal Guardian')::VARCHAR AS g_relationship,
            cg.is_primary AS g_is_primary,
            FALSE AS g_is_emergency,
            COALESCE(NULLIF(TRIM(p.first_name || ' ' || p.last_name), ''), 'Parent / Guardian')::VARCHAR AS g_full_name,
            COALESCE(p.mobile_phone, p.alternate_phone)::VARCHAR AS g_phone,
            NULL::TEXT AS g_photo_url
        FROM child_guardians cg
        JOIN persons p ON cg.person_id = p.id
        WHERE cg.tenant_id = p_tenant_id AND cg.child_id = p_child_id
          AND (cg.can_pickup = TRUE OR cg.is_primary = TRUE)
    )
    SELECT DISTINCT ON (g_person_id)
           g_id, g_child_id, g_person_id, g_relationship, g_is_primary, g_is_emergency, g_full_name, g_phone, g_photo_url
    FROM guardians
    ORDER BY g_person_id, g_is_primary DESC;
END;
$$;


--
-- Name: fn_get_filtered_experience_schedules(uuid, date, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_filtered_experience_schedules(p_tenant_id uuid, p_date date, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT s.id, s.tenant_id, s.experience_id, 
           s.classroom_id, s.scheduled_date, 
           s.time_of_day, s.status, s.planned_start_time, 
           s.planned_end_time, s.preparation_notes
    FROM experience_schedules s
    JOIN classrooms c ON s.classroom_id = c.id
    JOIN branches b ON c.branch_id = b.id
    WHERE s.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR b.organization_id = p_organization_id)
      AND s.scheduled_date = p_date
      AND (p_classroom_id IS NULL OR s.classroom_id = p_classroom_id)
      AND (p_branch_id IS NULL OR c.branch_id = p_branch_id)
    ORDER BY s.planned_start_time ASC NULLS LAST, s.time_of_day ASC;
END;
$$;


--
-- Name: fn_get_filtered_experience_schedules(uuid, timestamp without time zone, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_filtered_experience_schedules(p_tenant_id uuid, p_date timestamp without time zone, p_organization_id uuid, p_branch_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_filtered_experience_schedules(p_tenant_id, p_date::DATE, p_organization_id, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_filtered_experience_schedules(uuid, timestamp with time zone, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_filtered_experience_schedules(p_tenant_id uuid, p_date timestamp with time zone, p_organization_id uuid, p_branch_id uuid, p_classroom_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, experience_id uuid, classroom_id uuid, scheduled_date date, time_of_day character varying, status character varying, planned_start_time time without time zone, planned_end_time time without time zone, preparation_notes text)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_filtered_experience_schedules(p_tenant_id, p_date::DATE, p_organization_id, p_branch_id, p_classroom_id);
$$;


--
-- Name: fn_get_floor_map_history(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_floor_map_history(p_tenant_id uuid, p_branch_id uuid, p_floor_level character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, name character varying, building_area character varying, floor_level character varying, description text, file_storage_key character varying, file_type character varying, file_size bigint, content_hash character varying, version integer, status character varying, review_due_at timestamp with time zone, created_by character varying, created_at timestamp with time zone, published_by character varying, published_at timestamp with time zone, supersedes_map_id uuid, row_version integer, emergency_instructions text)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id
      AND branch_id = p_branch_id
      AND floor_level = p_floor_level
    ORDER BY version DESC, created_at DESC;
$$;


--
-- Name: fn_get_floor_map_markers(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_floor_map_markers(p_floor_map_id uuid) RETURNS TABLE(id uuid, floor_map_id uuid, marker_type character varying, label character varying, instructions text, normalized_x double precision, normalized_y double precision, display_order integer, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at
    FROM floor_map_markers
    WHERE floor_map_id = p_floor_map_id
    ORDER BY display_order ASC, created_at ASC;
$$;


--
-- Name: fn_get_floor_map_markers_by_map_ids(uuid[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_floor_map_markers_by_map_ids(p_map_ids uuid[]) RETURNS TABLE(id uuid, floor_map_id uuid, marker_type character varying, label character varying, instructions text, normalized_x double precision, normalized_y double precision, display_order integer, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at
    FROM floor_map_markers
    WHERE floor_map_id = ANY(p_map_ids)
    ORDER BY display_order ASC;
$$;


--
-- Name: fn_get_harvest_monthly_metrics(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_harvest_monthly_metrics(p_tenant_id uuid) RETURNS TABLE(total_experiences integer, total_feedbacks integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(*)::INT 
         FROM experience_schedules es 
         WHERE es.tenant_id = p_tenant_id 
           AND EXTRACT(MONTH FROM es.scheduled_date) = EXTRACT(MONTH FROM CURRENT_DATE) 
           AND EXTRACT(YEAR FROM es.scheduled_date) = EXTRACT(YEAR FROM CURRENT_DATE)) AS total_experiences,
        (SELECT COUNT(*)::INT 
         FROM experience_feedback f
         JOIN experience_schedules s ON f.experience_schedule_id = s.id
         WHERE f.tenant_id = p_tenant_id 
           AND EXTRACT(MONTH FROM s.scheduled_date) = EXTRACT(MONTH FROM CURRENT_DATE) 
           AND EXTRACT(YEAR FROM s.scheduled_date) = EXTRACT(YEAR FROM CURRENT_DATE)) AS total_feedbacks;
END;
$$;


--
-- Name: fn_get_harvest_performance_items(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_harvest_performance_items(p_tenant_id uuid) RETURNS TABLE(experience_id uuid, title character varying, subtitle text, uses integer, response text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.id AS experience_id,
        e.title,
        ('Created by ' || COALESCE((SELECT u.first_name FROM users u WHERE u.id = e.created_by_user_id), 'Educator'))::TEXT AS subtitle,
        (SELECT COUNT(*)::INT FROM experience_schedules es WHERE es.experience_id = e.id) AS uses,
        ('💬 ' || (SELECT COUNT(1) FROM experience_feedback f JOIN experience_schedules s ON f.experience_schedule_id = s.id WHERE s.experience_id = e.id))::TEXT AS response
    FROM experiences e
    WHERE e.tenant_id = p_tenant_id AND e.status NOT IN ('Idea', 'Draft', 'Archived')
    ORDER BY uses DESC
    LIMIT 10;
END;
$$;


--
-- Name: fn_get_intelligence_curriculum_variety(uuid, uuid, uuid, uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_curriculum_variety(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date date, p_end_date date) RETURNS TABLE(title character varying, usage_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        e.title AS title,
        COUNT(es.id) AS usage_count
    FROM experience_schedules es
    JOIN experiences e ON es.experience_id = e.id
    JOIN classrooms cl ON es.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE es.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND es.scheduled_date BETWEEN p_start_date AND p_end_date
    GROUP BY e.title
    ORDER BY usage_count DESC;
$$;


--
-- Name: fn_get_intelligence_curriculum_variety(uuid, uuid, uuid, uuid, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_curriculum_variety(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date timestamp without time zone, p_end_date timestamp without time zone) RETURNS TABLE(title character varying, usage_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_curriculum_variety(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;


--
-- Name: fn_get_intelligence_curriculum_variety(uuid, uuid, uuid, uuid, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_curriculum_variety(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date timestamp with time zone, p_end_date timestamp with time zone) RETURNS TABLE(title character varying, usage_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_curriculum_variety(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;


--
-- Name: fn_get_intelligence_data_quality(uuid, uuid, uuid, uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_data_quality(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date date, p_end_date date) RETURNS TABLE(missing_departure_times_count integer, drafts_awaiting_review_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        (SELECT COUNT(*)::int 
         FROM child_attendance ca
         JOIN classrooms cl ON ca.classroom_id = cl.id
         JOIN branches b ON cl.branch_id = b.id
         WHERE ca.tenant_id = p_tenant_id
           AND b.organization_id = p_org_id
           AND (p_branch_id IS NULL OR b.id = p_branch_id)
           AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
           AND ca.status = 'Present'
           AND ca.departure_time IS NULL
           AND ca.attendance_date < CURRENT_DATE
           AND ca.attendance_date BETWEEN p_start_date AND p_end_date) AS missing_departure_times_count,
        (SELECT COUNT(*)::int 
         FROM parent_communications pc
         JOIN classrooms cl ON pc.classroom_id = cl.id
         JOIN branches b ON cl.branch_id = b.id
         WHERE pc.tenant_id = p_tenant_id
           AND b.organization_id = p_org_id
           AND (p_branch_id IS NULL OR b.id = p_branch_id)
           AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
           AND pc.status IN ('Draft', 'PendingReview')) AS drafts_awaiting_review_count;
$$;


--
-- Name: fn_get_intelligence_data_quality(uuid, uuid, uuid, uuid, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_data_quality(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date timestamp without time zone, p_end_date timestamp without time zone) RETURNS TABLE(missing_departure_times_count integer, drafts_awaiting_review_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_data_quality(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;


--
-- Name: fn_get_intelligence_data_quality(uuid, uuid, uuid, uuid, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_data_quality(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date timestamp with time zone, p_end_date timestamp with time zone) RETURNS TABLE(missing_departure_times_count integer, drafts_awaiting_review_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_data_quality(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_start_date::DATE, p_end_date::DATE
    );
$$;


--
-- Name: fn_get_intelligence_operational_overview(uuid, uuid, uuid, uuid, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_operational_overview(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_start_date timestamp with time zone, p_end_date timestamp with time zone) RETURNS TABLE(total_attendance integer, present_count integer, absent_count integer, late_count integer, total_classrooms integer, planned_classrooms integer, planned_experiences integer, completed_experiences integer, upcoming_experiences integer, moments_count integer, care_count integer)
    LANGUAGE sql STABLE
    AS $$
    WITH att AS (
        SELECT 
            COUNT(*)::int AS total_att,
            COUNT(*) FILTER (WHERE ca.status = 'Present')::int AS present_cnt,
            COUNT(*) FILTER (WHERE ca.status = 'Absent')::int AS absent_cnt,
            COUNT(*) FILTER (WHERE ca.status = 'Late')::int AS late_cnt
        FROM child_attendance ca
        JOIN classrooms cl ON ca.classroom_id = cl.id
        JOIN branches b ON cl.branch_id = b.id
        WHERE ca.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND ca.attendance_date BETWEEN p_start_date::date AND p_end_date::date
    ),
    cls AS (
        SELECT 
            COUNT(DISTINCT cl.id)::int AS total_cls,
            COUNT(DISTINCT es.classroom_id)::int AS planned_cls
        FROM classrooms cl
        JOIN branches b ON cl.branch_id = b.id
        LEFT JOIN experience_schedules es ON cl.id = es.classroom_id 
             AND es.tenant_id = p_tenant_id
             AND es.scheduled_date BETWEEN p_start_date::date AND p_end_date::date
        WHERE cl.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND cl.is_active = true
    ),
    exp AS (
        SELECT 
            COUNT(*)::int AS planned_exp,
            COUNT(*) FILTER (WHERE es.status IN ('Completed', 'Finished'))::int AS completed_exp,
            COUNT(*) FILTER (WHERE es.status IN ('Confirmed', 'Scheduled'))::int AS upcoming_exp
        FROM experience_schedules es
        JOIN classrooms cl ON es.classroom_id = cl.id
        JOIN branches b ON cl.branch_id = b.id
        WHERE es.tenant_id = p_tenant_id
          AND b.organization_id = p_org_id
          AND (p_branch_id IS NULL OR b.id = p_branch_id)
          AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
          AND es.scheduled_date BETWEEN p_start_date::date AND p_end_date::date
    ),
    act AS (
        SELECT 
            (SELECT COUNT(*)::int 
             FROM child_journey_entries cje
             JOIN classrooms cl ON cje.classroom_id = cl.id
             JOIN branches b ON cl.branch_id = b.id
             WHERE cje.tenant_id = p_tenant_id
               AND b.organization_id = p_org_id
               AND (p_branch_id IS NULL OR b.id = p_branch_id)
               AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
               AND cje.observation_date >= p_start_date::date 
               AND cje.observation_date <= p_end_date::date) AS moments_cnt,
            (SELECT COUNT(*)::int
             FROM child_care_events cce
             JOIN classrooms cl ON cce.classroom_id = cl.id
             JOIN branches b ON cl.branch_id = b.id
             WHERE cce.tenant_id = p_tenant_id
               AND b.organization_id = p_org_id
               AND (p_branch_id IS NULL OR b.id = p_branch_id)
               AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
               AND cce.event_time >= p_start_date 
               AND cce.event_time <= p_end_date) AS care_cnt
    )
    SELECT 
        att.total_att, att.present_cnt, att.absent_cnt, att.late_cnt,
        cls.total_cls, cls.planned_cls,
        exp.planned_exp, exp.completed_exp, exp.upcoming_exp,
        act.moments_cnt, act.care_cnt
    FROM att, cls, exp, act;
$$;


--
-- Name: fn_get_intelligence_overdue_plans_count(uuid, uuid, uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_overdue_plans_count(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today date) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT COUNT(*)::int
    FROM experience_schedules es
    JOIN classrooms cl ON es.classroom_id = cl.id
    JOIN branches b ON cl.branch_id = b.id
    WHERE es.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND es.scheduled_date < p_today
      AND es.status IN ('Confirmed', 'Scheduled');
$$;


--
-- Name: fn_get_intelligence_overdue_plans_count(uuid, uuid, uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_overdue_plans_count(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today timestamp without time zone) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT public.fn_get_intelligence_overdue_plans_count(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE
    );
$$;


--
-- Name: fn_get_intelligence_overdue_plans_count(uuid, uuid, uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_overdue_plans_count(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today timestamp with time zone) RETURNS integer
    LANGUAGE sql STABLE
    AS $$
    SELECT public.fn_get_intelligence_overdue_plans_count(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE
    );
$$;


--
-- Name: fn_get_intelligence_planning_gaps(uuid, uuid, uuid, uuid, date, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_planning_gaps(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today date, p_seven_days_out date) RETURNS TABLE(classroom_id uuid, classroom_name character varying, branch_name character varying, upcoming_plans_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        cl.id AS classroom_id,
        cl.name AS classroom_name,
        b.name AS branch_name,
        COUNT(es.id) AS upcoming_plans_count
    FROM classrooms cl
    JOIN branches b ON cl.branch_id = b.id
    LEFT JOIN experience_schedules es ON cl.id = es.classroom_id
         AND es.tenant_id = p_tenant_id
         AND es.scheduled_date BETWEEN p_today AND p_seven_days_out
         AND es.status NOT IN ('Cancelled')
    WHERE cl.tenant_id = p_tenant_id
      AND b.organization_id = p_org_id
      AND (p_branch_id IS NULL OR b.id = p_branch_id)
      AND (p_classroom_id IS NULL OR cl.id = p_classroom_id)
      AND cl.is_active = true
    GROUP BY cl.id, cl.name, b.name
    HAVING COUNT(es.id) < 2
    ORDER BY upcoming_plans_count ASC, cl.name;
$$;


--
-- Name: fn_get_intelligence_planning_gaps(uuid, uuid, uuid, uuid, timestamp without time zone, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_planning_gaps(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today timestamp without time zone, p_seven_days_out timestamp without time zone) RETURNS TABLE(classroom_id uuid, classroom_name character varying, branch_name character varying, upcoming_plans_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_planning_gaps(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE, p_seven_days_out::DATE
    );
$$;


--
-- Name: fn_get_intelligence_planning_gaps(uuid, uuid, uuid, uuid, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_intelligence_planning_gaps(p_tenant_id uuid, p_org_id uuid, p_branch_id uuid, p_classroom_id uuid, p_today timestamp with time zone, p_seven_days_out timestamp with time zone) RETURNS TABLE(classroom_id uuid, classroom_name character varying, branch_name character varying, upcoming_plans_count bigint)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_intelligence_planning_gaps(
        p_tenant_id, p_org_id, p_branch_id, p_classroom_id, p_today::DATE, p_seven_days_out::DATE
    );
$$;


--
-- Name: fn_get_inventory_balance(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_inventory_balance(p_product_id uuid, p_location_id uuid) RETURNS TABLE(id uuid, material_product_id uuid, location_id uuid, total_stock integer, available_stock integer, reserved_stock integer, dispatched_stock integer, on_loan_stock integer, under_inspection_stock integer, under_cleaning_stock integer, damaged_stock integer, missing_stock integer, retired_stock integer, updated_at timestamp with time zone, product_name character varying, product_sku character varying, location_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        b.id, b.material_product_id, b.location_id,
        b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock,
        b.damaged_stock, b.missing_stock, b.retired_stock, b.updated_at,
        p.name AS product_name, p.sku AS product_sku, l.name AS location_name
    FROM inventory_balances b
    JOIN material_products p ON b.material_product_id = p.id
    JOIN inventory_locations l ON b.location_id = l.id
    WHERE b.material_product_id = p_product_id AND b.location_id = p_location_id;
$$;


--
-- Name: fn_get_inventory_balances(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_inventory_balances(p_location_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, material_product_id uuid, location_id uuid, total_stock integer, available_stock integer, reserved_stock integer, dispatched_stock integer, on_loan_stock integer, under_inspection_stock integer, under_cleaning_stock integer, damaged_stock integer, missing_stock integer, retired_stock integer, updated_at timestamp with time zone, product_name character varying, product_sku character varying, location_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        b.id, b.material_product_id, b.location_id,
        b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock,
        b.damaged_stock, b.missing_stock, b.retired_stock, b.updated_at,
        p.name AS product_name, p.sku AS product_sku, l.name AS location_name
    FROM inventory_balances b
    JOIN material_products p ON b.material_product_id = p.id
    JOIN inventory_locations l ON b.location_id = l.id
    WHERE (p_location_id IS NULL OR b.location_id = p_location_id)
    ORDER BY p.name ASC;
$$;


--
-- Name: fn_get_inventory_locations(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_inventory_locations() RETURNS TABLE(id uuid, code character varying, name character varying, address text, contact_phone character varying, is_central_warehouse boolean, is_active boolean, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at
    FROM inventory_locations 
    WHERE is_active = TRUE 
    ORDER BY is_central_warehouse DESC, name ASC;
$$;


--
-- Name: fn_get_invoice_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_invoice_by_id(p_tenant_id uuid DEFAULT NULL::uuid, p_invoice_id uuid DEFAULT NULL::uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_number character varying, billing_period character varying, subtotal numeric, discount_amount numeric, tax_amount numeric, total_amount numeric, amount_paid numeric, currency character varying, status character varying, issue_date timestamp with time zone, due_date timestamp with time zone, paid_at timestamp with time zone, published_at timestamp with time zone, published_by character varying, voided_at timestamp with time zone, voided_by character varying, void_reason text, created_by character varying, description text, notes text, payment_instructions text, billing_contact_email character varying, document_storage_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE id = p_invoice_id
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id);
$$;


--
-- Name: fn_get_invoice_by_number(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_invoice_by_number(p_invoice_number character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_number character varying, billing_period character varying, subtotal numeric, discount_amount numeric, tax_amount numeric, total_amount numeric, amount_paid numeric, currency character varying, status character varying, issue_date timestamp with time zone, due_date timestamp with time zone, paid_at timestamp with time zone, published_at timestamp with time zone, published_by character varying, voided_at timestamp with time zone, voided_by character varying, void_reason text, created_by character varying, description text, notes text, payment_instructions text, billing_contact_email character varying, document_storage_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE invoice_number = p_invoice_number;
$$;


--
-- Name: fn_get_invoice_line_items(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_invoice_line_items(p_invoice_id uuid) RETURNS TABLE(id uuid, invoice_id uuid, line_number integer, description character varying, quantity numeric, unit_price numeric, line_total numeric, category character varying, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    FROM invoice_line_items
    WHERE invoice_id = p_invoice_id
    ORDER BY line_number ASC;
$$;


--
-- Name: fn_get_invoice_line_items_by_ids(uuid[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_invoice_line_items_by_ids(p_invoice_ids uuid[]) RETURNS TABLE(id uuid, invoice_id uuid, line_number integer, description character varying, quantity numeric, unit_price numeric, line_total numeric, category character varying, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    FROM invoice_line_items
    WHERE invoice_id = ANY(p_invoice_ids)
    ORDER BY line_number ASC;
$$;


--
-- Name: fn_get_invoices_for_organization(uuid, uuid, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_invoices_for_organization(p_tenant_id uuid, p_organization_id uuid, p_include_drafts boolean DEFAULT true) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_number character varying, billing_period character varying, subtotal numeric, discount_amount numeric, tax_amount numeric, total_amount numeric, amount_paid numeric, currency character varying, status character varying, issue_date timestamp with time zone, due_date timestamp with time zone, paid_at timestamp with time zone, published_at timestamp with time zone, published_by character varying, voided_at timestamp with time zone, voided_by character varying, void_reason text, created_by character varying, description text, notes text, payment_instructions text, billing_contact_email character varying, document_storage_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE organization_id = p_organization_id 
      AND tenant_id = p_tenant_id
      AND (p_include_drafts OR status != 'Draft')
    ORDER BY issue_date DESC, created_at DESC;
$$;


--
-- Name: fn_get_journey_entries_for_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_journey_entries_for_child(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, enrollment_id uuid, classroom_id uuid, teacher_id uuid, experience_id uuid, experience_schedule_id uuid, experience_session_id uuid, experience_participation_id uuid, little_moment_id uuid, milestone_id uuid, journey_type character varying, title character varying, observation text, child_voice text, teacher_reflection text, observation_date timestamp with time zone, participation_type character varying, is_milestone boolean, is_parent_visible boolean, parent_visibility_status character varying, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid, version integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.tenant_id, cje.child_id, cje.enrollment_id, cje.classroom_id,
           cje.teacher_id, cje.experience_id, cje.experience_schedule_id,
           cje.experience_session_id, cje.experience_participation_id,
           cje.little_moment_id, cje.milestone_id, cje.journey_type,
           cje.title, cje.observation, cje.child_voice, cje.teacher_reflection,
           cje.observation_date, cje.participation_type, cje.is_milestone,
           cje.is_parent_visible, cje.parent_visibility_status,
           cje.created_at, cje.created_by, cje.updated_at, cje.updated_by, cje.version
    FROM child_journey_entries cje
    WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    ORDER BY cje.observation_date DESC;
END;
$$;


--
-- Name: fn_get_journey_entry_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_journey_entry_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, enrollment_id uuid, classroom_id uuid, teacher_id uuid, experience_id uuid, experience_schedule_id uuid, experience_session_id uuid, experience_participation_id uuid, little_moment_id uuid, milestone_id uuid, journey_type character varying, title character varying, observation text, child_voice text, teacher_reflection text, observation_date timestamp with time zone, participation_type character varying, is_milestone boolean, is_parent_visible boolean, parent_visibility_status character varying, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid, version integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cje.id, cje.tenant_id, cje.child_id, cje.enrollment_id, cje.classroom_id,
           cje.teacher_id, cje.experience_id, cje.experience_schedule_id,
           cje.experience_session_id, cje.experience_participation_id,
           cje.little_moment_id, cje.milestone_id, cje.journey_type,
           cje.title, cje.observation, cje.child_voice, cje.teacher_reflection,
           cje.observation_date, cje.participation_type, cje.is_milestone,
           cje.is_parent_visible, cje.parent_visibility_status,
           cje.created_at, cje.created_by, cje.updated_at, cje.updated_by, cje.version
    FROM child_journey_entries cje
    WHERE cje.id = p_id AND cje.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_latest_child_journey_insight(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_latest_child_journey_insight(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, requested_by_user_id uuid, period_type character varying, period_start_date date, period_end_date date, focus_area character varying, teacher_question text, evidence_count integer, overall_summary text, evidence_gaps text, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id
    ORDER BY created_at DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_latest_media_consent_document(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_latest_media_consent_document(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, status character varying, document_title character varying, signed_at timestamp with time zone, withdrawn_at timestamp with time zone, declined_at timestamp with time zone, document_type character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.status::VARCHAR, d.document_title::VARCHAR,
           d.signed_at, d.withdrawn_at, d.declined_at,
           COALESCE(t.document_type, '')::VARCHAR AS document_type
    FROM child_consent_documents d
    LEFT JOIN consent_templates t ON d.consent_template_id = t.id
    WHERE d.tenant_id = p_tenant_id AND d.child_id = p_child_id
      AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
    ORDER BY d.created_at DESC
    LIMIT 1;
END;
$$;


--
-- Name: fn_get_latest_weekly_planning_draft_for_week(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_latest_weekly_planning_draft_for_week(p_tenant_id uuid, p_classroom_id uuid, p_week_start_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, requested_by_user_id uuid, week_start_date date, week_end_date date, focus_areas character varying, indoor_outdoor_preference character varying, materials_notes text, status character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, requested_by_user_id,
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference,
        materials_notes, status, created_at, updated_at
    FROM weekly_planning_draft_requests
    WHERE tenant_id = p_tenant_id AND classroom_id = p_classroom_id AND week_start_date = p_week_start_date
    ORDER BY created_at DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_logged_child_ids_for_session(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_logged_child_ids_for_session(p_tenant_id uuid, p_session_id uuid) RETURNS TABLE(child_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cje.child_id
    FROM child_journey_entries cje
    WHERE cje.tenant_id = p_tenant_id AND cje.experience_session_id = p_session_id;
END;
$$;


--
-- Name: fn_get_low_child_care_supplies_by_branch(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_low_child_care_supplies_by_branch(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, child_id uuid, supply_type character varying, status character varying, notes text, threshold_note character varying, last_reported_by_user_id uuid, last_reported_by_name character varying, parent_reminder_sent boolean, last_reminder_at timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.child_id,
           s.supply_type, s.status, s.notes, s.threshold_note, s.last_reported_by_user_id,
           s.last_reported_by_name, s.parent_reminder_sent, s.last_reminder_at, s.created_at, s.updated_at,
           CONCAT(c.first_name, ' ', c.last_name) AS child_name,
           cl.name AS classroom_name
    FROM child_care_supplies s
    JOIN children c ON s.child_id = c.id
    LEFT JOIN classrooms cl ON s.classroom_id = cl.id
    WHERE s.tenant_id = p_tenant_id 
      AND s.branch_id = p_branch_id 
      AND s.status IN ('Low', 'Depleted')
    ORDER BY s.updated_at DESC;
$$;


--
-- Name: fn_get_marketplace_order_by_transaction_ref(uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_marketplace_order_by_transaction_ref(p_organization_id uuid, p_transaction_reference character varying) RETURNS SETOF public.marketplace_orders
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM marketplace_orders
    WHERE organization_id = p_organization_id AND transaction_reference = p_transaction_reference;
$$;


--
-- Name: fn_get_marketplace_orders(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_marketplace_orders(p_organization_id uuid DEFAULT NULL::uuid) RETURNS SETOF public.marketplace_orders
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM marketplace_orders
    WHERE (p_organization_id IS NULL OR organization_id = p_organization_id)
    ORDER BY purchased_at DESC;
$$;


--
-- Name: fn_get_material_loan_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_loan_by_id(p_loan_id uuid) RETURNS TABLE(id uuid, order_id uuid, order_item_id uuid, material_product_id uuid, organization_id uuid, classroom_id uuid, quantity_issued integer, issued_at timestamp with time zone, due_date timestamp with time zone, actual_return_date timestamp with time zone, return_status character varying, deposit_amount numeric, deposit_status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying, order_number character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.id = p_loan_id;
$$;


--
-- Name: fn_get_material_loans_by_order_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_loans_by_order_id(p_order_id uuid) RETURNS TABLE(id uuid, order_id uuid, order_item_id uuid, material_product_id uuid, organization_id uuid, classroom_id uuid, quantity_issued integer, issued_at timestamp with time zone, due_date timestamp with time zone, actual_return_date timestamp with time zone, return_status character varying, deposit_amount numeric, deposit_status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying, order_number character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.order_id = p_order_id;
$$;


--
-- Name: fn_get_material_order_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_order_by_id(p_order_id uuid) RETURNS TABLE(id uuid, order_number character varying, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, planned_experience_id uuid, requesting_user_id uuid, requesting_user_email character varying, order_type character varying, workflow_status character varying, payment_status character varying, fulfilment_status character varying, total_purchase_amount numeric, total_rental_fee numeric, total_deposit numeric, shipping_fee numeric, tax_amount numeric, total_amount numeric, currency character varying, quotation_notes text, quotation_valid_until timestamp with time zone, rejection_reason text, cancellation_reason text, delivery_address text, delivery_contact_name character varying, delivery_contact_phone character varying, required_delivery_date timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, organization_name character varying, branch_name character varying, classroom_name character varying, planned_experience_title character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.id = p_order_id;
$$;


--
-- Name: fn_get_material_order_by_number(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_order_by_number(p_order_number character varying) RETURNS TABLE(id uuid, order_number character varying, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, planned_experience_id uuid, requesting_user_id uuid, requesting_user_email character varying, order_type character varying, workflow_status character varying, payment_status character varying, fulfilment_status character varying, total_purchase_amount numeric, total_rental_fee numeric, total_deposit numeric, shipping_fee numeric, tax_amount numeric, total_amount numeric, currency character varying, quotation_notes text, quotation_valid_until timestamp with time zone, rejection_reason text, cancellation_reason text, delivery_address text, delivery_contact_name character varying, delivery_contact_phone character varying, required_delivery_date timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, organization_name character varying, branch_name character varying, classroom_name character varying, planned_experience_title character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.order_number = p_order_number;
$$;


--
-- Name: fn_get_material_order_history(uuid, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_order_history(p_order_id uuid, p_include_internal_notes boolean DEFAULT false) RETURNS TABLE(id uuid, order_id uuid, event_type character varying, previous_status character varying, new_status character varying, actor_user_id uuid, actor_email character varying, actor_role character varying, description text, notes text, is_internal_note boolean, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, order_id, event_type, previous_status, new_status,
        actor_user_id, actor_email, actor_role, description, notes,
        is_internal_note, created_at
    FROM material_order_history
    WHERE order_id = p_order_id
      AND (p_include_internal_notes OR is_internal_note = FALSE)
    ORDER BY created_at ASC;
$$;


--
-- Name: fn_get_material_order_items(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_order_items(p_order_id uuid) RETURNS TABLE(id uuid, order_id uuid, material_product_id uuid, offering_id uuid, fulfilment_mode character varying, quantity integer, unit_price numeric, rental_fee numeric, deposit numeric, line_total numeric, loan_duration_days integer, return_due_date timestamp with time zone, status character varying, created_at timestamp with time zone, product_name character varying, product_sku character varying, image_url character varying, category character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        i.id, i.order_id, i.material_product_id, i.offering_id, i.fulfilment_mode,
        i.quantity, i.unit_price, i.rental_fee, i.deposit, i.line_total,
        i.loan_duration_days, i.return_due_date, i.status, i.created_at,
        p.name AS product_name, p.sku AS product_sku, p.image_url, p.category
    FROM material_order_items i
    JOIN material_products p ON i.material_product_id = p.id
    WHERE i.order_id = p_order_id;
$$;


--
-- Name: fn_get_material_orders_for_organization(uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_orders_for_organization(p_organization_id uuid, p_status_filter character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, order_number character varying, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, planned_experience_id uuid, requesting_user_id uuid, requesting_user_email character varying, order_type character varying, workflow_status character varying, payment_status character varying, fulfilment_status character varying, total_purchase_amount numeric, total_rental_fee numeric, total_deposit numeric, shipping_fee numeric, tax_amount numeric, total_amount numeric, currency character varying, quotation_notes text, quotation_valid_until timestamp with time zone, rejection_reason text, cancellation_reason text, delivery_address text, delivery_contact_name character varying, delivery_contact_phone character varying, required_delivery_date timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, organization_name character varying, branch_name character varying, classroom_name character varying, planned_experience_title character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.organization_id = p_organization_id
      AND (p_status_filter IS NULL OR p_status_filter = 'All' OR o.workflow_status = p_status_filter)
    ORDER BY o.created_at DESC;
$$;


--
-- Name: fn_get_material_product_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_product_by_id(p_product_id uuid) RETURNS TABLE(id uuid, sku character varying, name character varying, description text, category character varying, image_url character varying, material_type character varying, safety_instructions text, age_min_months integer, age_max_months integer, usage_restrictions text, cleaning_instructions text, return_policy text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone, off_id uuid, off_prod_id uuid, allow_purchase boolean, purchase_price numeric, allow_borrow boolean, rental_fee numeric, refundable_deposit numeric, default_loan_duration_days integer, max_loan_duration_days integer, currency character varying, off_is_active boolean, bal_id uuid, total_stock integer, available_stock integer, reserved_stock integer, dispatched_stock integer, on_loan_stock integer, under_inspection_stock integer, under_cleaning_stock integer, damaged_stock integer, missing_stock integer, retired_stock integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE p.id = p_product_id;
$$;


--
-- Name: fn_get_material_product_by_sku(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_product_by_sku(p_sku character varying) RETURNS TABLE(id uuid, sku character varying, name character varying, description text, category character varying, image_url character varying, material_type character varying, safety_instructions text, age_min_months integer, age_max_months integer, usage_restrictions text, cleaning_instructions text, return_policy text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone, off_id uuid, off_prod_id uuid, allow_purchase boolean, purchase_price numeric, allow_borrow boolean, rental_fee numeric, refundable_deposit numeric, default_loan_duration_days integer, max_loan_duration_days integer, currency character varying, off_is_active boolean, bal_id uuid, total_stock integer, available_stock integer, reserved_stock integer, dispatched_stock integer, on_loan_stock integer, under_inspection_stock integer, under_cleaning_stock integer, damaged_stock integer, missing_stock integer, retired_stock integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE p.sku = p_sku;
$$;


--
-- Name: fn_get_material_products(character varying, character varying, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_products(p_category character varying DEFAULT NULL::character varying, p_material_type character varying DEFAULT NULL::character varying, p_active_only boolean DEFAULT true) RETURNS TABLE(id uuid, sku character varying, name character varying, description text, category character varying, image_url character varying, material_type character varying, safety_instructions text, age_min_months integer, age_max_months integer, usage_restrictions text, cleaning_instructions text, return_policy text, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone, off_id uuid, off_prod_id uuid, allow_purchase boolean, purchase_price numeric, allow_borrow boolean, rental_fee numeric, refundable_deposit numeric, default_loan_duration_days integer, max_loan_duration_days integer, currency character varying, off_is_active boolean, bal_id uuid, total_stock integer, available_stock integer, reserved_stock integer, dispatched_stock integer, on_loan_stock integer, under_inspection_stock integer, under_cleaning_stock integer, damaged_stock integer, missing_stock integer, retired_stock integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE (NOT p_active_only OR p.is_active = TRUE)
      AND (p_category IS NULL OR p_category = 'All' OR p.category = p_category)
      AND (p_material_type IS NULL OR p_material_type = 'All' OR p.material_type = p_material_type)
    ORDER BY p.name ASC;
$$;


--
-- Name: fn_get_material_recurring_schedules(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_recurring_schedules(p_organization_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, schedule_type character varying, frequency character varying, material_product_id uuid, offering_id uuid, quantity integer, loan_duration_days integer, delivery_address text, next_run_date timestamp with time zone, last_run_date timestamp with time zone, status character varying, notes text, created_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.schedule_type,
        s.frequency, s.material_product_id, s.offering_id, s.quantity, s.loan_duration_days,
        s.delivery_address, s.next_run_date, s.last_run_date, s.status, s.notes,
        s.created_by_user_id, s.created_at, s.updated_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_recurring_schedules s
    JOIN material_products p ON s.material_product_id = p.id
    WHERE s.organization_id = p_organization_id
    ORDER BY s.next_run_date ASC;
$$;


--
-- Name: fn_get_material_return_inspection_by_loan_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_return_inspection_by_loan_id(p_loan_id uuid) RETURNS TABLE(id uuid, loan_id uuid, order_id uuid, material_product_id uuid, returned_quantity integer, inspected_at timestamp with time zone, inspected_by_user_id uuid, inspected_by_email character varying, condition character varying, quantity_approved_for_stock integer, quantity_requiring_cleaning integer, quantity_damaged integer, quantity_missing integer, inspection_notes text, deposit_disposition character varying, deposit_deduction_amount numeric, resolution_status character varying, created_at timestamp with time zone, product_name character varying, product_sku character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        i.id, i.loan_id, i.order_id, i.material_product_id, i.returned_quantity,
        i.inspected_at, i.inspected_by_user_id, i.inspected_by_email, i.condition,
        i.quantity_approved_for_stock, i.quantity_requiring_cleaning,
        i.quantity_damaged, i.quantity_missing, i.inspection_notes,
        i.deposit_disposition, i.deposit_deduction_amount, i.resolution_status, i.created_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_return_inspections i
    JOIN material_products p ON i.material_product_id = p.id
    WHERE i.loan_id = p_loan_id
    ORDER BY i.created_at DESC 
    LIMIT 1;
$$;


--
-- Name: fn_get_material_shipment_by_order_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_material_shipment_by_order_id(p_order_id uuid) RETURNS TABLE(id uuid, order_id uuid, shipment_number character varying, carrier_name character varying, tracking_reference character varying, dispatch_date timestamp with time zone, estimated_delivery_date timestamp with time zone, actual_delivery_date timestamp with time zone, delivery_status character varying, proof_of_delivery_notes text, recorded_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, order_id, shipment_number, carrier_name, tracking_reference,
        dispatch_date, estimated_delivery_date, actual_delivery_date,
        delivery_status, proof_of_delivery_notes, recorded_by_user_id,
        created_at, updated_at
    FROM material_shipments 
    WHERE order_id = p_order_id 
    ORDER BY created_at DESC 
    LIMIT 1;
$$;


--
-- Name: fn_get_milestones_for_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_milestones_for_child(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, child_id uuid, classroom_id uuid, teacher_id uuid, title character varying, description text, milestone_date timestamp with time zone, development_area_id uuid, evidence_summary text, journey_entry_id uuid, parent_visible boolean, status character varying, created_at timestamp with time zone, created_by uuid, updated_at timestamp with time zone, updated_by uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cm.id, cm.tenant_id, cm.child_id, cm.classroom_id, cm.teacher_id,
           cm.title, cm.description, cm.milestone_date, cm.development_area_id,
           cm.evidence_summary, cm.journey_entry_id, cm.parent_visible, cm.status,
           cm.created_at, cm.created_by, cm.updated_at, cm.updated_by
    FROM child_milestones cm
    WHERE cm.tenant_id = p_tenant_id AND cm.child_id = p_child_id
    ORDER BY cm.milestone_date DESC;
END;
$$;


--
-- Name: fn_get_organization_billing_settings(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_billing_settings(p_organization_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, mode character varying, frequency character varying, effective_date timestamp with time zone, next_billing_date timestamp with time zone, auto_charge_enabled boolean, notes text, updated_by character varying, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    FROM organization_billing_settings
    WHERE organization_id = p_organization_id;
$$;


--
-- Name: fn_get_organization_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id AND o.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_organization_by_id_global(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_by_id_global(p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at, o.updated_at
    FROM organizations o
    WHERE o.id = p_id;
END;
$$;


--
-- Name: fn_get_organization_entitlement(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_entitlement(p_organization_id uuid, p_platform_experience_id uuid) RETURNS SETOF public.organization_entitlements
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM organization_entitlements
    WHERE organization_id = p_organization_id AND platform_experience_id = p_platform_experience_id
      AND status = 'Active' AND (expires_at IS NULL OR expires_at > NOW());
$$;


--
-- Name: fn_get_organization_entitlements(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_entitlements(p_organization_id uuid) RETURNS SETOF public.organization_entitlements
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM organization_entitlements
    WHERE organization_id = p_organization_id AND status = 'Active'
      AND (expires_at IS NULL OR expires_at > NOW())
    ORDER BY granted_at DESC;
$$;


--
-- Name: fn_get_organization_role_names(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organization_role_names(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid) RETURNS TABLE(role_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT r.name::VARCHAR
    FROM user_organization_roles uor
    JOIN roles r ON r.id = uor.role_id AND r.tenant_id = uor.tenant_id
    JOIN organization_memberships m ON m.user_id = uor.user_id AND m.organization_id = uor.organization_id AND m.tenant_id = uor.tenant_id
    WHERE uor.tenant_id = p_tenant_id AND uor.organization_id = p_organization_id AND uor.user_id = p_user_id
      AND m.status = 'Active';
END;
$$;


--
-- Name: fn_get_organizations_by_user_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organizations_by_user_id(p_user_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_get_organizations_due_for_automatic_billing(timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_organizations_due_for_automatic_billing(p_as_of_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, mode character varying, frequency character varying, effective_date timestamp with time zone, next_billing_date timestamp with time zone, auto_charge_enabled boolean, notes text, updated_by character varying, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    FROM organization_billing_settings
    WHERE mode = 'Automatic'
      AND effective_date <= p_as_of_date
      AND (next_billing_date IS NULL OR next_billing_date <= p_as_of_date);
$$;


--
-- Name: events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    event_type_id uuid,
    event_date date NOT NULL,
    start_time time without time zone,
    end_time time without time zone,
    location character varying(255),
    organizer_id uuid,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    is_organization_wide boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    scope character varying(30) DEFAULT 'Classroom'::character varying NOT NULL,
    branch_id uuid,
    photo_urls text[] DEFAULT '{}'::text[]
);


--
-- Name: fn_get_parent_approved_events_for_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_approved_events_for_child(p_tenant_id uuid, p_child_id uuid) RETURNS SETOF public.events
    LANGUAGE sql STABLE
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


--
-- Name: child_journey_entries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_entries (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    enrollment_id uuid,
    classroom_id uuid,
    teacher_id uuid NOT NULL,
    experience_id uuid,
    experience_schedule_id uuid,
    little_moment_id uuid,
    milestone_id uuid,
    journey_type character varying(100) NOT NULL,
    title character varying(255),
    observation text NOT NULL,
    child_voice text,
    teacher_reflection text,
    observation_date timestamp with time zone NOT NULL,
    participation_type character varying(100),
    is_milestone boolean DEFAULT false NOT NULL,
    is_parent_visible boolean DEFAULT false NOT NULL,
    parent_visibility_status character varying(50),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by uuid,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by uuid,
    version integer DEFAULT 1 NOT NULL,
    experience_session_id uuid,
    experience_participation_id uuid,
    event_id uuid
);


--
-- Name: fn_get_parent_approved_journey_entries(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_approved_journey_entries(p_child_id uuid) RETURNS SETOF public.child_journey_entries
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM child_journey_entries
    WHERE child_id = p_child_id 
      AND is_parent_visible = true
    ORDER BY observation_date DESC;
$$;


--
-- Name: child_milestones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_milestones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    classroom_id uuid,
    teacher_id uuid NOT NULL,
    title character varying(255) NOT NULL,
    description text,
    milestone_date timestamp with time zone NOT NULL,
    development_area_id uuid,
    evidence_summary text,
    journey_entry_id uuid,
    parent_visible boolean DEFAULT false NOT NULL,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by uuid,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by uuid
);


--
-- Name: fn_get_parent_approved_milestones(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_approved_milestones(p_child_id uuid) RETURNS SETOF public.child_milestones
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM child_milestones
    WHERE child_id = p_child_id 
      AND parent_visible = true
    ORDER BY milestone_date DESC;
$$;


--
-- Name: fn_get_parent_authorized_children(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_authorized_children(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(child_id uuid, tenant_id uuid, first_name character varying, last_name character varying, date_of_birth date, classroom_id uuid, classroom_name character varying, branch_id uuid, branch_name character varying, organization_id uuid, organization_name character varying, photo_url text, relationship character varying, is_primary boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c.id AS child_id,
        c.tenant_id,
        c.first_name,
        c.last_name,
        c.date_of_birth,
        c.classroom_id,
        cl.name::VARCHAR AS classroom_name,
        cl.branch_id,
        b.name::VARCHAR AS branch_name,
        COALESCE(b.organization_id, o.id, (SELECT o2.id FROM organizations o2 WHERE o2.tenant_id = c.tenant_id ORDER BY o2.created_at LIMIT 1)) AS organization_id,
        o.name::VARCHAR AS organization_name,
        c.photo_url,
        fr.relationship::VARCHAR,
        fr.is_primary
    FROM family_relationships fr
    JOIN children c ON fr.child_id = c.id
    LEFT JOIN classrooms cl ON c.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    LEFT JOIN organizations o ON b.organization_id = o.id
    WHERE fr.user_id = p_user_id 
      AND fr.status = 'Verified' AND fr.revoked_at IS NULL
      AND c.is_active = true
    ORDER BY c.first_name, c.last_name;
END;
$$;


--
-- Name: fn_get_parent_child_photo_consent(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_child_photo_consent(p_child_id uuid) RETURNS character varying
    LANGUAGE sql STABLE
    AS $$
    SELECT d.status
    FROM child_consent_documents d
    LEFT JOIN consent_templates t ON d.consent_template_id = t.id
    WHERE d.child_id = p_child_id
      AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
    ORDER BY d.created_at DESC
    LIMIT 1;
$$;


--
-- Name: fn_get_parent_communication_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_communication_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, recipient_person_id uuid, recipient_user_id uuid, recipient_email character varying, recipient_name character varying, recipient_relationship character varying, communication_type character varying, reporting_date_start date, reporting_date_end date, title character varying, content_draft text, content_final text, family_conversation_starter text, evidence_summary_json jsonb, consent_verified boolean, consent_notes character varying, status character varying, teacher_id uuid, teacher_name character varying, approved_by_user_id uuid, approved_at timestamp with time zone, sent_at timestamp with time zone, delivery_channel character varying, delivery_status character varying, delivery_error text, idempotency_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.id = p_id AND pc.tenant_id = p_tenant_id
    LIMIT 1;
$$;


--
-- Name: fn_get_parent_communications_by_child_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_communications_by_child_id(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, recipient_person_id uuid, recipient_user_id uuid, recipient_email character varying, recipient_name character varying, recipient_relationship character varying, communication_type character varying, reporting_date_start date, reporting_date_end date, title character varying, content_draft text, content_final text, family_conversation_starter text, evidence_summary_json jsonb, consent_verified boolean, consent_notes character varying, status character varying, teacher_id uuid, teacher_name character varying, approved_by_user_id uuid, approved_at timestamp with time zone, sent_at timestamp with time zone, delivery_channel character varying, delivery_status character varying, delivery_error text, idempotency_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.child_id = p_child_id AND pc.tenant_id = p_tenant_id
    ORDER BY pc.created_at DESC;
$$;


--
-- Name: fn_get_parent_journey_evidence(uuid[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_parent_journey_evidence(p_entry_ids uuid[]) RETURNS TABLE(journey_entry_id uuid, text_content text)
    LANGUAGE sql STABLE
    AS $$
    SELECT journey_entry_id, text_content
    FROM child_journey_evidence
    WHERE journey_entry_id = ANY(p_entry_ids)
      AND evidence_type = 'Photo'
      AND (visibility = 'Parent' OR visibility = 'Public')
      AND text_content IS NOT NULL AND text_content != ''
    ORDER BY created_at ASC;
$$;


--
-- Name: fn_get_participations_for_session(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_participations_for_session(p_session_id uuid) RETURNS TABLE(id uuid, experience_session_id uuid, child_id uuid, participation_status character varying, participation_notes text, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ep.id, ep.experience_session_id, 
           ep.child_id, ep.participation_status, 
           ep.participation_notes, 
           ep.created_at, ep.updated_at
    FROM experience_participations ep
    WHERE ep.experience_session_id = p_session_id;
END;
$$;


--
-- Name: fn_get_payment_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payment_by_id(p_payment_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE id = p_payment_id;
$$;


--
-- Name: fn_get_payment_by_provider_payment_id(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payment_by_provider_payment_id(p_provider_payment_id character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE provider_payment_id = p_provider_payment_id AND is_reversed = FALSE
    LIMIT 1;
$$;


--
-- Name: fn_get_payment_by_receipt_number(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payment_by_receipt_number(p_receipt_number character varying) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE receipt_number = p_receipt_number AND is_reversed = FALSE
    LIMIT 1;
$$;


--
-- Name: fn_get_payments_by_invoice_ids(uuid[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payments_by_invoice_ids(p_invoice_ids uuid[]) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE invoice_id = ANY(p_invoice_ids)
    ORDER BY payment_date ASC;
$$;


--
-- Name: fn_get_payments_for_invoice(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payments_for_invoice(p_invoice_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE invoice_id = p_invoice_id
    ORDER BY payment_date ASC;
$$;


--
-- Name: fn_get_payments_for_organization(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_payments_for_organization(p_organization_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, invoice_id uuid, invoice_number character varying, payment_reference character varying, amount numeric, currency character varying, method character varying, is_verified boolean, verified_by character varying, verified_at timestamp with time zone, status character varying, transaction_id character varying, notes text, provider_order_id character varying, provider_payment_id character varying, provider_signature character varying, receipt_number character varying, recorded_by_user_id uuid, recorded_by_email character varying, is_reversed boolean, reversed_at timestamp with time zone, reversed_by character varying, reversal_reason text, reversal_reference character varying, audit_metadata text, payment_date timestamp with time zone, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE organization_id = p_organization_id
    ORDER BY payment_date DESC, created_at DESC;
$$;


--
-- Name: fn_get_pending_teacher_handovers_by_branch(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_pending_teacher_handovers_by_branch(p_tenant_id uuid, p_branch_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, handover_date date, shift_type character varying, outgoing_teacher_user_id uuid, outgoing_teacher_name character varying, incoming_teacher_user_id uuid, incoming_teacher_name character varying, summary_notes text, child_specific_notes text, outstanding_care_tasks text, pending_followups text, status character varying, is_critical boolean, acknowledged_at timestamp with time zone, acknowledged_by_user_id uuid, acknowledged_by_name character varying, acknowledgement_notes text, created_at timestamp with time zone, updated_at timestamp with time zone, classroom_name character varying, branch_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.tenant_id = p_tenant_id 
      AND th.branch_id = p_branch_id 
      AND th.status = 'PendingAcknowledgement'
    ORDER BY th.is_critical DESC, th.created_at DESC;
$$;


--
-- Name: fn_get_person_by_user_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_person_by_user_id(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, user_id uuid, first_name character varying, last_name character varying, relationship character varying, email character varying, mobile_phone character varying, alternate_phone character varying, preferred_contact_method character varying, preferred_language character varying, has_portal_access boolean, receives_daily_updates boolean, receives_photos boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.tenant_id, p.user_id, p.first_name, p.last_name, p.relationship,
           p.email, p.mobile_phone, p.alternate_phone, p.preferred_contact_method,
           p.preferred_language, p.has_portal_access, p.receives_daily_updates,
           p.receives_photos, p.created_at, p.updated_at
    FROM persons p
    WHERE p.user_id = p_user_id
    ORDER BY (p.tenant_id = p_tenant_id) DESC
    LIMIT 1;
END;
$$;


--
-- Name: fn_get_platform_all_branches(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_all_branches() RETURNS TABLE(id uuid, organization_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, organization_name character varying, effective_location text, classrooms_count integer, children_count integer)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        b.id AS id,
        b.organization_id AS organization_id,
        b.name AS name,
        b.location AS location,
        b.is_active AS is_active,
        b.created_at AS created_at,
        o.name AS organization_name,
        COALESCE(b.location, o.location, 'Location unspecified') AS effective_location,
        COALESCE((SELECT COUNT(*) FROM classrooms c WHERE c.branch_id = b.id), 0)::int AS classrooms_count,
        COALESCE((SELECT COUNT(*) FROM children ch JOIN classrooms c ON ch.classroom_id = c.id WHERE c.branch_id = b.id), 0)::int AS children_count
    FROM branches b
    JOIN organizations o ON b.organization_id = o.id
    ORDER BY o.name, b.name;
$$;


--
-- Name: fn_get_platform_experience_by_id(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_experience_by_id(p_id uuid) RETURNS SETOF public.platform_experiences
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM platform_experiences
    WHERE id = p_id;
$$;


--
-- Name: fn_get_platform_experience_versions(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_experience_versions(p_platform_experience_id uuid) RETURNS SETOF public.platform_experience_versions
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM platform_experience_versions
    WHERE platform_experience_id = p_platform_experience_id
    ORDER BY version_number DESC;
$$;


--
-- Name: fn_get_platform_loan_queue(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_loan_queue(p_status_filter character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, order_id uuid, order_item_id uuid, material_product_id uuid, organization_id uuid, classroom_id uuid, quantity_issued integer, issued_at timestamp with time zone, due_date timestamp with time zone, actual_return_date timestamp with time zone, return_status character varying, deposit_amount numeric, deposit_status character varying, notes text, created_at timestamp with time zone, updated_at timestamp with time zone, product_name character varying, product_sku character varying, order_number character varying, classroom_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE (p_status_filter IS NULL OR p_status_filter = 'All' OR l.return_status = p_status_filter)
    ORDER BY l.due_date ASC;
$$;


--
-- Name: fn_get_platform_marketplace_catalog(character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_marketplace_catalog(p_category character varying DEFAULT NULL::character varying, p_pricing_model character varying DEFAULT NULL::character varying, p_search_query character varying DEFAULT NULL::character varying) RETURNS SETOF public.platform_experiences
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM platform_experiences
    WHERE publication_status = 'Published'
      AND (p_category IS NULL OR category = p_category)
      AND (p_pricing_model IS NULL OR pricing_model = p_pricing_model)
      AND (p_search_query IS NULL OR title ILIKE ('%' || trim(p_search_query) || '%') OR description ILIKE ('%' || trim(p_search_query) || '%'))
    ORDER BY created_at DESC;
$$;


--
-- Name: fn_get_platform_order_queue(character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_order_queue(p_status_filter character varying DEFAULT NULL::character varying) RETURNS TABLE(id uuid, order_number character varying, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, planned_experience_id uuid, requesting_user_id uuid, requesting_user_email character varying, order_type character varying, workflow_status character varying, payment_status character varying, fulfilment_status character varying, total_purchase_amount numeric, total_rental_fee numeric, total_deposit numeric, shipping_fee numeric, tax_amount numeric, total_amount numeric, currency character varying, quotation_notes text, quotation_valid_until timestamp with time zone, rejection_reason text, cancellation_reason text, delivery_address text, delivery_contact_name character varying, delivery_contact_phone character varying, required_delivery_date timestamp with time zone, created_at timestamp with time zone, updated_at timestamp with time zone, organization_name character varying, branch_name character varying, classroom_name character varying, planned_experience_title character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE (p_status_filter IS NULL OR p_status_filter = 'All' OR o.workflow_status = p_status_filter)
    ORDER BY o.created_at DESC;
$$;


--
-- Name: fn_get_platform_organization_admins(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_organization_admins() RETURNS TABLE(user_id uuid, admin_name text, admin_email character varying, organization_id uuid, organization_name character varying, role_name character varying, account_status text)
    LANGUAGE sql STABLE
    AS $$
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
    ORDER BY o.id, COALESCE(u.id, '00000000-0000-0000-0000-000000000000'::uuid), 
             CASE WHEN r.name = 'OrganizationOwner' THEN 1 WHEN r.name = 'OrganizationAdmin' THEN 2 ELSE 3 END;
$$;


--
-- Name: fn_get_platform_organizations_overview(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_platform_organizations_overview() RETURNS TABLE(id uuid, tenant_id uuid, name character varying, location character varying, is_active boolean, created_at timestamp with time zone, branch_count integer, admin_email character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT o.id AS id, o.tenant_id AS tenant_id, o.name AS name,
           o.location AS location, o.is_active AS is_active, o.created_at AS created_at,
           COALESCE((SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id), 0)::int AS branch_count,
           (SELECT u.email FROM organization_memberships om JOIN users u ON om.user_id = u.id WHERE om.organization_id = o.id AND om.status = 'Active' ORDER BY om.created_at LIMIT 1) AS admin_email
    FROM organizations o
    ORDER BY o.name;
$$;


--
-- Name: fn_get_portfolio_child_profile(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_child_profile(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, first_name character varying, last_name character varying, photo_url text, date_of_birth date, classroom_name character varying, branch_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.first_name::VARCHAR, c.last_name::VARCHAR,
           c.photo_url, c.date_of_birth,
           cr.name::VARCHAR AS classroom_name, b.name::VARCHAR AS branch_name
    FROM children c
    LEFT JOIN classrooms cr ON c.classroom_id = cr.id
    LEFT JOIN branches b ON cr.branch_id = b.id
    WHERE c.tenant_id = p_tenant_id AND c.id = p_child_id;
END;
$$;


--
-- Name: fn_get_portfolio_consent_choices(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_consent_choices(p_tenant_id uuid, p_consent_document_id uuid) RETURNS TABLE(choice character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cc.choice::VARCHAR
    FROM consent_choices cc
    WHERE cc.tenant_id = p_tenant_id AND cc.consent_document_id = p_consent_document_id
      AND (cc.section_key ILIKE '%photo%' OR cc.section_key ILIKE '%media%');
END;
$$;


--
-- Name: fn_get_portfolio_development_areas(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_development_areas(p_tenant_id uuid) RETURNS TABLE(id uuid, name character varying, icon character varying, display_order integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT da.id, da.name::VARCHAR, da.icon::VARCHAR, da.display_order
    FROM development_areas da
    WHERE (da.tenant_id IS NULL OR da.tenant_id = p_tenant_id) AND da.is_active = TRUE
    ORDER BY da.display_order;
END;
$$;


--
-- Name: fn_get_portfolio_entry_development_areas(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_entry_development_areas(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(journey_entry_id uuid, development_area_id uuid, name character varying, icon character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT d.journey_entry_id, d.development_area_id,
           a.name::VARCHAR, a.icon::VARCHAR
    FROM child_journey_developments d
    JOIN development_areas a ON d.development_area_id = a.id
    WHERE d.journey_entry_id IN (
        SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    );
END;
$$;


--
-- Name: fn_get_portfolio_entry_evidence(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_entry_evidence(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(journey_entry_id uuid, text_content text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT cje_ev.journey_entry_id, cje_ev.text_content
    FROM child_journey_evidence cje_ev
    WHERE cje_ev.journey_entry_id IN (
        SELECT cje.id FROM child_journey_entries cje WHERE cje.tenant_id = p_tenant_id AND cje.child_id = p_child_id
    )
    ORDER BY cje_ev.created_at DESC;
END;
$$;


--
-- Name: fn_get_portfolio_experience_participations(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_experience_participations(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, session_date timestamp with time zone, experience_title character varying, notes text, teacher_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT p.id,
           (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS session_date,
           e.title::VARCHAR AS experience_title,
           COALESCE(p.participation_notes, s.actual_notes) AS notes,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM experience_participations p
    JOIN experience_sessions s ON p.experience_session_id = s.id
    JOIN experiences e ON s.experience_id = e.id
    LEFT JOIN teachers t ON s.primary_teacher_id = t.id
    WHERE s.tenant_id = p_tenant_id AND p.child_id = p_child_id AND p.participation_status = 'Participated'
    ORDER BY s.session_date DESC;
END;
$$;


--
-- Name: fn_get_portfolio_journey_entries(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_journey_entries(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, observation_date timestamp with time zone, journey_type character varying, title character varying, observation text, child_voice text, teacher_reflection text, is_milestone boolean, is_parent_visible boolean, parent_visibility_status character varying, teacher_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT je.id, je.observation_date, je.journey_type::VARCHAR,
           je.title::VARCHAR, je.observation, je.child_voice,
           je.teacher_reflection, je.is_milestone,
           je.is_parent_visible, je.parent_visibility_status::VARCHAR,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM child_journey_entries je
    LEFT JOIN teachers t ON je.teacher_id = t.id
    WHERE je.tenant_id = p_tenant_id AND je.child_id = p_child_id
    ORDER BY je.observation_date DESC;
END;
$$;


--
-- Name: fn_get_portfolio_milestones(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_portfolio_milestones(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(id uuid, milestone_date timestamp with time zone, title character varying, description text, evidence_summary text, parent_visible boolean, status character varying, development_area_id uuid, teacher_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT m.id, m.milestone_date, m.title::VARCHAR,
           m.description, m.evidence_summary,
           m.parent_visible, m.status::VARCHAR,
           m.development_area_id,
           COALESCE(t.first_name || ' ' || t.last_name, 'Teacher')::VARCHAR AS teacher_name
    FROM child_milestones m
    LEFT JOIN teachers t ON m.teacher_id = t.id
    WHERE m.tenant_id = p_tenant_id AND m.child_id = p_child_id
    ORDER BY m.milestone_date DESC;
END;
$$;


--
-- Name: fn_get_present_child_ids_by_classroom(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_present_child_ids_by_classroom(p_tenant_id uuid, p_classroom_id uuid, p_date date) RETURNS TABLE(child_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ca.child_id
    FROM child_attendance ca
    WHERE ca.tenant_id = p_tenant_id AND ca.classroom_id = p_classroom_id AND ca.attendance_date = p_date AND ca.status IN ('Present', 'Late');
END;
$$;


--
-- Name: fn_get_present_child_ids_by_classroom(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_present_child_ids_by_classroom(p_tenant_id uuid, p_classroom_id uuid, p_date timestamp without time zone) RETURNS TABLE(child_id uuid)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_present_child_ids_by_classroom(p_tenant_id, p_classroom_id, p_date::DATE);
$$;


--
-- Name: fn_get_present_child_ids_by_classroom(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_present_child_ids_by_classroom(p_tenant_id uuid, p_classroom_id uuid, p_date timestamp with time zone) RETURNS TABLE(child_id uuid)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_present_child_ids_by_classroom(p_tenant_id, p_classroom_id, p_date::DATE);
$$;


--
-- Name: fn_get_product_ledger(uuid, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_product_ledger(p_product_id uuid, p_limit integer DEFAULT 50) RETURNS TABLE(id uuid, material_product_id uuid, location_id uuid, transaction_type character varying, quantity_delta integer, available_delta integer, reserved_delta integer, on_loan_delta integer, reference_type character varying, reference_id uuid, notes text, performed_by_user_id uuid, performed_by_email character varying, created_at timestamp with time zone, product_name character varying, product_sku character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        l.id, l.material_product_id, l.location_id, l.transaction_type,
        l.quantity_delta, l.available_delta, l.reserved_delta, l.on_loan_delta,
        l.reference_type, l.reference_id, l.notes,
        l.performed_by_user_id, l.performed_by_email, l.created_at,
        p.name AS product_name, p.sku AS product_sku
    FROM inventory_ledger l
    JOIN material_products p ON l.material_product_id = p.id
    WHERE l.material_product_id = p_product_id
    ORDER BY l.created_at DESC
    LIMIT p_limit;
$$;


--
-- Name: organization_intelligence_insights; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_intelligence_insights (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    scope_type character varying(50) NOT NULL,
    reporting_period character varying(50) NOT NULL,
    category character varying(50) NOT NULL,
    title character varying(255) NOT NULL,
    observation text NOT NULL,
    supporting_metrics_json jsonb,
    possible_explanation text,
    suggested_next_step text,
    evidence_strength character varying(50) DEFAULT 'Strong'::character varying NOT NULL,
    data_limitations text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    source_type character varying(50) DEFAULT 'DeterministicRule'::character varying NOT NULL,
    is_ai_generated boolean DEFAULT false NOT NULL
);


--
-- Name: fn_get_recent_organization_intelligence_insights(uuid, uuid, uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_recent_organization_intelligence_insights(p_tenant_id uuid, p_organization_id uuid, p_branch_id uuid, p_classroom_id uuid, p_reporting_period character varying) RETURNS SETOF public.organization_intelligence_insights
    LANGUAGE sql STABLE
    AS $$
    SELECT *
    FROM organization_intelligence_insights
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_organization_id
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR classroom_id = p_classroom_id)
      AND reporting_period = p_reporting_period
    ORDER BY created_at DESC
    LIMIT 10;
$$;


--
-- Name: fn_get_recent_parent_communications(uuid, uuid, uuid, uuid, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_recent_parent_communications(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid, p_limit integer DEFAULT 50) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, child_id uuid, recipient_person_id uuid, recipient_user_id uuid, recipient_email character varying, recipient_name character varying, recipient_relationship character varying, communication_type character varying, reporting_date_start date, reporting_date_end date, title character varying, content_draft text, content_final text, family_conversation_starter text, evidence_summary_json jsonb, consent_verified boolean, consent_notes character varying, status character varying, teacher_id uuid, teacher_name character varying, approved_by_user_id uuid, approved_at timestamp with time zone, sent_at timestamp with time zone, delivery_channel character varying, delivery_status character varying, delivery_error text, idempotency_key character varying, created_at timestamp with time zone, updated_at timestamp with time zone, child_name text, classroom_name character varying, branch_name character varying, branch_id uuid)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name,
        b.name AS branch_name,
        cl.branch_id
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    WHERE pc.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR pc.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR pc.classroom_id = p_classroom_id)
    ORDER BY pc.created_at DESC
    LIMIT p_limit;
$$;


--
-- Name: fn_get_recipients_for_child(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_recipients_for_child(p_tenant_id uuid, p_child_id uuid) RETURNS TABLE(person_id uuid, user_id uuid, full_name text, relationship character varying, email character varying, mobile_phone character varying, can_receive_information boolean, receives_daily_updates boolean, has_portal_access boolean, photo_consent_granted boolean)
    LANGUAGE sql STABLE
    AS $$
    SELECT DISTINCT
        p.id AS person_id,
        p.user_id,
        TRIM(CONCAT(p.first_name, ' ', p.last_name)) AS full_name,
        COALESCE(cg.relationship, fr.relationship, p.relationship, 'Guardian')::VARCHAR AS relationship,
        COALESCE(p.email, u.email)::VARCHAR AS email,
        p.mobile_phone,
        COALESCE(cg.can_receive_information, TRUE) AS can_receive_information,
        COALESCE(p.receives_daily_updates, TRUE) AS receives_daily_updates,
        COALESCE(p.has_portal_access, FALSE) AS has_portal_access,
        TRUE AS photo_consent_granted
    FROM persons p
    LEFT JOIN child_guardians cg ON cg.person_id = p.id AND cg.child_id = p_child_id
    LEFT JOIN family_relationships fr ON fr.user_id = p.user_id AND fr.child_id = p_child_id
    LEFT JOIN users u ON p.user_id = u.id
    WHERE p.tenant_id = p_tenant_id
      AND (cg.child_id = p_child_id OR fr.child_id = p_child_id)
      AND (fr.id IS NULL OR (fr.status = 'Verified' AND fr.revoked_at IS NULL));
$$;


--
-- Name: fn_get_statutory_ratio_rules(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_statutory_ratio_rules(p_tenant_id uuid) RETURNS TABLE(age_group character varying, max_ratio integer, description character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT csr.age_group::VARCHAR, csr.max_children_per_educator::INT, csr.description::VARCHAR
    FROM classroom_statutory_ratios csr
    WHERE (csr.tenant_id IS NULL OR csr.tenant_id = p_tenant_id) AND csr.is_active = TRUE;
END;
$$;


--
-- Name: fn_get_teacher_attendance_by_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, teacher_id uuid, attendance_date date, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, notes text, recorded_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT ta.id, ta.tenant_id, ta.branch_id, ta.teacher_id, ta.attendance_date, ta.status,
           ta.clock_in_time, ta.clock_out_time, ta.notes, ta.recorded_by_user_id, ta.created_at, ta.updated_at
    FROM teacher_attendance ta
    WHERE ta.tenant_id = p_tenant_id AND ta.branch_id = p_branch_id AND ta.attendance_date = p_date;
END;
$$;


--
-- Name: fn_get_teacher_attendance_by_date(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp without time zone) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, teacher_id uuid, attendance_date date, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, notes text, recorded_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_teacher_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_teacher_attendance_by_date(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_by_date(p_tenant_id uuid, p_branch_id uuid, p_date timestamp with time zone) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, teacher_id uuid, attendance_date date, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, notes text, recorded_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_teacher_attendance_by_date(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_teacher_attendance_summary(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_summary(p_tenant_id uuid, p_branch_id uuid, p_date date) RETURNS TABLE(teacher_id uuid, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, teacher_name character varying, classroom_id uuid)
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


--
-- Name: fn_get_teacher_attendance_summary(uuid, uuid, timestamp without time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_summary(p_tenant_id uuid, p_branch_id uuid, p_date timestamp without time zone) RETURNS TABLE(teacher_id uuid, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, teacher_name character varying, classroom_id uuid)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_teacher_attendance_summary(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_teacher_attendance_summary(uuid, uuid, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_attendance_summary(p_tenant_id uuid, p_branch_id uuid, p_date timestamp with time zone) RETURNS TABLE(teacher_id uuid, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, teacher_name character varying, classroom_id uuid)
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM public.fn_get_teacher_attendance_summary(p_tenant_id, p_branch_id, p_date::DATE);
$$;


--
-- Name: fn_get_teacher_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_by_id(p_tenant_id uuid, p_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, person_id uuid, user_id uuid, employee_number character varying, first_name character varying, middle_name character varying, last_name character varying, preferred_name character varying, display_name character varying, profile_photo_url text, date_of_birth date, gender character varying, preferred_language character varying, other_languages text, pronouns character varying, bio text, status character varying, draft_data jsonb, profile_completion_percentage integer, created_at timestamp with time zone, updated_at timestamp with time zone)
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


--
-- Name: fn_get_teacher_handover_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_handover_by_id(p_tenant_id uuid, p_handover_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, handover_date date, shift_type character varying, outgoing_teacher_user_id uuid, outgoing_teacher_name character varying, incoming_teacher_user_id uuid, incoming_teacher_name character varying, summary_notes text, child_specific_notes text, outstanding_care_tasks text, pending_followups text, status character varying, is_critical boolean, acknowledged_at timestamp with time zone, acknowledged_by_user_id uuid, acknowledged_by_name character varying, acknowledgement_notes text, created_at timestamp with time zone, updated_at timestamp with time zone, classroom_name character varying, branch_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.id = p_handover_id AND th.tenant_id = p_tenant_id;
$$;


--
-- Name: fn_get_teacher_handovers_by_classroom_and_date(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_handovers_by_classroom_and_date(p_tenant_id uuid, p_classroom_id uuid, p_date date) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, branch_id uuid, classroom_id uuid, handover_date date, shift_type character varying, outgoing_teacher_user_id uuid, outgoing_teacher_name character varying, incoming_teacher_user_id uuid, incoming_teacher_name character varying, summary_notes text, child_specific_notes text, outstanding_care_tasks text, pending_followups text, status character varying, is_critical boolean, acknowledged_at timestamp with time zone, acknowledged_by_user_id uuid, acknowledged_by_name character varying, acknowledgement_notes text, created_at timestamp with time zone, updated_at timestamp with time zone, classroom_name character varying, branch_name character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT th.id, th.tenant_id, th.organization_id, th.branch_id, th.classroom_id,
           th.handover_date, th.shift_type, th.outgoing_teacher_user_id, th.outgoing_teacher_name,
           th.incoming_teacher_user_id, th.incoming_teacher_name, th.summary_notes,
           th.child_specific_notes, th.outstanding_care_tasks, th.pending_followups,
           th.status, th.is_critical, th.acknowledged_at, th.acknowledged_by_user_id,
           th.acknowledged_by_name, th.acknowledgement_notes, th.created_at, th.updated_at,
           cl.name AS classroom_name,
           b.name AS branch_name
    FROM teacher_handovers th
    JOIN classrooms cl ON th.classroom_id = cl.id
    JOIN branches b ON th.branch_id = b.id
    WHERE th.tenant_id = p_tenant_id 
      AND th.classroom_id = p_classroom_id 
      AND th.handover_date = p_date
    ORDER BY th.created_at DESC;
$$;


--
-- Name: fn_get_teacher_memories(uuid, uuid, timestamp with time zone, timestamp with time zone, integer, integer); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_memories(p_tenant_id uuid, p_teacher_id uuid, p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_limit integer DEFAULT 100, p_offset integer DEFAULT 0) RETURNS TABLE(id uuid, tenant_id uuid, event_date timestamp with time zone, event_type character varying, title character varying, description text, source_id uuid, source_type character varying, child_id uuid, classroom_id uuid, teacher_id uuid, experience_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM (
        SELECT 
            cs.id, cs.tenant_id, cs.created_at AS event_date,
            'Observation'::VARCHAR AS event_type,
            'Story'::VARCHAR AS title,
            cs.content AS description,
            cs.id AS source_id,
            'child_stories'::VARCHAR AS source_type,
            cs.child_id,
            NULL::UUID AS classroom_id,
            cs.teacher_id,
            es.experience_id
        FROM child_stories cs
        LEFT JOIN experience_schedules es ON cs.experience_schedule_id = es.id
        WHERE cs.tenant_id = p_tenant_id AND cs.teacher_id = p_teacher_id

        UNION ALL
        
        SELECT 
            je.id, je.tenant_id, je.observation_date AS event_date,
            'Observation'::VARCHAR AS event_type,
            COALESCE(je.title, 'Journey Entry')::VARCHAR AS title,
            je.observation AS description,
            je.id AS source_id,
            'child_journey_entries'::VARCHAR AS source_type,
            je.child_id,
            je.classroom_id,
            je.teacher_id,
            je.experience_id
        FROM child_journey_entries je
        WHERE je.tenant_id = p_tenant_id AND je.teacher_id = p_teacher_id

        UNION ALL

        SELECT 
            s.id, s.tenant_id,
            (s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::TIME))::TIMESTAMPTZ AS event_date,
            'Session'::VARCHAR AS event_type,
            'Experience Session'::VARCHAR AS title,
            s.actual_notes AS description,
            s.id AS source_id,
            'experience_sessions'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            s.primary_teacher_id AS teacher_id,
            s.experience_id
        FROM experience_sessions s
        WHERE s.tenant_id = p_tenant_id AND s.primary_teacher_id = p_teacher_id

        UNION ALL

        SELECT 
            f.id, f.tenant_id,
            f.created_at AS event_date,
            'Feedback'::VARCHAR AS event_type,
            'Teacher Feedback'::VARCHAR AS title,
            f.notes AS description,
            f.id AS source_id,
            'experience_feedbacks'::VARCHAR AS source_type,
            NULL::UUID AS child_id,
            s.classroom_id,
            f.teacher_id,
            s.experience_id
        FROM experience_feedback f
        JOIN experience_schedules s ON f.experience_schedule_id = s.id
        WHERE f.tenant_id = p_tenant_id AND f.teacher_id = p_teacher_id
    ) AS combined
    WHERE (p_start_date IS NULL OR combined.event_date >= p_start_date)
      AND (p_end_date IS NULL OR combined.event_date <= p_end_date)
    ORDER BY combined.event_date DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


--
-- Name: fn_get_teacher_user_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_teacher_user_id(p_tenant_id uuid, p_teacher_id uuid) RETURNS uuid
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


--
-- Name: fn_get_tenant_encrypted_ai_api_key(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_tenant_encrypted_ai_api_key(p_tenant_id uuid) RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_key VARCHAR;
BEGIN
    SELECT encrypted_ai_api_key INTO v_key FROM tenants WHERE id = p_tenant_id;
    RETURN v_key;
END;
$$;


--
-- Name: fn_get_today_child_attendance(uuid, uuid, date); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_today_child_attendance(p_tenant_id uuid, p_child_id uuid, p_date date) RETURNS SETOF public.child_attendance
    LANGUAGE sql STABLE
    AS $$
    SELECT * FROM child_attendance
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id AND attendance_date = p_date
    LIMIT 1;
$$;


--
-- Name: fn_get_unread_parent_communications_count(uuid, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_unread_parent_communications_count(p_tenant_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_branch_id uuid DEFAULT NULL::uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS bigint
    LANGUAGE sql STABLE
    AS $$
    SELECT COUNT(*)
    FROM parent_communications pc
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.tenant_id = p_tenant_id
      AND pc.teacher_id = '00000000-0000-0000-0000-000000000000'
      AND pc.delivery_status != 'Read'
      AND (p_organization_id IS NULL OR pc.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR pc.classroom_id = p_classroom_id);
$$;


--
-- Name: fn_get_user_auth_claims(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_user_auth_claims(p_user_id uuid, p_active_org_id uuid DEFAULT NULL::uuid) RETURNS TABLE(claim_type character varying, claim_value character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Roles
    RETURN QUERY
    SELECT 'role'::VARCHAR, r.name::VARCHAR
    FROM user_roles ur 
    JOIN roles r ON ur.role_id = r.id 
    WHERE ur.user_id = p_user_id
    UNION
    SELECT 'role'::VARCHAR, r.name::VARCHAR
    FROM user_organization_roles uor 
    JOIN roles r ON uor.role_id = r.id 
    JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
    WHERE uor.user_id = p_user_id AND (uor.organization_id = p_active_org_id OR p_active_org_id IS NULL)
    UNION
    SELECT 'role'::VARCHAR, 'DaycareTeacher'::VARCHAR
    FROM teachers t
    WHERE t.user_id = p_user_id AND t.status != 'Inactive';

    -- Permissions
    RETURN QUERY
    SELECT 'permission'::VARCHAR, p.name::VARCHAR
    FROM user_roles ur 
    JOIN role_permissions rp ON ur.role_id = rp.role_id 
    JOIN permissions p ON rp.permission_id = p.id 
    WHERE ur.user_id = p_user_id
    UNION
    SELECT 'permission'::VARCHAR, p.name::VARCHAR
    FROM user_organization_roles uor 
    JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
    JOIN role_permissions rp ON uor.role_id = rp.role_id 
    JOIN permissions p ON rp.permission_id = p.id 
    WHERE uor.user_id = p_user_id AND (uor.organization_id = p_active_org_id OR p_active_org_id IS NULL)
    UNION
    SELECT 'permission'::VARCHAR, p.name::VARCHAR
    FROM teachers t
    JOIN roles r ON r.name IN ('DaycareTeacher', 'ClassroomTeacher', 'Teacher')
    JOIN role_permissions rp ON r.id = rp.role_id
    JOIN permissions p ON rp.permission_id = p.id
    WHERE t.user_id = p_user_id AND t.status != 'Inactive';
END;
$$;


--
-- Name: fn_get_user_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_user_by_id(p_tenant_id uuid, p_user_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.id = p_user_id AND u.tenant_id = p_tenant_id;
END;
$$;


--
-- Name: fn_get_user_profile_security_details(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_user_profile_security_details(p_user_id uuid) RETURNS TABLE(user_id uuid, display_name character varying, first_name character varying, last_name character varying, assigned_role_names character varying[], is_classroom_teacher boolean, is_parent boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        u.id as user_id,
        u.display_name::VARCHAR,
        u.first_name::VARCHAR,
        u.last_name::VARCHAR,
        COALESCE((
            SELECT array_agg(DISTINCT r.name::VARCHAR)
            FROM (
                SELECT ur.role_id FROM user_roles ur JOIN roles r ON ur.role_id = r.id WHERE ur.user_id = p_user_id AND r.scope_type = 'Platform' AND r.tenant_id IS NULL
                UNION
                SELECT uor.role_id FROM user_organization_roles uor JOIN roles r ON uor.role_id = r.id WHERE uor.user_id = p_user_id
            ) comb
            JOIN roles r ON r.id = comb.role_id
            WHERE r.name IS NOT NULL
        ), ARRAY[]::VARCHAR[]) as assigned_role_names,
        EXISTS(SELECT 1 FROM classroom_teachers ct WHERE ct.user_id = p_user_id) as is_classroom_teacher,
        EXISTS(SELECT 1 FROM family_relationships fr WHERE fr.user_id = p_user_id) as is_parent
    FROM users u
    WHERE u.id = p_user_id;
END;
$$;


--
-- Name: fn_get_users_by_role(uuid, character varying, character varying[]); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_users_by_role(p_tenant_id uuid, p_role_name character varying, p_names character varying[]) RETURNS TABLE(id uuid, tenant_id uuid, email character varying, first_name character varying, last_name character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.email, u.first_name, u.last_name, u.display_name, u.is_active, u.created_at, u.updated_at
    FROM users u
    WHERE u.tenant_id = p_tenant_id
      AND (EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON ur.role_id = r.id
                   WHERE ur.user_id = u.id AND r.name = p_role_name)
           OR EXISTS (SELECT 1 FROM user_organization_roles uor JOIN roles r ON r.id = uor.role_id
                      WHERE uor.user_id = u.id AND uor.tenant_id = p_tenant_id AND r.name = ANY(p_names)))
    ORDER BY u.first_name, u.last_name;
END;
$$;


--
-- Name: fn_get_users_hierarchy(uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_users_hierarchy(p_tenant_id uuid) RETURNS TABLE(user_id uuid, email character varying, display_name character varying, is_active boolean, created_at timestamp with time zone, organization_name character varying, classroom_name character varying, associated_children_count integer, assigned_roles character varying[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    WITH user_role_agg AS (
        SELECT comb.user_id, array_agg(DISTINCT r.name::VARCHAR) as roles, MAX(o.name) as org_name
        FROM (
            SELECT ur.user_id, ur.role_id, NULL::UUID as org_id FROM user_roles ur
            UNION ALL
            SELECT uor.user_id, uor.role_id, uor.organization_id as org_id FROM user_organization_roles uor
        ) comb
        JOIN roles r ON comb.role_id = r.id
        LEFT JOIN organizations o ON o.id = comb.org_id
        GROUP BY comb.user_id
    ),
    classroom_agg AS (
        SELECT ct.user_id, MAX(c.name) as c_name
        FROM classroom_teachers ct
        JOIN classrooms c ON c.id = ct.classroom_id
        GROUP BY ct.user_id
    ),
    parent_agg AS (
        SELECT fr.user_id, COUNT(fr.child_id)::int as child_count
        FROM family_relationships fr
        GROUP BY fr.user_id
    )
    SELECT
        u.id as user_id,
        u.email::VARCHAR,
        COALESCE(NULLIF(TRIM(u.display_name), ''), NULLIF(TRIM(u.first_name || ' ' || u.last_name), ''), u.email)::VARCHAR as display_name,
        u.is_active,
        u.created_at,
        ura.org_name::VARCHAR as organization_name,
        cla.c_name::VARCHAR as classroom_name,
        COALESCE(pa.child_count, 0) as associated_children_count,
        COALESCE(ura.roles, ARRAY[]::VARCHAR[]) as assigned_roles
    FROM users u
    LEFT JOIN user_role_agg ura ON ura.user_id = u.id
    LEFT JOIN classroom_agg cla ON cla.user_id = u.id
    LEFT JOIN parent_agg pa ON pa.user_id = u.id
    WHERE p_tenant_id = '00000000-0000-0000-0000-000000000000'::uuid OR u.tenant_id = p_tenant_id
    ORDER BY u.first_name, u.last_name;
END;
$$;


--
-- Name: fn_get_weekly_planning_draft_by_id(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_weekly_planning_draft_by_id(p_tenant_id uuid, p_draft_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, organization_id uuid, classroom_id uuid, requested_by_user_id uuid, week_start_date date, week_end_date date, focus_areas character varying, indoor_outdoor_preference character varying, materials_notes text, status character varying, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, requested_by_user_id,
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference,
        materials_notes, status, created_at, updated_at
    FROM weekly_planning_draft_requests
    WHERE tenant_id = p_tenant_id AND id = p_draft_id;
$$;


--
-- Name: fn_get_weekly_planning_draft_items(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_get_weekly_planning_draft_items(p_tenant_id uuid, p_request_id uuid) RETURNS TABLE(id uuid, request_id uuid, tenant_id uuid, scheduled_date date, day_of_week character varying, time_of_day character varying, experience_id uuid, experience_title character varying, learning_goal text, group_format character varying, estimated_duration_minutes integer, required_materials text, rationale text, validation_status character varying, validation_notes text, resulting_schedule_id uuid, created_at timestamp with time zone)
    LANGUAGE sql STABLE
    AS $$
    SELECT 
        id, request_id, tenant_id, scheduled_date, day_of_week, time_of_day,
        experience_id, experience_title, learning_goal, group_format,
        estimated_duration_minutes, required_materials, rationale,
        validation_status, validation_notes, resulting_schedule_id, created_at
    FROM weekly_planning_draft_items
    WHERE tenant_id = p_tenant_id AND request_id = p_request_id
    ORDER BY scheduled_date ASC, time_of_day ASC;
$$;


--
-- Name: fn_has_active_membership(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_has_active_membership(p_tenant_id uuid, p_organization_id uuid, p_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 FROM organization_memberships m
        JOIN users u ON u.id = m.user_id
        WHERE m.tenant_id = p_tenant_id AND m.organization_id = p_organization_id AND m.user_id = p_user_id
          AND m.status = 'Active' AND u.is_active = true
    );
END;
$$;


--
-- Name: fn_has_parent_child_access(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_has_parent_child_access(p_tenant_id uuid, p_user_id uuid, p_child_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 
        FROM family_relationships fr
        JOIN children c ON fr.child_id = c.id
        WHERE fr.user_id = p_user_id 
          AND fr.child_id = p_child_id
          AND fr.status = 'Verified' AND fr.revoked_at IS NULL
          AND c.is_active = true
    );
END;
$$;


--
-- Name: fn_has_verified_guardian_relationship(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_has_verified_guardian_relationship(p_tenant_id uuid, p_user_id uuid, p_child_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN EXISTS(
        SELECT 1 FROM family_relationships fr
        JOIN children c ON c.id = fr.child_id AND c.tenant_id = fr.tenant_id
        JOIN users u ON u.id = fr.user_id AND u.tenant_id = fr.tenant_id
        WHERE fr.tenant_id = p_tenant_id AND fr.user_id = p_user_id AND fr.child_id = p_child_id
          AND fr.status = 'Verified' AND fr.revoked_at IS NULL
          AND c.is_active = true AND u.is_active = true
    );
END;
$$;


--
-- Name: fn_is_teacher_assigned_to_classroom(uuid, uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_is_teacher_assigned_to_classroom(p_tenant_id uuid, p_classroom_id uuid, p_teacher_id uuid, p_user_id uuid DEFAULT NULL::uuid) RETURNS boolean
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


--
-- Name: fn_record_teacher_attendance(uuid, uuid, uuid, uuid, date, character varying, time without time zone, time without time zone, text, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_record_teacher_attendance(p_id uuid, p_tenant_id uuid, p_branch_id uuid, p_teacher_id uuid, p_attendance_date date, p_status character varying, p_clock_in_time time without time zone, p_clock_out_time time without time zone, p_notes text, p_recorded_by_user_id uuid) RETURNS TABLE(id uuid, tenant_id uuid, branch_id uuid, teacher_id uuid, attendance_date date, status character varying, clock_in_time time without time zone, clock_out_time time without time zone, notes text, recorded_by_user_id uuid, created_at timestamp with time zone, updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_actual_teacher_id UUID;
    v_attendance_id UUID;
    v_user RECORD;
BEGIN
    SELECT t.id INTO v_actual_teacher_id FROM teachers t WHERE t.id = p_teacher_id OR t.user_id = p_teacher_id LIMIT 1;
    
    IF v_actual_teacher_id IS NULL THEN
        SELECT u.id, u.tenant_id, u.first_name, u.last_name, u.display_name INTO v_user FROM users u WHERE u.id = p_teacher_id;
        IF v_user.id IS NOT NULL THEN
            v_actual_teacher_id := gen_random_uuid();
            INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
            VALUES (
                v_actual_teacher_id,
                COALESCE(NULLIF(p_tenant_id, '00000000-0000-0000-0000-000000000000'::uuid), v_user.tenant_id),
                v_user.id,
                COALESCE(v_user.first_name, 'Teacher'),
                COALESCE(v_user.last_name, ''),
                COALESCE(v_user.display_name, v_user.first_name || ' ' || v_user.last_name),
                'Active', NOW(), NOW()
            ) ON CONFLICT (id) DO NOTHING;
        ELSE
            v_actual_teacher_id := p_teacher_id;
        END IF;
    END IF;

    v_attendance_id := p_id;
    IF v_attendance_id IS NULL OR v_attendance_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        SELECT ta.id INTO v_attendance_id
        FROM teacher_attendance ta
        WHERE ta.branch_id = p_branch_id AND ta.teacher_id = v_actual_teacher_id AND ta.attendance_date = p_attendance_date;
        
        IF v_attendance_id IS NULL THEN
            v_attendance_id := gen_random_uuid();
        END IF;
    END IF;

    INSERT INTO teacher_attendance (
        id, tenant_id, branch_id, teacher_id, attendance_date, status, clock_in_time, clock_out_time, notes, recorded_by_user_id, created_at, updated_at
    )
    VALUES (
        v_attendance_id, p_tenant_id, p_branch_id, v_actual_teacher_id, p_attendance_date, p_status, p_clock_in_time, p_clock_out_time, p_notes, p_recorded_by_user_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (id) DO UPDATE SET 
        status = EXCLUDED.status, clock_in_time = EXCLUDED.clock_in_time, clock_out_time = EXCLUDED.clock_out_time, notes = EXCLUDED.notes, updated_at = CURRENT_TIMESTAMP;

    RETURN QUERY
    SELECT ta.id, ta.tenant_id, ta.branch_id, ta.teacher_id, ta.attendance_date, ta.status,
           ta.clock_in_time, ta.clock_out_time, ta.notes, ta.recorded_by_user_id, ta.created_at, ta.updated_at
    FROM teacher_attendance ta
    WHERE ta.id = v_attendance_id;
END;
$$;


--
-- Name: fn_resolve_teacher_id(uuid, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_resolve_teacher_id(p_tenant_id uuid, p_teacher_or_user_id uuid, p_classroom_id uuid DEFAULT NULL::uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_found_id UUID;
    v_new_teacher_id UUID;
BEGIN
    -- 1. Direct teacher ID
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.id = p_teacher_or_user_id AND t.tenant_id = p_tenant_id
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 2. User ID linked to teacher
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.user_id = p_teacher_or_user_id AND t.tenant_id = p_tenant_id
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 3. Classroom teacher
    IF p_classroom_id IS NOT NULL THEN
        SELECT t.id INTO v_found_id
        FROM classroom_teachers ct
        JOIN teachers t ON ct.user_id = t.user_id AND ct.tenant_id = t.tenant_id
        WHERE ct.classroom_id = p_classroom_id AND ct.tenant_id = p_tenant_id
        ORDER BY ct.is_primary DESC
        LIMIT 1;

        IF v_found_id IS NOT NULL THEN
            RETURN v_found_id;
        END IF;
    END IF;

    -- 4. Any active teacher
    SELECT t.id INTO v_found_id
    FROM teachers t
    WHERE t.tenant_id = p_tenant_id AND t.status != 'Inactive'
    LIMIT 1;

    IF v_found_id IS NOT NULL THEN
        RETURN v_found_id;
    END IF;

    -- 5. Auto-create if user exists
    v_new_teacher_id := gen_random_uuid();
    INSERT INTO teachers (
        id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at
    )
    SELECT v_new_teacher_id, p_tenant_id, u.id, COALESCE(u.first_name, 'Teacher'), COALESCE(u.last_name, 'Staff'), COALESCE(u.display_name, 'Lead Educator'), 'Active', NOW(), NOW()
    FROM users u
    WHERE u.id = p_teacher_or_user_id AND u.tenant_id = p_tenant_id
    ON CONFLICT (id) DO NOTHING;

    IF FOUND THEN
        RETURN v_new_teacher_id;
    END IF;

    RETURN p_teacher_or_user_id;
END;
$$;


--
-- Name: experience_harvest_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_harvest_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    source_experience_id uuid NOT NULL,
    source_version_number integer DEFAULT 1 NOT NULL,
    license_id uuid,
    submitted_by_user_id uuid NOT NULL,
    submitted_at timestamp with time zone DEFAULT now() NOT NULL,
    status character varying(50) DEFAULT 'Submitted'::character varying NOT NULL,
    review_notes text,
    reviewed_by_user_id uuid,
    reviewed_at timestamp with time zone,
    safety_suitability_verified boolean DEFAULT false NOT NULL,
    learning_objectives_verified boolean DEFAULT false NOT NULL,
    instructions_verified boolean DEFAULT false NOT NULL,
    materials_verified boolean DEFAULT false NOT NULL,
    media_rights_verified boolean DEFAULT false NOT NULL,
    commercial_rights_verified boolean DEFAULT false NOT NULL
);


--
-- Name: fn_submit_experience_harvest_request(uuid, uuid, uuid, uuid, integer, uuid, uuid, timestamp with time zone, character varying, text, uuid, timestamp with time zone, boolean, boolean, boolean, boolean, boolean, boolean); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_submit_experience_harvest_request(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_source_experience_id uuid, p_source_version_number integer, p_license_id uuid, p_submitted_by_user_id uuid, p_submitted_at timestamp with time zone, p_status character varying, p_review_notes text, p_reviewed_by_user_id uuid, p_reviewed_at timestamp with time zone, p_safety_suitability_verified boolean, p_learning_objectives_verified boolean, p_instructions_verified boolean, p_materials_verified boolean, p_media_rights_verified boolean, p_commercial_rights_verified boolean) RETURNS SETOF public.experience_harvest_requests
    LANGUAGE sql
    AS $$
    INSERT INTO experience_harvest_requests (
        id, tenant_id, organization_id, source_experience_id, source_version_number, license_id,
        submitted_by_user_id, submitted_at, status, review_notes, reviewed_by_user_id, reviewed_at,
        safety_suitability_verified, learning_objectives_verified, instructions_verified,
        materials_verified, media_rights_verified, commercial_rights_verified
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_source_experience_id, p_source_version_number, p_license_id,
        p_submitted_by_user_id, p_submitted_at, p_status, p_review_notes, p_reviewed_by_user_id, p_reviewed_at,
        p_safety_suitability_verified, p_learning_objectives_verified, p_instructions_verified,
        p_materials_verified, p_media_rights_verified, p_commercial_rights_verified
    )
    RETURNING *;
$$;


--
-- Name: fn_sync_external_login(character varying, character varying, character varying, character varying, character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_sync_external_login(p_provider character varying, p_provider_key character varying, p_email character varying, p_first_name character varying DEFAULT NULL::character varying, p_last_name character varying DEFAULT NULL::character varying, p_display_name character varying DEFAULT NULL::character varying, p_required_portal character varying DEFAULT 'Daycare'::character varying) RETURNS TABLE(out_status character varying, out_user_id uuid, out_tenant_id uuid, out_effective_tenant_id uuid, out_active_org_id uuid, out_email character varying, out_display_name character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_user_id UUID;
    v_tenant_id UUID;
    v_effective_tenant_id UUID;
    v_is_active BOOLEAN;
    v_first_name VARCHAR;
    v_last_name VARCHAR;
    v_display_name VARCHAR;
    v_user_email VARCHAR;
    v_has_access BOOLEAN := FALSE;
    v_active_org_id UUID;
    v_default_org_id UUID;
BEGIN
    -- 1. Check if external login is already linked
    SELECT uel.user_id INTO v_user_id
    FROM user_external_logins uel
    WHERE uel.provider = p_provider AND uel.provider_key = p_provider_key
    LIMIT 1;

    -- 2. If not linked, check by normalized email
    IF v_user_id IS NULL THEN
        SELECT u.id, u.tenant_id, u.is_active, u.first_name, u.last_name, u.display_name, u.email
        INTO v_user_id, v_tenant_id, v_is_active, v_first_name, v_last_name, v_display_name, v_user_email
        FROM users u
        WHERE u.normalized_email = UPPER(p_email)
        LIMIT 1;

        IF v_user_id IS NULL THEN
            RETURN QUERY SELECT 'not_registered'::VARCHAR, NULL::UUID, NULL::UUID, NULL::UUID, NULL::UUID, p_email, NULL::VARCHAR;
            RETURN;
        END IF;
    ELSE
        SELECT u.id, u.tenant_id, u.is_active, u.first_name, u.last_name, u.display_name, u.email
        INTO v_user_id, v_tenant_id, v_is_active, v_first_name, v_last_name, v_display_name, v_user_email
        FROM users u
        WHERE u.id = v_user_id;
    END IF;

    -- 3. Check portal access
    IF p_required_portal = 'Daycare' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id AND r.name IN ('PlatformAdmin', 'SuperAdmin')
            UNION
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            WHERE uor.user_id = v_user_id AND r.name IN ('DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher', 'AssistantTeacher', 'OrganizationAdmin', 'OrganizationOwner')
            UNION
            SELECT 1 FROM teachers t
            WHERE t.user_id = v_user_id
        ) INTO v_has_access;
    ELSIF p_required_portal = 'Admin' THEN
        SELECT EXISTS (
            SELECT 1 FROM user_roles ur
            JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = v_user_id AND r.name IN ('PlatformAdmin', 'SuperAdmin')
            UNION
            SELECT 1 FROM user_organization_roles uor
            JOIN roles r ON uor.role_id = r.id
            WHERE uor.user_id = v_user_id AND r.name IN ('OrganizationAdmin', 'OrganizationOwner')
        ) INTO v_has_access;
    ELSE
        v_has_access := TRUE;
    END IF;

    IF NOT v_has_access THEN
        RETURN QUERY SELECT 'insufficient_permissions'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, NULL::VARCHAR;
        RETURN;
    END IF;

    -- 4. Check if account is active
    IF v_is_active IS NOT TRUE THEN
        RETURN QUERY SELECT 'account_disabled'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, NULL::VARCHAR;
        RETURN;
    END IF;

    -- 5. Link Google external login and confirm email
    INSERT INTO user_external_logins (provider, provider_key, user_id, created_at)
    VALUES (p_provider, p_provider_key, v_user_id, NOW())
    ON CONFLICT (provider, provider_key) DO NOTHING;

    UPDATE users SET email_confirmed = TRUE, updated_at = NOW() WHERE id = v_user_id;

    -- 6. Resolve display name
    IF v_display_name IS NULL OR TRIM(v_display_name) = '' THEN
        v_display_name := NULLIF(TRIM(COALESCE(v_first_name, '') || ' ' || COALESCE(v_last_name, '')), '');
    END IF;
    IF v_display_name IS NULL OR TRIM(v_display_name) = '' THEN
        v_display_name := COALESCE(p_display_name, p_first_name, v_user_email, 'Teacher');
    END IF;

    -- 7. Ensure teacher profile exists if Daycare portal
    IF p_required_portal = 'Daycare' THEN
        INSERT INTO teachers (id, tenant_id, user_id, first_name, last_name, display_name, status, created_at, updated_at)
        SELECT gen_random_uuid(), v_tenant_id, v_user_id, COALESCE(p_first_name, v_first_name), COALESCE(p_last_name, v_last_name), v_display_name, 'Active', NOW(), NOW()
        WHERE NOT EXISTS (SELECT 1 FROM teachers tch WHERE tch.user_id = v_user_id);
    END IF;

    -- 8. Ensure organization membership
    IF NOT EXISTS (SELECT 1 FROM organization_memberships om WHERE om.user_id = v_user_id AND om.status = 'Active') THEN
        SELECT id INTO v_default_org_id FROM organizations WHERE tenant_id = v_tenant_id LIMIT 1;
        IF v_default_org_id IS NOT NULL THEN
            INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status, created_at, updated_at)
            VALUES (v_user_id, v_default_org_id, v_tenant_id, 'Active', NOW(), NOW())
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 9. Resolve active organization ID
    SELECT uor.organization_id INTO v_active_org_id
    FROM user_organization_roles uor
    JOIN roles r ON uor.role_id = r.id
    JOIN organization_memberships om ON om.user_id = uor.user_id AND om.organization_id = uor.organization_id AND om.status = 'Active'
    WHERE uor.user_id = v_user_id 
      AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher', 'LeadTeacher')
    LIMIT 1;

    IF v_active_org_id IS NULL THEN
        SELECT om.organization_id INTO v_active_org_id
        FROM organization_memberships om
        WHERE om.user_id = v_user_id AND om.status = 'Active'
        LIMIT 1;
    END IF;

    IF v_active_org_id IS NULL THEN
        RETURN QUERY SELECT 'no_organizations'::VARCHAR, v_user_id, v_tenant_id, NULL::UUID, NULL::UUID, v_user_email, v_display_name;
        RETURN;
    END IF;

    -- 10. Resolve effective tenant ID
    SELECT o.tenant_id INTO v_effective_tenant_id
    FROM organizations o
    WHERE o.id = v_active_org_id;

    IF v_effective_tenant_id IS NULL THEN
        v_effective_tenant_id := v_tenant_id;
    END IF;

    RETURN QUERY SELECT 
        'success'::VARCHAR,
        v_user_id,
        v_tenant_id,
        v_effective_tenant_id,
        v_active_org_id,
        v_user_email,
        v_display_name;
END;
$$;


--
-- Name: fn_validate_and_set_organization(uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.fn_validate_and_set_organization(p_user_id uuid, p_organization_id uuid DEFAULT NULL::uuid) RETURNS TABLE(is_valid boolean, tenant_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_tenant_id UUID;
BEGIN
    IF p_organization_id IS NOT NULL THEN
        SELECT o.tenant_id INTO v_tenant_id
        FROM organization_memberships om
        JOIN organizations o ON om.organization_id = o.id
        WHERE om.user_id = p_user_id
          AND om.organization_id = p_organization_id
          AND om.status = 'Active'
        LIMIT 1;

        IF v_tenant_id IS NOT NULL THEN
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    ELSE
        IF EXISTS (SELECT 1 FROM family_relationships WHERE user_id = p_user_id) THEN
            SELECT u.tenant_id INTO v_tenant_id FROM users u WHERE u.id = p_user_id;
            RETURN QUERY SELECT TRUE, v_tenant_id;
        ELSE
            RETURN QUERY SELECT FALSE, NULL::UUID;
        END IF;
    END IF;
END;
$$;


--
-- Name: sp_acknowledge_teacher_handover(uuid, uuid, uuid, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_acknowledge_teacher_handover(IN p_tenant_id uuid, IN p_handover_id uuid, IN p_user_id uuid, IN p_user_name character varying, IN p_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE teacher_handovers
    SET status = 'Acknowledged',
        acknowledged_at = NOW(),
        acknowledged_by_user_id = p_user_id,
        acknowledged_by_name = p_user_name,
        acknowledgement_notes = p_notes,
        updated_at = NOW()
    WHERE id = p_handover_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_add_authorized_pickup(uuid, uuid, uuid, uuid, character varying, character varying, text, timestamp with time zone, timestamp with time zone, boolean, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_authorized_pickup(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_person_id uuid, IN p_relationship character varying, IN p_pickup_code character varying, IN p_photo_url text, IN p_valid_from timestamp with time zone, IN p_valid_to timestamp with time zone, IN p_is_active boolean, IN p_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO authorized_pickups (
        id, tenant_id, child_id, person_id, relationship, pickup_code, photo_url, valid_from, valid_to, is_active, notes
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_person_id, p_relationship, p_pickup_code, p_photo_url, p_valid_from, p_valid_to, p_is_active, p_notes
    );
END;
$$;


--
-- Name: sp_add_child_care_event(uuid, uuid, uuid, uuid, uuid, uuid, character varying, timestamp with time zone, character varying, character varying, character varying, character varying, character varying, numeric, character varying, character varying, character varying, character varying, text, boolean, uuid, character varying, text, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_child_care_event(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_event_type character varying, IN p_event_time timestamp with time zone, IN p_diaper_condition character varying, IN p_skin_observation character varying, IN p_meal_type character varying, IN p_food_offered character varying, IN p_amount_consumed character varying, IN p_fluid_oz numeric, IN p_rest_status character varying, IN p_toileting_activity character varying, IN p_hygiene_type character varying, IN p_comfort_mood character varying, IN p_notes text, IN p_parent_visible boolean, IN p_recorded_by_user_id uuid, IN p_recorded_by_name character varying, IN p_correction_reason text, IN p_is_corrected boolean, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_care_events (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        event_type, event_time, diaper_condition, skin_observation,
        meal_type, food_offered, amount_consumed, fluid_oz,
        rest_status, toileting_activity, hygiene_type, comfort_mood,
        notes, parent_visible, recorded_by_user_id, recorded_by_name,
        correction_reason, is_corrected, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_event_type, p_event_time, p_diaper_condition, p_skin_observation,
        p_meal_type, p_food_offered, p_amount_consumed, p_fluid_oz,
        p_rest_status, p_toileting_activity, p_hygiene_type, p_comfort_mood,
        p_notes, p_parent_visible, p_recorded_by_user_id, p_recorded_by_name,
        p_correction_reason, p_is_corrected, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_add_consent_audit_event(uuid, uuid, uuid, uuid, character varying, uuid, character varying, character varying, timestamp with time zone, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_consent_audit_event(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_consent_document_id uuid, IN p_actor_type character varying, IN p_actor_user_id uuid, IN p_actor_name character varying, IN p_event_type character varying, IN p_timestamp_utc timestamp with time zone, IN p_correlation_id character varying, IN p_safe_metadata text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO consent_audit_events (
        id, tenant_id, organization_id, consent_document_id,
        actor_type, actor_user_id, actor_name, event_type,
        timestamp_utc, correlation_id, safe_metadata
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_consent_document_id,
        p_actor_type, p_actor_user_id, p_actor_name, p_event_type,
        p_timestamp_utc, p_correlation_id, p_safe_metadata
    );
END;
$$;


--
-- Name: sp_add_event_classroom(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_event_classroom(IN p_event_id uuid, IN p_classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO event_classrooms (event_id, classroom_id) 
    VALUES (p_event_id, p_classroom_id)
    ON CONFLICT DO NOTHING;
END;
$$;


--
-- Name: sp_add_event_experience(uuid, uuid, uuid, uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_event_experience(IN p_id uuid, IN p_event_id uuid, IN p_experience_id uuid, IN p_experience_schedule_id uuid, IN p_experience_session_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO event_experiences (id, event_id, experience_id, experience_schedule_id, experience_session_id)
    VALUES (p_id, p_event_id, p_experience_id, p_experience_schedule_id, p_experience_session_id);
END;
$$;


--
-- Name: sp_add_material_order_history(uuid, uuid, character varying, character varying, character varying, uuid, character varying, character varying, text, text, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_add_material_order_history(IN p_id uuid, IN p_order_id uuid, IN p_event_type character varying, IN p_previous_status character varying, IN p_new_status character varying, IN p_actor_user_id uuid, IN p_actor_email character varying, IN p_actor_role character varying, IN p_description text, IN p_notes text, IN p_is_internal_note boolean, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_order_history (
        id, order_id, event_type, previous_status, new_status, actor_user_id,
        actor_email, actor_role, description, notes, is_internal_note, created_at
    ) VALUES (
        p_id, p_order_id, p_event_type, p_previous_status, p_new_status, p_actor_user_id,
        p_actor_email, p_actor_role, p_description, p_notes, p_is_internal_note, p_created_at
    );
END;
$$;


--
-- Name: sp_assign_organization_role(uuid, uuid, uuid, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_assign_organization_role(IN p_tenant_id uuid, IN p_organization_id uuid, IN p_user_id uuid, IN p_role_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO organization_memberships (user_id, organization_id, tenant_id)
    SELECT u.id, o.id, o.tenant_id
    FROM users u JOIN organizations o ON o.tenant_id = u.tenant_id
    WHERE u.id = p_user_id AND u.tenant_id = p_tenant_id AND o.id = p_organization_id
    ON CONFLICT DO NOTHING;

    INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
    SELECT p_user_id, r.id, p_organization_id, p_tenant_id
    FROM roles r
    WHERE r.tenant_id = p_tenant_id AND r.name = p_role_name AND r.scope_type = 'Organization'
      AND EXISTS (SELECT 1 FROM organization_memberships m
                  WHERE m.user_id = p_user_id AND m.organization_id = p_organization_id AND m.tenant_id = p_tenant_id)
    ON CONFLICT DO NOTHING;
END;
$$;


--
-- Name: sp_assign_platform_role(uuid, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_assign_platform_role(IN p_user_id uuid, IN p_role_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO user_roles (user_id, role_id)
    SELECT p_user_id, id
    FROM roles
    WHERE name = p_role_name AND tenant_id IS NULL AND scope_type = 'Platform'
    ON CONFLICT DO NOTHING;
END;
$$;


--
-- Name: sp_assign_substitute_coverage(uuid, uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_assign_substitute_coverage(IN p_tenant_id uuid, IN p_leave_id uuid, IN p_substitute_teacher_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE staff_leaves
    SET substitute_teacher_id = p_substitute_teacher_id,
        coverage_status = 'Covered'
    WHERE tenant_id = p_tenant_id AND id = p_leave_id;
END;
$$;


--
-- Name: sp_close_end_of_day_activities(uuid, uuid[], time without time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_close_end_of_day_activities(IN p_tenant_id uuid, IN p_schedule_ids uuid[], IN p_end_time time without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experience_schedules
    SET status = 'Completed',
        preparation_notes = CASE WHEN preparation_notes IS NULL OR preparation_notes = '' THEN 'System Auto-Closed at End of Day'
                                 WHEN preparation_notes LIKE '%System Auto-Closed%' THEN preparation_notes
                                 ELSE preparation_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_schedule_ids);

    UPDATE experience_sessions
    SET status = 'Completed', end_time = p_end_time,
        actual_notes = CASE WHEN actual_notes IS NULL OR actual_notes = '' THEN 'System Auto-Closed at End of Day'
                            WHEN actual_notes LIKE '%System Auto-Closed%' THEN actual_notes
                            ELSE actual_notes || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id
      AND experience_schedule_id = ANY(p_schedule_ids)
      AND status NOT IN ('Completed', 'Cancelled', 'Finished');
END;
$$;


--
-- Name: sp_close_end_of_day_children(time without time zone, uuid, uuid[]); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_close_end_of_day_children(IN p_departure time without time zone, IN p_user_id uuid, IN p_ids uuid[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_attendance
    SET departure_time = p_departure,
        pickup_person_name = 'System Auto-Closed',
        pickup_person_relationship = 'System Process',
        pickup_verification_method = 'System Auto-Closed',
        pickup_notes = CASE WHEN pickup_notes IS NULL OR pickup_notes = '' THEN 'System auto-checkout at End of Day'
                            ELSE pickup_notes || ' · System auto-checkout at End of Day' END,
        departure_verified_by_user_id = p_user_id,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;


--
-- Name: sp_close_end_of_day_events(uuid[]); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_close_end_of_day_events(IN p_ids uuid[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE events
    SET status = 'Completed',
        description = CASE WHEN description IS NULL OR description = '' THEN 'System Auto-Closed at End of Day'
                           WHEN description LIKE '%System Auto-Closed%' THEN description
                           ELSE description || ' [System Auto-Closed]' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;


--
-- Name: sp_close_end_of_day_teachers(time without time zone, uuid[]); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_close_end_of_day_teachers(IN p_clock_out time without time zone, IN p_ids uuid[])
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE teacher_attendance
    SET clock_out_time = p_clock_out,
        notes = CASE WHEN notes IS NULL OR notes = '' THEN 'System auto-checkout at End of Day'
                     ELSE notes || ' · System auto-checkout at End of Day' END,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = ANY(p_ids);
END;
$$;


--
-- Name: sp_create_activity_recommendation_item(uuid, uuid, uuid, uuid, character varying, character varying, integer, text, jsonb, jsonb, integer, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_activity_recommendation_item(IN p_id uuid, IN p_request_id uuid, IN p_tenant_id uuid, IN p_experience_id uuid, IN p_experience_title character varying, IN p_suggested_format character varying, IN p_estimated_duration_minutes integer, IN p_rationale text, IN p_supporting_evidence jsonb, IN p_materials jsonb, IN p_rank_order integer, IN p_is_evidence_limited boolean, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO activity_recommendation_items (
        id, request_id, tenant_id, experience_id, experience_title,
        suggested_format, estimated_duration_minutes, rationale,
        supporting_evidence, materials, rank_order, is_evidence_limited, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_experience_id, p_experience_title,
        p_suggested_format, p_estimated_duration_minutes, p_rationale,
        p_supporting_evidence, p_materials, p_rank_order, p_is_evidence_limited, p_created_at
    );
END;
$$;


--
-- Name: sp_create_activity_recommendation_request(uuid, uuid, uuid, uuid, uuid, uuid, timestamp with time zone, character varying, jsonb, integer, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_activity_recommendation_request(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_requested_by_user_id uuid, IN p_request_timestamp timestamp with time zone, IN p_scope character varying, IN p_context_preferences jsonb, IN p_evidence_count integer, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO activity_recommendation_requests (
        id, tenant_id, organization_id, classroom_id, child_id, requested_by_user_id,
        request_timestamp, scope, context_preferences, evidence_count, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id, p_requested_by_user_id,
        p_request_timestamp, p_scope, p_context_preferences, p_evidence_count, p_created_at
    );
END;
$$;


--
-- Name: sp_create_branch_floor_map(uuid, uuid, uuid, uuid, character varying, character varying, character varying, text, character varying, character varying, bigint, character varying, integer, character varying, timestamp with time zone, character varying, timestamp with time zone, character varying, timestamp with time zone, uuid, integer, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_branch_floor_map(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_name character varying, IN p_building_area character varying, IN p_floor_level character varying, IN p_description text, IN p_file_storage_key character varying, IN p_file_type character varying, IN p_file_size bigint, IN p_content_hash character varying, IN p_version integer, IN p_status character varying, IN p_review_due_at timestamp with time zone, IN p_created_by character varying, IN p_created_at timestamp with time zone, IN p_published_by character varying, IN p_published_at timestamp with time zone, IN p_supersedes_map_id uuid, IN p_row_version integer, IN p_emergency_instructions text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO branch_floor_maps (
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_name, p_building_area,
        p_floor_level, p_description, p_file_storage_key, p_file_type, p_file_size,
        p_content_hash, p_version, p_status, p_review_due_at, p_created_by, p_created_at,
        p_published_by, p_published_at, p_supersedes_map_id, p_row_version, p_emergency_instructions
    );
END;
$$;


--
-- Name: sp_create_branch_operational_goal(uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, date, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_branch_operational_goal(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_title character varying, IN p_category character varying, IN p_target_metric character varying, IN p_current_value character varying, IN p_target_value character varying, IN p_status character varying, IN p_target_date date, IN p_owner_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO branch_operational_goals (
        id, tenant_id, organization_id, branch_id, title, category,
        target_metric, current_value, target_value, status, target_date, owner_name
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_title, p_category,
        p_target_metric, p_current_value, p_target_value, p_status, p_target_date, p_owner_name
    );
END;
$$;


--
-- Name: sp_create_child_consent_document(uuid, uuid, uuid, uuid, uuid, uuid, uuid, character varying, integer, character varying, text, character varying, timestamp with time zone, uuid, timestamp with time zone, uuid, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_child_consent_document(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_consent_template_id uuid, IN p_template_version character varying, IN p_document_version integer, IN p_document_title character varying, IN p_document_snapshot text, IN p_status character varying, IN p_signing_deadline timestamp with time zone, IN p_created_by uuid, IN p_created_at timestamp with time zone, IN p_supersedes_document_id uuid, IN p_row_version integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_consent_documents (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        consent_template_id, template_version, document_version, document_title,
        document_snapshot, status, signing_deadline, created_by, created_at,
        supersedes_document_id, row_version
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_consent_template_id, p_template_version, p_document_version, p_document_title,
        p_document_snapshot, p_status, p_signing_deadline, p_created_by, p_created_at,
        p_supersedes_document_id, p_row_version
    );
END;
$$;


--
-- Name: sp_create_child_journey_insight_item(uuid, uuid, uuid, character varying, character varying, text, character varying, jsonb, uuid, character varying, integer, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_child_journey_insight_item(IN p_id uuid, IN p_request_id uuid, IN p_tenant_id uuid, IN p_category character varying, IN p_classification character varying, IN p_statement text, IN p_evidence_strength character varying, IN p_supporting_evidence jsonb, IN p_suggested_experience_id uuid, IN p_suggested_experience_title character varying, IN p_rank_order integer, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_journey_insight_items (
        id, request_id, tenant_id, category, classification, 
        statement, evidence_strength, supporting_evidence, 
        suggested_experience_id, suggested_experience_title, rank_order, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_category, p_classification, 
        p_statement, p_evidence_strength, p_supporting_evidence, 
        p_suggested_experience_id, p_suggested_experience_title, p_rank_order, p_created_at
    );
END;
$$;


--
-- Name: sp_create_child_journey_insight_request(uuid, uuid, uuid, uuid, uuid, uuid, character varying, date, date, character varying, text, integer, text, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_child_journey_insight_request(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_requested_by_user_id uuid, IN p_period_type character varying, IN p_period_start_date date, IN p_period_end_date date, IN p_focus_area character varying, IN p_teacher_question text, IN p_evidence_count integer, IN p_overall_summary text, IN p_evidence_gaps text, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_journey_insight_requests (
        id, tenant_id, organization_id, classroom_id, child_id, 
        requested_by_user_id, period_type, period_start_date, period_end_date, 
        focus_area, teacher_question, evidence_count, overall_summary, evidence_gaps, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id, 
        p_requested_by_user_id, p_period_type, p_period_start_date, p_period_end_date, 
        p_focus_area, p_teacher_question, p_evidence_count, p_overall_summary, p_evidence_gaps, p_created_at
    );
END;
$$;


--
-- Name: sp_create_emergency_contact(uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, text, text, character varying, character varying, timestamp with time zone, timestamp with time zone, boolean, integer, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_emergency_contact(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_scope character varying, IN p_category character varying, IN p_name character varying, IN p_phone_number character varying, IN p_alternate_phone_number character varying, IN p_country_region character varying, IN p_address text, IN p_instructions text, IN p_verification_status character varying, IN p_verified_by character varying, IN p_verified_at timestamp with time zone, IN p_review_due_at timestamp with time zone, IN p_is_active boolean, IN p_display_order integer, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO emergency_contacts (
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_scope, p_category,
        p_name, p_phone_number, p_alternate_phone_number, p_country_region, p_address,
        p_instructions, p_verification_status, p_verified_by, p_verified_at,
        p_review_due_at, p_is_active, p_display_order, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_enrollment_application(uuid, uuid, uuid, character varying, character varying, date, character varying, character varying, character varying, character varying, uuid, date, character varying, character varying, timestamp with time zone, numeric, numeric, numeric, numeric, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_enrollment_application(IN p_id uuid, IN p_tenant_id uuid, IN p_branch_id uuid, IN p_child_first_name character varying, IN p_child_last_name character varying, IN p_date_of_birth date, IN p_gender character varying, IN p_parent_guardian_name character varying, IN p_parent_email character varying, IN p_parent_phone character varying, IN p_target_classroom_id uuid, IN p_desired_start_date date, IN p_schedule_type character varying, IN p_pipeline_stage character varying, IN p_tour_date timestamp with time zone, IN p_tuition_monthly_rate numeric, IN p_deposit_amount numeric, IN p_sibling_discount_percent numeric, IN p_registration_fee numeric, IN p_internal_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO enrollment_pipeline_applications (
        id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
        parent_guardian_name, parent_email, parent_phone, target_classroom_id, desired_start_date,
        schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
        sibling_discount_percent, registration_fee, internal_notes
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_child_first_name, p_child_last_name, p_date_of_birth, p_gender,
        p_parent_guardian_name, p_parent_email, p_parent_phone, p_target_classroom_id, p_desired_start_date,
        p_schedule_type, p_pipeline_stage, p_tour_date, p_tuition_monthly_rate, p_deposit_amount,
        p_sibling_discount_percent, p_registration_fee, p_internal_notes
    );
END;
$$;


--
-- Name: sp_create_experience_session(uuid, uuid, uuid, uuid, uuid, date, time without time zone, time without time zone, character varying, uuid, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_experience_session(IN p_id uuid, IN p_tenant_id uuid, IN p_experience_id uuid, IN p_experience_schedule_id uuid, IN p_classroom_id uuid, IN p_session_date date, IN p_start_time time without time zone, IN p_end_time time without time zone, IN p_status character varying, IN p_primary_teacher_id uuid, IN p_actual_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO experience_sessions (
        id, tenant_id, experience_id, experience_schedule_id, classroom_id, 
        session_date, start_time, end_time, status, primary_teacher_id, actual_notes
    ) VALUES (
        p_id, p_tenant_id, p_experience_id, p_experience_schedule_id, p_classroom_id, 
        p_session_date, p_start_time, p_end_time, p_status, p_primary_teacher_id, p_actual_notes
    );
END;
$$;


--
-- Name: sp_create_family_billing_contract(uuid, uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, numeric, numeric, numeric, numeric, date); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_family_billing_contract(IN p_id uuid, IN p_tenant_id uuid, IN p_branch_id uuid, IN p_application_id uuid, IN p_child_id uuid, IN p_parent_name character varying, IN p_parent_email character varying, IN p_schedule_type character varying, IN p_billing_cycle character varying, IN p_base_rate numeric, IN p_discount_percent numeric, IN p_effective_rate numeric, IN p_deposit_held numeric, IN p_start_date date)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO family_billing_contracts (
        id, tenant_id, branch_id, application_id, child_id, parent_name, parent_email,
        schedule_type, billing_cycle, base_rate, discount_percent, effective_rate,
        deposit_held, status, start_date, auto_invoice
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_application_id, p_child_id, p_parent_name, p_parent_email,
        p_schedule_type, p_billing_cycle, p_base_rate, p_discount_percent, p_effective_rate,
        p_deposit_held, 'Active', p_start_date, TRUE
    );
END;
$$;


--
-- Name: sp_create_initial_tuition_invoice(uuid, uuid, uuid, character varying, numeric, text, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_initial_tuition_invoice(IN p_invoice_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_invoice_num character varying, IN p_total_amount numeric, IN p_desc text, IN p_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, invoice_number, currency, issue_date, due_date, status,
        subtotal, tax_amount, total_amount, amount_paid, description, notes, created_by, created_at, updated_at
    ) VALUES (
        p_invoice_id, p_tenant_id, p_organization_id, p_invoice_num, 'INR', NOW(), NOW() + INTERVAL '7 days', 'Issued',
        p_total_amount, 0, p_total_amount, 0,
        p_desc, p_notes, 'Enrollment System', NOW(), NOW()
    );
END;
$$;


--
-- Name: sp_create_journey_entry(uuid, uuid, uuid, uuid, uuid, uuid, uuid, uuid, uuid, uuid, uuid, uuid, character varying, character varying, text, text, text, timestamp with time zone, character varying, boolean, boolean, character varying, timestamp with time zone, uuid, timestamp with time zone, uuid, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_journey_entry(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_enrollment_id uuid, IN p_classroom_id uuid, IN p_teacher_id uuid, IN p_experience_id uuid, IN p_experience_schedule_id uuid, IN p_experience_session_id uuid, IN p_experience_participation_id uuid, IN p_little_moment_id uuid, IN p_milestone_id uuid, IN p_event_id uuid, IN p_journey_type character varying, IN p_title character varying, IN p_observation text, IN p_child_voice text, IN p_teacher_reflection text, IN p_observation_date timestamp with time zone, IN p_participation_type character varying, IN p_is_milestone boolean, IN p_is_parent_visible boolean, IN p_parent_visibility_status character varying, IN p_created_at timestamp with time zone, IN p_created_by uuid, IN p_updated_at timestamp with time zone, IN p_updated_by uuid, IN p_version integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_journey_entries (
        id, tenant_id, child_id, enrollment_id, classroom_id, teacher_id, 
        experience_id, experience_schedule_id, experience_session_id, experience_participation_id, 
        little_moment_id, milestone_id, event_id,
        journey_type, title, observation, child_voice, teacher_reflection, 
        observation_date, participation_type, is_milestone, is_parent_visible, 
        parent_visibility_status, created_at, created_by, updated_at, updated_by, version
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_enrollment_id, p_classroom_id, p_teacher_id, 
        p_experience_id, p_experience_schedule_id, p_experience_session_id, p_experience_participation_id, 
        p_little_moment_id, p_milestone_id, p_event_id,
        p_journey_type, p_title, p_observation, p_child_voice, p_teacher_reflection, 
        p_observation_date, p_participation_type, p_is_milestone, p_is_parent_visible, 
        p_parent_visibility_status, p_created_at, p_created_by, p_updated_at, p_updated_by, p_version
    );
END;
$$;


--
-- Name: sp_create_journey_evidence(uuid, uuid, character varying, uuid, text, timestamp with time zone, uuid, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_journey_evidence(IN p_id uuid, IN p_journey_entry_id uuid, IN p_evidence_type character varying, IN p_media_id uuid, IN p_text_content text, IN p_captured_at timestamp with time zone, IN p_captured_by uuid, IN p_visibility character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_journey_evidence (
        id, journey_entry_id, evidence_type, media_id, text_content, 
        captured_at, captured_by, visibility, created_at
    ) VALUES (
        p_id, p_journey_entry_id, p_evidence_type, p_media_id, p_text_content, 
        p_captured_at, p_captured_by, p_visibility, p_created_at
    );
END;
$$;


--
-- Name: sp_create_material_loan(uuid, uuid, uuid, uuid, uuid, uuid, integer, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, numeric, character varying, text, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_loan(IN p_id uuid, IN p_order_id uuid, IN p_order_item_id uuid, IN p_material_product_id uuid, IN p_organization_id uuid, IN p_classroom_id uuid, IN p_quantity_issued integer, IN p_issued_at timestamp with time zone, IN p_due_date timestamp with time zone, IN p_actual_return_date timestamp with time zone, IN p_return_status character varying, IN p_deposit_amount numeric, IN p_deposit_status character varying, IN p_notes text, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_loans (
        id, order_id, order_item_id, material_product_id, organization_id,
        classroom_id, quantity_issued, issued_at, due_date, actual_return_date,
        return_status, deposit_amount, deposit_status, notes, created_at, updated_at
    ) VALUES (
        p_id, p_order_id, p_order_item_id, p_material_product_id, p_organization_id,
        p_classroom_id, p_quantity_issued, p_issued_at, p_due_date, p_actual_return_date,
        p_return_status, p_deposit_amount, p_deposit_status, p_notes, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_material_offering(uuid, uuid, boolean, numeric, boolean, numeric, numeric, integer, integer, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_offering(IN p_id uuid, IN p_material_product_id uuid, IN p_allow_purchase boolean, IN p_purchase_price numeric, IN p_allow_borrow boolean, IN p_rental_fee numeric, IN p_refundable_deposit numeric, IN p_default_loan_duration_days integer, IN p_max_loan_duration_days integer, IN p_currency character varying, IN p_is_active boolean, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_offerings (
        id, material_product_id, allow_purchase, purchase_price, allow_borrow,
        rental_fee, refundable_deposit, default_loan_duration_days, max_loan_duration_days,
        currency, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_material_product_id, p_allow_purchase, p_purchase_price, p_allow_borrow,
        p_rental_fee, p_refundable_deposit, p_default_loan_duration_days, p_max_loan_duration_days,
        p_currency, p_is_active, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_material_order(uuid, character varying, uuid, uuid, uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, numeric, numeric, numeric, numeric, numeric, numeric, character varying, text, timestamp with time zone, text, character varying, character varying, timestamp with time zone, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_order(IN p_id uuid, IN p_order_number character varying, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_planned_experience_id uuid, IN p_requesting_user_id uuid, IN p_requesting_user_email character varying, IN p_order_type character varying, IN p_workflow_status character varying, IN p_payment_status character varying, IN p_fulfilment_status character varying, IN p_total_purchase_amount numeric, IN p_total_rental_fee numeric, IN p_total_deposit numeric, IN p_shipping_fee numeric, IN p_tax_amount numeric, IN p_total_amount numeric, IN p_currency character varying, IN p_quotation_notes text, IN p_quotation_valid_until timestamp with time zone, IN p_delivery_address text, IN p_delivery_contact_name character varying, IN p_delivery_contact_phone character varying, IN p_required_delivery_date timestamp with time zone, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_orders (
        id, order_number, tenant_id, organization_id, branch_id, classroom_id,
        planned_experience_id, requesting_user_id, requesting_user_email,
        order_type, workflow_status, payment_status, fulfilment_status,
        total_purchase_amount, total_rental_fee, total_deposit, shipping_fee,
        tax_amount, total_amount, currency, quotation_notes, quotation_valid_until,
        delivery_address, delivery_contact_name, delivery_contact_phone,
        required_delivery_date, created_at, updated_at
    ) VALUES (
        p_id, p_order_number, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        p_planned_experience_id, p_requesting_user_id, p_requesting_user_email,
        p_order_type, p_workflow_status, p_payment_status, p_fulfilment_status,
        p_total_purchase_amount, p_total_rental_fee, p_total_deposit, p_shipping_fee,
        p_tax_amount, p_total_amount, p_currency, p_quotation_notes, p_quotation_valid_until,
        p_delivery_address, p_delivery_contact_name, p_delivery_contact_phone,
        p_required_delivery_date, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_material_order_item(uuid, uuid, uuid, uuid, character varying, integer, numeric, numeric, numeric, numeric, integer, timestamp with time zone, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_order_item(IN p_id uuid, IN p_order_id uuid, IN p_material_product_id uuid, IN p_offering_id uuid, IN p_fulfilment_mode character varying, IN p_quantity integer, IN p_unit_price numeric, IN p_rental_fee numeric, IN p_deposit numeric, IN p_line_total numeric, IN p_loan_duration_days integer, IN p_return_due_date timestamp with time zone, IN p_status character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_order_items (
        id, order_id, material_product_id, offering_id, fulfilment_mode,
        quantity, unit_price, rental_fee, deposit, line_total, loan_duration_days,
        return_due_date, status, created_at
    ) VALUES (
        p_id, p_order_id, p_material_product_id, p_offering_id, p_fulfilment_mode,
        p_quantity, p_unit_price, p_rental_fee, p_deposit, p_line_total, p_loan_duration_days,
        p_return_due_date, p_status, p_created_at
    );
END;
$$;


--
-- Name: sp_create_material_product(uuid, character varying, character varying, text, character varying, character varying, character varying, text, integer, integer, text, text, text, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_product(IN p_id uuid, IN p_sku character varying, IN p_name character varying, IN p_description text, IN p_category character varying, IN p_image_url character varying, IN p_material_type character varying, IN p_safety_instructions text, IN p_age_min_months integer, IN p_age_max_months integer, IN p_usage_restrictions text, IN p_cleaning_instructions text, IN p_return_policy text, IN p_is_active boolean, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_products (
        id, sku, name, description, category, image_url, material_type,
        safety_instructions, age_min_months, age_max_months, usage_restrictions,
        cleaning_instructions, return_policy, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_sku, p_name, p_description, p_category, p_image_url, p_material_type,
        p_safety_instructions, p_age_min_months, p_age_max_months, p_usage_restrictions,
        p_cleaning_instructions, p_return_policy, p_is_active, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_material_recurring_schedule(uuid, uuid, uuid, uuid, uuid, character varying, character varying, uuid, uuid, integer, integer, text, timestamp with time zone, timestamp with time zone, character varying, text, uuid, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_recurring_schedule(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_schedule_type character varying, IN p_frequency character varying, IN p_material_product_id uuid, IN p_offering_id uuid, IN p_quantity integer, IN p_loan_duration_days integer, IN p_delivery_address text, IN p_next_run_date timestamp with time zone, IN p_last_run_date timestamp with time zone, IN p_status character varying, IN p_notes text, IN p_created_by_user_id uuid, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_recurring_schedules (
        id, tenant_id, organization_id, branch_id, classroom_id, schedule_type,
        frequency, material_product_id, offering_id, quantity, loan_duration_days,
        delivery_address, next_run_date, last_run_date, status, notes,
        created_by_user_id, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_schedule_type,
        p_frequency, p_material_product_id, p_offering_id, p_quantity, p_loan_duration_days,
        p_delivery_address, p_next_run_date, p_last_run_date, p_status, p_notes,
        p_created_by_user_id, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_material_shipment(uuid, uuid, character varying, character varying, character varying, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, text, uuid, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_material_shipment(IN p_id uuid, IN p_order_id uuid, IN p_shipment_number character varying, IN p_carrier_name character varying, IN p_tracking_reference character varying, IN p_dispatch_date timestamp with time zone, IN p_estimated_delivery_date timestamp with time zone, IN p_actual_delivery_date timestamp with time zone, IN p_delivery_status character varying, IN p_proof_of_delivery_notes text, IN p_recorded_by_user_id uuid, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_shipments (
        id, order_id, shipment_number, carrier_name, tracking_reference,
        dispatch_date, estimated_delivery_date, actual_delivery_date,
        delivery_status, proof_of_delivery_notes, recorded_by_user_id,
        created_at, updated_at
    ) VALUES (
        p_id, p_order_id, p_shipment_number, p_carrier_name, p_tracking_reference,
        p_dispatch_date, p_estimated_delivery_date, p_actual_delivery_date,
        p_delivery_status, p_proof_of_delivery_notes, p_recorded_by_user_id,
        p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_milestone(uuid, uuid, uuid, uuid, uuid, character varying, text, timestamp with time zone, uuid, text, uuid, boolean, character varying, timestamp with time zone, uuid, timestamp with time zone, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_milestone(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_classroom_id uuid, IN p_teacher_id uuid, IN p_title character varying, IN p_description text, IN p_milestone_date timestamp with time zone, IN p_development_area_id uuid, IN p_evidence_summary text, IN p_journey_entry_id uuid, IN p_parent_visible boolean, IN p_status character varying, IN p_created_at timestamp with time zone, IN p_created_by uuid, IN p_updated_at timestamp with time zone, IN p_updated_by uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_milestones (
        id, tenant_id, child_id, classroom_id, teacher_id, title, description, 
        milestone_date, development_area_id, evidence_summary, journey_entry_id, 
        parent_visible, status, created_at, created_by, updated_at, updated_by
    ) VALUES (
        p_id, p_tenant_id, p_child_id, p_classroom_id, p_teacher_id, p_title, p_description, 
        p_milestone_date, p_development_area_id, p_evidence_summary, p_journey_entry_id, 
        p_parent_visible, p_status, p_created_at, p_created_by, p_updated_at, p_updated_by
    );
END;
$$;


--
-- Name: sp_create_parent_communication(uuid, uuid, uuid, uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, date, date, character varying, text, text, text, jsonb, boolean, character varying, character varying, uuid, character varying, uuid, timestamp with time zone, timestamp with time zone, character varying, character varying, text, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_parent_communication(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_recipient_person_id uuid, IN p_recipient_user_id uuid, IN p_recipient_email character varying, IN p_recipient_name character varying, IN p_recipient_relationship character varying, IN p_communication_type character varying, IN p_reporting_date_start date, IN p_reporting_date_end date, IN p_title character varying, IN p_content_draft text, IN p_content_final text, IN p_family_conversation_starter text, IN p_evidence_summary_json jsonb, IN p_consent_verified boolean, IN p_consent_notes character varying, IN p_status character varying, IN p_teacher_id uuid, IN p_teacher_name character varying, IN p_approved_by_user_id uuid, IN p_approved_at timestamp with time zone, IN p_sent_at timestamp with time zone, IN p_delivery_channel character varying, IN p_delivery_status character varying, IN p_delivery_error text, IN p_idempotency_key character varying, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO parent_communications (
        id, tenant_id, organization_id, classroom_id, child_id,
        recipient_person_id, recipient_user_id, recipient_email, recipient_name, recipient_relationship,
        communication_type, reporting_date_start, reporting_date_end,
        title, content_draft, content_final, family_conversation_starter,
        evidence_summary_json, consent_verified, consent_notes,
        status, teacher_id, teacher_name, approved_by_user_id, approved_at, sent_at,
        delivery_channel, delivery_status, delivery_error, idempotency_key,
        created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id,
        p_recipient_person_id, p_recipient_user_id, p_recipient_email, p_recipient_name, p_recipient_relationship,
        p_communication_type, p_reporting_date_start, p_reporting_date_end,
        p_title, p_content_draft, p_content_final, p_family_conversation_starter,
        p_evidence_summary_json, p_consent_verified, p_consent_notes,
        p_status, p_teacher_id, p_teacher_name, p_approved_by_user_id, p_approved_at, p_sent_at,
        p_delivery_channel, p_delivery_status, p_delivery_error, p_idempotency_key,
        p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_teacher_handover(uuid, uuid, uuid, uuid, uuid, date, character varying, uuid, character varying, uuid, character varying, text, text, text, text, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_teacher_handover(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_handover_date date, IN p_shift_type character varying, IN p_outgoing_teacher_user_id uuid, IN p_outgoing_teacher_name character varying, IN p_incoming_teacher_user_id uuid, IN p_incoming_teacher_name character varying, IN p_summary_notes text, IN p_child_specific_notes text, IN p_outstanding_care_tasks text, IN p_pending_followups text, IN p_status character varying, IN p_is_critical boolean, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO teacher_handovers (
        id, tenant_id, organization_id, branch_id, classroom_id, handover_date,
        shift_type, outgoing_teacher_user_id, outgoing_teacher_name,
        incoming_teacher_user_id, incoming_teacher_name, summary_notes,
        child_specific_notes, outstanding_care_tasks, pending_followups,
        status, is_critical, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_handover_date,
        p_shift_type, p_outgoing_teacher_user_id, p_outgoing_teacher_name,
        p_incoming_teacher_user_id, p_incoming_teacher_name, p_summary_notes,
        p_child_specific_notes, p_outstanding_care_tasks, p_pending_followups,
        p_status, p_is_critical, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_create_weekly_planning_draft_item(uuid, uuid, uuid, date, character varying, character varying, uuid, character varying, text, character varying, integer, text, text, character varying, text, uuid, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_weekly_planning_draft_item(IN p_id uuid, IN p_request_id uuid, IN p_tenant_id uuid, IN p_scheduled_date date, IN p_day_of_week character varying, IN p_time_of_day character varying, IN p_experience_id uuid, IN p_experience_title character varying, IN p_learning_goal text, IN p_group_format character varying, IN p_estimated_duration_minutes integer, IN p_required_materials text, IN p_rationale text, IN p_validation_status character varying, IN p_validation_notes text, IN p_resulting_schedule_id uuid, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO weekly_planning_draft_items (
        id, request_id, tenant_id, scheduled_date, day_of_week, time_of_day, 
        experience_id, experience_title, learning_goal, group_format, 
        estimated_duration_minutes, required_materials, rationale, 
        validation_status, validation_notes, resulting_schedule_id, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_scheduled_date, p_day_of_week, p_time_of_day, 
        p_experience_id, p_experience_title, p_learning_goal, p_group_format, 
        p_estimated_duration_minutes, p_required_materials, p_rationale, 
        p_validation_status, p_validation_notes, p_resulting_schedule_id, p_created_at
    );
END;
$$;


--
-- Name: sp_create_weekly_planning_draft_request(uuid, uuid, uuid, uuid, uuid, date, date, character varying, character varying, text, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_create_weekly_planning_draft_request(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_classroom_id uuid, IN p_requested_by_user_id uuid, IN p_week_start_date date, IN p_week_end_date date, IN p_focus_areas character varying, IN p_indoor_outdoor_preference character varying, IN p_materials_notes text, IN p_status character varying, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO weekly_planning_draft_requests (
        id, tenant_id, organization_id, classroom_id, requested_by_user_id, 
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference, 
        materials_notes, status, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_requested_by_user_id, 
        p_week_start_date, p_week_end_date, p_focus_areas, p_indoor_outdoor_preference, 
        p_materials_notes, p_status, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_delete_branch(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_branch(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM branches
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_child(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_child(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE children SET is_active = false
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_classroom(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_classroom(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE classrooms SET is_active = false
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_consent_choices(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_consent_choices(IN p_tenant_id uuid, IN p_doc_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM consent_choices WHERE tenant_id = p_tenant_id AND consent_document_id = p_doc_id;
END;
$$;


--
-- Name: sp_delete_emergency_contact(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_emergency_contact(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM emergency_contacts WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;


--
-- Name: sp_delete_event(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_event(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM events WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_event_task(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_event_task(IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM event_tasks WHERE id = p_id;
END;
$$;


--
-- Name: sp_delete_experience(uuid, uuid, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_experience(IN p_tenant_id uuid, IN p_id uuid, IN p_status character varying, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experiences
    SET status = p_status, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_experience_feedback(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_experience_feedback(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM experience_feedback WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_experience_schedule(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_experience_schedule(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM experience_schedules WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_floor_map_markers(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_floor_map_markers(IN p_floor_map_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM floor_map_markers WHERE floor_map_id = p_floor_map_id;
END;
$$;


--
-- Name: sp_delete_invoice_line_items(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_invoice_line_items(IN p_invoice_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM invoice_line_items WHERE invoice_id = p_invoice_id;
END;
$$;


--
-- Name: sp_delete_journey_developments(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_journey_developments(IN p_journey_entry_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM child_journey_developments WHERE journey_entry_id = p_journey_entry_id;
END;
$$;


--
-- Name: sp_delete_journey_entry(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_journey_entry(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM child_journey_entries WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_organization(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_organization(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM organizations
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_participations_for_session(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_participations_for_session(IN p_session_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM experience_participations WHERE experience_session_id = p_session_id;
END;
$$;


--
-- Name: sp_delete_teacher(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_teacher(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE teachers SET status = 'Inactive'
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_delete_user(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_delete_user(IN p_tenant_id uuid, IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM users WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_dispatch_teacher_to_classroom(uuid, uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_dispatch_teacher_to_classroom(IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_user_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO classroom_teachers (id, tenant_id, classroom_id, user_id, is_primary)
    VALUES (gen_random_uuid(), p_tenant_id, p_classroom_id, p_user_id, FALSE)
    ON CONFLICT (classroom_id, user_id) DO NOTHING;
END;
$$;


--
-- Name: sp_end_child_nap_session(uuid, uuid, timestamp with time zone, integer, character varying, text, uuid, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_end_child_nap_session(IN p_tenant_id uuid, IN p_session_id uuid, IN p_end_time timestamp with time zone, IN p_duration_minutes integer, IN p_outcome character varying, IN p_notes text, IN p_ended_by_user_id uuid, IN p_ended_by_name character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_nap_sessions
    SET end_time = p_end_time,
        duration_minutes = p_duration_minutes,
        status = 'Completed',
        outcome = p_outcome,
        notes = COALESCE(p_notes, notes),
        ended_by_user_id = p_ended_by_user_id,
        ended_by_name = p_ended_by_name,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_session_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_ensure_child_enrolled(uuid, uuid, uuid, character varying, character varying, date); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_ensure_child_enrolled(IN p_child_id uuid, IN p_tenant_id uuid, IN p_classroom_id uuid, IN p_first_name character varying, IN p_last_name character varying, IN p_date_of_birth date)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth, enrollment_status, is_active, created_at, updated_at)
    VALUES (p_child_id, p_tenant_id, p_classroom_id, p_first_name, p_last_name, p_date_of_birth, 'Enrolled', TRUE, NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        classroom_id = EXCLUDED.classroom_id,
        enrollment_status = 'Enrolled',
        updated_at = NOW();
END;
$$;


--
-- Name: sp_get_child_journey_insight_items(uuid, uuid, refcursor); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_get_child_journey_insight_items(IN p_tenant_id uuid, IN p_request_id uuid, INOUT cur refcursor DEFAULT 'cur'::refcursor)
    LANGUAGE plpgsql
    AS $$
BEGIN
    OPEN cur FOR
    SELECT 
        id, request_id, tenant_id, category, classification,
        statement, evidence_strength, supporting_evidence,
        suggested_experience_id, suggested_experience_title, rank_order, created_at
    FROM child_journey_insight_items
    WHERE tenant_id = p_tenant_id AND request_id = p_request_id
    ORDER BY rank_order ASC, created_at ASC;
END;
$$;


--
-- Name: sp_get_child_journey_insight_request_by_id(uuid, uuid, refcursor); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_get_child_journey_insight_request_by_id(IN p_tenant_id uuid, IN p_request_id uuid, INOUT cur refcursor DEFAULT 'cur'::refcursor)
    LANGUAGE plpgsql
    AS $$
BEGIN
    OPEN cur FOR
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND id = p_request_id;
END;
$$;


--
-- Name: sp_get_latest_child_journey_insight(uuid, uuid, refcursor); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_get_latest_child_journey_insight(IN p_tenant_id uuid, IN p_child_id uuid, INOUT cur refcursor DEFAULT 'cur'::refcursor)
    LANGUAGE plpgsql
    AS $$
BEGIN
    OPEN cur FOR
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id
    ORDER BY created_at DESC
    LIMIT 1;
END;
$$;


--
-- Name: sp_insert_child_interest(uuid, uuid, uuid, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_child_interest(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_name character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_interests (id, tenant_id, child_id, name, created_at)
    VALUES (p_id, p_tenant_id, p_child_id, p_name, p_created_at);
END;
$$;


--
-- Name: sp_insert_child_story(uuid, uuid, uuid, uuid, uuid, text, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_child_story(IN p_id uuid, IN p_tenant_id uuid, IN p_child_id uuid, IN p_teacher_id uuid, IN p_experience_schedule_id uuid, IN p_content text, IN p_status character varying, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_stories (id, tenant_id, child_id, teacher_id, experience_schedule_id, content, status, created_at, updated_at)
    VALUES (p_id, p_tenant_id, p_child_id, p_teacher_id, p_experience_schedule_id, p_content, p_status, p_created_at, p_updated_at);
END;
$$;


--
-- Name: sp_insert_consent_choice(uuid, uuid, uuid, uuid, character varying, character varying, boolean, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_consent_choice(IN p_id uuid, IN p_tenant_id uuid, IN p_consent_document_id uuid, IN p_consent_signer_id uuid, IN p_section_key character varying, IN p_section_title character varying, IN p_is_mandatory boolean, IN p_choice character varying, IN p_recorded_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO consent_choices (
        id, tenant_id, consent_document_id, consent_signer_id,
        section_key, section_title, is_mandatory, choice, recorded_at
    ) VALUES (
        p_id, p_tenant_id, p_consent_document_id, p_consent_signer_id,
        p_section_key, p_section_title, p_is_mandatory, p_choice, p_recorded_at
    );
END;
$$;


--
-- Name: sp_insert_consent_signer(uuid, uuid, uuid, uuid, uuid, uuid, character varying, character varying, character varying, boolean, character varying, character varying, timestamp with time zone, integer, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_consent_signer(IN p_id uuid, IN p_tenant_id uuid, IN p_consent_document_id uuid, IN p_family_relationship_id uuid, IN p_person_id uuid, IN p_intended_signer_user_id uuid, IN p_signer_name_snapshot character varying, IN p_signer_email_snapshot character varying, IN p_signer_relationship character varying, IN p_is_required boolean, IN p_signing_status character varying, IN p_token_hash character varying, IN p_token_expires_at timestamp with time zone, IN p_invitation_sent_count integer, IN p_last_invitation_sent_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO consent_signers (
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at
    ) VALUES (
        p_id, p_tenant_id, p_consent_document_id, p_family_relationship_id, p_person_id,
        p_intended_signer_user_id, p_signer_name_snapshot, p_signer_email_snapshot,
        p_signer_relationship, p_is_required, p_signing_status, p_token_hash,
        p_token_expires_at, p_invitation_sent_count, p_last_invitation_sent_at
    );
END;
$$;


--
-- Name: sp_insert_experience_feedback(uuid, uuid, uuid, uuid, uuid, integer, character varying, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_experience_feedback(IN p_id uuid, IN p_tenant_id uuid, IN p_experience_schedule_id uuid, IN p_experience_id uuid, IN p_teacher_id uuid, IN p_stars integer, IN p_rating character varying, IN p_notes text, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO experience_feedback (id, tenant_id, experience_schedule_id, experience_id, teacher_id, stars, rating, notes, created_at)
    VALUES (p_id, p_tenant_id, p_experience_schedule_id, p_experience_id, p_teacher_id, p_stars, p_rating, p_notes, p_created_at);
END;
$$;


--
-- Name: sp_insert_experience_participation(uuid, uuid, uuid, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_experience_participation(IN p_id uuid, IN p_experience_session_id uuid, IN p_child_id uuid, IN p_participation_status character varying, IN p_participation_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO experience_participations (
        id, experience_session_id, child_id, participation_status, participation_notes
    ) VALUES (
        p_id, p_experience_session_id, p_child_id, p_participation_status, p_participation_notes
    );
END;
$$;


--
-- Name: sp_insert_floor_map_marker(uuid, uuid, character varying, character varying, text, double precision, double precision, integer, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_floor_map_marker(IN p_id uuid, IN p_floor_map_id uuid, IN p_marker_type character varying, IN p_label character varying, IN p_instructions text, IN p_normalized_x double precision, IN p_normalized_y double precision, IN p_display_order integer, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO floor_map_markers (
        id, floor_map_id, marker_type, label, instructions,
        normalized_x, normalized_y, display_order, created_at
    ) VALUES (
        p_id, p_floor_map_id, p_marker_type, p_label, p_instructions,
        p_normalized_x, p_normalized_y, p_display_order, p_created_at
    );
END;
$$;


--
-- Name: sp_insert_invoice(uuid, uuid, uuid, character varying, character varying, numeric, numeric, numeric, numeric, numeric, character varying, character varying, timestamp with time zone, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, timestamp with time zone, character varying, text, character varying, text, text, text, character varying, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_invoice(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_invoice_number character varying, IN p_billing_period character varying, IN p_subtotal numeric, IN p_discount_amount numeric, IN p_tax_amount numeric, IN p_total_amount numeric, IN p_amount_paid numeric, IN p_currency character varying, IN p_status character varying, IN p_issue_date timestamp with time zone, IN p_due_date timestamp with time zone, IN p_paid_at timestamp with time zone, IN p_published_at timestamp with time zone, IN p_published_by character varying, IN p_voided_at timestamp with time zone, IN p_voided_by character varying, IN p_void_reason text, IN p_created_by character varying, IN p_description text, IN p_notes text, IN p_payment_instructions text, IN p_billing_contact_email character varying, IN p_document_storage_key character varying, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_invoice_number, p_billing_period,
        p_subtotal, p_discount_amount, p_tax_amount, p_total_amount, p_amount_paid,
        p_currency, p_status, p_issue_date, p_due_date, p_paid_at, p_published_at,
        p_published_by, p_voided_at, p_voided_by, p_void_reason, p_created_by,
        p_description, p_notes, p_payment_instructions, p_billing_contact_email,
        p_document_storage_key, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_insert_invoice_line_item(uuid, uuid, integer, character varying, numeric, numeric, numeric, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_invoice_line_item(IN p_id uuid, IN p_invoice_id uuid, IN p_line_number integer, IN p_description character varying, IN p_quantity numeric, IN p_unit_price numeric, IN p_line_total numeric, IN p_category character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO invoice_line_items (
        id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    ) VALUES (
        p_id, p_invoice_id, p_line_number, p_description, p_quantity, p_unit_price, p_line_total, p_category, p_created_at
    );
END;
$$;


--
-- Name: sp_insert_journey_development(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_journey_development(IN p_journey_entry_id uuid, IN p_development_area_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_journey_developments (journey_entry_id, development_area_id) 
    VALUES (p_journey_entry_id, p_development_area_id);
END;
$$;


--
-- Name: sp_insert_payment(uuid, uuid, uuid, uuid, character varying, character varying, numeric, character varying, character varying, boolean, character varying, timestamp with time zone, character varying, character varying, text, character varying, character varying, character varying, character varying, uuid, character varying, boolean, timestamp with time zone, character varying, text, character varying, text, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_payment(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_invoice_id uuid, IN p_invoice_number character varying, IN p_payment_reference character varying, IN p_amount numeric, IN p_currency character varying, IN p_method character varying, IN p_is_verified boolean, IN p_verified_by character varying, IN p_verified_at timestamp with time zone, IN p_status character varying, IN p_transaction_id character varying, IN p_notes text, IN p_provider_order_id character varying, IN p_provider_payment_id character varying, IN p_provider_signature character varying, IN p_receipt_number character varying, IN p_recorded_by_user_id uuid, IN p_recorded_by_email character varying, IN p_is_reversed boolean, IN p_reversed_at timestamp with time zone, IN p_reversed_by character varying, IN p_reversal_reason text, IN p_reversal_reference character varying, IN p_audit_metadata text, IN p_payment_date timestamp with time zone, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO payments (
        id, tenant_id, organization_id, invoice_id, invoice_number,
        payment_reference, amount, currency, method, is_verified,
        verified_by, verified_at, status, transaction_id, notes,
        provider_order_id, provider_payment_id, provider_signature,
        receipt_number, recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by, reversal_reason, reversal_reference,
        audit_metadata, payment_date, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_invoice_id, p_invoice_number,
        p_payment_reference, p_amount, p_currency, p_method, p_is_verified,
        p_verified_by, p_verified_at, p_status, p_transaction_id, p_notes,
        p_provider_order_id, p_provider_payment_id, p_provider_signature,
        p_receipt_number, p_recorded_by_user_id, p_recorded_by_email,
        p_is_reversed, p_reversed_at, p_reversed_by, p_reversal_reason, p_reversal_reference,
        CASE WHEN p_audit_metadata IS NULL THEN NULL ELSE p_audit_metadata::jsonb END,
        p_payment_date, p_created_at
    );
END;
$$;


--
-- Name: sp_insert_staff_leave(uuid, uuid, uuid, character varying, date, date, character varying, character varying, character varying, uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_insert_staff_leave(IN p_id uuid, IN p_tenant_id uuid, IN p_teacher_id uuid, IN p_leave_type character varying, IN p_start_date date, IN p_end_date date, IN p_reason character varying, IN p_status character varying, IN p_coverage_status character varying, IN p_substitute_teacher_id uuid, IN p_reviewed_by_user_id uuid)
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


--
-- Name: sp_log_audit_entry(uuid, uuid, character varying, character varying, uuid, jsonb, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_log_audit_entry(IN p_tenant_id uuid, IN p_user_id uuid, IN p_action character varying, IN p_entity_type character varying, IN p_entity_id uuid, IN p_details jsonb, IN p_ip_address character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO audit_log (tenant_id, user_id, action, entity_type, entity_id, details, ip_address)
    VALUES (
        p_tenant_id, 
        CASE WHEN EXISTS (SELECT 1 FROM users WHERE id = p_user_id) THEN p_user_id ELSE NULL END, 
        p_action, p_entity_type, p_entity_id, p_details, p_ip_address
    );
END;
$$;


--
-- Name: sp_log_experience_audit_event(uuid, uuid, uuid, uuid, character varying, uuid, jsonb, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_log_experience_audit_event(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_platform_experience_id uuid, IN p_action character varying, IN p_performed_by_user_id uuid, IN p_details jsonb, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO experience_audit_events (
        id, tenant_id, organization_id, platform_experience_id, action, performed_by_user_id, details, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_platform_experience_id, p_action, p_performed_by_user_id, p_details, p_created_at
    );
END;
$$;


--
-- Name: sp_mark_application_enrolled(uuid, uuid, uuid, uuid, numeric, numeric, numeric, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_mark_application_enrolled(IN p_tenant_id uuid, IN p_application_id uuid, IN p_child_id uuid, IN p_classroom_id uuid, IN p_base_rate numeric, IN p_deposit_held numeric, IN p_discount_percent numeric, IN p_enrollment_log text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE enrollment_pipeline_applications
    SET pipeline_stage = 'Enrolled',
        child_id = p_child_id,
        target_classroom_id = p_classroom_id,
        tuition_monthly_rate = p_base_rate,
        deposit_amount = p_deposit_held,
        sibling_discount_percent = p_discount_percent,
        internal_notes = COALESCE(internal_notes || E'\n', '') || p_enrollment_log,
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_application_id;
END;
$$;


--
-- Name: sp_mark_parent_communications_as_read(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_mark_parent_communications_as_read(IN p_tenant_id uuid, IN p_child_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE parent_communications
    SET delivery_status = 'Read', updated_at = NOW()
    WHERE tenant_id = p_tenant_id
      AND child_id = p_child_id
      AND teacher_id = '00000000-0000-0000-0000-000000000000'
      AND delivery_status != 'Read';
END;
$$;


--
-- Name: sp_provision_platform_organization(uuid, character varying, character varying, character varying, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_provision_platform_organization(IN p_org_id uuid, IN p_name character varying, IN p_location character varying, IN p_admin_email character varying, IN p_branch_count integer DEFAULT 1)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_raw_slug VARCHAR;
    v_slug VARCHAR;
    v_slug_count INT;
    v_existing_user_id UUID;
    v_existing_tenant_id UUID;
    v_admin_user_id UUID;
    v_derived_name VARCHAR;
    v_branches_to_create INT;
    v_i INT;
    v_branch_id UUID;
    v_branch_name VARCHAR;
    v_branch_loc VARCHAR;
    v_role_id UUID;
BEGIN
    v_raw_slug := trim(both '-' from regexp_replace(lower(p_name), '[^a-z0-9]+', '-', 'g'));
    IF v_raw_slug IS NULL OR v_raw_slug = '' THEN
        v_slug := 'org-' || substring(p_org_id::text, 1, 8);
    ELSE
        v_slug := v_raw_slug;
    END IF;

    SELECT COUNT(1) INTO v_slug_count FROM tenants WHERE slug = v_slug;
    IF v_slug_count > 0 THEN
        v_slug := v_slug || '-' || substring(p_org_id::text, 1, 4);
    END IF;

    INSERT INTO tenants (id, name, slug) 
    VALUES (p_org_id, p_name, v_slug) 
    ON CONFLICT (id) DO NOTHING;

    SELECT id, tenant_id INTO v_existing_user_id, v_existing_tenant_id 
    FROM users 
    WHERE LOWER(email) = LOWER(trim(p_admin_email)) OR normalized_email = UPPER(trim(p_admin_email)) 
    LIMIT 1;

    IF v_existing_user_id IS NOT NULL THEN
        v_admin_user_id := v_existing_user_id;
        IF v_existing_tenant_id = '10000000-0000-0000-0000-000000000001'::uuid THEN
            UPDATE users SET tenant_id = p_org_id WHERE id = v_admin_user_id;
        END IF;
    ELSE
        v_admin_user_id := gen_random_uuid();
        v_derived_name := initcap(split_part(split_part(trim(p_admin_email), '@', 1), '.', 1));
        IF v_derived_name IS NULL OR v_derived_name = '' THEN
            v_derived_name := 'Daycare Director';
        END IF;

        INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
        VALUES (v_admin_user_id, p_org_id, trim(p_admin_email), UPPER(trim(p_admin_email)), TRUE, 'Admin', v_derived_name, v_derived_name, TRUE)
        ON CONFLICT (normalized_email) DO NOTHING;
    END IF;

    INSERT INTO organizations (id, tenant_id, name, location, is_active)
    VALUES (p_org_id, p_org_id, p_name, p_location, TRUE)
    ON CONFLICT (id) DO NOTHING;

    v_branches_to_create := GREATEST(1, p_branch_count);
    FOR v_i IN 1..v_branches_to_create LOOP
        v_branch_id := gen_random_uuid();
        IF v_i = 1 THEN
            v_branch_name := 'Main Campus';
        ELSIF v_i = 2 THEN
            v_branch_name := 'Annex Campus';
        ELSE
            v_branch_name := 'Campus ' || v_i;
        END IF;

        IF p_location IS NOT NULL AND trim(p_location) <> '' THEN
            v_branch_loc := p_location;
        ELSE
            v_branch_loc := 'Main Branch';
        END IF;

        INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
        VALUES (v_branch_id, p_org_id, p_org_id, v_branch_name, v_branch_loc, TRUE)
        ON CONFLICT (id) DO NOTHING;
    END LOOP;

    IF v_admin_user_id IS NOT NULL THEN
        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
        VALUES (v_admin_user_id, p_org_id, p_org_id, 'Active')
        ON CONFLICT (user_id, organization_id) DO NOTHING;

        SELECT id INTO v_role_id 
        FROM roles 
        WHERE name IN ('OrganizationAdmin', 'OrganizationOwner') 
          AND (tenant_id = p_org_id OR tenant_id IS NULL) 
        ORDER BY tenant_id NULLS LAST 
        LIMIT 1;

        IF v_role_id IS NOT NULL THEN
            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
            VALUES (v_admin_user_id, v_role_id, p_org_id, p_org_id)
            ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;
        END IF;
    END IF;
END;
$$;


--
-- Name: sp_recalculate_invoice_balance(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_recalculate_invoice_balance(IN p_invoice_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_total_paid NUMERIC;
BEGIN
    -- 1. Calculate sum of valid non-reversed completed payments
    SELECT COALESCE(SUM(amount), 0)
    INTO v_total_paid
    FROM payments
    WHERE invoice_id = p_invoice_id
      AND status = 'Completed'
      AND is_reversed = FALSE;

    -- 2. Update invoice amount_paid, paid_at, and status
    UPDATE invoices
    SET amount_paid = v_total_paid,
        paid_at = CASE 
            WHEN (total_amount - v_total_paid) <= 0 THEN COALESCE(paid_at, NOW())
            ELSE NULL 
        END,
        status = CASE
            WHEN status = 'Void' THEN 'Void'
            WHEN status = 'Draft' THEN 'Draft'
            WHEN (total_amount - v_total_paid) <= 0 THEN 'Paid'
            WHEN v_total_paid > 0 THEN 'PartiallyPaid'
            ELSE 'Unpaid'
        END,
        updated_at = NOW()
    WHERE id = p_invoice_id;
END;
$$;


--
-- Name: sp_record_activity_recommendation_decision(uuid, uuid, uuid, uuid, uuid, character varying, text, uuid, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_record_activity_recommendation_decision(IN p_id uuid, IN p_request_id uuid, IN p_recommendation_item_id uuid, IN p_tenant_id uuid, IN p_decided_by_user_id uuid, IN p_decision character varying, IN p_rejection_reason text, IN p_resulting_schedule_id uuid, IN p_decided_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO activity_recommendation_decisions (
        id, request_id, recommendation_item_id, tenant_id, decided_by_user_id,
        decision, rejection_reason, resulting_schedule_id, decided_at
    ) VALUES (
        p_id, p_request_id, p_recommendation_item_id, p_tenant_id, p_decided_by_user_id,
        p_decision, p_rejection_reason, p_resulting_schedule_id, p_decided_at
    );
END;
$$;


--
-- Name: sp_record_child_attendance(uuid, uuid, uuid, uuid, uuid, date, character varying, time without time zone, time without time zone, character varying, uuid, character varying, character varying, character varying, character varying, uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_record_child_attendance(IN p_id uuid, IN p_tenant_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_attendance_date date, IN p_status character varying, IN p_arrival_time time without time zone, IN p_departure_time time without time zone, IN p_notes character varying, IN p_recorded_by_user_id uuid, IN p_pickup_person_name character varying, IN p_pickup_person_relationship character varying, IN p_pickup_verification_method character varying, IN p_pickup_notes character varying, IN p_authorized_pickup_id uuid, IN p_departure_verified_by_user_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_attendance (
        id, tenant_id, branch_id, classroom_id, child_id, attendance_date, status, 
        arrival_time, departure_time, notes, recorded_by_user_id,
        pickup_person_name, pickup_person_relationship, pickup_verification_method, pickup_notes, authorized_pickup_id, departure_verified_by_user_id,
        created_at, updated_at
    )
    VALUES (
        p_id, p_tenant_id, p_branch_id, p_classroom_id, p_child_id, p_attendance_date, p_status, 
        p_arrival_time, p_departure_time, p_notes, p_recorded_by_user_id,
        p_pickup_person_name, p_pickup_person_relationship, p_pickup_verification_method, p_pickup_notes, p_authorized_pickup_id, p_departure_verified_by_user_id,
        CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT (id) DO UPDATE SET 
        status = EXCLUDED.status, 
        arrival_time = EXCLUDED.arrival_time, 
        departure_time = EXCLUDED.departure_time, 
        notes = EXCLUDED.notes,
        pickup_person_name = EXCLUDED.pickup_person_name,
        pickup_person_relationship = EXCLUDED.pickup_person_relationship,
        pickup_verification_method = EXCLUDED.pickup_verification_method,
        pickup_notes = EXCLUDED.pickup_notes,
        authorized_pickup_id = EXCLUDED.authorized_pickup_id,
        departure_verified_by_user_id = EXCLUDED.departure_verified_by_user_id,
        updated_at = CURRENT_TIMESTAMP;
END;
$$;


--
-- Name: sp_record_inventory_ledger(uuid, uuid, uuid, character varying, integer, integer, integer, integer, character varying, uuid, text, uuid, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_record_inventory_ledger(IN p_id uuid, IN p_material_product_id uuid, IN p_location_id uuid, IN p_transaction_type character varying, IN p_quantity_delta integer, IN p_available_delta integer, IN p_reserved_delta integer, IN p_on_loan_delta integer, IN p_reference_type character varying, IN p_reference_id uuid, IN p_notes text, IN p_performed_by_user_id uuid, IN p_performed_by_email character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO inventory_ledger (
        id, material_product_id, location_id, transaction_type, quantity_delta,
        available_delta, reserved_delta, on_loan_delta, reference_type, reference_id,
        notes, performed_by_user_id, performed_by_email, created_at
    ) VALUES (
        p_id, p_material_product_id, p_location_id, p_transaction_type, p_quantity_delta,
        p_available_delta, p_reserved_delta, p_on_loan_delta, p_reference_type, p_reference_id,
        p_notes, p_performed_by_user_id, p_performed_by_email, p_created_at
    );
END;
$$;


--
-- Name: sp_record_material_return_inspection(uuid, uuid, uuid, uuid, integer, timestamp with time zone, uuid, character varying, character varying, integer, integer, integer, integer, text, character varying, numeric, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_record_material_return_inspection(IN p_id uuid, IN p_loan_id uuid, IN p_order_id uuid, IN p_material_product_id uuid, IN p_returned_quantity integer, IN p_inspected_at timestamp with time zone, IN p_inspected_by_user_id uuid, IN p_inspected_by_email character varying, IN p_condition character varying, IN p_quantity_approved_for_stock integer, IN p_quantity_requiring_cleaning integer, IN p_quantity_damaged integer, IN p_quantity_missing integer, IN p_inspection_notes text, IN p_deposit_disposition character varying, IN p_deposit_deduction_amount numeric, IN p_resolution_status character varying, IN p_created_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO material_return_inspections (
        id, loan_id, order_id, material_product_id, returned_quantity,
        inspected_at, inspected_by_user_id, inspected_by_email, condition,
        quantity_approved_for_stock, quantity_requiring_cleaning,
        quantity_damaged, quantity_missing, inspection_notes,
        deposit_disposition, deposit_deduction_amount, resolution_status, created_at
    ) VALUES (
        p_id, p_loan_id, p_order_id, p_material_product_id, p_returned_quantity,
        p_inspected_at, p_inspected_by_user_id, p_inspected_by_email, p_condition,
        p_quantity_approved_for_stock, p_quantity_requiring_cleaning,
        p_quantity_damaged, p_quantity_missing, p_inspection_notes,
        p_deposit_disposition, p_deposit_deduction_amount, p_resolution_status, p_created_at
    );
END;
$$;


--
-- Name: sp_remove_event_classroom(uuid, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_remove_event_classroom(IN p_event_id uuid, IN p_classroom_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM event_classrooms WHERE event_id = p_event_id AND classroom_id = p_classroom_id;
END;
$$;


--
-- Name: sp_remove_event_experience(uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_remove_event_experience(IN p_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM event_experiences WHERE id = p_id;
END;
$$;


--
-- Name: sp_reverse_payment(uuid, character varying, text, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_reverse_payment(IN p_payment_id uuid, IN p_reversed_by character varying, IN p_reason text, IN p_reversal_reference character varying, IN p_reversed_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE payments
    SET is_reversed = TRUE,
        status = 'Reversed',
        reversed_at = p_reversed_at,
        reversed_by = p_reversed_by,
        reversal_reason = p_reason,
        reversal_reference = p_reversal_reference
    WHERE id = p_payment_id;
END;
$$;


--
-- Name: sp_save_child_care_plan(uuid, uuid, uuid, uuid, uuid, integer, text, text, text, text, text, text, text, text, date, date, character varying, uuid, uuid, timestamp with time zone, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_save_child_care_plan(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_child_id uuid, IN p_version integer, IN p_diaper_routine_guidance text, IN p_feeding_instructions text, IN p_bottle_schedule_guidance text, IN p_sleep_routine_guidance text, IN p_toileting_support_plan text, IN p_comfort_calming_preferences text, IN p_required_supplies_note text, IN p_dietary_allergies_summary text, IN p_effective_date date, IN p_review_date date, IN p_status character varying, IN p_created_by_user_id uuid, IN p_approved_by_user_id uuid, IN p_approved_at timestamp with time zone, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_care_plans (
        id, tenant_id, organization_id, branch_id, child_id, version,
        diaper_routine_guidance, feeding_instructions, bottle_schedule_guidance,
        sleep_routine_guidance, toileting_support_plan, comfort_calming_preferences,
        required_supplies_note, dietary_allergies_summary, effective_date, review_date,
        status, created_by_user_id, approved_by_user_id, approved_at, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_version,
        p_diaper_routine_guidance, p_feeding_instructions, p_bottle_schedule_guidance,
        p_sleep_routine_guidance, p_toileting_support_plan, p_comfort_calming_preferences,
        p_required_supplies_note, p_dietary_allergies_summary, p_effective_date, p_review_date,
        p_status, p_created_by_user_id, p_approved_by_user_id, p_approved_at, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_save_child_care_supply_status(uuid, uuid, uuid, uuid, uuid, uuid, character varying, character varying, text, character varying, uuid, character varying, boolean, timestamp with time zone, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_save_child_care_supply_status(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_supply_type character varying, IN p_status character varying, IN p_notes text, IN p_threshold_note character varying, IN p_last_reported_by_user_id uuid, IN p_last_reported_by_name character varying, IN p_parent_reminder_sent boolean, IN p_last_reminder_at timestamp with time zone, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_care_supplies (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        supply_type, status, notes, threshold_note, last_reported_by_user_id,
        last_reported_by_name, parent_reminder_sent, last_reminder_at, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_supply_type, p_status, p_notes, p_threshold_note, p_last_reported_by_user_id,
        p_last_reported_by_name, p_parent_reminder_sent, p_last_reminder_at, p_created_at, p_updated_at
    )
    ON CONFLICT (id) DO UPDATE
    SET status = EXCLUDED.status,
        notes = EXCLUDED.notes,
        threshold_note = EXCLUDED.threshold_note,
        last_reported_by_user_id = EXCLUDED.last_reported_by_user_id,
        last_reported_by_name = EXCLUDED.last_reported_by_name,
        parent_reminder_sent = EXCLUDED.parent_reminder_sent,
        last_reminder_at = EXCLUDED.last_reminder_at,
        updated_at = EXCLUDED.updated_at;
END;
$$;


--
-- Name: sp_save_organization_intelligence_insight(uuid, uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, text, jsonb, text, text, character varying, text, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.sp_save_organization_intelligence_insight(p_id uuid, p_tenant_id uuid, p_organization_id uuid, p_branch_id uuid, p_classroom_id uuid, p_scope_type character varying, p_reporting_period character varying, p_category character varying, p_title character varying, p_observation text, p_supporting_metrics_json jsonb, p_possible_explanation text, p_suggested_next_step text, p_evidence_strength character varying, p_data_limitations text, p_source_type character varying, p_is_ai_generated boolean, p_created_at timestamp with time zone, p_updated_at timestamp with time zone) RETURNS SETOF public.organization_intelligence_insights
    LANGUAGE sql
    AS $$
    INSERT INTO organization_intelligence_insights (
        id, tenant_id, organization_id, branch_id, classroom_id,
        scope_type, reporting_period, category, title, observation,
        supporting_metrics_json, possible_explanation, suggested_next_step,
        evidence_strength, data_limitations, source_type, is_ai_generated,
        created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        p_scope_type, p_reporting_period, p_category, p_title, p_observation,
        COALESCE(p_supporting_metrics_json, '{}'::jsonb), p_possible_explanation, p_suggested_next_step,
        p_evidence_strength, p_data_limitations, p_source_type, p_is_ai_generated,
        p_created_at, p_updated_at
    )
    RETURNING *;
$$;


--
-- Name: sp_set_tenant_encrypted_ai_api_key(uuid, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_set_tenant_encrypted_ai_api_key(IN p_tenant_id uuid, IN p_encrypted_key character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE tenants SET encrypted_ai_api_key = p_encrypted_key WHERE id = p_tenant_id;
END;
$$;


--
-- Name: sp_start_child_nap_session(uuid, uuid, uuid, uuid, uuid, uuid, timestamp with time zone, text, uuid, character varying, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_start_child_nap_session(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_classroom_id uuid, IN p_child_id uuid, IN p_start_time timestamp with time zone, IN p_notes text, IN p_started_by_user_id uuid, IN p_started_by_name character varying, IN p_parent_visible boolean, IN p_created_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO child_nap_sessions (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        start_time, status, notes, started_by_user_id, started_by_name,
        parent_visible, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_start_time, 'Active', p_notes, p_started_by_user_id, p_started_by_name,
        p_parent_visible, p_created_at, p_updated_at
    );
END;
$$;


--
-- Name: sp_sync_parent_guardian(uuid, uuid, character varying, character varying, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_sync_parent_guardian(IN p_tenant_id uuid, IN p_child_id uuid, IN p_parent_email character varying, IN p_parent_name character varying, IN p_relationship character varying, IN p_phone character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_norm_email VARCHAR;
    v_trimmed_email VARCHAR;
    v_user_id UUID;
    v_role_id UUID;
    v_person_id UUID;
    v_first_name VARCHAR;
    v_last_name VARCHAR;
    v_rel VARCHAR;
    v_name_parts TEXT[];
BEGIN
    IF p_parent_email IS NULL OR TRIM(p_parent_email) = '' THEN
        RETURN;
    END IF;

    v_trimmed_email := TRIM(p_parent_email);
    v_norm_email := UPPER(v_trimmed_email);
    v_rel := COALESCE(NULLIF(TRIM(p_relationship), ''), 'Parent');

    -- Split name into first and last
    v_name_parts := regexp_split_to_array(COALESCE(NULLIF(TRIM(p_parent_name), ''), 'Parent Guardian'), '\s+');
    v_first_name := v_name_parts[1];
    IF array_length(v_name_parts, 1) > 1 THEN
        v_last_name := array_to_string(v_name_parts[2:], ' ');
    ELSE
        v_last_name := 'Guardian';
    END IF;

    -- 1. Find or create User
    SELECT id INTO v_user_id FROM users WHERE normalized_email = v_norm_email LIMIT 1;
    IF v_user_id IS NULL THEN
        v_user_id := gen_random_uuid();
        INSERT INTO users (
            id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, password_hash, security_stamp, is_active, created_at, updated_at
        ) VALUES (
            v_user_id, p_tenant_id, v_trimmed_email, v_norm_email, TRUE, v_first_name, v_last_name,
            COALESCE(NULLIF(TRIM(p_parent_name), ''), v_first_name || ' ' || v_last_name),
            '', md5(random()::text), TRUE, NOW(), NOW()
        ) ON CONFLICT (normalized_email) DO NOTHING;

        SELECT id INTO v_user_id FROM users WHERE normalized_email = v_norm_email LIMIT 1;

        SELECT id INTO v_role_id FROM roles WHERE name = 'Parent' AND (tenant_id = p_tenant_id OR tenant_id IS NULL) LIMIT 1;
        IF v_role_id IS NOT NULL THEN
            INSERT INTO user_roles (user_id, role_id) VALUES (v_user_id, v_role_id) ON CONFLICT DO NOTHING;
        END IF;
    END IF;

    -- 2. Find or create Person
    SELECT id INTO v_person_id FROM persons WHERE tenant_id = p_tenant_id AND user_id = v_user_id LIMIT 1;
    IF v_person_id IS NULL THEN
        v_person_id := gen_random_uuid();
        INSERT INTO persons (
            id, tenant_id, user_id, first_name, last_name, relationship, email, mobile_phone, has_portal_access, receives_daily_updates, receives_photos, created_at, updated_at
        ) VALUES (
            v_person_id, p_tenant_id, v_user_id, v_first_name, v_last_name, v_rel, v_trimmed_email, NULLIF(TRIM(p_phone), ''), TRUE, TRUE, TRUE, NOW(), NOW()
        );
    ELSE
        IF p_phone IS NOT NULL AND TRIM(p_phone) <> '' THEN
            UPDATE persons SET mobile_phone = TRIM(p_phone), updated_at = NOW() WHERE id = v_person_id;
        END IF;
    END IF;

    -- 3. Link child_guardians
    INSERT INTO child_guardians (
        id, tenant_id, child_id, person_id, relationship, is_primary, has_legal_custody, can_pickup, can_receive_information, can_authorize_medical_care
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_person_id, v_rel, TRUE, TRUE, TRUE, TRUE, TRUE
    ) ON CONFLICT (child_id, person_id) DO UPDATE SET 
        relationship = EXCLUDED.relationship, 
        can_pickup = TRUE;

    -- 4. Link family_relationships
    INSERT INTO family_relationships (
        id, tenant_id, child_id, user_id, relationship, is_primary, created_at, status, verified_at
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_user_id, v_rel, TRUE, NOW(), 'Verified', NOW()
    ) ON CONFLICT (child_id, user_id) DO UPDATE SET 
        relationship = EXCLUDED.relationship,
        status = 'Verified',
        revoked_at = NULL,
        verified_at = NOW();

    -- 5. Link authorized_pickups
    INSERT INTO authorized_pickups (
        id, tenant_id, child_id, person_id, relationship, is_active, notes
    ) VALUES (
        gen_random_uuid(), p_tenant_id, p_child_id, v_person_id, v_rel, TRUE, 'Primary guardian on file.'
    ) ON CONFLICT DO NOTHING;
END;
$$;


--
-- Name: sp_update_branch(uuid, uuid, character varying, character varying, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_branch(IN p_id uuid, IN p_tenant_id uuid, IN p_name character varying, IN p_location character varying, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
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


--
-- Name: sp_update_branch_floor_map(uuid, uuid, character varying, character varying, character varying, text, character varying, character varying, bigint, character varying, integer, character varying, timestamp with time zone, character varying, timestamp with time zone, uuid, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_branch_floor_map(IN p_id uuid, IN p_tenant_id uuid, IN p_name character varying, IN p_building_area character varying, IN p_floor_level character varying, IN p_description text, IN p_file_storage_key character varying, IN p_file_type character varying, IN p_file_size bigint, IN p_content_hash character varying, IN p_version integer, IN p_status character varying, IN p_review_due_at timestamp with time zone, IN p_published_by character varying, IN p_published_at timestamp with time zone, IN p_supersedes_map_id uuid, IN p_emergency_instructions text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE branch_floor_maps SET
        name = p_name,
        building_area = p_building_area,
        floor_level = p_floor_level,
        description = p_description,
        file_storage_key = p_file_storage_key,
        file_type = p_file_type,
        file_size = p_file_size,
        content_hash = p_content_hash,
        version = p_version,
        status = p_status,
        review_due_at = p_review_due_at,
        published_by = p_published_by,
        published_at = p_published_at,
        supersedes_map_id = p_supersedes_map_id,
        row_version = row_version + 1,
        emergency_instructions = p_emergency_instructions
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;


--
-- Name: sp_update_branch_operational_goal_status(uuid, uuid, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_branch_operational_goal_status(IN p_tenant_id uuid, IN p_goal_id uuid, IN p_status character varying, IN p_current_value character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE branch_operational_goals
    SET status = p_status,
        current_value = COALESCE(p_current_value, current_value),
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_goal_id;
END;
$$;


--
-- Name: sp_update_child(uuid, uuid, character varying, character varying, date, uuid, text, integer, character varying, text, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_child(IN p_id uuid, IN p_tenant_id uuid, IN p_first_name character varying, IN p_last_name character varying, IN p_date_of_birth date, IN p_classroom_id uuid, IN p_photo_url text, IN p_profile_completion_percentage integer, IN p_enrollment_status character varying, IN p_draft_data text, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE children
    SET first_name = p_first_name,
        last_name = p_last_name,
        date_of_birth = p_date_of_birth,
        classroom_id = p_classroom_id,
        photo_url = p_photo_url,
        profile_completion_percentage = p_profile_completion_percentage,
        enrollment_status = p_enrollment_status,
        draft_data = p_draft_data,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_child(uuid, uuid, character varying, character varying, date, uuid, character varying, integer, character varying, jsonb, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_child(IN p_id uuid, IN p_tenant_id uuid, IN p_first_name character varying, IN p_last_name character varying, IN p_date_of_birth date, IN p_classroom_id uuid, IN p_photo_url character varying, IN p_profile_completion_percentage integer, IN p_enrollment_status character varying, IN p_draft_data jsonb, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE children
    SET first_name = p_first_name,
        last_name = p_last_name,
        date_of_birth = p_date_of_birth,
        classroom_id = p_classroom_id,
        photo_url = p_photo_url,
        profile_completion_percentage = p_profile_completion_percentage,
        enrollment_status = p_enrollment_status,
        draft_data = p_draft_data,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_child_care_event(uuid, uuid, timestamp with time zone, character varying, character varying, character varying, character varying, character varying, numeric, character varying, character varying, character varying, character varying, text, boolean, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_child_care_event(IN p_id uuid, IN p_tenant_id uuid, IN p_event_time timestamp with time zone, IN p_diaper_condition character varying, IN p_skin_observation character varying, IN p_meal_type character varying, IN p_food_offered character varying, IN p_amount_consumed character varying, IN p_fluid_oz numeric, IN p_rest_status character varying, IN p_toileting_activity character varying, IN p_hygiene_type character varying, IN p_comfort_mood character varying, IN p_notes text, IN p_parent_visible boolean, IN p_correction_reason text, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_care_events
    SET event_time = p_event_time,
        diaper_condition = p_diaper_condition,
        skin_observation = p_skin_observation,
        meal_type = p_meal_type,
        food_offered = p_food_offered,
        amount_consumed = p_amount_consumed,
        fluid_oz = p_fluid_oz,
        rest_status = p_rest_status,
        toileting_activity = p_toileting_activity,
        hygiene_type = p_hygiene_type,
        comfort_mood = p_comfort_mood,
        notes = p_notes,
        parent_visible = p_parent_visible,
        correction_reason = p_correction_reason,
        is_corrected = TRUE,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_child_consent_document(uuid, uuid, character varying, uuid, timestamp with time zone, text, timestamp with time zone, timestamp with time zone, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_child_consent_document(IN p_id uuid, IN p_tenant_id uuid, IN p_status character varying, IN p_admin_reviewed_by uuid, IN p_admin_reviewed_at timestamp with time zone, IN p_admin_review_notes text, IN p_approved_at timestamp with time zone, IN p_sent_at timestamp with time zone, IN p_signed_at timestamp with time zone, IN p_declined_at timestamp with time zone, IN p_withdrawn_at timestamp with time zone, IN p_signed_document_storage_key character varying, IN p_signed_document_checksum character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_consent_documents
    SET status = p_status,
        admin_reviewed_by = p_admin_reviewed_by,
        admin_reviewed_at = p_admin_reviewed_at,
        admin_review_notes = p_admin_review_notes,
        approved_at = p_approved_at,
        sent_at = p_sent_at,
        signed_at = p_signed_at,
        declined_at = p_declined_at,
        withdrawn_at = p_withdrawn_at,
        signed_document_storage_key = p_signed_document_storage_key,
        signed_document_checksum = p_signed_document_checksum,
        row_version = row_version + 1
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_child_story_status(uuid, uuid, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_child_story_status(IN p_id uuid, IN p_tenant_id uuid, IN p_status character varying, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_stories
    SET status = p_status, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_classroom(uuid, uuid, character varying, character varying, character varying, character varying, text, character varying, character varying, character varying, integer, integer, integer, integer, character varying, text, jsonb, integer, boolean, timestamp with time zone, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_classroom(IN p_id uuid, IN p_tenant_id uuid, IN p_name character varying, IN p_display_name character varying, IN p_short_name character varying, IN p_code character varying, IN p_description text, IN p_classroom_type character varying, IN p_status character varying, IN p_age_group character varying, IN p_min_age_months integer, IN p_max_age_months integer, IN p_capacity integer, IN p_current_enrollment integer, IN p_environment character varying, IN p_photo_url text, IN p_draft_data jsonb, IN p_profile_completion_percentage integer, IN p_is_active boolean, IN p_updated_at timestamp with time zone, IN p_updated_by uuid)
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


--
-- Name: sp_update_consent_signer(uuid, uuid, character varying, character varying, timestamp with time zone, integer, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, text, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_consent_signer(IN p_id uuid, IN p_tenant_id uuid, IN p_signing_status character varying, IN p_token_hash character varying, IN p_token_expires_at timestamp with time zone, IN p_invitation_sent_count integer, IN p_last_invitation_sent_at timestamp with time zone, IN p_last_opened_at timestamp with time zone, IN p_signed_at timestamp with time zone, IN p_signature_legal_name character varying, IN p_signature_intent_text text, IN p_signature_ip_address character varying, IN p_signature_user_agent text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE consent_signers
    SET signing_status = p_signing_status,
        token_hash = p_token_hash,
        token_expires_at = p_token_expires_at,
        invitation_sent_count = p_invitation_sent_count,
        last_invitation_sent_at = p_last_invitation_sent_at,
        last_opened_at = p_last_opened_at,
        signed_at = p_signed_at,
        signature_legal_name = p_signature_legal_name,
        signature_intent_text = p_signature_intent_text,
        signature_ip_address = p_signature_ip_address,
        signature_user_agent = p_signature_user_agent
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_emergency_contact(uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, text, text, character varying, character varying, timestamp with time zone, timestamp with time zone, boolean, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_emergency_contact(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_branch_id uuid, IN p_scope character varying, IN p_category character varying, IN p_name character varying, IN p_phone_number character varying, IN p_alternate_phone_number character varying, IN p_country_region character varying, IN p_address text, IN p_instructions text, IN p_verification_status character varying, IN p_verified_by character varying, IN p_verified_at timestamp with time zone, IN p_review_due_at timestamp with time zone, IN p_is_active boolean, IN p_display_order integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE emergency_contacts SET
        organization_id = p_organization_id,
        branch_id = p_branch_id,
        scope = p_scope,
        category = p_category,
        name = p_name,
        phone_number = p_phone_number,
        alternate_phone_number = p_alternate_phone_number,
        country_region = p_country_region,
        address = p_address,
        instructions = p_instructions,
        verification_status = p_verification_status,
        verified_by = p_verified_by,
        verified_at = p_verified_at,
        review_due_at = p_review_due_at,
        is_active = p_is_active,
        display_order = p_display_order,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;


--
-- Name: sp_update_enrollment_application_stage(uuid, uuid, character varying, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_enrollment_application_stage(IN p_tenant_id uuid, IN p_application_id uuid, IN p_new_stage character varying, IN p_notes text, IN p_tour_date timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE enrollment_pipeline_applications
    SET pipeline_stage = p_new_stage,
        tour_date = COALESCE(p_tour_date, tour_date),
        internal_notes = CASE WHEN p_notes IS NOT NULL THEN p_notes ELSE internal_notes END,
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_application_id;
END;
$$;


--
-- Name: sp_update_event(uuid, uuid, character varying, text, uuid, date, time without time zone, time without time zone, character varying, uuid, character varying, character varying, uuid, boolean, text[], timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_event(IN p_id uuid, IN p_tenant_id uuid, IN p_title character varying, IN p_description text, IN p_event_type_id uuid, IN p_event_date date, IN p_start_time time without time zone, IN p_end_time time without time zone, IN p_location character varying, IN p_organizer_id uuid, IN p_status character varying, IN p_scope character varying, IN p_branch_id uuid, IN p_is_organization_wide boolean, IN p_photo_urls text[], IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE events 
    SET title = p_title, description = p_description, event_type_id = p_event_type_id, 
        event_date = p_event_date, start_time = p_start_time, end_time = p_end_time, 
        location = p_location, organizer_id = p_organizer_id, status = p_status, 
        scope = p_scope, branch_id = p_branch_id, is_organization_wide = p_is_organization_wide, 
        photo_urls = p_photo_urls, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_event_photos(uuid, uuid, text[], timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_event_photos(IN p_id uuid, IN p_tenant_id uuid, IN p_photo_urls text[], IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE events 
    SET photo_urls = p_photo_urls, updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_event_rsvp(uuid, character varying, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_event_rsvp(IN p_id uuid, IN p_status character varying, IN p_notes text, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE event_rsvps 
    SET status = p_status, notes = p_notes, updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;


--
-- Name: sp_update_event_task(uuid, character varying, uuid, timestamp with time zone, boolean, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_event_task(IN p_id uuid, IN p_description character varying, IN p_owner_id uuid, IN p_due_date timestamp with time zone, IN p_is_completed boolean, IN p_completed_at timestamp with time zone, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE event_tasks 
    SET description = p_description, owner_id = p_owner_id, due_date = p_due_date, 
        is_completed = p_is_completed, completed_at = p_completed_at, updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;


--
-- Name: sp_update_experience(uuid, uuid, character varying, text, character varying, jsonb, timestamp with time zone, uuid, text, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_experience(IN p_id uuid, IN p_tenant_id uuid, IN p_title character varying, IN p_description text, IN p_status character varying, IN p_dna_payload jsonb, IN p_updated_at timestamp with time zone, IN p_version_id uuid, IN p_change_reason text, IN p_modified_by_user_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_next_version INT;
BEGIN
    SELECT COALESCE(MAX(version_number), 0) + 1 INTO v_next_version
    FROM experience_versions
    WHERE experience_id = p_id;

    UPDATE experiences
    SET title = p_title,
        description = p_description,
        status = p_status,
        dna_payload = p_dna_payload,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;

    INSERT INTO experience_versions (id, experience_id, version_number, title, description, dna_payload, change_reason, modified_by_user_id, created_at)
    VALUES (p_version_id, p_id, v_next_version, p_title, p_description, p_dna_payload, p_change_reason, p_modified_by_user_id, p_updated_at);
END;
$$;


--
-- Name: sp_update_experience_feedback(uuid, uuid, integer, character varying, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_experience_feedback(IN p_id uuid, IN p_tenant_id uuid, IN p_stars integer, IN p_rating character varying, IN p_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experience_feedback
    SET stars = p_stars,
        rating = p_rating,
        notes = p_notes
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_experience_harvest_request(uuid, character varying, text, uuid, timestamp with time zone, boolean, boolean, boolean, boolean, boolean, boolean); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_experience_harvest_request(IN p_id uuid, IN p_status character varying, IN p_review_notes text, IN p_reviewed_by_user_id uuid, IN p_reviewed_at timestamp with time zone, IN p_safety_suitability_verified boolean, IN p_learning_objectives_verified boolean, IN p_instructions_verified boolean, IN p_materials_verified boolean, IN p_media_rights_verified boolean, IN p_commercial_rights_verified boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experience_harvest_requests SET
        status = p_status,
        review_notes = p_review_notes,
        reviewed_by_user_id = p_reviewed_by_user_id,
        reviewed_at = p_reviewed_at,
        safety_suitability_verified = p_safety_suitability_verified,
        learning_objectives_verified = p_learning_objectives_verified,
        instructions_verified = p_instructions_verified,
        materials_verified = p_materials_verified,
        media_rights_verified = p_media_rights_verified,
        commercial_rights_verified = p_commercial_rights_verified
    WHERE id = p_id;
END;
$$;


--
-- Name: sp_update_experience_schedule(uuid, uuid, date, character varying, character varying, uuid, time without time zone, time without time zone, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_experience_schedule(IN p_id uuid, IN p_tenant_id uuid, IN p_scheduled_date date, IN p_time_of_day character varying, IN p_status character varying, IN p_primary_teacher_id uuid, IN p_planned_start_time time without time zone, IN p_planned_end_time time without time zone, IN p_preparation_notes text, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experience_schedules
    SET scheduled_date = p_scheduled_date,
        time_of_day = p_time_of_day,
        status = p_status,
        primary_teacher_id = p_primary_teacher_id,
        planned_start_time = p_planned_start_time,
        planned_end_time = p_planned_end_time,
        preparation_notes = p_preparation_notes,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_experience_session(uuid, uuid, character varying, text, time without time zone, time without time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_experience_session(IN p_id uuid, IN p_tenant_id uuid, IN p_status character varying, IN p_actual_notes text, IN p_start_time time without time zone, IN p_end_time time without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE experience_sessions SET
        status = p_status,
        actual_notes = p_actual_notes,
        start_time = p_start_time,
        end_time = p_end_time,
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_inventory_balance_atomic(uuid, uuid, integer, integer, integer, integer, integer, integer, integer, integer, integer, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_inventory_balance_atomic(IN p_product_id uuid, IN p_location_id uuid, IN p_total_delta integer, IN p_available_delta integer, IN p_reserved_delta integer, IN p_dispatched_delta integer, IN p_on_loan_delta integer, IN p_under_inspection_delta integer, IN p_under_cleaning_delta integer, IN p_damaged_delta integer, IN p_missing_delta integer, IN p_retired_delta integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_rows INT;
BEGIN
    UPDATE inventory_balances SET
        total_stock = total_stock + p_total_delta,
        available_stock = GREATEST(0, available_stock + p_available_delta),
        reserved_stock = GREATEST(0, reserved_stock + p_reserved_delta),
        dispatched_stock = GREATEST(0, dispatched_stock + p_dispatched_delta),
        on_loan_stock = GREATEST(0, on_loan_stock + p_on_loan_delta),
        under_inspection_stock = GREATEST(0, under_inspection_stock + p_under_inspection_delta),
        under_cleaning_stock = GREATEST(0, under_cleaning_stock + p_under_cleaning_delta),
        damaged_stock = GREATEST(0, damaged_stock + p_damaged_delta),
        missing_stock = GREATEST(0, missing_stock + p_missing_delta),
        retired_stock = GREATEST(0, retired_stock + p_retired_delta),
        updated_at = NOW()
    WHERE material_product_id = p_product_id AND location_id = p_location_id;

    GET DIAGNOSTICS v_rows = ROW_COUNT;

    IF v_rows = 0 THEN
        INSERT INTO inventory_balances (
            id, material_product_id, location_id, total_stock, available_stock,
            reserved_stock, dispatched_stock, on_loan_stock, under_inspection_stock,
            under_cleaning_stock, damaged_stock, missing_stock, retired_stock, updated_at
        ) VALUES (
            gen_random_uuid(), p_product_id, p_location_id,
            GREATEST(0, p_total_delta), GREATEST(0, p_available_delta),
            GREATEST(0, p_reserved_delta), GREATEST(0, p_dispatched_delta),
            GREATEST(0, p_on_loan_delta), GREATEST(0, p_under_inspection_delta),
            GREATEST(0, p_under_cleaning_delta), GREATEST(0, p_damaged_delta),
            GREATEST(0, p_missing_delta), GREATEST(0, p_retired_delta), NOW()
        )
        ON CONFLICT (material_product_id, location_id) DO UPDATE SET
            total_stock = inventory_balances.total_stock + p_total_delta,
            available_stock = GREATEST(0, inventory_balances.available_stock + p_available_delta),
            reserved_stock = GREATEST(0, inventory_balances.reserved_stock + p_reserved_delta),
            dispatched_stock = GREATEST(0, inventory_balances.dispatched_stock + p_dispatched_delta),
            on_loan_stock = GREATEST(0, inventory_balances.on_loan_stock + p_on_loan_delta),
            under_inspection_stock = GREATEST(0, inventory_balances.under_inspection_stock + p_under_inspection_delta),
            under_cleaning_stock = GREATEST(0, inventory_balances.under_cleaning_stock + p_under_cleaning_delta),
            damaged_stock = GREATEST(0, inventory_balances.damaged_stock + p_damaged_delta),
            missing_stock = GREATEST(0, inventory_balances.missing_stock + p_missing_delta),
            retired_stock = inventory_balances.retired_stock + p_retired_delta,
            updated_at = NOW();
    END IF;
END;
$$;


--
-- Name: sp_update_invoice(uuid, uuid, numeric, numeric, numeric, numeric, numeric, character varying, timestamp with time zone, timestamp with time zone, timestamp with time zone, timestamp with time zone, character varying, timestamp with time zone, character varying, text, text, text, text, character varying, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_invoice(IN p_id uuid, IN p_tenant_id uuid, IN p_subtotal numeric, IN p_discount_amount numeric, IN p_tax_amount numeric, IN p_total_amount numeric, IN p_amount_paid numeric, IN p_status character varying, IN p_issue_date timestamp with time zone, IN p_due_date timestamp with time zone, IN p_paid_at timestamp with time zone, IN p_published_at timestamp with time zone, IN p_published_by character varying, IN p_voided_at timestamp with time zone, IN p_voided_by character varying, IN p_void_reason text, IN p_description text, IN p_notes text, IN p_payment_instructions text, IN p_billing_contact_email character varying, IN p_document_storage_key character varying, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE invoices SET
        subtotal = p_subtotal,
        discount_amount = p_discount_amount,
        tax_amount = p_tax_amount,
        total_amount = p_total_amount,
        amount_paid = p_amount_paid,
        status = p_status,
        issue_date = p_issue_date,
        due_date = p_due_date,
        paid_at = p_paid_at,
        published_at = p_published_at,
        published_by = p_published_by,
        voided_at = p_voided_at,
        voided_by = p_voided_by,
        void_reason = p_void_reason,
        description = p_description,
        notes = p_notes,
        payment_instructions = p_payment_instructions,
        billing_contact_email = p_billing_contact_email,
        document_storage_key = p_document_storage_key,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_journey_entry(uuid, uuid, character varying, character varying, text, text, text, timestamp with time zone, character varying, boolean, boolean, character varying, timestamp with time zone, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_journey_entry(IN p_id uuid, IN p_tenant_id uuid, IN p_journey_type character varying, IN p_title character varying, IN p_observation text, IN p_child_voice text, IN p_teacher_reflection text, IN p_observation_date timestamp with time zone, IN p_participation_type character varying, IN p_is_milestone boolean, IN p_is_parent_visible boolean, IN p_parent_visibility_status character varying, IN p_updated_at timestamp with time zone, IN p_updated_by uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE child_journey_entries SET
        journey_type = p_journey_type,
        title = p_title,
        observation = p_observation,
        child_voice = p_child_voice,
        teacher_reflection = p_teacher_reflection,
        observation_date = p_observation_date,
        participation_type = p_participation_type,
        is_milestone = p_is_milestone,
        is_parent_visible = p_is_parent_visible,
        parent_visibility_status = p_parent_visibility_status,
        updated_at = p_updated_at,
        updated_by = p_updated_by,
        version = version + 1
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_material_loan_status(uuid, character varying, timestamp with time zone, text, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_loan_status(IN p_loan_id uuid, IN p_return_status character varying, IN p_actual_return_date timestamp with time zone DEFAULT NULL::timestamp with time zone, IN p_notes text DEFAULT NULL::text, IN p_deposit_status character varying DEFAULT NULL::character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_loans SET
        return_status = p_return_status,
        actual_return_date = COALESCE(p_actual_return_date, actual_return_date),
        notes = COALESCE(p_notes, notes),
        deposit_status = COALESCE(p_deposit_status, deposit_status),
        updated_at = NOW()
    WHERE id = p_loan_id;
END;
$$;


--
-- Name: sp_update_material_offering(uuid, boolean, numeric, boolean, numeric, numeric, integer, integer, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_offering(IN p_material_product_id uuid, IN p_allow_purchase boolean, IN p_purchase_price numeric, IN p_allow_borrow boolean, IN p_rental_fee numeric, IN p_refundable_deposit numeric, IN p_default_loan_duration_days integer, IN p_max_loan_duration_days integer, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_offerings SET
        allow_purchase = p_allow_purchase,
        purchase_price = p_purchase_price,
        allow_borrow = p_allow_borrow,
        rental_fee = p_rental_fee,
        refundable_deposit = p_refundable_deposit,
        default_loan_duration_days = p_default_loan_duration_days,
        max_loan_duration_days = p_max_loan_duration_days,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE material_product_id = p_material_product_id;
END;
$$;


--
-- Name: sp_update_material_order_quotation(uuid, numeric, numeric, numeric, numeric, numeric, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_order_quotation(IN p_order_id uuid, IN p_total_purchase_amount numeric, IN p_total_rental_fee numeric, IN p_total_deposit numeric, IN p_shipping_fee numeric, IN p_total_amount numeric, IN p_quotation_notes text, IN p_valid_until timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_orders SET
        total_purchase_amount = p_total_purchase_amount,
        total_rental_fee = p_total_rental_fee,
        total_deposit = p_total_deposit,
        shipping_fee = p_shipping_fee,
        total_amount = p_total_amount,
        quotation_notes = p_quotation_notes,
        quotation_valid_until = p_valid_until,
        workflow_status = 'QuotationOffered',
        updated_at = NOW()
    WHERE id = p_order_id;
END;
$$;


--
-- Name: sp_update_material_order_status(uuid, character varying, character varying, character varying, text, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_order_status(IN p_order_id uuid, IN p_new_status character varying, IN p_payment_status character varying DEFAULT NULL::character varying, IN p_fulfilment_status character varying DEFAULT NULL::character varying, IN p_rejection_reason text DEFAULT NULL::text, IN p_cancellation_reason text DEFAULT NULL::text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_orders SET
        workflow_status = p_new_status,
        payment_status = COALESCE(p_payment_status, payment_status),
        fulfilment_status = COALESCE(p_fulfilment_status, fulfilment_status),
        rejection_reason = COALESCE(p_rejection_reason, rejection_reason),
        cancellation_reason = COALESCE(p_cancellation_reason, cancellation_reason),
        updated_at = NOW()
    WHERE id = p_order_id;
END;
$$;


--
-- Name: sp_update_material_product(uuid, character varying, text, character varying, character varying, character varying, text, integer, integer, text, text, text, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_product(IN p_id uuid, IN p_name character varying, IN p_description text, IN p_category character varying, IN p_image_url character varying, IN p_material_type character varying, IN p_safety_instructions text, IN p_age_min_months integer, IN p_age_max_months integer, IN p_usage_restrictions text, IN p_cleaning_instructions text, IN p_return_policy text, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_products SET
        name = p_name,
        description = p_description,
        category = p_category,
        image_url = p_image_url,
        material_type = p_material_type,
        safety_instructions = p_safety_instructions,
        age_min_months = p_age_min_months,
        age_max_months = p_age_max_months,
        usage_restrictions = p_usage_restrictions,
        cleaning_instructions = p_cleaning_instructions,
        return_policy = p_return_policy,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;


--
-- Name: sp_update_material_recurring_schedule_status(uuid, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_recurring_schedule_status(IN p_schedule_id uuid, IN p_status character varying, IN p_next_run_date timestamp with time zone DEFAULT NULL::timestamp with time zone, IN p_last_run_date timestamp with time zone DEFAULT NULL::timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_recurring_schedules SET
        status = p_status,
        next_run_date = COALESCE(p_next_run_date, next_run_date),
        last_run_date = COALESCE(p_last_run_date, last_run_date),
        updated_at = NOW()
    WHERE id = p_schedule_id;
END;
$$;


--
-- Name: sp_update_material_shipment_status(uuid, character varying, timestamp with time zone, text); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_material_shipment_status(IN p_shipment_id uuid, IN p_delivery_status character varying, IN p_actual_delivery_date timestamp with time zone, IN p_proof_notes text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE material_shipments SET
        delivery_status = p_delivery_status,
        actual_delivery_date = COALESCE(p_actual_delivery_date, actual_delivery_date),
        proof_of_delivery_notes = COALESCE(p_proof_notes, proof_of_delivery_notes),
        updated_at = NOW()
    WHERE id = p_shipment_id;
END;
$$;


--
-- Name: sp_update_organization(uuid, uuid, character varying, character varying, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_organization(IN p_id uuid, IN p_tenant_id uuid, IN p_name character varying, IN p_location character varying, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
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


--
-- Name: sp_update_parent_communication(uuid, uuid, character varying, text, text, jsonb, boolean, character varying, character varying, character varying, uuid, timestamp with time zone, timestamp with time zone, character varying, character varying, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_parent_communication(IN p_id uuid, IN p_tenant_id uuid, IN p_title character varying, IN p_content_final text, IN p_family_conversation_starter text, IN p_evidence_summary_json jsonb, IN p_consent_verified boolean, IN p_consent_notes character varying, IN p_status character varying, IN p_teacher_name character varying, IN p_approved_by_user_id uuid, IN p_approved_at timestamp with time zone, IN p_sent_at timestamp with time zone, IN p_delivery_channel character varying, IN p_delivery_status character varying, IN p_delivery_error text, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE parent_communications
    SET title = p_title,
        content_final = p_content_final,
        family_conversation_starter = p_family_conversation_starter,
        evidence_summary_json = p_evidence_summary_json,
        consent_verified = p_consent_verified,
        consent_notes = p_consent_notes,
        status = p_status,
        teacher_name = p_teacher_name,
        approved_by_user_id = p_approved_by_user_id,
        approved_at = p_approved_at,
        sent_at = p_sent_at,
        delivery_channel = p_delivery_channel,
        delivery_status = p_delivery_status,
        delivery_error = p_delivery_error,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_parent_communication_delivery_status(uuid, uuid, character varying, text, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_parent_communication_delivery_status(IN p_tenant_id uuid, IN p_id uuid, IN p_status character varying, IN p_error text, IN p_sent_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE parent_communications
    SET delivery_status = p_status,
        delivery_error = p_error,
        sent_at = COALESCE(p_sent_at, sent_at),
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_person_contact(uuid, uuid, character varying, character varying, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_person_contact(IN p_tenant_id uuid, IN p_person_id uuid, IN p_mobile_phone character varying, IN p_preferred_contact_method character varying, IN p_preferred_language character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE persons
    SET mobile_phone = p_mobile_phone,
        preferred_contact_method = p_preferred_contact_method,
        preferred_language = p_preferred_language,
        updated_at = NOW()
    WHERE id = p_person_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_platform_experience(uuid, character varying, text, character varying, integer, integer, integer, text, text, text, character varying, numeric, character varying, character varying, character varying, integer, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_platform_experience(IN p_id uuid, IN p_title character varying, IN p_description text, IN p_category character varying, IN p_age_range_min_months integer, IN p_age_range_max_months integer, IN p_duration_minutes integer, IN p_instructions text, IN p_safety_notes text, IN p_dna_payload text, IN p_pricing_model character varying, IN p_price_amount numeric, IN p_currency character varying, IN p_publication_status character varying, IN p_ownership_type character varying, IN p_current_version integer, IN p_creator_attribution character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE platform_experiences SET
        title = p_title,
        description = p_description,
        category = p_category,
        age_range_min_months = p_age_range_min_months,
        age_range_max_months = p_age_range_max_months,
        duration_minutes = p_duration_minutes,
        instructions = p_instructions,
        safety_notes = p_safety_notes,
        dna_payload = p_dna_payload,
        pricing_model = p_pricing_model,
        price_amount = p_price_amount,
        currency = p_currency,
        publication_status = p_publication_status,
        ownership_type = p_ownership_type,
        current_version = p_current_version,
        creator_attribution = p_creator_attribution,
        updated_at = NOW()
    WHERE id = p_id;
END;
$$;


--
-- Name: sp_update_platform_organization_details(uuid, character varying, character varying, character varying, integer); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_platform_organization_details(IN p_id uuid, IN p_name character varying, IN p_location character varying, IN p_admin_email character varying, IN p_branch_count integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_tenant_id UUID;
    v_current_branches_count INT;
    v_i INT;
    v_branch_name VARCHAR;
    v_trimmed_email VARCHAR;
    v_derived_name VARCHAR;
    v_existing_user_id UUID;
    v_admin_user_id UUID;
    v_current_user_id UUID;
BEGIN
    UPDATE organizations 
    SET name = p_name, location = p_location, updated_at = NOW() 
    WHERE id = p_id;

    UPDATE tenants 
    SET name = p_name, updated_at = NOW() 
    WHERE id = p_id;

    SELECT tenant_id INTO v_tenant_id FROM organizations WHERE id = p_id;
    IF v_tenant_id IS NULL THEN
        v_tenant_id := p_id;
    END IF;

    SELECT COUNT(*) INTO v_current_branches_count FROM branches WHERE organization_id = p_id;
    IF v_current_branches_count = 0 THEN
        FOR v_i IN 1..GREATEST(1, p_branch_count) LOOP
            IF v_i = 1 THEN
                v_branch_name := 'Main Campus';
            ELSIF v_i = 2 THEN
                v_branch_name := 'Annex Campus';
            ELSE
                v_branch_name := 'Campus ' || v_i;
            END IF;
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (gen_random_uuid(), v_tenant_id, p_id, v_branch_name, COALESCE(p_location, 'Branch Location'), TRUE);
        END LOOP;
    ELSIF v_current_branches_count < p_branch_count THEN
        FOR v_i IN (v_current_branches_count + 1)..p_branch_count LOOP
            IF v_i = 2 THEN
                v_branch_name := 'Annex Campus';
            ELSE
                v_branch_name := 'Campus ' || v_i;
            END IF;
            INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
            VALUES (gen_random_uuid(), v_tenant_id, p_id, v_branch_name, COALESCE(p_location, 'Branch Location'), TRUE);
        END LOOP;
    END IF;

    IF p_admin_email IS NOT NULL AND trim(p_admin_email) <> '' THEN
        v_trimmed_email := trim(p_admin_email);
        v_derived_name := initcap(split_part(split_part(v_trimmed_email, '@', 1), '.', 1));
        IF v_derived_name IS NULL OR v_derived_name = '' THEN
            v_derived_name := 'Daycare Director';
        END IF;

        SELECT id INTO v_existing_user_id 
        FROM users 
        WHERE normalized_email = UPPER(v_trimmed_email) 
        LIMIT 1;

        IF v_existing_user_id IS NOT NULL THEN
            v_admin_user_id := v_existing_user_id;
            UPDATE users 
            SET is_active = TRUE,
                display_name = COALESCE(NULLIF(TRIM(display_name), ''), v_derived_name),
                updated_at = NOW() 
            WHERE id = v_admin_user_id;
        ELSE
            SELECT om.user_id INTO v_current_user_id
            FROM organization_memberships om
            JOIN users u ON u.id = om.user_id
            WHERE om.organization_id = p_id AND om.status = 'Active'
            ORDER BY om.created_at
            LIMIT 1;

            IF v_current_user_id IS NOT NULL THEN
                v_admin_user_id := v_current_user_id;
                UPDATE users 
                SET email = v_trimmed_email, 
                    normalized_email = UPPER(v_trimmed_email), 
                    display_name = v_derived_name,
                    updated_at = NOW() 
                WHERE id = v_admin_user_id;
            ELSE
                v_admin_user_id := gen_random_uuid();
                INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                VALUES (v_admin_user_id, v_tenant_id, v_trimmed_email, UPPER(v_trimmed_email), TRUE, 'Admin', v_derived_name, v_derived_name, TRUE)
                ON CONFLICT (normalized_email) DO UPDATE SET updated_at = NOW();
            END IF;
        END IF;

        DELETE FROM user_organization_roles 
        WHERE organization_id = p_id AND user_id != v_admin_user_id;
        
        DELETE FROM organization_memberships 
        WHERE organization_id = p_id AND user_id != v_admin_user_id;

        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
        VALUES (v_admin_user_id, p_id, v_tenant_id, 'Active')
        ON CONFLICT (user_id, organization_id) DO UPDATE SET status = 'Active', tenant_id = v_tenant_id;

        INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
        SELECT v_admin_user_id, r.id, p_id, v_tenant_id
        FROM roles r
        WHERE r.tenant_id = v_tenant_id AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
        ON CONFLICT (user_id, role_id, organization_id) DO NOTHING;
    END IF;
END;
$$;


--
-- Name: sp_update_teacher(uuid, uuid, uuid, uuid, character varying, character varying, character varying, character varying, character varying, character varying, text, date, character varying, character varying, text, character varying, text, character varying, jsonb, integer, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_teacher(IN p_id uuid, IN p_tenant_id uuid, IN p_person_id uuid, IN p_user_id uuid, IN p_employee_number character varying, IN p_first_name character varying, IN p_middle_name character varying, IN p_last_name character varying, IN p_preferred_name character varying, IN p_display_name character varying, IN p_profile_photo_url text, IN p_date_of_birth date, IN p_gender character varying, IN p_preferred_language character varying, IN p_other_languages text, IN p_pronouns character varying, IN p_bio text, IN p_status character varying, IN p_draft_data jsonb, IN p_profile_completion_percentage integer, IN p_updated_at timestamp with time zone)
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


--
-- Name: sp_update_user(uuid, uuid, character varying, character varying, character varying, character varying, character varying, boolean, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_user(IN p_id uuid, IN p_tenant_id uuid, IN p_email character varying, IN p_normalized_email character varying, IN p_first_name character varying, IN p_last_name character varying, IN p_display_name character varying, IN p_is_active boolean, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE users
    SET first_name = p_first_name,
        last_name = p_last_name,
        display_name = p_display_name,
        email = p_email,
        normalized_email = p_normalized_email,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_weekly_planning_draft_item(uuid, uuid, date, character varying, character varying, uuid, character varying, text, character varying, integer, text, text, character varying, text, uuid); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_weekly_planning_draft_item(IN p_id uuid, IN p_tenant_id uuid, IN p_scheduled_date date, IN p_day_of_week character varying, IN p_time_of_day character varying, IN p_experience_id uuid, IN p_experience_title character varying, IN p_learning_goal text, IN p_group_format character varying, IN p_estimated_duration_minutes integer, IN p_required_materials text, IN p_rationale text, IN p_validation_status character varying, IN p_validation_notes text, IN p_resulting_schedule_id uuid)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE weekly_planning_draft_items SET
        scheduled_date = p_scheduled_date,
        day_of_week = p_day_of_week,
        time_of_day = p_time_of_day,
        experience_id = p_experience_id,
        experience_title = p_experience_title,
        learning_goal = p_learning_goal,
        group_format = p_group_format,
        estimated_duration_minutes = p_estimated_duration_minutes,
        required_materials = p_required_materials,
        rationale = p_rationale,
        validation_status = p_validation_status,
        validation_notes = p_validation_notes,
        resulting_schedule_id = p_resulting_schedule_id
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_update_weekly_planning_draft_status(uuid, uuid, character varying); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_update_weekly_planning_draft_status(IN p_tenant_id uuid, IN p_draft_id uuid, IN p_status character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE weekly_planning_draft_requests SET
        status = p_status,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_draft_id AND tenant_id = p_tenant_id;
END;
$$;


--
-- Name: sp_upsert_organization_billing_settings(uuid, uuid, uuid, character varying, character varying, timestamp with time zone, timestamp with time zone, boolean, text, character varying, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_upsert_organization_billing_settings(IN p_id uuid, IN p_tenant_id uuid, IN p_organization_id uuid, IN p_mode character varying, IN p_frequency character varying, IN p_effective_date timestamp with time zone, IN p_next_billing_date timestamp with time zone, IN p_auto_charge_enabled boolean, IN p_notes text, IN p_updated_by character varying, IN p_updated_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO organization_billing_settings (
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_mode, p_frequency, p_effective_date,
        p_next_billing_date, p_auto_charge_enabled, p_notes, p_updated_by, p_updated_at
    )
    ON CONFLICT (organization_id) DO UPDATE SET
        mode = EXCLUDED.mode,
        frequency = EXCLUDED.frequency,
        effective_date = EXCLUDED.effective_date,
        next_billing_date = EXCLUDED.next_billing_date,
        auto_charge_enabled = EXCLUDED.auto_charge_enabled,
        notes = EXCLUDED.notes,
        updated_by = EXCLUDED.updated_by,
        updated_at = EXCLUDED.updated_at;
END;
$$;


--
-- Name: sp_verify_emergency_contact(uuid, uuid, character varying, timestamp with time zone, timestamp with time zone); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.sp_verify_emergency_contact(IN p_tenant_id uuid, IN p_id uuid, IN p_verified_by character varying, IN p_verified_at timestamp with time zone, IN p_review_due_at timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE emergency_contacts SET
        verification_status = 'Verified',
        verified_by = p_verified_by,
        verified_at = p_verified_at,
        review_due_at = p_review_due_at,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;


--
-- Name: trg_tenants_ensure_roles(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.trg_tenants_ensure_roles() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM ensure_tenant_roles(NEW.id);
    RETURN NEW;
END
$$;


--
-- Name: _migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public._migrations (
    id integer NOT NULL,
    filename character varying(500) NOT NULL,
    applied_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: _migrations_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public._migrations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: _migrations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public._migrations_id_seq OWNED BY public._migrations.id;


--
-- Name: activity_recommendation_decisions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_decisions (
    id uuid NOT NULL,
    request_id uuid NOT NULL,
    recommendation_item_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    decided_by_user_id uuid NOT NULL,
    decision character varying(50) NOT NULL,
    rejection_reason text,
    resulting_schedule_id uuid,
    decided_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: activity_recommendation_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_items (
    id uuid NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_title character varying(255) NOT NULL,
    suggested_format character varying(100),
    estimated_duration_minutes integer,
    rationale text NOT NULL,
    supporting_evidence jsonb,
    materials jsonb,
    rank_order integer DEFAULT 1 NOT NULL,
    is_evidence_limited boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: activity_recommendation_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.activity_recommendation_requests (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid,
    requested_by_user_id uuid NOT NULL,
    request_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    scope character varying(50) NOT NULL,
    context_preferences jsonb,
    evidence_count integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: addresses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.addresses (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    address_type character varying(50),
    address_line1 character varying(255) NOT NULL,
    address_line2 character varying(255),
    city character varying(100) NOT NULL,
    province character varying(100) NOT NULL,
    postal_code character varying(50) NOT NULL,
    country character varying(100) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: audit_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_log (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    user_id uuid,
    action character varying(200) NOT NULL,
    entity_type character varying(100),
    entity_id uuid,
    details jsonb,
    ip_address character varying(45),
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: authorized_pickups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.authorized_pickups (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    person_id uuid,
    relationship character varying(100),
    pickup_code character varying(50),
    photo_url text,
    valid_from timestamp with time zone,
    valid_to timestamp with time zone,
    is_active boolean DEFAULT true NOT NULL,
    notes text
);


--
-- Name: branch_floor_maps; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.branch_floor_maps (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    name character varying(255) NOT NULL,
    building_area character varying(255),
    floor_level character varying(100) NOT NULL,
    description text,
    file_storage_key character varying(500) NOT NULL,
    file_type character varying(50) NOT NULL,
    file_size bigint NOT NULL,
    content_hash character varying(128) NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    review_due_at timestamp with time zone,
    created_by character varying(255) NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    published_by character varying(255),
    published_at timestamp with time zone,
    supersedes_map_id uuid,
    row_version integer DEFAULT 1 NOT NULL,
    emergency_instructions text
);


--
-- Name: branch_operational_goals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.branch_operational_goals (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    branch_id uuid,
    title character varying(200) NOT NULL,
    category character varying(50) DEFAULT 'Enrollment'::character varying NOT NULL,
    target_metric character varying(100),
    current_value character varying(50),
    target_value character varying(50),
    status character varying(50) DEFAULT 'InProgress'::character varying NOT NULL,
    target_date date,
    owner_name character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: branches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.branches (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    location character varying(300),
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_allergies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_allergies (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    allergen character varying(200) NOT NULL,
    reaction character varying(200),
    severity character varying(50) NOT NULL,
    symptoms text,
    treatment text,
    emergency_action text,
    has_epi_pen boolean DEFAULT false NOT NULL,
    notes text
);


--
-- Name: child_care_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    event_type character varying(50) NOT NULL,
    event_time timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    diaper_condition character varying(50),
    skin_observation character varying(200),
    meal_type character varying(50),
    food_offered character varying(250),
    amount_consumed character varying(50),
    fluid_oz numeric(4,1),
    rest_status character varying(50),
    toileting_activity character varying(50),
    hygiene_type character varying(50),
    comfort_mood character varying(50),
    notes text,
    parent_visible boolean DEFAULT true NOT NULL,
    recorded_by_user_id uuid NOT NULL,
    recorded_by_name character varying(150),
    correction_reason text,
    is_corrected boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_care_plans; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_plans (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    child_id uuid NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    diaper_routine_guidance text,
    feeding_instructions text,
    bottle_schedule_guidance text,
    sleep_routine_guidance text,
    toileting_support_plan text,
    comfort_calming_preferences text,
    required_supplies_note text,
    dietary_allergies_summary text,
    effective_date date DEFAULT CURRENT_DATE NOT NULL,
    review_date date,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    created_by_user_id uuid NOT NULL,
    approved_by_user_id uuid,
    approved_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_care_supplies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_care_supplies (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    supply_type character varying(50) NOT NULL,
    status character varying(50) DEFAULT 'Adequate'::character varying NOT NULL,
    notes text,
    threshold_note character varying(150),
    last_reported_by_user_id uuid NOT NULL,
    last_reported_by_name character varying(150),
    parent_reminder_sent boolean DEFAULT false NOT NULL,
    last_reminder_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_communication_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_communication_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    primary_language character varying(100),
    secondary_language character varying(100),
    communication_method character varying(200),
    speech_notes text,
    communication_support text,
    interpreter_required boolean DEFAULT false NOT NULL
);


--
-- Name: child_consent_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_consent_documents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    branch_id uuid,
    classroom_id uuid,
    child_id uuid NOT NULL,
    consent_template_id uuid NOT NULL,
    template_version character varying(50) NOT NULL,
    document_version integer DEFAULT 1 NOT NULL,
    document_title character varying(255) NOT NULL,
    document_snapshot text NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    signing_deadline timestamp with time zone,
    created_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    admin_reviewed_by uuid,
    admin_reviewed_at timestamp with time zone,
    admin_review_notes text,
    approved_at timestamp with time zone,
    sent_at timestamp with time zone,
    signed_at timestamp with time zone,
    declined_at timestamp with time zone,
    withdrawn_at timestamp with time zone,
    signed_document_storage_key character varying(500),
    signed_document_checksum character varying(128),
    supersedes_document_id uuid,
    row_version integer DEFAULT 1 NOT NULL
);


--
-- Name: child_consents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_consents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    consent_type character varying(100) NOT NULL,
    granted boolean DEFAULT false NOT NULL,
    granted_by character varying(255),
    granted_at timestamp with time zone,
    expires_at timestamp with time zone,
    document_id uuid,
    version character varying(50)
);


--
-- Name: child_dietary_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_dietary_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    diet_type character varying(100),
    food_restrictions text,
    religious_restrictions text,
    texture_restrictions text,
    feeding_notes text,
    self_feeds boolean DEFAULT true NOT NULL,
    special_instructions text
);


--
-- Name: child_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_documents (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    document_type character varying(100) NOT NULL,
    file_name character varying(255) NOT NULL,
    storage_url character varying(1024) NOT NULL,
    uploaded_by uuid,
    uploaded_at timestamp with time zone DEFAULT now() NOT NULL,
    expiration_date date,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL
);


--
-- Name: child_enrollment_histories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_enrollment_histories (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    classroom_id uuid,
    start_date date NOT NULL,
    end_date date,
    reason text
);


--
-- Name: child_feeding_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_feeding_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    feeding_method character varying(100),
    bottle_type character varying(100),
    bottle_amount character varying(100),
    bottle_schedule character varying(200),
    solid_food_stage character varying(100),
    feeding_assistance character varying(100),
    choking_risk text,
    feeding_instructions text
);


--
-- Name: child_food_restrictions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_food_restrictions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    food character varying(200) NOT NULL,
    restriction_type character varying(100) NOT NULL,
    severity character varying(50),
    notes text
);


--
-- Name: child_guardians; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_guardians (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    person_id uuid NOT NULL,
    relationship character varying(100) NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    has_legal_custody boolean DEFAULT false NOT NULL,
    can_pickup boolean DEFAULT true NOT NULL,
    can_receive_information boolean DEFAULT true NOT NULL,
    can_authorize_medical_care boolean DEFAULT false NOT NULL
);


--
-- Name: child_interests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_interests (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    name character varying(255) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_journey_developments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_developments (
    journey_entry_id uuid NOT NULL,
    development_area_id uuid NOT NULL
);


--
-- Name: child_journey_evidence; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_evidence (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    journey_entry_id uuid NOT NULL,
    evidence_type character varying(100) NOT NULL,
    media_id uuid,
    text_content text,
    captured_at timestamp with time zone,
    captured_by uuid,
    visibility character varying(50) DEFAULT 'InternalOnly'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_journey_insight_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_insight_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    category character varying(50) NOT NULL,
    classification character varying(50) DEFAULT 'RecordedFact'::character varying NOT NULL,
    statement text NOT NULL,
    evidence_strength character varying(50) DEFAULT 'Supported'::character varying NOT NULL,
    supporting_evidence jsonb,
    suggested_experience_id uuid,
    suggested_experience_title character varying(255),
    rank_order integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_journey_insight_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_journey_insight_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    child_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    period_type character varying(50) DEFAULT 'Last30Days'::character varying NOT NULL,
    period_start_date date NOT NULL,
    period_end_date date NOT NULL,
    evidence_count integer DEFAULT 0 NOT NULL,
    overall_summary text,
    evidence_gaps text,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    focus_area character varying(100),
    teacher_question text
);


--
-- Name: child_legal_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_legal_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    has_custody_restrictions boolean DEFAULT false NOT NULL,
    legal_notes text,
    court_order_document_id uuid
);


--
-- Name: child_medical_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_medical_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    primary_physician character varying(200),
    physician_phone character varying(50),
    clinic_name character varying(200),
    clinic_phone character varying(50),
    health_card_number character varying(100),
    blood_type character varying(20),
    medical_notes text,
    special_medical_needs text,
    medical_consent boolean DEFAULT false NOT NULL
);


--
-- Name: child_medications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_medications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    medication_name character varying(200) NOT NULL,
    dosage character varying(100),
    route character varying(100),
    frequency character varying(100),
    start_date date,
    end_date date,
    prescribed_by character varying(200),
    instructions text,
    parent_authorization boolean DEFAULT false NOT NULL,
    medication_authorization_document_id uuid,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL
);


--
-- Name: child_nap_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_nap_sessions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone,
    duration_minutes integer,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    outcome character varying(50),
    notes text,
    started_by_user_id uuid NOT NULL,
    started_by_name character varying(150),
    ended_by_user_id uuid,
    ended_by_name character varying(150),
    parent_visible boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: child_person_restrictions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_person_restrictions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    person_id uuid NOT NULL,
    restriction_type character varying(100) NOT NULL,
    effective_from timestamp with time zone,
    effective_to timestamp with time zone,
    notes text
);


--
-- Name: child_relationships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_relationships (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    related_child_id uuid NOT NULL,
    relationship_type character varying(100) NOT NULL,
    notes text
);


--
-- Name: child_routines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_routines (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    morning_routine text,
    comfort_strategies text,
    favorite_activities text,
    favorite_toys text,
    favorite_books text,
    calming_strategies text,
    communication_preferences text,
    transition_strategies text,
    other_notes text
);


--
-- Name: child_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    day_of_week integer NOT NULL,
    expected_arrival character varying(50),
    expected_departure character varying(50)
);


--
-- Name: child_sleep_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_sleep_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    typical_nap_start character varying(50),
    typical_nap_duration character varying(50),
    comfort_item character varying(200),
    sleep_routine text,
    sleep_instructions text,
    other_notes text
);


--
-- Name: child_stories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_stories (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    experience_schedule_id uuid,
    content text NOT NULL,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: child_support_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_support_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    support_required boolean DEFAULT false NOT NULL,
    support_description text,
    learning_preferences text,
    accessibility_needs text,
    communication_support text,
    sensory_considerations text,
    behavior_support_plan text,
    external_support_provider character varying(255),
    notes text
);


--
-- Name: child_toileting_profiles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_toileting_profiles (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    toilet_training_status character varying(100),
    diaper_required boolean DEFAULT false NOT NULL,
    diaper_size character varying(50),
    changing_instructions text,
    bathroom_assistance character varying(200),
    accident_notes text
);


--
-- Name: child_transportations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.child_transportations (
    child_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    transportation_required boolean DEFAULT false NOT NULL,
    pickup_location character varying(255),
    dropoff_location character varying(255),
    route character varying(100),
    vehicle character varying(100),
    authorized_adult character varying(255),
    special_instructions text
);


--
-- Name: children; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.children (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    date_of_birth date,
    classroom_id uuid,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    photo_url text,
    profile_completion_percentage integer DEFAULT 0,
    enrollment_status character varying(50) DEFAULT 'Draft'::character varying,
    draft_data text
);


--
-- Name: classroom_statutory_ratios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classroom_statutory_ratios (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    age_group character varying(50) NOT NULL,
    max_children_per_educator integer NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: classroom_teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classroom_teachers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    user_id uuid NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: classrooms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.classrooms (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    age_group character varying(100),
    capacity integer,
    environment character varying(50) DEFAULT 'Indoor'::character varying,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    display_name character varying(255),
    short_name character varying(50),
    code character varying(50),
    description text,
    classroom_type character varying(100),
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    min_age_months integer,
    max_age_months integer,
    current_enrollment integer DEFAULT 0,
    photo_url text,
    draft_data jsonb,
    profile_completion_percentage integer DEFAULT 0,
    created_by uuid,
    updated_by uuid
);


--
-- Name: consent_audit_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_audit_events (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    consent_document_id uuid NOT NULL,
    actor_type character varying(50) NOT NULL,
    actor_user_id uuid,
    actor_name character varying(255),
    event_type character varying(100) NOT NULL,
    timestamp_utc timestamp with time zone DEFAULT now() NOT NULL,
    correlation_id character varying(100),
    safe_metadata text
);


--
-- Name: consent_choices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_choices (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    consent_document_id uuid NOT NULL,
    consent_signer_id uuid,
    section_key character varying(100) NOT NULL,
    section_title character varying(255) NOT NULL,
    is_mandatory boolean DEFAULT false NOT NULL,
    choice character varying(50) NOT NULL,
    recorded_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: consent_signers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_signers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    consent_document_id uuid NOT NULL,
    family_relationship_id uuid,
    person_id uuid,
    intended_signer_user_id uuid,
    signer_name_snapshot character varying(255) NOT NULL,
    signer_email_snapshot character varying(255) NOT NULL,
    signer_relationship character varying(100) NOT NULL,
    is_required boolean DEFAULT true NOT NULL,
    signing_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    token_hash character varying(128),
    token_expires_at timestamp with time zone,
    invitation_sent_count integer DEFAULT 0 NOT NULL,
    last_invitation_sent_at timestamp with time zone,
    last_opened_at timestamp with time zone,
    signed_at timestamp with time zone,
    signature_legal_name character varying(255),
    signature_intent_text text,
    signature_ip_address character varying(100),
    signature_user_agent text
);


--
-- Name: consent_templates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.consent_templates (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid,
    name character varying(255) NOT NULL,
    description text,
    document_type character varying(100) NOT NULL,
    version character varying(50) DEFAULT '1.0'::character varying NOT NULL,
    template_content text NOT NULL,
    required_for_enrollment boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: development_areas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.development_areas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    name character varying(255) NOT NULL,
    description text,
    icon character varying(100),
    display_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL
);


--
-- Name: emergency_contacts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.emergency_contacts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid,
    first_name character varying(100),
    last_name character varying(100),
    relationship character varying(100),
    phone character varying(50),
    alternate_phone character varying(50),
    email character varying(255),
    address character varying(500),
    priority integer DEFAULT 1 NOT NULL,
    is_authorized_pickup boolean DEFAULT false NOT NULL,
    notes text,
    organization_id uuid,
    branch_id uuid,
    scope character varying(50) DEFAULT 'Branch'::character varying,
    category character varying(100) DEFAULT 'EmergencyServices'::character varying,
    name character varying(255),
    phone_number character varying(100),
    alternate_phone_number character varying(100),
    country_region character varying(100),
    instructions text,
    verification_status character varying(50) DEFAULT 'Unverified'::character varying,
    verified_by character varying(255),
    verified_at timestamp with time zone,
    review_due_at timestamp with time zone,
    is_active boolean DEFAULT true,
    display_order integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: enrollment_pipeline_applications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.enrollment_pipeline_applications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid,
    child_first_name character varying(100) NOT NULL,
    child_last_name character varying(100) NOT NULL,
    date_of_birth date NOT NULL,
    gender character varying(20),
    parent_guardian_name character varying(150) NOT NULL,
    parent_email character varying(150) NOT NULL,
    parent_phone character varying(50) NOT NULL,
    target_classroom_id uuid,
    desired_start_date date,
    schedule_type character varying(50) DEFAULT 'FullTime'::character varying NOT NULL,
    pipeline_stage character varying(50) DEFAULT 'Enquiry'::character varying NOT NULL,
    tour_date timestamp with time zone,
    tuition_monthly_rate numeric(10,2) DEFAULT 850.00 NOT NULL,
    deposit_amount numeric(10,2) DEFAULT 200.00 NOT NULL,
    sibling_discount_percent numeric(5,2) DEFAULT 0,
    registration_fee numeric(10,2) DEFAULT 50.00,
    internal_notes text,
    child_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: enrollments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.enrollments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    daycare_id uuid,
    branch_id uuid,
    classroom_id uuid,
    enrollment_date date,
    start_date date,
    expected_end_date date,
    program character varying(100),
    enrollment_status character varying(50) NOT NULL,
    schedule_type character varying(100),
    days_per_week integer,
    arrival_time character varying(50),
    departure_time character varying(50),
    assigned_teacher_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: event_classrooms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_classrooms (
    event_id uuid NOT NULL,
    classroom_id uuid NOT NULL
);


--
-- Name: event_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_schedule_id uuid,
    experience_session_id uuid
);


--
-- Name: event_rsvps; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_rsvps (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    child_id uuid NOT NULL,
    guardian_id uuid NOT NULL,
    status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: event_tasks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_tasks (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    event_id uuid NOT NULL,
    description text NOT NULL,
    owner_id uuid,
    due_date timestamp with time zone,
    is_completed boolean DEFAULT false NOT NULL,
    completed_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: event_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.event_types (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_feedback; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_feedback (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    experience_schedule_id uuid,
    teacher_id uuid NOT NULL,
    rating character varying(50) NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    stars integer DEFAULT 5 NOT NULL,
    experience_id uuid
);


--
-- Name: experience_participations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_participations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_session_id uuid NOT NULL,
    child_id uuid NOT NULL,
    participation_status character varying(50) DEFAULT 'Participated'::character varying NOT NULL,
    participation_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    scheduled_date date NOT NULL,
    time_of_day character varying(50) NOT NULL,
    status character varying(50) DEFAULT 'Scheduled'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    primary_teacher_id uuid,
    planned_start_time time without time zone,
    planned_end_time time without time zone,
    preparation_notes text
);


--
-- Name: experience_session_teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_session_teachers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_session_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    role character varying(50) DEFAULT 'LeadTeacher'::character varying NOT NULL
);


--
-- Name: experience_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_sessions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    experience_id uuid NOT NULL,
    experience_schedule_id uuid,
    classroom_id uuid NOT NULL,
    session_date date NOT NULL,
    start_time time without time zone,
    end_time time without time zone,
    status character varying(50) DEFAULT 'Planned'::character varying NOT NULL,
    primary_teacher_id uuid,
    actual_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experience_versions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experience_versions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    experience_id uuid NOT NULL,
    version_number integer NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    dna_payload jsonb DEFAULT '{}'::jsonb NOT NULL,
    change_reason character varying(500),
    modified_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.experiences (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    description text,
    status character varying(50) DEFAULT 'Idea'::character varying NOT NULL,
    dna_payload jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_by_user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    organization_id uuid
);


--
-- Name: family_billing_contracts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.family_billing_contracts (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid,
    application_id uuid,
    child_id uuid NOT NULL,
    parent_name character varying(150) NOT NULL,
    parent_email character varying(150) NOT NULL,
    schedule_type character varying(50) DEFAULT 'FullTime'::character varying NOT NULL,
    billing_cycle character varying(50) DEFAULT 'Monthly'::character varying NOT NULL,
    base_rate numeric(10,2) NOT NULL,
    discount_percent numeric(5,2) DEFAULT 0,
    effective_rate numeric(10,2) NOT NULL,
    deposit_held numeric(10,2) DEFAULT 0,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    start_date date NOT NULL,
    end_date date,
    auto_invoice boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: family_relationships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.family_relationships (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    child_id uuid NOT NULL,
    user_id uuid NOT NULL,
    relationship character varying(50) NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    status character varying(20) DEFAULT 'Pending'::character varying NOT NULL,
    verified_at timestamp with time zone,
    verified_by uuid,
    revoked_at timestamp with time zone,
    revoked_by uuid,
    CONSTRAINT chk_family_relationship_status CHECK (((status)::text = ANY ((ARRAY['Pending'::character varying, 'Verified'::character varying, 'Revoked'::character varying])::text[])))
);


--
-- Name: floor_map_markers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.floor_map_markers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    floor_map_id uuid NOT NULL,
    marker_type character varying(100) NOT NULL,
    label character varying(255) NOT NULL,
    instructions text,
    normalized_x double precision NOT NULL,
    normalized_y double precision NOT NULL,
    display_order integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: inventory_balances; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_balances (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    location_id uuid NOT NULL,
    total_stock integer DEFAULT 0 NOT NULL,
    available_stock integer DEFAULT 0 NOT NULL,
    reserved_stock integer DEFAULT 0 NOT NULL,
    dispatched_stock integer DEFAULT 0 NOT NULL,
    on_loan_stock integer DEFAULT 0 NOT NULL,
    under_inspection_stock integer DEFAULT 0 NOT NULL,
    under_cleaning_stock integer DEFAULT 0 NOT NULL,
    damaged_stock integer DEFAULT 0 NOT NULL,
    missing_stock integer DEFAULT 0 NOT NULL,
    retired_stock integer DEFAULT 0 NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT chk_avail_nonneg CHECK ((available_stock >= 0)),
    CONSTRAINT chk_loan_nonneg CHECK ((on_loan_stock >= 0)),
    CONSTRAINT chk_reserv_nonneg CHECK ((reserved_stock >= 0))
);


--
-- Name: inventory_ledger; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_ledger (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    location_id uuid NOT NULL,
    transaction_type character varying(50) NOT NULL,
    quantity_delta integer DEFAULT 0 NOT NULL,
    available_delta integer DEFAULT 0 NOT NULL,
    reserved_delta integer DEFAULT 0 NOT NULL,
    on_loan_delta integer DEFAULT 0 NOT NULL,
    reference_type character varying(50),
    reference_id uuid,
    notes text,
    performed_by_user_id uuid,
    performed_by_email character varying(256),
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: inventory_locations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.inventory_locations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(150) NOT NULL,
    address text,
    contact_phone character varying(50),
    is_central_warehouse boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: invoice_line_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.invoice_line_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    invoice_id uuid NOT NULL,
    line_number integer DEFAULT 1 NOT NULL,
    description character varying(300) NOT NULL,
    quantity numeric(10,2) DEFAULT 1 NOT NULL,
    unit_price numeric(12,2) DEFAULT 0 NOT NULL,
    line_total numeric(12,2) DEFAULT 0 NOT NULL,
    category character varying(50) DEFAULT 'Subscription'::character varying,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: invoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.invoices (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    invoice_number character varying(50) NOT NULL,
    billing_period character varying(20),
    subtotal numeric(12,2) DEFAULT 0 NOT NULL,
    discount_amount numeric(12,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    total_amount numeric(12,2) DEFAULT 0 NOT NULL,
    amount_paid numeric(12,2) DEFAULT 0 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    status character varying(30) DEFAULT 'Draft'::character varying NOT NULL,
    issue_date timestamp with time zone DEFAULT now() NOT NULL,
    due_date timestamp with time zone DEFAULT (now() + '14 days'::interval) NOT NULL,
    paid_at timestamp with time zone,
    published_at timestamp with time zone,
    published_by character varying(200),
    voided_at timestamp with time zone,
    voided_by character varying(200),
    void_reason text,
    created_by character varying(200),
    description text,
    notes text,
    payment_instructions text,
    billing_contact_email character varying(256),
    document_storage_key character varying(200),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_loans; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_loans (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    order_item_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid,
    quantity_issued integer DEFAULT 1 NOT NULL,
    issued_at timestamp with time zone DEFAULT now() NOT NULL,
    due_date timestamp with time zone NOT NULL,
    actual_return_date timestamp with time zone,
    return_status character varying(50) DEFAULT 'Reserved'::character varying NOT NULL,
    deposit_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    deposit_status character varying(50) DEFAULT 'Held'::character varying NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_offerings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_offerings (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    material_product_id uuid NOT NULL,
    allow_purchase boolean DEFAULT true NOT NULL,
    purchase_price numeric(10,2) DEFAULT 0.00 NOT NULL,
    allow_borrow boolean DEFAULT false NOT NULL,
    rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    refundable_deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    default_loan_duration_days integer DEFAULT 14 NOT NULL,
    max_loan_duration_days integer DEFAULT 30 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_order_history; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_order_history (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    event_type character varying(100) NOT NULL,
    previous_status character varying(50),
    new_status character varying(50),
    actor_user_id uuid,
    actor_email character varying(256),
    actor_role character varying(50) DEFAULT 'DaycareOrganization'::character varying NOT NULL,
    description text NOT NULL,
    notes text,
    is_internal_note boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_order_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_order_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    offering_id uuid,
    fulfilment_mode character varying(50) DEFAULT 'Purchase'::character varying NOT NULL,
    quantity integer DEFAULT 1 NOT NULL,
    unit_price numeric(10,2) DEFAULT 0.00 NOT NULL,
    rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    line_total numeric(10,2) DEFAULT 0.00 NOT NULL,
    loan_duration_days integer DEFAULT 0 NOT NULL,
    return_due_date timestamp with time zone,
    status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_orders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_orders (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_number character varying(50) NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    planned_experience_id uuid,
    requesting_user_id uuid NOT NULL,
    requesting_user_email character varying(256),
    order_type character varying(50) DEFAULT 'StandardCatalog'::character varying NOT NULL,
    workflow_status character varying(50) DEFAULT 'RequestSubmitted'::character varying NOT NULL,
    payment_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    fulfilment_status character varying(50) DEFAULT 'Unfulfilled'::character varying NOT NULL,
    total_purchase_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_rental_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_deposit numeric(10,2) DEFAULT 0.00 NOT NULL,
    shipping_fee numeric(10,2) DEFAULT 0.00 NOT NULL,
    tax_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    total_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    quotation_notes text,
    quotation_valid_until timestamp with time zone,
    rejection_reason text,
    cancellation_reason text,
    delivery_address text NOT NULL,
    delivery_contact_name character varying(150),
    delivery_contact_phone character varying(50),
    required_delivery_date timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_products; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_products (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    sku character varying(64) NOT NULL,
    name character varying(200) NOT NULL,
    description text NOT NULL,
    category character varying(100) DEFAULT 'General'::character varying NOT NULL,
    image_url character varying(500),
    material_type character varying(50) DEFAULT 'Consumable'::character varying NOT NULL,
    safety_instructions text,
    age_min_months integer,
    age_max_months integer,
    usage_restrictions text,
    cleaning_instructions text,
    return_policy text,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_recurring_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_recurring_schedules (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid,
    classroom_id uuid,
    schedule_type character varying(50) DEFAULT 'RecurringPurchase'::character varying NOT NULL,
    frequency character varying(50) DEFAULT 'Monthly'::character varying NOT NULL,
    material_product_id uuid NOT NULL,
    offering_id uuid,
    quantity integer DEFAULT 1 NOT NULL,
    loan_duration_days integer DEFAULT 0 NOT NULL,
    delivery_address text NOT NULL,
    next_run_date timestamp with time zone NOT NULL,
    last_run_date timestamp with time zone,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    notes text,
    created_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_return_inspections; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_return_inspections (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    loan_id uuid NOT NULL,
    order_id uuid NOT NULL,
    material_product_id uuid NOT NULL,
    returned_quantity integer NOT NULL,
    inspected_at timestamp with time zone DEFAULT now() NOT NULL,
    inspected_by_user_id uuid,
    inspected_by_email character varying(256),
    condition character varying(50) DEFAULT 'Good'::character varying NOT NULL,
    quantity_approved_for_stock integer DEFAULT 0 NOT NULL,
    quantity_requiring_cleaning integer DEFAULT 0 NOT NULL,
    quantity_damaged integer DEFAULT 0 NOT NULL,
    quantity_missing integer DEFAULT 0 NOT NULL,
    inspection_notes text,
    deposit_disposition character varying(50) DEFAULT 'FullRefund'::character varying NOT NULL,
    deposit_deduction_amount numeric(10,2) DEFAULT 0.00 NOT NULL,
    resolution_status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: material_shipments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.material_shipments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    order_id uuid NOT NULL,
    shipment_number character varying(50) NOT NULL,
    carrier_name character varying(100) NOT NULL,
    tracking_reference character varying(100) NOT NULL,
    dispatch_date timestamp with time zone DEFAULT now() NOT NULL,
    estimated_delivery_date timestamp with time zone,
    actual_delivery_date timestamp with time zone,
    delivery_status character varying(50) DEFAULT 'Dispatched'::character varying NOT NULL,
    proof_of_delivery_notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: medication_administrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.medication_administrations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    medication_id uuid NOT NULL,
    child_id uuid NOT NULL,
    administered_at timestamp with time zone NOT NULL,
    dosage character varying(100),
    administered_by uuid NOT NULL,
    notes text
);


--
-- Name: organization_billing_settings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_billing_settings (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    mode character varying(20) DEFAULT 'Manual'::character varying NOT NULL,
    frequency character varying(20) DEFAULT 'Monthly'::character varying NOT NULL,
    effective_date timestamp with time zone DEFAULT now() NOT NULL,
    next_billing_date timestamp with time zone,
    auto_charge_enabled boolean DEFAULT false NOT NULL,
    notes text,
    updated_by character varying(200),
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: organization_memberships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organization_memberships (
    user_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    status character varying(50) DEFAULT 'Active'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: organizations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.organizations (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    location character varying(300),
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: parent_communications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.parent_communications (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid,
    child_id uuid NOT NULL,
    recipient_person_id uuid,
    recipient_user_id uuid,
    recipient_email character varying(255),
    recipient_name character varying(255),
    recipient_relationship character varying(100),
    communication_type character varying(50) NOT NULL,
    reporting_date_start date NOT NULL,
    reporting_date_end date NOT NULL,
    title character varying(255) NOT NULL,
    content_draft text NOT NULL,
    content_final text NOT NULL,
    family_conversation_starter text,
    evidence_summary_json jsonb,
    consent_verified boolean DEFAULT true NOT NULL,
    consent_notes character varying(500),
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    teacher_id uuid NOT NULL,
    teacher_name character varying(255),
    approved_by_user_id uuid,
    approved_at timestamp with time zone,
    sent_at timestamp with time zone,
    delivery_channel character varying(50) DEFAULT 'PortalAndEmail'::character varying NOT NULL,
    delivery_status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    delivery_error text,
    idempotency_key character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: payments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.payments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    invoice_id uuid,
    invoice_number character varying(50),
    payment_reference character varying(100) NOT NULL,
    amount numeric(12,2) DEFAULT 0 NOT NULL,
    currency character varying(10) DEFAULT 'INR'::character varying NOT NULL,
    method character varying(50) DEFAULT 'BankTransfer'::character varying NOT NULL,
    is_verified boolean DEFAULT true NOT NULL,
    verified_by character varying(200),
    verified_at timestamp with time zone,
    status character varying(50) DEFAULT 'Completed'::character varying NOT NULL,
    transaction_id character varying(100),
    notes text,
    payment_date timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    provider_order_id character varying(100),
    provider_payment_id character varying(100),
    provider_signature character varying(256),
    receipt_number character varying(100),
    recorded_by_user_id uuid,
    recorded_by_email character varying(256),
    is_reversed boolean DEFAULT false NOT NULL,
    reversed_at timestamp with time zone,
    reversed_by character varying(256),
    reversal_reason text,
    reversal_reference character varying(100),
    audit_metadata jsonb
);


--
-- Name: permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.permissions (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500)
);


--
-- Name: persons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.persons (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    relationship character varying(100),
    email character varying(255),
    mobile_phone character varying(50),
    alternate_phone character varying(50),
    occupation character varying(100),
    employer character varying(100),
    preferred_contact_method character varying(50),
    preferred_language character varying(50),
    has_portal_access boolean DEFAULT false NOT NULL,
    receives_daily_updates boolean DEFAULT false NOT NULL,
    receives_photos boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: role_permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.role_permissions (
    role_id uuid NOT NULL,
    permission_id uuid NOT NULL
);


--
-- Name: roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.roles (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid,
    name character varying(100) NOT NULL,
    description character varying(500),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    scope_type character varying(20) DEFAULT 'Organization'::character varying NOT NULL,
    CONSTRAINT chk_roles_scope CHECK (((scope_type)::text = ANY ((ARRAY['Platform'::character varying, 'Tenant'::character varying, 'Organization'::character varying])::text[])))
);


--
-- Name: skills; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.skills (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    category character varying(100),
    description text,
    active boolean DEFAULT true
);


--
-- Name: staff_leaves; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.staff_leaves (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    leave_type character varying(50) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    reason text,
    status character varying(50) DEFAULT 'Approved'::character varying NOT NULL,
    coverage_status character varying(50) DEFAULT 'Uncovered'::character varying NOT NULL,
    substitute_teacher_id uuid,
    reviewed_by_user_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: support_access_grants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.support_access_grants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    granted_to_user_id uuid NOT NULL,
    granted_by_user_id uuid,
    purpose character varying(500) NOT NULL,
    read_only boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    CONSTRAINT chk_support_grant_window CHECK (((expires_at > created_at) AND (expires_at <= (created_at + '7 days'::interval)))),
    CONSTRAINT support_access_grants_purpose_check CHECK ((length(TRIM(BOTH FROM purpose)) > 0))
);


--
-- Name: teacher_attendance; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_attendance (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    attendance_date date NOT NULL,
    status character varying(50) NOT NULL,
    clock_in_time time without time zone,
    clock_out_time time without time zone,
    notes text,
    recorded_by_user_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: teacher_availabilities; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_availabilities (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    can_work_mornings boolean DEFAULT false,
    can_work_afternoons boolean DEFAULT false,
    can_work_evenings boolean DEFAULT false,
    can_work_weekends boolean DEFAULT false,
    max_hours_per_week numeric(5,2),
    notes text
);


--
-- Name: teacher_certifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_certifications (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    certification_type character varying(100),
    certification_name character varying(200) NOT NULL,
    issuing_authority character varying(200),
    certificate_number character varying(100),
    issue_date date,
    expiration_date date,
    status character varying(50),
    verification_status character varying(50),
    document_id uuid,
    notes text
);


--
-- Name: teacher_documents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_documents (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    document_type character varying(100) NOT NULL,
    file_name character varying(200) NOT NULL,
    file_path text NOT NULL,
    uploaded_at timestamp with time zone DEFAULT now() NOT NULL,
    notes text
);


--
-- Name: teacher_education; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_education (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    institution_name character varying(200) NOT NULL,
    degree character varying(100),
    diploma character varying(100),
    certificate character varying(100),
    field_of_study character varying(200),
    start_date date,
    end_date date,
    graduation_date date,
    country character varying(100),
    verification_status character varying(50),
    document_id uuid,
    notes text
);


--
-- Name: teacher_emergency_contacts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_emergency_contacts (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    relationship character varying(100),
    primary_phone character varying(50),
    alternate_phone character varying(50),
    email character varying(200),
    address text,
    priority integer DEFAULT 1,
    notes text
);


--
-- Name: teacher_employments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_employments (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    employee_number character varying(50),
    job_title character varying(100),
    employment_type character varying(50),
    employment_status character varying(50),
    hire_date date,
    probation_end_date date,
    termination_date date,
    department character varying(100),
    branch_id uuid,
    primary_classroom_id uuid,
    reports_to_user_id uuid,
    work_location character varying(100),
    pay_type character varying(50),
    notes text
);


--
-- Name: teacher_experiences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_experiences (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    organization_name character varying(200) NOT NULL,
    "position" character varying(100) NOT NULL,
    start_date date,
    end_date date,
    age_group character varying(100),
    description text,
    reference_information text,
    verification_status character varying(50)
);


--
-- Name: teacher_handovers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_handovers (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    handover_date date DEFAULT CURRENT_DATE NOT NULL,
    shift_type character varying(50) DEFAULT 'MorningToAfternoon'::character varying NOT NULL,
    outgoing_teacher_user_id uuid NOT NULL,
    outgoing_teacher_name character varying(200) NOT NULL,
    incoming_teacher_user_id uuid,
    incoming_teacher_name character varying(200),
    summary_notes text,
    child_specific_notes text,
    outstanding_care_tasks text,
    pending_followups text,
    status character varying(30) DEFAULT 'PendingAcknowledgement'::character varying NOT NULL,
    is_critical boolean DEFAULT false NOT NULL,
    acknowledged_at timestamp with time zone,
    acknowledged_by_user_id uuid,
    acknowledged_by_name character varying(200),
    acknowledgement_notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: teacher_languages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_languages (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    language character varying(100) NOT NULL,
    proficiency character varying(50),
    is_primary_language boolean DEFAULT false,
    can_communicate_with_parents boolean DEFAULT false,
    can_teach_in_language boolean DEFAULT false
);


--
-- Name: teacher_observations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_observations (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    observer_user_id uuid NOT NULL,
    observation_date date NOT NULL,
    rating character varying(50),
    feedback text,
    action_items text
);


--
-- Name: teacher_preferences; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_preferences (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    preference_type character varying(100) NOT NULL,
    preference_value character varying(200) NOT NULL,
    notes text
);


--
-- Name: teacher_professional_developments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_professional_developments (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    course_name character varying(200) NOT NULL,
    provider character varying(200),
    hours_completed numeric(5,2),
    completion_date date,
    notes text
);


--
-- Name: teacher_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_schedules (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    day_of_week character varying(50) NOT NULL,
    start_time time without time zone NOT NULL,
    end_time time without time zone NOT NULL,
    shift_type character varying(100),
    is_active boolean DEFAULT true,
    notes text
);


--
-- Name: teacher_skills; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_skills (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    skill_id uuid NOT NULL,
    skill_level character varying(50),
    years_experience integer,
    is_primary_skill boolean DEFAULT false,
    verified boolean DEFAULT false,
    verified_by uuid,
    verified_at timestamp with time zone,
    notes text
);


--
-- Name: teacher_specialties; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_specialties (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    specialty_id uuid,
    level character varying(100),
    years_experience integer,
    notes text
);


--
-- Name: teacher_trainings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teacher_trainings (
    id uuid NOT NULL,
    teacher_id uuid NOT NULL,
    training_name character varying(200) NOT NULL,
    is_mandatory boolean DEFAULT false,
    completion_date date,
    expiry_date date,
    status character varying(50)
);


--
-- Name: teachers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.teachers (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    person_id uuid,
    user_id uuid,
    employee_number character varying(50),
    first_name character varying(100) NOT NULL,
    middle_name character varying(100),
    last_name character varying(100) NOT NULL,
    preferred_name character varying(100),
    display_name character varying(200),
    profile_photo_url text,
    date_of_birth date,
    gender character varying(50),
    preferred_language character varying(50),
    other_languages text,
    pronouns character varying(50),
    bio text,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    draft_data jsonb,
    profile_completion_percentage integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: tenants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tenants (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(200) NOT NULL,
    slug character varying(100) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    encrypted_ai_api_key character varying(1000)
);


--
-- Name: user_branch_assignments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_branch_assignments (
    user_id uuid NOT NULL,
    branch_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    assigned_by uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    revoked_at timestamp with time zone
);


--
-- Name: user_external_logins; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_external_logins (
    provider character varying(100) NOT NULL,
    provider_key character varying(256) NOT NULL,
    user_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: user_organization_roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_organization_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: user_roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    email character varying(256) NOT NULL,
    normalized_email character varying(256) NOT NULL,
    email_confirmed boolean DEFAULT false NOT NULL,
    password_hash text,
    security_stamp character varying(256),
    concurrency_stamp character varying(256),
    phone_number character varying(50),
    phone_number_confirmed boolean DEFAULT false NOT NULL,
    two_factor_enabled boolean DEFAULT false NOT NULL,
    lockout_end timestamp with time zone,
    lockout_enabled boolean DEFAULT true NOT NULL,
    access_failed_count integer DEFAULT 0 NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    display_name character varying(200),
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: weekly_planning_draft_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.weekly_planning_draft_items (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    request_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    scheduled_date date NOT NULL,
    day_of_week character varying(20) NOT NULL,
    time_of_day character varying(50) DEFAULT 'Morning'::character varying NOT NULL,
    experience_id uuid NOT NULL,
    experience_title character varying(255) NOT NULL,
    learning_goal text,
    group_format character varying(50) DEFAULT 'Small Group'::character varying,
    estimated_duration_minutes integer DEFAULT 25,
    required_materials text,
    rationale text,
    validation_status character varying(50) DEFAULT 'Valid'::character varying NOT NULL,
    validation_notes text,
    resulting_schedule_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: weekly_planning_draft_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.weekly_planning_draft_requests (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    tenant_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    classroom_id uuid NOT NULL,
    requested_by_user_id uuid NOT NULL,
    week_start_date date NOT NULL,
    week_end_date date NOT NULL,
    focus_areas character varying(255),
    indoor_outdoor_preference character varying(50) DEFAULT 'Balanced'::character varying,
    materials_notes text,
    status character varying(50) DEFAULT 'Draft'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: _migrations id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public._migrations ALTER COLUMN id SET DEFAULT nextval('public._migrations_id_seq'::regclass);


--
-- Name: _migrations _migrations_filename_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public._migrations
    ADD CONSTRAINT _migrations_filename_key UNIQUE (filename);


--
-- Name: _migrations _migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public._migrations
    ADD CONSTRAINT _migrations_pkey PRIMARY KEY (id);


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_pkey PRIMARY KEY (id);


--
-- Name: activity_recommendation_items activity_recommendation_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_pkey PRIMARY KEY (id);


--
-- Name: activity_recommendation_requests activity_recommendation_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_requests
    ADD CONSTRAINT activity_recommendation_requests_pkey PRIMARY KEY (id);


--
-- Name: addresses addresses_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.addresses
    ADD CONSTRAINT addresses_pkey PRIMARY KEY (id);


--
-- Name: audit_log audit_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_pkey PRIMARY KEY (id);


--
-- Name: authorized_pickups authorized_pickups_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_pkey PRIMARY KEY (id);


--
-- Name: branch_floor_maps branch_floor_maps_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_floor_maps
    ADD CONSTRAINT branch_floor_maps_pkey PRIMARY KEY (id);


--
-- Name: branch_operational_goals branch_operational_goals_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_pkey PRIMARY KEY (id);


--
-- Name: branches branches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branches
    ADD CONSTRAINT branches_pkey PRIMARY KEY (id);


--
-- Name: child_allergies child_allergies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_pkey PRIMARY KEY (id);


--
-- Name: child_attendance child_attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_pkey PRIMARY KEY (id);


--
-- Name: child_care_events child_care_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_pkey PRIMARY KEY (id);


--
-- Name: child_care_plans child_care_plans_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_pkey PRIMARY KEY (id);


--
-- Name: child_care_supplies child_care_supplies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_pkey PRIMARY KEY (id);


--
-- Name: child_communication_profiles child_communication_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_consent_documents child_consent_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_pkey PRIMARY KEY (id);


--
-- Name: child_consents child_consents_child_id_consent_type_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_child_id_consent_type_key UNIQUE (child_id, consent_type);


--
-- Name: child_consents child_consents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_pkey PRIMARY KEY (id);


--
-- Name: child_dietary_profiles child_dietary_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_documents child_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_pkey PRIMARY KEY (id);


--
-- Name: child_enrollment_histories child_enrollment_histories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_pkey PRIMARY KEY (id);


--
-- Name: child_feeding_profiles child_feeding_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_food_restrictions child_food_restrictions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_pkey PRIMARY KEY (id);


--
-- Name: child_guardians child_guardians_child_id_person_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_child_id_person_id_key UNIQUE (child_id, person_id);


--
-- Name: child_guardians child_guardians_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_pkey PRIMARY KEY (id);


--
-- Name: child_interests child_interests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_interests
    ADD CONSTRAINT child_interests_pkey PRIMARY KEY (id);


--
-- Name: child_journey_developments child_journey_developments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_pkey PRIMARY KEY (journey_entry_id, development_area_id);


--
-- Name: child_journey_entries child_journey_entries_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_pkey PRIMARY KEY (id);


--
-- Name: child_journey_evidence child_journey_evidence_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_evidence
    ADD CONSTRAINT child_journey_evidence_pkey PRIMARY KEY (id);


--
-- Name: child_journey_insight_items child_journey_insight_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_items
    ADD CONSTRAINT child_journey_insight_items_pkey PRIMARY KEY (id);


--
-- Name: child_journey_insight_requests child_journey_insight_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_requests
    ADD CONSTRAINT child_journey_insight_requests_pkey PRIMARY KEY (id);


--
-- Name: child_legal_profiles child_legal_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_medical_profiles child_medical_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_medications child_medications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_pkey PRIMARY KEY (id);


--
-- Name: child_milestones child_milestones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_pkey PRIMARY KEY (id);


--
-- Name: child_nap_sessions child_nap_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_pkey PRIMARY KEY (id);


--
-- Name: child_person_restrictions child_person_restrictions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_person_restrictions
    ADD CONSTRAINT child_person_restrictions_pkey PRIMARY KEY (id);


--
-- Name: child_relationships child_relationships_child_id_related_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_child_id_related_child_id_key UNIQUE (child_id, related_child_id);


--
-- Name: child_relationships child_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_pkey PRIMARY KEY (id);


--
-- Name: child_routines child_routines_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_pkey PRIMARY KEY (child_id);


--
-- Name: child_schedules child_schedules_child_id_day_of_week_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_child_id_day_of_week_key UNIQUE (child_id, day_of_week);


--
-- Name: child_schedules child_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_pkey PRIMARY KEY (id);


--
-- Name: child_sleep_profiles child_sleep_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_sleep_profiles
    ADD CONSTRAINT child_sleep_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_stories child_stories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_pkey PRIMARY KEY (id);


--
-- Name: child_support_profiles child_support_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_toileting_profiles child_toileting_profiles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_pkey PRIMARY KEY (child_id);


--
-- Name: child_transportations child_transportations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_pkey PRIMARY KEY (child_id);


--
-- Name: children children_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_pkey PRIMARY KEY (id);


--
-- Name: classroom_statutory_ratios classroom_statutory_ratios_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_statutory_ratios
    ADD CONSTRAINT classroom_statutory_ratios_pkey PRIMARY KEY (id);


--
-- Name: classroom_teachers classroom_teachers_classroom_id_user_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_classroom_id_user_id_key UNIQUE (classroom_id, user_id);


--
-- Name: classroom_teachers classroom_teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_pkey PRIMARY KEY (id);


--
-- Name: classrooms classrooms_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_pkey PRIMARY KEY (id);


--
-- Name: consent_audit_events consent_audit_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_pkey PRIMARY KEY (id);


--
-- Name: consent_choices consent_choices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_pkey PRIMARY KEY (id);


--
-- Name: consent_signers consent_signers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_pkey PRIMARY KEY (id);


--
-- Name: consent_templates consent_templates_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_pkey PRIMARY KEY (id);


--
-- Name: development_areas development_areas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.development_areas
    ADD CONSTRAINT development_areas_pkey PRIMARY KEY (id);


--
-- Name: emergency_contacts emergency_contacts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_pkey PRIMARY KEY (id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_pkey PRIMARY KEY (id);


--
-- Name: enrollments enrollments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_pkey PRIMARY KEY (id);


--
-- Name: event_classrooms event_classrooms_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_pkey PRIMARY KEY (event_id, classroom_id);


--
-- Name: event_experiences event_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_pkey PRIMARY KEY (id);


--
-- Name: event_rsvps event_rsvps_event_id_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_event_id_child_id_key UNIQUE (event_id, child_id);


--
-- Name: event_rsvps event_rsvps_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_pkey PRIMARY KEY (id);


--
-- Name: event_tasks event_tasks_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_pkey PRIMARY KEY (id);


--
-- Name: event_types event_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_types
    ADD CONSTRAINT event_types_pkey PRIMARY KEY (id);


--
-- Name: events events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_pkey PRIMARY KEY (id);


--
-- Name: experience_audit_events experience_audit_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_pkey PRIMARY KEY (id);


--
-- Name: experience_feedback experience_feedback_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_feedback
    ADD CONSTRAINT experience_feedback_pkey PRIMARY KEY (id);


--
-- Name: experience_harvest_requests experience_harvest_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_pkey PRIMARY KEY (id);


--
-- Name: experience_licenses experience_licenses_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_pkey PRIMARY KEY (id);


--
-- Name: experience_participations experience_participations_experience_session_id_child_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_experience_session_id_child_id_key UNIQUE (experience_session_id, child_id);


--
-- Name: experience_participations experience_participations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_pkey PRIMARY KEY (id);


--
-- Name: experience_schedules experience_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT experience_schedules_pkey PRIMARY KEY (id);


--
-- Name: experience_session_teachers experience_session_teachers_experience_session_id_teacher_i_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_experience_session_id_teacher_i_key UNIQUE (experience_session_id, teacher_id);


--
-- Name: experience_session_teachers experience_session_teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_pkey PRIMARY KEY (id);


--
-- Name: experience_sessions experience_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_pkey PRIMARY KEY (id);


--
-- Name: experience_versions experience_versions_experience_id_version_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_experience_id_version_number_key UNIQUE (experience_id, version_number);


--
-- Name: experience_versions experience_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_pkey PRIMARY KEY (id);


--
-- Name: experiences experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_pkey PRIMARY KEY (id);


--
-- Name: family_billing_contracts family_billing_contracts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_pkey PRIMARY KEY (id);


--
-- Name: family_relationships family_relationships_child_id_user_id_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_child_id_user_id_key UNIQUE (child_id, user_id);


--
-- Name: family_relationships family_relationships_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_pkey PRIMARY KEY (id);


--
-- Name: floor_map_markers floor_map_markers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floor_map_markers
    ADD CONSTRAINT floor_map_markers_pkey PRIMARY KEY (id);


--
-- Name: inventory_balances inventory_balances_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_pkey PRIMARY KEY (id);


--
-- Name: inventory_ledger inventory_ledger_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_pkey PRIMARY KEY (id);


--
-- Name: inventory_locations inventory_locations_code_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_locations
    ADD CONSTRAINT inventory_locations_code_key UNIQUE (code);


--
-- Name: inventory_locations inventory_locations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_locations
    ADD CONSTRAINT inventory_locations_pkey PRIMARY KEY (id);


--
-- Name: invoice_line_items invoice_line_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoice_line_items
    ADD CONSTRAINT invoice_line_items_pkey PRIMARY KEY (id);


--
-- Name: invoices invoices_invoice_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_invoice_number_key UNIQUE (invoice_number);


--
-- Name: invoices invoices_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_pkey PRIMARY KEY (id);


--
-- Name: marketplace_orders marketplace_orders_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_pkey PRIMARY KEY (id);


--
-- Name: material_loans material_loans_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_pkey PRIMARY KEY (id);


--
-- Name: material_offerings material_offerings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT material_offerings_pkey PRIMARY KEY (id);


--
-- Name: material_order_history material_order_history_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_history
    ADD CONSTRAINT material_order_history_pkey PRIMARY KEY (id);


--
-- Name: material_order_items material_order_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_pkey PRIMARY KEY (id);


--
-- Name: material_orders material_orders_order_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_order_number_key UNIQUE (order_number);


--
-- Name: material_orders material_orders_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_pkey PRIMARY KEY (id);


--
-- Name: material_products material_products_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_products
    ADD CONSTRAINT material_products_pkey PRIMARY KEY (id);


--
-- Name: material_products material_products_sku_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_products
    ADD CONSTRAINT material_products_sku_key UNIQUE (sku);


--
-- Name: material_recurring_schedules material_recurring_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_pkey PRIMARY KEY (id);


--
-- Name: material_return_inspections material_return_inspections_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_pkey PRIMARY KEY (id);


--
-- Name: material_shipments material_shipments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_pkey PRIMARY KEY (id);


--
-- Name: material_shipments material_shipments_shipment_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_shipment_number_key UNIQUE (shipment_number);


--
-- Name: medication_administrations medication_administrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_pkey PRIMARY KEY (id);


--
-- Name: organization_billing_settings organization_billing_settings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_pkey PRIMARY KEY (id);


--
-- Name: organization_entitlements organization_entitlements_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_pkey PRIMARY KEY (id);


--
-- Name: organization_intelligence_insights organization_intelligence_insights_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_pkey PRIMARY KEY (id);


--
-- Name: organization_memberships organization_memberships_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_memberships
    ADD CONSTRAINT organization_memberships_pkey PRIMARY KEY (user_id, organization_id);


--
-- Name: organizations organizations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT organizations_pkey PRIMARY KEY (id);


--
-- Name: parent_communications parent_communications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_pkey PRIMARY KEY (id);


--
-- Name: payments payments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_pkey PRIMARY KEY (id);


--
-- Name: permissions permissions_name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_name_key UNIQUE (name);


--
-- Name: permissions permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_pkey PRIMARY KEY (id);


--
-- Name: persons persons_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_pkey PRIMARY KEY (id);


--
-- Name: platform_experience_versions platform_experience_versions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT platform_experience_versions_pkey PRIMARY KEY (id);


--
-- Name: platform_experiences platform_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_pkey PRIMARY KEY (id);


--
-- Name: role_permissions role_permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_pkey PRIMARY KEY (role_id, permission_id);


--
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);


--
-- Name: skills skills_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.skills
    ADD CONSTRAINT skills_pkey PRIMARY KEY (id);


--
-- Name: staff_leaves staff_leaves_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_pkey PRIMARY KEY (id);


--
-- Name: support_access_grants support_access_grants_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_pkey PRIMARY KEY (id);


--
-- Name: teacher_attendance teacher_attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT teacher_attendance_pkey PRIMARY KEY (id);


--
-- Name: teacher_availabilities teacher_availabilities_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_availabilities
    ADD CONSTRAINT teacher_availabilities_pkey PRIMARY KEY (id);


--
-- Name: teacher_certifications teacher_certifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_certifications
    ADD CONSTRAINT teacher_certifications_pkey PRIMARY KEY (id);


--
-- Name: teacher_documents teacher_documents_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_documents
    ADD CONSTRAINT teacher_documents_pkey PRIMARY KEY (id);


--
-- Name: teacher_education teacher_education_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_education
    ADD CONSTRAINT teacher_education_pkey PRIMARY KEY (id);


--
-- Name: teacher_emergency_contacts teacher_emergency_contacts_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_emergency_contacts
    ADD CONSTRAINT teacher_emergency_contacts_pkey PRIMARY KEY (id);


--
-- Name: teacher_employments teacher_employments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_employments
    ADD CONSTRAINT teacher_employments_pkey PRIMARY KEY (id);


--
-- Name: teacher_experiences teacher_experiences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_experiences
    ADD CONSTRAINT teacher_experiences_pkey PRIMARY KEY (id);


--
-- Name: teacher_handovers teacher_handovers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_pkey PRIMARY KEY (id);


--
-- Name: teacher_languages teacher_languages_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_languages
    ADD CONSTRAINT teacher_languages_pkey PRIMARY KEY (id);


--
-- Name: teacher_observations teacher_observations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_observations
    ADD CONSTRAINT teacher_observations_pkey PRIMARY KEY (id);


--
-- Name: teacher_preferences teacher_preferences_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_preferences
    ADD CONSTRAINT teacher_preferences_pkey PRIMARY KEY (id);


--
-- Name: teacher_professional_developments teacher_professional_developments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_professional_developments
    ADD CONSTRAINT teacher_professional_developments_pkey PRIMARY KEY (id);


--
-- Name: teacher_schedules teacher_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_schedules
    ADD CONSTRAINT teacher_schedules_pkey PRIMARY KEY (id);


--
-- Name: teacher_skills teacher_skills_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT teacher_skills_pkey PRIMARY KEY (id);


--
-- Name: teacher_specialties teacher_specialties_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_specialties
    ADD CONSTRAINT teacher_specialties_pkey PRIMARY KEY (id);


--
-- Name: teacher_trainings teacher_trainings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_trainings
    ADD CONSTRAINT teacher_trainings_pkey PRIMARY KEY (id);


--
-- Name: teachers teachers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teachers
    ADD CONSTRAINT teachers_pkey PRIMARY KEY (id);


--
-- Name: tenants tenants_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (id);


--
-- Name: tenants tenants_slug_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenants
    ADD CONSTRAINT tenants_slug_key UNIQUE (slug);


--
-- Name: inventory_balances uk_inv_bal_prod_loc; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT uk_inv_bal_prod_loc UNIQUE (material_product_id, location_id);


--
-- Name: material_offerings uk_mat_offering_prod; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT uk_mat_offering_prod UNIQUE (material_product_id);


--
-- Name: organization_billing_settings uk_org_billing_settings_org; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT uk_org_billing_settings_org UNIQUE (organization_id);


--
-- Name: organization_memberships uk_org_memberships_tenant; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_memberships
    ADD CONSTRAINT uk_org_memberships_tenant UNIQUE (user_id, organization_id, tenant_id);


--
-- Name: organizations uk_organizations_tenant; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT uk_organizations_tenant UNIQUE (id, tenant_id);


--
-- Name: roles uk_roles_tenant; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT uk_roles_tenant UNIQUE (id, tenant_id);


--
-- Name: users uk_users_tenant; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT uk_users_tenant UNIQUE (id, tenant_id);


--
-- Name: marketplace_orders uq_marketplace_orders_txn; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT uq_marketplace_orders_txn UNIQUE (organization_id, platform_experience_id, transaction_reference);


--
-- Name: organization_entitlements uq_org_entitlement; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT uq_org_entitlement UNIQUE (organization_id, platform_experience_id);


--
-- Name: platform_experience_versions uq_platform_exp_version; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT uq_platform_exp_version UNIQUE (platform_experience_id, version_number);


--
-- Name: user_branch_assignments user_branch_assignments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_branch_assignments
    ADD CONSTRAINT user_branch_assignments_pkey PRIMARY KEY (user_id, branch_id);


--
-- Name: user_external_logins user_external_logins_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_external_logins
    ADD CONSTRAINT user_external_logins_pkey PRIMARY KEY (provider, provider_key);


--
-- Name: user_organization_roles user_organization_roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_pkey PRIMARY KEY (user_id, role_id, organization_id);


--
-- Name: user_roles user_roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_pkey PRIMARY KEY (user_id, role_id);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_pkey PRIMARY KEY (id);


--
-- Name: weekly_planning_draft_requests weekly_planning_draft_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_requests
    ADD CONSTRAINT weekly_planning_draft_requests_pkey PRIMARY KEY (id);


--
-- Name: idx_addresses_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_addresses_tenant ON public.addresses USING btree (tenant_id);


--
-- Name: idx_audit_log_entity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_log_entity ON public.audit_log USING btree (entity_type, entity_id);


--
-- Name: idx_audit_log_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_log_tenant ON public.audit_log USING btree (tenant_id);


--
-- Name: idx_audit_log_timestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_log_timestamp ON public.audit_log USING btree ("timestamp");


--
-- Name: idx_authorized_pickups_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_authorized_pickups_child ON public.authorized_pickups USING btree (tenant_id, child_id, is_active);


--
-- Name: idx_branch_floor_maps_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_floor_maps_lookup ON public.branch_floor_maps USING btree (tenant_id, organization_id, branch_id, status);


--
-- Name: idx_branch_goals_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_goals_branch ON public.branch_operational_goals USING btree (branch_id);


--
-- Name: idx_branch_goals_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branch_goals_tenant_org ON public.branch_operational_goals USING btree (tenant_id, organization_id);


--
-- Name: idx_branches_organization; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branches_organization ON public.branches USING btree (organization_id);


--
-- Name: idx_branches_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_branches_tenant ON public.branches USING btree (tenant_id);


--
-- Name: idx_ca_tenant_branch_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ca_tenant_branch_date ON public.child_attendance USING btree (tenant_id, branch_id, attendance_date);


--
-- Name: idx_ca_tenant_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ca_tenant_classroom_date ON public.child_attendance USING btree (tenant_id, classroom_id, attendance_date);


--
-- Name: idx_cce_child_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_child_time ON public.child_care_events USING btree (child_id, event_time DESC);


--
-- Name: idx_cce_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_classroom_date ON public.child_care_events USING btree (classroom_id, event_time);


--
-- Name: idx_cce_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cce_tenant_org ON public.child_care_events USING btree (tenant_id, organization_id);


--
-- Name: idx_ccp_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ccp_child ON public.child_care_plans USING btree (child_id, status);


--
-- Name: idx_ccs_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ccs_child ON public.child_care_supplies USING btree (child_id, status);


--
-- Name: idx_child_attendance_departure; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_attendance_departure ON public.child_attendance USING btree (tenant_id, child_id, attendance_date, departure_time);


--
-- Name: idx_child_consent_docs_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_child ON public.child_consent_documents USING btree (tenant_id, child_id);


--
-- Name: idx_child_consent_docs_org_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_org_branch ON public.child_consent_documents USING btree (organization_id, branch_id);


--
-- Name: idx_child_consent_docs_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_consent_docs_status ON public.child_consent_documents USING btree (tenant_id, status);


--
-- Name: idx_child_interests_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_interests_child ON public.child_interests USING btree (child_id);


--
-- Name: idx_child_interests_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_interests_tenant ON public.child_interests USING btree (tenant_id);


--
-- Name: idx_child_journey_participation; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_journey_participation ON public.child_journey_entries USING btree (experience_participation_id);


--
-- Name: idx_child_journey_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_journey_session ON public.child_journey_entries USING btree (experience_session_id);


--
-- Name: idx_child_milestones_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_child ON public.child_milestones USING btree (child_id);


--
-- Name: idx_child_milestones_portfolio; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_portfolio ON public.child_milestones USING btree (tenant_id, child_id, parent_visible, milestone_date DESC);


--
-- Name: idx_child_milestones_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_milestones_tenant ON public.child_milestones USING btree (tenant_id);


--
-- Name: idx_child_restrictions_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_restrictions_child ON public.child_person_restrictions USING btree (tenant_id, child_id);


--
-- Name: idx_child_stories_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_stories_child ON public.child_stories USING btree (child_id);


--
-- Name: idx_child_stories_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_child_stories_tenant ON public.child_stories USING btree (tenant_id);


--
-- Name: idx_children_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_children_classroom ON public.children USING btree (classroom_id);


--
-- Name: idx_children_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_children_tenant ON public.children USING btree (tenant_id);


--
-- Name: idx_classroom_teachers_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classroom_teachers_tenant ON public.classroom_teachers USING btree (tenant_id);


--
-- Name: idx_classrooms_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_branch ON public.classrooms USING btree (branch_id);


--
-- Name: idx_classrooms_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_status ON public.classrooms USING btree (status);


--
-- Name: idx_classrooms_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_classrooms_tenant ON public.classrooms USING btree (tenant_id);


--
-- Name: idx_cns_child_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cns_child_active ON public.child_nap_sessions USING btree (child_id, status);


--
-- Name: idx_consent_audit_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_audit_doc ON public.consent_audit_events USING btree (consent_document_id);


--
-- Name: idx_consent_audit_tenant_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_audit_tenant_time ON public.consent_audit_events USING btree (tenant_id, timestamp_utc DESC);


--
-- Name: idx_consent_choices_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_choices_doc ON public.consent_choices USING btree (consent_document_id);


--
-- Name: idx_consent_signers_doc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_signers_doc ON public.consent_signers USING btree (consent_document_id);


--
-- Name: idx_consent_signers_token; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_signers_token ON public.consent_signers USING btree (token_hash);


--
-- Name: idx_consent_templates_doc_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_templates_doc_type ON public.consent_templates USING btree (tenant_id, document_type);


--
-- Name: idx_consent_templates_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_consent_templates_tenant_org ON public.consent_templates USING btree (tenant_id, organization_id);


--
-- Name: idx_development_areas_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_development_areas_tenant ON public.development_areas USING btree (tenant_id);


--
-- Name: idx_emergency_contacts_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_emergency_contacts_lookup ON public.emergency_contacts USING btree (tenant_id, organization_id, branch_id, is_active);


--
-- Name: idx_enrollment_pipeline_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_enrollment_pipeline_branch ON public.enrollment_pipeline_applications USING btree (branch_id);


--
-- Name: idx_enrollment_pipeline_tenant_stage; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_enrollment_pipeline_tenant_stage ON public.enrollment_pipeline_applications USING btree (tenant_id, pipeline_stage);


--
-- Name: idx_es_tenant_date_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_es_tenant_date_classroom ON public.experience_schedules USING btree (tenant_id, scheduled_date, classroom_id);


--
-- Name: idx_event_experiences_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_experiences_event ON public.event_experiences USING btree (event_id);


--
-- Name: idx_event_rsvps_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_rsvps_event ON public.event_rsvps USING btree (event_id);


--
-- Name: idx_event_tasks_event; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_tasks_event ON public.event_tasks USING btree (event_id);


--
-- Name: idx_event_types_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_event_types_tenant ON public.event_types USING btree (tenant_id);


--
-- Name: idx_events_branch; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_branch ON public.events USING btree (branch_id);


--
-- Name: idx_events_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_date ON public.events USING btree (event_date);


--
-- Name: idx_events_scope; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_scope ON public.events USING btree (tenant_id, scope);


--
-- Name: idx_events_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_events_tenant ON public.events USING btree (tenant_id);


--
-- Name: idx_exp_audit_action; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_audit_action ON public.experience_audit_events USING btree (action);


--
-- Name: idx_exp_audit_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_audit_org ON public.experience_audit_events USING btree (organization_id);


--
-- Name: idx_exp_participations_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_participations_child ON public.experience_participations USING btree (child_id);


--
-- Name: idx_exp_participations_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_participations_session ON public.experience_participations USING btree (experience_session_id);


--
-- Name: idx_exp_sess_teachers_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_sess_teachers_session ON public.experience_session_teachers USING btree (experience_session_id);


--
-- Name: idx_exp_versions_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_exp_versions_exp ON public.experience_versions USING btree (experience_id);


--
-- Name: idx_experience_feedback_schedule; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_feedback_schedule ON public.experience_feedback USING btree (experience_schedule_id);


--
-- Name: idx_experience_feedback_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_feedback_tenant ON public.experience_feedback USING btree (tenant_id);


--
-- Name: idx_experience_licenses_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_licenses_exp ON public.experience_licenses USING btree (experience_id);


--
-- Name: idx_experience_licenses_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_licenses_org ON public.experience_licenses USING btree (organization_id);


--
-- Name: idx_experience_schedules_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_schedules_classroom_date ON public.experience_schedules USING btree (classroom_id, scheduled_date);


--
-- Name: idx_experience_schedules_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_schedules_tenant ON public.experience_schedules USING btree (tenant_id);


--
-- Name: idx_experience_sessions_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_classroom ON public.experience_sessions USING btree (classroom_id);


--
-- Name: idx_experience_sessions_schedule; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_schedule ON public.experience_sessions USING btree (experience_schedule_id);


--
-- Name: idx_experience_sessions_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experience_sessions_tenant ON public.experience_sessions USING btree (tenant_id);


--
-- Name: idx_experiences_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_org ON public.experiences USING btree (organization_id);


--
-- Name: idx_experiences_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_status ON public.experiences USING btree (status);


--
-- Name: idx_experiences_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_experiences_tenant ON public.experiences USING btree (tenant_id);


--
-- Name: idx_family_contracts_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_contracts_tenant_child ON public.family_billing_contracts USING btree (tenant_id, child_id);


--
-- Name: idx_family_relationships_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_relationships_tenant ON public.family_relationships USING btree (tenant_id);


--
-- Name: idx_family_relationships_user_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_family_relationships_user_child ON public.family_relationships USING btree (tenant_id, user_id, child_id) WHERE (((status)::text = 'Verified'::text) AND (revoked_at IS NULL));


--
-- Name: idx_floor_map_markers_map_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_floor_map_markers_map_id ON public.floor_map_markers USING btree (floor_map_id);


--
-- Name: idx_harvest_requests_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_org ON public.experience_harvest_requests USING btree (organization_id);


--
-- Name: idx_harvest_requests_source; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_source ON public.experience_harvest_requests USING btree (source_experience_id);


--
-- Name: idx_harvest_requests_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_harvest_requests_status ON public.experience_harvest_requests USING btree (status);


--
-- Name: idx_inv_bal_prod; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_bal_prod ON public.inventory_balances USING btree (material_product_id);


--
-- Name: idx_inv_ledger_prod; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_ledger_prod ON public.inventory_ledger USING btree (material_product_id, created_at DESC);


--
-- Name: idx_inv_ledger_ref; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_inv_ledger_ref ON public.inventory_ledger USING btree (reference_type, reference_id);


--
-- Name: idx_invoice_line_items_inv; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_invoice_line_items_inv ON public.invoice_line_items USING btree (invoice_id);


--
-- Name: idx_invoices_due; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_invoices_due ON public.invoices USING btree (due_date);


--
-- Name: idx_invoices_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_invoices_org ON public.invoices USING btree (organization_id, tenant_id);


--
-- Name: idx_invoices_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_invoices_status ON public.invoices USING btree (status);


--
-- Name: idx_journey_entries_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_child ON public.child_journey_entries USING btree (child_id);


--
-- Name: idx_journey_entries_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_classroom ON public.child_journey_entries USING btree (classroom_id);


--
-- Name: idx_journey_entries_portfolio; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_portfolio ON public.child_journey_entries USING btree (tenant_id, child_id, is_parent_visible, observation_date DESC);


--
-- Name: idx_journey_entries_teacher; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_teacher ON public.child_journey_entries USING btree (teacher_id);


--
-- Name: idx_journey_entries_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_entries_tenant ON public.child_journey_entries USING btree (tenant_id);


--
-- Name: idx_journey_evidence_journey; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_evidence_journey ON public.child_journey_evidence USING btree (journey_entry_id);


--
-- Name: idx_journey_insight_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_insight_items_request ON public.child_journey_insight_items USING btree (request_id, category, rank_order);


--
-- Name: idx_journey_insight_requests_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_journey_insight_requests_tenant_child ON public.child_journey_insight_requests USING btree (tenant_id, child_id, created_at DESC);


--
-- Name: idx_marketplace_orders_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_marketplace_orders_exp ON public.marketplace_orders USING btree (platform_experience_id);


--
-- Name: idx_marketplace_orders_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_marketplace_orders_org ON public.marketplace_orders USING btree (organization_id);


--
-- Name: idx_mat_inspections_loan; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_inspections_loan ON public.material_return_inspections USING btree (loan_id);


--
-- Name: idx_mat_loans_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_loans_org ON public.material_loans USING btree (organization_id, due_date);


--
-- Name: idx_mat_loans_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_loans_status ON public.material_loans USING btree (return_status);


--
-- Name: idx_mat_order_hist_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_order_hist_order ON public.material_order_history USING btree (order_id, created_at);


--
-- Name: idx_mat_order_items_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_order_items_order ON public.material_order_items USING btree (order_id);


--
-- Name: idx_mat_orders_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_exp ON public.material_orders USING btree (planned_experience_id);


--
-- Name: idx_mat_orders_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_org ON public.material_orders USING btree (organization_id, created_at DESC);


--
-- Name: idx_mat_orders_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_orders_status ON public.material_orders USING btree (workflow_status);


--
-- Name: idx_mat_prod_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_active ON public.material_products USING btree (is_active);


--
-- Name: idx_mat_prod_cat; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_cat ON public.material_products USING btree (category);


--
-- Name: idx_mat_prod_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_prod_type ON public.material_products USING btree (material_type);


--
-- Name: idx_mat_recurring_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_recurring_org ON public.material_recurring_schedules USING btree (organization_id, status);


--
-- Name: idx_mat_shipments_order; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_mat_shipments_order ON public.material_shipments USING btree (order_id);


--
-- Name: idx_org_billing_settings_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_billing_settings_org ON public.organization_billing_settings USING btree (organization_id);


--
-- Name: idx_org_entitlements_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_exp ON public.organization_entitlements USING btree (platform_experience_id);


--
-- Name: idx_org_entitlements_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_org ON public.organization_entitlements USING btree (organization_id);


--
-- Name: idx_org_entitlements_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_entitlements_status ON public.organization_entitlements USING btree (status);


--
-- Name: idx_org_intel_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_category ON public.organization_intelligence_insights USING btree (tenant_id, category);


--
-- Name: idx_org_intel_created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_created ON public.organization_intelligence_insights USING btree (tenant_id, created_at DESC);


--
-- Name: idx_org_intel_tenant_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_org_intel_tenant_org ON public.organization_intelligence_insights USING btree (tenant_id, organization_id);


--
-- Name: idx_organizations_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_organizations_tenant ON public.organizations USING btree (tenant_id);


--
-- Name: idx_parent_comms_recipient; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_recipient ON public.parent_communications USING btree (recipient_user_id);


--
-- Name: idx_parent_comms_tenant_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_tenant_child ON public.parent_communications USING btree (tenant_id, child_id);


--
-- Name: idx_parent_comms_tenant_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_parent_comms_tenant_status ON public.parent_communications USING btree (tenant_id, status);


--
-- Name: idx_payments_invoice; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_invoice ON public.payments USING btree (invoice_id);


--
-- Name: idx_payments_method_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_method_date ON public.payments USING btree (method, payment_date DESC);


--
-- Name: idx_payments_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_org ON public.payments USING btree (organization_id, tenant_id);


--
-- Name: idx_payments_provider_order_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_payments_provider_order_id ON public.payments USING btree (provider_order_id) WHERE (provider_order_id IS NOT NULL);


--
-- Name: idx_persons_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_persons_tenant ON public.persons USING btree (tenant_id);


--
-- Name: idx_platform_exp_cat; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_cat ON public.platform_experiences USING btree (category);


--
-- Name: idx_platform_exp_pricing; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_pricing ON public.platform_experiences USING btree (pricing_model);


--
-- Name: idx_platform_exp_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_status ON public.platform_experiences USING btree (publication_status);


--
-- Name: idx_platform_exp_ver_exp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_platform_exp_ver_exp ON public.platform_experience_versions USING btree (platform_experience_id);


--
-- Name: idx_rec_decisions_req; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_decisions_req ON public.activity_recommendation_decisions USING btree (request_id);


--
-- Name: idx_rec_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_items_request ON public.activity_recommendation_items USING btree (request_id);


--
-- Name: idx_rec_req_child; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_req_child ON public.activity_recommendation_requests USING btree (tenant_id, child_id);


--
-- Name: idx_rec_req_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_rec_req_classroom ON public.activity_recommendation_requests USING btree (tenant_id, classroom_id);


--
-- Name: idx_staff_leaves_teacher; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_staff_leaves_teacher ON public.staff_leaves USING btree (teacher_id);


--
-- Name: idx_staff_leaves_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_staff_leaves_tenant ON public.staff_leaves USING btree (tenant_id, start_date, end_date);


--
-- Name: idx_support_access_grants_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_support_access_grants_lookup ON public.support_access_grants USING btree (tenant_id, granted_to_user_id) WHERE (revoked_at IS NULL);


--
-- Name: idx_teacher_handovers_classroom_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_classroom_date ON public.teacher_handovers USING btree (classroom_id, handover_date);


--
-- Name: idx_teacher_handovers_org; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_org ON public.teacher_handovers USING btree (tenant_id, organization_id, branch_id);


--
-- Name: idx_teacher_handovers_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_teacher_handovers_status ON public.teacher_handovers USING btree (status);


--
-- Name: idx_user_branch_assignments_lookup; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_branch_assignments_lookup ON public.user_branch_assignments USING btree (tenant_id, organization_id, user_id) WHERE (revoked_at IS NULL);


--
-- Name: idx_user_external_logins_user; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_external_logins_user ON public.user_external_logins USING btree (user_id);


--
-- Name: idx_users_email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX idx_users_email ON public.users USING btree (normalized_email);


--
-- Name: idx_users_tenant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_users_tenant ON public.users USING btree (tenant_id);


--
-- Name: idx_weekly_planning_items_request; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_weekly_planning_items_request ON public.weekly_planning_draft_items USING btree (request_id, scheduled_date);


--
-- Name: idx_weekly_planning_requests_tenant_classroom; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_weekly_planning_requests_tenant_classroom ON public.weekly_planning_draft_requests USING btree (tenant_id, classroom_id, week_start_date DESC);


--
-- Name: uk_invoices_org_billing_period; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_invoices_org_billing_period ON public.invoices USING btree (organization_id, billing_period) WHERE ((billing_period IS NOT NULL) AND ((status)::text <> 'Void'::text));


--
-- Name: uk_payments_provider_payment_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_payments_provider_payment_id ON public.payments USING btree (provider_payment_id) WHERE ((provider_payment_id IS NOT NULL) AND (is_reversed = false));


--
-- Name: uk_payments_receipt_number; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_payments_receipt_number ON public.payments USING btree (receipt_number) WHERE ((receipt_number IS NOT NULL) AND (is_reversed = false));


--
-- Name: audit_log audit_log_no_change; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER audit_log_no_change BEFORE DELETE OR UPDATE ON public.audit_log FOR EACH ROW EXECUTE FUNCTION public.audit_log_append_only();


--
-- Name: audit_log audit_log_no_truncate; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER audit_log_no_truncate BEFORE TRUNCATE ON public.audit_log FOR EACH STATEMENT EXECUTE FUNCTION public.audit_log_append_only();


--
-- Name: tenants tenants_ensure_roles; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER tenants_ensure_roles AFTER INSERT ON public.tenants FOR EACH ROW EXECUTE FUNCTION public.trg_tenants_ensure_roles();


--
-- Name: user_organization_roles user_organization_roles_audit; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER user_organization_roles_audit AFTER INSERT OR DELETE ON public.user_organization_roles FOR EACH ROW EXECUTE FUNCTION public.audit_org_role_change();


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_recommendation_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_recommendation_item_id_fkey FOREIGN KEY (recommendation_item_id) REFERENCES public.activity_recommendation_items(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.activity_recommendation_requests(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_decisions activity_recommendation_decisions_resulting_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_decisions
    ADD CONSTRAINT activity_recommendation_decisions_resulting_schedule_id_fkey FOREIGN KEY (resulting_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: activity_recommendation_items activity_recommendation_items_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: activity_recommendation_items activity_recommendation_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.activity_recommendation_items
    ADD CONSTRAINT activity_recommendation_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.activity_recommendation_requests(id) ON DELETE CASCADE;


--
-- Name: addresses addresses_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.addresses
    ADD CONSTRAINT addresses_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: audit_log audit_log_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: audit_log audit_log_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: authorized_pickups authorized_pickups_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: authorized_pickups authorized_pickups_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id);


--
-- Name: authorized_pickups authorized_pickups_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.authorized_pickups
    ADD CONSTRAINT authorized_pickups_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: branch_floor_maps branch_floor_maps_supersedes_map_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_floor_maps
    ADD CONSTRAINT branch_floor_maps_supersedes_map_id_fkey FOREIGN KEY (supersedes_map_id) REFERENCES public.branch_floor_maps(id);


--
-- Name: branch_operational_goals branch_operational_goals_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: branch_operational_goals branch_operational_goals_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: branch_operational_goals branch_operational_goals_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branch_operational_goals
    ADD CONSTRAINT branch_operational_goals_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: branches branches_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branches
    ADD CONSTRAINT branches_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: branches branches_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.branches
    ADD CONSTRAINT branches_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_allergies child_allergies_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_allergies child_allergies_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_allergies
    ADD CONSTRAINT child_allergies_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_attendance child_attendance_authorized_pickup_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_authorized_pickup_id_fkey FOREIGN KEY (authorized_pickup_id) REFERENCES public.authorized_pickups(id) ON DELETE SET NULL;


--
-- Name: child_attendance child_attendance_departure_verified_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT child_attendance_departure_verified_by_user_id_fkey FOREIGN KEY (departure_verified_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: child_care_events child_care_events_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_events child_care_events_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_events child_care_events_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_care_events child_care_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_events child_care_events_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_events child_care_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_events
    ADD CONSTRAINT child_care_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_care_plans child_care_plans_approved_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_approved_by_user_id_fkey FOREIGN KEY (approved_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_plans child_care_plans_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_plans child_care_plans_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_plans child_care_plans_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_plans child_care_plans_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_plans child_care_plans_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_plans
    ADD CONSTRAINT child_care_plans_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_care_supplies child_care_supplies_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_care_supplies child_care_supplies_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_care_supplies child_care_supplies_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_care_supplies child_care_supplies_last_reported_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_last_reported_by_user_id_fkey FOREIGN KEY (last_reported_by_user_id) REFERENCES public.users(id);


--
-- Name: child_care_supplies child_care_supplies_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_care_supplies child_care_supplies_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_care_supplies
    ADD CONSTRAINT child_care_supplies_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_communication_profiles child_communication_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_communication_profiles child_communication_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_communication_profiles
    ADD CONSTRAINT child_communication_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_consent_documents child_consent_documents_admin_reviewed_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_admin_reviewed_by_fkey FOREIGN KEY (admin_reviewed_by) REFERENCES public.users(id);


--
-- Name: child_consent_documents child_consent_documents_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_consent_documents child_consent_documents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_consent_documents child_consent_documents_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_consent_documents child_consent_documents_consent_template_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_consent_template_id_fkey FOREIGN KEY (consent_template_id) REFERENCES public.consent_templates(id);


--
-- Name: child_consent_documents child_consent_documents_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);


--
-- Name: child_consent_documents child_consent_documents_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_consent_documents child_consent_documents_supersedes_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_supersedes_document_id_fkey FOREIGN KEY (supersedes_document_id) REFERENCES public.child_consent_documents(id);


--
-- Name: child_consent_documents child_consent_documents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consent_documents
    ADD CONSTRAINT child_consent_documents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_consents child_consents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_consents child_consents_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.child_documents(id);


--
-- Name: child_consents child_consents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_consents
    ADD CONSTRAINT child_consents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_dietary_profiles child_dietary_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_dietary_profiles child_dietary_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_dietary_profiles
    ADD CONSTRAINT child_dietary_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_documents child_documents_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_documents child_documents_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_documents child_documents_uploaded_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_documents
    ADD CONSTRAINT child_documents_uploaded_by_fkey FOREIGN KEY (uploaded_by) REFERENCES public.users(id);


--
-- Name: child_enrollment_histories child_enrollment_histories_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_enrollment_histories child_enrollment_histories_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_enrollment_histories child_enrollment_histories_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_enrollment_histories
    ADD CONSTRAINT child_enrollment_histories_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_feeding_profiles child_feeding_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_feeding_profiles child_feeding_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_feeding_profiles
    ADD CONSTRAINT child_feeding_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_food_restrictions child_food_restrictions_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_food_restrictions child_food_restrictions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_food_restrictions
    ADD CONSTRAINT child_food_restrictions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_guardians child_guardians_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_guardians child_guardians_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id) ON DELETE CASCADE;


--
-- Name: child_guardians child_guardians_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_guardians
    ADD CONSTRAINT child_guardians_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_interests child_interests_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_interests
    ADD CONSTRAINT child_interests_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_journey_developments child_journey_developments_development_area_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_development_area_id_fkey FOREIGN KEY (development_area_id) REFERENCES public.development_areas(id) ON DELETE CASCADE;


--
-- Name: child_journey_developments child_journey_developments_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_developments
    ADD CONSTRAINT child_journey_developments_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE CASCADE;


--
-- Name: child_journey_entries child_journey_entries_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_journey_entries child_journey_entries_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_participation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_participation_id_fkey FOREIGN KEY (experience_participation_id) REFERENCES public.experience_participations(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE SET NULL;


--
-- Name: child_journey_entries child_journey_entries_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: child_journey_entries child_journey_entries_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_entries
    ADD CONSTRAINT child_journey_entries_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_journey_evidence child_journey_evidence_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_evidence
    ADD CONSTRAINT child_journey_evidence_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE CASCADE;


--
-- Name: child_journey_insight_items child_journey_insight_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_items
    ADD CONSTRAINT child_journey_insight_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.child_journey_insight_requests(id) ON DELETE CASCADE;


--
-- Name: child_journey_insight_requests child_journey_insight_requests_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_journey_insight_requests
    ADD CONSTRAINT child_journey_insight_requests_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_legal_profiles child_legal_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_legal_profiles child_legal_profiles_court_order_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_court_order_document_id_fkey FOREIGN KEY (court_order_document_id) REFERENCES public.child_documents(id);


--
-- Name: child_legal_profiles child_legal_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_legal_profiles
    ADD CONSTRAINT child_legal_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_medical_profiles child_medical_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_medical_profiles child_medical_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medical_profiles
    ADD CONSTRAINT child_medical_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_medications child_medications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_medications child_medications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_medications
    ADD CONSTRAINT child_medications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_milestones child_milestones_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_milestones child_milestones_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_development_area_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_development_area_id_fkey FOREIGN KEY (development_area_id) REFERENCES public.development_areas(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_journey_entry_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_journey_entry_id_fkey FOREIGN KEY (journey_entry_id) REFERENCES public.child_journey_entries(id) ON DELETE SET NULL;


--
-- Name: child_milestones child_milestones_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: child_milestones child_milestones_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_milestones
    ADD CONSTRAINT child_milestones_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_nap_sessions child_nap_sessions_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_nap_sessions child_nap_sessions_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_nap_sessions child_nap_sessions_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_nap_sessions child_nap_sessions_ended_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_ended_by_user_id_fkey FOREIGN KEY (ended_by_user_id) REFERENCES public.users(id);


--
-- Name: child_nap_sessions child_nap_sessions_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: child_nap_sessions child_nap_sessions_started_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_started_by_user_id_fkey FOREIGN KEY (started_by_user_id) REFERENCES public.users(id);


--
-- Name: child_nap_sessions child_nap_sessions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_nap_sessions
    ADD CONSTRAINT child_nap_sessions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_person_restrictions child_person_restrictions_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_person_restrictions
    ADD CONSTRAINT child_person_restrictions_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_person_restrictions child_person_restrictions_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_person_restrictions
    ADD CONSTRAINT child_person_restrictions_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id);


--
-- Name: child_person_restrictions child_person_restrictions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_person_restrictions
    ADD CONSTRAINT child_person_restrictions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_relationships child_relationships_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_relationships child_relationships_related_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_related_child_id_fkey FOREIGN KEY (related_child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_relationships child_relationships_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_relationships
    ADD CONSTRAINT child_relationships_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_routines child_routines_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_routines child_routines_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_routines
    ADD CONSTRAINT child_routines_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_schedules child_schedules_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_schedules child_schedules_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_schedules
    ADD CONSTRAINT child_schedules_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_sleep_profiles child_sleep_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_sleep_profiles
    ADD CONSTRAINT child_sleep_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_sleep_profiles child_sleep_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_sleep_profiles
    ADD CONSTRAINT child_sleep_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_stories child_stories_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_stories child_stories_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_stories
    ADD CONSTRAINT child_stories_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: child_support_profiles child_support_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_support_profiles child_support_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_support_profiles
    ADD CONSTRAINT child_support_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_toileting_profiles child_toileting_profiles_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_toileting_profiles child_toileting_profiles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_toileting_profiles
    ADD CONSTRAINT child_toileting_profiles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: child_transportations child_transportations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: child_transportations child_transportations_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_transportations
    ADD CONSTRAINT child_transportations_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: children children_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: children children_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.children
    ADD CONSTRAINT children_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: classroom_statutory_ratios classroom_statutory_ratios_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_statutory_ratios
    ADD CONSTRAINT classroom_statutory_ratios_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: classroom_teachers classroom_teachers_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: classroom_teachers classroom_teachers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: classroom_teachers classroom_teachers_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classroom_teachers
    ADD CONSTRAINT classroom_teachers_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: classrooms classrooms_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: classrooms classrooms_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.classrooms
    ADD CONSTRAINT classrooms_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: consent_audit_events consent_audit_events_actor_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_actor_user_id_fkey FOREIGN KEY (actor_user_id) REFERENCES public.users(id);


--
-- Name: consent_audit_events consent_audit_events_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_audit_events consent_audit_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: consent_audit_events consent_audit_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_audit_events
    ADD CONSTRAINT consent_audit_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: consent_choices consent_choices_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_choices consent_choices_consent_signer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_consent_signer_id_fkey FOREIGN KEY (consent_signer_id) REFERENCES public.consent_signers(id) ON DELETE CASCADE;


--
-- Name: consent_choices consent_choices_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_choices
    ADD CONSTRAINT consent_choices_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: consent_signers consent_signers_consent_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_consent_document_id_fkey FOREIGN KEY (consent_document_id) REFERENCES public.child_consent_documents(id) ON DELETE CASCADE;


--
-- Name: consent_signers consent_signers_family_relationship_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_family_relationship_id_fkey FOREIGN KEY (family_relationship_id) REFERENCES public.family_relationships(id);


--
-- Name: consent_signers consent_signers_intended_signer_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_intended_signer_user_id_fkey FOREIGN KEY (intended_signer_user_id) REFERENCES public.users(id);


--
-- Name: consent_signers consent_signers_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.persons(id);


--
-- Name: consent_signers consent_signers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_signers
    ADD CONSTRAINT consent_signers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: consent_templates consent_templates_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);


--
-- Name: consent_templates consent_templates_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: consent_templates consent_templates_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.consent_templates
    ADD CONSTRAINT consent_templates_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: development_areas development_areas_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.development_areas
    ADD CONSTRAINT development_areas_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: emergency_contacts emergency_contacts_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: emergency_contacts emergency_contacts_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.emergency_contacts
    ADD CONSTRAINT emergency_contacts_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE SET NULL;


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_target_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_target_classroom_id_fkey FOREIGN KEY (target_classroom_id) REFERENCES public.classrooms(id);


--
-- Name: enrollment_pipeline_applications enrollment_pipeline_applications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollment_pipeline_applications
    ADD CONSTRAINT enrollment_pipeline_applications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: enrollments enrollments_assigned_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_assigned_teacher_id_fkey FOREIGN KEY (assigned_teacher_id) REFERENCES public.users(id);


--
-- Name: enrollments enrollments_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: enrollments enrollments_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: enrollments enrollments_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: enrollments enrollments_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.enrollments
    ADD CONSTRAINT enrollments_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: event_classrooms event_classrooms_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: event_classrooms event_classrooms_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_classrooms
    ADD CONSTRAINT event_classrooms_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_experiences event_experiences_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_experiences event_experiences_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id);


--
-- Name: event_experiences event_experiences_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id);


--
-- Name: event_experiences event_experiences_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_experiences
    ADD CONSTRAINT event_experiences_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id);


--
-- Name: event_rsvps event_rsvps_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: event_rsvps event_rsvps_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_rsvps event_rsvps_guardian_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_rsvps
    ADD CONSTRAINT event_rsvps_guardian_id_fkey FOREIGN KEY (guardian_id) REFERENCES public.users(id);


--
-- Name: event_tasks event_tasks_event_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_event_id_fkey FOREIGN KEY (event_id) REFERENCES public.events(id) ON DELETE CASCADE;


--
-- Name: event_tasks event_tasks_owner_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_tasks
    ADD CONSTRAINT event_tasks_owner_id_fkey FOREIGN KEY (owner_id) REFERENCES public.users(id);


--
-- Name: event_types event_types_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.event_types
    ADD CONSTRAINT event_types_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: events events_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE SET NULL;


--
-- Name: events events_event_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_event_type_id_fkey FOREIGN KEY (event_type_id) REFERENCES public.event_types(id);


--
-- Name: events events_organizer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_organizer_id_fkey FOREIGN KEY (organizer_id) REFERENCES public.users(id);


--
-- Name: events events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: experience_audit_events experience_audit_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: experience_audit_events experience_audit_events_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_audit_events
    ADD CONSTRAINT experience_audit_events_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: experience_feedback experience_feedback_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_feedback
    ADD CONSTRAINT experience_feedback_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE CASCADE;


--
-- Name: experience_harvest_requests experience_harvest_requests_license_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_license_id_fkey FOREIGN KEY (license_id) REFERENCES public.experience_licenses(id) ON DELETE SET NULL;


--
-- Name: experience_harvest_requests experience_harvest_requests_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: experience_harvest_requests experience_harvest_requests_reviewed_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_reviewed_by_user_id_fkey FOREIGN KEY (reviewed_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_harvest_requests experience_harvest_requests_source_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_source_experience_id_fkey FOREIGN KEY (source_experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_harvest_requests experience_harvest_requests_submitted_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_submitted_by_user_id_fkey FOREIGN KEY (submitted_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_harvest_requests experience_harvest_requests_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_harvest_requests
    ADD CONSTRAINT experience_harvest_requests_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: experience_licenses experience_licenses_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_licenses experience_licenses_granted_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_granted_by_user_id_fkey FOREIGN KEY (granted_by_user_id) REFERENCES public.users(id);


--
-- Name: experience_licenses experience_licenses_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_licenses
    ADD CONSTRAINT experience_licenses_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: experience_participations experience_participations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: experience_participations experience_participations_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_participations
    ADD CONSTRAINT experience_participations_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE CASCADE;


--
-- Name: experience_schedules experience_schedules_primary_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT experience_schedules_primary_teacher_id_fkey FOREIGN KEY (primary_teacher_id) REFERENCES public.teachers(id) ON DELETE SET NULL;


--
-- Name: experience_session_teachers experience_session_teachers_experience_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_experience_session_id_fkey FOREIGN KEY (experience_session_id) REFERENCES public.experience_sessions(id) ON DELETE CASCADE;


--
-- Name: experience_session_teachers experience_session_teachers_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_session_teachers
    ADD CONSTRAINT experience_session_teachers_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.users(id);


--
-- Name: experience_sessions experience_sessions_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: experience_sessions experience_sessions_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id);


--
-- Name: experience_sessions experience_sessions_experience_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_experience_schedule_id_fkey FOREIGN KEY (experience_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: experience_sessions experience_sessions_primary_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_primary_teacher_id_fkey FOREIGN KEY (primary_teacher_id) REFERENCES public.users(id);


--
-- Name: experience_sessions experience_sessions_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_sessions
    ADD CONSTRAINT experience_sessions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: experience_versions experience_versions_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_versions experience_versions_modified_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_versions
    ADD CONSTRAINT experience_versions_modified_by_user_id_fkey FOREIGN KEY (modified_by_user_id) REFERENCES public.users(id);


--
-- Name: experiences experiences_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: experiences experiences_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: experiences experiences_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experiences
    ADD CONSTRAINT experiences_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: family_billing_contracts family_billing_contracts_application_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_application_id_fkey FOREIGN KEY (application_id) REFERENCES public.enrollment_pipeline_applications(id) ON DELETE SET NULL;


--
-- Name: family_billing_contracts family_billing_contracts_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: family_billing_contracts family_billing_contracts_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: family_billing_contracts family_billing_contracts_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_billing_contracts
    ADD CONSTRAINT family_billing_contracts_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: family_relationships family_relationships_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: family_relationships family_relationships_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: family_relationships family_relationships_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.family_relationships
    ADD CONSTRAINT family_relationships_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: teacher_availabilities fk_availability_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_availabilities
    ADD CONSTRAINT fk_availability_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: child_attendance fk_ca_branch; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_branch FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: child_attendance fk_ca_child; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_child FOREIGN KEY (child_id) REFERENCES public.children(id);


--
-- Name: child_attendance fk_ca_classroom; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_classroom FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: child_attendance fk_ca_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.child_attendance
    ADD CONSTRAINT fk_ca_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: teacher_certifications fk_certification_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_certifications
    ADD CONSTRAINT fk_certification_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_documents fk_doc_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_documents
    ADD CONSTRAINT fk_doc_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_education fk_education_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_education
    ADD CONSTRAINT fk_education_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_emergency_contacts fk_emergency_contact_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_emergency_contacts
    ADD CONSTRAINT fk_emergency_contact_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_employments fk_employment_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_employments
    ADD CONSTRAINT fk_employment_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: experience_schedules fk_experience_schedules_classroom; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_classroom FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: experience_schedules fk_experience_schedules_experience; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_experience FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE CASCADE;


--
-- Name: experience_schedules fk_experience_schedules_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.experience_schedules
    ADD CONSTRAINT fk_experience_schedules_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: teacher_experiences fk_experience_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_experiences
    ADD CONSTRAINT fk_experience_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_languages fk_language_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_languages
    ADD CONSTRAINT fk_language_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_observations fk_observation_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_observations
    ADD CONSTRAINT fk_observation_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_professional_developments fk_pd_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_professional_developments
    ADD CONSTRAINT fk_pd_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_preferences fk_preference_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_preferences
    ADD CONSTRAINT fk_preference_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_schedules fk_schedule_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_schedules
    ADD CONSTRAINT fk_schedule_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: skills fk_skills_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.skills
    ADD CONSTRAINT fk_skills_tenant FOREIGN KEY (tenant_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: teacher_specialties fk_specialty_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_specialties
    ADD CONSTRAINT fk_specialty_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teacher_attendance fk_ta_branch; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_branch FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: teacher_attendance fk_ta_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id);


--
-- Name: teacher_attendance fk_ta_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_attendance
    ADD CONSTRAINT fk_ta_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: teacher_skills fk_teacher_skills_skill; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT fk_teacher_skills_skill FOREIGN KEY (skill_id) REFERENCES public.skills(id) ON DELETE CASCADE;


--
-- Name: teacher_skills fk_teacher_skills_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_skills
    ADD CONSTRAINT fk_teacher_skills_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: teachers fk_teacher_tenant; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teachers
    ADD CONSTRAINT fk_teacher_tenant FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: teacher_trainings fk_training_teacher; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_trainings
    ADD CONSTRAINT fk_training_teacher FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: floor_map_markers floor_map_markers_floor_map_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floor_map_markers
    ADD CONSTRAINT floor_map_markers_floor_map_id_fkey FOREIGN KEY (floor_map_id) REFERENCES public.branch_floor_maps(id) ON DELETE CASCADE;


--
-- Name: inventory_balances inventory_balances_location_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_location_id_fkey FOREIGN KEY (location_id) REFERENCES public.inventory_locations(id) ON DELETE CASCADE;


--
-- Name: inventory_balances inventory_balances_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_balances
    ADD CONSTRAINT inventory_balances_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- Name: inventory_ledger inventory_ledger_location_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_location_id_fkey FOREIGN KEY (location_id) REFERENCES public.inventory_locations(id) ON DELETE CASCADE;


--
-- Name: inventory_ledger inventory_ledger_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.inventory_ledger
    ADD CONSTRAINT inventory_ledger_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- Name: invoice_line_items invoice_line_items_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoice_line_items
    ADD CONSTRAINT invoice_line_items_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id) ON DELETE CASCADE;


--
-- Name: invoices invoices_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: invoices invoices_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invoices
    ADD CONSTRAINT invoices_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: marketplace_orders marketplace_orders_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: marketplace_orders marketplace_orders_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- Name: marketplace_orders marketplace_orders_purchased_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_purchased_by_user_id_fkey FOREIGN KEY (purchased_by_user_id) REFERENCES public.users(id);


--
-- Name: marketplace_orders marketplace_orders_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.marketplace_orders
    ADD CONSTRAINT marketplace_orders_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: material_loans material_loans_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: material_loans material_loans_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_loans material_loans_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_loans material_loans_order_item_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_order_item_id_fkey FOREIGN KEY (order_item_id) REFERENCES public.material_order_items(id) ON DELETE CASCADE;


--
-- Name: material_loans material_loans_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_loans
    ADD CONSTRAINT material_loans_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: material_offerings material_offerings_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_offerings
    ADD CONSTRAINT material_offerings_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id) ON DELETE CASCADE;


--
-- Name: material_order_history material_order_history_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_history
    ADD CONSTRAINT material_order_history_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_order_items material_order_items_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_order_items material_order_items_offering_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_offering_id_fkey FOREIGN KEY (offering_id) REFERENCES public.material_offerings(id);


--
-- Name: material_order_items material_order_items_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_order_items
    ADD CONSTRAINT material_order_items_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_orders material_orders_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: material_orders material_orders_planned_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_planned_experience_id_fkey FOREIGN KEY (planned_experience_id) REFERENCES public.experiences(id) ON DELETE SET NULL;


--
-- Name: material_orders material_orders_requesting_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_requesting_user_id_fkey FOREIGN KEY (requesting_user_id) REFERENCES public.users(id);


--
-- Name: material_orders material_orders_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_orders
    ADD CONSTRAINT material_orders_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: material_recurring_schedules material_recurring_schedules_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_offering_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_offering_id_fkey FOREIGN KEY (offering_id) REFERENCES public.material_offerings(id);


--
-- Name: material_recurring_schedules material_recurring_schedules_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: material_recurring_schedules material_recurring_schedules_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_recurring_schedules
    ADD CONSTRAINT material_recurring_schedules_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: material_return_inspections material_return_inspections_inspected_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_inspected_by_user_id_fkey FOREIGN KEY (inspected_by_user_id) REFERENCES public.users(id);


--
-- Name: material_return_inspections material_return_inspections_loan_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_loan_id_fkey FOREIGN KEY (loan_id) REFERENCES public.material_loans(id) ON DELETE CASCADE;


--
-- Name: material_return_inspections material_return_inspections_material_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_material_product_id_fkey FOREIGN KEY (material_product_id) REFERENCES public.material_products(id);


--
-- Name: material_return_inspections material_return_inspections_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_return_inspections
    ADD CONSTRAINT material_return_inspections_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id);


--
-- Name: material_shipments material_shipments_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.material_orders(id) ON DELETE CASCADE;


--
-- Name: material_shipments material_shipments_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.material_shipments
    ADD CONSTRAINT material_shipments_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id);


--
-- Name: medication_administrations medication_administrations_administered_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_administered_by_fkey FOREIGN KEY (administered_by) REFERENCES public.users(id);


--
-- Name: medication_administrations medication_administrations_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: medication_administrations medication_administrations_medication_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_medication_id_fkey FOREIGN KEY (medication_id) REFERENCES public.child_medications(id) ON DELETE CASCADE;


--
-- Name: medication_administrations medication_administrations_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.medication_administrations
    ADD CONSTRAINT medication_administrations_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: organization_billing_settings organization_billing_settings_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_billing_settings organization_billing_settings_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_billing_settings
    ADD CONSTRAINT organization_billing_settings_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: organization_entitlements organization_entitlements_order_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_order_id_fkey FOREIGN KEY (order_id) REFERENCES public.marketplace_orders(id) ON DELETE SET NULL;


--
-- Name: organization_entitlements organization_entitlements_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_entitlements organization_entitlements_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- Name: organization_entitlements organization_entitlements_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_entitlements
    ADD CONSTRAINT organization_entitlements_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: organization_intelligence_insights organization_intelligence_insights_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE CASCADE;


--
-- Name: organization_intelligence_insights organization_intelligence_insights_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: organization_intelligence_insights organization_intelligence_insights_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: organization_intelligence_insights organization_intelligence_insights_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_intelligence_insights
    ADD CONSTRAINT organization_intelligence_insights_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: organization_memberships organization_memberships_organization_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_memberships
    ADD CONSTRAINT organization_memberships_organization_id_tenant_id_fkey FOREIGN KEY (organization_id, tenant_id) REFERENCES public.organizations(id, tenant_id) ON DELETE CASCADE;


--
-- Name: organization_memberships organization_memberships_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organization_memberships
    ADD CONSTRAINT organization_memberships_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: organizations organizations_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT organizations_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: parent_communications parent_communications_child_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_child_id_fkey FOREIGN KEY (child_id) REFERENCES public.children(id) ON DELETE CASCADE;


--
-- Name: parent_communications parent_communications_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id);


--
-- Name: parent_communications parent_communications_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id);


--
-- Name: parent_communications parent_communications_recipient_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_recipient_person_id_fkey FOREIGN KEY (recipient_person_id) REFERENCES public.persons(id);


--
-- Name: parent_communications parent_communications_recipient_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_recipient_user_id_fkey FOREIGN KEY (recipient_user_id) REFERENCES public.users(id);


--
-- Name: parent_communications parent_communications_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.parent_communications
    ADD CONSTRAINT parent_communications_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: payments payments_invoice_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id) ON DELETE SET NULL;


--
-- Name: payments payments_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: payments payments_recorded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_recorded_by_user_id_fkey FOREIGN KEY (recorded_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: payments payments_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.payments
    ADD CONSTRAINT payments_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: persons persons_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: persons persons_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.persons
    ADD CONSTRAINT persons_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: platform_experience_versions platform_experience_versions_platform_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experience_versions
    ADD CONSTRAINT platform_experience_versions_platform_experience_id_fkey FOREIGN KEY (platform_experience_id) REFERENCES public.platform_experiences(id) ON DELETE CASCADE;


--
-- Name: platform_experiences platform_experiences_license_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_license_id_fkey FOREIGN KEY (license_id) REFERENCES public.experience_licenses(id) ON DELETE SET NULL;


--
-- Name: platform_experiences platform_experiences_source_harvest_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.platform_experiences
    ADD CONSTRAINT platform_experiences_source_harvest_request_id_fkey FOREIGN KEY (source_harvest_request_id) REFERENCES public.experience_harvest_requests(id) ON DELETE SET NULL;


--
-- Name: role_permissions role_permissions_permission_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_permission_id_fkey FOREIGN KEY (permission_id) REFERENCES public.permissions(id);


--
-- Name: role_permissions role_permissions_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: roles roles_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: staff_leaves staff_leaves_reviewed_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_reviewed_by_user_id_fkey FOREIGN KEY (reviewed_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: staff_leaves staff_leaves_substitute_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_substitute_teacher_id_fkey FOREIGN KEY (substitute_teacher_id) REFERENCES public.teachers(id) ON DELETE SET NULL;


--
-- Name: staff_leaves staff_leaves_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_teacher_id_fkey FOREIGN KEY (teacher_id) REFERENCES public.teachers(id) ON DELETE CASCADE;


--
-- Name: staff_leaves staff_leaves_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff_leaves
    ADD CONSTRAINT staff_leaves_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: support_access_grants support_access_grants_granted_to_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_granted_to_user_id_fkey FOREIGN KEY (granted_to_user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: support_access_grants support_access_grants_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.support_access_grants
    ADD CONSTRAINT support_access_grants_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_acknowledged_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_acknowledged_by_user_id_fkey FOREIGN KEY (acknowledged_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: teacher_handovers teacher_handovers_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_incoming_teacher_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_incoming_teacher_user_id_fkey FOREIGN KEY (incoming_teacher_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: teacher_handovers teacher_handovers_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_outgoing_teacher_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_outgoing_teacher_user_id_fkey FOREIGN KEY (outgoing_teacher_user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: teacher_handovers teacher_handovers_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.teacher_handovers
    ADD CONSTRAINT teacher_handovers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id) ON DELETE CASCADE;


--
-- Name: user_branch_assignments user_branch_assignments_branch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_branch_assignments
    ADD CONSTRAINT user_branch_assignments_branch_id_fkey FOREIGN KEY (branch_id) REFERENCES public.branches(id) ON DELETE CASCADE;


--
-- Name: user_branch_assignments user_branch_assignments_organization_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_branch_assignments
    ADD CONSTRAINT user_branch_assignments_organization_id_tenant_id_fkey FOREIGN KEY (organization_id, tenant_id) REFERENCES public.organizations(id, tenant_id) ON DELETE CASCADE;


--
-- Name: user_branch_assignments user_branch_assignments_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_branch_assignments
    ADD CONSTRAINT user_branch_assignments_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: user_external_logins user_external_logins_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_external_logins
    ADD CONSTRAINT user_external_logins_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: user_organization_roles user_organization_roles_role_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_role_id_tenant_id_fkey FOREIGN KEY (role_id, tenant_id) REFERENCES public.roles(id, tenant_id) ON DELETE CASCADE;


--
-- Name: user_organization_roles user_organization_roles_user_id_organization_id_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_organization_roles
    ADD CONSTRAINT user_organization_roles_user_id_organization_id_tenant_id_fkey FOREIGN KEY (user_id, organization_id, tenant_id) REFERENCES public.organization_memberships(user_id, organization_id, tenant_id) ON DELETE CASCADE;


--
-- Name: user_roles user_roles_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: user_roles user_roles_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: users users_tenant_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_experience_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_experience_id_fkey FOREIGN KEY (experience_id) REFERENCES public.experiences(id) ON DELETE RESTRICT;


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_request_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_request_id_fkey FOREIGN KEY (request_id) REFERENCES public.weekly_planning_draft_requests(id) ON DELETE CASCADE;


--
-- Name: weekly_planning_draft_items weekly_planning_draft_items_resulting_schedule_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_items
    ADD CONSTRAINT weekly_planning_draft_items_resulting_schedule_id_fkey FOREIGN KEY (resulting_schedule_id) REFERENCES public.experience_schedules(id) ON DELETE SET NULL;


--
-- Name: weekly_planning_draft_requests weekly_planning_draft_requests_classroom_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.weekly_planning_draft_requests
    ADD CONSTRAINT weekly_planning_draft_requests_classroom_id_fkey FOREIGN KEY (classroom_id) REFERENCES public.classrooms(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict LTCoGMcAl9xbIfsxlldfDnXsY5a86BPKYuYCUohJdHNeWXGbqKJ4xtuTweb05pz

