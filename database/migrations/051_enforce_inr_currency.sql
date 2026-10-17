-- ============================================================
-- EnjoyEveryday — Migration 051: Enforce INR Currency Platform-Wide
-- Standardizes all default table currencies and existing ledger data to INR.
-- ============================================================

-- 1. UPDATE COLUMN DEFAULTS TO INR
ALTER TABLE IF EXISTS invoices 
    ALTER COLUMN currency SET DEFAULT 'INR';

ALTER TABLE IF EXISTS payments 
    ALTER COLUMN currency SET DEFAULT 'INR';

ALTER TABLE IF EXISTS platform_experiences 
    ALTER COLUMN currency SET DEFAULT 'INR';

ALTER TABLE IF EXISTS marketplace_orders 
    ALTER COLUMN currency SET DEFAULT 'INR';

-- 2. STANDARDIZE ALL EXISTING RECORDS TO INR
UPDATE invoices 
SET currency = 'INR' 
WHERE currency IS NULL OR currency != 'INR';

UPDATE payments 
SET currency = 'INR' 
WHERE currency IS NULL OR currency != 'INR';

UPDATE platform_experiences 
SET currency = 'INR' 
WHERE currency IS NULL OR currency != 'INR';

UPDATE marketplace_orders 
SET currency = 'INR' 
WHERE currency IS NULL OR currency != 'INR';
