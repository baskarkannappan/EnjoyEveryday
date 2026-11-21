using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ClassroomGiftingRepository : IClassroomGiftingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassroomGiftingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<GiftingPeerChildDto>> GetClassroomPeersForGiftingAsync(
        Guid tenantId,
        Guid classroomId,
        Guid? excludeChildId = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<GiftingPeerChildDto>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_classroom_peers_for_gifting(@p_tenant_id, @p_classroom_id, @p_exclude_child_id)",
                new
                {
                    p_tenant_id = tenantId,
                    p_classroom_id = classroomId,
                    p_exclude_child_id = excludeChildId
                },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<(bool Success, string Message, Guid OrderId, string OrderNumber)> SubmitOrderAsync(
        ClassroomGiftingOrder order,
        List<ClassroomGiftingOrderItem> items,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var itemsPayload = items.Select(i => new
        {
            material_product_id = i.MaterialProductId,
            product_name = i.ProductName,
            product_sku = i.ProductSku,
            category = i.Category ?? "General",
            image_url = i.ImageUrl ?? string.Empty,
            quantity = i.Quantity,
            unit_price = i.UnitPrice,
            line_total = i.LineTotal
        });
        string itemsJson = JsonSerializer.Serialize(itemsPayload);

        var row = await conn.QueryFirstAsync<SubmitOrderRow>(
            new CommandDefinition(
                "SELECT * FROM public.sp_submit_classroom_gifting_order(@p_order_id, @p_order_number, @p_tenant_id, @p_organization_id, @p_branch_id, @p_classroom_id, @p_parent_user_id, @p_parent_email, @p_parent_name, @p_child_id, @p_recipient_type, @p_recipient_child_id, @p_recipient_child_name, @p_greeting_message, @p_delivery_destination_type, @p_confirmed_delivery_address, @p_confirmed_contact_name, @p_confirmed_contact_phone, @p_total_amount, @p_currency, @p_items_json::jsonb)",
                new
                {
                    p_order_id = order.Id,
                    p_order_number = order.OrderNumber,
                    p_tenant_id = order.TenantId,
                    p_organization_id = order.OrganizationId,
                    p_branch_id = order.BranchId,
                    p_classroom_id = order.ClassroomId,
                    p_parent_user_id = order.ParentUserId,
                    p_parent_email = order.ParentEmail,
                    p_parent_name = order.ParentName,
                    p_child_id = order.ChildId,
                    p_recipient_type = order.RecipientType,
                    p_recipient_child_id = order.RecipientChildId,
                    p_recipient_child_name = order.RecipientChildName,
                    p_greeting_message = order.GreetingMessage,
                    p_delivery_destination_type = order.DeliveryDestinationType,
                    p_confirmed_delivery_address = order.ConfirmedDeliveryAddress,
                    p_confirmed_contact_name = order.ConfirmedContactName,
                    p_confirmed_contact_phone = order.ConfirmedContactPhone,
                    p_total_amount = order.TotalAmount,
                    p_currency = order.Currency,
                    p_items_json = itemsJson
                },
                cancellationToken: ct));

        return (row.success, row.message, row.order_id, row.order_number);
    }

    public async Task<(bool Success, string Message, Guid OrderId, string OrderNumber, string ApprovalStatus, string? DecidedByName, DateTime? DecidedAt)> ApproveOrRejectOrderAsync(
        Guid orderId,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string decision,
        string? reason = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<ApprovalRow>(
            new CommandDefinition(
                "SELECT * FROM public.sp_approve_or_reject_gifting_order(@p_order_id, @p_actor_user_id, @p_actor_name, @p_actor_role, @p_decision, @p_reason)",
                new
                {
                    p_order_id = orderId,
                    p_actor_user_id = actorUserId,
                    p_actor_name = actorName,
                    p_actor_role = actorRole,
                    p_decision = decision,
                    p_reason = reason
                },
                cancellationToken: ct));

        return (row.success, row.message, row.order_id, row.order_number, row.approval_status, row.approval_decided_by_name, row.approval_decided_at);
    }

    public async Task<(bool Success, string Message, Guid PaymentId, string ReceiptNumber)> RecordCashPaymentAsync(
        Guid paymentId,
        Guid orderId,
        decimal amountReceived,
        Guid staffUserId,
        string staffEmail,
        string staffName,
        string receiptNumber,
        string? notes = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<CashPaymentRow>(
            new CommandDefinition(
                "SELECT * FROM public.sp_record_gifting_cash_payment(@p_payment_id, @p_order_id, @p_amount_received, @p_staff_user_id, @p_staff_email, @p_staff_name, @p_receipt_number, @p_notes)",
                new
                {
                    p_payment_id = paymentId,
                    p_order_id = orderId,
                    p_amount_received = amountReceived,
                    p_staff_user_id = staffUserId,
                    p_staff_email = staffEmail,
                    p_staff_name = staffName,
                    p_receipt_number = receiptNumber,
                    p_notes = notes
                },
                cancellationToken: ct));

        return (row.success, row.message, row.payment_id, row.receipt_number);
    }

    public async Task<(bool Success, string Message, Guid? OrderId, Guid PaymentId)> VerifyPaymentAsync(
        Guid paymentId,
        Guid verifierUserId,
        string verifierName,
        string verifierRole,
        string? remarks = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<VerifyPaymentRow>(
            new CommandDefinition(
                "SELECT * FROM public.fn_verify_gifting_payment(@p_payment_id, @p_verifier_user_id, @p_verifier_name, @p_verifier_role, @p_remarks)",
                new
                {
                    p_payment_id = paymentId,
                    p_verifier_user_id = verifierUserId,
                    p_verifier_name = verifierName,
                    p_verifier_role = verifierRole,
                    p_remarks = remarks
                },
                cancellationToken: ct));

        return (row.success, row.message, row.order_id, row.payment_id);
    }

    public async Task<(bool Success, string Message, Guid? OrderId, Guid PaymentId)> RejectPaymentAsync(
        Guid paymentId,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string reason,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<VerifyPaymentRow>(
            new CommandDefinition(
                "SELECT * FROM public.fn_reject_gifting_payment(@p_payment_id, @p_actor_user_id, @p_actor_name, @p_actor_role, @p_rejection_reason)",
                new
                {
                    p_payment_id = paymentId,
                    p_actor_user_id = actorUserId,
                    p_actor_name = actorName,
                    p_actor_role = actorRole,
                    p_rejection_reason = reason
                },
                cancellationToken: ct));

        return (row.success, row.message, row.order_id, row.payment_id);
    }

    public async Task<(bool Success, string Message, Guid OrderId, string FulfilmentStatus)> UpdateFulfilmentStatusAsync(
        Guid orderId,
        string targetStatus,
        Guid actorUserId,
        string actorName,
        string actorRole,
        string? notes = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<FulfilmentRow>(
            new CommandDefinition(
                "SELECT * FROM public.sp_update_gifting_fulfilment_status(@p_order_id, @p_target_status, @p_actor_user_id, @p_actor_name, @p_actor_role, @p_notes)",
                new
                {
                    p_order_id = orderId,
                    p_target_status = targetStatus,
                    p_actor_user_id = actorUserId,
                    p_actor_name = actorName,
                    p_actor_role = actorRole,
                    p_notes = notes
                },
                cancellationToken: ct));

        return (row.success, row.message, row.order_id, row.fulfilment_status);
    }

    public async Task<List<ClassroomGiftingOrder>> GetOrdersForAdminAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ClassroomGiftingOrder>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_gifting_orders_for_admin(@p_tenant_id, @p_organization_id, @p_branch_id, @p_classroom_id, @p_status_filter)",
                new
                {
                    p_tenant_id = tenantId,
                    p_organization_id = organizationId,
                    p_branch_id = branchId,
                    p_classroom_id = classroomId,
                    p_status_filter = statusFilter
                },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<ClassroomGiftingOrder>> GetOrdersForParentAsync(
        Guid tenantId,
        Guid parentUserId,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ClassroomGiftingOrder>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_gifting_orders_for_parent(@p_tenant_id, @p_parent_user_id)",
                new
                {
                    p_tenant_id = tenantId,
                    p_parent_user_id = parentUserId
                },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<ClassroomGiftingOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ClassroomGiftingOrder>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_gifting_order_by_id(@p_order_id)",
                new { p_order_id = orderId },
                cancellationToken: ct));
    }

    public async Task<List<ClassroomGiftingOrderItem>> GetOrderItemsAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ClassroomGiftingOrderItem>(
            new CommandDefinition(
                "SELECT * FROM public.classroom_gifting_order_items WHERE order_id = @p_order_id ORDER BY created_at ASC",
                new { p_order_id = orderId },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<ClassroomGiftingAuditEvent>> GetAuditEventsAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<ClassroomGiftingAuditEvent>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_gifting_audit_events(@p_order_id)",
                new { p_order_id = orderId },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<List<Payment>> GetPaymentsForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<Payment>(
            new CommandDefinition(
                "SELECT * FROM public.payments WHERE gifting_order_id = @p_order_id ORDER BY created_at DESC",
                new { p_order_id = orderId },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<GiftingFinancialReconciliationReport> GetFinancialReconciliationAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<GiftingFinancialReconciliationReport>(
            new CommandDefinition(
                "SELECT * FROM public.fn_get_gifting_financial_reconciliation(@p_tenant_id, @p_organization_id, @p_branch_id)",
                new
                {
                    p_tenant_id = tenantId,
                    p_organization_id = organizationId,
                    p_branch_id = branchId
                },
                cancellationToken: ct));
        return row ?? new GiftingFinancialReconciliationReport();
    }

    public async Task<(bool Success, string Message, Guid PaymentId)> RecordOnlineRazorpayPaymentAsync(
        Payment payment,
        Guid orderId,
        CancellationToken ct = default)
    {
        if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // Idempotent insertion into payments
        const string sql = @"
            INSERT INTO public.payments (
                id, tenant_id, organization_id, branch_id, child_id, gifting_order_id,
                amount, currency, method, is_verified, verified_by, verified_at,
                status, transaction_id, provider_order_id, provider_payment_id, provider_signature,
                payment_reference, receipt_number, submission_source, notes, billing_domain,
                payment_date, created_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ChildId, @GiftingOrderId,
                @Amount, @Currency, 'Razorpay', TRUE, 'Razorpay Gateway', NOW(),
                'Completed', @TransactionId, @ProviderOrderId, @ProviderPaymentId, @ProviderSignature,
                @PaymentReference, @ReceiptNumber, 'Gateway', @Notes, 'ClassroomGifting',
                NOW(), NOW()
            );

            UPDATE public.classroom_gifting_orders
            SET payment_method = 'Razorpay',
                payment_status = 'Paid',
                updated_at = NOW()
            WHERE id = @GiftingOrderId;

            INSERT INTO public.classroom_gifting_audit_events (
                order_id, tenant_id, organization_id, branch_id, classroom_id,
                event_type, previous_status, new_status, actor_user_id, actor_name,
                actor_role, description, notes
            ) VALUES (
                @GiftingOrderId, @TenantId, @OrganizationId, @BranchId, NULL,
                'RazorpayPaymentVerified', 'AwaitingPayment', 'Paid', NULL, 'Razorpay Gateway',
                'System', format('Online Razorpay payment of %s %s confirmed (Payment ID: %s).', @Currency, @Amount, @ProviderPaymentId),
                @Notes
            );";

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                payment.Id,
                payment.TenantId,
                payment.OrganizationId,
                payment.BranchId,
                payment.ChildId,
                GiftingOrderId = orderId,
                payment.Amount,
                Currency = string.IsNullOrWhiteSpace(payment.Currency) ? "INR" : payment.Currency,
                payment.TransactionId,
                payment.ProviderOrderId,
                payment.ProviderPaymentId,
                payment.ProviderSignature,
                payment.PaymentReference,
                ReceiptNumber = payment.ReceiptNumber ?? $"RCP-GIFT-{DateTime.UtcNow:yyyyMMdd}-{orderId.ToString()[..6]}",
                payment.Notes
            },
            cancellationToken: ct));

        return (true, "Razorpay payment verified and recorded.", payment.Id);
    }

    public async Task<Guid> SubmitManualPaymentDeclarationAsync(
        Payment payment,
        Guid orderId,
        CancellationToken ct = default)
    {
        if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO public.payments (
                id, tenant_id, organization_id, branch_id, child_id, gifting_order_id,
                amount, currency, method, is_verified, status, transaction_id,
                payment_reference, submission_source, proof_storage_key, notes,
                billing_domain, payment_date, created_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @BranchId, @ChildId, @GiftingOrderId,
                @Amount, @Currency, @Method, FALSE, 'PendingVerification', @TransactionId,
                @PaymentReference, 'Parent', @ProofStorageKey, @Notes,
                'ClassroomGifting', NOW(), NOW()
            );

            UPDATE public.classroom_gifting_orders
            SET payment_method = @Method,
                payment_status = 'PaymentVerificationPending',
                updated_at = NOW()
            WHERE id = @GiftingOrderId;

            INSERT INTO public.classroom_gifting_audit_events (
                order_id, tenant_id, organization_id, branch_id, classroom_id,
                event_type, previous_status, new_status, actor_user_id, actor_name,
                actor_role, description, notes
            ) VALUES (
                @GiftingOrderId, @TenantId, @OrganizationId, @BranchId, NULL,
                'PaymentSubmittedForVerification', 'AwaitingPayment', 'PaymentVerificationPending',
                @RecordedByUserId, @RecordedByEmail, 'Parent',
                format('Parent submitted %s payment (%s %s, Ref: %s) for verification.', @Method, @Currency, @Amount, COALESCE(@TransactionId, 'None')),
                @Notes
            );";

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                payment.Id,
                payment.TenantId,
                payment.OrganizationId,
                payment.BranchId,
                payment.ChildId,
                GiftingOrderId = orderId,
                payment.Amount,
                Currency = string.IsNullOrWhiteSpace(payment.Currency) ? "INR" : payment.Currency,
                payment.Method,
                payment.TransactionId,
                payment.PaymentReference,
                payment.ProofStorageKey,
                payment.Notes,
                RecordedByUserId = payment.RecordedByUserId,
                RecordedByEmail = payment.RecordedByEmail ?? "Parent"
            },
            cancellationToken: ct));

        return payment.Id;
    }

    private class SubmitOrderRow
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid order_id { get; set; }
        public string order_number { get; set; } = string.Empty;
    }

    private class ApprovalRow
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid order_id { get; set; }
        public string order_number { get; set; } = string.Empty;
        public string approval_status { get; set; } = string.Empty;
        public string? approval_decided_by_name { get; set; }
        public DateTime? approval_decided_at { get; set; }
    }

    private class CashPaymentRow
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid payment_id { get; set; }
        public string receipt_number { get; set; } = string.Empty;
    }

    private class VerifyPaymentRow
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid? order_id { get; set; }
        public Guid payment_id { get; set; }
    }

    private class FulfilmentRow
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid order_id { get; set; }
        public string fulfilment_status { get; set; } = string.Empty;
    }
}
