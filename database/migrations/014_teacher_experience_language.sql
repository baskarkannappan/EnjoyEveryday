CREATE TABLE IF NOT EXISTS teacher_experiences (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    organization_name VARCHAR(200) NOT NULL,
    position VARCHAR(100) NOT NULL,
    start_date DATE,
    end_date DATE,
    age_group VARCHAR(100),
    description TEXT,
    reference_information TEXT,
    verification_status VARCHAR(50),
    CONSTRAINT fk_experience_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_languages (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    language VARCHAR(100) NOT NULL,
    proficiency VARCHAR(50),
    is_primary_language BOOLEAN DEFAULT FALSE,
    can_communicate_with_parents BOOLEAN DEFAULT FALSE,
    can_teach_in_language BOOLEAN DEFAULT FALSE,
    CONSTRAINT fk_language_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
