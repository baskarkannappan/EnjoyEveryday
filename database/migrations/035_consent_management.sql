-- ============================================================
-- EnjoyEveryday — Migration 035: Electronic Consent Management Workflow
-- Creates tables for configurable templates, versioned documents,
-- signers, granular choices, and append-only audit trail.
-- ============================================================

-- 1. CONSENT TEMPLATES
CREATE TABLE IF NOT EXISTS consent_templates (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id),
    organization_id         UUID REFERENCES organizations(id),
    name                    VARCHAR(255) NOT NULL,
    description             TEXT,
    document_type           VARCHAR(100) NOT NULL,
    version                 VARCHAR(50) NOT NULL DEFAULT '1.0',
    template_content        TEXT NOT NULL,
    required_for_enrollment BOOLEAN NOT NULL DEFAULT TRUE,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_by              UUID REFERENCES users(id),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_consent_templates_tenant_org ON consent_templates(tenant_id, organization_id);
CREATE INDEX IF NOT EXISTS idx_consent_templates_doc_type ON consent_templates(tenant_id, document_type);

-- 2. CHILD CONSENT DOCUMENTS
CREATE TABLE IF NOT EXISTS child_consent_documents (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id),
    organization_id             UUID REFERENCES organizations(id),
    branch_id                   UUID REFERENCES branches(id),
    classroom_id                UUID REFERENCES classrooms(id),
    child_id                    UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    consent_template_id         UUID NOT NULL REFERENCES consent_templates(id),
    template_version            VARCHAR(50) NOT NULL,
    document_version            INT NOT NULL DEFAULT 1,
    document_title              VARCHAR(255) NOT NULL,
    document_snapshot           TEXT NOT NULL,
    status                      VARCHAR(50) NOT NULL DEFAULT 'Draft',
    signing_deadline            TIMESTAMPTZ,
    created_by                  UUID REFERENCES users(id),
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    admin_reviewed_by           UUID REFERENCES users(id),
    admin_reviewed_at           TIMESTAMPTZ,
    admin_review_notes          TEXT,
    approved_at                 TIMESTAMPTZ,
    sent_at                     TIMESTAMPTZ,
    signed_at                   TIMESTAMPTZ,
    declined_at                 TIMESTAMPTZ,
    withdrawn_at                TIMESTAMPTZ,
    signed_document_storage_key VARCHAR(500),
    signed_document_checksum    VARCHAR(128),
    supersedes_document_id      UUID REFERENCES child_consent_documents(id),
    row_version                 INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_child_consent_docs_child ON child_consent_documents(tenant_id, child_id);
CREATE INDEX IF NOT EXISTS idx_child_consent_docs_status ON child_consent_documents(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_child_consent_docs_org_branch ON child_consent_documents(organization_id, branch_id);

-- 3. CONSENT SIGNERS
CREATE TABLE IF NOT EXISTS consent_signers (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id),
    consent_document_id         UUID NOT NULL REFERENCES child_consent_documents(id) ON DELETE CASCADE,
    family_relationship_id      UUID REFERENCES family_relationships(id),
    person_id                   UUID REFERENCES persons(id),
    intended_signer_user_id     UUID REFERENCES users(id),
    signer_name_snapshot        VARCHAR(255) NOT NULL,
    signer_email_snapshot       VARCHAR(255) NOT NULL,
    signer_relationship         VARCHAR(100) NOT NULL,
    is_required                 BOOLEAN NOT NULL DEFAULT TRUE,
    signing_status              VARCHAR(50) NOT NULL DEFAULT 'Pending',
    token_hash                  VARCHAR(128),
    token_expires_at            TIMESTAMPTZ,
    invitation_sent_count       INT NOT NULL DEFAULT 0,
    last_invitation_sent_at     TIMESTAMPTZ,
    last_opened_at              TIMESTAMPTZ,
    signed_at                   TIMESTAMPTZ,
    signature_legal_name        VARCHAR(255),
    signature_intent_text       TEXT,
    signature_ip_address        VARCHAR(100),
    signature_user_agent        TEXT
);

CREATE INDEX IF NOT EXISTS idx_consent_signers_doc ON consent_signers(consent_document_id);
CREATE INDEX IF NOT EXISTS idx_consent_signers_token ON consent_signers(token_hash);

-- 4. CONSENT CHOICES
CREATE TABLE IF NOT EXISTS consent_choices (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id),
    consent_document_id         UUID NOT NULL REFERENCES child_consent_documents(id) ON DELETE CASCADE,
    consent_signer_id           UUID REFERENCES consent_signers(id) ON DELETE CASCADE,
    section_key                 VARCHAR(100) NOT NULL,
    section_title               VARCHAR(255) NOT NULL,
    is_mandatory                BOOLEAN NOT NULL DEFAULT FALSE,
    choice                      VARCHAR(50) NOT NULL,
    recorded_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_consent_choices_doc ON consent_choices(consent_document_id);

