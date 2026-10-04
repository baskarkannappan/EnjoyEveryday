CREATE TABLE IF NOT EXISTS teacher_schedules (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    day_of_week VARCHAR(50) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    shift_type VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    notes TEXT,
    CONSTRAINT fk_schedule_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_availabilities (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    can_work_mornings BOOLEAN DEFAULT FALSE,
    can_work_afternoons BOOLEAN DEFAULT FALSE,
    can_work_evenings BOOLEAN DEFAULT FALSE,
    can_work_weekends BOOLEAN DEFAULT FALSE,
    max_hours_per_week DECIMAL(5,2),
    notes TEXT,
    CONSTRAINT fk_availability_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
