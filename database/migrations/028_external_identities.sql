-- ============================================================
-- EnjoyEveryday — Migration 028: External Identities
-- Creates: user_external_logins
-- ============================================================

CREATE TABLE IF NOT EXISTS user_external_logins (
    provider        VARCHAR(100) NOT NULL,
    provider_key    VARCHAR(256) NOT NULL,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (provider, provider_key)
);

CREATE INDEX idx_user_external_logins_user ON user_external_logins(user_id);
