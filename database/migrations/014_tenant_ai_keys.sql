-- ============================================================
-- EnjoyEveryday — Migration 014: Tenant AI API Keys
-- Adds: encrypted_ai_api_key to tenants table
-- ============================================================

ALTER TABLE tenants 
ADD COLUMN IF NOT EXISTS encrypted_ai_api_key VARCHAR(1000);
