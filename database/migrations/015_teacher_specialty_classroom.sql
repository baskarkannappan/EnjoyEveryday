CREATE TABLE IF NOT EXISTS teacher_specialties (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    specialty_id UUID,
    level VARCHAR(100),
    years_experience INTEGER,
    notes TEXT,
    CONSTRAINT fk_specialty_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS classroom_teachers (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    classroom_id UUID NOT NULL,
    user_id UUID NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_classroom_teacher_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);
