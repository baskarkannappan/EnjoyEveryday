-- 036_floor_maps_and_emergency_contacts.sql
-- Branch Floor Maps, Safety Markers, and Emergency Contacts

CREATE TABLE IF NOT EXISTS branch_floor_maps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    branch_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    building_area VARCHAR(255),
    floor_level VARCHAR(100) NOT NULL,
    description TEXT,
    file_storage_key VARCHAR(500) NOT NULL,
    file_type VARCHAR(50) NOT NULL,
    file_size BIGINT NOT NULL,
    content_hash VARCHAR(128) NOT NULL,
    version INT NOT NULL DEFAULT 1,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    review_due_at TIMESTAMPTZ,
    created_by VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    published_by VARCHAR(255),
    published_at TIMESTAMPTZ,
    supersedes_map_id UUID REFERENCES branch_floor_maps(id),
    row_version INT NOT NULL DEFAULT 1,
    emergency_instructions TEXT
);

CREATE INDEX IF NOT EXISTS idx_branch_floor_maps_lookup 
    ON branch_floor_maps(tenant_id, organization_id, branch_id, status);

CREATE TABLE IF NOT EXISTS floor_map_markers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_map_id UUID NOT NULL REFERENCES branch_floor_maps(id) ON DELETE CASCADE,
    marker_type VARCHAR(100) NOT NULL,
    label VARCHAR(255) NOT NULL,
    instructions TEXT,
    normalized_x DOUBLE PRECISION NOT NULL,
    normalized_y DOUBLE PRECISION NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_floor_map_markers_map_id 
    ON floor_map_markers(floor_map_id);

-- Alter emergency_contacts to support Safety & Trust directory and preserve existing child contacts
CREATE TABLE IF NOT EXISTS emergency_contacts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL
);

ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS organization_id UUID;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS branch_id UUID;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS scope VARCHAR(50) DEFAULT 'Branch';
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS category VARCHAR(100) DEFAULT 'EmergencyServices';
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS name VARCHAR(255);
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS phone_number VARCHAR(100);
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS alternate_phone_number VARCHAR(100);
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS country_region VARCHAR(100);
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS instructions TEXT;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS verification_status VARCHAR(50) DEFAULT 'Unverified';
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS verified_by VARCHAR(255);
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS verified_at TIMESTAMPTZ;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS review_due_at TIMESTAMPTZ;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS display_order INT DEFAULT 0;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE emergency_contacts ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'emergency_contacts' AND column_name = 'child_id') THEN
        ALTER TABLE emergency_contacts ALTER COLUMN child_id DROP NOT NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'emergency_contacts' AND column_name = 'first_name') THEN
        ALTER TABLE emergency_contacts ALTER COLUMN first_name DROP NOT NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'emergency_contacts' AND column_name = 'last_name') THEN
        ALTER TABLE emergency_contacts ALTER COLUMN last_name DROP NOT NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'emergency_contacts' AND column_name = 'relationship') THEN
        ALTER TABLE emergency_contacts ALTER COLUMN relationship DROP NOT NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'emergency_contacts' AND column_name = 'phone') THEN
        ALTER TABLE emergency_contacts ALTER COLUMN phone DROP NOT NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_emergency_contacts_lookup 
    ON emergency_contacts(tenant_id, organization_id, branch_id, is_active);

-- Seed initial demonstration contacts using active tenant and branch FKs
INSERT INTO emergency_contacts (
    id, tenant_id, organization_id, branch_id, scope, category, name, 
    phone_number, alternate_phone_number, country_region, address, instructions, 
    verification_status, is_active, display_order, created_at, updated_at
)
SELECT 
    'e0000001-0000-0000-0000-000000000001'::uuid,
    t.id, o.id, b.id,
    'Jurisdiction',
    'EmergencyServices',
    'Local Emergency Dispatch (Sample Demo)',
    '911',
    NULL,
    'US / Local',
    'Regional Dispatch Center',
    'Call immediately in life-threatening emergencies. State branch address clearly.',
    'Unverified',
    TRUE,
    1,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.organization_id = o.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO emergency_contacts (
    id, tenant_id, organization_id, branch_id, scope, category, name, 
    phone_number, alternate_phone_number, country_region, address, instructions, 
    verification_status, is_active, display_order, created_at, updated_at
)
SELECT 
    'e0000001-0000-0000-0000-000000000002'::uuid,
    t.id, o.id, b.id,
    'Jurisdiction',
    'PoisonControl',
    'National Poison Help Line (Sample Demo)',
    '1-800-222-1222',
    NULL,
    'US National',
    NULL,
    'Have the chemical or substance container in hand when calling.',
    'Unverified',
    TRUE,
    2,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.organization_id = o.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO emergency_contacts (
    id, tenant_id, organization_id, branch_id, scope, category, name, 
    phone_number, alternate_phone_number, country_region, address, instructions, 
    verification_status, is_active, display_order, created_at, updated_at
)
SELECT 
    'e0000001-0000-0000-0000-000000000003'::uuid,
    t.id, o.id, b.id,
    'Branch',
    'NearestHospital',
    'Children''s Regional Memorial Hospital (Sample Demo)',
    '(555) 019-2834',
    '(555) 019-2835',
    'Regional',
    '1200 Healthcare Plaza, Suite 100',
    'Pediatric Emergency Center - Level 1 Trauma. 1.8 miles from daycare.',
    'Unverified',
    TRUE,
    3,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.organization_id = o.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;

INSERT INTO emergency_contacts (
    id, tenant_id, organization_id, branch_id, scope, category, name, 
    phone_number, alternate_phone_number, country_region, address, instructions, 
    verification_status, is_active, display_order, created_at, updated_at
)
SELECT 
    'e0000001-0000-0000-0000-000000000004'::uuid,
    t.id, o.id, b.id,
    'Branch',
    'BranchDirector',
    'Sarah Jenkins - Branch Director (Sample Demo)',
    '(555) 782-9901',
    '(555) 782-9902',
    'Branch Campus',
    'Administrative Suite Room 101',
    'Primary point of contact for incident management and parental notification.',
    'Unverified',
    TRUE,
    4,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM tenants t
JOIN organizations o ON o.tenant_id = t.id
JOIN branches b ON b.organization_id = o.id
LIMIT 1
ON CONFLICT (id) DO NOTHING;
