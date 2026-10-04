-- ============================================================
-- EnjoyEveryday — Migration 008: Full Child Domain
-- Creates: All tables related to full child identity, safety, and enrollment
-- ============================================================

-- ADDRESS
CREATE TABLE IF NOT EXISTS addresses (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    address_type    VARCHAR(50),
    address_line1   VARCHAR(255) NOT NULL,
    address_line2   VARCHAR(255),
    city            VARCHAR(100) NOT NULL,
    province        VARCHAR(100) NOT NULL,
    postal_code     VARCHAR(50) NOT NULL,
    country         VARCHAR(100) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_addresses_tenant ON addresses(tenant_id);

-- PERSON (Parent / Guardian / Contact)
CREATE TABLE IF NOT EXISTS persons (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    user_id         UUID REFERENCES users(id), -- Optional link to auth user
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    relationship    VARCHAR(100),
    email           VARCHAR(255),
    mobile_phone    VARCHAR(50),
    alternate_phone VARCHAR(50),
    occupation      VARCHAR(100),
    employer        VARCHAR(100),
    preferred_contact_method VARCHAR(50),
    preferred_language VARCHAR(50),
    has_portal_access BOOLEAN NOT NULL DEFAULT FALSE,
    receives_daily_updates BOOLEAN NOT NULL DEFAULT FALSE,
    receives_photos BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_persons_tenant ON persons(tenant_id);

-- CHILD GUARDIAN
CREATE TABLE IF NOT EXISTS child_guardians (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    person_id       UUID NOT NULL REFERENCES persons(id) ON DELETE CASCADE,
    relationship    VARCHAR(100) NOT NULL,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    has_legal_custody BOOLEAN NOT NULL DEFAULT FALSE,
    can_pickup      BOOLEAN NOT NULL DEFAULT TRUE,
    can_receive_information BOOLEAN NOT NULL DEFAULT TRUE,
    can_authorize_medical_care BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE (child_id, person_id)
);

-- EMERGENCY CONTACTS
CREATE TABLE IF NOT EXISTS emergency_contacts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    relationship    VARCHAR(100) NOT NULL,
    phone           VARCHAR(50) NOT NULL,
    alternate_phone VARCHAR(50),
    email           VARCHAR(255),
    address         VARCHAR(500),
    priority        INT NOT NULL DEFAULT 1,
    is_authorized_pickup BOOLEAN NOT NULL DEFAULT FALSE,
    notes           TEXT
);

-- AUTHORIZED PICKUP
CREATE TABLE IF NOT EXISTS authorized_pickups (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    person_id       UUID REFERENCES persons(id), -- Can be a known person or separate
    relationship    VARCHAR(100),
    pickup_code     VARCHAR(50),
    photo_url       VARCHAR(1024),
    valid_from      TIMESTAMPTZ,
    valid_to        TIMESTAMPTZ,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    notes           TEXT
);

-- CHILD PERSON RESTRICTIONS
CREATE TABLE IF NOT EXISTS child_person_restrictions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    person_id       UUID NOT NULL REFERENCES persons(id),
    restriction_type VARCHAR(100) NOT NULL,
    effective_from  TIMESTAMPTZ,
    effective_to    TIMESTAMPTZ,
    notes           TEXT
);

-- CHILD MEDICAL PROFILE
CREATE TABLE IF NOT EXISTS child_medical_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    primary_physician VARCHAR(200),
    physician_phone VARCHAR(50),
    clinic_name     VARCHAR(200),
    clinic_phone    VARCHAR(50),
    health_card_number VARCHAR(100),
    blood_type      VARCHAR(20),
    medical_notes   TEXT,
    special_medical_needs TEXT,
    medical_consent BOOLEAN NOT NULL DEFAULT FALSE
);

-- CHILD ALLERGIES
CREATE TABLE IF NOT EXISTS child_allergies (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    allergen        VARCHAR(200) NOT NULL,
    reaction        VARCHAR(200),
    severity        VARCHAR(50) NOT NULL,
    symptoms        TEXT,
    treatment       TEXT,
    emergency_action TEXT,
    has_epi_pen     BOOLEAN NOT NULL DEFAULT FALSE,
    notes           TEXT
);

-- CHILD MEDICATIONS
CREATE TABLE IF NOT EXISTS child_medications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    medication_name VARCHAR(200) NOT NULL,
    dosage          VARCHAR(100),
    route           VARCHAR(100),
    frequency       VARCHAR(100),
    start_date      DATE,
    end_date        DATE,
    prescribed_by   VARCHAR(200),
    instructions    TEXT,
    parent_authorization BOOLEAN NOT NULL DEFAULT FALSE,
    medication_authorization_document_id UUID,
    status          VARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- MEDICATION ADMINISTRATION
CREATE TABLE IF NOT EXISTS medication_administrations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    medication_id   UUID NOT NULL REFERENCES child_medications(id) ON DELETE CASCADE,
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    administered_at TIMESTAMPTZ NOT NULL,
    dosage          VARCHAR(100),
    administered_by UUID NOT NULL REFERENCES users(id),
    notes           TEXT
);

-- CHILD DIETARY PROFILE
CREATE TABLE IF NOT EXISTS child_dietary_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    diet_type       VARCHAR(100),
    food_restrictions TEXT,
    religious_restrictions TEXT,
    texture_restrictions TEXT,
    feeding_notes   TEXT,
    self_feeds      BOOLEAN NOT NULL DEFAULT TRUE,
    special_instructions TEXT
);

-- CHILD FOOD RESTRICTIONS
CREATE TABLE IF NOT EXISTS child_food_restrictions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    food            VARCHAR(200) NOT NULL,
    restriction_type VARCHAR(100) NOT NULL,
    severity        VARCHAR(50),
    notes           TEXT
);

