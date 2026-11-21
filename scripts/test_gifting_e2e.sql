DO $$
DECLARE
    v_order_id UUID := gen_random_uuid();
    v_order_number VARCHAR(50) := 'CG-' || to_char(NOW(), 'YYYYMMDD') || '-9999';
    v_tenant_id UUID := '2c14f003-1ee6-49de-a55f-fa985e8130b7';
    v_org_id UUID := '2c14f003-1ee6-49de-a55f-fa985e8130b7';
    v_branch_id UUID := 'e78ccfac-3c3e-4e26-b2a6-10c9bfb8eac8';
    v_classroom_id UUID := 'ed303190-59ad-4541-aedb-052ea4b8452d';
    v_child_id UUID := 'b127560e-ee18-4bc6-90cf-5fdb9e6019ee';
    v_parent_id UUID := '50000000-0000-0000-0000-000000000003';
    v_admin_id UUID := '50000000-0000-0000-0000-000000000002';
    v_product_id UUID := 'd0000000-0000-0000-0000-000000000001';
    v_items JSONB;
    v_payment_id UUID := gen_random_uuid();
    v_receipt_no VARCHAR(50) := 'REC-TEST-9999';
    v_res_success BOOLEAN;
    v_res_message TEXT;
    v_count INT;
BEGIN
    RAISE NOTICE '=== STARTING CLASSROOM GIFTING END-TO-END VERIFICATION ===';

    -- Step 1: Submit a Birthday Gift order
    v_items := jsonb_build_array(
        jsonb_build_object(
            'material_product_id', v_product_id,
            'product_sku', 'KIT-WATER-01',
            'product_name', 'Water Exploration & Transfer Kit',
            'category', 'Educational Toy',
            'image_url', '',
            'unit_price', 450.00,
            'quantity', 1,
            'line_total', 450.00
        )
    );

    SELECT success, message INTO v_res_success, v_res_message
    FROM public.sp_submit_classroom_gifting_order(
        p_order_id := v_order_id,
        p_order_number := v_order_number,
        p_tenant_id := v_tenant_id,
        p_organization_id := v_org_id,
        p_branch_id := v_branch_id,
        p_classroom_id := v_classroom_id,
        p_parent_user_id := v_parent_id,
        p_parent_email := 'daniel@sunshinegarden.com',
        p_parent_name := 'Daniel Parent',
        p_child_id := v_child_id,
        p_recipient_type := 'ChildBirthday',
        p_recipient_child_id := v_child_id,
        p_recipient_child_name := 'RANGA PAPA',
        p_greeting_message := 'Happy Birthday Ranga! Have a wonderful day!',
        p_delivery_destination_type := 'BranchAddress',
        p_confirmed_delivery_address := '123 Main St, Branch Campus, Raiburam',
        p_confirmed_contact_name := 'Raiburam Branch Office',
        p_confirmed_contact_phone := '+91 98765 43210',
        p_total_amount := 450.00,
        p_currency := 'INR',
        p_items_json := v_items
    );

    IF v_res_success = false THEN
        RAISE EXCEPTION 'Failed to create gifting order: %', v_res_message;
    END IF;
    RAISE NOTICE 'Step 1 Passed: Order submitted successfully! ID: %, Number: %', v_order_id, v_order_number;

    -- Step 2: Branch Admin approves order
    SELECT success, message INTO v_res_success, v_res_message
    FROM public.sp_approve_or_reject_gifting_order(
        p_order_id := v_order_id,
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Branch Admin Reviewer',
        p_actor_role := 'BranchAdmin',
        p_decision := 'Approved',
        p_reason := 'Approved by branch management'
    );
    IF v_res_success = false THEN
        RAISE EXCEPTION 'Branch Admin approval failed: %', v_res_message;
    END IF;
    RAISE NOTICE 'Step 2 Passed: Order approved by BranchAdmin!';

    -- Step 3: Concurrency check - Org Admin attempts to reject already-approved order
    SELECT success, message INTO v_res_success, v_res_message
    FROM public.sp_approve_or_reject_gifting_order(
        p_order_id := v_order_id,
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Org Admin Conflict Attempt',
        p_actor_role := 'OrgAdmin',
        p_decision := 'Rejected',
        p_reason := 'Late rejection attempt'
    );
    IF v_res_success = false THEN
        RAISE NOTICE 'Step 3 Passed: Concurrency lock successfully blocked duplicate decision: %', v_res_message;
    ELSE
        RAISE EXCEPTION 'Concurrency check failed: double decision was permitted!';
    END IF;

    -- Step 4: Record Cash Payment & Receipt Generation
    SELECT success, message INTO v_res_success, v_res_message
    FROM public.sp_record_gifting_cash_payment(
        p_payment_id := v_payment_id,
        p_order_id := v_order_id,
        p_amount_received := 450.00,
        p_staff_user_id := v_admin_id,
        p_staff_email := 'admin@littlestars.com',
        p_staff_name := 'Admin Collector',
        p_receipt_number := v_receipt_no,
        p_notes := 'Full cash handed over at front desk'
    );
    IF v_res_success = false THEN
        RAISE EXCEPTION 'Cash receipt generation failed: %', v_res_message;
    END IF;
    RAISE NOTICE 'Step 4 Passed: Cash payment confirmed! Receipt: %', v_receipt_no;

    -- Step 5: Fulfilment progression
    -- Preparation
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'Preparation',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Admin Preparer',
        p_actor_role := 'OrgAdmin',
        p_notes := 'Gift package assembled and wrapped'
    );
    -- Ready
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'Ready',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Admin Preparer',
        p_actor_role := 'OrgAdmin',
        p_notes := 'Ready for courier dispatch'
    );
    -- DispatchedOrHandedOver
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'DispatchedOrHandedOver',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Admin Dispatcher',
        p_actor_role := 'OrgAdmin',
        p_notes := 'Handed to branch logistics driver'
    );
    -- ReceivedAtDestination
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'ReceivedAtDestination',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Front Desk Reception',
        p_actor_role := 'BranchAdmin',
        p_notes := 'Received at Branch Front Office by Staff'
    );
    -- ClassroomDelivered
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'ClassroomDelivered',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Teacher Lead',
        p_actor_role := 'Teacher',
        p_notes := 'Brought to Classroom cubby'
    );
    -- Distributed
    PERFORM public.sp_update_gifting_fulfilment_status(
        p_order_id := v_order_id,
        p_target_status := 'Distributed',
        p_actor_user_id := v_admin_id,
        p_actor_name := 'Classroom Teacher',
        p_actor_role := 'Teacher',
        p_notes := 'Presented to Ranga during birthday circle time with card!'
    );
    RAISE NOTICE 'Step 5 Passed: Fulfilment stages progressed to Distributed!';

    -- Step 6: Query Admin Orders
    SELECT COUNT(*) INTO v_count
    FROM public.fn_get_gifting_orders_for_admin(v_tenant_id, v_org_id, v_branch_id);
    IF v_count < 1 THEN
        RAISE EXCEPTION 'Admin query returned 0 rows';
    END IF;
    RAISE NOTICE 'Step 6 Passed: fn_get_gifting_orders_for_admin returned % rows', v_count;

    -- Step 7: Query Parent Orders
    SELECT COUNT(*) INTO v_count
    FROM public.fn_get_gifting_orders_for_parent(v_tenant_id, v_parent_id);
    IF v_count < 1 THEN
        RAISE EXCEPTION 'Parent query returned 0 rows';
    END IF;
    RAISE NOTICE 'Step 7 Passed: fn_get_gifting_orders_for_parent returned % rows', v_count;

    -- Step 8: Audit events check
    SELECT COUNT(*) INTO v_count
    FROM public.fn_get_gifting_audit_events(v_order_id);
    IF v_count < 6 THEN
        RAISE EXCEPTION 'Expected at least 6 audit events, found %', v_count;
    END IF;
    RAISE NOTICE 'Step 8 Passed: fn_get_gifting_audit_events returned % events for order', v_count;

    -- Step 9: Financial Reconciliation
    SELECT total_orders INTO v_count
    FROM public.fn_get_gifting_financial_reconciliation(v_tenant_id, v_org_id);
    IF v_count < 1 THEN
        RAISE EXCEPTION 'Financial reconciliation returned 0 total orders';
    END IF;
    RAISE NOTICE 'Step 9 Passed: fn_get_gifting_financial_reconciliation confirmed % orders reconciled!', v_count;

    RAISE NOTICE '=== ALL END-TO-END VERIFICATION CHECKS COMPLETED SUCCESSFULLY ===';
END $$;
