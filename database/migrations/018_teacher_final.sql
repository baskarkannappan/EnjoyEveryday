CREATE TABLE IF NOT EXISTS teacher_observations (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    observer_user_id UUID NOT NULL,
    observation_date DATE NOT NULL,
    rating VARCHAR(50),
    feedback TEXT,
    action_items TEXT,
    CONSTRAINT fk_observation_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_preferences (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    preference_type VARCHAR(100) NOT NULL,
    preference_value VARCHAR(200) NOT NULL,
    notes TEXT,
    CONSTRAINT fk_preference_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
