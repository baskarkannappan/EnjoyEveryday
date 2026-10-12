-- ============================================================
-- EnjoyEveryday — Migration 041: AI Parent Communication Assistant
-- Stores teacher-approved parent communication drafts, evidence summaries,
-- guardian authorization status, and delivery tracking.
-- ============================================================

CREATE TABLE IF NOT EXISTS parent_communications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    classroom_id UUID REFERENCES classrooms(id),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    recipient_person_id UUID REFERENCES persons(id),
    recipient_user_id UUID REFERENCES users(id),
    recipient_email VARCHAR(255),
    recipient_name VARCHAR(255),
    recipient_relationship VARCHAR(100),
    communication_type VARCHAR(50) NOT NULL,
    reporting_date_start DATE NOT NULL,
    reporting_date_end DATE NOT NULL,
    title VARCHAR(255) NOT NULL,
    content_draft TEXT NOT NULL,
    content_final TEXT NOT NULL,
    family_conversation_starter TEXT,
    evidence_summary_json JSONB,
    consent_verified BOOLEAN NOT NULL DEFAULT TRUE,
    consent_notes VARCHAR(500),
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    teacher_id UUID NOT NULL,
    teacher_name VARCHAR(255),
    approved_by_user_id UUID,
    approved_at TIMESTAMP WITH TIME ZONE,
    sent_at TIMESTAMP WITH TIME ZONE,
    delivery_channel VARCHAR(50) NOT NULL DEFAULT 'PortalAndEmail',
    delivery_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    delivery_error TEXT,
    idempotency_key VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_parent_comms_tenant_child ON parent_communications(tenant_id, child_id);
CREATE INDEX IF NOT EXISTS idx_parent_comms_tenant_status ON parent_communications(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_parent_comms_recipient ON parent_communications(recipient_user_id);
