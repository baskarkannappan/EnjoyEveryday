using System.Data;
using System.Text.Json;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public partial class OrgFeeRepository
{
    public async Task<OrgBankAccount?> GetBankAccountAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<OrgBankAccount>(
            "fn_get_org_bank_account",
            new { p_tenant_id = tenantId, p_organization_id = organizationId },
            cancellationToken: ct);
    }

    public async Task<OrgBankAccount> UpsertBankAccountAsync(OrgBankAccount account, CancellationToken ct = default)
    {
        if (account.Id == Guid.Empty) account.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstAsync<OrgBankAccount>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_org_bank_account(@p_id, @p_tenant_id, @p_organization_id, @p_account_holder_name, @p_bank_name, @p_branch_name, @p_account_number, @p_ifsc_code, @p_micr_code, @p_account_type, @p_additional_instructions, @p_allow_parent_cash_declaration, @p_require_payment_proof, @p_updated_by)",
                new
                {
                    p_id = account.Id,
                    p_tenant_id = account.TenantId,
                    p_organization_id = account.OrganizationId,
                    p_account_holder_name = account.AccountHolderName,
                    p_bank_name = account.BankName,
                    p_branch_name = account.BranchName,
                    p_account_number = account.AccountNumber,
                    p_ifsc_code = account.IfscCode,
                    p_micr_code = account.MicrCode,
                    p_account_type = account.AccountType,
                    p_additional_instructions = account.AdditionalInstructions,
                    p_allow_parent_cash_declaration = account.AllowParentCashDeclaration,
                    p_require_payment_proof = account.RequirePaymentProof,
                    p_updated_by = account.UpdatedBy
                },
                cancellationToken: ct));
    }

    public async Task<List<OrgPaymentQrCode>> GetQrCodesAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<OrgPaymentQrCode>(
            "fn_get_org_qr_codes",
            new { p_tenant_id = tenantId, p_organization_id = organizationId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<OrgPaymentQrCode> UpsertQrCodeAsync(OrgPaymentQrCode qrCode, CancellationToken ct = default)
    {
        if (qrCode.Id == Guid.Empty) qrCode.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstAsync<OrgPaymentQrCode>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_org_qr_code(@p_id, @p_tenant_id, @p_organization_id, @p_slot_index, @p_display_name, @p_payment_purpose, @p_qr_image_data_uri, @p_instructions, @p_is_active, @p_updated_by)",
                new
                {
                    p_id = qrCode.Id,
                    p_tenant_id = qrCode.TenantId,
                    p_organization_id = qrCode.OrganizationId,
                    p_slot_index = qrCode.SlotIndex,
                    p_display_name = qrCode.DisplayName,
                    p_payment_purpose = qrCode.PaymentPurpose,
                    p_qr_image_data_uri = qrCode.QrImageDataUri,
                    p_instructions = qrCode.Instructions,
                    p_is_active = qrCode.IsActive,
                    p_updated_by = qrCode.UpdatedBy
                },
                cancellationToken: ct));
    }

    public async Task<bool> DeleteQrCodeAsync(Guid tenantId, Guid organizationId, int slotIndex, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_delete_org_qr_code",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_slot_index = slotIndex },
            cancellationToken: ct);
    }

    public async Task<Payment> SubmitPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_submit_parent_payment",
            new
            {
                p_id = payment.Id,
                p_tenant_id = payment.TenantId,
                p_organization_id = payment.OrganizationId,
                p_branch_id = payment.BranchId,
                p_child_id = payment.ChildId,
                p_invoice_id = payment.InvoiceId,
                p_invoice_number = payment.InvoiceNumber,
                p_payment_reference = payment.PaymentReference,
                p_amount = payment.Amount,
                p_currency = payment.Currency,
                p_method = payment.Method,
                p_transaction_id = payment.TransactionId,
                p_notes = payment.Notes,
                p_submission_source = payment.SubmissionSource,
                p_proof_storage_key = payment.ProofStorageKey,
                p_recorded_by_user_id = payment.RecordedByUserId,
                p_recorded_by_email = payment.RecordedByEmail,
                p_payment_date = payment.PaymentDate
            },
            cancellationToken: ct);
        return payment;
    }

    public async Task<List<Payment>> GetOrgPaymentsQueueAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, string? status = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<Payment>(
            "fn_get_org_payments_queue",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_branch_id = branchId, p_status = status },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<(bool Success, string Message, Guid? InvoiceId, decimal NewAmountPaid, decimal NewBalanceDue)> VerifySubmittedPaymentAsync(
        Guid paymentId,
        string verifiedBy,
        string? remarks,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<VerifyPaymentRow>(
            new CommandDefinition(
                "SELECT * FROM public.fn_verify_submitted_payment(@p_payment_id, @p_verified_by, @p_remarks)",
                new { p_payment_id = paymentId, p_verified_by = verifiedBy, p_remarks = remarks },
                cancellationToken: ct));
        return (row.Success, row.Message, row.InvoiceId, row.NewAmountPaid, row.NewBalanceDue);
    }

    public async Task<(bool Success, string Message, Guid? InvoiceId)> RejectSubmittedPaymentAsync(
        Guid paymentId,
        string rejectedBy,
        string reason,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<RejectPaymentRow>(
            new CommandDefinition(
                "SELECT * FROM public.fn_reject_submitted_payment(@p_payment_id, @p_rejected_by, @p_reason)",
                new { p_payment_id = paymentId, p_rejected_by = rejectedBy, p_reason = reason },
                cancellationToken: ct));
        return (row.Success, row.Message, row.InvoiceId);
    }

    public async Task<OrgFeeDashboardSummary> GetDashboardSummaryAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<OrgFeeDashboardSummary>(
            "fn_get_org_fee_dashboard",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_branch_id = branchId },
            cancellationToken: ct);
        return row ?? new OrgFeeDashboardSummary();
    }

    public async Task<OrgPaymentGatewaySettings?> GetPaymentGatewaySettingsAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<OrgPaymentGatewaySettings>(
            "fn_get_org_payment_gateway_settings",
            new { p_tenant_id = tenantId, p_organization_id = organizationId },
            cancellationToken: ct);
    }

    public async Task<OrgPaymentGatewaySettings> UpsertPaymentGatewaySettingsAsync(OrgPaymentGatewaySettings settings, CancellationToken ct = default)
    {
        if (settings.Id == Guid.Empty) settings.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstAsync<OrgPaymentGatewaySettings>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_org_payment_gateway_settings(@p_id, @p_tenant_id, @p_organization_id, @p_has_razorpay_account, @p_key_id, @p_encrypted_key_secret, @p_encrypted_webhook_secret, @p_environment, @p_status, @p_is_active, @p_last_validated_at, @p_validation_message, @p_configured_by)",
                new
                {
                    p_id = settings.Id,
                    p_tenant_id = settings.TenantId,
                    p_organization_id = settings.OrganizationId,
                    p_has_razorpay_account = settings.HasRazorpayAccount,
                    p_key_id = settings.KeyId,
                    p_encrypted_key_secret = settings.EncryptedKeySecret,
                    p_encrypted_webhook_secret = settings.EncryptedWebhookSecret,
                    p_environment = settings.Environment,
                    p_status = settings.Status,
                    p_is_active = settings.IsActive,
                    p_last_validated_at = settings.LastValidatedAt,
                    p_validation_message = settings.ValidationMessage,
                    p_configured_by = settings.ConfiguredBy
                },
                cancellationToken: ct));
    }

    public async Task<(bool Success, string Message, Guid? PaymentId)> RecordOrgRazorpayPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<RecordRazorpayRow>(
            new CommandDefinition(
                "SELECT * FROM public.fn_record_org_razorpay_payment(@p_id, @p_tenant_id, @p_organization_id, @p_branch_id, @p_child_id, @p_invoice_id, @p_invoice_number, @p_amount, @p_currency, @p_provider_order_id, @p_provider_payment_id, @p_provider_signature, @p_recorded_by_user_id, @p_recorded_by_email, @p_notes)",
                new
                {
                    p_id = payment.Id,
                    p_tenant_id = payment.TenantId,
                    p_organization_id = payment.OrganizationId,
                    p_branch_id = payment.BranchId,
                    p_child_id = payment.ChildId,
                    p_invoice_id = payment.InvoiceId,
                    p_invoice_number = payment.InvoiceNumber,
                    p_amount = payment.Amount,
                    p_currency = payment.Currency,
                    p_provider_order_id = payment.ProviderOrderId,
                    p_provider_payment_id = payment.ProviderPaymentId,
                    p_provider_signature = payment.ProviderSignature,
                    p_recorded_by_user_id = payment.RecordedByUserId,
                    p_recorded_by_email = payment.RecordedByEmail,
                    p_notes = payment.Notes
                },
                cancellationToken: ct));
        return (row.Success, row.Message, row.PaymentId);
    }

    public async Task<List<ChildFeeSettlementReportItem>> GetFeeSettlementReportAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        Guid? childId = null,
        string? billingPeriod = null,
        string? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = (await conn.QueryFunctionAsync<ChildFeeSettlementReportItem>(
            "fn_get_org_fee_settlement_report",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId,
                p_branch_id = branchId,
                p_classroom_id = classroomId,
                p_child_id = childId,
                p_billing_period = string.IsNullOrWhiteSpace(billingPeriod) ? null : billingPeriod.Trim(),
                p_status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                p_search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
            },
            cancellationToken: ct)).ToList();

        foreach (var item in rows)
        {
            if (!string.IsNullOrWhiteSpace(item.InvoicesJson) && item.InvoicesJson != "[]")
            {
                try
                {
                    item.Invoices = JsonSerializer.Deserialize<List<FeeReportInvoiceDto>>(item.InvoicesJson, JsonOptions) ?? new();
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(item.PaymentsJson) && item.PaymentsJson != "[]")
            {
                try
                {
                    item.Payments = JsonSerializer.Deserialize<List<FeeReportPaymentDto>>(item.PaymentsJson, JsonOptions) ?? new();
                }
                catch { }
            }
        }

        return rows;
    }

    private sealed class RecordRazorpayRow
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? PaymentId { get; set; }
    }

    private static Invoice MapInvoice(TuitionInvoiceRow row)
    {
        var parsedStatus = Enum.TryParse<InvoiceStatus>(row.Status, true, out var st) ? st : InvoiceStatus.Issued;
        return new Invoice
        {
            Id = row.Id,
            TenantId = row.TenantId,
            OrganizationId = row.OrganizationId,
            BranchId = row.BranchId,
            ChildId = row.ChildId,
            ContractId = row.ContractId,
            ChildName = row.ChildName,
            BranchName = row.BranchName,
            InvoiceNumber = row.InvoiceNumber,
            BillingPeriod = row.BillingPeriod,
            Subtotal = row.Subtotal,
            DiscountAmount = row.DiscountAmount,
            TaxAmount = row.TaxAmount,
            TotalAmount = row.TotalAmount,
            AmountPaid = row.AmountPaid,
            AdjustmentAmount = row.AdjustmentAmount,
            PendingVerificationAmount = row.PendingVerificationAmount,
            Currency = row.Currency,
            Status = parsedStatus,
            FeeMethod = row.FeeMethod,
            FeePlanName = row.FeePlanName,
            IssueDate = row.IssueDate,
            DueDate = row.DueDate,
            PaidAt = row.PaidAt,
            Description = row.Description,
            Notes = row.Notes,
            PaymentInstructions = row.PaymentInstructions,
            PaymentInstructionsSnapshot = row.PaymentInstructionsSnapshot,
            BillingContactEmail = row.BillingContactEmail,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }

    private sealed class TuitionInvoiceRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ChildId { get; set; }
        public Guid? ContractId { get; set; }
        public string? ChildName { get; set; }
        public string? BranchName { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? BillingPeriod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public decimal PendingVerificationAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Issued";
        public string? FeeMethod { get; set; }
        public string? FeePlanName { get; set; }
        public DateTimeOffset IssueDate { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? PaymentInstructions { get; set; }
        public string? PaymentInstructionsSnapshot { get; set; }
        public string? BillingContactEmail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private sealed class VerifyPaymentRow
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? InvoiceId { get; set; }
        public decimal NewAmountPaid { get; set; }
        public decimal NewBalanceDue { get; set; }
    }

    private sealed class RejectPaymentRow
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? InvoiceId { get; set; }
    }
}
