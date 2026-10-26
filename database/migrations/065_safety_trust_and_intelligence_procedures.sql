-- ============================================================
-- Migration 065: Safety, Trust, Consent, and Intelligence/AI Procedures
-- Converts inline SQL queries from:
--   - FloorMapRepository
--   - EmergencyContactRepository
--   - ConsentRepository
--   - ActivityRecommendationRepository
--   - ChildJourneyInsightRepository
--   - ParentCommunicationRepository
--   - WeeklyPlanningRepository
--   - DapperAuditService
-- into PostgreSQL stored procedures and functions.
-- ============================================================

-- ------------------------------------------------------------
-- 1. FLOOR MAP PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_branch_floor_maps(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    name VARCHAR,
    building_area VARCHAR,
    floor_level VARCHAR,
    description TEXT,
    file_storage_key VARCHAR,
    file_type VARCHAR,
    file_size BIGINT,
    content_hash VARCHAR,
    version INT,
    status VARCHAR,
    review_due_at TIMESTAMPTZ,
    created_by VARCHAR,
    created_at TIMESTAMPTZ,
    published_by VARCHAR,
    published_at TIMESTAMPTZ,
    supersedes_map_id UUID,
    row_version INT,
    emergency_instructions TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
      AND (p_status IS NULL OR status = p_status)
    ORDER BY floor_level ASC, name ASC, version DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_floor_map_markers_by_map_ids(
    p_map_ids UUID[]
)
RETURNS TABLE (
    id UUID,
    floor_map_id UUID,
    marker_type VARCHAR,
    label VARCHAR,
    instructions TEXT,
    normalized_x DOUBLE PRECISION,
    normalized_y DOUBLE PRECISION,
    display_order INT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at
    FROM floor_map_markers
    WHERE floor_map_id = ANY(p_map_ids)
    ORDER BY display_order ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_branch_floor_map_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    name VARCHAR,
    building_area VARCHAR,
    floor_level VARCHAR,
    description TEXT,
    file_storage_key VARCHAR,
    file_type VARCHAR,
    file_size BIGINT,
    content_hash VARCHAR,
    version INT,
    status VARCHAR,
    review_due_at TIMESTAMPTZ,
    created_by VARCHAR,
    created_at TIMESTAMPTZ,
    published_by VARCHAR,
    published_at TIMESTAMPTZ,
    supersedes_map_id UUID,
    row_version INT,
    emergency_instructions TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_floor_map_markers(
    p_floor_map_id UUID
)
RETURNS TABLE (
    id UUID,
    floor_map_id UUID,
    marker_type VARCHAR,
    label VARCHAR,
    instructions TEXT,
    normalized_x DOUBLE PRECISION,
    normalized_y DOUBLE PRECISION,
    display_order INT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, floor_map_id, marker_type, label, instructions, normalized_x, normalized_y, display_order, created_at
    FROM floor_map_markers
    WHERE floor_map_id = p_floor_map_id
    ORDER BY display_order ASC, created_at ASC;
$$;

CREATE OR REPLACE PROCEDURE sp_create_branch_floor_map(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_name VARCHAR,
    p_building_area VARCHAR,
    p_floor_level VARCHAR,
    p_description TEXT,
    p_file_storage_key VARCHAR,
    p_file_type VARCHAR,
    p_file_size BIGINT,
    p_content_hash VARCHAR,
    p_version INT,
    p_status VARCHAR,
    p_review_due_at TIMESTAMPTZ,
    p_created_by VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_published_by VARCHAR,
    p_published_at TIMESTAMPTZ,
    p_supersedes_map_id UUID,
    p_row_version INT,
    p_emergency_instructions TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO branch_floor_maps (
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_name, p_building_area,
        p_floor_level, p_description, p_file_storage_key, p_file_type, p_file_size,
        p_content_hash, p_version, p_status, p_review_due_at, p_created_by, p_created_at,
        p_published_by, p_published_at, p_supersedes_map_id, p_row_version, p_emergency_instructions
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_branch_floor_map(
    p_id UUID,
    p_tenant_id UUID,
    p_name VARCHAR,
    p_building_area VARCHAR,
    p_floor_level VARCHAR,
    p_description TEXT,
    p_file_storage_key VARCHAR,
    p_file_type VARCHAR,
    p_file_size BIGINT,
    p_content_hash VARCHAR,
    p_version INT,
    p_status VARCHAR,
    p_review_due_at TIMESTAMPTZ,
    p_published_by VARCHAR,
    p_published_at TIMESTAMPTZ,
    p_supersedes_map_id UUID,
    p_emergency_instructions TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE branch_floor_maps SET
        name = p_name,
        building_area = p_building_area,
        floor_level = p_floor_level,
        description = p_description,
        file_storage_key = p_file_storage_key,
        file_type = p_file_type,
        file_size = p_file_size,
        content_hash = p_content_hash,
        version = p_version,
        status = p_status,
        review_due_at = p_review_due_at,
        published_by = p_published_by,
        published_at = p_published_at,
        supersedes_map_id = p_supersedes_map_id,
        row_version = row_version + 1,
        emergency_instructions = p_emergency_instructions
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_floor_map_markers(
    p_floor_map_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM floor_map_markers WHERE floor_map_id = p_floor_map_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_floor_map_marker(
    p_id UUID,
    p_floor_map_id UUID,
    p_marker_type VARCHAR,
    p_label VARCHAR,
    p_instructions TEXT,
    p_normalized_x DOUBLE PRECISION,
    p_normalized_y DOUBLE PRECISION,
    p_display_order INT,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO floor_map_markers (
        id, floor_map_id, marker_type, label, instructions,
        normalized_x, normalized_y, display_order, created_at
    ) VALUES (
        p_id, p_floor_map_id, p_marker_type, p_label, p_instructions,
        p_normalized_x, p_normalized_y, p_display_order, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_floor_map_history(
    p_tenant_id UUID,
    p_branch_id UUID,
    p_floor_level VARCHAR
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    name VARCHAR,
    building_area VARCHAR,
    floor_level VARCHAR,
    description TEXT,
    file_storage_key VARCHAR,
    file_type VARCHAR,
    file_size BIGINT,
    content_hash VARCHAR,
    version INT,
    status VARCHAR,
    review_due_at TIMESTAMPTZ,
    created_by VARCHAR,
    created_at TIMESTAMPTZ,
    published_by VARCHAR,
    published_at TIMESTAMPTZ,
    supersedes_map_id UUID,
    row_version INT,
    emergency_instructions TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, name, building_area,
        floor_level, description, file_storage_key, file_type, file_size,
        content_hash, version, status, review_due_at, created_by, created_at,
        published_by, published_at, supersedes_map_id, row_version, emergency_instructions
    FROM branch_floor_maps
    WHERE tenant_id = p_tenant_id
      AND branch_id = p_branch_id
      AND floor_level = p_floor_level
    ORDER BY version DESC, created_at DESC;
$$;


-- ------------------------------------------------------------
-- 2. EMERGENCY CONTACT PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_emergency_contacts(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_active_only BOOLEAN DEFAULT FALSE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    scope VARCHAR,
    category VARCHAR,
    name VARCHAR,
    phone_number VARCHAR,
    alternate_phone_number VARCHAR,
    country_region VARCHAR,
    address TEXT,
    instructions TEXT,
    verification_status VARCHAR,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    review_due_at TIMESTAMPTZ,
    is_active BOOLEAN,
    display_order INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    FROM emergency_contacts
    WHERE tenant_id = p_tenant_id
      AND (NOT p_active_only OR is_active = TRUE)
      AND (
          scope = 'Jurisdiction'
          OR (scope = 'Organization' AND (p_organization_id IS NULL OR organization_id IS NULL OR organization_id = p_organization_id))
          OR (scope = 'Branch' AND (p_branch_id IS NULL OR branch_id IS NULL OR branch_id = p_branch_id))
      )
    ORDER BY display_order ASC, name ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_emergency_contact_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    scope VARCHAR,
    category VARCHAR,
    name VARCHAR,
    phone_number VARCHAR,
    alternate_phone_number VARCHAR,
    country_region VARCHAR,
    address TEXT,
    instructions TEXT,
    verification_status VARCHAR,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    review_due_at TIMESTAMPTZ,
    is_active BOOLEAN,
    display_order INT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    FROM emergency_contacts
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;

CREATE OR REPLACE PROCEDURE sp_create_emergency_contact(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_scope VARCHAR,
    p_category VARCHAR,
    p_name VARCHAR,
    p_phone_number VARCHAR,
    p_alternate_phone_number VARCHAR,
    p_country_region VARCHAR,
    p_address TEXT,
    p_instructions TEXT,
    p_verification_status VARCHAR,
    p_verified_by VARCHAR,
    p_verified_at TIMESTAMPTZ,
    p_review_due_at TIMESTAMPTZ,
    p_is_active BOOLEAN,
    p_display_order INT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO emergency_contacts (
        id, tenant_id, organization_id, branch_id, scope, category,
        name, phone_number, alternate_phone_number, country_region, address,
        instructions, verification_status, verified_by, verified_at,
        review_due_at, is_active, display_order, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_scope, p_category,
        p_name, p_phone_number, p_alternate_phone_number, p_country_region, p_address,
        p_instructions, p_verification_status, p_verified_by, p_verified_at,
        p_review_due_at, p_is_active, p_display_order, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_emergency_contact(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_scope VARCHAR,
    p_category VARCHAR,
    p_name VARCHAR,
    p_phone_number VARCHAR,
    p_alternate_phone_number VARCHAR,
    p_country_region VARCHAR,
    p_address TEXT,
    p_instructions TEXT,
    p_verification_status VARCHAR,
    p_verified_by VARCHAR,
    p_verified_at TIMESTAMPTZ,
    p_review_due_at TIMESTAMPTZ,
    p_is_active BOOLEAN,
    p_display_order INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE emergency_contacts SET
        organization_id = p_organization_id,
        branch_id = p_branch_id,
        scope = p_scope,
        category = p_category,
        name = p_name,
        phone_number = p_phone_number,
        alternate_phone_number = p_alternate_phone_number,
        country_region = p_country_region,
        address = p_address,
        instructions = p_instructions,
        verification_status = p_verification_status,
        verified_by = p_verified_by,
        verified_at = p_verified_at,
        review_due_at = p_review_due_at,
        is_active = p_is_active,
        display_order = p_display_order,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_emergency_contact(
    p_tenant_id UUID,
    p_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM emergency_contacts WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_verify_emergency_contact(
    p_tenant_id UUID,
    p_id UUID,
    p_verified_by VARCHAR,
    p_verified_at TIMESTAMPTZ,
    p_review_due_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE emergency_contacts SET
        verification_status = 'Verified',
        verified_by = p_verified_by,
        verified_at = p_verified_at,
        review_due_at = p_review_due_at,
        updated_at = CURRENT_TIMESTAMP
    WHERE tenant_id = p_tenant_id AND id = p_id;
END;
$$;


-- ------------------------------------------------------------
-- 3. CONSENT MANAGEMENT PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_consent_templates(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    description TEXT,
    document_type VARCHAR,
    version VARCHAR,
    template_content TEXT,
    required_for_enrollment BOOLEAN,
    is_active BOOLEAN,
    created_by UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, name, description, document_type,
        version, template_content, required_for_enrollment, is_active,
        created_by, created_at, updated_at
    FROM consent_templates
    WHERE tenant_id = p_tenant_id
      AND is_active = TRUE
      AND (p_organization_id IS NULL OR organization_id IS NULL OR organization_id = p_organization_id)
    ORDER BY required_for_enrollment DESC, name ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_template_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    name VARCHAR,
    description TEXT,
    document_type VARCHAR,
    version VARCHAR,
    template_content TEXT,
    required_for_enrollment BOOLEAN,
    is_active BOOLEAN,
    created_by UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, name, description, document_type,
        version, template_content, required_for_enrollment, is_active,
        created_by, created_at, updated_at
    FROM consent_templates
    WHERE tenant_id = p_tenant_id AND id = p_id;
$$;

CREATE OR REPLACE PROCEDURE sp_create_child_consent_document(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_consent_template_id UUID,
    p_template_version VARCHAR,
    p_document_version INT,
    p_document_title VARCHAR,
    p_document_snapshot TEXT,
    p_status VARCHAR,
    p_signing_deadline TIMESTAMPTZ,
    p_created_by UUID,
    p_created_at TIMESTAMPTZ,
    p_supersedes_document_id UUID,
    p_row_version INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_consent_documents (
        id, tenant_id, organization_id, branch_id, classroom_id, child_id,
        consent_template_id, template_version, document_version, document_title,
        document_snapshot, status, signing_deadline, created_by, created_at,
        supersedes_document_id, row_version
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_child_id,
        p_consent_template_id, p_template_version, p_document_version, p_document_title,
        p_document_snapshot, p_status, p_signing_deadline, p_created_by, p_created_at,
        p_supersedes_document_id, p_row_version
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_consent_signer(
    p_id UUID,
    p_tenant_id UUID,
    p_consent_document_id UUID,
    p_family_relationship_id UUID,
    p_person_id UUID,
    p_intended_signer_user_id UUID,
    p_signer_name_snapshot VARCHAR,
    p_signer_email_snapshot VARCHAR,
    p_signer_relationship VARCHAR,
    p_is_required BOOLEAN,
    p_signing_status VARCHAR,
    p_token_hash VARCHAR,
    p_token_expires_at TIMESTAMPTZ,
    p_invitation_sent_count INT,
    p_last_invitation_sent_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO consent_signers (
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at
    ) VALUES (
        p_id, p_tenant_id, p_consent_document_id, p_family_relationship_id, p_person_id,
        p_intended_signer_user_id, p_signer_name_snapshot, p_signer_email_snapshot,
        p_signer_relationship, p_is_required, p_signing_status, p_token_hash,
        p_token_expires_at, p_invitation_sent_count, p_last_invitation_sent_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_consent_choice(
    p_id UUID,
    p_tenant_id UUID,
    p_consent_document_id UUID,
    p_consent_signer_id UUID,
    p_section_key VARCHAR,
    p_section_title VARCHAR,
    p_is_mandatory BOOLEAN,
    p_choice VARCHAR,
    p_recorded_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO consent_choices (
        id, tenant_id, consent_document_id, consent_signer_id,
        section_key, section_title, is_mandatory, choice, recorded_at
    ) VALUES (
        p_id, p_tenant_id, p_consent_document_id, p_consent_signer_id,
        p_section_key, p_section_title, p_is_mandatory, p_choice, p_recorded_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_child_consent_document(
    p_id UUID,
    p_tenant_id UUID,
    p_status VARCHAR,
    p_admin_reviewed_by UUID,
    p_admin_reviewed_at TIMESTAMPTZ,
    p_admin_review_notes TEXT,
    p_approved_at TIMESTAMPTZ,
    p_sent_at TIMESTAMPTZ,
    p_signed_at TIMESTAMPTZ,
    p_declined_at TIMESTAMPTZ,
    p_withdrawn_at TIMESTAMPTZ,
    p_signed_document_storage_key VARCHAR,
    p_signed_document_checksum VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_consent_documents
    SET status = p_status,
        admin_reviewed_by = p_admin_reviewed_by,
        admin_reviewed_at = p_admin_reviewed_at,
        admin_review_notes = p_admin_review_notes,
        approved_at = p_approved_at,
        sent_at = p_sent_at,
        signed_at = p_signed_at,
        declined_at = p_declined_at,
        withdrawn_at = p_withdrawn_at,
        signed_document_storage_key = p_signed_document_storage_key,
        signed_document_checksum = p_signed_document_checksum,
        row_version = row_version + 1
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_child_consent_document_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    consent_template_id UUID,
    template_version VARCHAR,
    document_version INT,
    document_title VARCHAR,
    document_snapshot TEXT,
    status VARCHAR,
    signing_deadline TIMESTAMPTZ,
    created_by UUID,
    created_at TIMESTAMPTZ,
    admin_reviewed_by UUID,
    admin_reviewed_at TIMESTAMPTZ,
    admin_review_notes TEXT,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    declined_at TIMESTAMPTZ,
    withdrawn_at TIMESTAMPTZ,
    signed_document_storage_key VARCHAR,
    signed_document_checksum VARCHAR,
    supersedes_document_id UUID,
    row_version INT,
    child_name TEXT,
    branch_name VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        d.id, d.tenant_id, d.organization_id, d.branch_id, d.classroom_id, d.child_id,
        d.consent_template_id, d.template_version, d.document_version, d.document_title,
        d.document_snapshot, d.status, d.signing_deadline, d.created_by, d.created_at,
        d.admin_reviewed_by, d.admin_reviewed_at, d.admin_review_notes, d.approved_at,
        d.sent_at, d.signed_at, d.declined_at, d.withdrawn_at,
        d.signed_document_storage_key, d.signed_document_checksum,
        d.supersedes_document_id, d.row_version,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        b.name AS branch_name,
        cl.name AS classroom_name
    FROM child_consent_documents d
    JOIN children c ON d.child_id = c.id
    LEFT JOIN branches b ON d.branch_id = b.id
    LEFT JOIN classrooms cl ON d.classroom_id = cl.id
    WHERE d.tenant_id = p_tenant_id AND d.id = p_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_signers_by_doc_id(
    p_doc_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    consent_document_id UUID,
    family_relationship_id UUID,
    person_id UUID,
    intended_signer_user_id UUID,
    signer_name_snapshot VARCHAR,
    signer_email_snapshot VARCHAR,
    signer_relationship VARCHAR,
    is_required BOOLEAN,
    signing_status VARCHAR,
    token_hash VARCHAR,
    token_expires_at TIMESTAMPTZ,
    invitation_sent_count INT,
    last_invitation_sent_at TIMESTAMPTZ,
    last_opened_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    signature_legal_name VARCHAR,
    signature_intent_text TEXT,
    signature_ip_address VARCHAR,
    signature_user_agent TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE consent_document_id = p_doc_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_choices_by_doc_id(
    p_doc_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    consent_document_id UUID,
    consent_signer_id UUID,
    section_key VARCHAR,
    section_title VARCHAR,
    is_mandatory BOOLEAN,
    choice VARCHAR,
    recorded_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, tenant_id, consent_document_id, consent_signer_id, section_key, section_title, is_mandatory, choice, recorded_at
    FROM consent_choices
    WHERE consent_document_id = p_doc_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_audit_events_by_doc_id(
    p_doc_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    consent_document_id UUID,
    actor_type VARCHAR,
    actor_user_id UUID,
    actor_name VARCHAR,
    event_type VARCHAR,
    timestamp_utc TIMESTAMPTZ,
    correlation_id VARCHAR,
    safe_metadata TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, tenant_id, organization_id, consent_document_id, actor_type, actor_user_id, actor_name, event_type, timestamp_utc, correlation_id, safe_metadata
    FROM consent_audit_events
    WHERE consent_document_id = p_doc_id
    ORDER BY timestamp_utc DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_child_consent_documents(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_child_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    consent_template_id UUID,
    template_version VARCHAR,
    document_version INT,
    document_title VARCHAR,
    document_snapshot TEXT,
    status VARCHAR,
    signing_deadline TIMESTAMPTZ,
    created_by UUID,
    created_at TIMESTAMPTZ,
    admin_reviewed_by UUID,
    admin_reviewed_at TIMESTAMPTZ,
    admin_review_notes TEXT,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    declined_at TIMESTAMPTZ,
    withdrawn_at TIMESTAMPTZ,
    signed_document_storage_key VARCHAR,
    signed_document_checksum VARCHAR,
    supersedes_document_id UUID,
    row_version INT,
    child_name TEXT,
    branch_name VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        d.id, d.tenant_id, d.organization_id, d.branch_id, d.classroom_id, d.child_id,
        d.consent_template_id, d.template_version, d.document_version, d.document_title,
        d.document_snapshot, d.status, d.signing_deadline, d.created_by, d.created_at,
        d.admin_reviewed_by, d.admin_reviewed_at, d.admin_review_notes, d.approved_at,
        d.sent_at, d.signed_at, d.declined_at, d.withdrawn_at,
        d.signed_document_storage_key, d.signed_document_checksum,
        d.supersedes_document_id, d.row_version,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        b.name AS branch_name,
        cl.name AS classroom_name
    FROM child_consent_documents d
    JOIN children c ON d.child_id = c.id
    LEFT JOIN branches b ON d.branch_id = b.id
    LEFT JOIN classrooms cl ON d.classroom_id = cl.id
    WHERE d.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR d.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR d.branch_id = p_branch_id)
      AND (p_child_id IS NULL OR d.child_id = p_child_id)
      AND (p_status IS NULL OR d.status = p_status)
    ORDER BY d.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_signers_by_doc_ids(
    p_doc_ids UUID[]
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    consent_document_id UUID,
    family_relationship_id UUID,
    person_id UUID,
    intended_signer_user_id UUID,
    signer_name_snapshot VARCHAR,
    signer_email_snapshot VARCHAR,
    signer_relationship VARCHAR,
    is_required BOOLEAN,
    signing_status VARCHAR,
    token_hash VARCHAR,
    token_expires_at TIMESTAMPTZ,
    invitation_sent_count INT,
    last_invitation_sent_at TIMESTAMPTZ,
    last_opened_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    signature_legal_name VARCHAR,
    signature_intent_text TEXT,
    signature_ip_address VARCHAR,
    signature_user_agent TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE consent_document_id = ANY(p_doc_ids);
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_signer_by_token_hash(
    p_token_hash VARCHAR
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    consent_document_id UUID,
    family_relationship_id UUID,
    person_id UUID,
    intended_signer_user_id UUID,
    signer_name_snapshot VARCHAR,
    signer_email_snapshot VARCHAR,
    signer_relationship VARCHAR,
    is_required BOOLEAN,
    signing_status VARCHAR,
    token_hash VARCHAR,
    token_expires_at TIMESTAMPTZ,
    invitation_sent_count INT,
    last_invitation_sent_at TIMESTAMPTZ,
    last_opened_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    signature_legal_name VARCHAR,
    signature_intent_text TEXT,
    signature_ip_address VARCHAR,
    signature_user_agent TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE token_hash = p_token_hash;
$$;

CREATE OR REPLACE FUNCTION fn_get_consent_signer_by_id(
    p_tenant_id UUID,
    p_signer_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    consent_document_id UUID,
    family_relationship_id UUID,
    person_id UUID,
    intended_signer_user_id UUID,
    signer_name_snapshot VARCHAR,
    signer_email_snapshot VARCHAR,
    signer_relationship VARCHAR,
    is_required BOOLEAN,
    signing_status VARCHAR,
    token_hash VARCHAR,
    token_expires_at TIMESTAMPTZ,
    invitation_sent_count INT,
    last_invitation_sent_at TIMESTAMPTZ,
    last_opened_at TIMESTAMPTZ,
    signed_at TIMESTAMPTZ,
    signature_legal_name VARCHAR,
    signature_intent_text TEXT,
    signature_ip_address VARCHAR,
    signature_user_agent TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, consent_document_id, family_relationship_id, person_id,
        intended_signer_user_id, signer_name_snapshot, signer_email_snapshot,
        signer_relationship, is_required, signing_status, token_hash,
        token_expires_at, invitation_sent_count, last_invitation_sent_at,
        last_opened_at, signed_at, signature_legal_name, signature_intent_text,
        signature_ip_address, signature_user_agent
    FROM consent_signers
    WHERE tenant_id = p_tenant_id AND id = p_signer_id;
$$;

CREATE OR REPLACE PROCEDURE sp_update_consent_signer(
    p_id UUID,
    p_tenant_id UUID,
    p_signing_status VARCHAR,
    p_token_hash VARCHAR,
    p_token_expires_at TIMESTAMPTZ,
    p_invitation_sent_count INT,
    p_last_invitation_sent_at TIMESTAMPTZ,
    p_last_opened_at TIMESTAMPTZ,
    p_signed_at TIMESTAMPTZ,
    p_signature_legal_name VARCHAR,
    p_signature_intent_text TEXT,
    p_signature_ip_address VARCHAR,
    p_signature_user_agent TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE consent_signers
    SET signing_status = p_signing_status,
        token_hash = p_token_hash,
        token_expires_at = p_token_expires_at,
        invitation_sent_count = p_invitation_sent_count,
        last_invitation_sent_at = p_last_invitation_sent_at,
        last_opened_at = p_last_opened_at,
        signed_at = p_signed_at,
        signature_legal_name = p_signature_legal_name,
        signature_intent_text = p_signature_intent_text,
        signature_ip_address = p_signature_ip_address,
        signature_user_agent = p_signature_user_agent
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_consent_choices(
    p_tenant_id UUID,
    p_doc_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM consent_choices WHERE tenant_id = p_tenant_id AND consent_document_id = p_doc_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_add_consent_audit_event(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_consent_document_id UUID,
    p_actor_type VARCHAR,
    p_actor_user_id UUID,
    p_actor_name VARCHAR,
    p_event_type VARCHAR,
    p_timestamp_utc TIMESTAMPTZ,
    p_correlation_id VARCHAR,
    p_safe_metadata TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO consent_audit_events (
        id, tenant_id, organization_id, consent_document_id,
        actor_type, actor_user_id, actor_name, event_type,
        timestamp_utc, correlation_id, safe_metadata
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_consent_document_id,
        p_actor_type, p_actor_user_id, p_actor_name, p_event_type,
        p_timestamp_utc, p_correlation_id, p_safe_metadata
    );
END;
$$;


-- ------------------------------------------------------------
-- 4. AI & INTELLIGENCE PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

-- Activity Recommendations:
CREATE OR REPLACE PROCEDURE sp_create_activity_recommendation_request(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_requested_by_user_id UUID,
    p_request_timestamp TIMESTAMPTZ,
    p_scope VARCHAR,
    p_context_preferences JSONB,
    p_evidence_count INT,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO activity_recommendation_requests (
        id, tenant_id, organization_id, classroom_id, child_id, requested_by_user_id,
        request_timestamp, scope, context_preferences, evidence_count, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id, p_requested_by_user_id,
        p_request_timestamp, p_scope, p_context_preferences, p_evidence_count, p_created_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_activity_recommendation_item(
    p_id UUID,
    p_request_id UUID,
    p_tenant_id UUID,
    p_experience_id UUID,
    p_experience_title VARCHAR,
    p_suggested_format VARCHAR,
    p_estimated_duration_minutes INT,
    p_rationale TEXT,
    p_supporting_evidence JSONB,
    p_materials JSONB,
    p_rank_order INT,
    p_is_evidence_limited BOOLEAN,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO activity_recommendation_items (
        id, request_id, tenant_id, experience_id, experience_title,
        suggested_format, estimated_duration_minutes, rationale,
        supporting_evidence, materials, rank_order, is_evidence_limited, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_experience_id, p_experience_title,
        p_suggested_format, p_estimated_duration_minutes, p_rationale,
        p_supporting_evidence, p_materials, p_rank_order, p_is_evidence_limited, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_activity_recommendation_request_by_id(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    requested_by_user_id UUID,
    request_timestamp TIMESTAMPTZ,
    scope VARCHAR,
    context_preferences JSONB,
    evidence_count INT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, tenant_id, organization_id, classroom_id, child_id, requested_by_user_id, request_timestamp, scope, context_preferences, evidence_count, created_at
    FROM activity_recommendation_requests
    WHERE id = p_request_id AND tenant_id = p_tenant_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_activity_recommendation_items(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    request_id UUID,
    tenant_id UUID,
    experience_id UUID,
    experience_title VARCHAR,
    suggested_format VARCHAR,
    estimated_duration_minutes INT,
    rationale TEXT,
    supporting_evidence JSONB,
    materials JSONB,
    rank_order INT,
    is_evidence_limited BOOLEAN,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, request_id, tenant_id, experience_id, experience_title, suggested_format, estimated_duration_minutes, rationale, supporting_evidence, materials, rank_order, is_evidence_limited, created_at
    FROM activity_recommendation_items
    WHERE request_id = p_request_id AND tenant_id = p_tenant_id
    ORDER BY rank_order ASC;
$$;

CREATE OR REPLACE PROCEDURE sp_record_activity_recommendation_decision(
    p_id UUID,
    p_request_id UUID,
    p_recommendation_item_id UUID,
    p_tenant_id UUID,
    p_decided_by_user_id UUID,
    p_decision VARCHAR,
    p_rejection_reason TEXT,
    p_resulting_schedule_id UUID,
    p_decided_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO activity_recommendation_decisions (
        id, request_id, recommendation_item_id, tenant_id, decided_by_user_id,
        decision, rejection_reason, resulting_schedule_id, decided_at
    ) VALUES (
        p_id, p_request_id, p_recommendation_item_id, p_tenant_id, p_decided_by_user_id,
        p_decision, p_rejection_reason, p_resulting_schedule_id, p_decided_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_activity_recommendation_decisions(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    request_id UUID,
    recommendation_item_id UUID,
    tenant_id UUID,
    decided_by_user_id UUID,
    decision VARCHAR,
    rejection_reason TEXT,
    resulting_schedule_id UUID,
    decided_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, request_id, recommendation_item_id, tenant_id, decided_by_user_id, decision, rejection_reason, resulting_schedule_id, decided_at
    FROM activity_recommendation_decisions
    WHERE request_id = p_request_id AND tenant_id = p_tenant_id
    ORDER BY decided_at DESC;
$$;

-- Child Journey Insights:
CREATE OR REPLACE PROCEDURE sp_create_child_journey_insight_request(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_requested_by_user_id UUID,
    p_period_type VARCHAR,
    p_period_start_date DATE,
    p_period_end_date DATE,
    p_focus_area VARCHAR,
    p_teacher_question TEXT,
    p_evidence_count INT,
    p_overall_summary TEXT,
    p_evidence_gaps TEXT,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_insight_requests (
        id, tenant_id, organization_id, classroom_id, child_id, 
        requested_by_user_id, period_type, period_start_date, period_end_date, 
        focus_area, teacher_question, evidence_count, overall_summary, evidence_gaps, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id, 
        p_requested_by_user_id, p_period_type, p_period_start_date, p_period_end_date, 
        p_focus_area, p_teacher_question, p_evidence_count, p_overall_summary, p_evidence_gaps, p_created_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_child_journey_insight_item(
    p_id UUID,
    p_request_id UUID,
    p_tenant_id UUID,
    p_category VARCHAR,
    p_classification VARCHAR,
    p_statement TEXT,
    p_evidence_strength VARCHAR,
    p_supporting_evidence JSONB,
    p_suggested_experience_id UUID,
    p_suggested_experience_title VARCHAR,
    p_rank_order INT,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO child_journey_insight_items (
        id, request_id, tenant_id, category, classification, 
        statement, evidence_strength, supporting_evidence, 
        suggested_experience_id, suggested_experience_title, rank_order, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_category, p_classification, 
        p_statement, p_evidence_strength, p_supporting_evidence, 
        p_suggested_experience_id, p_suggested_experience_title, p_rank_order, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_latest_child_journey_insight(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    requested_by_user_id UUID,
    period_type VARCHAR,
    period_start_date DATE,
    period_end_date DATE,
    focus_area VARCHAR,
    teacher_question TEXT,
    evidence_count INT,
    overall_summary TEXT,
    evidence_gaps TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND child_id = p_child_id
    ORDER BY created_at DESC
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_get_child_journey_insight_request_by_id(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    requested_by_user_id UUID,
    period_type VARCHAR,
    period_start_date DATE,
    period_end_date DATE,
    focus_area VARCHAR,
    teacher_question TEXT,
    evidence_count INT,
    overall_summary TEXT,
    evidence_gaps TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, child_id,
        requested_by_user_id, period_type, period_start_date, period_end_date,
        focus_area, teacher_question, evidence_count, overall_summary,
        evidence_gaps, created_at
    FROM child_journey_insight_requests
    WHERE tenant_id = p_tenant_id AND id = p_request_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_child_journey_insight_items(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    request_id UUID,
    tenant_id UUID,
    category VARCHAR,
    classification VARCHAR,
    statement TEXT,
    evidence_strength VARCHAR,
    supporting_evidence TEXT,
    suggested_experience_id UUID,
    suggested_experience_title VARCHAR,
    rank_order INT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, request_id, tenant_id, category, classification,
        statement, evidence_strength, supporting_evidence::text,
        suggested_experience_id, suggested_experience_title,
        rank_order, created_at
    FROM child_journey_insight_items
    WHERE tenant_id = p_tenant_id AND request_id = p_request_id
    ORDER BY rank_order ASC;
$$;

-- Parent Communications:
CREATE OR REPLACE PROCEDURE sp_create_parent_communication(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_recipient_person_id UUID,
    p_recipient_user_id UUID,
    p_recipient_email VARCHAR,
    p_recipient_name VARCHAR,
    p_recipient_relationship VARCHAR,
    p_communication_type VARCHAR,
    p_reporting_date_start DATE,
    p_reporting_date_end DATE,
    p_title VARCHAR,
    p_content_draft TEXT,
    p_content_final TEXT,
    p_family_conversation_starter TEXT,
    p_evidence_summary_json JSONB,
    p_consent_verified BOOLEAN,
    p_consent_notes VARCHAR,
    p_status VARCHAR,
    p_teacher_id UUID,
    p_teacher_name VARCHAR,
    p_approved_by_user_id UUID,
    p_approved_at TIMESTAMPTZ,
    p_sent_at TIMESTAMPTZ,
    p_delivery_channel VARCHAR,
    p_delivery_status VARCHAR,
    p_delivery_error TEXT,
    p_idempotency_key VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO parent_communications (
        id, tenant_id, organization_id, classroom_id, child_id,
        recipient_person_id, recipient_user_id, recipient_email, recipient_name, recipient_relationship,
        communication_type, reporting_date_start, reporting_date_end,
        title, content_draft, content_final, family_conversation_starter,
        evidence_summary_json, consent_verified, consent_notes,
        status, teacher_id, teacher_name, approved_by_user_id, approved_at, sent_at,
        delivery_channel, delivery_status, delivery_error, idempotency_key,
        created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_child_id,
        p_recipient_person_id, p_recipient_user_id, p_recipient_email, p_recipient_name, p_recipient_relationship,
        p_communication_type, p_reporting_date_start, p_reporting_date_end,
        p_title, p_content_draft, p_content_final, p_family_conversation_starter,
        p_evidence_summary_json, p_consent_verified, p_consent_notes,
        p_status, p_teacher_id, p_teacher_name, p_approved_by_user_id, p_approved_at, p_sent_at,
        p_delivery_channel, p_delivery_status, p_delivery_error, p_idempotency_key,
        p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_parent_communication(
    p_id UUID,
    p_tenant_id UUID,
    p_title VARCHAR,
    p_content_final TEXT,
    p_family_conversation_starter TEXT,
    p_evidence_summary_json JSONB,
    p_consent_verified BOOLEAN,
    p_consent_notes VARCHAR,
    p_status VARCHAR,
    p_teacher_name VARCHAR,
    p_approved_by_user_id UUID,
    p_approved_at TIMESTAMPTZ,
    p_sent_at TIMESTAMPTZ,
    p_delivery_channel VARCHAR,
    p_delivery_status VARCHAR,
    p_delivery_error TEXT,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE parent_communications
    SET title = p_title,
        content_final = p_content_final,
        family_conversation_starter = p_family_conversation_starter,
        evidence_summary_json = p_evidence_summary_json,
        consent_verified = p_consent_verified,
        consent_notes = p_consent_notes,
        status = p_status,
        teacher_name = p_teacher_name,
        approved_by_user_id = p_approved_by_user_id,
        approved_at = p_approved_at,
        sent_at = p_sent_at,
        delivery_channel = p_delivery_channel,
        delivery_status = p_delivery_status,
        delivery_error = p_delivery_error,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_parent_communication_by_id(
    p_tenant_id UUID,
    p_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    recipient_person_id UUID,
    recipient_user_id UUID,
    recipient_email VARCHAR,
    recipient_name VARCHAR,
    recipient_relationship VARCHAR,
    communication_type VARCHAR,
    reporting_date_start DATE,
    reporting_date_end DATE,
    title VARCHAR,
    content_draft TEXT,
    content_final TEXT,
    family_conversation_starter TEXT,
    evidence_summary_json JSONB,
    consent_verified BOOLEAN,
    consent_notes VARCHAR,
    status VARCHAR,
    teacher_id UUID,
    teacher_name VARCHAR,
    approved_by_user_id UUID,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    delivery_channel VARCHAR,
    delivery_status VARCHAR,
    delivery_error TEXT,
    idempotency_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.id = p_id AND pc.tenant_id = p_tenant_id
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_get_parent_communications_by_child_id(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    recipient_person_id UUID,
    recipient_user_id UUID,
    recipient_email VARCHAR,
    recipient_name VARCHAR,
    recipient_relationship VARCHAR,
    communication_type VARCHAR,
    reporting_date_start DATE,
    reporting_date_end DATE,
    title VARCHAR,
    content_draft TEXT,
    content_final TEXT,
    family_conversation_starter TEXT,
    evidence_summary_json JSONB,
    consent_verified BOOLEAN,
    consent_notes VARCHAR,
    status VARCHAR,
    teacher_id UUID,
    teacher_name VARCHAR,
    approved_by_user_id UUID,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    delivery_channel VARCHAR,
    delivery_status VARCHAR,
    delivery_error TEXT,
    idempotency_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.child_id = p_child_id AND pc.tenant_id = p_tenant_id
    ORDER BY pc.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_recipients_for_child(
    p_tenant_id UUID,
    p_child_id UUID
)
RETURNS TABLE (
    person_id UUID,
    user_id UUID,
    full_name TEXT,
    relationship VARCHAR,
    email VARCHAR,
    mobile_phone VARCHAR,
    can_receive_information BOOLEAN,
    receives_daily_updates BOOLEAN,
    has_portal_access BOOLEAN,
    photo_consent_granted BOOLEAN
)
LANGUAGE sql
STABLE
AS $$
    SELECT DISTINCT
        p.id AS person_id,
        p.user_id,
        TRIM(CONCAT(p.first_name, ' ', p.last_name)) AS full_name,
        COALESCE(cg.relationship, fr.relationship, p.relationship, 'Guardian')::VARCHAR AS relationship,
        COALESCE(p.email, u.email)::VARCHAR AS email,
        p.mobile_phone,
        COALESCE(cg.can_receive_information, TRUE) AS can_receive_information,
        COALESCE(p.receives_daily_updates, TRUE) AS receives_daily_updates,
        COALESCE(p.has_portal_access, FALSE) AS has_portal_access,
        TRUE AS photo_consent_granted
    FROM persons p
    LEFT JOIN child_guardians cg ON cg.person_id = p.id AND cg.child_id = p_child_id
    LEFT JOIN family_relationships fr ON fr.user_id = p.user_id AND fr.child_id = p_child_id
    LEFT JOIN users u ON p.user_id = u.id
    WHERE p.tenant_id = p_tenant_id
      AND (cg.child_id = p_child_id OR fr.child_id = p_child_id)
      AND (fr.id IS NULL OR (fr.status = 'Verified' AND fr.revoked_at IS NULL));
$$;

CREATE OR REPLACE PROCEDURE sp_update_parent_communication_delivery_status(
    p_tenant_id UUID,
    p_id UUID,
    p_status VARCHAR,
    p_error TEXT,
    p_sent_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE parent_communications
    SET delivery_status = p_status,
        delivery_error = p_error,
        sent_at = COALESCE(p_sent_at, sent_at),
        updated_at = NOW()
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_recent_parent_communications(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL,
    p_limit INT DEFAULT 50
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    child_id UUID,
    recipient_person_id UUID,
    recipient_user_id UUID,
    recipient_email VARCHAR,
    recipient_name VARCHAR,
    recipient_relationship VARCHAR,
    communication_type VARCHAR,
    reporting_date_start DATE,
    reporting_date_end DATE,
    title VARCHAR,
    content_draft TEXT,
    content_final TEXT,
    family_conversation_starter TEXT,
    evidence_summary_json JSONB,
    consent_verified BOOLEAN,
    consent_notes VARCHAR,
    status VARCHAR,
    teacher_id UUID,
    teacher_name VARCHAR,
    approved_by_user_id UUID,
    approved_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    delivery_channel VARCHAR,
    delivery_status VARCHAR,
    delivery_error TEXT,
    idempotency_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    child_name TEXT,
    classroom_name VARCHAR,
    branch_name VARCHAR,
    branch_id UUID
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        pc.id, pc.tenant_id, pc.organization_id, pc.classroom_id, pc.child_id,
        pc.recipient_person_id, pc.recipient_user_id, pc.recipient_email, pc.recipient_name, pc.recipient_relationship,
        pc.communication_type, pc.reporting_date_start, pc.reporting_date_end,
        pc.title, pc.content_draft, pc.content_final, pc.family_conversation_starter,
        pc.evidence_summary_json, pc.consent_verified, pc.consent_notes,
        pc.status, pc.teacher_id, pc.teacher_name, pc.approved_by_user_id, pc.approved_at, pc.sent_at,
        pc.delivery_channel, pc.delivery_status, pc.delivery_error, pc.idempotency_key,
        pc.created_at, pc.updated_at,
        TRIM(CONCAT(c.first_name, ' ', c.last_name)) AS child_name,
        cl.name AS classroom_name,
        b.name AS branch_name,
        cl.branch_id
    FROM parent_communications pc
    JOIN children c ON pc.child_id = c.id
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    LEFT JOIN branches b ON cl.branch_id = b.id
    WHERE pc.tenant_id = p_tenant_id
      AND (p_organization_id IS NULL OR pc.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR pc.classroom_id = p_classroom_id)
    ORDER BY pc.created_at DESC
    LIMIT p_limit;
$$;

CREATE OR REPLACE FUNCTION fn_get_unread_parent_communications_count(
    p_tenant_id UUID,
    p_organization_id UUID DEFAULT NULL,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)
    FROM parent_communications pc
    LEFT JOIN classrooms cl ON pc.classroom_id = cl.id
    WHERE pc.tenant_id = p_tenant_id
      AND pc.teacher_id = '00000000-0000-0000-0000-000000000000'
      AND pc.delivery_status != 'Read'
      AND (p_organization_id IS NULL OR pc.organization_id = p_organization_id)
      AND (p_branch_id IS NULL OR cl.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR pc.classroom_id = p_classroom_id);
$$;

CREATE OR REPLACE PROCEDURE sp_mark_parent_communications_as_read(
    p_tenant_id UUID,
    p_child_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE parent_communications
    SET delivery_status = 'Read', updated_at = NOW()
    WHERE tenant_id = p_tenant_id
      AND child_id = p_child_id
      AND teacher_id = '00000000-0000-0000-0000-000000000000'
      AND delivery_status != 'Read';
END;
$$;

-- Weekly Planning:
CREATE OR REPLACE PROCEDURE sp_create_weekly_planning_draft_request(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_requested_by_user_id UUID,
    p_week_start_date DATE,
    p_week_end_date DATE,
    p_focus_areas VARCHAR,
    p_indoor_outdoor_preference VARCHAR,
    p_materials_notes TEXT,
    p_status VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO weekly_planning_draft_requests (
        id, tenant_id, organization_id, classroom_id, requested_by_user_id, 
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference, 
        materials_notes, status, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_classroom_id, p_requested_by_user_id, 
        p_week_start_date, p_week_end_date, p_focus_areas, p_indoor_outdoor_preference, 
        p_materials_notes, p_status, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_weekly_planning_draft_item(
    p_id UUID,
    p_request_id UUID,
    p_tenant_id UUID,
    p_scheduled_date DATE,
    p_day_of_week VARCHAR,
    p_time_of_day VARCHAR,
    p_experience_id UUID,
    p_experience_title VARCHAR,
    p_learning_goal TEXT,
    p_group_format VARCHAR,
    p_estimated_duration_minutes INT,
    p_required_materials TEXT,
    p_rationale TEXT,
    p_validation_status VARCHAR,
    p_validation_notes TEXT,
    p_resulting_schedule_id UUID,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO weekly_planning_draft_items (
        id, request_id, tenant_id, scheduled_date, day_of_week, time_of_day, 
        experience_id, experience_title, learning_goal, group_format, 
        estimated_duration_minutes, required_materials, rationale, 
        validation_status, validation_notes, resulting_schedule_id, created_at
    ) VALUES (
        p_id, p_request_id, p_tenant_id, p_scheduled_date, p_day_of_week, p_time_of_day, 
        p_experience_id, p_experience_title, p_learning_goal, p_group_format, 
        p_estimated_duration_minutes, p_required_materials, p_rationale, 
        p_validation_status, p_validation_notes, p_resulting_schedule_id, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_weekly_planning_draft_by_id(
    p_tenant_id UUID,
    p_draft_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    requested_by_user_id UUID,
    week_start_date DATE,
    week_end_date DATE,
    focus_areas VARCHAR,
    indoor_outdoor_preference VARCHAR,
    materials_notes TEXT,
    status VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, requested_by_user_id,
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference,
        materials_notes, status, created_at, updated_at
    FROM weekly_planning_draft_requests
    WHERE tenant_id = p_tenant_id AND id = p_draft_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_latest_weekly_planning_draft_for_week(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_week_start_date DATE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    classroom_id UUID,
    requested_by_user_id UUID,
    week_start_date DATE,
    week_end_date DATE,
    focus_areas VARCHAR,
    indoor_outdoor_preference VARCHAR,
    materials_notes TEXT,
    status VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, classroom_id, requested_by_user_id,
        week_start_date, week_end_date, focus_areas, indoor_outdoor_preference,
        materials_notes, status, created_at, updated_at
    FROM weekly_planning_draft_requests
    WHERE tenant_id = p_tenant_id AND classroom_id = p_classroom_id AND week_start_date = p_week_start_date
    ORDER BY created_at DESC
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_get_weekly_planning_draft_items(
    p_tenant_id UUID,
    p_request_id UUID
)
RETURNS TABLE (
    id UUID,
    request_id UUID,
    tenant_id UUID,
    scheduled_date DATE,
    day_of_week VARCHAR,
    time_of_day VARCHAR,
    experience_id UUID,
    experience_title VARCHAR,
    learning_goal TEXT,
    group_format VARCHAR,
    estimated_duration_minutes INT,
    required_materials TEXT,
    rationale TEXT,
    validation_status VARCHAR,
    validation_notes TEXT,
    resulting_schedule_id UUID,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, request_id, tenant_id, scheduled_date, day_of_week, time_of_day,
        experience_id, experience_title, learning_goal, group_format,
        estimated_duration_minutes, required_materials, rationale,
        validation_status, validation_notes, resulting_schedule_id, created_at
    FROM weekly_planning_draft_items
    WHERE tenant_id = p_tenant_id AND request_id = p_request_id
    ORDER BY scheduled_date ASC, time_of_day ASC;
$$;

CREATE OR REPLACE PROCEDURE sp_update_weekly_planning_draft_item(
    p_id UUID,
    p_tenant_id UUID,
    p_scheduled_date DATE,
    p_day_of_week VARCHAR,
    p_time_of_day VARCHAR,
    p_experience_id UUID,
    p_experience_title VARCHAR,
    p_learning_goal TEXT,
    p_group_format VARCHAR,
    p_estimated_duration_minutes INT,
    p_required_materials TEXT,
    p_rationale TEXT,
    p_validation_status VARCHAR,
    p_validation_notes TEXT,
    p_resulting_schedule_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE weekly_planning_draft_items SET
        scheduled_date = p_scheduled_date,
        day_of_week = p_day_of_week,
        time_of_day = p_time_of_day,
        experience_id = p_experience_id,
        experience_title = p_experience_title,
        learning_goal = p_learning_goal,
        group_format = p_group_format,
        estimated_duration_minutes = p_estimated_duration_minutes,
        required_materials = p_required_materials,
        rationale = p_rationale,
        validation_status = p_validation_status,
        validation_notes = p_validation_notes,
        resulting_schedule_id = p_resulting_schedule_id
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_weekly_planning_draft_status(
    p_tenant_id UUID,
    p_draft_id UUID,
    p_status VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE weekly_planning_draft_requests SET
        status = p_status,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_draft_id AND tenant_id = p_tenant_id;
END;
$$;


-- ------------------------------------------------------------
-- 5. AUDIT LOGGING PROCEDURE
-- ------------------------------------------------------------

CREATE OR REPLACE PROCEDURE sp_log_audit_entry(
    p_tenant_id UUID,
    p_user_id UUID,
    p_action VARCHAR,
    p_entity_type VARCHAR,
    p_entity_id UUID,
    p_details JSONB,
    p_ip_address VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (tenant_id, user_id, action, entity_type, entity_id, details, ip_address)
    VALUES (
        p_tenant_id, 
        CASE WHEN EXISTS (SELECT 1 FROM users WHERE id = p_user_id) THEN p_user_id ELSE NULL END, 
        p_action, p_entity_type, p_entity_id, p_details, p_ip_address
    );
END;
$$;
