CREATE TABLE IF NOT EXISTS teachers (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    person_id UUID,
    user_id UUID,
    employee_number VARCHAR(50),
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    last_name VARCHAR(100) NOT NULL,
    preferred_name VARCHAR(100),
    display_name VARCHAR(200),
    profile_photo_url TEXT,
    date_of_birth DATE,
    gender VARCHAR(50),
    preferred_language VARCHAR(50),
    other_languages TEXT,
    pronouns VARCHAR(50),
    bio TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    draft_data JSONB,
    profile_completion_percentage INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_teacher_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_employments (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    employee_number VARCHAR(50),
    job_title VARCHAR(100),
    employment_type VARCHAR(50),
    employment_status VARCHAR(50),
    hire_date DATE,
    probation_end_date DATE,
    termination_date DATE,
    department VARCHAR(100),
    branch_id UUID,
    primary_classroom_id UUID,
    reports_to_user_id UUID,
    work_location VARCHAR(100),
    pay_type VARCHAR(50),
    notes TEXT,
    CONSTRAINT fk_employment_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_emergency_contacts (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    relationship VARCHAR(100),
    primary_phone VARCHAR(50),
    alternate_phone VARCHAR(50),
    email VARCHAR(200),
    address TEXT,
    priority INTEGER DEFAULT 1,
    notes TEXT,
    CONSTRAINT fk_emergency_contact_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS skills (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    category VARCHAR(100),
    description TEXT,
    active BOOLEAN DEFAULT TRUE,
    CONSTRAINT fk_skills_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_skills (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    skill_id UUID NOT NULL,
    skill_level VARCHAR(50),
    years_experience INTEGER,
    is_primary_skill BOOLEAN DEFAULT FALSE,
    verified BOOLEAN DEFAULT FALSE,
    verified_by UUID,
    verified_at TIMESTAMPTZ,
    notes TEXT,
    CONSTRAINT fk_teacher_skills_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE,
    CONSTRAINT fk_teacher_skills_skill FOREIGN KEY (skill_id) REFERENCES skills(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS teacher_certifications (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL,
    certification_type VARCHAR(100),
    certification_name VARCHAR(200) NOT NULL,
    issuing_authority VARCHAR(200),
    certificate_number VARCHAR(100),
    issue_date DATE,
    expiration_date DATE,
    status VARCHAR(50),
    verification_status VARCHAR(50),
    document_id UUID,
    notes TEXT,
    CONSTRAINT fk_certification_teacher FOREIGN KEY (teacher_id) REFERENCES teachers(id) ON DELETE CASCADE
);
