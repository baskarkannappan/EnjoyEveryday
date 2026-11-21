using System.Data;
using System.Text.Json;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public partial class OrgFeeRepository : IOrgFeeRepository
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

}
