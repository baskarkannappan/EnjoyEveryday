-- ============================================================
-- Migration 064: Commercial, Billing, Invoicing, and Material Marketplace Procedures
-- Converts inline SQL queries from:
--   - EnrollmentPipelineRepository
--   - BillingRepository
--   - MaterialMarketplaceRepository
-- into PostgreSQL stored procedures and functions.
-- ============================================================

-- ------------------------------------------------------------
-- 1. ENROLLMENT PIPELINE PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_enrollment_applications(
    p_tenant_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_stage VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    branch_id UUID,
    branch_name VARCHAR,
    child_first_name VARCHAR,
    child_last_name VARCHAR,
    date_of_birth DATE,
    gender VARCHAR,
    parent_guardian_name VARCHAR,
    parent_email VARCHAR,
    parent_phone VARCHAR,
    target_classroom_id UUID,
    target_classroom_name VARCHAR,
    desired_start_date DATE,
    schedule_type VARCHAR,
    pipeline_stage VARCHAR,
    tour_date TIMESTAMPTZ,
    tuition_monthly_rate NUMERIC,
    deposit_amount NUMERIC,
    sibling_discount_percent NUMERIC,
    registration_fee NUMERIC,
    internal_notes TEXT,
    child_id UUID,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        a.id,
        a.branch_id,
        b.name AS branch_name,
        a.child_first_name,
        a.child_last_name,
        a.date_of_birth,
        a.gender,
        a.parent_guardian_name,
        a.parent_email,
        a.parent_phone,
        a.target_classroom_id,
        c.name AS target_classroom_name,
        a.desired_start_date,
        a.schedule_type,
        a.pipeline_stage,
        a.tour_date,
        a.tuition_monthly_rate,
        a.deposit_amount,
        a.sibling_discount_percent,
        a.registration_fee,
        a.internal_notes,
        a.child_id,
        a.created_at
    FROM enrollment_pipeline_applications a
    LEFT JOIN branches b ON a.branch_id = b.id
    LEFT JOIN classrooms c ON a.target_classroom_id = c.id
    WHERE a.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR a.branch_id = p_branch_id)
      AND (p_stage IS NULL OR a.pipeline_stage = p_stage)
    ORDER BY a.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_enrollment_application_by_id(
    p_tenant_id UUID,
    p_application_id UUID
)
RETURNS TABLE (
    id UUID,
    branch_id UUID,
    branch_name VARCHAR,
    child_first_name VARCHAR,
    child_last_name VARCHAR,
    date_of_birth DATE,
    gender VARCHAR,
    parent_guardian_name VARCHAR,
    parent_email VARCHAR,
    parent_phone VARCHAR,
    target_classroom_id UUID,
    target_classroom_name VARCHAR,
    desired_start_date DATE,
    schedule_type VARCHAR,
    pipeline_stage VARCHAR,
    tour_date TIMESTAMPTZ,
    tuition_monthly_rate NUMERIC,
    deposit_amount NUMERIC,
    sibling_discount_percent NUMERIC,
    registration_fee NUMERIC,
    internal_notes TEXT,
    child_id UUID,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        a.id,
        a.branch_id,
        b.name AS branch_name,
        a.child_first_name,
        a.child_last_name,
        a.date_of_birth,
        a.gender,
        a.parent_guardian_name,
        a.parent_email,
        a.parent_phone,
        a.target_classroom_id,
        c.name AS target_classroom_name,
        a.desired_start_date,
        a.schedule_type,
        a.pipeline_stage,
        a.tour_date,
        a.tuition_monthly_rate,
        a.deposit_amount,
        a.sibling_discount_percent,
        a.registration_fee,
        a.internal_notes,
        a.child_id,
        a.created_at
    FROM enrollment_pipeline_applications a
    LEFT JOIN branches b ON a.branch_id = b.id
    LEFT JOIN classrooms c ON a.target_classroom_id = c.id
    WHERE a.tenant_id = p_tenant_id AND a.id = p_application_id;
$$;

CREATE OR REPLACE PROCEDURE sp_create_enrollment_application(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_child_first_name VARCHAR,
    p_child_last_name VARCHAR,
    p_date_of_birth DATE,
    p_gender VARCHAR,
    p_parent_guardian_name VARCHAR,
    p_parent_email VARCHAR,
    p_parent_phone VARCHAR,
    p_target_classroom_id UUID,
    p_desired_start_date DATE,
    p_schedule_type VARCHAR,
    p_pipeline_stage VARCHAR,
    p_tour_date TIMESTAMPTZ,
    p_tuition_monthly_rate NUMERIC,
    p_deposit_amount NUMERIC,
    p_sibling_discount_percent NUMERIC,
    p_registration_fee NUMERIC,
    p_internal_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO enrollment_pipeline_applications (
        id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
        parent_guardian_name, parent_email, parent_phone, target_classroom_id, desired_start_date,
        schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
        sibling_discount_percent, registration_fee, internal_notes
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_child_first_name, p_child_last_name, p_date_of_birth, p_gender,
        p_parent_guardian_name, p_parent_email, p_parent_phone, p_target_classroom_id, p_desired_start_date,
        p_schedule_type, p_pipeline_stage, p_tour_date, p_tuition_monthly_rate, p_deposit_amount,
        p_sibling_discount_percent, p_registration_fee, p_internal_notes
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_enrollment_application_stage(
    p_tenant_id UUID,
    p_application_id UUID,
    p_new_stage VARCHAR,
    p_notes TEXT,
    p_tour_date TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE enrollment_pipeline_applications
    SET pipeline_stage = p_new_stage,
        tour_date = COALESCE(p_tour_date, tour_date),
        internal_notes = CASE WHEN p_notes IS NOT NULL THEN p_notes ELSE internal_notes END,
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_application_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_ensure_child_enrolled(
    p_child_id UUID,
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_date_of_birth DATE
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth, enrollment_status, is_active, created_at, updated_at)
    VALUES (p_child_id, p_tenant_id, p_classroom_id, p_first_name, p_last_name, p_date_of_birth, 'Enrolled', TRUE, NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        classroom_id = EXCLUDED.classroom_id,
        enrollment_status = 'Enrolled',
        updated_at = NOW();
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_family_billing_contract(
    p_id UUID,
    p_tenant_id UUID,
    p_branch_id UUID,
    p_application_id UUID,
    p_child_id UUID,
    p_parent_name VARCHAR,
    p_parent_email VARCHAR,
    p_schedule_type VARCHAR,
    p_billing_cycle VARCHAR,
    p_base_rate NUMERIC,
    p_discount_percent NUMERIC,
    p_effective_rate NUMERIC,
    p_deposit_held NUMERIC,
    p_start_date DATE
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO family_billing_contracts (
        id, tenant_id, branch_id, application_id, child_id, parent_name, parent_email,
        schedule_type, billing_cycle, base_rate, discount_percent, effective_rate,
        deposit_held, status, start_date, auto_invoice
    ) VALUES (
        p_id, p_tenant_id, p_branch_id, p_application_id, p_child_id, p_parent_name, p_parent_email,
        p_schedule_type, p_billing_cycle, p_base_rate, p_discount_percent, p_effective_rate,
        p_deposit_held, 'Active', p_start_date, TRUE
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_initial_tuition_invoice(
    p_invoice_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_invoice_num VARCHAR,
    p_total_amount NUMERIC,
    p_desc TEXT,
    p_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, invoice_number, currency, issue_date, due_date, status,
        subtotal, tax_amount, total_amount, amount_paid, description, notes, created_by, created_at, updated_at
    ) VALUES (
        p_invoice_id, p_tenant_id, p_organization_id, p_invoice_num, 'INR', NOW(), NOW() + INTERVAL '7 days', 'Issued',
        p_total_amount, 0, p_total_amount, 0,
        p_desc, p_notes, 'Enrollment System', NOW(), NOW()
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_mark_application_enrolled(
    p_tenant_id UUID,
    p_application_id UUID,
    p_child_id UUID,
    p_classroom_id UUID,
    p_base_rate NUMERIC,
    p_deposit_held NUMERIC,
    p_discount_percent NUMERIC,
    p_enrollment_log TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE enrollment_pipeline_applications
    SET pipeline_stage = 'Enrolled',
        child_id = p_child_id,
        target_classroom_id = p_classroom_id,
        tuition_monthly_rate = p_base_rate,
        deposit_amount = p_deposit_held,
        sibling_discount_percent = p_discount_percent,
        internal_notes = COALESCE(internal_notes || E'\n', '') || p_enrollment_log,
        updated_at = NOW()
    WHERE tenant_id = p_tenant_id AND id = p_application_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_classroom_vacancies(
    p_tenant_id UUID,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    classroom_id UUID,
    classroom_name VARCHAR,
    age_group VARCHAR,
    capacity INT,
    current_enrolled BIGINT,
    pending_offers BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        c.id AS classroom_id,
        c.name AS classroom_name,
        COALESCE(c.age_group, c.classroom_type, 'Toddler')::VARCHAR AS age_group,
        COALESCE(c.capacity, 15) AS capacity,
        (SELECT COUNT(*) FROM children ch WHERE ch.classroom_id = c.id AND ch.is_active = TRUE) AS current_enrolled,
        (SELECT COUNT(*) FROM enrollment_pipeline_applications a WHERE a.target_classroom_id = c.id AND a.pipeline_stage IN ('UnderReview', 'OfferSent')) AS pending_offers
    FROM classrooms c
    WHERE c.tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR c.branch_id = p_branch_id)
    ORDER BY c.name;
$$;

CREATE OR REPLACE FUNCTION fn_get_active_family_contracts(
    p_tenant_id UUID,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    branch_id UUID,
    application_id UUID,
    child_id UUID,
    parent_name VARCHAR,
    parent_email VARCHAR,
    schedule_type VARCHAR,
    billing_cycle VARCHAR,
    base_rate NUMERIC,
    discount_percent NUMERIC,
    effective_rate NUMERIC,
    deposit_held NUMERIC,
    status VARCHAR,
    start_date DATE,
    end_date DATE,
    auto_invoice BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, branch_id, application_id,
        child_id, parent_name, parent_email,
        schedule_type, billing_cycle,
        base_rate, discount_percent, effective_rate,
        deposit_held, status, start_date, end_date,
        auto_invoice, created_at, updated_at
    FROM family_billing_contracts
    WHERE tenant_id = p_tenant_id
      AND (p_branch_id IS NULL OR branch_id = p_branch_id)
    ORDER BY created_at DESC;
$$;



-- ------------------------------------------------------------
-- 2. BILLING & INVOICING PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_invoices_for_organization(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_include_drafts BOOLEAN DEFAULT TRUE
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    published_by VARCHAR,
    voided_at TIMESTAMPTZ,
    voided_by VARCHAR,
    void_reason TEXT,
    created_by VARCHAR,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    billing_contact_email VARCHAR,
    document_storage_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE organization_id = p_organization_id 
      AND tenant_id = p_tenant_id
      AND (p_include_drafts OR status != 'Draft')
    ORDER BY issue_date DESC, created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_all_invoices(
    p_tenant_id UUID DEFAULT NULL,
    p_organization_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    published_by VARCHAR,
    voided_at TIMESTAMPTZ,
    voided_by VARCHAR,
    void_reason TEXT,
    created_by VARCHAR,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    billing_contact_email VARCHAR,
    document_storage_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE (p_tenant_id IS NULL OR tenant_id = p_tenant_id)
      AND (p_organization_id IS NULL OR organization_id = p_organization_id)
      AND (p_status IS NULL OR status = p_status)
    ORDER BY issue_date DESC, created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_invoice_by_id(
    p_tenant_id UUID DEFAULT NULL,
    p_invoice_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    published_by VARCHAR,
    voided_at TIMESTAMPTZ,
    voided_by VARCHAR,
    void_reason TEXT,
    created_by VARCHAR,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    billing_contact_email VARCHAR,
    document_storage_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE id = p_invoice_id
      AND (p_tenant_id IS NULL OR tenant_id = p_tenant_id);
$$;

CREATE OR REPLACE FUNCTION fn_get_invoice_by_number(
    p_invoice_number VARCHAR
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_number VARCHAR,
    billing_period VARCHAR,
    subtotal NUMERIC,
    discount_amount NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    amount_paid NUMERIC,
    currency VARCHAR,
    status VARCHAR,
    issue_date TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    published_by VARCHAR,
    voided_at TIMESTAMPTZ,
    voided_by VARCHAR,
    void_reason TEXT,
    created_by VARCHAR,
    description TEXT,
    notes TEXT,
    payment_instructions TEXT,
    billing_contact_email VARCHAR,
    document_storage_key VARCHAR,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    FROM invoices
    WHERE invoice_number = p_invoice_number;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_invoice(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_invoice_number VARCHAR,
    p_billing_period VARCHAR,
    p_subtotal NUMERIC,
    p_discount_amount NUMERIC,
    p_tax_amount NUMERIC,
    p_total_amount NUMERIC,
    p_amount_paid NUMERIC,
    p_currency VARCHAR,
    p_status VARCHAR,
    p_issue_date TIMESTAMPTZ,
    p_due_date TIMESTAMPTZ,
    p_paid_at TIMESTAMPTZ,
    p_published_at TIMESTAMPTZ,
    p_published_by VARCHAR,
    p_voided_at TIMESTAMPTZ,
    p_voided_by VARCHAR,
    p_void_reason TEXT,
    p_created_by VARCHAR,
    p_description TEXT,
    p_notes TEXT,
    p_payment_instructions TEXT,
    p_billing_contact_email VARCHAR,
    p_document_storage_key VARCHAR,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, invoice_number, billing_period,
        subtotal, discount_amount, tax_amount, total_amount, amount_paid,
        currency, status, issue_date, due_date, paid_at, published_at,
        published_by, voided_at, voided_by, void_reason, created_by,
        description, notes, payment_instructions, billing_contact_email,
        document_storage_key, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_invoice_number, p_billing_period,
        p_subtotal, p_discount_amount, p_tax_amount, p_total_amount, p_amount_paid,
        p_currency, p_status, p_issue_date, p_due_date, p_paid_at, p_published_at,
        p_published_by, p_voided_at, p_voided_by, p_void_reason, p_created_by,
        p_description, p_notes, p_payment_instructions, p_billing_contact_email,
        p_document_storage_key, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_invoice(
    p_id UUID,
    p_tenant_id UUID,
    p_subtotal NUMERIC,
    p_discount_amount NUMERIC,
    p_tax_amount NUMERIC,
    p_total_amount NUMERIC,
    p_amount_paid NUMERIC,
    p_status VARCHAR,
    p_issue_date TIMESTAMPTZ,
    p_due_date TIMESTAMPTZ,
    p_paid_at TIMESTAMPTZ,
    p_published_at TIMESTAMPTZ,
    p_published_by VARCHAR,
    p_voided_at TIMESTAMPTZ,
    p_voided_by VARCHAR,
    p_void_reason TEXT,
    p_description TEXT,
    p_notes TEXT,
    p_payment_instructions TEXT,
    p_billing_contact_email VARCHAR,
    p_document_storage_key VARCHAR,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE invoices SET
        subtotal = p_subtotal,
        discount_amount = p_discount_amount,
        tax_amount = p_tax_amount,
        total_amount = p_total_amount,
        amount_paid = p_amount_paid,
        status = p_status,
        issue_date = p_issue_date,
        due_date = p_due_date,
        paid_at = p_paid_at,
        published_at = p_published_at,
        published_by = p_published_by,
        voided_at = p_voided_at,
        voided_by = p_voided_by,
        void_reason = p_void_reason,
        description = p_description,
        notes = p_notes,
        payment_instructions = p_payment_instructions,
        billing_contact_email = p_billing_contact_email,
        document_storage_key = p_document_storage_key,
        updated_at = p_updated_at
    WHERE id = p_id AND tenant_id = p_tenant_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_invoice_line_item(
    p_id UUID,
    p_invoice_id UUID,
    p_line_number INT,
    p_description VARCHAR,
    p_quantity NUMERIC,
    p_unit_price NUMERIC,
    p_line_total NUMERIC,
    p_category VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO invoice_line_items (
        id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    ) VALUES (
        p_id, p_invoice_id, p_line_number, p_description, p_quantity, p_unit_price, p_line_total, p_category, p_created_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_invoice_line_items(
    p_invoice_id UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM invoice_line_items WHERE invoice_id = p_invoice_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_invoice_line_items(
    p_invoice_id UUID
)
RETURNS TABLE (
    id UUID,
    invoice_id UUID,
    line_number INT,
    description VARCHAR,
    quantity NUMERIC,
    unit_price NUMERIC,
    line_total NUMERIC,
    category VARCHAR,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    FROM invoice_line_items
    WHERE invoice_id = p_invoice_id
    ORDER BY line_number ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_invoice_line_items_by_ids(
    p_invoice_ids UUID[]
)
RETURNS TABLE (
    id UUID,
    invoice_id UUID,
    line_number INT,
    description VARCHAR,
    quantity NUMERIC,
    unit_price NUMERIC,
    line_total NUMERIC,
    category VARCHAR,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
    FROM invoice_line_items
    WHERE invoice_id = ANY(p_invoice_ids)
    ORDER BY line_number ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_payments_by_invoice_ids(
    p_invoice_ids UUID[]
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE invoice_id = ANY(p_invoice_ids)
    ORDER BY payment_date ASC;
$$;

CREATE OR REPLACE PROCEDURE sp_insert_payment(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_invoice_id UUID,
    p_invoice_number VARCHAR,
    p_payment_reference VARCHAR,
    p_amount NUMERIC,
    p_currency VARCHAR,
    p_method VARCHAR,
    p_is_verified BOOLEAN,
    p_verified_by VARCHAR,
    p_verified_at TIMESTAMPTZ,
    p_status VARCHAR,
    p_transaction_id VARCHAR,
    p_notes TEXT,
    p_provider_order_id VARCHAR,
    p_provider_payment_id VARCHAR,
    p_provider_signature VARCHAR,
    p_receipt_number VARCHAR,
    p_recorded_by_user_id UUID,
    p_recorded_by_email VARCHAR,
    p_is_reversed BOOLEAN,
    p_reversed_at TIMESTAMPTZ,
    p_reversed_by VARCHAR,
    p_reversal_reason TEXT,
    p_reversal_reference VARCHAR,
    p_audit_metadata TEXT,
    p_payment_date TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO payments (
        id, tenant_id, organization_id, invoice_id, invoice_number,
        payment_reference, amount, currency, method, is_verified,
        verified_by, verified_at, status, transaction_id, notes,
        provider_order_id, provider_payment_id, provider_signature,
        receipt_number, recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by, reversal_reason, reversal_reference,
        audit_metadata, payment_date, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_invoice_id, p_invoice_number,
        p_payment_reference, p_amount, p_currency, p_method, p_is_verified,
        p_verified_by, p_verified_at, p_status, p_transaction_id, p_notes,
        p_provider_order_id, p_provider_payment_id, p_provider_signature,
        p_receipt_number, p_recorded_by_user_id, p_recorded_by_email,
        p_is_reversed, p_reversed_at, p_reversed_by, p_reversal_reason, p_reversal_reference,
        CASE WHEN p_audit_metadata IS NULL THEN NULL ELSE p_audit_metadata::jsonb END,
        p_payment_date, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_payment_by_id(
    p_payment_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE id = p_payment_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_payment_by_provider_payment_id(
    p_provider_payment_id VARCHAR
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE provider_payment_id = p_provider_payment_id AND is_reversed = FALSE
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_get_payment_by_receipt_number(
    p_receipt_number VARCHAR
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE receipt_number = p_receipt_number AND is_reversed = FALSE
    LIMIT 1;
$$;

CREATE OR REPLACE PROCEDURE sp_reverse_payment(
    p_payment_id UUID,
    p_reversed_by VARCHAR,
    p_reason TEXT,
    p_reversal_reference VARCHAR,
    p_reversed_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE payments
    SET is_reversed = TRUE,
        status = 'Reversed',
        reversed_at = p_reversed_at,
        reversed_by = p_reversed_by,
        reversal_reason = p_reason,
        reversal_reference = p_reversal_reference
    WHERE id = p_payment_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_recalculate_invoice_balance(
    p_invoice_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_total_paid NUMERIC;
BEGIN
    -- 1. Calculate sum of valid non-reversed completed payments
    SELECT COALESCE(SUM(amount), 0)
    INTO v_total_paid
    FROM payments
    WHERE invoice_id = p_invoice_id
      AND status = 'Completed'
      AND is_reversed = FALSE;

    -- 2. Update invoice amount_paid, paid_at, and status
    UPDATE invoices
    SET amount_paid = v_total_paid,
        paid_at = CASE 
            WHEN (total_amount - v_total_paid) <= 0 THEN COALESCE(paid_at, NOW())
            ELSE NULL 
        END,
        status = CASE
            WHEN status = 'Void' THEN 'Void'
            WHEN status = 'Draft' THEN 'Draft'
            WHEN (total_amount - v_total_paid) <= 0 THEN 'Paid'
            WHEN v_total_paid > 0 THEN 'PartiallyPaid'
            ELSE 'Unpaid'
        END,
        updated_at = NOW()
    WHERE id = p_invoice_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_payments_for_invoice(
    p_invoice_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE invoice_id = p_invoice_id
    ORDER BY payment_date ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_payments_for_organization(
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    invoice_id UUID,
    invoice_number VARCHAR,
    payment_reference VARCHAR,
    amount NUMERIC,
    currency VARCHAR,
    method VARCHAR,
    is_verified BOOLEAN,
    verified_by VARCHAR,
    verified_at TIMESTAMPTZ,
    status VARCHAR,
    transaction_id VARCHAR,
    notes TEXT,
    provider_order_id VARCHAR,
    provider_payment_id VARCHAR,
    provider_signature VARCHAR,
    receipt_number VARCHAR,
    recorded_by_user_id UUID,
    recorded_by_email VARCHAR,
    is_reversed BOOLEAN,
    reversed_at TIMESTAMPTZ,
    reversed_by VARCHAR,
    reversal_reason TEXT,
    reversal_reference VARCHAR,
    audit_metadata TEXT,
    payment_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, 
        invoice_id, invoice_number,
        payment_reference, amount, currency, method, 
        is_verified, verified_by, verified_at, 
        status, transaction_id, notes,
        provider_order_id, provider_payment_id, 
        provider_signature, receipt_number,
        recorded_by_user_id, recorded_by_email,
        is_reversed, reversed_at, reversed_by,
        reversal_reason, reversal_reference,
        audit_metadata::text, payment_date, created_at
    FROM payments
    WHERE organization_id = p_organization_id
    ORDER BY payment_date DESC, created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_organization_billing_settings(
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    mode VARCHAR,
    frequency VARCHAR,
    effective_date TIMESTAMPTZ,
    next_billing_date TIMESTAMPTZ,
    auto_charge_enabled BOOLEAN,
    notes TEXT,
    updated_by VARCHAR,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    FROM organization_billing_settings
    WHERE organization_id = p_organization_id;
$$;

CREATE OR REPLACE PROCEDURE sp_upsert_organization_billing_settings(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_mode VARCHAR,
    p_frequency VARCHAR,
    p_effective_date TIMESTAMPTZ,
    p_next_billing_date TIMESTAMPTZ,
    p_auto_charge_enabled BOOLEAN,
    p_notes TEXT,
    p_updated_by VARCHAR,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO organization_billing_settings (
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_mode, p_frequency, p_effective_date,
        p_next_billing_date, p_auto_charge_enabled, p_notes, p_updated_by, p_updated_at
    )
    ON CONFLICT (organization_id) DO UPDATE SET
        mode = EXCLUDED.mode,
        frequency = EXCLUDED.frequency,
        effective_date = EXCLUDED.effective_date,
        next_billing_date = EXCLUDED.next_billing_date,
        auto_charge_enabled = EXCLUDED.auto_charge_enabled,
        notes = EXCLUDED.notes,
        updated_by = EXCLUDED.updated_by,
        updated_at = EXCLUDED.updated_at;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_organizations_due_for_automatic_billing(
    p_as_of_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    mode VARCHAR,
    frequency VARCHAR,
    effective_date TIMESTAMPTZ,
    next_billing_date TIMESTAMPTZ,
    auto_charge_enabled BOOLEAN,
    notes TEXT,
    updated_by VARCHAR,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, tenant_id, organization_id, mode, frequency, effective_date,
        next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
    FROM organization_billing_settings
    WHERE mode = 'Automatic'
      AND effective_date <= p_as_of_date
      AND (next_billing_date IS NULL OR next_billing_date <= p_as_of_date);
$$;


-- ------------------------------------------------------------
-- 3. MATERIAL MARKETPLACE & WAREHOUSE PROCEDURES & FUNCTIONS
-- ------------------------------------------------------------

CREATE OR REPLACE FUNCTION fn_get_material_products(
    p_category VARCHAR DEFAULT NULL,
    p_material_type VARCHAR DEFAULT NULL,
    p_active_only BOOLEAN DEFAULT TRUE
)
RETURNS TABLE (
    id UUID,
    sku VARCHAR,
    name VARCHAR,
    description TEXT,
    category VARCHAR,
    image_url VARCHAR,
    material_type VARCHAR,
    safety_instructions TEXT,
    age_min_months INT,
    age_max_months INT,
    usage_restrictions TEXT,
    cleaning_instructions TEXT,
    return_policy TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    off_id UUID,
    off_prod_id UUID,
    allow_purchase BOOLEAN,
    purchase_price NUMERIC,
    allow_borrow BOOLEAN,
    rental_fee NUMERIC,
    refundable_deposit NUMERIC,
    default_loan_duration_days INT,
    max_loan_duration_days INT,
    currency VARCHAR,
    off_is_active BOOLEAN,
    bal_id UUID,
    total_stock INT,
    available_stock INT,
    reserved_stock INT,
    dispatched_stock INT,
    on_loan_stock INT,
    under_inspection_stock INT,
    under_cleaning_stock INT,
    damaged_stock INT,
    missing_stock INT,
    retired_stock INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE (NOT p_active_only OR p.is_active = TRUE)
      AND (p_category IS NULL OR p_category = 'All' OR p.category = p_category)
      AND (p_material_type IS NULL OR p_material_type = 'All' OR p.material_type = p_material_type)
    ORDER BY p.name ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_product_by_id(
    p_product_id UUID
)
RETURNS TABLE (
    id UUID,
    sku VARCHAR,
    name VARCHAR,
    description TEXT,
    category VARCHAR,
    image_url VARCHAR,
    material_type VARCHAR,
    safety_instructions TEXT,
    age_min_months INT,
    age_max_months INT,
    usage_restrictions TEXT,
    cleaning_instructions TEXT,
    return_policy TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    off_id UUID,
    off_prod_id UUID,
    allow_purchase BOOLEAN,
    purchase_price NUMERIC,
    allow_borrow BOOLEAN,
    rental_fee NUMERIC,
    refundable_deposit NUMERIC,
    default_loan_duration_days INT,
    max_loan_duration_days INT,
    currency VARCHAR,
    off_is_active BOOLEAN,
    bal_id UUID,
    total_stock INT,
    available_stock INT,
    reserved_stock INT,
    dispatched_stock INT,
    on_loan_stock INT,
    under_inspection_stock INT,
    under_cleaning_stock INT,
    damaged_stock INT,
    missing_stock INT,
    retired_stock INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE p.id = p_product_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_product_by_sku(
    p_sku VARCHAR
)
RETURNS TABLE (
    id UUID,
    sku VARCHAR,
    name VARCHAR,
    description TEXT,
    category VARCHAR,
    image_url VARCHAR,
    material_type VARCHAR,
    safety_instructions TEXT,
    age_min_months INT,
    age_max_months INT,
    usage_restrictions TEXT,
    cleaning_instructions TEXT,
    return_policy TEXT,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    off_id UUID,
    off_prod_id UUID,
    allow_purchase BOOLEAN,
    purchase_price NUMERIC,
    allow_borrow BOOLEAN,
    rental_fee NUMERIC,
    refundable_deposit NUMERIC,
    default_loan_duration_days INT,
    max_loan_duration_days INT,
    currency VARCHAR,
    off_is_active BOOLEAN,
    bal_id UUID,
    total_stock INT,
    available_stock INT,
    reserved_stock INT,
    dispatched_stock INT,
    on_loan_stock INT,
    under_inspection_stock INT,
    under_cleaning_stock INT,
    damaged_stock INT,
    missing_stock INT,
    retired_stock INT
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        p.id, p.sku, p.name, p.description, p.category, p.image_url,
        p.material_type, p.safety_instructions, p.age_min_months, p.age_max_months,
        p.usage_restrictions, p.cleaning_instructions, p.return_policy,
        p.is_active, p.created_at, p.updated_at,
        o.id AS off_id, o.material_product_id AS off_prod_id, o.allow_purchase, o.purchase_price,
        o.allow_borrow, o.rental_fee, o.refundable_deposit, o.default_loan_duration_days,
        o.max_loan_duration_days, o.currency, o.is_active AS off_is_active,
        b.id AS bal_id, b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock, b.damaged_stock,
        b.missing_stock, b.retired_stock
    FROM material_products p
    LEFT JOIN material_offerings o ON p.id = o.material_product_id
    LEFT JOIN inventory_balances b ON p.id = b.material_product_id
    WHERE p.sku = p_sku;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_product(
    p_id UUID,
    p_sku VARCHAR,
    p_name VARCHAR,
    p_description TEXT,
    p_category VARCHAR,
    p_image_url VARCHAR,
    p_material_type VARCHAR,
    p_safety_instructions TEXT,
    p_age_min_months INT,
    p_age_max_months INT,
    p_usage_restrictions TEXT,
    p_cleaning_instructions TEXT,
    p_return_policy TEXT,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_products (
        id, sku, name, description, category, image_url, material_type,
        safety_instructions, age_min_months, age_max_months, usage_restrictions,
        cleaning_instructions, return_policy, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_sku, p_name, p_description, p_category, p_image_url, p_material_type,
        p_safety_instructions, p_age_min_months, p_age_max_months, p_usage_restrictions,
        p_cleaning_instructions, p_return_policy, p_is_active, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_offering(
    p_id UUID,
    p_material_product_id UUID,
    p_allow_purchase BOOLEAN,
    p_purchase_price NUMERIC,
    p_allow_borrow BOOLEAN,
    p_rental_fee NUMERIC,
    p_refundable_deposit NUMERIC,
    p_default_loan_duration_days INT,
    p_max_loan_duration_days INT,
    p_currency VARCHAR,
    p_is_active BOOLEAN,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_offerings (
        id, material_product_id, allow_purchase, purchase_price, allow_borrow,
        rental_fee, refundable_deposit, default_loan_duration_days, max_loan_duration_days,
        currency, is_active, created_at, updated_at
    ) VALUES (
        p_id, p_material_product_id, p_allow_purchase, p_purchase_price, p_allow_borrow,
        p_rental_fee, p_refundable_deposit, p_default_loan_duration_days, p_max_loan_duration_days,
        p_currency, p_is_active, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_product(
    p_id UUID,
    p_name VARCHAR,
    p_description TEXT,
    p_category VARCHAR,
    p_image_url VARCHAR,
    p_material_type VARCHAR,
    p_safety_instructions TEXT,
    p_age_min_months INT,
    p_age_max_months INT,
    p_usage_restrictions TEXT,
    p_cleaning_instructions TEXT,
    p_return_policy TEXT,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_products SET
        name = p_name,
        description = p_description,
        category = p_category,
        image_url = p_image_url,
        material_type = p_material_type,
        safety_instructions = p_safety_instructions,
        age_min_months = p_age_min_months,
        age_max_months = p_age_max_months,
        usage_restrictions = p_usage_restrictions,
        cleaning_instructions = p_cleaning_instructions,
        return_policy = p_return_policy,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_offering(
    p_material_product_id UUID,
    p_allow_purchase BOOLEAN,
    p_purchase_price NUMERIC,
    p_allow_borrow BOOLEAN,
    p_rental_fee NUMERIC,
    p_refundable_deposit NUMERIC,
    p_default_loan_duration_days INT,
    p_max_loan_duration_days INT,
    p_is_active BOOLEAN,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_offerings SET
        allow_purchase = p_allow_purchase,
        purchase_price = p_purchase_price,
        allow_borrow = p_allow_borrow,
        rental_fee = p_rental_fee,
        refundable_deposit = p_refundable_deposit,
        default_loan_duration_days = p_default_loan_duration_days,
        max_loan_duration_days = p_max_loan_duration_days,
        is_active = p_is_active,
        updated_at = p_updated_at
    WHERE material_product_id = p_material_product_id;
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_inventory_locations()
RETURNS TABLE (
    id UUID,
    code VARCHAR,
    name VARCHAR,
    address TEXT,
    contact_phone VARCHAR,
    is_central_warehouse BOOLEAN,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at
    FROM inventory_locations 
    WHERE is_active = TRUE 
    ORDER BY is_central_warehouse DESC, name ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_central_warehouse_location()
RETURNS TABLE (
    id UUID,
    code VARCHAR,
    name VARCHAR,
    address TEXT,
    contact_phone VARCHAR,
    is_central_warehouse BOOLEAN,
    is_active BOOLEAN,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT id, code, name, address, contact_phone, is_central_warehouse, is_active, created_at
    FROM inventory_locations 
    WHERE is_central_warehouse = TRUE AND is_active = TRUE 
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION fn_get_inventory_balances(
    p_location_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    material_product_id UUID,
    location_id UUID,
    total_stock INT,
    available_stock INT,
    reserved_stock INT,
    dispatched_stock INT,
    on_loan_stock INT,
    under_inspection_stock INT,
    under_cleaning_stock INT,
    damaged_stock INT,
    missing_stock INT,
    retired_stock INT,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    location_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        b.id, b.material_product_id, b.location_id,
        b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock,
        b.damaged_stock, b.missing_stock, b.retired_stock, b.updated_at,
        p.name AS product_name, p.sku AS product_sku, l.name AS location_name
    FROM inventory_balances b
    JOIN material_products p ON b.material_product_id = p.id
    JOIN inventory_locations l ON b.location_id = l.id
    WHERE (p_location_id IS NULL OR b.location_id = p_location_id)
    ORDER BY p.name ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_inventory_balance(
    p_product_id UUID,
    p_location_id UUID
)
RETURNS TABLE (
    id UUID,
    material_product_id UUID,
    location_id UUID,
    total_stock INT,
    available_stock INT,
    reserved_stock INT,
    dispatched_stock INT,
    on_loan_stock INT,
    under_inspection_stock INT,
    under_cleaning_stock INT,
    damaged_stock INT,
    missing_stock INT,
    retired_stock INT,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    location_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        b.id, b.material_product_id, b.location_id,
        b.total_stock, b.available_stock, b.reserved_stock, b.dispatched_stock,
        b.on_loan_stock, b.under_inspection_stock, b.under_cleaning_stock,
        b.damaged_stock, b.missing_stock, b.retired_stock, b.updated_at,
        p.name AS product_name, p.sku AS product_sku, l.name AS location_name
    FROM inventory_balances b
    JOIN material_products p ON b.material_product_id = p.id
    JOIN inventory_locations l ON b.location_id = l.id
    WHERE b.material_product_id = p_product_id AND b.location_id = p_location_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_product_ledger(
    p_product_id UUID,
    p_limit INT DEFAULT 50
)
RETURNS TABLE (
    id UUID,
    material_product_id UUID,
    location_id UUID,
    transaction_type VARCHAR,
    quantity_delta INT,
    available_delta INT,
    reserved_delta INT,
    on_loan_delta INT,
    reference_type VARCHAR,
    reference_id UUID,
    notes TEXT,
    performed_by_user_id UUID,
    performed_by_email VARCHAR,
    created_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        l.id, l.material_product_id, l.location_id, l.transaction_type,
        l.quantity_delta, l.available_delta, l.reserved_delta, l.on_loan_delta,
        l.reference_type, l.reference_id, l.notes,
        l.performed_by_user_id, l.performed_by_email, l.created_at,
        p.name AS product_name, p.sku AS product_sku
    FROM inventory_ledger l
    JOIN material_products p ON l.material_product_id = p.id
    WHERE l.material_product_id = p_product_id
    ORDER BY l.created_at DESC
    LIMIT p_limit;
$$;

CREATE OR REPLACE PROCEDURE sp_record_inventory_ledger(
    p_id UUID,
    p_material_product_id UUID,
    p_location_id UUID,
    p_transaction_type VARCHAR,
    p_quantity_delta INT,
    p_available_delta INT,
    p_reserved_delta INT,
    p_on_loan_delta INT,
    p_reference_type VARCHAR,
    p_reference_id UUID,
    p_notes TEXT,
    p_performed_by_user_id UUID,
    p_performed_by_email VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO inventory_ledger (
        id, material_product_id, location_id, transaction_type, quantity_delta,
        available_delta, reserved_delta, on_loan_delta, reference_type, reference_id,
        notes, performed_by_user_id, performed_by_email, created_at
    ) VALUES (
        p_id, p_material_product_id, p_location_id, p_transaction_type, p_quantity_delta,
        p_available_delta, p_reserved_delta, p_on_loan_delta, p_reference_type, p_reference_id,
        p_notes, p_performed_by_user_id, p_performed_by_email, p_created_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_inventory_balance_atomic(
    p_product_id UUID,
    p_location_id UUID,
    p_total_delta INT,
    p_available_delta INT,
    p_reserved_delta INT,
    p_dispatched_delta INT,
    p_on_loan_delta INT,
    p_under_inspection_delta INT,
    p_under_cleaning_delta INT,
    p_damaged_delta INT,
    p_missing_delta INT,
    p_retired_delta INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_rows INT;
BEGIN
    UPDATE inventory_balances SET
        total_stock = total_stock + p_total_delta,
        available_stock = GREATEST(0, available_stock + p_available_delta),
        reserved_stock = GREATEST(0, reserved_stock + p_reserved_delta),
        dispatched_stock = GREATEST(0, dispatched_stock + p_dispatched_delta),
        on_loan_stock = GREATEST(0, on_loan_stock + p_on_loan_delta),
        under_inspection_stock = GREATEST(0, under_inspection_stock + p_under_inspection_delta),
        under_cleaning_stock = GREATEST(0, under_cleaning_stock + p_under_cleaning_delta),
        damaged_stock = GREATEST(0, damaged_stock + p_damaged_delta),
        missing_stock = GREATEST(0, missing_stock + p_missing_delta),
        retired_stock = GREATEST(0, retired_stock + p_retired_delta),
        updated_at = NOW()
    WHERE material_product_id = p_product_id AND location_id = p_location_id;

    GET DIAGNOSTICS v_rows = ROW_COUNT;

    IF v_rows = 0 THEN
        INSERT INTO inventory_balances (
            id, material_product_id, location_id, total_stock, available_stock,
            reserved_stock, dispatched_stock, on_loan_stock, under_inspection_stock,
            under_cleaning_stock, damaged_stock, missing_stock, retired_stock, updated_at
        ) VALUES (
            gen_random_uuid(), p_product_id, p_location_id,
            GREATEST(0, p_total_delta), GREATEST(0, p_available_delta),
            GREATEST(0, p_reserved_delta), GREATEST(0, p_dispatched_delta),
            GREATEST(0, p_on_loan_delta), GREATEST(0, p_under_inspection_delta),
            GREATEST(0, p_under_cleaning_delta), GREATEST(0, p_damaged_delta),
            GREATEST(0, p_missing_delta), GREATEST(0, p_retired_delta), NOW()
        )
        ON CONFLICT (material_product_id, location_id) DO UPDATE SET
            total_stock = inventory_balances.total_stock + p_total_delta,
            available_stock = GREATEST(0, inventory_balances.available_stock + p_available_delta),
            reserved_stock = GREATEST(0, inventory_balances.reserved_stock + p_reserved_delta),
            dispatched_stock = GREATEST(0, inventory_balances.dispatched_stock + p_dispatched_delta),
            on_loan_stock = GREATEST(0, inventory_balances.on_loan_stock + p_on_loan_delta),
            under_inspection_stock = GREATEST(0, inventory_balances.under_inspection_stock + p_under_inspection_delta),
            under_cleaning_stock = GREATEST(0, inventory_balances.under_cleaning_stock + p_under_cleaning_delta),
            damaged_stock = GREATEST(0, inventory_balances.damaged_stock + p_damaged_delta),
            missing_stock = GREATEST(0, inventory_balances.missing_stock + p_missing_delta),
            retired_stock = inventory_balances.retired_stock + p_retired_delta,
            updated_at = NOW();
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_order(
    p_id UUID,
    p_order_number VARCHAR,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_planned_experience_id UUID,
    p_requesting_user_id UUID,
    p_requesting_user_email VARCHAR,
    p_order_type VARCHAR,
    p_workflow_status VARCHAR,
    p_payment_status VARCHAR,
    p_fulfilment_status VARCHAR,
    p_total_purchase_amount NUMERIC,
    p_total_rental_fee NUMERIC,
    p_total_deposit NUMERIC,
    p_shipping_fee NUMERIC,
    p_tax_amount NUMERIC,
    p_total_amount NUMERIC,
    p_currency VARCHAR,
    p_quotation_notes TEXT,
    p_quotation_valid_until TIMESTAMPTZ,
    p_delivery_address TEXT,
    p_delivery_contact_name VARCHAR,
    p_delivery_contact_phone VARCHAR,
    p_required_delivery_date TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_orders (
        id, order_number, tenant_id, organization_id, branch_id, classroom_id,
        planned_experience_id, requesting_user_id, requesting_user_email,
        order_type, workflow_status, payment_status, fulfilment_status,
        total_purchase_amount, total_rental_fee, total_deposit, shipping_fee,
        tax_amount, total_amount, currency, quotation_notes, quotation_valid_until,
        delivery_address, delivery_contact_name, delivery_contact_phone,
        required_delivery_date, created_at, updated_at
    ) VALUES (
        p_id, p_order_number, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        p_planned_experience_id, p_requesting_user_id, p_requesting_user_email,
        p_order_type, p_workflow_status, p_payment_status, p_fulfilment_status,
        p_total_purchase_amount, p_total_rental_fee, p_total_deposit, p_shipping_fee,
        p_tax_amount, p_total_amount, p_currency, p_quotation_notes, p_quotation_valid_until,
        p_delivery_address, p_delivery_contact_name, p_delivery_contact_phone,
        p_required_delivery_date, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_order_item(
    p_id UUID,
    p_order_id UUID,
    p_material_product_id UUID,
    p_offering_id UUID,
    p_fulfilment_mode VARCHAR,
    p_quantity INT,
    p_unit_price NUMERIC,
    p_rental_fee NUMERIC,
    p_deposit NUMERIC,
    p_line_total NUMERIC,
    p_loan_duration_days INT,
    p_return_due_date TIMESTAMPTZ,
    p_status VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_order_items (
        id, order_id, material_product_id, offering_id, fulfilment_mode,
        quantity, unit_price, rental_fee, deposit, line_total, loan_duration_days,
        return_due_date, status, created_at
    ) VALUES (
        p_id, p_order_id, p_material_product_id, p_offering_id, p_fulfilment_mode,
        p_quantity, p_unit_price, p_rental_fee, p_deposit, p_line_total, p_loan_duration_days,
        p_return_due_date, p_status, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_order_by_id(
    p_order_id UUID
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    planned_experience_id UUID,
    requesting_user_id UUID,
    requesting_user_email VARCHAR,
    order_type VARCHAR,
    workflow_status VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    total_purchase_amount NUMERIC,
    total_rental_fee NUMERIC,
    total_deposit NUMERIC,
    shipping_fee NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    currency VARCHAR,
    quotation_notes TEXT,
    quotation_valid_until TIMESTAMPTZ,
    rejection_reason TEXT,
    cancellation_reason TEXT,
    delivery_address TEXT,
    delivery_contact_name VARCHAR,
    delivery_contact_phone VARCHAR,
    required_delivery_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    organization_name VARCHAR,
    branch_name VARCHAR,
    classroom_name VARCHAR,
    planned_experience_title VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.id = p_order_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_order_by_number(
    p_order_number VARCHAR
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    planned_experience_id UUID,
    requesting_user_id UUID,
    requesting_user_email VARCHAR,
    order_type VARCHAR,
    workflow_status VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    total_purchase_amount NUMERIC,
    total_rental_fee NUMERIC,
    total_deposit NUMERIC,
    shipping_fee NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    currency VARCHAR,
    quotation_notes TEXT,
    quotation_valid_until TIMESTAMPTZ,
    rejection_reason TEXT,
    cancellation_reason TEXT,
    delivery_address TEXT,
    delivery_contact_name VARCHAR,
    delivery_contact_phone VARCHAR,
    required_delivery_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    organization_name VARCHAR,
    branch_name VARCHAR,
    classroom_name VARCHAR,
    planned_experience_title VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.order_number = p_order_number;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_orders_for_organization(
    p_organization_id UUID,
    p_status_filter VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    planned_experience_id UUID,
    requesting_user_id UUID,
    requesting_user_email VARCHAR,
    order_type VARCHAR,
    workflow_status VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    total_purchase_amount NUMERIC,
    total_rental_fee NUMERIC,
    total_deposit NUMERIC,
    shipping_fee NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    currency VARCHAR,
    quotation_notes TEXT,
    quotation_valid_until TIMESTAMPTZ,
    rejection_reason TEXT,
    cancellation_reason TEXT,
    delivery_address TEXT,
    delivery_contact_name VARCHAR,
    delivery_contact_phone VARCHAR,
    required_delivery_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    organization_name VARCHAR,
    branch_name VARCHAR,
    classroom_name VARCHAR,
    planned_experience_title VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE o.organization_id = p_organization_id
      AND (p_status_filter IS NULL OR p_status_filter = 'All' OR o.workflow_status = p_status_filter)
    ORDER BY o.created_at DESC;
$$;

CREATE OR REPLACE FUNCTION fn_get_platform_order_queue(
    p_status_filter VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    planned_experience_id UUID,
    requesting_user_id UUID,
    requesting_user_email VARCHAR,
    order_type VARCHAR,
    workflow_status VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    total_purchase_amount NUMERIC,
    total_rental_fee NUMERIC,
    total_deposit NUMERIC,
    shipping_fee NUMERIC,
    tax_amount NUMERIC,
    total_amount NUMERIC,
    currency VARCHAR,
    quotation_notes TEXT,
    quotation_valid_until TIMESTAMPTZ,
    rejection_reason TEXT,
    cancellation_reason TEXT,
    delivery_address TEXT,
    delivery_contact_name VARCHAR,
    delivery_contact_phone VARCHAR,
    required_delivery_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    organization_name VARCHAR,
    branch_name VARCHAR,
    classroom_name VARCHAR,
    planned_experience_title VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.planned_experience_id, o.requesting_user_id, o.requesting_user_email,
        o.order_type, o.workflow_status, o.payment_status, o.fulfilment_status,
        o.total_purchase_amount, o.total_rental_fee, o.total_deposit, o.shipping_fee,
        o.tax_amount, o.total_amount, o.currency, o.quotation_notes, o.quotation_valid_until,
        o.rejection_reason, o.cancellation_reason, o.delivery_address, o.delivery_contact_name,
        o.delivery_contact_phone, o.required_delivery_date, o.created_at, o.updated_at,
        org.name AS organization_name, b.name AS branch_name, c.name AS classroom_name,
        e.title AS planned_experience_title
    FROM material_orders o
    JOIN organizations org ON o.organization_id = org.id
    LEFT JOIN branches b ON o.branch_id = b.id
    LEFT JOIN classrooms c ON o.classroom_id = c.id
    LEFT JOIN experiences e ON o.planned_experience_id = e.id
    WHERE (p_status_filter IS NULL OR p_status_filter = 'All' OR o.workflow_status = p_status_filter)
    ORDER BY o.created_at DESC;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_order_status(
    p_order_id UUID,
    p_new_status VARCHAR,
    p_payment_status VARCHAR DEFAULT NULL,
    p_fulfilment_status VARCHAR DEFAULT NULL,
    p_rejection_reason TEXT DEFAULT NULL,
    p_cancellation_reason TEXT DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_orders SET
        workflow_status = p_new_status,
        payment_status = COALESCE(p_payment_status, payment_status),
        fulfilment_status = COALESCE(p_fulfilment_status, fulfilment_status),
        rejection_reason = COALESCE(p_rejection_reason, rejection_reason),
        cancellation_reason = COALESCE(p_cancellation_reason, cancellation_reason),
        updated_at = NOW()
    WHERE id = p_order_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_order_quotation(
    p_order_id UUID,
    p_total_purchase_amount NUMERIC,
    p_total_rental_fee NUMERIC,
    p_total_deposit NUMERIC,
    p_shipping_fee NUMERIC,
    p_total_amount NUMERIC,
    p_quotation_notes TEXT,
    p_valid_until TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_orders SET
        total_purchase_amount = p_total_purchase_amount,
        total_rental_fee = p_total_rental_fee,
        total_deposit = p_total_deposit,
        shipping_fee = p_shipping_fee,
        total_amount = p_total_amount,
        quotation_notes = p_quotation_notes,
        quotation_valid_until = p_valid_until,
        workflow_status = 'QuotationOffered',
        updated_at = NOW()
    WHERE id = p_order_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_add_material_order_history(
    p_id UUID,
    p_order_id UUID,
    p_event_type VARCHAR,
    p_previous_status VARCHAR,
    p_new_status VARCHAR,
    p_actor_user_id UUID,
    p_actor_email VARCHAR,
    p_actor_role VARCHAR,
    p_description TEXT,
    p_notes TEXT,
    p_is_internal_note BOOLEAN,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_order_history (
        id, order_id, event_type, previous_status, new_status, actor_user_id,
        actor_email, actor_role, description, notes, is_internal_note, created_at
    ) VALUES (
        p_id, p_order_id, p_event_type, p_previous_status, p_new_status, p_actor_user_id,
        p_actor_email, p_actor_role, p_description, p_notes, p_is_internal_note, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_order_history(
    p_order_id UUID,
    p_include_internal_notes BOOLEAN DEFAULT FALSE
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    event_type VARCHAR,
    previous_status VARCHAR,
    new_status VARCHAR,
    actor_user_id UUID,
    actor_email VARCHAR,
    actor_role VARCHAR,
    description TEXT,
    notes TEXT,
    is_internal_note BOOLEAN,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, order_id, event_type, previous_status, new_status,
        actor_user_id, actor_email, actor_role, description, notes,
        is_internal_note, created_at
    FROM material_order_history
    WHERE order_id = p_order_id
      AND (p_include_internal_notes OR is_internal_note = FALSE)
    ORDER BY created_at ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_order_items(
    p_order_id UUID
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    material_product_id UUID,
    offering_id UUID,
    fulfilment_mode VARCHAR,
    quantity INT,
    unit_price NUMERIC,
    rental_fee NUMERIC,
    deposit NUMERIC,
    line_total NUMERIC,
    loan_duration_days INT,
    return_due_date TIMESTAMPTZ,
    status VARCHAR,
    created_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    image_url VARCHAR,
    category VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        i.id, i.order_id, i.material_product_id, i.offering_id, i.fulfilment_mode,
        i.quantity, i.unit_price, i.rental_fee, i.deposit, i.line_total,
        i.loan_duration_days, i.return_due_date, i.status, i.created_at,
        p.name AS product_name, p.sku AS product_sku, p.image_url, p.category
    FROM material_order_items i
    JOIN material_products p ON i.material_product_id = p.id
    WHERE i.order_id = p_order_id;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_shipment(
    p_id UUID,
    p_order_id UUID,
    p_shipment_number VARCHAR,
    p_carrier_name VARCHAR,
    p_tracking_reference VARCHAR,
    p_dispatch_date TIMESTAMPTZ,
    p_estimated_delivery_date TIMESTAMPTZ,
    p_actual_delivery_date TIMESTAMPTZ,
    p_delivery_status VARCHAR,
    p_proof_of_delivery_notes TEXT,
    p_recorded_by_user_id UUID,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_shipments (
        id, order_id, shipment_number, carrier_name, tracking_reference,
        dispatch_date, estimated_delivery_date, actual_delivery_date,
        delivery_status, proof_of_delivery_notes, recorded_by_user_id,
        created_at, updated_at
    ) VALUES (
        p_id, p_order_id, p_shipment_number, p_carrier_name, p_tracking_reference,
        p_dispatch_date, p_estimated_delivery_date, p_actual_delivery_date,
        p_delivery_status, p_proof_of_delivery_notes, p_recorded_by_user_id,
        p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_shipment_by_order_id(
    p_order_id UUID
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    shipment_number VARCHAR,
    carrier_name VARCHAR,
    tracking_reference VARCHAR,
    dispatch_date TIMESTAMPTZ,
    estimated_delivery_date TIMESTAMPTZ,
    actual_delivery_date TIMESTAMPTZ,
    delivery_status VARCHAR,
    proof_of_delivery_notes TEXT,
    recorded_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        id, order_id, shipment_number, carrier_name, tracking_reference,
        dispatch_date, estimated_delivery_date, actual_delivery_date,
        delivery_status, proof_of_delivery_notes, recorded_by_user_id,
        created_at, updated_at
    FROM material_shipments 
    WHERE order_id = p_order_id 
    ORDER BY created_at DESC 
    LIMIT 1;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_shipment_status(
    p_shipment_id UUID,
    p_delivery_status VARCHAR,
    p_actual_delivery_date TIMESTAMPTZ,
    p_proof_notes TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_shipments SET
        delivery_status = p_delivery_status,
        actual_delivery_date = COALESCE(p_actual_delivery_date, actual_delivery_date),
        proof_of_delivery_notes = COALESCE(p_proof_notes, proof_of_delivery_notes),
        updated_at = NOW()
    WHERE id = p_shipment_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_loan(
    p_id UUID,
    p_order_id UUID,
    p_order_item_id UUID,
    p_material_product_id UUID,
    p_organization_id UUID,
    p_classroom_id UUID,
    p_quantity_issued INT,
    p_issued_at TIMESTAMPTZ,
    p_due_date TIMESTAMPTZ,
    p_actual_return_date TIMESTAMPTZ,
    p_return_status VARCHAR,
    p_deposit_amount NUMERIC,
    p_deposit_status VARCHAR,
    p_notes TEXT,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_loans (
        id, order_id, order_item_id, material_product_id, organization_id,
        classroom_id, quantity_issued, issued_at, due_date, actual_return_date,
        return_status, deposit_amount, deposit_status, notes, created_at, updated_at
    ) VALUES (
        p_id, p_order_id, p_order_item_id, p_material_product_id, p_organization_id,
        p_classroom_id, p_quantity_issued, p_issued_at, p_due_date, p_actual_return_date,
        p_return_status, p_deposit_amount, p_deposit_status, p_notes, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_loans_by_order_id(
    p_order_id UUID
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    order_item_id UUID,
    material_product_id UUID,
    organization_id UUID,
    classroom_id UUID,
    quantity_issued INT,
    issued_at TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    actual_return_date TIMESTAMPTZ,
    return_status VARCHAR,
    deposit_amount NUMERIC,
    deposit_status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    order_number VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.order_id = p_order_id;
$$;

CREATE OR REPLACE FUNCTION fn_get_active_material_loans_for_organization(
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    order_item_id UUID,
    material_product_id UUID,
    organization_id UUID,
    classroom_id UUID,
    quantity_issued INT,
    issued_at TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    actual_return_date TIMESTAMPTZ,
    return_status VARCHAR,
    deposit_amount NUMERIC,
    deposit_status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    order_number VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.organization_id = p_organization_id
    ORDER BY l.due_date ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_platform_loan_queue(
    p_status_filter VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    order_item_id UUID,
    material_product_id UUID,
    organization_id UUID,
    classroom_id UUID,
    quantity_issued INT,
    issued_at TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    actual_return_date TIMESTAMPTZ,
    return_status VARCHAR,
    deposit_amount NUMERIC,
    deposit_status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    order_number VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE (p_status_filter IS NULL OR p_status_filter = 'All' OR l.return_status = p_status_filter)
    ORDER BY l.due_date ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_loan_by_id(
    p_loan_id UUID
)
RETURNS TABLE (
    id UUID,
    order_id UUID,
    order_item_id UUID,
    material_product_id UUID,
    organization_id UUID,
    classroom_id UUID,
    quantity_issued INT,
    issued_at TIMESTAMPTZ,
    due_date TIMESTAMPTZ,
    actual_return_date TIMESTAMPTZ,
    return_status VARCHAR,
    deposit_amount NUMERIC,
    deposit_status VARCHAR,
    notes TEXT,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR,
    order_number VARCHAR,
    classroom_name VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        l.id, l.order_id, l.order_item_id, l.material_product_id, l.organization_id,
        l.classroom_id, l.quantity_issued, l.issued_at, l.due_date, l.actual_return_date,
        l.return_status, l.deposit_amount, l.deposit_status, l.notes, l.created_at, l.updated_at,
        p.name AS product_name, p.sku AS product_sku, o.order_number, c.name AS classroom_name
    FROM material_loans l
    JOIN material_products p ON l.material_product_id = p.id
    JOIN material_orders o ON l.order_id = o.id
    LEFT JOIN classrooms c ON l.classroom_id = c.id
    WHERE l.id = p_loan_id;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_loan_status(
    p_loan_id UUID,
    p_return_status VARCHAR,
    p_actual_return_date TIMESTAMPTZ DEFAULT NULL,
    p_notes TEXT DEFAULT NULL,
    p_deposit_status VARCHAR DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_loans SET
        return_status = p_return_status,
        actual_return_date = COALESCE(p_actual_return_date, actual_return_date),
        notes = COALESCE(p_notes, notes),
        deposit_status = COALESCE(p_deposit_status, deposit_status),
        updated_at = NOW()
    WHERE id = p_loan_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_record_material_return_inspection(
    p_id UUID,
    p_loan_id UUID,
    p_order_id UUID,
    p_material_product_id UUID,
    p_returned_quantity INT,
    p_inspected_at TIMESTAMPTZ,
    p_inspected_by_user_id UUID,
    p_inspected_by_email VARCHAR,
    p_condition VARCHAR,
    p_quantity_approved_for_stock INT,
    p_quantity_requiring_cleaning INT,
    p_quantity_damaged INT,
    p_quantity_missing INT,
    p_inspection_notes TEXT,
    p_deposit_disposition VARCHAR,
    p_deposit_deduction_amount NUMERIC,
    p_resolution_status VARCHAR,
    p_created_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_return_inspections (
        id, loan_id, order_id, material_product_id, returned_quantity,
        inspected_at, inspected_by_user_id, inspected_by_email, condition,
        quantity_approved_for_stock, quantity_requiring_cleaning,
        quantity_damaged, quantity_missing, inspection_notes,
        deposit_disposition, deposit_deduction_amount, resolution_status, created_at
    ) VALUES (
        p_id, p_loan_id, p_order_id, p_material_product_id, p_returned_quantity,
        p_inspected_at, p_inspected_by_user_id, p_inspected_by_email, p_condition,
        p_quantity_approved_for_stock, p_quantity_requiring_cleaning,
        p_quantity_damaged, p_quantity_missing, p_inspection_notes,
        p_deposit_disposition, p_deposit_deduction_amount, p_resolution_status, p_created_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_return_inspection_by_loan_id(
    p_loan_id UUID
)
RETURNS TABLE (
    id UUID,
    loan_id UUID,
    order_id UUID,
    material_product_id UUID,
    returned_quantity INT,
    inspected_at TIMESTAMPTZ,
    inspected_by_user_id UUID,
    inspected_by_email VARCHAR,
    condition VARCHAR,
    quantity_approved_for_stock INT,
    quantity_requiring_cleaning INT,
    quantity_damaged INT,
    quantity_missing INT,
    inspection_notes TEXT,
    deposit_disposition VARCHAR,
    deposit_deduction_amount NUMERIC,
    resolution_status VARCHAR,
    created_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        i.id, i.loan_id, i.order_id, i.material_product_id, i.returned_quantity,
        i.inspected_at, i.inspected_by_user_id, i.inspected_by_email, i.condition,
        i.quantity_approved_for_stock, i.quantity_requiring_cleaning,
        i.quantity_damaged, i.quantity_missing, i.inspection_notes,
        i.deposit_disposition, i.deposit_deduction_amount, i.resolution_status, i.created_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_return_inspections i
    JOIN material_products p ON i.material_product_id = p.id
    WHERE i.loan_id = p_loan_id
    ORDER BY i.created_at DESC 
    LIMIT 1;
$$;

CREATE OR REPLACE PROCEDURE sp_create_material_recurring_schedule(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_schedule_type VARCHAR,
    p_frequency VARCHAR,
    p_material_product_id UUID,
    p_offering_id UUID,
    p_quantity INT,
    p_loan_duration_days INT,
    p_delivery_address TEXT,
    p_next_run_date TIMESTAMPTZ,
    p_last_run_date TIMESTAMPTZ,
    p_status VARCHAR,
    p_notes TEXT,
    p_created_by_user_id UUID,
    p_created_at TIMESTAMPTZ,
    p_updated_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO material_recurring_schedules (
        id, tenant_id, organization_id, branch_id, classroom_id, schedule_type,
        frequency, material_product_id, offering_id, quantity, loan_duration_days,
        delivery_address, next_run_date, last_run_date, status, notes,
        created_by_user_id, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id, p_schedule_type,
        p_frequency, p_material_product_id, p_offering_id, p_quantity, p_loan_duration_days,
        p_delivery_address, p_next_run_date, p_last_run_date, p_status, p_notes,
        p_created_by_user_id, p_created_at, p_updated_at
    );
END;
$$;

CREATE OR REPLACE FUNCTION fn_get_material_recurring_schedules(
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    schedule_type VARCHAR,
    frequency VARCHAR,
    material_product_id UUID,
    offering_id UUID,
    quantity INT,
    loan_duration_days INT,
    delivery_address TEXT,
    next_run_date TIMESTAMPTZ,
    last_run_date TIMESTAMPTZ,
    status VARCHAR,
    notes TEXT,
    created_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.schedule_type,
        s.frequency, s.material_product_id, s.offering_id, s.quantity, s.loan_duration_days,
        s.delivery_address, s.next_run_date, s.last_run_date, s.status, s.notes,
        s.created_by_user_id, s.created_at, s.updated_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_recurring_schedules s
    JOIN material_products p ON s.material_product_id = p.id
    WHERE s.organization_id = p_organization_id
    ORDER BY s.next_run_date ASC;
$$;

CREATE OR REPLACE FUNCTION fn_get_due_material_recurring_schedules(
    p_as_of_date TIMESTAMPTZ
)
RETURNS TABLE (
    id UUID,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    schedule_type VARCHAR,
    frequency VARCHAR,
    material_product_id UUID,
    offering_id UUID,
    quantity INT,
    loan_duration_days INT,
    delivery_address TEXT,
    next_run_date TIMESTAMPTZ,
    last_run_date TIMESTAMPTZ,
    status VARCHAR,
    notes TEXT,
    created_by_user_id UUID,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ,
    product_name VARCHAR,
    product_sku VARCHAR
)
LANGUAGE sql
STABLE
AS $$
    SELECT 
        s.id, s.tenant_id, s.organization_id, s.branch_id, s.classroom_id, s.schedule_type,
        s.frequency, s.material_product_id, s.offering_id, s.quantity, s.loan_duration_days,
        s.delivery_address, s.next_run_date, s.last_run_date, s.status, s.notes,
        s.created_by_user_id, s.created_at, s.updated_at,
        p.name AS product_name, p.sku AS product_sku
    FROM material_recurring_schedules s
    JOIN material_products p ON s.material_product_id = p.id
    WHERE s.status = 'Active' AND s.next_run_date <= p_as_of_date
    ORDER BY s.next_run_date ASC;
$$;

CREATE OR REPLACE PROCEDURE sp_update_material_recurring_schedule_status(
    p_schedule_id UUID,
    p_status VARCHAR,
    p_next_run_date TIMESTAMPTZ DEFAULT NULL,
    p_last_run_date TIMESTAMPTZ DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE material_recurring_schedules SET
        status = p_status,
        next_run_date = COALESCE(p_next_run_date, next_run_date),
        last_run_date = COALESCE(p_last_run_date, last_run_date),
        updated_at = NOW()
    WHERE id = p_schedule_id;
END;
$$;
