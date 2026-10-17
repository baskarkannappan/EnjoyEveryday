-- ============================================================
-- EnjoyEveryday — Migration 050: Shared Payment Ledger & Razorpay Integration
-- Adds: Razorpay order & transaction tracking, cash receipts, reversal tracking, unique idempotency constraints
-- Seeds: platform.billing.cash_collect permission
-- ============================================================

-- 1. PERMISSION SEED FOR CASH COLLECTION
INSERT INTO permissions (name, description) VALUES
    ('platform.billing.cash_collect', 'Record, verify, and reverse in-person cash payments and generate official cash receipts')
ON CONFLICT (name) DO NOTHING;

-- Grant permissions to PlatformAdmin system role
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name = 'PlatformAdmin'
  AND p.name = 'platform.billing.cash_collect'
ON CONFLICT DO NOTHING;

-- 2. EXTEND PAYMENTS TABLE
ALTER TABLE payments 
    ADD COLUMN IF NOT EXISTS provider_order_id VARCHAR(100),
    ADD COLUMN IF NOT EXISTS provider_payment_id VARCHAR(100),
    ADD COLUMN IF NOT EXISTS provider_signature VARCHAR(256),
    ADD COLUMN IF NOT EXISTS receipt_number VARCHAR(100),
    ADD COLUMN IF NOT EXISTS recorded_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS recorded_by_email VARCHAR(256),
    ADD COLUMN IF NOT EXISTS is_reversed BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS reversed_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS reversed_by VARCHAR(256),
    ADD COLUMN IF NOT EXISTS reversal_reason TEXT,
    ADD COLUMN IF NOT EXISTS reversal_reference VARCHAR(100),
    ADD COLUMN IF NOT EXISTS audit_metadata JSONB;

-- 3. IDEMPOTENCY & AUDIT INDEXES
-- Enforce unique provider payment id (Razorpay transaction id) to prevent duplicate payment capture
CREATE UNIQUE INDEX IF NOT EXISTS uk_payments_provider_payment_id 
    ON payments(provider_payment_id) 
    WHERE provider_payment_id IS NOT NULL AND is_reversed = FALSE;

-- Enforce unique receipt number for manual cash receipts
CREATE UNIQUE INDEX IF NOT EXISTS uk_payments_receipt_number 
    ON payments(receipt_number) 
    WHERE receipt_number IS NOT NULL AND is_reversed = FALSE;

-- Index for quick lookup of payments by provider order id
CREATE INDEX IF NOT EXISTS idx_payments_provider_order_id 
    ON payments(provider_order_id) 
    WHERE provider_order_id IS NOT NULL;

-- Index for payment ledger reconciliation and audit
CREATE INDEX IF NOT EXISTS idx_payments_method_date 
    ON payments(method, payment_date DESC);
