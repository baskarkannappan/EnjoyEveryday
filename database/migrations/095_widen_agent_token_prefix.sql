-- 095_widen_agent_token_prefix.sql
-- Widen token_prefix from VARCHAR(16) to VARCHAR(32) to accommodate prefix strings like 'eed_pat_xxxxxxxx...'

ALTER TABLE agent_personal_access_tokens 
ALTER COLUMN token_prefix TYPE VARCHAR(32);
