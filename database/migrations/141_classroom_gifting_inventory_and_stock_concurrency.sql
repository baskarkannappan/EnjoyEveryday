-- ============================================================
-- Migration 141: Classroom Gifting Inventory & Stock Concurrency
-- Ensures live stock validation, atomic reservation, and
-- concurrent order notifications ("Someone has already ordered it")
-- ============================================================

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
    v_product_id UUID;
    v_qty INT;
    v_avail INT;
    v_prod_name TEXT;
BEGIN
    -- 1. Validate live stock for all items with row-level locks
    FOR v_item IN SELECT * FROM jsonb_array_elements(p_items_json)
    LOOP
        v_product_id := (v_item->>'material_product_id')::UUID;
        v_qty := (v_item->>'quantity')::INT;
        v_prod_name := COALESCE(v_item->>'product_name', 'Item');

        -- Lock inventory balance row to serialize concurrent checks
        SELECT available_stock INTO v_avail
        FROM inventory_balances
        WHERE material_product_id = v_product_id
        FOR UPDATE;

        IF v_avail IS NOT NULL AND v_avail < v_qty THEN
            IF v_avail <= 0 THEN
                RETURN QUERY SELECT FALSE, 
                    format('Someone has already ordered "%s" and it is now out of stock. Please remove it from your selection.', v_prod_name), 
                    NULL::UUID, NULL::VARCHAR;
                RETURN;
            ELSE
                RETURN QUERY SELECT FALSE, 
                    format('Someone has already ordered "%s". Only %s item(s) remain in stock (you requested %s). Please adjust your quantity.', v_prod_name, v_avail, v_qty), 
                    NULL::UUID, NULL::VARCHAR;
                RETURN;
            END IF;
        END IF;
    END LOOP;

    -- 2. Deduct available stock & reserve stock atomically upon order placement
    FOR v_item IN SELECT * FROM jsonb_array_elements(p_items_json)
    LOOP
        v_product_id := (v_item->>'material_product_id')::UUID;
        v_qty := (v_item->>'quantity')::INT;

        UPDATE inventory_balances
        SET available_stock = GREATEST(0, available_stock - v_qty),
            reserved_stock = reserved_stock + v_qty,
            updated_at = NOW()
        WHERE material_product_id = v_product_id;
    END LOOP;

    -- 3. Insert order record
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

    -- 4. Insert items
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

    -- 5. Insert audit event
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

-- Update rejection procedure to release reserved stock back to available stock
CREATE OR REPLACE FUNCTION public.sp_approve_or_reject_gifting_order(
    p_order_id UUID,
    p_actor_user_id UUID,
    p_actor_name VARCHAR,
    p_actor_role VARCHAR,
    p_decision VARCHAR,
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
    v_order RECORD;
    v_new_approval VARCHAR;
    v_new_payment VARCHAR;
    v_new_fulfilment VARCHAR;
    v_item RECORD;
BEGIN
    -- 1. Pessimistic lock on the order row
    SELECT * INTO v_order
    FROM classroom_gifting_orders
    WHERE id = p_order_id
    FOR UPDATE;

    IF v_order IS NULL THEN
        RETURN QUERY SELECT FALSE, 'Gifting order not found.'::TEXT, NULL::UUID, NULL::VARCHAR, NULL::VARCHAR, NULL::VARCHAR, NULL::TIMESTAMPTZ;
        RETURN;
    END IF;

    -- 2. Concurrency guard: Check if already decided
    IF v_order.approval_status IN ('Approved', 'Rejected') THEN
        RETURN QUERY SELECT FALSE,
            format('Order %s has already been decided (%s by %s on %s).',
                v_order.order_number,
                v_order.approval_status,
                v_order.approval_decided_by_name,
                to_char(v_order.approval_decided_at, 'YYYY-MM-DD HH24:MI:SS')),
            v_order.id, v_order.order_number, v_order.approval_status,
            v_order.approval_decided_by_name, v_order.approval_decided_at;
        RETURN;
    END IF;

    -- 3. Transition statuses
    IF p_decision = 'Approved' THEN
        v_new_approval := 'Approved';
        v_new_payment := 'AwaitingPayment';
        v_new_fulfilment := 'NotStarted';
    ELSIF p_decision = 'Rejected' THEN
        v_new_approval := 'Rejected';
        v_new_payment := 'Cancelled';
        v_new_fulfilment := 'Cancelled';

        -- Release reserved stock back to available stock
        FOR v_item IN SELECT material_product_id, quantity FROM classroom_gifting_order_items WHERE classroom_gifting_order_items.order_id = p_order_id
        LOOP
            UPDATE inventory_balances
            SET available_stock = available_stock + v_item.quantity,
                reserved_stock = GREATEST(0, reserved_stock - v_item.quantity),
                updated_at = NOW()
            WHERE material_product_id = v_item.material_product_id;
        END LOOP;
    ELSE
        RETURN QUERY SELECT FALSE, 'Invalid decision. Must be Approved or Rejected.'::TEXT, v_order.id, v_order.order_number, v_order.approval_status, NULL::VARCHAR, NULL::TIMESTAMPTZ;
        RETURN;
    END IF;

    -- 4. Update the order
    UPDATE classroom_gifting_orders
    SET approval_status = v_new_approval,
        payment_status = v_new_payment,
        fulfilment_status = v_new_fulfilment,
        approval_decided_by_user_id = p_actor_user_id,
        approval_decided_by_name = p_actor_name,
        approval_decided_by_role = p_actor_role,
        approval_decided_at = NOW(),
        rejection_reason = CASE WHEN p_decision = 'Rejected' THEN p_reason ELSE NULL END,
        updated_at = NOW()
    WHERE id = p_order_id;

    -- 5. Record audit event
    INSERT INTO classroom_gifting_audit_events (
        order_id, tenant_id, organization_id, branch_id, classroom_id,
        event_type, previous_status, new_status, actor_user_id, actor_name,
        actor_role, description, notes
    ) VALUES (
        v_order.id, v_order.tenant_id, v_order.organization_id, v_order.branch_id, v_order.classroom_id,
        CASE WHEN p_decision = 'Approved' THEN 'OrderApproved' ELSE 'OrderRejected' END,
        v_order.approval_status, v_new_approval,
        p_actor_user_id, p_actor_name, p_actor_role,
        format('Order %s %s by %s (%s).', v_order.order_number, lower(p_decision), p_actor_name, p_actor_role),
        p_reason
    );

    RETURN QUERY SELECT TRUE,
        format('Order %s successfully %s.', v_order.order_number, lower(p_decision)),
        v_order.id, v_order.order_number, v_new_approval, p_actor_name, NOW();
END;
$function$;

