-- ============================================================
-- EnjoyEveryday — Migration 140: Birthday Gifting & Classroom Giving
-- Creates: classroom_gifting_orders, classroom_gifting_order_items,
--          classroom_gifting_audit_events
-- Extends: payments (gifting_order_id)
-- Seeds: gifting permissions and role mappings
-- Functions & Procedures:
--   - fn_get_classroom_peers_for_gifting
--   - sp_submit_classroom_gifting_order
--   - sp_approve_or_reject_gifting_order
--   - sp_record_gifting_cash_payment
--   - fn_verify_gifting_payment
--   - fn_reject_gifting_payment
--   - sp_update_gifting_fulfilment_status
--   - fn_get_gifting_orders_for_admin
--   - fn_get_gifting_orders_for_parent
--   - fn_get_gifting_order_by_id
--   - fn_get_gifting_audit_events
--   - fn_get_gifting_financial_reconciliation
-- ============================================================

-- 1. SEED PERMISSIONS
INSERT INTO permissions (name, description) VALUES
    ('gifting.order.create', 'Create and submit classroom gifts and birthday contributions'),
    ('gifting.order.approve', 'Approve or reject classroom gifting orders (Branch Admin or Org Admin)'),
    ('gifting.payment.verify', 'Verify manual UPI, bank transfer, and gateway payments for gifting'),
    ('gifting.cash.receive', 'Physically receive and record cash payments for gifting orders'),
    ('gifting.fulfilment.manage', 'Update order preparation and handover/dispatch status'),
    ('gifting.distribution.confirm', 'Confirm physical receipt at classroom and distribution to children'),
    ('gifting.reports.view', 'View gifting financial reconciliation and order volume reports')
ON CONFLICT (name) DO NOTHING;

-- Grant permissions to PlatformAdmin system role
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name = 'PlatformAdmin'
  AND p.name LIKE 'gifting.%'
ON CONFLICT DO NOTHING;

-- Grant permissions to OrganizationOwner & OrganizationAdmin system roles
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin')
  AND p.name IN ('gifting.order.approve', 'gifting.payment.verify', 'gifting.cash.receive', 'gifting.fulfilment.manage', 'gifting.distribution.confirm', 'gifting.reports.view')
ON CONFLICT DO NOTHING;

-- Grant permissions to BranchAdmin system role
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name = 'BranchAdmin'
  AND p.name IN ('gifting.order.approve', 'gifting.payment.verify', 'gifting.cash.receive', 'gifting.fulfilment.manage', 'gifting.distribution.confirm', 'gifting.reports.view')
ON CONFLICT DO NOTHING;

