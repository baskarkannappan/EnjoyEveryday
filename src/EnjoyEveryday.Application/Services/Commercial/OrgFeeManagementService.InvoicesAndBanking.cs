using System.Text.Json;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public partial class OrgFeeManagementService
{
    public async Task<Invoice> GenerateChildTuitionInvoiceAsync(GenerateChildTuitionInvoiceRequest request, CancellationToken ct = default)
    {
        if (request.ChildId == Guid.Empty)
            throw new ArgumentException("Child must be selected.", nameof(request.ChildId));
        if (request.Amount <= 0)
            throw new ArgumentException("Invoice amount must be greater than zero.", nameof(request.Amount));
        if (request.DueDate < request.IssueDate)
            throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(request.DueDate));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(request.BranchId, ct);
        var child = await _childRepository.GetByIdAsync(tenantId, request.ChildId, ct)
            ?? throw new KeyNotFoundException("Child record was not found.");

        Guid? branchId = request.BranchId;
        if (!branchId.HasValue && child.ClassroomId.HasValue)
        {
            var room = await _classroomRepository.GetByIdAsync(tenantId, child.ClassroomId.Value, ct);
            branchId = room?.BranchId;
        }
        await EnsureBranchAccessIfRestrictedAsync(branchId, ct);

        var org = await _organizationRepository.GetByIdAsync(orgId, ct);
        var currency = !string.IsNullOrWhiteSpace(org?.Currency) ? org.Currency.ToUpperInvariant() : "INR";

        var subtotal = Math.Round(request.Amount, 2);
        var discount = Math.Round(Math.Max(0, request.DiscountAmount), 2);
        var taxable = Math.Max(0, subtotal - discount);
        var tax = Math.Round(taxable * (Math.Max(0, request.TaxRatePercentage) / 100m), 2);
        var total = taxable + tax;

        // Snapshot organization's bank details and active QR codes on issue
        var bankAccount = await _feeRepository.GetBankAccountAsync(tenantId, orgId, ct);
        var qrCodes = (await _feeRepository.GetQrCodesAsync(tenantId, orgId, ct)).Where(q => q.IsActive).ToList();
        var snapshot = new PaymentInstructionsSnapshotDto
        {
            BankAccount = bankAccount,
            QrCodes = qrCodes,
            SnapshottedAt = DateTimeOffset.UtcNow
        };
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        var invoiceNumber = $"INV-FEE-{DateTimeOffset.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var description = !string.IsNullOrWhiteSpace(request.Description)
            ? request.Description.Trim()
            : $"{(request.FeeMethod == FeeMethods.FlatRate ? "Flat-Rate Fee" : "Tariff Tuition")} — {child.FirstName} {child.LastName}";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = orgId,
            BranchId = branchId,
            ChildId = child.Id,
            ContractId = request.ContractId,
            ChildName = $"{child.FirstName} {child.LastName}".Trim(),
            InvoiceNumber = invoiceNumber,
            BillingPeriod = request.BillingPeriod ?? request.IssueDate.ToString("yyyy-MM"),
            Subtotal = subtotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            TotalAmount = total,
            AdjustmentAmount = 0,
            AmountPaid = 0,
            PendingVerificationAmount = 0,
            Currency = currency,
            Status = request.SaveAsDraft ? InvoiceStatus.Draft : InvoiceStatus.Issued,
            FeeMethod = request.FeeMethod,
            FeePlanName = request.FeePlanName,
            IssueDate = request.IssueDate.ToUniversalTime(),
            DueDate = request.DueDate.ToUniversalTime(),
            CreatedBy = _userContext.Email ?? "Administrator",
            Description = description,
            Notes = request.Notes?.Trim(),
            PaymentInstructions = bankAccount?.AdditionalInstructions,
            PaymentInstructionsSnapshot = snapshotJson,
            LineItems = new List<InvoiceLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    LineNumber = 1,
                    Description = description,
                    Quantity = 1,
                    UnitPrice = subtotal,
                    Category = request.FeeMethod == FeeMethods.FlatRate ? "FlatRateFee" : "TariffTuition"
                }
            }
        };

        var installments = request.Installments
            .Where(i => i.AmountDue > 0)
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new InvoiceInstallment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate.Date,
                AmountDue = Math.Round(i.AmountDue, 2),
                AllowCustomAmount = i.AllowCustomAmount,
                AllowMultipleTransactions = i.AllowMultipleTransactions,
                Notes = i.Notes?.Trim()
            })
            .ToList();

        var created = await _feeRepository.CreateTuitionInvoiceAsync(invoice, installments, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "TuitionInvoice.Generated",
            EntityType = nameof(Invoice),
            EntityId = created.Id,
            Details = new
            {
                created.OrganizationId,
                created.BranchId,
                created.ChildId,
                created.InvoiceNumber,
                created.FeeMethod,
                created.TotalAmount,
                created.Currency,
                InstallmentCount = installments.Count
            }
        }, ct);

        return created;
    }

    public async Task<List<Invoice>> GetOrgTuitionInvoicesAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, allowedBranches) = await RequireStaffFinancialScopeAsync(branchId, ct);
        var list = await _feeRepository.GetOrgTuitionInvoicesAsync(tenantId, orgId, branchId, ct);
        if (isOrgAdmin) return list;
        return list.Where(i => i.BranchId.HasValue && allowedBranches.Contains(i.BranchId.Value)).ToList();
    }

    public async Task<List<InvoiceInstallment>> GetInvoiceInstallmentsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        return await _feeRepository.GetInvoiceInstallmentsAsync(invoiceId, ct);
    }

    public async Task<bool> PublishTuitionInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot publish an invoice belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(invoice.BranchId, ct);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be published.");

        var bankAccount = await _feeRepository.GetBankAccountAsync(tenantId, orgId, ct);
        var qrCodes = (await _feeRepository.GetQrCodesAsync(tenantId, orgId, ct)).Where(q => q.IsActive).ToList();
        var snapshotJson = JsonSerializer.Serialize(new PaymentInstructionsSnapshotDto
        {
            BankAccount = bankAccount,
            QrCodes = qrCodes,
            SnapshottedAt = DateTimeOffset.UtcNow
        }, JsonOptions);

        var actor = _userContext.Email ?? "Administrator";
        var published = await _feeRepository.PublishTuitionInvoiceAsync(
            invoiceId,
            actor,
            bankAccount?.AdditionalInstructions,
            snapshotJson,
            ct);

        if (published)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "TuitionInvoice.Published",
                EntityType = nameof(Invoice),
                EntityId = invoiceId,
                Details = new { invoice.OrganizationId, invoice.BranchId, invoice.ChildId, invoice.InvoiceNumber, PublishedBy = actor }
            }, ct);
        }

        return published;
    }

    public async Task<bool> ApplyInvoiceAdjustmentAsync(ApplyInvoiceAdjustmentRequest request, CancellationToken ct = default)
    {
        if (request.AdjustmentAmount <= 0)
            throw new ArgumentException("Adjustment amount must be greater than zero.", nameof(request.AdjustmentAmount));
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Adjustment reason is required.", nameof(request.Reason));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot adjust an invoice belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(invoice.BranchId, ct);

        var delta = Math.Round(request.AdjustmentAmount, 2);
        var ok = await _feeRepository.ApplyInvoiceAdjustmentAsync(invoice.Id, delta, nameof(InvoiceStatus.Adjusted), ct);
        if (ok && invoice.ChildId.HasValue)
        {
            await _feeRepository.RecordChildFeeAdjustmentAsync(new ChildFeeAdjustment
            {
                TenantId = tenantId,
                OrganizationId = orgId,
                BranchId = invoice.BranchId,
                ChildId = invoice.ChildId.Value,
                ContractId = invoice.ContractId,
                InvoiceId = invoice.Id,
                AdjustmentType = request.AdjustmentType,
                PreviousAmount = invoice.EffectivePayableAmount,
                NewAmount = Math.Max(0, invoice.EffectivePayableAmount - delta),
                AdjustmentDelta = -delta,
                Reason = request.Reason.Trim(),
                EffectiveDate = DateTime.UtcNow.Date,
                AdjustedByUserId = _userContext.UserId,
                AdjustedByEmail = _userContext.Email ?? "Administrator"
            }, ct);

            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "TuitionInvoice.Adjusted",
                EntityType = nameof(Invoice),
                EntityId = invoice.Id,
                Details = new { invoice.InvoiceNumber, request.AdjustmentType, AdjustmentAmount = delta, request.Reason }
            }, ct);
        }
        return ok;
    }

    public async Task<bool> CancelTuitionInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(tenantId, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot cancel an invoice belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(invoice.BranchId, ct);

        if (invoice.AmountPaid > 0)
            throw new InvalidOperationException("Cannot cancel an invoice that already has verified payments. Apply an adjustment or payment reversal instead.");

        var cancelled = await _feeRepository.CancelInvoiceAsync(invoiceId, _userContext.Email ?? "Administrator", reason.Trim(), ct);
        if (cancelled)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "TuitionInvoice.Cancelled",
                EntityType = nameof(Invoice),
                EntityId = invoiceId,
                Details = new { invoice.InvoiceNumber, Reason = reason.Trim() }
            }, ct);
        }
        return cancelled;
    }

    public async Task<OrgBankAccount?> GetBankAccountAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        return await _feeRepository.GetBankAccountAsync(tenantId, orgId, ct);
    }

    public async Task<OrgBankAccount> UpsertBankAccountAsync(OrgBankAccount account, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can update organization bank details.");

        account.TenantId = tenantId;
        account.OrganizationId = orgId;
        account.UpdatedBy = _userContext.Email ?? "Org Admin";

        var saved = await _feeRepository.UpsertBankAccountAsync(account, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrganizationBankDetails.Updated",
            EntityType = nameof(OrgBankAccount),
            EntityId = saved.Id,
            Details = new { saved.OrganizationId, saved.BankName, saved.BranchName, saved.IfscCode, saved.AllowParentCashDeclaration, saved.RequirePaymentProof }
        }, ct);

        return saved;
    }

    public async Task<List<OrgPaymentQrCode>> GetQrCodesAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        return await _feeRepository.GetQrCodesAsync(tenantId, orgId, ct);
    }

    public async Task<OrgPaymentQrCode> UpsertQrCodeAsync(OrgPaymentQrCode qrCode, CancellationToken ct = default)
    {
        if (qrCode.SlotIndex is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(qrCode.SlotIndex), "Organizations may configure up to 3 QR code slots (1–3).");
        if (string.IsNullOrWhiteSpace(qrCode.DisplayName))
            throw new ArgumentException("QR code display name is required.");
        if (string.IsNullOrWhiteSpace(qrCode.QrImageDataUri))
            throw new ArgumentException("QR code image is required.");

        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can manage payment QR codes.");

        qrCode.TenantId = tenantId;
        qrCode.OrganizationId = orgId;
        qrCode.UpdatedBy = _userContext.Email ?? "Org Admin";

        var saved = await _feeRepository.UpsertQrCodeAsync(qrCode, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrganizationQrCode.Saved",
            EntityType = nameof(OrgPaymentQrCode),
            EntityId = saved.Id,
            Details = new { saved.OrganizationId, saved.SlotIndex, saved.DisplayName, saved.PaymentPurpose, saved.IsActive }
        }, ct);

        return saved;
    }

    public async Task<bool> DeleteQrCodeAsync(int slotIndex, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can remove payment QR codes.");

        var removed = await _feeRepository.DeleteQrCodeAsync(tenantId, orgId, slotIndex, ct);
        if (removed)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "OrganizationQrCode.Deleted",
                EntityType = nameof(OrgPaymentQrCode),
                Details = new { OrganizationId = orgId, SlotIndex = slotIndex }
            }, ct);
        }
        return removed;
    }

}
