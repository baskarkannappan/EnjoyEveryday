-- ============================================================
-- EnjoyEveryday — Migration 049: Manual & Automatic Billing and Invoicing
-- Creates: invoices, invoice_line_items, payments, organization_billing_settings
-- Seeds: platform.billing.manage, billing.view permissions
-- ============================================================

-- 1. PERMISSIONS SEED
INSERT INTO permissions (name, description) VALUES
    ('platform.billing.manage', 'Create, publish, void manual invoices, record payments, and configure billing modes'),
    ('billing.view', 'View published organization invoices, payment history, and download invoice documents')
ON CONFLICT (name) DO NOTHING;

-- Grant permissions to system roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND (
    (r.name = 'PlatformAdmin' AND p.name IN ('platform.billing.manage', 'billing.view')) OR
    (r.name IN ('OrganizationOwner', 'OrganizationAdmin') AND p.name = 'billing.view')
  )
ON CONFLICT DO NOTHING;

-- Grant permissions to tenant-scoped copies of these roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN permissions p ON (
    (r.name IN ('OrganizationOwner', 'OrganizationAdmin') AND p.name = 'billing.view')
)
WHERE r.tenant_id IS NOT NULL
ON CONFLICT DO NOTHING;

-- 2. INVOICES TABLE
CREATE TABLE IF NOT EXISTS invoices (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id         UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    invoice_number          VARCHAR(50) NOT NULL UNIQUE,
    billing_period          VARCHAR(20),
    subtotal                NUMERIC(12, 2) NOT NULL DEFAULT 0,
    discount_amount         NUMERIC(12, 2) NOT NULL DEFAULT 0,
    tax_amount              NUMERIC(12, 2) NOT NULL DEFAULT 0,
    total_amount            NUMERIC(12, 2) NOT NULL DEFAULT 0,
    amount_paid             NUMERIC(12, 2) NOT NULL DEFAULT 0,
    currency                VARCHAR(10) NOT NULL DEFAULT 'INR',
    status                  VARCHAR(30) NOT NULL DEFAULT 'Draft',
    issue_date              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    due_date                TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '14 days'),
    paid_at                 TIMESTAMPTZ,
    published_at            TIMESTAMPTZ,
    published_by            VARCHAR(200),
    voided_at               TIMESTAMPTZ,
    voided_by               VARCHAR(200),
    void_reason             TEXT,
    created_by              VARCHAR(200),
    description             TEXT,
    notes                   TEXT,
    payment_instructions    TEXT,
    billing_contact_email   VARCHAR(256),
    document_storage_key    VARCHAR(200),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_invoices_org ON invoices(organization_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_due ON invoices(due_date);
CREATE UNIQUE INDEX IF NOT EXISTS uk_invoices_org_billing_period 
    ON invoices(organization_id, billing_period) 
    WHERE billing_period IS NOT NULL AND status != 'Void';

-- 3. INVOICE LINE ITEMS TABLE
CREATE TABLE IF NOT EXISTS invoice_line_items (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id              UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    line_number             INT NOT NULL DEFAULT 1,
    description             VARCHAR(300) NOT NULL,
    quantity                NUMERIC(10, 2) NOT NULL DEFAULT 1,
    unit_price              NUMERIC(12, 2) NOT NULL DEFAULT 0,
    line_total              NUMERIC(12, 2) NOT NULL DEFAULT 0,
    category                VARCHAR(50) DEFAULT 'Subscription',
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_invoice_line_items_inv ON invoice_line_items(invoice_id);

-- 4. PAYMENTS TABLE
CREATE TABLE IF NOT EXISTS payments (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id         UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    invoice_id              UUID REFERENCES invoices(id) ON DELETE SET NULL,
    invoice_number          VARCHAR(50),
    payment_reference       VARCHAR(100) NOT NULL,
    amount                  NUMERIC(12, 2) NOT NULL DEFAULT 0,
    currency                VARCHAR(10) NOT NULL DEFAULT 'INR',
    method                  VARCHAR(50) NOT NULL DEFAULT 'BankTransfer',
    is_verified             BOOLEAN NOT NULL DEFAULT TRUE,
    verified_by             VARCHAR(200),
    verified_at             TIMESTAMPTZ,
    status                  VARCHAR(50) NOT NULL DEFAULT 'Completed',
    transaction_id          VARCHAR(100),
    notes                   TEXT,
    payment_date            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_payments_org ON payments(organization_id, tenant_id);
CREATE INDEX IF NOT EXISTS idx_payments_invoice ON payments(invoice_id);

-- 5. ORGANIZATION BILLING SETTINGS TABLE
CREATE TABLE IF NOT EXISTS organization_billing_settings (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id         UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    mode                    VARCHAR(20) NOT NULL DEFAULT 'Manual',
    frequency               VARCHAR(20) NOT NULL DEFAULT 'Monthly',
    effective_date          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    next_billing_date       TIMESTAMPTZ,
    auto_charge_enabled     BOOLEAN NOT NULL DEFAULT FALSE,
    notes                   TEXT,
    updated_by              VARCHAR(200),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uk_org_billing_settings_org UNIQUE (organization_id)
);

CREATE INDEX IF NOT EXISTS idx_org_billing_settings_org ON organization_billing_settings(organization_id);