-- 5. CONSENT AUDIT EVENTS
CREATE TABLE IF NOT EXISTS consent_audit_events (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   UUID NOT NULL REFERENCES tenants(id),
    organization_id             UUID REFERENCES organizations(id),
    consent_document_id         UUID NOT NULL REFERENCES child_consent_documents(id) ON DELETE CASCADE,
    actor_type                  VARCHAR(50) NOT NULL,
    actor_user_id               UUID REFERENCES users(id),
    actor_name                  VARCHAR(255),
    event_type                  VARCHAR(100) NOT NULL,
    timestamp_utc               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    correlation_id              VARCHAR(100),
    safe_metadata               TEXT
);

CREATE INDEX IF NOT EXISTS idx_consent_audit_doc ON consent_audit_events(consent_document_id);
CREATE INDEX IF NOT EXISTS idx_consent_audit_tenant_time ON consent_audit_events(tenant_id, timestamp_utc DESC);

-- 6. SEED DEFAULT TEMPLATES (Valid Hex UUIDs)
INSERT INTO consent_templates (
    id, tenant_id, organization_id, name, description, document_type, version, template_content, required_for_enrollment, is_active
) VALUES
(
    'e0000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'General Daycare Agreement',
    'Comprehensive standard agreement covering enrollment terms, daycare policies, operating hours, and parental responsibilities.',
    'GeneralDaycareAgreement',
    '1.0',
    'This General Daycare Agreement outlines the standards, care commitments, attendance guidelines, and code of conduct between Little Stars Daycare and the enrolled child and family.',
    TRUE,
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'Emergency & Medical Information Acknowledgment',
    'Authorizes immediate medical assistance in life-threatening scenarios and acknowledges health records accuracy.',
    'EmergencyMedical',
    '1.0',
    'In case of emergency, staff will make every reasonable effort to contact guardians. If immediate treatment is required, staff are authorized to call emergency responders and administer first aid.',
    TRUE,
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'Photo & Video Consent',
    'Granular permissions for classroom observations, internal portfolio albums, and public daycare communications.',
    'PhotoVideoConsent',
    '1.0',
    'Little Stars respects child privacy. Please indicate your choices for classroom documentation, learning journey portfolios, and public communications.',
    FALSE,
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000004',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'Authorized Pickup & Release',
    'Designates legal guardians and emergency contacts with formal authority to collect the child from daycare premises.',
    'AuthorizedPickup',
    '1.0',
    'Only individuals explicitly authorized in this document and presenting valid government photo identification will be permitted to collect your child.',
    TRUE,
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000005',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'Field Trip & Outing Consent',
    'Authorizes neighborhood nature walks and supervised community excursions with qualified staff.',
    'FieldTrip',
    '1.0',
    'From time to time, children participate in structured walking trips to adjacent parks, community libraries, and nature trails under full staff supervision.',
    FALSE,
    TRUE
)
ON CONFLICT (id) DO NOTHING;

-- 7. SEED INITIAL SAMPLE CONSENT WORKFLOW DOCUMENTS FOR AARAV PATEL (Matches Dashboard Mockup)
-- Document 1: Photo & Video Consent (Awaiting Parent Review / Sent)
INSERT INTO child_consent_documents (
    id, tenant_id, organization_id, branch_id, classroom_id, child_id, consent_template_id,
    template_version, document_version, document_title, document_snapshot, status,
    signing_deadline, created_at, approved_at, sent_at
) VALUES (
    'd0000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000001',
    'e0000000-0000-0000-0000-000000000003',
    '1.0', 1, 'Photo and Video Consent',
    'Approved snapshot of photo and video permissions for Aarav Patel. Includes learning journey documentation and optional public media.',
    'SentToParent',
    NOW() + INTERVAL '7 days',
    NOW() - INTERVAL '2 days',
    NOW() - INTERVAL '1 day',
    NOW() - INTERVAL '1 day'
) ON CONFLICT (id) DO NOTHING;

