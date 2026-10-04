ALTER TABLE children ADD COLUMN profile_completion_percentage INT DEFAULT 0;
ALTER TABLE children ADD COLUMN enrollment_status VARCHAR(50) DEFAULT 'Draft';
