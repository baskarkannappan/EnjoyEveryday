-- Migration 119: Facility Profiles (About Us & Contact Us) and Contact Inquiries
-- Allows Org Admins and Branch Admins to customize Center branding, logo, motto, story, contact details, photos, and location
-- Allows Parents and Teachers to view the facility profile and submit inquiries to the branch/org

CREATE TABLE IF NOT EXISTS facility_profiles (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            UUID NOT NULL REFERENCES tenants(id),
    organization_id      UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id            UUID NULL REFERENCES branches(id) ON DELETE CASCADE,
    
    -- Branding & Story
    title                VARCHAR(200) NOT NULL,
    motto                VARCHAR(300),
    about_story          TEXT,
    logo_media_url       VARCHAR(500),         -- Stored in IObjectStorageRegistry (e.g. /api/media/{id}/display)
    cover_media_url      VARCHAR(500),         -- Hero/banner photo
    gallery_media_urls   JSONB DEFAULT '[]'::jsonb, -- Array of photo URLs
    
    -- Contact & Hours
    phone                VARCHAR(50),
    secondary_phone      VARCHAR(50),
    emergency_phone      VARCHAR(50),
    email                VARCHAR(150),
    inquiry_email        VARCHAR(150),
    operating_hours      VARCHAR(250),         -- e.g. "Mon - Fri: 7:00 AM – 6:00 PM"
    
    -- Location & Directions
    street_address       VARCHAR(250),
    city                 VARCHAR(100),
    state                VARCHAR(100),
    postal_code          VARCHAR(30),
    country              VARCHAR(100) DEFAULT 'USA',
    map_url              TEXT,                 -- Google Maps or OpenStreetMap link
    directions_notes     TEXT,                 -- Parking or entrance notes
    
    -- Philosophy & Core Values
    core_values          JSONB DEFAULT '[]'::jsonb, -- Array of strings e.g. ["Nature-inspired play", "Certified Reggio Educators"]
    
    is_active            BOOLEAN NOT NULL DEFAULT true,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by_user_id   UUID NULL
);

-- Unique constraint: At most one profile per organization default (branch_id IS NULL)
CREATE UNIQUE INDEX IF NOT EXISTS uk_facility_profiles_org 
    ON facility_profiles (tenant_id, organization_id) WHERE branch_id IS NULL;

-- Unique constraint: At most one profile per physical branch (branch_id IS NOT NULL)
CREATE UNIQUE INDEX IF NOT EXISTS uk_facility_profiles_branch 
    ON facility_profiles (tenant_id, organization_id, branch_id) WHERE branch_id IS NOT NULL;

-- Fast index for lookup by branch or org
CREATE INDEX IF NOT EXISTS idx_facility_profiles_lookup 
    ON facility_profiles (tenant_id, organization_id, branch_id);

-- Contact Inquiries submitted by Parents, prospective families, or staff
CREATE TABLE IF NOT EXISTS contact_inquiries (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            UUID NOT NULL REFERENCES tenants(id),
    organization_id      UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id            UUID NULL REFERENCES branches(id) ON DELETE CASCADE,
    sender_user_id       UUID NULL,
    sender_name          VARCHAR(150) NOT NULL,
    sender_email         VARCHAR(150) NOT NULL,
    sender_phone         VARCHAR(50),
    category             VARCHAR(50) NOT NULL DEFAULT 'General', -- General, Tour Request, Feedback, Enrollment, Absence
    subject              VARCHAR(200) NOT NULL,
    message              TEXT NOT NULL,
    status               VARCHAR(30) NOT NULL DEFAULT 'New',     -- New, InReview, Resolved
    admin_notes          TEXT NULL,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at          TIMESTAMPTZ NULL,
    resolved_by_user_id  UUID NULL
);

CREATE INDEX IF NOT EXISTS idx_contact_inquiries_branch_status 
    ON contact_inquiries (tenant_id, organization_id, branch_id, status);

-- Seed initial default organization profile for dev Little Stars Daycare and Downtown branch
INSERT INTO facility_profiles (
    tenant_id,
    organization_id,
    branch_id,
    title,
    motto,
    about_story,
    logo_media_url,
    phone,
    secondary_phone,
    emergency_phone,
    email,
    inquiry_email,
    operating_hours,
    street_address,
    city,
    state,
    postal_code,
    country,
    directions_notes,
    core_values
) VALUES (
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    NULL,
    'Little Stars Daycare & Early Learning',
    'Nurturing curious minds through wonder, play, and nature.',
    'At Little Stars, we believe every child is naturally curious, capable, and full of wonder. Our Scandinavian-inspired curriculum blends outdoor sensory exploration, child-centered discovery, and nurturing educator guidance to create a second home where children thrive emotionally, socially, and intellectually.',
    NULL,
    '(416) 555-0199',
    '(416) 555-0198',
    '(416) 555-0911',
    'contact@littlestars.com',
    'admissions@littlestars.com',
    'Monday – Friday: 7:00 AM – 6:00 PM',
    '100 University Avenue, Suite 400',
    'Toronto',
    'ON',
    'M5J 1V6',
    'Canada',
    'Visitor check-in at front reception on the 4th floor.',
    '["Child-led inquiry & exploration", "Wholesome organic meals & snacks", "Low educator-to-child ratios", "Safe outdoor nature spaces"]'::jsonb
) ON CONFLICT DO NOTHING;

-- Seed initial Downtown Branch profile
INSERT INTO facility_profiles (
    tenant_id,
    organization_id,
    branch_id,
    title,
    motto,
    about_story,
    logo_media_url,
    phone,
    secondary_phone,
    emergency_phone,
    email,
    inquiry_email,
    operating_hours,
    street_address,
    city,
    state,
    postal_code,
    country,
    directions_notes,
    core_values
) VALUES (
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    'Little Stars — Downtown Campus',
    'Urban warmth and natural wonder in the heart of Toronto.',
    'Located in the vibrant core of Downtown Toronto, our Downtown campus offers bright, sunlit classrooms, a custom indoor nature playscape, and dedicated secure outdoor terrace gardens designed for daily experiential learning.',
    NULL,
    '(416) 555-0101',
    '(416) 555-0102',
    '(416) 555-0911',
    'downtown@littlestars.com',
    'downtown.tours@littlestars.com',
    'Monday – Friday: 7:00 AM – 6:00 PM',
    '125 King Street West',
    'Toronto',
    'ON',
    'M5X 1A9',
    'Canada',
    'Dedicated parent drop-off zone on King St. Buzz suite 200 at the main gate.',
    '["Secure terrace playground", "Downtown transit accessible", "Dedicated infant sensory atelier", "Full-time licensed pediatric nurse on site"]'::jsonb
) ON CONFLICT DO NOTHING;

