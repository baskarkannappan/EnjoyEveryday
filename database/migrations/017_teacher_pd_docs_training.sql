CREATE TABLE IF NOT EXISTS teacher_professional_developments (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    course_name VARCHAR(200) NOT NULL,
    provider VARCHAR(200),
    hours_completed DECIMAL(5,2),
    completion_date DATE,
    notes TEXT,
    CONSTRAINT fk_pd_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_documents (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    document_type VARCHAR(100) NOT NULL,
    file_name VARCHAR(200) NOT NULL,
    file_path TEXT NOT NULL,
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    notes TEXT,
    CONSTRAINT fk_doc_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_trainings (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    training_name VARCHAR(200) NOT NULL,
    is_mandatory BOOLEAN DEFAULT FALSE,
    completion_date DATE,
    expiry_date DATE,
    status VARCHAR(50),
    CONSTRAINT fk_training_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
