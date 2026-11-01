-- 090_create_agent_webhook_subscriptions.sql
-- Subscriptions for external AI agents to receive real-time daycare domain events

CREATE TABLE IF NOT EXISTS agent_webhook_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    webhook_url TEXT NOT NULL,
    secret_key VARCHAR(128) NOT NULL,
    subscribed_events TEXT[] NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_triggered_at TIMESTAMPTZ NULL,
    failure_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_agent_webhooks_tenant ON agent_webhook_subscriptions(tenant_id) WHERE is_active = TRUE;
