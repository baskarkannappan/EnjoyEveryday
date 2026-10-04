CREATE TABLE IF NOT EXISTS teacher_education (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    institution_name VARCHAR(200) NOT NULL,
    degree VARCHAR(100),
    diploma VARCHAR(100),
    certificate VARCHAR(100),
    field_of_study VARCHAR(200),
    start_date DATE,
    end_date DATE,
    graduation_date DATE,
    country VARCHAR(100),
    verification_status VARCHAR(50),
    document_id UUID,
    notes TEXT,
    CONSTRAINT fk_education_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
