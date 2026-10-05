ALTER TABLE experience_schedules ADD COLUMN IF NOT EXISTS primary_teacher_id UUID REFERENCES teachers(id) ON DELETE SET NULL;
ALTER TABLE experience_schedules ADD COLUMN IF NOT EXISTS planned_start_time TIME;
ALTER TABLE experience_schedules ADD COLUMN IF NOT EXISTS planned_end_time TIME;
ALTER TABLE experience_schedules ADD COLUMN IF NOT EXISTS preparation_notes TEXT;
