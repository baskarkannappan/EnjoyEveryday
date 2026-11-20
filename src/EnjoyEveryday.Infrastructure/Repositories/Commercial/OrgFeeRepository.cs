using System.Data;
using System.Text.Json;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class OrgFeeRepository : IOrgFeeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private readonly IDbConnectionFactory _connectionFactory;

    public OrgFeeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<OrgFeePlan>> GetFeePlansAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<OrgFeePlan>(
            "fn_get_org_fee_plans",
            new { p_tenant_id = tenantId, p_organization_id = organizationId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<OrgFeePlan> UpsertFeePlanAsync(OrgFeePlan plan, CancellationToken ct = default)
    {
        if (plan.Id == Guid.Empty) plan.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var saved = await conn.QueryFirstAsync<OrgFeePlan>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_org_fee_plan(@p_id, @p_tenant_id, @p_organization_id, @p_name, @p_description, @p_fee_category, @p_billing_frequency, @p_currency, @p_is_active, @p_allow_partial_payment, @p_minimum_partial_payment, @p_allow_parent_cash_declaration, @p_created_by)",
                new
                {
                    p_id = plan.Id,
                    p_tenant_id = plan.TenantId,
                    p_organization_id = plan.OrganizationId,
                    p_name = plan.Name,
                    p_description = plan.Description,
                    p_fee_category = plan.FeeCategory,
                    p_billing_frequency = plan.BillingFrequency,
                    p_currency = plan.Currency,
                    p_is_active = plan.IsActive,
                    p_allow_partial_payment = plan.AllowPartialPayment,
                    p_minimum_partial_payment = plan.MinimumPartialPayment,
                    p_allow_parent_cash_declaration = plan.AllowParentCashDeclaration,
                    p_created_by = plan.CreatedBy
                },
                cancellationToken: ct));
        return saved;
    }

    public async Task<bool> SetFeePlanActiveAsync(Guid tenantId, Guid organizationId, Guid planId, bool isActive, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_set_org_fee_plan_active",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_plan_id = planId, p_is_active = isActive },
            cancellationToken: ct);
    }

    public async Task<List<OrgTariffRule>> GetTariffRulesAsync(Guid tenantId, Guid organizationId, Guid? feePlanId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<OrgTariffRule>(
            "fn_get_org_tariff_rules",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_fee_plan_id = feePlanId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<OrgTariffRule> UpsertTariffRuleAsync(OrgTariffRule rule, CancellationToken ct = default)
    {
        if (rule.Id == Guid.Empty) rule.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var saved = await conn.QueryFirstAsync<OrgTariffRule>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_org_tariff_rule(@p_id, @p_tenant_id, @p_organization_id, @p_fee_plan_id, @p_scope_type, @p_branch_id, @p_classroom_id, @p_child_id, @p_location_tier, @p_child_category, @p_amount, @p_currency, @p_effective_date, @p_expiry_date, @p_is_active, @p_notes, @p_created_by)",
                new
                {
                    p_id = rule.Id,
                    p_tenant_id = rule.TenantId,
                    p_organization_id = rule.OrganizationId,
                    p_fee_plan_id = rule.FeePlanId,
                    p_scope_type = rule.ScopeType,
                    p_branch_id = rule.BranchId,
                    p_classroom_id = rule.ClassroomId,
                    p_child_id = rule.ChildId,
                    p_location_tier = rule.LocationTier,
                    p_child_category = rule.ChildCategory,
                    p_amount = rule.Amount,
                    p_currency = rule.Currency,
                    p_effective_date = DateOnly.FromDateTime(rule.EffectiveDate),
                    p_expiry_date = rule.ExpiryDate.HasValue ? DateOnly.FromDateTime(rule.ExpiryDate.Value) : (DateOnly?)null,
                    p_is_active = rule.IsActive,
                    p_notes = rule.Notes,
                    p_created_by = rule.CreatedBy
                },
                cancellationToken: ct));
        return saved;
    }

    public async Task<bool> SetTariffRuleActiveAsync(Guid tenantId, Guid organizationId, Guid ruleId, bool isActive, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_set_org_tariff_rule_active",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_rule_id = ruleId, p_is_active = isActive },
            cancellationToken: ct);
    }

    public async Task<ResolvedTariffResult?> ResolveChildTariffAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId,
        Guid? classroomId,
        Guid? childId,
        string? locationTier,
        string? billingFrequency,
        DateTime asOfDate,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<ResolvedTariffResult>(
            "fn_resolve_child_tariff",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId,
                p_branch_id = branchId,
                p_classroom_id = classroomId,
                p_child_id = childId,
                p_location_tier = locationTier,
                p_billing_frequency = billingFrequency,
                p_as_of_date = DateOnly.FromDateTime(asOfDate)
            },
            cancellationToken: ct);
    }

    public async Task<FamilyBillingContract> UpsertFamilyContractAsync(FamilyBillingContract contract, CancellationToken ct = default)
    {
        if (contract.Id == Guid.Empty) contract.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstAsync<FamilyBillingContract>(
            new CommandDefinition(
                "SELECT * FROM public.fn_upsert_family_billing_contract_v2(@p_id, @p_tenant_id, @p_organization_id, @p_branch_id, @p_application_id, @p_child_id, @p_parent_name, @p_parent_email, @p_schedule_type, @p_billing_cycle, @p_base_rate, @p_discount_percent, @p_effective_rate, @p_deposit_held, @p_status, @p_start_date, @p_end_date, @p_auto_invoice, @p_fee_method, @p_fee_plan_id, @p_tariff_rule_id, @p_fee_period, @p_payment_arrangement, @p_currency, @p_remarks, @p_allow_parent_cash_declaration, @p_minimum_partial_payment)",
                new
                {
                    p_id = contract.Id,
                    p_tenant_id = contract.TenantId,
                    p_organization_id = contract.OrganizationId,
                    p_branch_id = contract.BranchId,
                    p_application_id = contract.ApplicationId,
                    p_child_id = contract.ChildId,
                    p_parent_name = contract.ParentName,
                    p_parent_email = contract.ParentEmail,
                    p_schedule_type = contract.ScheduleType,
                    p_billing_cycle = contract.BillingCycle,
                    p_base_rate = contract.BaseRate,
                    p_discount_percent = contract.DiscountPercent,
                    p_effective_rate = contract.EffectiveRate,
                    p_deposit_held = contract.DepositHeld,
                    p_status = contract.Status,
                    p_start_date = DateOnly.FromDateTime(contract.StartDate),
                    p_end_date = contract.EndDate.HasValue ? DateOnly.FromDateTime(contract.EndDate.Value) : (DateOnly?)null,
                    p_auto_invoice = contract.AutoInvoice,
                    p_fee_method = contract.FeeMethod,
                    p_fee_plan_id = contract.FeePlanId,
                    p_tariff_rule_id = contract.TariffRuleId,
                    p_fee_period = contract.FeePeriod,
                    p_payment_arrangement = contract.PaymentArrangement,
                    p_currency = contract.Currency,
                    p_remarks = contract.Remarks,
                    p_allow_parent_cash_declaration = contract.AllowParentCashDeclaration,
                    p_minimum_partial_payment = contract.MinimumPartialPayment
                },
                cancellationToken: ct));
    }

    public async Task<FamilyBillingContract?> GetFamilyContractByIdAsync(Guid tenantId, Guid contractId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<FamilyBillingContract>(
            "fn_get_family_contract_by_id",
            new { p_tenant_id = tenantId, p_contract_id = contractId },
            cancellationToken: ct);
    }

    public async Task<List<FamilyBillingContract>> GetFamilyContractsForChildAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<FamilyBillingContract>(
            "fn_get_family_contracts_for_child",
            new { p_tenant_id = tenantId, p_child_id = childId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<ChildFeeAdjustment> RecordChildFeeAdjustmentAsync(ChildFeeAdjustment adjustment, CancellationToken ct = default)
    {
        if (adjustment.Id == Guid.Empty) adjustment.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstAsync<ChildFeeAdjustment>(
            new CommandDefinition(
                "SELECT * FROM public.fn_insert_child_fee_adjustment(@p_id, @p_tenant_id, @p_organization_id, @p_branch_id, @p_child_id, @p_contract_id, @p_invoice_id, @p_adjustment_type, @p_previous_amount, @p_new_amount, @p_adjustment_delta, @p_reason, @p_effective_date, @p_adjusted_by_user_id, @p_adjusted_by_email)",
                new
                {
                    p_id = adjustment.Id,
                    p_tenant_id = adjustment.TenantId,
                    p_organization_id = adjustment.OrganizationId,
                    p_branch_id = adjustment.BranchId,
                    p_child_id = adjustment.ChildId,
                    p_contract_id = adjustment.ContractId,
                    p_invoice_id = adjustment.InvoiceId,
                    p_adjustment_type = adjustment.AdjustmentType,
                    p_previous_amount = adjustment.PreviousAmount,
                    p_new_amount = adjustment.NewAmount,
                    p_adjustment_delta = adjustment.AdjustmentDelta,
                    p_reason = adjustment.Reason,
                    p_effective_date = DateOnly.FromDateTime(adjustment.EffectiveDate),
                    p_adjusted_by_user_id = adjustment.AdjustedByUserId,
                    p_adjusted_by_email = adjustment.AdjustedByEmail
                },
                cancellationToken: ct));
    }

    public async Task<List<ChildFeeAdjustment>> GetChildFeeAdjustmentsAsync(Guid tenantId, Guid organizationId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<ChildFeeAdjustment>(
            "fn_get_child_fee_adjustments",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_child_id = childId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<Invoice> CreateTuitionInvoiceAsync(Invoice invoice, List<InvoiceInstallment>? installments = null, CancellationToken ct = default)
    {
        if (invoice.Id == Guid.Empty) invoice.Id = Guid.NewGuid();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteProcedureAsync(
            "sp_insert_tuition_invoice",
            new
            {
                p_id = invoice.Id,
                p_tenant_id = invoice.TenantId,
                p_organization_id = invoice.OrganizationId,
                p_branch_id = invoice.BranchId,
                p_child_id = invoice.ChildId,
                p_contract_id = invoice.ContractId,
                p_invoice_number = invoice.InvoiceNumber,
                p_billing_period = invoice.BillingPeriod,
                p_subtotal = invoice.Subtotal,
                p_discount_amount = invoice.DiscountAmount,
                p_tax_amount = invoice.TaxAmount,
                p_total_amount = invoice.TotalAmount,
                p_currency = invoice.Currency,
                p_status = invoice.Status.ToString(),
                p_fee_method = invoice.FeeMethod,
                p_fee_plan_name = invoice.FeePlanName,
                p_issue_date = invoice.IssueDate,
                p_due_date = invoice.DueDate,
                p_created_by = invoice.CreatedBy,
                p_description = invoice.Description,
                p_notes = invoice.Notes,
                p_payment_instructions = invoice.PaymentInstructions,
                p_payment_instructions_snapshot = invoice.PaymentInstructionsSnapshot,
                p_billing_contact_email = invoice.BillingContactEmail
            },
            transaction: tx,
            cancellationToken: ct);

        foreach (var item in invoice.LineItems)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            await conn.ExecuteProcedureAsync(
                "sp_insert_invoice_line_item",
                new
                {
                    p_id = item.Id,
                    p_invoice_id = invoice.Id,
                    p_line_number = item.LineNumber,
                    p_description = item.Description,
                    p_quantity = item.Quantity,
                    p_unit_price = item.UnitPrice,
                    p_line_total = item.LineTotal,
                    p_category = item.Category,
                    p_created_at = item.CreatedAt
                },
                transaction: tx,
                cancellationToken: ct);
        }

        if (installments != null)
        {
            foreach (var inst in installments)
            {
                if (inst.Id == Guid.Empty) inst.Id = Guid.NewGuid();
                await conn.ExecuteProcedureAsync(
                    "sp_insert_invoice_installment",
                    new
                    {
                        p_id = inst.Id,
                        p_invoice_id = invoice.Id,
                        p_installment_number = inst.InstallmentNumber,
                        p_due_date = DateOnly.FromDateTime(inst.DueDate),
                        p_amount_due = inst.AmountDue,
                        p_allow_custom_amount = inst.AllowCustomAmount,
                        p_allow_multiple_transactions = inst.AllowMultipleTransactions,
                        p_notes = inst.Notes
                    },
                    transaction: tx,
                    cancellationToken: ct);
            }
        }

        tx.Commit();
        return invoice;
    }

    public async Task<List<Invoice>> GetOrgTuitionInvoicesAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<TuitionInvoiceRow>(
            "fn_get_org_tuition_invoices",
            new { p_tenant_id = tenantId, p_organization_id = organizationId, p_branch_id = branchId },
            cancellationToken: ct);
        return rows.Select(MapInvoice).ToList();
    }

    public async Task<List<Invoice>> GetChildInvoicesAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<TuitionInvoiceRow>(
            "fn_get_child_invoices",
            new { p_tenant_id = tenantId, p_child_id = childId },
            cancellationToken: ct);
        return rows.Select(MapInvoice).ToList();
    }

    public async Task<List<InvoiceInstallment>> GetInvoiceInstallmentsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<InvoiceInstallment>(
            "fn_get_invoice_installments",
            new { p_invoice_id = invoiceId },
            cancellationToken: ct);
        return rows.ToList();
    }

    public async Task<bool> PublishTuitionInvoiceAsync(Guid invoiceId, string publishedBy, string? paymentInstructions, string? paymentInstructionsSnapshotJson, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_publish_tuition_invoice",
            new
            {
                p_invoice_id = invoiceId,
                p_published_by = publishedBy,
                p_payment_instructions = paymentInstructions,
                p_payment_instructions_snapshot = paymentInstructionsSnapshotJson
            },
            cancellationToken: ct);
    }

    public async Task<bool> ApplyInvoiceAdjustmentAsync(Guid invoiceId, decimal adjustmentDelta, string newStatus, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_apply_invoice_adjustment",
            new { p_invoice_id = invoiceId, p_adjustment_delta = adjustmentDelta, p_new_status = newStatus },
            cancellationToken: ct);
    }

    public async Task<bool> CancelInvoiceAsync(Guid invoiceId, string cancelledBy, string reason, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<bool>(
            "fn_cancel_invoice",
            new { p_invoice_id = invoiceId, p_cancelled_by = cancelledBy, p_reason = reason },
            cancellationToken: ct);
    }

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
