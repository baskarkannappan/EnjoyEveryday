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

public class BillingRepository : IBillingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<BillingRepository> _logger;

    public BillingRepository(IDbConnectionFactory connectionFactory, ILogger<BillingRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<Invoice>> GetInvoicesForOrganizationAsync(Guid tenantId, Guid organizationId, bool includeDrafts, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var invoices = (await conn.QueryFunctionAsync<InvoiceRow>(
            "fn_get_invoices_for_organization",
            new { TenantId = tenantId, OrganizationId = organizationId, IncludeDrafts = includeDrafts },
            cancellationToken: ct)).ToList();

        if (invoices.Count == 0) return new List<Invoice>();

        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var lineItemsLookup = (await conn.QueryFunctionAsync<InvoiceLineItem>(
            "fn_get_invoice_line_items_by_ids",
            new { InvoiceIds = invoiceIds },
            cancellationToken: ct)).ToLookup(x => x.InvoiceId);

        var paymentsLookup = (await conn.QueryFunctionAsync<Payment>(
            "fn_get_payments_by_invoice_ids",
            new { InvoiceIds = invoiceIds },
            cancellationToken: ct))
            .Where(p => p.InvoiceId.HasValue)
            .ToLookup(p => p.InvoiceId!.Value);

        var result = new List<Invoice>(invoices.Count);
        foreach (var row in invoices)
        {
            var inv = MapToInvoice(row);
            inv.LineItems = lineItemsLookup[inv.Id].ToList();
            inv.Payments = paymentsLookup[inv.Id].ToList();
            result.Add(inv);
        }

        return result;
    }

    public async Task<List<Invoice>> GetAllInvoicesAsync(Guid? tenantId, Guid? organizationId, InvoiceStatus? status, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var statusString = status?.ToString();
        var invoices = (await conn.QueryFunctionAsync<InvoiceRow>(
            "fn_get_all_invoices",
            new { TenantId = tenantId, OrganizationId = organizationId, Status = statusString },
            cancellationToken: ct)).ToList();

        if (invoices.Count == 0) return new List<Invoice>();

        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var lineItemsLookup = (await conn.QueryFunctionAsync<InvoiceLineItem>(
            "fn_get_invoice_line_items_by_ids",
            new { InvoiceIds = invoiceIds },
            cancellationToken: ct)).ToLookup(x => x.InvoiceId);

        var paymentsLookup = (await conn.QueryFunctionAsync<Payment>(
            "fn_get_payments_by_invoice_ids",
            new { InvoiceIds = invoiceIds },
            cancellationToken: ct))
            .Where(p => p.InvoiceId.HasValue)
            .ToLookup(p => p.InvoiceId!.Value);

        var result = new List<Invoice>(invoices.Count);
        foreach (var row in invoices)
        {
            var inv = MapToInvoice(row);
            inv.LineItems = lineItemsLookup[inv.Id].ToList();
            inv.Payments = paymentsLookup[inv.Id].ToList();
            result.Add(inv);
        }

        return result;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tId = tenantId == Guid.Empty ? (Guid?)null : tenantId;
        var row = await conn.QuerySingleFunctionAsync<InvoiceRow>(
            "fn_get_invoice_by_id",
            new { TenantId = tId, InvoiceId = invoiceId },
            cancellationToken: ct);

        if (row == null) return null;

        var invoice = MapToInvoice(row);
        invoice.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, invoice.Id);
        invoice.Payments = await GetPaymentsForInvoiceInternalAsync(conn, invoice.Id);
        return invoice;
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleFunctionAsync<InvoiceRow>(
            "fn_get_invoice_by_number",
            new { InvoiceNumber = invoiceNumber.Trim() },
            cancellationToken: ct);

        if (row == null) return null;

        var invoice = MapToInvoice(row);
        invoice.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, invoice.Id);
        invoice.Payments = await GetPaymentsForInvoiceInternalAsync(conn, invoice.Id);
        return invoice;
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            await conn.ExecuteProcedureAsync("sp_insert_invoice", new
            {
                invoice.Id,
                invoice.TenantId,
                invoice.OrganizationId,
                invoice.InvoiceNumber,
                invoice.BillingPeriod,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.Currency,
                Status = invoice.Status.ToString(),
                IssueDate = ToUtc(invoice.IssueDate),
                DueDate = ToUtc(invoice.DueDate),
                PaidAt = ToUtc(invoice.PaidAt),
                PublishedAt = ToUtc(invoice.PublishedAt),
                invoice.PublishedBy,
                VoidedAt = ToUtc(invoice.VoidedAt),
                invoice.VoidedBy,
                invoice.VoidReason,
                invoice.CreatedBy,
                invoice.Description,
                invoice.Notes,
                invoice.PaymentInstructions,
                invoice.BillingContactEmail,
                invoice.DocumentStorageKey,
                CreatedAt = ToUtc(invoice.CreatedAt),
                UpdatedAt = ToUtc(invoice.UpdatedAt)
            }, transaction: trans, cancellationToken: ct);

            if (invoice.LineItems.Any())
            {
                foreach (var item in invoice.LineItems)
                {
                    if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                    item.InvoiceId = invoice.Id;
                    await conn.ExecuteProcedureAsync("sp_insert_invoice_line_item", new
                    {
                        item.Id,
                        item.InvoiceId,
                        item.LineNumber,
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,
                        LineTotal = item.LineTotal,
                        item.Category,
                        CreatedAt = ToUtc(item.CreatedAt)
                    }, transaction: trans, cancellationToken: ct);
                }
            }

            trans.Commit();
            return invoice;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            await conn.ExecuteProcedureAsync("sp_update_invoice", new
            {
                invoice.Id,
                invoice.TenantId,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.AmountPaid,
                Status = invoice.Status.ToString(),
                IssueDate = ToUtc(invoice.IssueDate),
                DueDate = ToUtc(invoice.DueDate),
                PaidAt = ToUtc(invoice.PaidAt),
                PublishedAt = ToUtc(invoice.PublishedAt),
                invoice.PublishedBy,
                VoidedAt = ToUtc(invoice.VoidedAt),
                invoice.VoidedBy,
                invoice.VoidReason,
                invoice.Description,
                invoice.Notes,
                invoice.PaymentInstructions,
                invoice.BillingContactEmail,
                invoice.DocumentStorageKey,
                UpdatedAt = DateTimeOffset.UtcNow
            }, transaction: trans, cancellationToken: ct);

            // Re-sync line items if draft
            if (invoice.Status == InvoiceStatus.Draft && invoice.LineItems.Any())
            {
                await conn.ExecuteProcedureAsync("sp_delete_invoice_line_items", new { InvoiceId = invoice.Id }, transaction: trans, cancellationToken: ct);

                foreach (var item in invoice.LineItems)
                {
                    if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                    item.InvoiceId = invoice.Id;
                    await conn.ExecuteProcedureAsync("sp_insert_invoice_line_item", new
                    {
                        item.Id,
                        item.InvoiceId,
                        item.LineNumber,
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,
                        LineTotal = item.LineTotal,
                        item.Category,
                        CreatedAt = ToUtc(item.CreatedAt)
                    }, transaction: trans, cancellationToken: ct);
                }
            }

            trans.Commit();
            return true;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            // Idempotency: if provider_payment_id is already present, return existing record
            if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                var existing = await conn.QuerySingleFunctionAsync<Payment>(
                    "fn_get_payment_by_provider_payment_id",
                    new { ProviderPaymentId = payment.ProviderPaymentId },
                    transaction: trans,
                    cancellationToken: ct);

                if (existing != null)
                {
                    trans.Commit();
                    return existing;
                }
            }

            if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();

            await conn.ExecuteProcedureAsync("sp_insert_payment", new
            {
                payment.Id,
                payment.TenantId,
                payment.OrganizationId,
                payment.InvoiceId,
                payment.InvoiceNumber,
                payment.PaymentReference,
                payment.Amount,
                payment.Currency,
                payment.Method,
                payment.IsVerified,
                payment.VerifiedBy,
                VerifiedAt = ToUtc(payment.VerifiedAt),
                payment.Status,
                payment.TransactionId,
                payment.Notes,
                payment.ProviderOrderId,
                payment.ProviderPaymentId,
                payment.ProviderSignature,
                payment.ReceiptNumber,
                payment.RecordedByUserId,
                payment.RecordedByEmail,
                payment.IsReversed,
                ReversedAt = ToUtc(payment.ReversedAt),
                payment.ReversedBy,
                payment.ReversalReason,
                payment.ReversalReference,
                payment.AuditMetadata,
                PaymentDate = ToUtc(payment.PaymentDate),
                CreatedAt = ToUtc(payment.CreatedAt)
            }, transaction: trans, cancellationToken: ct);

            // Transactional balance & status recalculation from valid ledger records
            if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != Guid.Empty)
            {
                await conn.ExecuteProcedureAsync("sp_recalculate_invoice_balance", new { InvoiceId = payment.InvoiceId.Value }, transaction: trans, cancellationToken: ct);
            }

            trans.Commit();
            return payment;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<Payment>(
            "fn_get_payment_by_id",
            new { PaymentId = paymentId },
            cancellationToken: ct);
    }

    public async Task<Payment?> GetPaymentByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<Payment>(
            "fn_get_payment_by_provider_payment_id",
            new { ProviderPaymentId = providerPaymentId.Trim() },
            cancellationToken: ct);
    }

    public async Task<Payment?> GetPaymentByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<Payment>(
            "fn_get_payment_by_receipt_number",
            new { ReceiptNumber = receiptNumber.Trim() },
            cancellationToken: ct);
    }

    public async Task<Payment> ReversePaymentAsync(Guid paymentId, string reversedBy, string reason, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            var payment = await conn.QuerySingleFunctionAsync<Payment>(
                "fn_get_payment_by_id",
                new { PaymentId = paymentId },
                transaction: trans,
                cancellationToken: ct)
                ?? throw new KeyNotFoundException($"Payment with ID '{paymentId}' was not found.");

            if (payment.IsReversed)
            {
                throw new InvalidOperationException($"Payment '{payment.PaymentReference}' has already been reversed.");
            }

            var reversalRef = "REV-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var now = DateTimeOffset.UtcNow;

            await conn.ExecuteProcedureAsync("sp_reverse_payment", new
            {
                PaymentId = paymentId,
                ReversedBy = reversedBy,
                ReversalReason = reason,
                ReversalReference = reversalRef,
                ReversedAt = ToUtc(now)
            }, transaction: trans, cancellationToken: ct);

            payment.IsReversed = true;
            payment.Status = "Reversed";
            payment.ReversedAt = now;
            payment.ReversedBy = reversedBy;
            payment.ReversalReason = reason;
            payment.ReversalReference = reversalRef;

            if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != Guid.Empty)
            {
                await conn.ExecuteProcedureAsync("sp_recalculate_invoice_balance", new { InvoiceId = payment.InvoiceId.Value }, transaction: trans, cancellationToken: ct);
            }

            trans.Commit();
            return payment;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<Invoice> RecalculateInvoiceBalanceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            await conn.ExecuteProcedureAsync("sp_recalculate_invoice_balance", new { InvoiceId = invoiceId }, transaction: trans, cancellationToken: ct);
            trans.Commit();

            return await GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
                ?? throw new KeyNotFoundException($"Invoice '{invoiceId}' not found.");
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await GetPaymentsForInvoiceInternalAsync(conn, invoiceId);
    }

    public async Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return (await conn.QueryFunctionAsync<Payment>(
            "fn_get_payments_for_organization",
            new { OrganizationId = organizationId },
            cancellationToken: ct)).ToList();
    }

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
            InvoiceNumber = row.InvoiceNumber,
            BillingPeriod = row.BillingPeriod,
            Subtotal = row.Subtotal,
            DiscountAmount = row.DiscountAmount,
            TaxAmount = row.TaxAmount,
            TotalAmount = row.TotalAmount,
            AmountPaid = row.AmountPaid,
            Currency = row.Currency,
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
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? BillingPeriod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Draft";
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

    private static DateTimeOffset ToUtc(DateTimeOffset dto) => dto.ToUniversalTime();
    private static DateTimeOffset? ToUtc(DateTimeOffset? dto) => dto?.ToUniversalTime();
}