-- CHILD FEEDING PROFILE
CREATE TABLE IF NOT EXISTS child_feeding_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    feeding_method  VARCHAR(100),
    bottle_type     VARCHAR(100),
    bottle_amount   VARCHAR(100),
    bottle_schedule VARCHAR(200),
    solid_food_stage VARCHAR(100),
    feeding_assistance VARCHAR(100),
    choking_risk    TEXT,
    feeding_instructions TEXT
);

-- CHILD SLEEP PROFILE
CREATE TABLE IF NOT EXISTS child_sleep_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    typical_nap_start VARCHAR(50),
    typical_nap_duration VARCHAR(50),
    comfort_item    VARCHAR(200),
    sleep_routine   TEXT,
    sleep_instructions TEXT,
    other_notes     TEXT
);

-- CHILD TOILETING PROFILE
CREATE TABLE IF NOT EXISTS child_toileting_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    toilet_training_status VARCHAR(100),
    diaper_required BOOLEAN NOT NULL DEFAULT FALSE,
    diaper_size     VARCHAR(50),
    changing_instructions TEXT,
    bathroom_assistance VARCHAR(200),
    accident_notes  TEXT
);

-- CHILD COMMUNICATION PROFILE
CREATE TABLE IF NOT EXISTS child_communication_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    primary_language VARCHAR(100),
    secondary_language VARCHAR(100),
    communication_method VARCHAR(200),
    speech_notes    TEXT,
    communication_support TEXT,
    interpreter_required BOOLEAN NOT NULL DEFAULT FALSE
);

-- CHILD ROUTINE
CREATE TABLE IF NOT EXISTS child_routines (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    morning_routine TEXT,
    comfort_strategies TEXT,
    favorite_activities TEXT,
    favorite_toys   TEXT,
    favorite_books  TEXT,
    calming_strategies TEXT,
    communication_preferences TEXT,
    transition_strategies TEXT,
    other_notes     TEXT
);

-- CHILD SUPPORT PROFILE
CREATE TABLE IF NOT EXISTS child_support_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    support_required BOOLEAN NOT NULL DEFAULT FALSE,
    support_description TEXT,
    learning_preferences TEXT,
    accessibility_needs TEXT,
    communication_support TEXT,
    sensory_considerations TEXT,
    behavior_support_plan TEXT,
    external_support_provider VARCHAR(255),
    notes           TEXT
);

-- ENROLLMENTS
CREATE TABLE IF NOT EXISTS enrollments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    daycare_id      UUID,
    branch_id       UUID REFERENCES branches(id),
    classroom_id    UUID REFERENCES classrooms(id),
    enrollment_date DATE,
    start_date      DATE,
    expected_end_date DATE,
    program         VARCHAR(100),
    enrollment_status VARCHAR(50) NOT NULL,
    schedule_type   VARCHAR(100),
    days_per_week   INT,
    arrival_time    VARCHAR(50),
    departure_time  VARCHAR(50),
    assigned_teacher_id UUID REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- CHILD SCHEDULE
CREATE TABLE IF NOT EXISTS child_schedules (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    day_of_week     INT NOT NULL, -- 0=Sunday, 1=Monday...
    expected_arrival VARCHAR(50),
    expected_departure VARCHAR(50),
    UNIQUE (child_id, day_of_week)
);

-- CHILD DOCUMENTS
CREATE TABLE IF NOT EXISTS child_documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    document_type   VARCHAR(100) NOT NULL,
    file_name       VARCHAR(255) NOT NULL,
    storage_url     VARCHAR(1024) NOT NULL,
    uploaded_by     UUID REFERENCES users(id),
    uploaded_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expiration_date DATE,
    status          VARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- CHILD CONSENTS
CREATE TABLE IF NOT EXISTS child_consents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    consent_type    VARCHAR(100) NOT NULL,
    granted         BOOLEAN NOT NULL DEFAULT FALSE,
    granted_by      VARCHAR(255),
    granted_at      TIMESTAMPTZ,
    expires_at      TIMESTAMPTZ,
    document_id     UUID REFERENCES child_documents(id),
    version         VARCHAR(50),
    UNIQUE (child_id, consent_type)
);

-- CHILD LEGAL PROFILE
CREATE TABLE IF NOT EXISTS child_legal_profiles (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    has_custody_restrictions BOOLEAN NOT NULL DEFAULT FALSE,
    legal_notes     TEXT,
    court_order_document_id UUID REFERENCES child_documents(id)
);

-- CHILD TRANSPORTATION
CREATE TABLE IF NOT EXISTS child_transportations (
    child_id        UUID PRIMARY KEY REFERENCES children(id) ON DELETE CASCADE,
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    transportation_required BOOLEAN NOT NULL DEFAULT FALSE,
    pickup_location VARCHAR(255),
    dropoff_location VARCHAR(255),
    route           VARCHAR(100),
    vehicle         VARCHAR(100),
    authorized_adult VARCHAR(255),
    special_instructions TEXT
);

-- CHILD RELATIONSHIPS (Sibling, Twin, etc)
CREATE TABLE IF NOT EXISTS child_relationships (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    related_child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    relationship_type VARCHAR(100) NOT NULL,
    notes           TEXT,
    UNIQUE (child_id, related_child_id)
);

-- CHILD ENROLLMENT HISTORY
CREATE TABLE IF NOT EXISTS child_enrollment_histories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id),
    child_id        UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    classroom_id    UUID REFERENCES classrooms(id),
    start_date      DATE NOT NULL,
    end_date        DATE,
    reason          TEXT
);
