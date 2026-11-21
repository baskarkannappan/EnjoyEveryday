using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Repositories;

public partial class BillingRepository
{
    public async Task<OrganizationBillingSettings?> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<BillingSettingsRow>(
            "fn_get_organization_billing_settings",
            new { OrganizationId = organizationId },
            cancellationToken: ct);

        if (row == null) return null;

        return new OrganizationBillingSettings
        {
            Id = row.Id,
            TenantId = row.TenantId,
            OrganizationId = row.OrganizationId,
            Mode = Enum.TryParse<BillingMode>(row.Mode, true, out var m) ? m : BillingMode.Manual,
            Frequency = Enum.TryParse<BillingFrequency>(row.Frequency, true, out var f) ? f : BillingFrequency.Monthly,
            EffectiveDate = row.EffectiveDate,
            NextBillingDate = row.NextBillingDate,
            AutoChargeEnabled = row.AutoChargeEnabled,
            Notes = row.Notes,
            UpdatedBy = row.UpdatedBy,
            UpdatedAt = row.UpdatedAt
        };
    }

    public async Task<bool> UpsertBillingSettingsAsync(OrganizationBillingSettings settings, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        if (settings.Id == Guid.Empty) settings.Id = Guid.NewGuid();
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await conn.ExecuteProcedureAsync("sp_upsert_organization_billing_settings", new
        {
            settings.Id,
            settings.TenantId,
            settings.OrganizationId,
            Mode = settings.Mode.ToString(),
            Frequency = settings.Frequency.ToString(),
            EffectiveDate = ToUtc(settings.EffectiveDate),
            NextBillingDate = ToUtc(settings.NextBillingDate),
            settings.AutoChargeEnabled,
            settings.Notes,
            settings.UpdatedBy,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: ct);

        return true;
    }

    public async Task<List<OrganizationBillingSettings>> GetOrganizationsDueForAutomaticBillingAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<BillingSettingsRow>(
            "fn_get_organizations_due_for_automatic_billing",
            new { AsOfDate = ToUtc(asOfDate) },
            cancellationToken: ct);

        return rows.Select(row => new OrganizationBillingSettings
        {
            Id = row.Id,
            TenantId = row.TenantId,
            OrganizationId = row.OrganizationId,
            Mode = BillingMode.Automatic,
            Frequency = Enum.TryParse<BillingFrequency>(row.Frequency, true, out var f) ? f : BillingFrequency.Monthly,
            EffectiveDate = row.EffectiveDate,
            NextBillingDate = row.NextBillingDate,
            AutoChargeEnabled = row.AutoChargeEnabled,
            Notes = row.Notes,
            UpdatedBy = row.UpdatedBy,
            UpdatedAt = row.UpdatedAt
        }).ToList();
    }

    public async Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid tenantId, Guid? branchId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await conn.QueryFunctionAsync<FamilyBillingContract>(
            "fn_get_active_family_contracts",
            new { p_tenant_id = tenantId, p_branch_id = branchId },
            cancellationToken: ct);
        return rows.ToList();
    }

    private static async Task<List<InvoiceLineItem>> GetLineItemsForInvoiceInternalAsync(IDbConnection conn, Guid invoiceId)
    {
        return (await conn.QueryFunctionAsync<InvoiceLineItem>(
            "fn_get_invoice_line_items",
            new { InvoiceId = invoiceId })).ToList();
    }

    private static async Task<List<Payment>> GetPaymentsForInvoiceInternalAsync(IDbConnection conn, Guid invoiceId)
    {
        return (await conn.QueryFunctionAsync<Payment>(
            "fn_get_payments_for_invoice",
            new { InvoiceId = invoiceId })).ToList();
    }

    private static Invoice MapToInvoice(InvoiceRow row)
    {
        var inv = new Invoice
        {
            Id = row.Id,
            TenantId = row.TenantId,
            OrganizationId = row.OrganizationId,
            BranchId = row.BranchId,
            ChildId = row.ChildId,
            ContractId = row.ContractId,
            InvoiceNumber = row.InvoiceNumber,
            BillingPeriod = row.BillingPeriod,
            Subtotal = row.Subtotal,
            DiscountAmount = row.DiscountAmount,
            TaxAmount = row.TaxAmount,
            TotalAmount = row.TotalAmount,
            AdjustmentAmount = row.AdjustmentAmount,
            AmountPaid = row.AmountPaid,
            PendingVerificationAmount = row.PendingVerificationAmount,
            Currency = row.Currency,
            FeeMethod = row.FeeMethod,
            FeePlanName = row.FeePlanName,
            IssueDate = row.IssueDate,
            DueDate = row.DueDate,
            PaidAt = row.PaidAt,
            PublishedAt = row.PublishedAt,
            PublishedBy = row.PublishedBy,
            VoidedAt = row.VoidedAt,
            VoidedBy = row.VoidedBy,
            VoidReason = row.VoidReason,
            CreatedBy = row.CreatedBy,
            Description = row.Description,
            Notes = row.Notes,
            PaymentInstructions = row.PaymentInstructions,
            PaymentInstructionsSnapshot = row.PaymentInstructionsSnapshot,
            BillingContactEmail = row.BillingContactEmail,
            DocumentStorageKey = row.DocumentStorageKey,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };

        if (Enum.TryParse<InvoiceStatus>(row.Status, true, out var parsed))
        {
            inv.Status = parsed;
        }

        return inv;
    }

    private class InvoiceRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ChildId { get; set; }
        public Guid? ContractId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? BillingPeriod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal PendingVerificationAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Draft";
        public string? FeeMethod { get; set; }
        public string? FeePlanName { get; set; }
        public DateTimeOffset IssueDate { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public string? PublishedBy { get; set; }
        public DateTimeOffset? VoidedAt { get; set; }
        public string? VoidedBy { get; set; }
        public string? VoidReason { get; set; }
        public string? CreatedBy { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public string? PaymentInstructions { get; set; }
        public string? PaymentInstructionsSnapshot { get; set; }
        public string? BillingContactEmail { get; set; }
        public string? DocumentStorageKey { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private class BillingSettingsRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Mode { get; set; } = "Manual";
        public string Frequency { get; set; } = "Monthly";
        public DateTimeOffset EffectiveDate { get; set; }
        public DateTimeOffset? NextBillingDate { get; set; }
        public bool AutoChargeEnabled { get; set; }
        public string? Notes { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public async Task<bool> HasWebhookEventBeenProcessedAsync(string eventId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return false;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM public.razorpay_webhook_events WHERE event_id = @EventId;";
        var count = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, new Dapper.CommandDefinition(sql, new { EventId = eventId.Trim() }, cancellationToken: ct));
        return count > 0;
    }

    public async Task RecordWebhookEventAsync(string eventId, string eventType, Guid? invoiceId, string? paymentId, long? amountPaise, string? currency, string status, string? payloadJson, string? errorMessage = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return;
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            INSERT INTO public.razorpay_webhook_events (id, event_id, event_type, invoice_id, payment_id, amount_in_paise, currency, status, payload, received_at, error_message)
            VALUES (gen_random_uuid(), @EventId, @EventType, @InvoiceId, @PaymentId, @AmountPaise, @Currency, @Status, @Payload::jsonb, NOW(), @ErrorMessage)
            ON CONFLICT (event_id) DO UPDATE 
            SET status = EXCLUDED.status, error_message = EXCLUDED.error_message;";
        await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(sql, new
        {
            EventId = eventId.Trim(),
            EventType = eventType.Trim(),
            InvoiceId = invoiceId,
            PaymentId = paymentId,
            AmountPaise = amountPaise,
            Currency = currency ?? "INR",
            Status = status,
            Payload = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson,
            ErrorMessage = errorMessage
        }, cancellationToken: ct));
    }

    private static DateTimeOffset ToUtc(DateTimeOffset dto) => dto.ToUniversalTime();
    private static DateTimeOffset? ToUtc(DateTimeOffset? dto) => dto?.ToUniversalTime();
}
