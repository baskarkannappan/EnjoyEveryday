-- Migration 113: Phase 2 - Experience Session Journey Integration & Warehouse Dispatch Reconciliations
-- EnjoyEveryday Daycare Management Platform

-- 1. Function to check if a journey moment already exists for an experience participation (prevent duplicate moments)
CREATE OR REPLACE FUNCTION public.fn_has_journey_entry_for_participation(
    p_tenant_id UUID,
    p_participation_id UUID
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 
        FROM child_journey_entries 
        WHERE tenant_id = p_tenant_id 
          AND experience_participation_id = p_participation_id
    );
END;
$$;

-- 2. Procedure for educators/lead teachers/admins to approve a child journey moment for publication to parents
CREATE OR REPLACE PROCEDURE public.sp_approve_journey_entry(
    IN p_tenant_id UUID,
    IN p_entry_id UUID,
    IN p_approved_by UUID
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE child_journey_entries
    SET is_parent_visible = true,
        parent_visibility_status = 'Approved',
        updated_at = NOW(),
        updated_by = p_approved_by
    WHERE tenant_id = p_tenant_id AND id = p_entry_id;
END;
$$;

-- 3. Procedure to safely reverse dispatched inventory if a dispatched order or shipment is cancelled/reversed
CREATE OR REPLACE PROCEDURE public.sp_reverse_dispatched_inventory(
    IN p_order_id UUID,
    IN p_actor_email VARCHAR(255)
)
LANGUAGE plpgsql
AS $$
DECLARE
    r_item RECORD;
    v_central_id UUID;
BEGIN
    SELECT id INTO v_central_id FROM inventory_locations WHERE is_central_warehouse = true LIMIT 1;
    IF v_central_id IS NULL THEN
        RETURN;
    END IF;

    FOR r_item IN 
        SELECT material_product_id, quantity, fulfilment_mode 
        FROM material_order_items 
        WHERE order_id = p_order_id
    LOOP
        IF r_item.fulfilment_mode = 'Purchase' THEN
            -- Return from dispatched back to total and available
            CALL public.sp_update_inventory_balance_atomic(
                r_item.material_product_id,
                v_central_id,
                r_item.quantity, -- totalDelta: +qty
                r_item.quantity, -- availableDelta: +qty
                0,               -- reservedDelta
                -r_item.quantity,-- dispatchedDelta: -qty
                0, 0, 0, 0, 0, 0
            );
        ELSE -- Borrow / Loan
            -- Return from dispatched back to available
            CALL public.sp_update_inventory_balance_atomic(
                r_item.material_product_id,
                v_central_id,
                0,               -- totalDelta: 0
                r_item.quantity, -- availableDelta: +qty
                0,               -- reservedDelta
                -r_item.quantity,-- dispatchedDelta: -qty
                0, 0, 0, 0, 0, 0
            );
        END IF;

        -- Record ledger
        INSERT INTO inventory_ledger (
            id, material_product_id, location_id, transaction_type,
            quantity_delta, available_delta, reserved_delta, on_loan_delta,
            reference_type, reference_id, notes, performed_by_email, created_at
        ) VALUES (
            gen_random_uuid(), r_item.material_product_id, v_central_id, 'StockReversal',
            CASE WHEN r_item.fulfilment_mode = 'Purchase' THEN r_item.quantity ELSE 0 END,
            r_item.quantity, 0, 0,
            'OrderCancellation', p_order_id,
            'Stock restored upon cancellation/reversal of dispatched order',
            p_actor_email, NOW()
        );
    END LOOP;
END;
$$;

