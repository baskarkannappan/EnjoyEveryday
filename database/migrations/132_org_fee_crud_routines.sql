-- Migration 132: Stored functions & procedures for Org Fee Plans, Tariff Rules,
-- Family Contracts, Adjustments, Bank Accounts, QR Codes, Invoices, and Payments.

CREATE OR REPLACE FUNCTION public.fn_get_org_fee_plans(
    p_tenant_id UUID,
    p_organization_id UUID
)
RETURNS TABLE (
    id UUID, tenant_id UUID, organization_id UUID, name VARCHAR, description TEXT,
    fee_category VARCHAR, billing_frequency VARCHAR, currency VARCHAR, is_active BOOLEAN,
    allow_partial_payment BOOLEAN, minimum_partial_payment NUMERIC,
    allow_parent_cash_declaration BOOLEAN, created_by VARCHAR,
    created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT id, tenant_id, organization_id, name, description,
           fee_category, billing_frequency, currency, is_active,
           allow_partial_payment, minimum_partial_payment,
           allow_parent_cash_declaration, created_by, created_at, updated_at
    FROM org_fee_plans
    WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id
    ORDER BY is_active DESC, name ASC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_org_fee_plan(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_name VARCHAR,
    p_description TEXT,
    p_fee_category VARCHAR,
    p_billing_frequency VARCHAR,
    p_currency VARCHAR,
    p_is_active BOOLEAN,
    p_allow_partial_payment BOOLEAN,
    p_minimum_partial_payment NUMERIC,
    p_allow_parent_cash_declaration BOOLEAN,
    p_created_by VARCHAR
)
RETURNS SETOF org_fee_plans
LANGUAGE sql AS $function$
    INSERT INTO org_fee_plans (
        id, tenant_id, organization_id, name, description,
        fee_category, billing_frequency, currency, is_active,
        allow_partial_payment, minimum_partial_payment,
        allow_parent_cash_declaration, created_by, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_name, p_description,
        p_fee_category, p_billing_frequency, p_currency, p_is_active,
        p_allow_partial_payment, p_minimum_partial_payment,
        p_allow_parent_cash_declaration, p_created_by, NOW(), NOW()
    )
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        description = EXCLUDED.description,
        fee_category = EXCLUDED.fee_category,
        billing_frequency = EXCLUDED.billing_frequency,
        currency = EXCLUDED.currency,
        is_active = EXCLUDED.is_active,
        allow_partial_payment = EXCLUDED.allow_partial_payment,
        minimum_partial_payment = EXCLUDED.minimum_partial_payment,
        allow_parent_cash_declaration = EXCLUDED.allow_parent_cash_declaration,
        updated_at = NOW()
    WHERE org_fee_plans.tenant_id = p_tenant_id
      AND org_fee_plans.organization_id = p_organization_id
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_set_org_fee_plan_active(
    p_tenant_id UUID, p_organization_id UUID, p_plan_id UUID, p_is_active BOOLEAN
)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $function$
BEGIN
    UPDATE org_fee_plans
    SET is_active = p_is_active, updated_at = NOW()
    WHERE id = p_plan_id AND tenant_id = p_tenant_id AND organization_id = p_organization_id;
    RETURN FOUND;
END;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_org_tariff_rules(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_fee_plan_id UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID, tenant_id UUID, organization_id UUID, fee_plan_id UUID,
    plan_name VARCHAR, billing_frequency VARCHAR, fee_category VARCHAR,
    scope_type VARCHAR, branch_id UUID, branch_name VARCHAR,
    classroom_id UUID, classroom_name VARCHAR,
    child_id UUID, child_name TEXT,
    location_tier VARCHAR, child_category VARCHAR,
    amount NUMERIC, currency VARCHAR, effective_date DATE, expiry_date DATE,
    is_active BOOLEAN, notes TEXT, created_by VARCHAR,
    created_at TIMESTAMPTZ, updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT
        r.id, r.tenant_id, r.organization_id, r.fee_plan_id,
        p.name AS plan_name, p.billing_frequency, p.fee_category,
        r.scope_type, r.branch_id, b.name AS branch_name,
        r.classroom_id, cl.name AS classroom_name,
        r.child_id, TRIM(CONCAT(ch.first_name, ' ', ch.last_name)) AS child_name,
        r.location_tier, r.child_category,
        r.amount, r.currency, r.effective_date, r.expiry_date,
        r.is_active, r.notes, r.created_by, r.created_at, r.updated_at
    FROM org_tariff_rules r
    INNER JOIN org_fee_plans p ON p.id = r.fee_plan_id
    LEFT JOIN branches b ON b.id = r.branch_id
    LEFT JOIN classrooms cl ON cl.id = r.classroom_id
    LEFT JOIN children ch ON ch.id = r.child_id
    WHERE r.tenant_id = p_tenant_id
      AND r.organization_id = p_organization_id
      AND (p_fee_plan_id IS NULL OR r.fee_plan_id = p_fee_plan_id)
    ORDER BY
        CASE r.scope_type
            WHEN 'Child' THEN 1
            WHEN 'Classroom' THEN 2
            WHEN 'Branch' THEN 3
            WHEN 'LocationTier' THEN 4
            ELSE 5
        END,
        r.effective_date DESC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_org_tariff_rule(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_fee_plan_id UUID,
    p_scope_type VARCHAR,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_child_id UUID,
    p_location_tier VARCHAR,
    p_child_category VARCHAR,
    p_amount NUMERIC,
    p_currency VARCHAR,
    p_effective_date DATE,
    p_expiry_date DATE,
    p_is_active BOOLEAN,
    p_notes TEXT,
    p_created_by VARCHAR
)
RETURNS SETOF org_tariff_rules
LANGUAGE sql AS $function$
    INSERT INTO org_tariff_rules (
        id, tenant_id, organization_id, fee_plan_id, scope_type,
        branch_id, classroom_id, child_id, location_tier, child_category,
        amount, currency, effective_date, expiry_date, is_active, notes,
        created_by, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_fee_plan_id, p_scope_type,
        p_branch_id, p_classroom_id, p_child_id, p_location_tier, p_child_category,
        p_amount, p_currency, p_effective_date, p_expiry_date, p_is_active, p_notes,
        p_created_by, NOW(), NOW()
    )
    ON CONFLICT (id) DO UPDATE SET
        fee_plan_id = EXCLUDED.fee_plan_id,
        scope_type = EXCLUDED.scope_type,
        branch_id = EXCLUDED.branch_id,
        classroom_id = EXCLUDED.classroom_id,
        child_id = EXCLUDED.child_id,
        location_tier = EXCLUDED.location_tier,
        child_category = EXCLUDED.child_category,
        amount = EXCLUDED.amount,
        currency = EXCLUDED.currency,
        effective_date = EXCLUDED.effective_date,
        expiry_date = EXCLUDED.expiry_date,
        is_active = EXCLUDED.is_active,
        notes = EXCLUDED.notes,
        updated_at = NOW()
    WHERE org_tariff_rules.tenant_id = p_tenant_id
      AND org_tariff_rules.organization_id = p_organization_id
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_set_org_tariff_rule_active(
    p_tenant_id UUID, p_organization_id UUID, p_rule_id UUID, p_is_active BOOLEAN
)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $function$
BEGIN
    UPDATE org_tariff_rules
    SET is_active = p_is_active, updated_at = NOW()
    WHERE id = p_rule_id AND tenant_id = p_tenant_id AND organization_id = p_organization_id;
    RETURN FOUND;
END;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_family_billing_contract_v2(
    p_id UUID,
    p_tenant_id UUID,
    p_organization_id UUID,
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
    p_status VARCHAR,
    p_start_date DATE,
    p_end_date DATE,
    p_auto_invoice BOOLEAN,
    p_fee_method VARCHAR,
    p_fee_plan_id UUID,
    p_tariff_rule_id UUID,
    p_fee_period VARCHAR,
    p_payment_arrangement VARCHAR,
    p_currency VARCHAR,
    p_remarks TEXT,
    p_allow_parent_cash_declaration BOOLEAN,
    p_minimum_partial_payment NUMERIC
)
RETURNS SETOF family_billing_contracts
LANGUAGE sql AS $function$
    INSERT INTO family_billing_contracts (
        id, tenant_id, organization_id, branch_id, application_id, child_id,
        parent_name, parent_email, schedule_type, billing_cycle,
        base_rate, discount_percent, effective_rate, deposit_held, status,
        start_date, end_date, auto_invoice, fee_method, fee_plan_id, tariff_rule_id,
        fee_period, payment_arrangement, currency, remarks,
        allow_parent_cash_declaration, minimum_partial_payment, created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_application_id, p_child_id,
        p_parent_name, p_parent_email, p_schedule_type, p_billing_cycle,
        p_base_rate, p_discount_percent, p_effective_rate, p_deposit_held, p_status,
        p_start_date, p_end_date, p_auto_invoice, p_fee_method, p_fee_plan_id, p_tariff_rule_id,
        p_fee_period, p_payment_arrangement, p_currency, p_remarks,
        p_allow_parent_cash_declaration, p_minimum_partial_payment, NOW(), NOW()
    )
    ON CONFLICT (id) DO UPDATE SET
        branch_id = EXCLUDED.branch_id,
        schedule_type = EXCLUDED.schedule_type,
        billing_cycle = EXCLUDED.billing_cycle,
        base_rate = EXCLUDED.base_rate,
        discount_percent = EXCLUDED.discount_percent,
        effective_rate = EXCLUDED.effective_rate,
        deposit_held = EXCLUDED.deposit_held,
        status = EXCLUDED.status,
        start_date = EXCLUDED.start_date,
        end_date = EXCLUDED.end_date,
        auto_invoice = EXCLUDED.auto_invoice,
        fee_method = EXCLUDED.fee_method,
        fee_plan_id = EXCLUDED.fee_plan_id,
        tariff_rule_id = EXCLUDED.tariff_rule_id,
        fee_period = EXCLUDED.fee_period,
        payment_arrangement = EXCLUDED.payment_arrangement,
        currency = EXCLUDED.currency,
        remarks = EXCLUDED.remarks,
        allow_parent_cash_declaration = EXCLUDED.allow_parent_cash_declaration,
        minimum_partial_payment = EXCLUDED.minimum_partial_payment,
        updated_at = NOW()
    WHERE family_billing_contracts.tenant_id = p_tenant_id
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_family_contract_by_id(
    p_tenant_id UUID, p_contract_id UUID
)
RETURNS SETOF family_billing_contracts
LANGUAGE sql STABLE AS $function$
    SELECT * FROM family_billing_contracts WHERE tenant_id = p_tenant_id AND id = p_contract_id;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_family_contracts_for_child(
    p_tenant_id UUID, p_child_id UUID
)
RETURNS SETOF family_billing_contracts
LANGUAGE sql STABLE AS $function$
    SELECT * FROM family_billing_contracts WHERE tenant_id = p_tenant_id AND child_id = p_child_id ORDER BY created_at DESC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_insert_child_fee_adjustment(
    p_id UUID, p_tenant_id UUID, p_organization_id UUID, p_branch_id UUID,
    p_child_id UUID, p_contract_id UUID, p_invoice_id UUID,
    p_adjustment_type VARCHAR, p_previous_amount NUMERIC, p_new_amount NUMERIC,
    p_adjustment_delta NUMERIC, p_reason TEXT, p_effective_date DATE,
    p_adjusted_by_user_id UUID, p_adjusted_by_email VARCHAR
)
RETURNS SETOF child_fee_adjustments
LANGUAGE sql AS $function$
    INSERT INTO child_fee_adjustments (
        id, tenant_id, organization_id, branch_id, child_id, contract_id, invoice_id,
        adjustment_type, previous_amount, new_amount, adjustment_delta, reason,
        effective_date, adjusted_by_user_id, adjusted_by_email, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_contract_id, p_invoice_id,
        p_adjustment_type, p_previous_amount, p_new_amount, p_adjustment_delta, p_reason,
        p_effective_date, p_adjusted_by_user_id, p_adjusted_by_email, NOW()
    )
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_child_fee_adjustments(
    p_tenant_id UUID, p_organization_id UUID, p_child_id UUID
)
RETURNS SETOF child_fee_adjustments
LANGUAGE sql STABLE AS $function$
    SELECT * FROM child_fee_adjustments
    WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND child_id = p_child_id
    ORDER BY created_at DESC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_org_bank_account(
    p_tenant_id UUID, p_organization_id UUID
)
RETURNS SETOF org_bank_accounts
LANGUAGE sql STABLE AS $function$
    SELECT * FROM org_bank_accounts WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_org_bank_account(
    p_id UUID, p_tenant_id UUID, p_organization_id UUID,
    p_account_holder_name VARCHAR, p_bank_name VARCHAR, p_branch_name VARCHAR,
    p_account_number VARCHAR, p_ifsc_code VARCHAR, p_micr_code VARCHAR,
    p_account_type VARCHAR, p_additional_instructions TEXT,
    p_allow_parent_cash_declaration BOOLEAN, p_require_payment_proof BOOLEAN,
    p_updated_by VARCHAR
)
RETURNS SETOF org_bank_accounts
LANGUAGE sql AS $function$
    INSERT INTO org_bank_accounts (
        id, tenant_id, organization_id, account_holder_name, bank_name, branch_name,
        account_number, ifsc_code, micr_code, account_type, additional_instructions,
        allow_parent_cash_declaration, require_payment_proof, updated_by, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_account_holder_name, p_bank_name, p_branch_name,
        p_account_number, p_ifsc_code, p_micr_code, p_account_type, p_additional_instructions,
        p_allow_parent_cash_declaration, p_require_payment_proof, p_updated_by, NOW()
    )
    ON CONFLICT (organization_id) DO UPDATE SET
        account_holder_name = EXCLUDED.account_holder_name,
        bank_name = EXCLUDED.bank_name,
        branch_name = EXCLUDED.branch_name,
        account_number = EXCLUDED.account_number,
        ifsc_code = EXCLUDED.ifsc_code,
        micr_code = EXCLUDED.micr_code,
        account_type = EXCLUDED.account_type,
        additional_instructions = EXCLUDED.additional_instructions,
        allow_parent_cash_declaration = EXCLUDED.allow_parent_cash_declaration,
        require_payment_proof = EXCLUDED.require_payment_proof,
        updated_by = EXCLUDED.updated_by,
        updated_at = NOW()
    WHERE org_bank_accounts.tenant_id = p_tenant_id
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_get_org_qr_codes(
    p_tenant_id UUID, p_organization_id UUID
)
RETURNS SETOF org_payment_qr_codes
LANGUAGE sql STABLE AS $function$
    SELECT * FROM org_payment_qr_codes
    WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id
    ORDER BY slot_index ASC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_upsert_org_qr_code(
    p_id UUID, p_tenant_id UUID, p_organization_id UUID, p_slot_index INT,
    p_display_name VARCHAR, p_payment_purpose VARCHAR, p_qr_image_data_uri TEXT,
    p_instructions TEXT, p_is_active BOOLEAN, p_updated_by VARCHAR
)
RETURNS SETOF org_payment_qr_codes
LANGUAGE sql AS $function$
    INSERT INTO org_payment_qr_codes (
        id, tenant_id, organization_id, slot_index, display_name, payment_purpose,
        qr_image_data_uri, instructions, is_active, updated_by, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_slot_index, p_display_name, p_payment_purpose,
        p_qr_image_data_uri, p_instructions, p_is_active, p_updated_by, NOW()
    )
    ON CONFLICT (organization_id, slot_index) DO UPDATE SET
        display_name = EXCLUDED.display_name,
        payment_purpose = EXCLUDED.payment_purpose,
        qr_image_data_uri = EXCLUDED.qr_image_data_uri,
        instructions = EXCLUDED.instructions,
        is_active = EXCLUDED.is_active,
        updated_by = EXCLUDED.updated_by,
        updated_at = NOW()
    WHERE org_payment_qr_codes.tenant_id = p_tenant_id
    RETURNING *;
$function$;

CREATE OR REPLACE FUNCTION public.fn_delete_org_qr_code(
    p_tenant_id UUID, p_organization_id UUID, p_slot_index INT
)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $function$
BEGIN
    DELETE FROM org_payment_qr_codes
    WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND slot_index = p_slot_index;
    RETURN FOUND;
END;
$function$;

CREATE OR REPLACE PROCEDURE public.sp_insert_tuition_invoice(
    IN p_id UUID, IN p_tenant_id UUID, IN p_organization_id UUID,
    IN p_branch_id UUID, IN p_child_id UUID, IN p_contract_id UUID,
    IN p_invoice_number VARCHAR, IN p_billing_period VARCHAR,
    IN p_subtotal NUMERIC, IN p_discount_amount NUMERIC, IN p_tax_amount NUMERIC,
    IN p_total_amount NUMERIC, IN p_currency VARCHAR, IN p_status VARCHAR,
    IN p_fee_method VARCHAR, IN p_fee_plan_name VARCHAR,
    IN p_issue_date TIMESTAMPTZ, IN p_due_date TIMESTAMPTZ,
    IN p_created_by VARCHAR, IN p_description TEXT, IN p_notes TEXT,
    IN p_payment_instructions TEXT, IN p_payment_instructions_snapshot JSONB,
    IN p_billing_contact_email VARCHAR
)
LANGUAGE plpgsql AS $procedure$
BEGIN
    INSERT INTO invoices (
        id, tenant_id, organization_id, branch_id, child_id, contract_id,
        invoice_number, billing_period, subtotal, discount_amount, tax_amount,
        total_amount, amount_paid, adjustment_amount, pending_verification_amount,
        currency, status, fee_method, fee_plan_name, issue_date, due_date,
        published_at, published_by, created_by, description, notes,
        payment_instructions, payment_instructions_snapshot, billing_contact_email,
        created_at, updated_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_contract_id,
        p_invoice_number, p_billing_period, p_subtotal, p_discount_amount, p_tax_amount,
        p_total_amount, 0, 0, 0,
        p_currency, p_status, p_fee_method, p_fee_plan_name, p_issue_date, p_due_date,
        CASE WHEN p_status <> 'Draft' THEN NOW() ELSE NULL END,
        CASE WHEN p_status <> 'Draft' THEN p_created_by ELSE NULL END,
        p_created_by, p_description, p_notes,
        p_payment_instructions, p_payment_instructions_snapshot, p_billing_contact_email,
        NOW(), NOW()
    );
END;
$procedure$;

CREATE OR REPLACE PROCEDURE public.sp_insert_invoice_installment(
    IN p_id UUID, IN p_invoice_id UUID, IN p_installment_number INT,
    IN p_due_date DATE, IN p_amount_due NUMERIC,
    IN p_allow_custom_amount BOOLEAN, IN p_allow_multiple_transactions BOOLEAN, IN p_notes TEXT
)
LANGUAGE plpgsql AS $procedure$
BEGIN
    INSERT INTO invoice_installments (
        id, invoice_id, installment_number, due_date, amount_due,
        allow_custom_amount, allow_multiple_transactions, notes, created_at
    ) VALUES (
        p_id, p_invoice_id, p_installment_number, p_due_date, p_amount_due,
        p_allow_custom_amount, p_allow_multiple_transactions, p_notes, NOW()
    );
END;
$procedure$;

CREATE OR REPLACE FUNCTION public.fn_get_invoice_installments(p_invoice_id UUID)
RETURNS SETOF invoice_installments
LANGUAGE sql STABLE AS $function$
    SELECT * FROM invoice_installments WHERE invoice_id = p_invoice_id ORDER BY installment_number ASC;
$function$;

CREATE OR REPLACE FUNCTION public.fn_apply_invoice_adjustment(
    p_invoice_id UUID, p_adjustment_delta NUMERIC, p_new_status VARCHAR
)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $function$
BEGIN
    UPDATE invoices
    SET adjustment_amount = COALESCE(adjustment_amount, 0) + p_adjustment_delta,
        status = CASE WHEN status IN ('Void', 'Cancelled') THEN status ELSE p_new_status END,
        updated_at = NOW()
    WHERE id = p_invoice_id;
    IF NOT FOUND THEN RETURN FALSE; END IF;
    CALL public.sp_recalculate_invoice_balance(p_invoice_id);
    RETURN TRUE;
END;
$function$;

CREATE OR REPLACE FUNCTION public.fn_cancel_invoice(
    p_invoice_id UUID, p_cancelled_by VARCHAR, p_reason TEXT
)
RETURNS BOOLEAN
LANGUAGE plpgsql AS $function$
BEGIN
    UPDATE invoices
    SET status = 'Cancelled',
        voided_at = NOW(),
        voided_by = p_cancelled_by,
        void_reason = p_reason,
        updated_at = NOW()
    WHERE id = p_invoice_id AND amount_paid = 0;
    RETURN FOUND;
END;
$function$;

CREATE OR REPLACE PROCEDURE public.sp_submit_parent_payment(
    IN p_id UUID, IN p_tenant_id UUID, IN p_organization_id UUID,
    IN p_branch_id UUID, IN p_child_id UUID, IN p_invoice_id UUID,
    IN p_invoice_number VARCHAR, IN p_payment_reference VARCHAR,
    IN p_amount NUMERIC, IN p_currency VARCHAR, IN p_method VARCHAR,
    IN p_transaction_id VARCHAR, IN p_notes TEXT,
    IN p_submission_source VARCHAR, IN p_proof_storage_key TEXT,
    IN p_recorded_by_user_id UUID, IN p_recorded_by_email VARCHAR,
    IN p_payment_date TIMESTAMPTZ
)
LANGUAGE plpgsql AS $procedure$
BEGIN
    INSERT INTO payments (
        id, tenant_id, organization_id, branch_id, child_id, invoice_id, invoice_number,
        payment_reference, amount, currency, method, is_verified, status, transaction_id,
        notes, submission_source, proof_storage_key, recorded_by_user_id, recorded_by_email,
        payment_date, created_at
    ) VALUES (
        p_id, p_tenant_id, p_organization_id, p_branch_id, p_child_id, p_invoice_id, p_invoice_number,
        p_payment_reference, p_amount, p_currency, p_method, FALSE, 'PendingVerification', p_transaction_id,
        p_notes, p_submission_source, p_proof_storage_key, p_recorded_by_user_id, p_recorded_by_email,
        p_payment_date, NOW()
    );
    CALL public.sp_recalculate_invoice_balance(p_invoice_id);
END;
$procedure$;

