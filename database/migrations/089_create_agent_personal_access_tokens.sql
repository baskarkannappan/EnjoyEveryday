-- 089_create_agent_personal_access_tokens.sql
-- Personal Access Tokens (PAT) for autonomous AI agents connecting to EnjoyEveryday

CREATE TABLE IF NOT EXISTS agent_personal_access_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_name VARCHAR(100) NOT NULL,
    token_prefix VARCHAR(32) NOT NULL,
    token_hash VARCHAR(128) NOT NULL UNIQUE,
    role VARCHAR(50) NOT NULL,
    scopes TEXT[] NOT NULL DEFAULT '{}',
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at TIMESTAMPTZ NULL,
    last_used_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_agent_tokens_hash ON agent_personal_access_tokens(token_hash) WHERE is_revoked = FALSE;
CREATE INDEX IF NOT EXISTS idx_agent_tokens_tenant_user ON agent_personal_access_tokens(tenant_id, user_id);
