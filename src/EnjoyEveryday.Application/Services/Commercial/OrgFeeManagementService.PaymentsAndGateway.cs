using System.Text.Json;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public partial class OrgFeeManagementService
{
    public async Task<List<Invoice>> GetParentChildInvoicesAsync(Guid childId, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(childId, ct);
        return await _feeRepository.GetChildInvoicesAsync(effectiveTenantId, childId, ct);
    }

    public async Task<List<Payment>> GetParentChildInvoicePaymentsAsync(Guid childId, Guid invoiceId, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(childId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != childId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        return await _billingRepository.GetPaymentsForInvoiceAsync(invoiceId, ct);
    }

    public async Task<List<InvoiceInstallment>> GetParentChildInvoiceInstallmentsAsync(Guid childId, Guid invoiceId, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(childId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != childId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        return await _feeRepository.GetInvoiceInstallmentsAsync(invoiceId, ct);
    }

    public async Task<PaymentInstructionsSnapshotDto> GetInvoicePaymentInstructionsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (!string.IsNullOrWhiteSpace(invoice.PaymentInstructionsSnapshot))
        {
            var snapshot = JsonSerializer.Deserialize<PaymentInstructionsSnapshotDto>(invoice.PaymentInstructionsSnapshot, JsonOptions);
            if (snapshot != null) return snapshot;
        }

        // Fallback for legacy invoices without snapshot
        var bank = await _feeRepository.GetBankAccountAsync(invoice.TenantId, invoice.OrganizationId, ct);
        var qrs = (await _feeRepository.GetQrCodesAsync(invoice.TenantId, invoice.OrganizationId, ct)).Where(q => q.IsActive).ToList();
        return new PaymentInstructionsSnapshotDto
        {
            BankAccount = bank,
            QrCodes = qrs
        };
    }

    public async Task<Payment> SubmitParentPaymentAsync(SubmitParentPaymentRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(request.Amount));

        var effectiveTenantId = await ValidateParentChildAccessAsync(request.ChildId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != request.ChildId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void or InvoiceStatus.Cancelled or InvoiceStatus.Paid)
            throw new InvalidOperationException($"Cannot submit payment for an invoice in {invoice.Status} status.");

        var remaining = invoice.BalanceDue;
        if (request.Amount > remaining)
            throw new InvalidOperationException($"Submitted amount ({request.Amount:N2}) exceeds remaining outstanding balance ({remaining:N2}).");

        var bankSettings = await _feeRepository.GetBankAccountAsync(invoice.TenantId, invoice.OrganizationId, ct);
        if (string.Equals(request.Method, PaymentMethods.Cash, StringComparison.OrdinalIgnoreCase) &&
            bankSettings?.AllowParentCashDeclaration != true)
        {
            throw new InvalidOperationException("Parent cash declarations are not enabled by this daycare organization.");
        }

        if (bankSettings?.RequirePaymentProof == true &&
            string.IsNullOrWhiteSpace(request.ProofStorageKey) &&
            !string.Equals(request.Method, PaymentMethods.Cash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Uploading a payment screenshot/proof is required by this organization.");
        }

        var paymentRef = "SUB-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            BranchId = invoice.BranchId,
            ChildId = request.ChildId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PaymentReference = paymentRef,
            Amount = Math.Round(request.Amount, 2),
            Currency = invoice.Currency,
            Method = request.Method,
            IsVerified = false,
            Status = PaymentStatus.PendingVerification,
            TransactionId = request.TransactionId?.Trim(),
            Notes = request.Remarks?.Trim(),
            SubmissionSource = "ParentPortal",
            ProofStorageKey = request.ProofStorageKey,
            RecordedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
            RecordedByEmail = _userContext.Email ?? "Parent Guardian",
            PaymentDate = request.PaymentDate.ToUniversalTime(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _feeRepository.SubmitPaymentAsync(payment, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentPayment.Submitted",
            EntityType = nameof(Payment),
            EntityId = saved.Id,
            Details = new
            {
                invoice.OrganizationId,
                invoice.BranchId,
                invoice.ChildId,
                invoice.InvoiceNumber,
                saved.PaymentReference,
                saved.Amount,
                saved.Method,
                saved.TransactionId
            }
        }, ct);

        return saved;
    }

    public async Task<List<Payment>> GetPaymentsQueueAsync(Guid? branchId = null, string? status = null, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, allowedBranches) = await RequireStaffFinancialScopeAsync(branchId, ct);
        var rows = await _feeRepository.GetOrgPaymentsQueueAsync(tenantId, orgId, branchId, status, ct);
        if (isOrgAdmin) return rows;
        return rows.Where(p => p.BranchId.HasValue && allowedBranches.Contains(p.BranchId.Value)).ToList();
    }

    public async Task<(bool Success, string Message)> VerifyPaymentAsync(Guid paymentId, string? remarks, CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var payment = await _billingRepository.GetPaymentByIdAsync(paymentId, ct)
            ?? throw new KeyNotFoundException("Payment record not found.");

        if (payment.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot verify a payment belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(payment.BranchId, ct);

        var actor = _userContext.Email ?? "Daycare Admin";
        var res = await _feeRepository.VerifySubmittedPaymentAsync(paymentId, actor, remarks, ct);

        if (res.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "ParentPayment.Verified",
                EntityType = nameof(Payment),
                EntityId = paymentId,
                Details = new
                {
                    OrganizationId = orgId,
                    payment.BranchId,
                    payment.ChildId,
                    payment.InvoiceNumber,
                    payment.Amount,
                    VerifiedBy = actor,
                    Remarks = remarks,
                    res.NewAmountPaid,
                    res.NewBalanceDue
                }
            }, ct);
        }

        return (res.Success, res.Message);
    }

    public async Task<(bool Success, string Message)> RejectPaymentAsync(Guid paymentId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var payment = await _billingRepository.GetPaymentByIdAsync(paymentId, ct)
            ?? throw new KeyNotFoundException("Payment record not found.");

        if (payment.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot reject a payment belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(payment.BranchId, ct);

        var actor = _userContext.Email ?? "Daycare Admin";
        var res = await _feeRepository.RejectSubmittedPaymentAsync(paymentId, actor, reason.Trim(), ct);

        if (res.Success)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "ParentPayment.Rejected",
                EntityType = nameof(Payment),
                EntityId = paymentId,
                Details = new { OrganizationId = orgId, payment.InvoiceNumber, payment.Amount, RejectedBy = actor, Reason = reason.Trim() }
            }, ct);
        }

        return (res.Success, res.Message);
    }

    public async Task<Payment> RecordDirectCashPaymentAsync(RecordDaycareCashPaymentRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Cash payment amount must be greater than zero.", nameof(request.Amount));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.OrganizationId != orgId)
            throw new UnauthorizedAccessException("Cannot record cash payment for another organization.");

        await EnsureBranchAccessIfRestrictedAsync(invoice.BranchId, ct);

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void or InvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Cannot record cash payment against a {invoice.Status} invoice.");

        if (request.Amount > invoice.BalanceDue)
            throw new InvalidOperationException($"Cash amount ({request.Amount:N2}) exceeds remaining balance ({invoice.BalanceDue:N2}).");

        var receiptNumber = !string.IsNullOrWhiteSpace(request.ReceiptNumber)
            ? request.ReceiptNumber.Trim()
            : "CASH-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var actor = _userContext.Email ?? "Daycare Admin";
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            BranchId = invoice.BranchId,
            ChildId = invoice.ChildId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PaymentReference = receiptNumber,
            ReceiptNumber = receiptNumber,
            Amount = Math.Round(request.Amount, 2),
            Currency = invoice.Currency,
            Method = PaymentMethods.Cash,
            IsVerified = true,
            VerifiedBy = actor,
            VerifiedAt = DateTimeOffset.UtcNow,
            Status = PaymentStatus.Completed,
            SubmissionSource = "DaycareStaff",
            Notes = request.Remarks?.Trim(),
            RecordedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
            RecordedByEmail = actor,
            PaymentDate = request.ReceiptDate.ToUniversalTime(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _billingRepository.RecordPaymentAsync(payment, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "DaycareCashPayment.Recorded",
            EntityType = nameof(Payment),
            EntityId = saved.Id,
            Details = new { OrganizationId = orgId, invoice.BranchId, invoice.ChildId, invoice.InvoiceNumber, saved.Amount, saved.ReceiptNumber, RecordedBy = actor }
        }, ct);

        return saved;
    }

    public async Task<OrgFeeDashboardSummary> GetDashboardSummaryAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(branchId, ct);
        return await _feeRepository.GetDashboardSummaryAsync(tenantId, orgId, branchId, ct);
    }

    public async Task<FeeSettlementReportSummary> GetFeeSettlementReportAsync(
        Guid? branchId = null,
        Guid? classroomId = null,
        Guid? childId = null,
        string? billingPeriod = null,
        string? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, isBranchAdmin, allowedBranches) = await RequireStaffFinancialScopeAsync(branchId, ct);

        if (!isOrgAdmin && branchId.HasValue && !allowedBranches.Contains(branchId.Value))
            throw new UnauthorizedAccessException("You are not authorized to view financial reports for this branch.");

        var effectiveBranchId = branchId;
        if (!isOrgAdmin && !branchId.HasValue && allowedBranches.Count == 1)
        {
            effectiveBranchId = allowedBranches[0];
        }

        var items = await _feeRepository.GetFeeSettlementReportAsync(
            tenantId,
            orgId,
            effectiveBranchId,
            classroomId,
            childId,
            billingPeriod,
            status,
            search,
            ct);

        if (!isOrgAdmin && !effectiveBranchId.HasValue)
        {
            items = items.Where(i => i.BranchId.HasValue && allowedBranches.Contains(i.BranchId.Value)).ToList();
        }

        return new FeeSettlementReportSummary
        {
            TotalInvoiced = items.Sum(i => i.TotalInvoiced),
            TotalCollected = items.Sum(i => i.TotalPaid),
            TotalOutstanding = items.Sum(i => i.BalanceDue),
            TotalPendingVerification = items.Sum(i => i.PendingVerificationAmount),
            TotalChildrenCount = items.Count,
            FullySettledCount = items.Count(i => i.SettlementStatus == SettlementStatuses.Paid),
            PartiallySettledCount = items.Count(i => i.SettlementStatus == SettlementStatuses.PartiallyPaid),
            UnpaidCount = items.Count(i => i.SettlementStatus == SettlementStatuses.Unpaid),
            OverdueCount = items.Count(i => i.SettlementStatus == SettlementStatuses.Overdue),
            PendingVerificationCount = items.Count(i => i.SettlementStatus == SettlementStatuses.PendingVerification || i.PendingVerificationCount > 0),
            NotInvoicedCount = items.Count(i => i.SettlementStatus == SettlementStatuses.NotInvoiced),
            Items = items
        };
    }

    public async Task<OrgPaymentGatewayPublicStatusDto> GetPaymentGatewayStatusAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can view or manage organization payment gateway settings.");

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(tenantId, orgId, ct);
        return ToPublicStatus(settings);
    }

    public async Task<OrgPaymentGatewayPublicStatusDto> SavePaymentGatewaySettingsAsync(SaveOrgPaymentGatewayRequest request, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can configure organization Razorpay credentials.");

        var existing = await _feeRepository.GetPaymentGatewaySettingsAsync(tenantId, orgId, ct);
        var env = string.Equals(request.Environment, "Live", StringComparison.OrdinalIgnoreCase) ? "Live" : "Test";
        var actor = _userContext.Email ?? "Organization Admin";

        if (!request.HasRazorpayAccount)
        {
            var disabled = await _feeRepository.UpsertPaymentGatewaySettingsAsync(new OrgPaymentGatewaySettings
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                TenantId = tenantId,
                OrganizationId = orgId,
                HasRazorpayAccount = false,
                KeyId = null,
                EncryptedKeySecret = string.Empty,
                EncryptedWebhookSecret = string.Empty,
                Environment = env,
                Status = GatewayConfigurationStatuses.NotConfigured,
                IsActive = false,
                LastValidatedAt = null,
                ValidationMessage = "Organization uses manual payment methods (Bank Transfer, QR, Cash).",
                ConfiguredBy = actor
            }, ct);

            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = "OrgPaymentGateway.Disabled",
                EntityType = nameof(OrgPaymentGatewaySettings),
                EntityId = disabled.Id,
                Details = new { OrganizationId = orgId, HasRazorpayAccount = false }
            }, ct);

            return ToPublicStatus(disabled);
        }

        if (string.IsNullOrWhiteSpace(request.KeyId))
            throw new ArgumentException("Razorpay Key ID is required.", nameof(request.KeyId));

        var hasNewSecret = !string.IsNullOrWhiteSpace(request.KeySecret);
        if (!hasNewSecret && string.IsNullOrWhiteSpace(existing?.EncryptedKeySecret))
            throw new ArgumentException("Razorpay Key Secret is required for initial setup.", nameof(request.KeySecret));

        var encryptedSecret = hasNewSecret ? ProtectSecret(request.KeySecret!.Trim()) : string.Empty;
        var encryptedWebhook = !string.IsNullOrWhiteSpace(request.WebhookSecret) ? ProtectSecret(request.WebhookSecret.Trim()) : string.Empty;

        var status = request.IsActive
            ? (env == "Live" ? GatewayConfigurationStatuses.LiveActive : GatewayConfigurationStatuses.TestActive)
            : GatewayConfigurationStatuses.ConfigurationPending;

        var saved = await _feeRepository.UpsertPaymentGatewaySettingsAsync(new OrgPaymentGatewaySettings
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = orgId,
            HasRazorpayAccount = true,
            KeyId = request.KeyId.Trim(),
            EncryptedKeySecret = encryptedSecret,
            EncryptedWebhookSecret = encryptedWebhook,
            Environment = env,
            Status = status,
            IsActive = request.IsActive,
            LastValidatedAt = existing?.LastValidatedAt,
            ValidationMessage = request.IsActive ? "Saved. Run validation to verify live API connectivity." : "Credentials saved; awaiting validation.",
            ConfiguredBy = actor
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrgPaymentGateway.Configured",
            EntityType = nameof(OrgPaymentGatewaySettings),
            EntityId = saved.Id,
            Details = new { OrganizationId = orgId, saved.KeyId, saved.Environment, saved.Status, saved.IsActive }
        }, ct);

        return ToPublicStatus(saved);
    }

    public async Task<OrgPaymentGatewayPublicStatusDto> ValidatePaymentGatewayAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can validate organization Razorpay credentials.");

        var existing = await _feeRepository.GetPaymentGatewaySettingsAsync(tenantId, orgId, ct)
            ?? throw new InvalidOperationException("No Razorpay configuration found for this organization.");

        if (!existing.HasRazorpayAccount || string.IsNullOrWhiteSpace(existing.KeyId) || string.IsNullOrWhiteSpace(existing.EncryptedKeySecret))
            throw new InvalidOperationException("Please save a valid Razorpay Key ID and Key Secret first.");

        if (_razorpayGateway == null)
            throw new InvalidOperationException("Razorpay HTTP gateway client is not available.");

        var plainSecret = UnprotectSecret(existing.EncryptedKeySecret);
        var actor = _userContext.Email ?? "Organization Admin";
        string status;
        string validationMsg;
        bool isActive;

        try
        {
            var testOrder = await _razorpayGateway.CreateOrderWithCredentialsAsync(
                existing.KeyId,
                plainSecret,
                amountInPaise: 100,
                currency: "INR",
                receipt: $"VAL-{orgId.ToString("N")[..8]}",
                notes: new Dictionary<string, string> { ["validation"] = "true", ["organization_id"] = orgId.ToString() },
                ct);

            status = existing.Environment == "Live"
                ? GatewayConfigurationStatuses.LiveActive
                : GatewayConfigurationStatuses.TestActive;
            validationMsg = $"Validated successfully in {existing.Environment} mode (probe order {testOrder.OrderId}).";
            isActive = true;
        }
        catch (Exception ex)
        {
            status = GatewayConfigurationStatuses.ValidationFailed;
            validationMsg = $"Validation failed: {ex.Message}";
            isActive = false;
        }

        var updated = await _feeRepository.UpsertPaymentGatewaySettingsAsync(new OrgPaymentGatewaySettings
        {
            Id = existing.Id,
            TenantId = tenantId,
            OrganizationId = orgId,
            HasRazorpayAccount = true,
            KeyId = existing.KeyId,
            EncryptedKeySecret = string.Empty,
            EncryptedWebhookSecret = string.Empty,
            Environment = existing.Environment,
            Status = status,
            IsActive = isActive,
            LastValidatedAt = DateTimeOffset.UtcNow,
            ValidationMessage = validationMsg,
            ConfiguredBy = actor
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrgPaymentGateway.Validated",
            EntityType = nameof(OrgPaymentGatewaySettings),
            EntityId = updated.Id,
            Details = new { OrganizationId = orgId, updated.Environment, updated.Status, updated.IsActive }
        }, ct);

        return ToPublicStatus(updated);
    }

    public async Task<OrgPaymentGatewayPublicStatusDto> DisablePaymentGatewayAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can disable organization Razorpay credentials.");

        var existing = await _feeRepository.GetPaymentGatewaySettingsAsync(tenantId, orgId, ct)
            ?? throw new InvalidOperationException("No Razorpay configuration found.");

        var saved = await _feeRepository.UpsertPaymentGatewaySettingsAsync(new OrgPaymentGatewaySettings
        {
            Id = existing.Id,
            TenantId = tenantId,
            OrganizationId = orgId,
            HasRazorpayAccount = existing.HasRazorpayAccount,
            KeyId = existing.KeyId,
            EncryptedKeySecret = string.Empty,
            EncryptedWebhookSecret = string.Empty,
            Environment = existing.Environment,
            Status = GatewayConfigurationStatuses.Disabled,
            IsActive = false,
            LastValidatedAt = existing.LastValidatedAt,
            ValidationMessage = "Disabled by Organization Admin.",
            ConfiguredBy = _userContext.Email ?? "Organization Admin"
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "OrgPaymentGateway.Disabled",
            EntityType = nameof(OrgPaymentGatewaySettings),
            EntityId = saved.Id,
            Details = new { OrganizationId = orgId }
        }, ct);

        return ToPublicStatus(saved);
    }

    public async Task<OrgPaymentGatewayPublicStatusDto> GetParentInvoiceGatewayAvailabilityAsync(Guid childId, Guid invoiceId, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(childId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != childId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(invoice.TenantId, invoice.OrganizationId, ct);
        return ToPublicStatus(settings);
    }

    public async Task<OrgRazorpayOrderResponse> CreateParentRazorpayOrderAsync(Guid childId, Guid invoiceId, decimal? requestedAmount = null, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(childId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != childId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void or InvoiceStatus.Cancelled or InvoiceStatus.Paid)
            throw new InvalidOperationException($"Cannot initiate Razorpay payment for invoice in {invoice.Status} status.");

        var payableAmount = requestedAmount.HasValue ? Math.Round(requestedAmount.Value, 2) : invoice.BalanceDue;
        if (payableAmount <= 0 || payableAmount > invoice.BalanceDue)
            throw new InvalidOperationException($"Requested amount ({payableAmount:N2}) must be between 0.01 and remaining balance ({invoice.BalanceDue:N2}).");

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(invoice.TenantId, invoice.OrganizationId, ct);
        if (settings == null || !settings.HasRazorpayAccount || !settings.IsActive ||
            (settings.Status != GatewayConfigurationStatuses.LiveActive && settings.Status != GatewayConfigurationStatuses.TestActive))
        {
            throw new InvalidOperationException("This daycare organization does not have an active Razorpay gateway configured. Please use manual payment options.");
        }

        if (_razorpayGateway == null)
            throw new InvalidOperationException("Razorpay gateway service is unavailable.");

        var plainSecret = UnprotectSecret(settings.EncryptedKeySecret ?? string.Empty);
        long amountInPaise = (long)Math.Round(payableAmount * 100m, MidpointRounding.AwayFromZero);
        var currency = string.IsNullOrWhiteSpace(invoice.Currency) ? "INR" : invoice.Currency.ToUpperInvariant();

        var notes = new Dictionary<string, string>
        {
            ["billing_domain"] = "OrgToParent",
            ["organization_id"] = invoice.OrganizationId.ToString(),
            ["tenant_id"] = invoice.TenantId.ToString(),
            ["invoice_id"] = invoice.Id.ToString(),
            ["child_id"] = childId.ToString()
        };

        var order = await _razorpayGateway.CreateOrderWithCredentialsAsync(
            settings.KeyId!,
            plainSecret,
            amountInPaise,
            currency,
            invoice.InvoiceNumber,
            notes,
            ct);

        var org = await _organizationRepository.GetByIdAsync(invoice.OrganizationId, ct);

        return new OrgRazorpayOrderResponse
        {
            OrderId = order.OrderId,
            KeyId = settings.KeyId!,
            Amount = payableAmount,
            AmountInPaise = amountInPaise,
            Currency = currency,
            InvoiceNumber = invoice.InvoiceNumber,
            OrganizationName = org?.Name ?? "Daycare Organization",
            Environment = settings.Environment,
            PrefillEmail = _userContext.Email ?? string.Empty
        };
    }

    public async Task<RazorpayVerificationResult> VerifyParentRazorpayPaymentAsync(VerifyParentRazorpayPaymentRequest request, CancellationToken ct = default)
    {
        var effectiveTenantId = await ValidateParentChildAccessAsync(request.ChildId, ct);
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.ChildId != request.ChildId)
            throw new UnauthorizedAccessException("Invoice does not belong to the selected child.");

        if (request.Amount <= 0 || request.Amount > invoice.BalanceDue)
            return new RazorpayVerificationResult { Success = false, ErrorMessage = "Payment amount exceeds remaining invoice balance." };

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(invoice.TenantId, invoice.OrganizationId, ct);
        if (settings == null || !settings.HasRazorpayAccount || !settings.IsActive || _razorpayGateway == null)
            return new RazorpayVerificationResult { Success = false, ErrorMessage = "Organization Razorpay gateway is not active." };

        var plainSecret = UnprotectSecret(settings.EncryptedKeySecret ?? string.Empty);
        if (!_razorpayGateway.VerifyPaymentSignatureWithSecret(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature, settings.KeyId ?? string.Empty, plainSecret))
        {
            return new RazorpayVerificationResult { Success = false, ErrorMessage = "Razorpay payment signature verification failed." };
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            BranchId = invoice.BranchId,
            ChildId = request.ChildId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = Math.Round(request.Amount, 2),
            Currency = invoice.Currency,
            Method = PaymentMethods.Razorpay,
            ProviderOrderId = request.RazorpayOrderId,
            ProviderPaymentId = request.RazorpayPaymentId,
            ProviderSignature = request.RazorpaySignature,
            RecordedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
            RecordedByEmail = _userContext.Email ?? "Parent Guardian",
            Notes = $"Online Razorpay payment via {settings.Environment} gateway"
        };

        var res = await _feeRepository.RecordOrgRazorpayPaymentAsync(payment, ct);
        if (!res.Success)
            return new RazorpayVerificationResult { Success = false, ErrorMessage = res.Message };

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentRazorpayPayment.Verified",
            EntityType = nameof(Payment),
            EntityId = res.PaymentId,
            Details = new
            {
                invoice.OrganizationId,
                invoice.BranchId,
                invoice.ChildId,
                invoice.InvoiceNumber,
                payment.Amount,
                payment.ProviderOrderId,
                payment.ProviderPaymentId
            }
        }, ct);

        var refreshed = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoice.Id, ct);
        return new RazorpayVerificationResult { Success = true, Payment = payment, Invoice = refreshed };
    }

    public async Task<bool> ProcessOrgRazorpayWebhookAsync(Guid organizationId, string rawBody, string signatureHeader, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(signatureHeader) || _razorpayGateway == null)
            return false;

        var org = await _organizationRepository.GetByIdAsync(organizationId, ct);
        if (org == null) return false;

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(org.TenantId, organizationId, ct);
        if (settings == null || !settings.HasRazorpayAccount || !settings.IsActive)
            return false;

        var webhookSecret = !string.IsNullOrWhiteSpace(settings.EncryptedWebhookSecret)
            ? UnprotectSecret(settings.EncryptedWebhookSecret)
            : UnprotectSecret(settings.EncryptedKeySecret ?? string.Empty);

        if (!_razorpayGateway.VerifyWebhookSignatureWithSecret(rawBody, signatureHeader, webhookSecret))
            return false;

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.TryGetProperty("event", out var evProp) ? evProp.GetString() ?? string.Empty : string.Empty;
        if (eventType != "payment.captured" && eventType != "order.paid")
            return true;

        var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var paymentId = paymentEntity.GetProperty("id").GetString() ?? string.Empty;
        var orderId = paymentEntity.TryGetProperty("order_id", out var ordProp) ? ordProp.GetString() ?? string.Empty : string.Empty;
        var amountPaise = paymentEntity.GetProperty("amount").GetInt64();
        var currency = paymentEntity.TryGetProperty("currency", out var curProp) ? curProp.GetString() ?? "INR" : "INR";

        var eventKey = $"org:{organizationId}:{eventType}:{paymentId}";
        if (await _billingRepository.HasWebhookEventBeenProcessedAsync(eventKey, ct))
            return true;

        Guid? invoiceId = null;
        Guid? childId = null;
        if (paymentEntity.TryGetProperty("notes", out var notesProp) && notesProp.ValueKind == JsonValueKind.Object)
        {
            if (notesProp.TryGetProperty("invoice_id", out var invIdProp) && Guid.TryParse(invIdProp.GetString(), out var parsedInv))
                invoiceId = parsedInv;
            if (notesProp.TryGetProperty("child_id", out var chIdProp) && Guid.TryParse(chIdProp.GetString(), out var parsedChild))
                childId = parsedChild;
        }

        if (!invoiceId.HasValue)
            return false;

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId.Value, ct);
        if (invoice == null || invoice.OrganizationId != organizationId || !invoice.ChildId.HasValue)
            return false;

        var amount = Math.Round(amountPaise / 100m, 2);
        var res = await _feeRepository.RecordOrgRazorpayPaymentAsync(new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            BranchId = invoice.BranchId,
            ChildId = childId ?? invoice.ChildId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = amount,
            Currency = currency,
            Method = PaymentMethods.Razorpay,
            ProviderOrderId = orderId,
            ProviderPaymentId = paymentId,
            ProviderSignature = signatureHeader,
            RecordedByEmail = "Razorpay Webhook",
            Notes = $"Verified via organization Razorpay webhook ({eventType})"
        }, ct);

        await _billingRepository.RecordWebhookEventAsync(
            eventKey,
            eventType,
            invoice.Id,
            paymentId,
            amountPaise,
            currency,
            res.Success ? "Processed" : "Rejected",
            rawBody,
            res.Success ? null : res.Message,
            ct);

        return res.Success;
    }

    private string ProtectSecret(string plaintext)
        => _credentialProtector != null ? _credentialProtector.Protect(plaintext) : plaintext;

    private string UnprotectSecret(string protectedText)
        => _credentialProtector != null && !string.IsNullOrEmpty(protectedText)
            ? _credentialProtector.Unprotect(protectedText)
            : protectedText;

    private static OrgPaymentGatewayPublicStatusDto ToPublicStatus(OrgPaymentGatewaySettings? settings)
    {
        if (settings == null)
            return new OrgPaymentGatewayPublicStatusDto();

        return new OrgPaymentGatewayPublicStatusDto
        {
            HasRazorpayAccount = settings.HasRazorpayAccount,
            KeyId = settings.KeyId,
            HasStoredKeySecret = !string.IsNullOrWhiteSpace(settings.EncryptedKeySecret),
            HasStoredWebhookSecret = !string.IsNullOrWhiteSpace(settings.EncryptedWebhookSecret),
            Environment = settings.Environment,
            Status = settings.Status,
            IsActive = settings.IsActive,
            LastValidatedAt = settings.LastValidatedAt,
            ValidationMessage = settings.ValidationMessage,
            ConfiguredBy = settings.ConfiguredBy,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private Guid RequireTenantId()
    {
        if (_tenantContext.TenantId == Guid.Empty)
            throw new UnauthorizedAccessException("Tenant context is required.");
        return _tenantContext.TenantId;
    }

    private async Task<(Guid TenantId, Guid OrganizationId, bool IsOrgAdmin, bool IsBranchAdmin, List<Guid> AllowedBranches)> RequireStaffFinancialScopeAsync(
        Guid? branchId,
        CancellationToken ct)
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required.");

        var tenantId = RequireTenantId();
        var orgId = _userContext.OrganizationId
            ?? throw new UnauthorizedAccessException("Active organization context is required.");

        var (isOrgAdmin, isBranchAdmin, allowedBranches) = await GetCurrentFinancialScopeAsync(ct);
        if (!isOrgAdmin && !isBranchAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins or authorized Branch Admins can manage fees and invoices.");

        if (!isOrgAdmin && branchId.HasValue && !allowedBranches.Contains(branchId.Value))
            throw new UnauthorizedAccessException("You are not authorized to access financial records for this branch.");

        return (tenantId, orgId, isOrgAdmin, isBranchAdmin, allowedBranches);
    }

    private async Task EnsureBranchAccessIfRestrictedAsync(Guid? branchId, CancellationToken ct)
    {
        var (isOrgAdmin, isBranchAdmin, allowedBranches) = await GetCurrentFinancialScopeAsync(ct);
        if (isOrgAdmin) return;
        if (!isBranchAdmin || !branchId.HasValue || !allowedBranches.Contains(branchId.Value))
            throw new UnauthorizedAccessException("Branch Admin access is restricted to authorized branches only.");
    }

    private async Task<Guid> ValidateParentChildAccessAsync(Guid childId, CancellationToken ct)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Parent authentication is required.");

        var tenantId = _tenantContext.TenantId;
        var children = await _parentRepository.GetAuthorizedChildrenAsync(tenantId, _userContext.UserId, ct);
        var match = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new UnauthorizedAccessException("You do not have access to this child's financial records.");

        return match.TenantId != Guid.Empty ? match.TenantId : tenantId;
    }
}
