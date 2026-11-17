-- ============================================================================
-- Migration 124: Platform Payment & UPI QR Code Settings
-- Allows Platform Admin to configure the bank account, UPI ID, payee name,
-- and optional custom QR code image used on organization invoices & emails.
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.platform_payment_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    upi_id VARCHAR(150) NOT NULL DEFAULT 'billing@enjoyeveryday',
    payee_name VARCHAR(200) NOT NULL DEFAULT 'EnjoyEveryday Platform',
    bank_name VARCHAR(200) NULL,
    account_number VARCHAR(100) NULL,
    ifsc_code VARCHAR(50) NULL,
    use_custom_qr_image BOOLEAN NOT NULL DEFAULT FALSE,
    custom_qr_data_uri TEXT NULL,
    updated_by_email VARCHAR(255) NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO public.platform_payment_settings (upi_id, payee_name, updated_by_email)
SELECT 'billing@enjoyeveryday', 'EnjoyEveryday Platform', 'platform-admin@enjoyeveryday.com'
WHERE NOT EXISTS (SELECT 1 FROM public.platform_payment_settings);

