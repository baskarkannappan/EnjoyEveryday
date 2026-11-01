-- 091_add_agent_draft_approvals.sql
-- Enables human-in-the-loop review for AI agent proposed parent communications and logs

ALTER TABLE parent_communications 
    ADD COLUMN IF NOT EXISTS is_agent_draft BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS agent_token_id UUID NULL REFERENCES agent_personal_access_tokens(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_parent_comms_agent_draft ON parent_communications(tenant_id, is_agent_draft, status);
