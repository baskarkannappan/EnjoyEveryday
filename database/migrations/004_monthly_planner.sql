-- Migration 004: Monthly Planner

CREATE TABLE IF NOT EXISTS experience_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    experience_id UUID NOT NULL,
    classroom_id UUID NOT NULL,
    scheduled_date DATE NOT NULL,
    time_of_day VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_experience_schedules_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_experience_schedules_experience FOREIGN KEY (experience_id) REFERENCES experiences(id) ON DELETE CASCADE,
    CONSTRAINT fk_experience_schedules_classroom FOREIGN KEY (classroom_id) REFERENCES classrooms(id) ON DELETE CASCADE
);

CREATE INDEX idx_experience_schedules_tenant ON experience_schedules(tenant_id);
CREATE INDEX idx_experience_schedules_classroom_date ON experience_schedules(classroom_id, scheduled_date);
