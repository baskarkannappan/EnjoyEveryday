-- Migration 111: Create email_notification_logs table for Azure Communication Services auditing
-- Date: 2026-11-11
-- Ecosystem: EnjoyEveryday Multi-Tenant Notification Architecture

CREATE TABLE IF NOT EXISTS email_notification_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    notification_type VARCHAR(64) NOT NULL,
    recipient_email VARCHAR(256) NOT NULL,
    status VARCHAR(24) NOT NULL, -- 'Sent', 'Skipped', 'Failed'
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    error_message TEXT NULL,
    metadata JSONB NULL
);

CREATE INDEX IF NOT EXISTS idx_email_notification_logs_tenant ON email_notification_logs(tenant_id, sent_at DESC);
CREATE INDEX IF NOT EXISTS idx_email_notification_logs_recipient ON email_notification_logs(recipient_email);

