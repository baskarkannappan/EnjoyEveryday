CREATE TABLE experience_feedback (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    experience_schedule_id UUID NOT NULL REFERENCES experience_schedules(id) ON DELETE CASCADE,
    teacher_id UUID NOT NULL,
    rating VARCHAR(50) NOT NULL,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_experience_feedback_tenant ON experience_feedback(tenant_id);
CREATE INDEX idx_experience_feedback_schedule ON experience_feedback(experience_schedule_id);

-- Note: The status column in experience_schedules is already VARCHAR(50) so we can update it from 'Planned' to 'Started' or 'Completed' without schema changes.
