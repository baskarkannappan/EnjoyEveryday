-- Phase 1: Core Classroom Domain Updates

ALTER TABLE classrooms 
ADD COLUMN IF NOT EXISTS display_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS short_name VARCHAR(50),
ADD COLUMN IF NOT EXISTS code VARCHAR(50),
ADD COLUMN IF NOT EXISTS description TEXT,
ADD COLUMN IF NOT EXISTS classroom_type VARCHAR(100),
ADD COLUMN IF NOT EXISTS status VARCHAR(50) NOT NULL DEFAULT 'Draft',
ADD COLUMN IF NOT EXISTS min_age_months INT,
ADD COLUMN IF NOT EXISTS max_age_months INT,
ADD COLUMN IF NOT EXISTS current_enrollment INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS photo_url VARCHAR(1000),
ADD COLUMN IF NOT EXISTS draft_data JSONB,
ADD COLUMN IF NOT EXISTS profile_completion_percentage INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS created_by UUID,
ADD COLUMN IF NOT EXISTS updated_by UUID;

-- Update age_group to be larger if it was small
ALTER TABLE classrooms ALTER COLUMN age_group TYPE VARCHAR(100);

CREATE INDEX IF NOT EXISTS idx_classrooms_status ON classrooms(status);
