-- Migration 096: Fix fn_get_event_tasks return column type and sp_create_parent_communication organization_id fallback

-- 1. Fix fn_get_event_tasks: description in event_tasks is TEXT, matching return column type to TEXT
DROP FUNCTION IF EXISTS public.fn_get_event_tasks(UUID);
CREATE OR REPLACE FUNCTION public.fn_get_event_tasks(
    p_event_id UUID
)
RETURNS TABLE (
    id UUID,
    event_id UUID,
    description TEXT,
    owner_id UUID,
    due_date TIMESTAMPTZ,
    is_completed BOOLEAN,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
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

-- 2. Enhance sp_create_parent_communication: Resolve organization_id from classroom/branch or tenant if not provided (prevents FK violation)
CREATE OR REPLACE PROCEDURE public.sp_create_parent_communication(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_recipient_person_id UUID,
    p_recipient_user_id UUID,
    p_recipient_email VARCHAR,
    p_recipient_name VARCHAR,
    p_recipient_relationship VARCHAR,
    p_communication_type VARCHAR,
    p_reporting_date_start DATE,
    p_reporting_date_end DATE,
    p_title VARCHAR,
    p_content_draft TEXT,
    p_content_final TEXT,
    p_family_conversation_starter TEXT,
    p_evidence_summary_json JSONB,
    p_consent_verified BOOLEAN,
    p_consent_notes VARCHAR,
    p_status VARCHAR,
    p_teacher_id UUID,
    p_teacher_name VARCHAR,
    p_approved_by_user_id UUID,
    p_approved_at TIMESTAMPTZ,
    p_sent_at TIMESTAMPTZ,
    p_delivery_channel VARCHAR,
    p_delivery_status VARCHAR,
    p_delivery_error TEXT,
    p_idempotency_key VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- Fallback: resolve organization_id if null or empty
    IF p_organization_id IS NULL OR p_organization_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        IF p_classroom_id IS NOT NULL THEN
            SELECT b.organization_id INTO p_organization_id
            FROM classrooms c
            JOIN branches b ON b.id = c.branch_id
            WHERE c.id = p_classroom_id
            LIMIT 1;
        END IF;

        IF p_organization_id IS NULL OR p_organization_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
            SELECT id INTO p_organization_id
            FROM organizations
            WHERE tenant_id = p_tenant_id
            ORDER BY created_at ASC
            LIMIT 1;
        END IF;
    END IF;

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
