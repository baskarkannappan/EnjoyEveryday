-- 057_enrollment_pipeline.down.sql
-- Rollback for Family Accounts & Enrollment Pipeline

DROP TABLE IF EXISTS family_billing_contracts CASCADE;
DROP TABLE IF EXISTS enrollment_pipeline_applications CASCADE;