-- Signer for Document 1 (Sunita Patel)
INSERT INTO consent_signers (
    id, tenant_id, consent_document_id, family_relationship_id, intended_signer_user_id,
    signer_name_snapshot, signer_email_snapshot, signer_relationship, is_required,
    signing_status, token_hash, token_expires_at, invitation_sent_count, last_invitation_sent_at
) VALUES (
    'b0000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    (SELECT id FROM family_relationships WHERE tenant_id = '10000000-0000-0000-0000-000000000001' LIMIT 1),
    '50000000-0000-0000-0000-000000000010',
    'Sunita Patel',
    'parent@littlestars.com',
    'Mother',
    TRUE,
    'Pending',
    -- SHA256 of mock test token "demo-photo-token-2026"
    'e975fb9c9bf656a8cfd7950c05fe9bf3115456f93796ec7e0569733075c2e17e',
    NOW() + INTERVAL '7 days',
    1,
    NOW() - INTERVAL '1 day'
) ON CONFLICT (id) DO NOTHING;

-- Choices for Document 1
INSERT INTO consent_choices (
    id, tenant_id, consent_document_id, consent_signer_id, section_key, section_title, is_mandatory, choice
) VALUES
(
    'c1000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'internal_learning_journey',
    'Classroom Learning Journey & Portfolio Photos',
    FALSE,
    'Granted'
),
(
    'c1000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'external_public_media',
    'Daycare Public Website & Social Media',
    FALSE,
    'Declined'
) ON CONFLICT (id) DO NOTHING;

-- Document 2: Authorized Pickup (Signed)
INSERT INTO child_consent_documents (
    id, tenant_id, organization_id, branch_id, classroom_id, child_id, consent_template_id,
    template_version, document_version, document_title, document_snapshot, status,
    signing_deadline, created_at, approved_at, sent_at, signed_at, signed_document_storage_key
) VALUES (
    'd0000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000001',
    'e0000000-0000-0000-0000-000000000004',
    '1.0', 1, 'Authorized Pickup and Release',
    'Formal authorization for Sunita Patel and designated emergency contacts to collect Aarav Patel from daycare.',
    'Signed',
    NOW() + INTERVAL '30 days',
    NOW() - INTERVAL '10 days',
    NOW() - INTERVAL '9 days',
    NOW() - INTERVAL '9 days',
    NOW() - INTERVAL '8 days',
    'signed_docs/c0000000-0000-0000-0000-000000000001/pickup_signed.json'
) ON CONFLICT (id) DO NOTHING;

-- Signer for Document 2
INSERT INTO consent_signers (
    id, tenant_id, consent_document_id, intended_signer_user_id,
    signer_name_snapshot, signer_email_snapshot, signer_relationship, is_required,
    signing_status, signed_at, signature_legal_name, signature_intent_text
) VALUES (
    'b0000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000002',
    '50000000-0000-0000-0000-000000000010',
    'Sunita Patel',
    'parent@littlestars.com',
    'Mother',
    TRUE,
    'Signed',
    NOW() - INTERVAL '8 days',
    'Sunita Patel',
    'I confirm that I have reviewed this document and intend to sign electronically.'
) ON CONFLICT (id) DO NOTHING;

-- Document 3: General Daycare Agreement (Pending Admin Review)
INSERT INTO child_consent_documents (
    id, tenant_id, organization_id, branch_id, classroom_id, child_id, consent_template_id,
    template_version, document_version, document_title, document_snapshot, status,
    signing_deadline, created_at
) VALUES (
    'd0000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000001',
    'c0000000-0000-0000-0000-000000000001',
    'e0000000-0000-0000-0000-000000000001',
    '1.0', 1, 'General Daycare Agreement',
    'Draft agreement for Aarav Patel prepared by enrollment coordinator, awaiting daycare administrator review.',
    'PendingAdminReview',
    NOW() + INTERVAL '14 days',
    NOW() - INTERVAL '3 hours'
) ON CONFLICT (id) DO NOTHING;

-- Audit trail events
INSERT INTO consent_audit_events (
    id, tenant_id, organization_id, consent_document_id, actor_type, actor_name, event_type, timestamp_utc, safe_metadata
) VALUES
(
    gen_random_uuid(), '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001', 'Admin', 'Admin User', 'Created', NOW() - INTERVAL '2 days', '{"version":1}'
),
(
    gen_random_uuid(), '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001', 'Admin', 'Admin User', 'Approved', NOW() - INTERVAL '1 day', '{"status":"ApprovedToSend"}'
),
(
    gen_random_uuid(), '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000001', 'System', 'ConsentEmailService', 'Sent', NOW() - INTERVAL '1 day', '{"recipient":"parent@littlestars.com"}'
),
(
    gen_random_uuid(), '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001',
    'd0000000-0000-0000-0000-000000000002', 'Parent', 'Sunita Patel', 'Signed', NOW() - INTERVAL '8 days', '{"signer":"Sunita Patel"}'
);
