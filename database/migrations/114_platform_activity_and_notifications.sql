-- Migration 114: Platform Business Events, Central Activity Ledger, Admin Notifications & Org Admin CC Audit
-- Enforces central visibility of commercial marketplace transactions, billing payments, and child onboarding events

-- 1. Central Platform Business Events Table
CREATE TABLE IF NOT EXISTS public.platform_business_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(64) NOT NULL,
    tenant_id UUID NOT NULL REFERENCES public.tenants(id),
    organization_id UUID NOT NULL REFERENCES public.organizations(id),
    branch_id UUID REFERENCES public.branches(id),
    initiated_by_user_id UUID REFERENCES public.users(id),
    transaction_reference VARCHAR(64),
    transaction_category VARCHAR(64) NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    amount NUMERIC(12,2),
    currency VARCHAR(10) DEFAULT 'INR',
    status VARCHAR(50) NOT NULL DEFAULT 'Completed',
    event_payload JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pbe_tenant_created ON public.platform_business_events (tenant_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_pbe_org_created ON public.platform_business_events (organization_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_pbe_category ON public.platform_business_events (transaction_category);
CREATE INDEX IF NOT EXISTS idx_pbe_event_type ON public.platform_business_events (event_type);
CREATE INDEX IF NOT EXISTS idx_pbe_tx_ref ON public.platform_business_events (transaction_reference);

-- 2. Platform Admin Notifications Table
CREATE TABLE IF NOT EXISTS public.platform_admin_notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_event_id UUID REFERENCES public.platform_business_events(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    action_url VARCHAR(300),
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pan_user_read ON public.platform_admin_notifications (user_id, is_read, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_pan_event ON public.platform_admin_notifications (business_event_id);

-- 3. Enhance email_notification_logs with CC recipients audit
ALTER TABLE public.email_notification_logs ADD COLUMN IF NOT EXISTS cc_recipients VARCHAR(500);

-- 4. Stored Function: Resolve active Organization Admin email for a given organization
CREATE OR REPLACE FUNCTION public.fn_get_org_admin_email_for_org(
    p_organization_id UUID
)
RETURNS TABLE (
    user_id UUID,
    email VARCHAR,
    display_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        u.id AS user_id,
        u.email::VARCHAR,
        COALESCE(u.display_name, u.first_name || ' ' || u.last_name, u.email)::VARCHAR AS display_name
    FROM public.user_organization_roles uor
    JOIN public.users u ON uor.user_id = u.id
    JOIN public.roles r ON uor.role_id = r.id
    WHERE uor.organization_id = p_organization_id 
      AND (r.name = 'OrganizationAdmin' OR r.name = 'OrgAdmin')
      AND u.is_active = TRUE
    ORDER BY u.created_at ASC
    LIMIT 1;
$$;

-- 5. Stored Function: Query Platform Business Events with filtering and pagination
CREATE OR REPLACE FUNCTION public.fn_get_platform_business_events(
    p_limit INT DEFAULT 50,
    p_offset INT DEFAULT 0,
    p_category VARCHAR DEFAULT NULL,
    p_event_type VARCHAR DEFAULT NULL,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    event_type VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    organization_name VARCHAR,
    branch_id UUID,
    branch_name VARCHAR,
    initiated_by_user_id UUID,
    initiator_name VARCHAR,
    transaction_reference VARCHAR,
    transaction_category VARCHAR,
    title VARCHAR,
    description TEXT,
    amount NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    event_payload JSONB,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        e.id,
        e.event_type::VARCHAR,
        e.tenant_id,
        e.organization_id,
        COALESCE(org.name, 'Unknown Organization')::VARCHAR AS organization_name,
        e.branch_id,
        COALESCE(b.name, 'All Branches')::VARCHAR AS branch_name,
        e.initiated_by_user_id,
        COALESCE(u.display_name, u.email, 'System')::VARCHAR AS initiator_name,
        e.transaction_reference::VARCHAR,
        e.transaction_category::VARCHAR,
        e.title::VARCHAR,
        e.description,
        e.amount,
        e.currency::VARCHAR,
        e.status::VARCHAR,
        e.event_payload,
        e.created_at
    FROM public.platform_business_events e
    JOIN public.organizations org ON e.organization_id = org.id
    LEFT JOIN public.branches b ON e.branch_id = b.id
    LEFT JOIN public.users u ON e.initiated_by_user_id = u.id
    WHERE (p_category IS NULL OR p_category = '' OR p_category = 'All' OR e.transaction_category = p_category)
      AND (p_event_type IS NULL OR p_event_type = '' OR p_event_type = 'All' OR e.event_type = p_event_type)
      AND (p_organization_id IS NULL OR e.organization_id = p_organization_id)
    ORDER BY e.created_at DESC
    LIMIT p_limit OFFSET p_offset;
$$;

-- 6. Stored Function: Query In-App Notifications for a Platform Admin User
CREATE OR REPLACE FUNCTION public.fn_get_platform_admin_notifications(
    p_user_id UUID,
    p_unread_only BOOLEAN DEFAULT FALSE,
    p_limit INT DEFAULT 30
)
RETURNS TABLE (
    id UUID,
    business_event_id UUID,
    title VARCHAR,
    message TEXT,
    action_url VARCHAR,
    is_read BOOLEAN,
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        n.id,
        n.business_event_id,
        n.title::VARCHAR,
        n.message,
        n.action_url::VARCHAR,
        n.is_read,
        n.read_at,
        n.created_at
    FROM public.platform_admin_notifications n
    WHERE (n.user_id = p_user_id OR n.user_id IS NULL)
      AND (NOT p_unread_only OR NOT n.is_read)
    ORDER BY n.created_at DESC
    LIMIT p_limit;
$$;

-- 7. Stored Procedure: Mark Notification as Read
CREATE OR REPLACE PROCEDURE public.sp_mark_platform_notification_read(
    p_notification_id UUID,
    p_user_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.platform_admin_notifications
    SET is_read = TRUE,
        read_at = NOW()
    WHERE id = p_notification_id
      AND (user_id = p_user_id OR user_id IS NULL);
END;
$$;

-- 8. Stored Procedure: Mark All Notifications as Read for User
CREATE OR REPLACE PROCEDURE public.sp_mark_all_platform_notifications_read(
    p_user_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.platform_admin_notifications
    SET is_read = TRUE,
        read_at = NOW()
    WHERE (user_id = p_user_id OR user_id IS NULL)
      AND is_read = FALSE;
END;
$$;

