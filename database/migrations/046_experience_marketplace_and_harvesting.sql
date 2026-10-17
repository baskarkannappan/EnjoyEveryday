-- Migration 046: Experience Marketplace, Harvesting, Licensing, and Entitlements

CREATE TABLE IF NOT EXISTS experience_licenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    experience_id UUID NOT NULL REFERENCES experiences(id) ON DELETE CASCADE,
    license_type VARCHAR(50) NOT NULL DEFAULT 'CommercialRoyalty',
    grant_scope VARCHAR(150) NOT NULL DEFAULT 'Curate, Reproduce, Distribute, Modify and Monetize',
    granted_by_user_id UUID NOT NULL REFERENCES users(id),
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NULL,
    terms_document_hash VARCHAR(128) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_experience_licenses_exp ON experience_licenses(experience_id);
CREATE INDEX IF NOT EXISTS idx_experience_licenses_org ON experience_licenses(organization_id);

CREATE TABLE IF NOT EXISTS experience_harvest_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    source_experience_id UUID NOT NULL REFERENCES experiences(id) ON DELETE CASCADE,
    source_version_number INT NOT NULL DEFAULT 1,
    license_id UUID REFERENCES experience_licenses(id) ON DELETE SET NULL,
    submitted_by_user_id UUID NOT NULL REFERENCES users(id),
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(50) NOT NULL DEFAULT 'Submitted',
    review_notes TEXT NULL,
    reviewed_by_user_id UUID NULL REFERENCES users(id),
    reviewed_at TIMESTAMPTZ NULL,
    safety_suitability_verified BOOLEAN NOT NULL DEFAULT FALSE,
    learning_objectives_verified BOOLEAN NOT NULL DEFAULT FALSE,
    instructions_verified BOOLEAN NOT NULL DEFAULT FALSE,
    materials_verified BOOLEAN NOT NULL DEFAULT FALSE,
    media_rights_verified BOOLEAN NOT NULL DEFAULT FALSE,
    commercial_rights_verified BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_harvest_requests_org ON experience_harvest_requests(organization_id);
CREATE INDEX IF NOT EXISTS idx_harvest_requests_status ON experience_harvest_requests(status);
CREATE INDEX IF NOT EXISTS idx_harvest_requests_source ON experience_harvest_requests(source_experience_id);

CREATE TABLE IF NOT EXISTS platform_experiences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    category VARCHAR(100) NOT NULL DEFAULT 'General',
    age_range_min_months INT NULL,
    age_range_max_months INT NULL,
    duration_minutes INT NULL,
    learning_objectives JSONB NULL,
    required_materials JSONB NULL,
    instructions TEXT NULL,
    safety_notes TEXT NULL,
    dna_payload TEXT NOT NULL,
    pricing_model VARCHAR(50) NOT NULL DEFAULT 'IncludedInSubscription',
    price_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    publication_status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    ownership_type VARCHAR(50) NOT NULL DEFAULT 'CuratedLicensed',
    current_version INT NOT NULL DEFAULT 1,
    license_id UUID NULL REFERENCES experience_licenses(id) ON DELETE SET NULL,
    source_harvest_request_id UUID NULL REFERENCES experience_harvest_requests(id) ON DELETE SET NULL,
    source_experience_id UUID NULL,
    source_organization_id UUID NULL,
    creator_attribution VARCHAR(200) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_platform_exp_status ON platform_experiences(publication_status);
CREATE INDEX IF NOT EXISTS idx_platform_exp_cat ON platform_experiences(category);
CREATE INDEX IF NOT EXISTS idx_platform_exp_pricing ON platform_experiences(pricing_model);

CREATE TABLE IF NOT EXISTS platform_experience_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    platform_experience_id UUID NOT NULL REFERENCES platform_experiences(id) ON DELETE CASCADE,
    version_number INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    dna_payload TEXT NOT NULL,
    change_summary TEXT NULL,
    created_by_user_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_platform_exp_version UNIQUE(platform_experience_id, version_number)
);

CREATE INDEX IF NOT EXISTS idx_platform_exp_ver_exp ON platform_experience_versions(platform_experience_id);

CREATE TABLE IF NOT EXISTS marketplace_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    platform_experience_id UUID NOT NULL REFERENCES platform_experiences(id) ON DELETE CASCADE,
    purchased_by_user_id UUID NOT NULL REFERENCES users(id),
    pricing_model VARCHAR(50) NOT NULL,
    amount_paid NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    currency VARCHAR(10) NOT NULL DEFAULT 'INR',
    payment_status VARCHAR(50) NOT NULL DEFAULT 'Completed',
    transaction_reference VARCHAR(100) NOT NULL,
    purchased_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refunded_at TIMESTAMPTZ NULL,
    refund_reason TEXT NULL,
    CONSTRAINT uq_marketplace_orders_txn UNIQUE(organization_id, platform_experience_id, transaction_reference)
);

CREATE INDEX IF NOT EXISTS idx_marketplace_orders_org ON marketplace_orders(organization_id);
CREATE INDEX IF NOT EXISTS idx_marketplace_orders_exp ON marketplace_orders(platform_experience_id);

CREATE TABLE IF NOT EXISTS organization_entitlements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    platform_experience_id UUID NOT NULL REFERENCES platform_experiences(id) ON DELETE CASCADE,
    order_id UUID NULL REFERENCES marketplace_orders(id) ON DELETE SET NULL,
    acquired_version INT NOT NULL DEFAULT 1,
    entitlement_type VARCHAR(50) NOT NULL DEFAULT 'OneTimePurchase',
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NULL,
    scope VARCHAR(50) NOT NULL DEFAULT 'OrganizationWide',
    CONSTRAINT uq_org_entitlement UNIQUE(organization_id, platform_experience_id)
);

CREATE INDEX IF NOT EXISTS idx_org_entitlements_org ON organization_entitlements(organization_id);
CREATE INDEX IF NOT EXISTS idx_org_entitlements_exp ON organization_entitlements(platform_experience_id);
CREATE INDEX IF NOT EXISTS idx_org_entitlements_status ON organization_entitlements(status);

CREATE TABLE IF NOT EXISTS experience_audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id UUID NULL REFERENCES organizations(id) ON DELETE CASCADE,
    platform_experience_id UUID NULL,
    action VARCHAR(100) NOT NULL,
    performed_by_user_id UUID NOT NULL,
    details JSONB NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_exp_audit_action ON experience_audit_events(action);
CREATE INDEX IF NOT EXISTS idx_exp_audit_org ON experience_audit_events(organization_id);
