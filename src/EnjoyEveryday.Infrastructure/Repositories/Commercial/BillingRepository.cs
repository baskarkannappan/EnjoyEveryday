using System.Data;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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
        var sql = @"
            SELECT id, tenant_id, organization_id, invoice_number, billing_period,
                   subtotal, discount_amount, tax_amount, total_amount, amount_paid,
                   currency, status, issue_date, due_date, paid_at, published_at,
                   published_by, voided_at, voided_by, void_reason, created_by,
                   description, notes, payment_instructions, billing_contact_email,
                   document_storage_key, created_at, updated_at
            FROM invoices
            WHERE organization_id = @OrganizationId AND tenant_id = @TenantId " +
            (includeDrafts ? "" : "AND status != 'Draft' ") +
            "ORDER BY issue_date DESC, created_at DESC";

        var invoices = (await conn.QueryAsync<InvoiceRow>(sql, new { OrganizationId = organizationId, TenantId = tenantId })).ToList();
        var result = new List<Invoice>();

        foreach (var row in invoices)
        {
            var inv = MapToInvoice(row);
            inv.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, inv.Id);
            inv.Payments = await GetPaymentsForInvoiceInternalAsync(conn, inv.Id);
            result.Add(inv);
        }

        return result;
    }

    public async Task<List<Invoice>> GetAllInvoicesAsync(Guid? tenantId, Guid? organizationId, InvoiceStatus? status, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT id, tenant_id, organization_id, invoice_number, billing_period,
                   subtotal, discount_amount, tax_amount, total_amount, amount_paid,
                   currency, status, issue_date, due_date, paid_at, published_at,
                   published_by, voided_at, voided_by, void_reason, created_by,
                   description, notes, payment_instructions, billing_contact_email,
                   document_storage_key, created_at, updated_at
            FROM invoices
            WHERE 1=1 ";

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            sql += " AND tenant_id = @TenantId ";

        if (organizationId.HasValue && organizationId.Value != Guid.Empty)
            sql += " AND organization_id = @OrganizationId ";

        if (status.HasValue)
            sql += " AND status = @Status ";

        sql += " ORDER BY issue_date DESC, created_at DESC";

        var statusString = status?.ToString();
        var invoices = (await conn.QueryAsync<InvoiceRow>(sql, new { TenantId = tenantId, OrganizationId = organizationId, Status = statusString })).ToList();
        var result = new List<Invoice>();

        foreach (var row in invoices)
        {
            var inv = MapToInvoice(row);
            inv.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, inv.Id);
            inv.Payments = await GetPaymentsForInvoiceInternalAsync(conn, inv.Id);
            result.Add(inv);
        }

        return result;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT id, tenant_id, organization_id, invoice_number, billing_period,
                   subtotal, discount_amount, tax_amount, total_amount, amount_paid,
                   currency, status, issue_date, due_date, paid_at, published_at,
                   published_by, voided_at, voided_by, void_reason, created_by,
                   description, notes, payment_instructions, billing_contact_email,
                   document_storage_key, created_at, updated_at
            FROM invoices
            WHERE id = @InvoiceId " +
            (tenantId != Guid.Empty ? " AND tenant_id = @TenantId" : "");

        var row = await conn.QuerySingleOrDefaultAsync<InvoiceRow>(sql, new { InvoiceId = invoiceId, TenantId = tenantId });
        if (row == null) return null;

        var invoice = MapToInvoice(row);
        invoice.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, invoice.Id);
        invoice.Payments = await GetPaymentsForInvoiceInternalAsync(conn, invoice.Id);
        return invoice;
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id, tenant_id, organization_id, invoice_number, billing_period,
                   subtotal, discount_amount, tax_amount, total_amount, amount_paid,
                   currency, status, issue_date, due_date, paid_at, published_at,
                   published_by, voided_at, voided_by, void_reason, created_by,
                   description, notes, payment_instructions, billing_contact_email,
                   document_storage_key, created_at, updated_at
            FROM invoices
            WHERE invoice_number = @InvoiceNumber";

        var row = await conn.QuerySingleOrDefaultAsync<InvoiceRow>(sql, new { InvoiceNumber = invoiceNumber.Trim() });
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
            const string insertInvoiceSql = @"
                INSERT INTO invoices (
                    id, tenant_id, organization_id, invoice_number, billing_period,
                    subtotal, discount_amount, tax_amount, total_amount, amount_paid,
                    currency, status, issue_date, due_date, paid_at, published_at,
                    published_by, voided_at, voided_by, void_reason, created_by,
                    description, notes, payment_instructions, billing_contact_email,
                    document_storage_key, created_at, updated_at
                ) VALUES (
                    @Id, @TenantId, @OrganizationId, @InvoiceNumber, @BillingPeriod,
                    @Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, @AmountPaid,
                    @Currency, @Status, @IssueDate, @DueDate, @PaidAt, @PublishedAt,
                    @PublishedBy, @VoidedAt, @VoidedBy, @VoidReason, @CreatedBy,
                    @Description, @Notes, @PaymentInstructions, @BillingContactEmail,
                    @DocumentStorageKey, @CreatedAt, @UpdatedAt
                )";

            await conn.ExecuteAsync(insertInvoiceSql, new
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
            }, trans);

            if (invoice.LineItems.Any())
            {
                const string insertItemSql = @"
                    INSERT INTO invoice_line_items (
                        id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
                    ) VALUES (
                        @Id, @InvoiceId, @LineNumber, @Description, @Quantity, @UnitPrice, @LineTotal, @Category, @CreatedAt
                    )";

                foreach (var item in invoice.LineItems)
                {
                    if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                    item.InvoiceId = invoice.Id;
                    await conn.ExecuteAsync(insertItemSql, new
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
                    }, trans);
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
            const string updateInvoiceSql = @"
                UPDATE invoices SET
                    subtotal = @Subtotal,
                    discount_amount = @DiscountAmount,
                    tax_amount = @TaxAmount,
                    total_amount = @TotalAmount,
                    amount_paid = @AmountPaid,
                    status = @Status,
                    issue_date = @IssueDate,
                    due_date = @DueDate,
                    paid_at = @PaidAt,
                    published_at = @PublishedAt,
                    published_by = @PublishedBy,
                    voided_at = @VoidedAt,
                    voided_by = @VoidedBy,
                    void_reason = @VoidReason,
                    description = @Description,
                    notes = @Notes,
                    payment_instructions = @PaymentInstructions,
                    billing_contact_email = @BillingContactEmail,
                    document_storage_key = @DocumentStorageKey,
                    updated_at = @UpdatedAt
                WHERE id = @Id AND tenant_id = @TenantId";

            var rows = await conn.ExecuteAsync(updateInvoiceSql, new
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
            }, trans);

            // Re-sync line items if draft
            if (invoice.Status == InvoiceStatus.Draft && invoice.LineItems.Any())
            {
                await conn.ExecuteAsync("DELETE FROM invoice_line_items WHERE invoice_id = @InvoiceId", new { InvoiceId = invoice.Id }, trans);
                const string insertItemSql = @"
                    INSERT INTO invoice_line_items (
                        id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
                    ) VALUES (
                        @Id, @InvoiceId, @LineNumber, @Description, @Quantity, @UnitPrice, @LineTotal, @Category, @CreatedAt
                    )";

                foreach (var item in invoice.LineItems)
                {
                    if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                    item.InvoiceId = invoice.Id;
                    await conn.ExecuteAsync(insertItemSql, new
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
                    }, trans);
                }
            }

            trans.Commit();
            return rows > 0;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    private const string PaymentSelectColumns = @"
        id, tenant_id AS TenantId, organization_id AS OrganizationId, 
        invoice_id AS InvoiceId, invoice_number AS InvoiceNumber,
        payment_reference AS PaymentReference, amount, currency, method, 
        is_verified AS IsVerified, verified_by AS VerifiedBy, verified_at AS VerifiedAt, 
        status, transaction_id AS TransactionId, notes,
        provider_order_id AS ProviderOrderId, provider_payment_id AS ProviderPaymentId, 
        provider_signature AS ProviderSignature, receipt_number AS ReceiptNumber,
        recorded_by_user_id AS RecordedByUserId, recorded_by_email AS RecordedByEmail,
        is_reversed AS IsReversed, reversed_at AS ReversedAt, reversed_by AS ReversedBy,
        reversal_reason AS ReversalReason, reversal_reference AS ReversalReference,
        audit_metadata AS AuditMetadata, payment_date AS PaymentDate, created_at AS CreatedAt";

    public async Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            // Idempotency: if provider_payment_id is already present, return existing record
            if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                var existingSql = @"
                    SELECT " + PaymentSelectColumns + @"
                    FROM payments
                    WHERE provider_payment_id = @ProviderPaymentId AND is_reversed = FALSE
                    LIMIT 1";
                var existing = await conn.QuerySingleOrDefaultAsync<Payment>(existingSql, new { payment.ProviderPaymentId }, trans);
                if (existing != null)
                {
                    trans.Commit();
                    return existing;
                }
            }

            if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();

            const string insertPaymentSql = @"
                INSERT INTO payments (
                    id, tenant_id, organization_id, invoice_id, invoice_number,
                    payment_reference, amount, currency, method, is_verified,
                    verified_by, verified_at, status, transaction_id, notes,
                    provider_order_id, provider_payment_id, provider_signature,
                    receipt_number, recorded_by_user_id, recorded_by_email,
                    is_reversed, reversed_at, reversed_by, reversal_reason, reversal_reference,
                    audit_metadata, payment_date, created_at
                ) VALUES (
                    @Id, @TenantId, @OrganizationId, @InvoiceId, @InvoiceNumber,
                    @PaymentReference, @Amount, @Currency, @Method, @IsVerified,
                    @VerifiedBy, @VerifiedAt, @Status, @TransactionId, @Notes,
                    @ProviderOrderId, @ProviderPaymentId, @ProviderSignature,
                    @ReceiptNumber, @RecordedByUserId, @RecordedByEmail,
                    @IsReversed, @ReversedAt, @ReversedBy, @ReversalReason, @ReversalReference,
                    CASE WHEN @AuditMetadata IS NULL THEN NULL ELSE @AuditMetadata::jsonb END,
                    @PaymentDate, @CreatedAt
                )";

            await conn.ExecuteAsync(insertPaymentSql, new
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
            }, trans);

            // Transactional balance & status recalculation from valid ledger records
            if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != Guid.Empty)
            {
                await RecalculateInvoiceBalanceInternalAsync(conn, trans, payment.InvoiceId.Value);
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
        var sql = @"
            SELECT " + PaymentSelectColumns + @"
            FROM payments
            WHERE id = @PaymentId";
        return await conn.QuerySingleOrDefaultAsync<Payment>(sql, new { PaymentId = paymentId });
    }

    public async Task<Payment?> GetPaymentByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT " + PaymentSelectColumns + @"
            FROM payments
            WHERE provider_payment_id = @ProviderPaymentId AND is_reversed = FALSE
            LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<Payment>(sql, new { ProviderPaymentId = providerPaymentId.Trim() });
    }

    public async Task<Payment?> GetPaymentByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT " + PaymentSelectColumns + @"
            FROM payments
            WHERE receipt_number = @ReceiptNumber AND is_reversed = FALSE
            LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<Payment>(sql, new { ReceiptNumber = receiptNumber.Trim() });
    }

    public async Task<Payment> ReversePaymentAsync(Guid paymentId, string reversedBy, string reason, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var trans = conn.BeginTransaction();
        try
        {
            var selectSql = @"
                SELECT " + PaymentSelectColumns + @"
                FROM payments
                WHERE id = @PaymentId
                FOR UPDATE";
            var payment = await conn.QuerySingleOrDefaultAsync<Payment>(selectSql, new { PaymentId = paymentId }, trans)
                ?? throw new KeyNotFoundException($"Payment with ID '{paymentId}' was not found.");

            if (payment.IsReversed)
            {
                throw new InvalidOperationException($"Payment '{payment.PaymentReference}' has already been reversed.");
            }

            var reversalRef = "REV-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var now = DateTimeOffset.UtcNow;

            const string updatePaymentSql = @"
                UPDATE payments
                SET is_reversed = TRUE,
                    status = 'Reversed',
                    reversed_at = @ReversedAt,
                    reversed_by = @ReversedBy,
                    reversal_reason = @ReversalReason,
                    reversal_reference = @ReversalReference
                WHERE id = @PaymentId";

            await conn.ExecuteAsync(updatePaymentSql, new
            {
                PaymentId = paymentId,
                ReversedAt = ToUtc(now),
                ReversedBy = reversedBy,
                ReversalReason = reason,
                ReversalReference = reversalRef
            }, trans);

            payment.IsReversed = true;
            payment.Status = "Reversed";
            payment.ReversedAt = now;
            payment.ReversedBy = reversedBy;
            payment.ReversalReason = reason;
            payment.ReversalReference = reversalRef;

            if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != Guid.Empty)
            {
                await RecalculateInvoiceBalanceInternalAsync(conn, trans, payment.InvoiceId.Value);
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
            await RecalculateInvoiceBalanceInternalAsync(conn, trans, invoiceId);
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

    private static async Task RecalculateInvoiceBalanceInternalAsync(IDbConnection conn, IDbTransaction trans, Guid invoiceId)
    {
        // 1. Authoritative server calculation: sum only valid non-reversed completed payments
        const string sumSql = @"
            SELECT COALESCE(SUM(amount), 0)
            FROM payments
            WHERE invoice_id = @InvoiceId
              AND status = 'Completed'
              AND is_reversed = FALSE";
        var totalPaid = await conn.ExecuteScalarAsync<decimal>(sumSql, new { InvoiceId = invoiceId }, trans);

        // 2. Update invoice amount_paid, paid_at, and status accordingly
        const string updateInvoiceSql = @"
            UPDATE invoices
            SET amount_paid = @TotalPaid,
                paid_at = CASE 
                    WHEN (total_amount - @TotalPaid) <= 0 THEN COALESCE(paid_at, NOW())
                    ELSE NULL 
                END,
                status = CASE
                    WHEN status = 'Void' THEN 'Void'
                    WHEN status = 'Draft' THEN 'Draft'
                    WHEN (total_amount - @TotalPaid) <= 0 THEN 'Paid'
                    WHEN @TotalPaid > 0 THEN 'PartiallyPaid'
                    ELSE 'Unpaid'
                END,
                updated_at = NOW()
            WHERE id = @InvoiceId";

        await conn.ExecuteAsync(updateInvoiceSql, new { TotalPaid = totalPaid, InvoiceId = invoiceId }, trans);
    }

    public async Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await GetPaymentsForInvoiceInternalAsync(conn, invoiceId);
    }

    public async Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var sql = @"
            SELECT " + PaymentSelectColumns + @"
            FROM payments
            WHERE organization_id = @OrganizationId
            ORDER BY payment_date DESC, created_at DESC";

        return (await conn.QueryAsync<Payment>(sql, new { OrganizationId = organizationId })).ToList();
    }

    public async Task<OrganizationBillingSettings?> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id, tenant_id, organization_id, mode, frequency, effective_date,
                    next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
            FROM organization_billing_settings
            WHERE organization_id = @OrganizationId";

        var row = await conn.QuerySingleOrDefaultAsync<BillingSettingsRow>(sql, new { OrganizationId = organizationId });
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
        const string sql = @"
            INSERT INTO organization_billing_settings (
                id, tenant_id, organization_id, mode, frequency, effective_date,
                next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
            ) VALUES (
                @Id, @TenantId, @OrganizationId, @Mode, @Frequency, @EffectiveDate,
                @NextBillingDate, @AutoChargeEnabled, @Notes, @UpdatedBy, @UpdatedAt
            )
            ON CONFLICT (organization_id) DO UPDATE SET
                mode = EXCLUDED.mode,
                frequency = EXCLUDED.frequency,
                effective_date = EXCLUDED.effective_date,
                next_billing_date = EXCLUDED.next_billing_date,
                auto_charge_enabled = EXCLUDED.auto_charge_enabled,
                notes = EXCLUDED.notes,
                updated_by = EXCLUDED.updated_by,
                updated_at = EXCLUDED.updated_at";

        if (settings.Id == Guid.Empty) settings.Id = Guid.NewGuid();
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        var rows = await conn.ExecuteAsync(sql, new
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
        });

        return rows > 0;
    }

    public async Task<List<OrganizationBillingSettings>> GetOrganizationsDueForAutomaticBillingAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT id, tenant_id, organization_id, mode, frequency, effective_date,
                    next_billing_date, auto_charge_enabled, notes, updated_by, updated_at
            FROM organization_billing_settings
            WHERE mode = 'Automatic'
                AND effective_date <= @AsOfDate
                AND (next_billing_date IS NULL OR next_billing_date <= @AsOfDate)";

        var rows = await conn.QueryAsync<BillingSettingsRow>(sql, new { AsOfDate = ToUtc(asOfDate) });
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
        const string sql = @"
            SELECT id, invoice_id, line_number, description, quantity, unit_price, line_total, category, created_at
            FROM invoice_line_items
            WHERE invoice_id = @InvoiceId
            ORDER BY line_number ASC";

        return (await conn.QueryAsync<InvoiceLineItem>(sql, new { InvoiceId = invoiceId })).ToList();
    }

    private static async Task<List<Payment>> GetPaymentsForInvoiceInternalAsync(IDbConnection conn, Guid invoiceId)
    {
        var sql = @"
            SELECT " + PaymentSelectColumns + @"
            FROM payments
            WHERE invoice_id = @InvoiceId
            ORDER BY payment_date ASC";

        return (await conn.QueryAsync<Payment>(sql, new { InvoiceId = invoiceId })).ToList();
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