-- Grant permissions to LeadTeacher, DaycareTeacher, ClassroomTeacher
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name IN ('LeadTeacher', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher')
  AND p.name IN ('gifting.distribution.confirm')
ON CONFLICT DO NOTHING;

-- Grant permissions to Parent system role
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NULL 
  AND r.name = 'Parent'
  AND p.name IN ('gifting.order.create')
ON CONFLICT DO NOTHING;

-- Tenant-scoped roles mirror
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NOT NULL 
  AND r.name IN ('OrganizationOwner', 'OrganizationAdmin', 'BranchAdmin')
  AND p.name IN ('gifting.order.approve', 'gifting.payment.verify', 'gifting.cash.receive', 'gifting.fulfilment.manage', 'gifting.distribution.confirm', 'gifting.reports.view')
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NOT NULL 
  AND r.name IN ('LeadTeacher', 'DaycareTeacher', 'ClassroomTeacher', 'Teacher')
  AND p.name IN ('gifting.distribution.confirm')
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.tenant_id IS NOT NULL 
  AND r.name = 'Parent'
  AND p.name IN ('gifting.order.create')
ON CONFLICT DO NOTHING;

-- 2. CREATE CLASSROOM_GIFTING_ORDERS TABLE
CREATE TABLE IF NOT EXISTS classroom_gifting_orders (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number                    VARCHAR(50) NOT NULL UNIQUE,
    tenant_id                       UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    organization_id                 UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    branch_id                       UUID NOT NULL REFERENCES branches(id) ON DELETE CASCADE,
    classroom_id                    UUID NOT NULL REFERENCES classrooms(id) ON DELETE CASCADE,
    parent_user_id                  UUID NOT NULL REFERENCES users(id),
    parent_email                    VARCHAR(256),
    parent_name                     VARCHAR(200),
    child_id                        UUID REFERENCES children(id) ON DELETE SET NULL,
    recipient_type                  VARCHAR(50) NOT NULL, -- 'BirthdayGift', 'ClassroomContribution'
    recipient_child_id              UUID REFERENCES children(id) ON DELETE SET NULL,
    recipient_child_name            VARCHAR(200),
    greeting_message                TEXT,
    delivery_destination_type       VARCHAR(50) NOT NULL, -- 'BranchAddress', 'OrganizationAddress'
    confirmed_delivery_address      TEXT NOT NULL,
    confirmed_contact_name          VARCHAR(150),
    confirmed_contact_phone         VARCHAR(50),
    total_amount                    NUMERIC(10, 2) NOT NULL CHECK (total_amount >= 0),
    currency                        VARCHAR(10) NOT NULL DEFAULT 'INR',
    approval_status                 VARCHAR(50) NOT NULL DEFAULT 'PendingApproval', 
    -- 'PendingApproval', 'Approved', 'Rejected', 'Cancelled'
    approval_decided_by_user_id     UUID REFERENCES users(id) ON DELETE SET NULL,
    approval_decided_by_name        VARCHAR(200),
    approval_decided_by_role        VARCHAR(50),
    approval_decided_at             TIMESTAMPTZ,
    rejection_reason                TEXT,
    payment_method                  VARCHAR(50), 
    -- 'Cash', 'Razorpay', 'GooglePay_UPI', 'BankTransfer'
    payment_status                  VARCHAR(50) NOT NULL DEFAULT 'AwaitingPayment',
    -- 'AwaitingPayment', 'CashPaymentPending', 'PaymentVerificationPending', 'PartiallyPaid', 'Paid', 'PaymentFailed', 'PaymentRejected'
    fulfilment_status               VARCHAR(50) NOT NULL DEFAULT 'NotStarted',
    -- 'NotStarted', 'Preparing', 'ReadyForDelivery', 'DispatchedOrHandedOver', 'ReceivedAtDestination', 'Distributed', 'Completed'
    prepared_by_user_id             UUID REFERENCES users(id) ON DELETE SET NULL,
    prepared_by_name                VARCHAR(200),
    prepared_at                     TIMESTAMPTZ,
    ready_at                        TIMESTAMPTZ,
    dispatched_at                   TIMESTAMPTZ,
    received_by_user_id             UUID REFERENCES users(id) ON DELETE SET NULL,
    received_by_name                VARCHAR(200),
    received_at                     TIMESTAMPTZ,
    distributed_by_user_id          UUID REFERENCES users(id) ON DELETE SET NULL,
    distributed_by_name             VARCHAR(200),
    distributed_at                  TIMESTAMPTZ,
    distribution_notes              TEXT,
    completed_at                    TIMESTAMPTZ,
    cancelled_at                    TIMESTAMPTZ,
    cancellation_reason             TEXT,
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gifting_orders_org ON classroom_gifting_orders(tenant_id, organization_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_branch ON classroom_gifting_orders(branch_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_classroom ON classroom_gifting_orders(classroom_id);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_parent ON classroom_gifting_orders(parent_user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_approval ON classroom_gifting_orders(approval_status);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_payment ON classroom_gifting_orders(payment_status);
CREATE INDEX IF NOT EXISTS idx_gifting_orders_fulfilment ON classroom_gifting_orders(fulfilment_status);

-- 3. CREATE CLASSROOM_GIFTING_ORDER_ITEMS TABLE
CREATE TABLE IF NOT EXISTS classroom_gifting_order_items (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES classroom_gifting_orders(id) ON DELETE CASCADE,
    material_product_id     UUID NOT NULL REFERENCES material_products(id),
    product_name            VARCHAR(200) NOT NULL,
    product_sku             VARCHAR(64) NOT NULL,
    category                VARCHAR(100),
    image_url               TEXT,
    quantity                INT NOT NULL CHECK (quantity > 0),
    unit_price              NUMERIC(10, 2) NOT NULL CHECK (unit_price >= 0),
    line_total              NUMERIC(10, 2) NOT NULL CHECK (line_total >= 0),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gifting_items_order ON classroom_gifting_order_items(order_id);

-- 4. CREATE CLASSROOM_GIFTING_AUDIT_EVENTS TABLE
CREATE TABLE IF NOT EXISTS classroom_gifting_audit_events (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES classroom_gifting_orders(id) ON DELETE CASCADE,
    tenant_id               UUID NOT NULL,
    organization_id         UUID NOT NULL,
    branch_id               UUID,
    classroom_id            UUID,
    event_type              VARCHAR(100) NOT NULL,
    previous_status         VARCHAR(50),
    new_status              VARCHAR(50),
    actor_user_id           UUID,
    actor_name              VARCHAR(200),
    actor_role              VARCHAR(50) NOT NULL DEFAULT 'System',
    description             TEXT NOT NULL,
    notes                   TEXT,
    metadata                JSONB,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gifting_audit_order ON classroom_gifting_audit_events(order_id, created_at ASC);
CREATE INDEX IF NOT EXISTS idx_gifting_audit_org ON classroom_gifting_audit_events(organization_id, created_at DESC);

-- 5. EXTEND PAYMENTS TABLE WITH GIFTING_ORDER_ID
ALTER TABLE payments
    ADD COLUMN IF NOT EXISTS gifting_order_id UUID REFERENCES classroom_gifting_orders(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_payments_gifting_order ON payments(gifting_order_id);

-- 6. STORED PROCEDURES & FUNCTIONS

-- Function to get classroom peers eligible for gifting (with upcoming birthdays)
CREATE OR REPLACE FUNCTION public.fn_get_classroom_peers_for_gifting(
    p_tenant_id UUID,
    p_classroom_id UUID,
    p_exclude_child_id UUID DEFAULT NULL
)
RETURNS TABLE (
    child_id UUID,
    first_name VARCHAR,
    last_name VARCHAR,
    date_of_birth DATE,
    photo_url TEXT,
    classroom_id UUID,
    is_birthday_today BOOLEAN,
    is_birthday_this_month BOOLEAN
)
LANGUAGE plpgsql STABLE AS $function$
BEGIN
    RETURN QUERY
    SELECT 
        c.id AS child_id,
        c.first_name,
        c.last_name,
        c.date_of_birth,
        c.photo_url,
        c.classroom_id,
        (EXTRACT(MONTH FROM c.date_of_birth) = EXTRACT(MONTH FROM CURRENT_DATE) AND 
         EXTRACT(DAY FROM c.date_of_birth) = EXTRACT(DAY FROM CURRENT_DATE)) AS is_birthday_today,
        (EXTRACT(MONTH FROM c.date_of_birth) = EXTRACT(MONTH FROM CURRENT_DATE)) AS is_birthday_this_month
    FROM children c
    WHERE c.tenant_id = p_tenant_id
      AND c.classroom_id = p_classroom_id
      AND c.is_active = TRUE
      AND (p_exclude_child_id IS NULL OR c.id <> p_exclude_child_id)
    ORDER BY is_birthday_today DESC, is_birthday_this_month DESC, c.first_name, c.last_name;
END;
$function$;

-- Procedure to create and submit a classroom gifting order atomically
CREATE OR REPLACE FUNCTION public.sp_submit_classroom_gifting_order(
    p_order_id UUID,
    p_order_number VARCHAR,
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID,
    p_classroom_id UUID,
    p_parent_user_id UUID,
    p_parent_email VARCHAR,
    p_parent_name VARCHAR,
    p_child_id UUID,
    p_recipient_type VARCHAR,
    p_recipient_child_id UUID,
    p_recipient_child_name VARCHAR,
    p_greeting_message TEXT,
    p_delivery_destination_type VARCHAR,
    p_confirmed_delivery_address TEXT,
    p_confirmed_contact_name VARCHAR,
    p_confirmed_contact_phone VARCHAR,
    p_total_amount NUMERIC,
    p_currency VARCHAR,
    p_items_json JSONB
)
RETURNS TABLE (success BOOLEAN, message TEXT, order_id UUID, order_number VARCHAR)
LANGUAGE plpgsql AS $function$
DECLARE
    v_item JSONB;
BEGIN
    -- 1. Insert order record
    INSERT INTO classroom_gifting_orders (
        id, order_number, tenant_id, organization_id, branch_id, classroom_id,
        parent_user_id, parent_email, parent_name, child_id, recipient_type,
        recipient_child_id, recipient_child_name, greeting_message,
        delivery_destination_type, confirmed_delivery_address, confirmed_contact_name,
        confirmed_contact_phone, total_amount, currency, approval_status,
        payment_status, fulfilment_status, created_at, updated_at
    ) VALUES (
        p_order_id, p_order_number, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        p_parent_user_id, p_parent_email, p_parent_name, p_child_id, p_recipient_type,
        p_recipient_child_id, p_recipient_child_name, p_greeting_message,
        p_delivery_destination_type, p_confirmed_delivery_address, p_confirmed_contact_name,
        p_confirmed_contact_phone, p_total_amount, COALESCE(p_currency, 'INR'), 'PendingApproval',
        'AwaitingPayment', 'NotStarted', NOW(), NOW()
    );

    -- 2. Insert items
    FOR v_item IN SELECT * FROM jsonb_array_elements(p_items_json)
    LOOP
        INSERT INTO classroom_gifting_order_items (
            order_id, material_product_id, product_name, product_sku,
            category, image_url, quantity, unit_price, line_total
        ) VALUES (
            p_order_id,
            (v_item->>'material_product_id')::UUID,
            v_item->>'product_name',
            v_item->>'product_sku',
            v_item->>'category',
            v_item->>'image_url',
            (v_item->>'quantity')::INT,
            (v_item->>'unit_price')::NUMERIC,
            (v_item->>'line_total')::NUMERIC
        );
    END LOOP;

    -- 3. Insert audit event
    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        p_order_id, p_tenant_id, p_organization_id, p_branch_id, p_classroom_id,
        'OrderSubmitted', NULL, 'PendingApproval', p_parent_user_id, p_parent_name,
        'Parent', format('Order %s submitted by parent %s for amount %s %s.', p_order_number, p_parent_name, COALESCE(p_currency, 'INR'), p_total_amount),
        p_greeting_message
    );

    RETURN QUERY SELECT TRUE, 'Order submitted successfully.'::TEXT, p_order_id, p_order_number;
END;
$function$;

-- Procedure for either Branch Admin OR Org Admin approval/rejection (First-decision concurrency protection)
CREATE OR REPLACE FUNCTION public.sp_approve_or_reject_gifting_order(
    p_order_id UUID,
    p_actor_user_id UUID,
    p_actor_name VARCHAR,
    p_actor_role VARCHAR,
    p_decision VARCHAR, -- 'Approved' or 'Rejected'
    p_reason TEXT DEFAULT NULL
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    order_id UUID,
    order_number VARCHAR,
    approval_status VARCHAR,
    approval_decided_by_name VARCHAR,
    approval_decided_at TIMESTAMPTZ
)
LANGUAGE plpgsql AS $function$
DECLARE
    v_order classroom_gifting_orders%ROWTYPE;
BEGIN
    -- Pessimistic locking to serialize competing decisions
    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Gifting order not found.'::TEXT, p_order_id, ''::VARCHAR, ''::VARCHAR, ''::VARCHAR, NULL::TIMESTAMPTZ;
        RETURN;
    END IF;

    IF v_order.approval_status <> 'PendingApproval' THEN
        RETURN QUERY SELECT FALSE, 
            format('Order %s has already been decided (%s by %s on %s).', 
                   v_order.order_number, v_order.approval_status, COALESCE(v_order.approval_decided_by_name, 'Unknown'), 
                   to_char(v_order.approval_decided_at, 'YYYY-MM-DD HH24:MI:SS')),
            v_order.id, v_order.order_number, v_order.approval_status, v_order.approval_decided_by_name, v_order.approval_decided_at;
        RETURN;
    END IF;

    IF p_decision = 'Approved' THEN
        UPDATE classroom_gifting_orders
        SET approval_status = 'Approved',
            approval_decided_by_user_id = p_actor_user_id,
            approval_decided_by_name = p_actor_name,
            approval_decided_by_role = p_actor_role,
            approval_decided_at = NOW(),
            payment_status = 'AwaitingPayment',
            updated_at = NOW()
        WHERE id = p_order_id;

        INSERT INTO classroom_gifting_audit_events (
            order_id, tenant_id, organization_id, branch_id, classroom_id,
            event_type, previous_status, new_status, actor_user_id, actor_name,
            actor_role, description, notes
        ) VALUES (
            v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
            'OrderApproved', 'PendingApproval', 'Approved', p_actor_user_id, p_actor_name,
            p_actor_role, format('Order %s was approved by %s (%s).', v_order.order_number, p_actor_name, p_actor_role),
            p_reason
        );

        RETURN QUERY SELECT TRUE, 'Order approved successfully.'::TEXT, v_order.id, v_order.order_number, 'Approved'::VARCHAR, p_actor_name, NOW();
    ELSIF p_decision = 'Rejected' THEN
        UPDATE classroom_gifting_orders
        SET approval_status = 'Rejected',
            approval_decided_by_user_id = p_actor_user_id,
            approval_decided_by_name = p_actor_name,
            approval_decided_by_role = p_actor_role,
            approval_decided_at = NOW(),
            rejection_reason = p_reason,
            payment_status = 'Cancelled',
            fulfilment_status = 'Cancelled',
            updated_at = NOW()
        WHERE id = p_order_id;

        INSERT INTO classroom_gifting_audit_events (
            order_id, tenant_id, organization_id, branch_id, classroom_id,
            event_type, previous_status, new_status, actor_user_id, actor_name,
            actor_role, description, notes
        ) VALUES (
            v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
            'OrderRejected', 'PendingApproval', 'Rejected', p_actor_user_id, p_actor_name,
            p_actor_role, format('Order %s was rejected by %s (%s). Reason: %s', v_order.order_number, p_actor_name, p_actor_role, COALESCE(p_reason, 'No reason specified')),
            p_reason
        );

        RETURN QUERY SELECT TRUE, 'Order rejected successfully.'::TEXT, v_order.id, v_order.order_number, 'Rejected'::VARCHAR, p_actor_name, NOW();
    ELSE
        RETURN QUERY SELECT FALSE, 'Invalid decision. Must be Approved or Rejected.'::TEXT, v_order.id, v_order.order_number, v_order.approval_status, v_order.approval_decided_by_name, v_order.approval_decided_at;
    END IF;
END;
$function$;

-- Procedure to record physical cash payment by authorized staff
CREATE OR REPLACE FUNCTION public.sp_record_gifting_cash_payment(
    p_payment_id UUID,
    p_order_id UUID,
    p_amount_received NUMERIC,
    p_staff_user_id UUID,
    p_staff_email VARCHAR,
    p_staff_name VARCHAR,
    p_receipt_number VARCHAR,
    p_notes TEXT DEFAULT NULL
)
RETURNS TABLE (success BOOLEAN, message TEXT, payment_id UUID, receipt_number VARCHAR)
LANGUAGE plpgsql AS $function$
DECLARE
    v_order classroom_gifting_orders%ROWTYPE;
BEGIN
    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Gifting order not found.'::TEXT, p_payment_id, ''::VARCHAR;
        RETURN;
    END IF;

    IF v_order.approval_status <> 'Approved' THEN
        RETURN QUERY SELECT FALSE, 'Payment cannot be received for an unapproved order.'::TEXT, p_payment_id, ''::VARCHAR;
        RETURN;
    END IF;

    -- Record in shared payments table
    INSERT INTO payments (
        id, tenant_id, organization_id, branch_id, child_id, gifting_order_id,
        amount, currency, method, is_verified, verified_by, verified_at,
        status, receipt_number, recorded_by_user_id, recorded_by_email,
        payment_reference, submission_source, notes, billing_domain,
        payment_date, created_at
    ) VALUES (
        p_payment_id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.child_id, v_order.id,
        p_amount_received, v_order.currency, 'Cash', TRUE, p_staff_name, NOW(),
        'Completed', p_receipt_number, p_staff_user_id, p_staff_email,
        p_receipt_number, 'Staff', p_notes, 'ClassroomGifting',
        NOW(), NOW()
    );

    -- Update order payment status
    UPDATE classroom_gifting_orders
    SET payment_method = 'Cash',
        payment_status = CASE 
            WHEN p_amount_received >= v_order.total_amount THEN 'Paid'
            ELSE 'PartiallyPaid'
        END,
        updated_at = NOW()
    WHERE id = p_order_id;

    -- Record audit event
    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
        'CashPaymentReceived', v_order.payment_status, 
        CASE WHEN p_amount_received >= v_order.total_amount THEN 'Paid' ELSE 'PartiallyPaid' END,
        p_staff_user_id, p_staff_name, 'Staff',
        format('Physical cash of %s %s received by %s (Receipt #%s).', v_order.currency, p_amount_received, p_staff_name, p_receipt_number),
        p_notes
    );

    RETURN QUERY SELECT TRUE, 'Cash payment recorded and receipt generated.'::TEXT, p_payment_id, p_receipt_number;
END;
$function$;

-- Function to verify manual UPI or Bank Transfer payment
CREATE OR REPLACE FUNCTION public.fn_verify_gifting_payment(
    p_payment_id UUID,
    p_verifier_user_id UUID,
    p_verifier_name VARCHAR,
    p_verifier_role VARCHAR,
    p_remarks TEXT DEFAULT NULL
)
RETURNS TABLE (success BOOLEAN, message TEXT, order_id UUID, payment_id UUID)
LANGUAGE plpgsql AS $function$
DECLARE
    v_payment payments%ROWTYPE;
    v_order classroom_gifting_orders%ROWTYPE;
    v_total_paid NUMERIC;
BEGIN
    SELECT * INTO v_payment
    FROM payments
    WHERE id = p_payment_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Payment submission not found.'::TEXT, NULL::UUID, p_payment_id;
        RETURN;
    END IF;

    IF v_payment.gifting_order_id IS NULL THEN
        RETURN QUERY SELECT FALSE, 'This payment is not linked to a gifting order.'::TEXT, NULL::UUID, p_payment_id;
        RETURN;
    END IF;

    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = v_payment.gifting_order_id
    FOR UPDATE;

    IF v_payment.is_verified OR v_payment.status = 'Completed' THEN
        RETURN QUERY SELECT FALSE, 'This payment has already been verified.'::TEXT, v_order.id, p_payment_id;
        RETURN;
    END IF;

    -- Update payment
    UPDATE payments
    SET is_verified = TRUE,
        verified_by = p_verifier_name,
        verified_at = NOW(),
        status = 'Completed',
        notes = CASE 
            WHEN p_remarks IS NOT NULL AND LENGTH(TRIM(p_remarks)) > 0
                THEN COALESCE(notes || E'\nVerification: ', 'Verification: ') || p_remarks
            ELSE notes
        END
    WHERE id = p_payment_id;

    -- Calculate total verified paid for this order
    SELECT COALESCE(SUM(amount), 0) INTO v_total_paid
    FROM payments
    WHERE gifting_order_id = v_order.id
      AND is_verified = TRUE
      AND status = 'Completed'
      AND is_reversed = FALSE;

    -- Update order payment status
    UPDATE classroom_gifting_orders
    SET payment_status = CASE
            WHEN v_total_paid >= total_amount THEN 'Paid'
            ELSE 'PartiallyPaid'
        END,
        updated_at = NOW()
    WHERE id = v_order.id;

    -- Record audit event
    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
        'PaymentVerified', v_order.payment_status,
        CASE WHEN v_total_paid >= v_order.total_amount THEN 'Paid' ELSE 'PartiallyPaid' END,
        p_verifier_user_id, p_verifier_name, p_verifier_role,
        format('Payment of %s %s (%s) verified by %s (%s).', v_payment.currency, v_payment.amount, v_payment.method, p_verifier_name, p_verifier_role),
        p_remarks
    );

    RETURN QUERY SELECT TRUE, 'Payment verified successfully.'::TEXT, v_order.id, p_payment_id;
END;
$function$;

-- Function to reject manual payment submission
CREATE OR REPLACE FUNCTION public.fn_reject_gifting_payment(
    p_payment_id UUID,
    p_actor_user_id UUID,
    p_actor_name VARCHAR,
    p_actor_role VARCHAR,
    p_rejection_reason TEXT
)
RETURNS TABLE (success BOOLEAN, message TEXT, order_id UUID, payment_id UUID)
LANGUAGE plpgsql AS $function$
DECLARE
    v_payment payments%ROWTYPE;
    v_order classroom_gifting_orders%ROWTYPE;
BEGIN
    SELECT * INTO v_payment
    FROM payments
    WHERE id = p_payment_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Payment record not found.'::TEXT, NULL::UUID, p_payment_id;
        RETURN;
    END IF;

    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = v_payment.gifting_order_id
    FOR UPDATE;

    UPDATE payments
    SET status = 'Rejected',
        rejected_by = p_actor_name,
        rejected_at = NOW(),
        rejection_reason = p_rejection_reason
    WHERE id = p_payment_id;

    UPDATE classroom_gifting_orders
    SET payment_status = 'PaymentRejected',
        updated_at = NOW()
    WHERE id = v_order.id;

    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
        'PaymentRejected', 'PaymentVerificationPending', 'PaymentRejected',
        p_actor_user_id, p_actor_name, p_actor_role,
        format('Payment submission (%s %s) rejected by %s. Reason: %s', v_payment.currency, v_payment.amount, p_actor_name, p_rejection_reason),
        p_rejection_reason
    );

    RETURN QUERY SELECT TRUE, 'Payment submission rejected.'::TEXT, v_order.id, p_payment_id;
END;
$function$;

-- Procedure to update fulfilment lifecycle stages
CREATE OR REPLACE FUNCTION public.sp_update_gifting_fulfilment_status(
    p_order_id UUID,
    p_target_status VARCHAR,
    p_actor_user_id UUID,
    p_actor_name VARCHAR,
    p_actor_role VARCHAR,
    p_notes TEXT DEFAULT NULL
)
RETURNS TABLE (success BOOLEAN, message TEXT, order_id UUID, fulfilment_status VARCHAR)
LANGUAGE plpgsql AS $function$
DECLARE
    v_order classroom_gifting_orders%ROWTYPE;
    v_prev_status VARCHAR;
BEGIN
    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, 'Order not found.'::TEXT, p_order_id, ''::VARCHAR;
        RETURN;
    END IF;

    -- Enforce that unapproved or unpaid orders cannot proceed to normal fulfilment
    IF v_order.approval_status <> 'Approved' THEN
        RETURN QUERY SELECT FALSE, 'Order must be approved before updating fulfilment.'::TEXT, p_order_id, v_order.fulfilment_status;
        RETURN;
    END IF;

    v_prev_status := v_order.fulfilment_status;

    IF p_target_status = 'Preparing' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'Preparing',
            prepared_by_user_id = p_actor_user_id,
            prepared_by_name = p_actor_name,
            prepared_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSIF p_target_status = 'ReadyForDelivery' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'ReadyForDelivery',
            ready_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSIF p_target_status = 'DispatchedOrHandedOver' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'DispatchedOrHandedOver',
            dispatched_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSIF p_target_status = 'ReceivedAtDestination' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'ReceivedAtDestination',
            received_by_user_id = p_actor_user_id,
            received_by_name = p_actor_name,
            received_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSIF p_target_status = 'Distributed' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'Distributed',
            distributed_by_user_id = p_actor_user_id,
            distributed_by_name = p_actor_name,
            distributed_at = NOW(),
            distribution_notes = p_notes,
            completed_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSIF p_target_status = 'Completed' THEN
        UPDATE classroom_gifting_orders
        SET fulfilment_status = 'Completed',
            completed_at = NOW(),
            updated_at = NOW()
        WHERE id = p_order_id;

    ELSE
        RETURN QUERY SELECT FALSE, 'Invalid target fulfilment status.'::TEXT, p_order_id, v_prev_status;
        RETURN;
    END IF;

    -- Audit event
    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
        'FulfilmentStatusChanged', v_prev_status, p_target_status,
        p_actor_user_id, p_actor_name, p_actor_role,
        format('Order fulfilment status updated to %s by %s (%s).', p_target_status, p_actor_name, p_actor_role),
        p_notes
    );

    RETURN QUERY SELECT TRUE, format('Fulfilment status updated to %s.', p_target_status)::TEXT, p_order_id, p_target_status;
END;
$function$;

-- Function for Admin Order Management (Scoped by Org or Branch)
CREATE OR REPLACE FUNCTION public.fn_get_gifting_orders_for_admin(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL,
    p_classroom_id UUID DEFAULT NULL,
    p_status_filter VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    tenant_id UUID,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    parent_user_id UUID,
    parent_email VARCHAR,
    parent_name VARCHAR,
    child_id UUID,
    recipient_type VARCHAR,
    recipient_child_id UUID,
    recipient_child_name VARCHAR,
    greeting_message TEXT,
    delivery_destination_type VARCHAR,
    confirmed_delivery_address TEXT,
    confirmed_contact_name VARCHAR,
    confirmed_contact_phone VARCHAR,
    total_amount NUMERIC,
    currency VARCHAR,
    approval_status VARCHAR,
    approval_decided_by_name VARCHAR,
    approval_decided_at TIMESTAMPTZ,
    rejection_reason TEXT,
    payment_method VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    prepared_by_name VARCHAR,
    prepared_at TIMESTAMPTZ,
    received_by_name VARCHAR,
    received_at TIMESTAMPTZ,
    distributed_by_name VARCHAR,
    distributed_at TIMESTAMPTZ,
    distribution_notes TEXT,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT 
        o.id, o.order_number, o.tenant_id, o.organization_id, o.branch_id, o.classroom_id,
        o.parent_user_id, o.parent_email, o.parent_name, o.child_id, o.recipient_type,
        o.recipient_child_id, o.recipient_child_name, o.greeting_message,
        o.delivery_destination_type, o.confirmed_delivery_address, o.confirmed_contact_name,
        o.confirmed_contact_phone, o.total_amount, o.currency, o.approval_status,
        o.approval_decided_by_name, o.approval_decided_at, o.rejection_reason,
        o.payment_method, o.payment_status, o.fulfilment_status, o.prepared_by_name,
        o.prepared_at, o.received_by_name, o.received_at, o.distributed_by_name,
        o.distributed_at, o.distribution_notes, o.completed_at, o.created_at, o.updated_at
    FROM classroom_gifting_orders o
    WHERE o.tenant_id = p_tenant_id
      AND o.organization_id = p_organization_id
      AND (p_branch_id IS NULL OR o.branch_id = p_branch_id)
      AND (p_classroom_id IS NULL OR o.classroom_id = p_classroom_id)
      AND (p_status_filter IS NULL OR p_status_filter = 'All' OR 
           o.approval_status = p_status_filter OR 
           o.payment_status = p_status_filter OR 
           o.fulfilment_status = p_status_filter)
    ORDER BY o.created_at DESC;
$function$;

-- Function for Parent Portal Order Tracking
CREATE OR REPLACE FUNCTION public.fn_get_gifting_orders_for_parent(
    p_tenant_id UUID,
    p_parent_user_id UUID
)
RETURNS TABLE (
    id UUID,
    order_number VARCHAR,
    organization_id UUID,
    branch_id UUID,
    classroom_id UUID,
    child_id UUID,
    recipient_type VARCHAR,
    recipient_child_name VARCHAR,
    greeting_message TEXT,
    delivery_destination_type VARCHAR,
    confirmed_delivery_address TEXT,
    total_amount NUMERIC,
    currency VARCHAR,
    approval_status VARCHAR,
    rejection_reason TEXT,
    payment_method VARCHAR,
    payment_status VARCHAR,
    fulfilment_status VARCHAR,
    distributed_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ
)
LANGUAGE sql STABLE AS $function$
    SELECT 
        o.id, o.order_number, o.organization_id, o.branch_id, o.classroom_id, o.child_id,
        o.recipient_type, o.recipient_child_name, o.greeting_message,
        o.delivery_destination_type, o.confirmed_delivery_address, o.total_amount,
        o.currency, o.approval_status, o.rejection_reason, o.payment_method,
        o.payment_status, o.fulfilment_status, o.distributed_at, o.completed_at, o.created_at
    FROM classroom_gifting_orders o
    WHERE o.tenant_id = p_tenant_id
      AND o.parent_user_id = p_parent_user_id
    ORDER BY o.created_at DESC;
$function$;

-- Function to get single order by ID with details
CREATE OR REPLACE FUNCTION public.fn_get_gifting_order_by_id(p_order_id UUID)
RETURNS SETOF classroom_gifting_orders
LANGUAGE sql STABLE AS $function$
    SELECT * FROM classroom_gifting_orders WHERE id = p_order_id;
$function$;

-- Function to get audit timeline events for an order
CREATE OR REPLACE FUNCTION public.fn_get_gifting_audit_events(p_order_id UUID)
RETURNS SETOF classroom_gifting_audit_events
LANGUAGE sql STABLE AS $function$
    SELECT * FROM classroom_gifting_audit_events 
    WHERE order_id = p_order_id 
    ORDER BY created_at ASC;
$function$;

-- Financial Reconciliation Report for Gifting Orders
CREATE OR REPLACE FUNCTION public.fn_get_gifting_financial_reconciliation(
    p_tenant_id UUID,
    p_organization_id UUID,
    p_branch_id UUID DEFAULT NULL
)
RETURNS TABLE (
    total_orders BIGINT,
    total_order_amount NUMERIC,
    total_paid_amount NUMERIC,
    total_pending_amount NUMERIC,
    cash_collected NUMERIC,
    razorpay_collected NUMERIC,
    upi_collected NUMERIC,
    bank_collected NUMERIC,
    pending_approvals BIGINT,
    pending_verifications BIGINT,
    in_preparation BIGINT,
    delivered_or_distributed BIGINT
)
LANGUAGE plpgsql STABLE AS $function$
BEGIN
    RETURN QUERY
    WITH order_base AS (
        SELECT *
        FROM classroom_gifting_orders
        WHERE tenant_id = p_tenant_id
          AND organization_id = p_organization_id
          AND (p_branch_id IS NULL OR branch_id = p_branch_id)
          AND approval_status <> 'Rejected'
          AND approval_status <> 'Cancelled'
    ),
    payments_base AS (
        SELECT p.*
        FROM payments p
        JOIN order_base o ON p.gifting_order_id = o.id
        WHERE p.is_verified = TRUE
          AND p.status = 'Completed'
          AND p.is_reversed = FALSE
    )
    SELECT
        COUNT(DISTINCT o.id)::BIGINT AS total_orders,
        COALESCE(SUM(o.total_amount), 0)::NUMERIC AS total_order_amount,
        COALESCE(SUM(p.amount), 0)::NUMERIC AS total_paid_amount,
        GREATEST(0, COALESCE(SUM(o.total_amount), 0) - COALESCE(SUM(p.amount), 0))::NUMERIC AS total_pending_amount,
        COALESCE(SUM(CASE WHEN p.method = 'Cash' THEN p.amount ELSE 0 END), 0)::NUMERIC AS cash_collected,
        COALESCE(SUM(CASE WHEN p.method = 'Razorpay' THEN p.amount ELSE 0 END), 0)::NUMERIC AS razorpay_collected,
        COALESCE(SUM(CASE WHEN p.method = 'GooglePay_UPI' OR p.method = 'UPI' THEN p.amount ELSE 0 END), 0)::NUMERIC AS upi_collected,
        COALESCE(SUM(CASE WHEN p.method = 'BankTransfer' THEN p.amount ELSE 0 END), 0)::NUMERIC AS bank_collected,
        (SELECT COUNT(*)::BIGINT FROM classroom_gifting_orders WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND (p_branch_id IS NULL OR branch_id = p_branch_id) AND approval_status = 'PendingApproval') AS pending_approvals,
        (SELECT COUNT(*)::BIGINT FROM classroom_gifting_orders WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND (p_branch_id IS NULL OR branch_id = p_branch_id) AND payment_status IN ('PaymentVerificationPending', 'CashPaymentPending')) AS pending_verifications,
        (SELECT COUNT(*)::BIGINT FROM classroom_gifting_orders WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND (p_branch_id IS NULL OR branch_id = p_branch_id) AND fulfilment_status IN ('Preparing', 'ReadyForDelivery')) AS in_preparation,
        (SELECT COUNT(*)::BIGINT FROM classroom_gifting_orders WHERE tenant_id = p_tenant_id AND organization_id = p_organization_id AND (p_branch_id IS NULL OR branch_id = p_branch_id) AND fulfilment_status IN ('Distributed', 'Completed')) AS delivered_or_distributed
    FROM order_base o
    LEFT JOIN payments_base p ON p.gifting_order_id = o.id;
END;
$function$;

