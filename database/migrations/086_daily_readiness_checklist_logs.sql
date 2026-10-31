-- Migration 086: Daily Readiness Checklist Persistence
-- Adds daily_readiness_checklist_logs table and functions

CREATE TABLE IF NOT EXISTS public.daily_readiness_checklist_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    branch_id UUID,
    classroom_id UUID,
    check_date DATE NOT NULL,
    item_id VARCHAR(100) NOT NULL,
    is_completed BOOLEAN NOT NULL,
    updated_by_user_id UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_daily_readiness_item UNIQUE (tenant_id, organization_id, check_date, item_id)
);

CREATE INDEX IF NOT EXISTS idx_readiness_logs_lookup ON public.daily_readiness_checklist_logs (tenant_id, organization_id, check_date);

CREATE OR REPLACE FUNCTION public.fn_get_daily_readiness_logs(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_check_date DATE
)
RETURNS TABLE (
    item_id VARCHAR,
    is_completed BOOLEAN,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT item_id, is_completed, updated_at
    FROM public.daily_readiness_checklist_logs
    WHERE tenant_id = p_tenant_id
      AND organization_id = p_organization_id
      AND check_date = p_check_date;
$$;

CREATE OR REPLACE FUNCTION public.sp_upsert_daily_readiness_log(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_check_date DATE,
    p_item_id VARCHAR,
    p_is_completed BOOLEAN,
    p_user_id UUID
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.daily_readiness_checklist_logs (
        tenant_id, organization_id, branch_id, classroom_id, check_date, item_id, is_completed, updated_by_user_id, updated_at
    )
    VALUES (
        p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_check_date, p_item_id, p_is_completed, p_user_id, NOW()
    )
    ON CONFLICT (tenant_id, organization_id, check_date, item_id)
    DO UPDATE SET
        is_completed = EXCLUDED.is_completed,
        updated_by_user_id = EXCLUDED.updated_by_user_id,
        updated_at = NOW(),
        branch_id = COALESCE(EXCLUDED.branch_id, daily_readiness_checklist_logs.branch_id),
        classroom_id = COALESCE(EXCLUDED.classroom_id, daily_readiness_checklist_logs.classroom_id);
END;
$$;
