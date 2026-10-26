-- Migration: 069_child_journey_insight_query_procedures.sql
-- Description: Stored procedures using refcursors for querying child journey insights.

CREATE OR REPLACE PROCEDURE sp_get_latest_child_journey_insight(
    p_tenant_id UUID,
    p_child_id UUID,
    INOUT cur refcursor = 'cur'
)
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

CREATE OR REPLACE PROCEDURE sp_get_child_journey_insight_request_by_id(
    p_tenant_id UUID,
    p_request_id UUID,
    INOUT cur refcursor = 'cur'
)
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

CREATE OR REPLACE PROCEDURE sp_get_child_journey_insight_items(
    p_tenant_id UUID,
    p_request_id UUID,
    INOUT cur refcursor = 'cur'
)
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
