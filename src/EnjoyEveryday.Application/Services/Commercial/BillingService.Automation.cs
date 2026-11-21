using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class BillingService
{
    public async Task<OrganizationBillingSettings> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var isPlatformAdmin = _userContext.HasPermission(Permissions.PlatformBillingManage);
        if (!isPlatformAdmin)
        {
            await _authorizer.RequireOrganizationAsync(organizationId, Permissions.OrganizationBillingView, ct);
        }

        var settings = await _billingRepository.GetBillingSettingsAsync(organizationId, ct);
        if (settings != null) return settings;

        // Default to Manual mode
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : organizationId;
        return new OrganizationBillingSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            Mode = BillingMode.Manual,
            Frequency = BillingFrequency.Monthly,
            EffectiveDate = DateTimeOffset.UtcNow,
            AutoChargeEnabled = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<OrganizationBillingSettings> UpdateBillingModeAsync(UpdateBillingModeRequest request, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("A valid organization must be specified.", nameof(request.OrganizationId));

        var existing = await _billingRepository.GetBillingSettingsAsync(request.OrganizationId, ct);
        var oldMode = existing?.Mode ?? BillingMode.Manual;

        var tenantId = existing?.TenantId ?? (_tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : request.OrganizationId);

        DateTimeOffset? nextBillingDate = null;
        if (request.Mode == BillingMode.Automatic)
        {
            // If switching to Automatic, schedule next billing date based on effective date
            nextBillingDate = request.EffectiveDate > DateTimeOffset.UtcNow 
                ? request.EffectiveDate 
                : DateTimeOffset.UtcNow.AddMonths(1);
        }

        var settings = new OrganizationBillingSettings
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = request.OrganizationId,
            Mode = request.Mode,
            Frequency = request.Frequency,
            EffectiveDate = request.EffectiveDate.ToUniversalTime(),
            NextBillingDate = nextBillingDate?.ToUniversalTime(),
            AutoChargeEnabled = false,
            Notes = request.Reason,
            UpdatedBy = _userContext.Email ?? "Platform Admin",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _billingRepository.UpsertBillingSettingsAsync(settings, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "BillingMode.Changed",
            EntityType = nameof(OrganizationBillingSettings),
            EntityId = settings.Id,
            Details = new 
            { 
                settings.OrganizationId, 
                OldMode = oldMode.ToString(), 
                NewMode = settings.Mode.ToString(), 
                settings.Frequency,
                settings.EffectiveDate,
                Reason = request.Reason
            }
        }, ct);

        return settings;
    }

    public async Task<AutomaticBillingRunResult> ExecuteAutomaticBillingRunAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        var result = new AutomaticBillingRunResult();
        var dueOrgs = await _billingRepository.GetOrganizationsDueForAutomaticBillingAsync(asOfDate, ct);
        result.ProcessedOrganizations = dueOrgs.Count;

        var periodKey = asOfDate.ToString("yyyy-MM");

        foreach (var orgSetting in dueOrgs)
        {
            try
            {
                // Check if an invoice for this organization and billing period already exists
                var existingInvoices = await _billingRepository.GetInvoicesForOrganizationAsync(orgSetting.TenantId, orgSetting.OrganizationId, includeDrafts: true, ct);
                var alreadyBilled = existingInvoices.Any(i => i.BillingPeriod == periodKey && i.Status != InvoiceStatus.Void);
                if (alreadyBilled)
                {
                    _logger.LogInformation("Organization {OrgId} already has an invoice for period {Period}. Skipping duplicate generation.", orgSetting.OrganizationId, periodKey);
                    continue;
                }

                // Generate automatic subscription invoice
                var org = await _organizationRepository.GetByIdAsync(orgSetting.OrganizationId, ct);
                var resolvedCurrency = !string.IsNullOrWhiteSpace(org?.Currency) ? org.Currency.ToUpperInvariant() : "INR";
                var invoiceNumber = await GenerateUniqueInvoiceNumberAsync(ct);
                var autoInvoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = orgSetting.TenantId,
                    OrganizationId = orgSetting.OrganizationId,
                    InvoiceNumber = invoiceNumber,
                    BillingPeriod = periodKey,
                    Currency = resolvedCurrency,
                    IssueDate = asOfDate,
                    DueDate = asOfDate.AddDays(14),
                    Status = InvoiceStatus.Unpaid,
                    Description = $"Automated Subscription Billing — Period {periodKey}",
                    Notes = "Generated automatically according to active organization subscription schedule.",
                    PaymentInstructions = "Please remit payment via Bank Transfer or ACH within 14 days.",
                    CreatedBy = "System Automated Billing",
                    PublishedAt = asOfDate,
                    PublishedBy = "System Automated Billing",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                // Standard monthly subscription line item
                var lineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = autoInvoice.Id,
                    LineNumber = 1,
                    Description = $"EnjoyEveryday Platform Subscription ({orgSetting.Frequency})",
                    Quantity = 1,
                    UnitPrice = 299.00m, // Baseline standard tariff snapshot
                    Category = "Subscription",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                autoInvoice.LineItems.Add(lineItem);
                autoInvoice.Subtotal = lineItem.LineTotal;
                autoInvoice.TaxAmount = 0;
                autoInvoice.TotalAmount = autoInvoice.Subtotal;

                await _billingRepository.CreateInvoiceAsync(autoInvoice, ct);

                // Advance next billing date
                orgSetting.NextBillingDate = orgSetting.Frequency switch
                {
                    BillingFrequency.Quarterly => asOfDate.AddMonths(3),
                    BillingFrequency.Annual => asOfDate.AddYears(1),
                    _ => asOfDate.AddMonths(1)
                };
                await _billingRepository.UpsertBillingSettingsAsync(orgSetting, ct);

                result.GeneratedInvoices++;
                result.GeneratedInvoiceNumbers.Add(autoInvoice.InvoiceNumber);

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = orgSetting.TenantId,
                    UserId = Guid.Empty,
                    Action = "Invoice.AutoGenerated",
                    EntityType = nameof(Invoice),
                    EntityId = autoInvoice.Id,
                    Details = new { autoInvoice.InvoiceNumber, autoInvoice.BillingPeriod, autoInvoice.TotalAmount, orgSetting.OrganizationId }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to automatically generate invoice for organization {OrgId}", orgSetting.OrganizationId);
                result.Errors.Add($"Org {orgSetting.OrganizationId}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<TuitionBillingRunResult> ExecuteMonthlyTuitionBillingRunAsync(Guid tenantId, DateTimeOffset asOfDate, Guid? branchId = null, CancellationToken ct = default)
    {
        var result = new TuitionBillingRunResult();
        var periodKey = asOfDate.ToString("yyyy-MM");
        var firstDayOfMonth = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var lastDayOfMonth = new DateTime(asOfDate.Year, asOfDate.Month, DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month));
        var daysInMonth = DateTime.DaysInMonth(asOfDate.Year, asOfDate.Month);

        var contracts = await _billingRepository.GetActiveFamilyContractsAsync(tenantId, branchId, ct);
        result.ProcessedContracts = contracts.Count;

        var existingInvoices = await _billingRepository.GetAllInvoicesAsync(tenantId, null, null, ct);

        foreach (var contract in contracts)
        {
            try
            {
                if (!contract.AutoInvoice || !string.Equals(contract.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (contract.StartDate > lastDayOfMonth)
                {
                    continue; // Has not started yet
                }
                if (contract.EndDate.HasValue && contract.EndDate.Value < firstDayOfMonth)
                {
                    continue; // Already ended
                }

                var contractTag = $"Contract:{contract.Id}|Period:{periodKey}";
                var alreadyExists = existingInvoices.Any(i => 
                    i.Status != InvoiceStatus.Void && 
                    ((i.Notes != null && i.Notes.Contains(contractTag)) ||
                     (i.Description != null && i.Description.Contains($"Tuition Billing - {periodKey}") && i.BillingContactEmail == contract.ParentEmail)));

                if (alreadyExists)
                {
                    result.SkippedExisting++;
                    continue;
                }

                var effectiveStartDate = contract.StartDate > firstDayOfMonth ? contract.StartDate.Date : firstDayOfMonth;
                var effectiveEndDate = (contract.EndDate.HasValue && contract.EndDate.Value < lastDayOfMonth) ? contract.EndDate.Value.Date : lastDayOfMonth;
                var activeDays = Math.Max(1, (effectiveEndDate - effectiveStartDate).Days + 1);

                decimal tuitionAmount;
                string lineDesc;
                if (activeDays < daysInMonth)
                {
                    tuitionAmount = Math.Round(contract.EffectiveRate * activeDays / daysInMonth, 2);
                    lineDesc = $"Monthly Tuition ({periodKey}) - Prorated {activeDays}/{daysInMonth} days";
                }
                else
                {
                    tuitionAmount = contract.EffectiveRate;
                    lineDesc = $"Monthly Tuition ({periodKey}) - {contract.ScheduleType}";
                }

                if (string.Equals(contract.FeeMethod, FeeMethods.FlatRate, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(contract.BillingCycle, "Monthly", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Flat-rate agreements are not auto-divided into monthly invoices
                }

                if (tuitionAmount <= 0) continue;

                var orgId = contract.OrganizationId
                    ?? _userContext.OrganizationId
                    ?? Guid.Parse("20000000-0000-0000-0000-000000000001");
                var invoiceNum = $"INV-TUIT-{asOfDate:yyMM}-{new Random().Next(1000, 9999)}";
                var tenantOrg = await _organizationRepository.GetByIdAsync(orgId, ct);
                var tuitionCurrency = !string.IsNullOrWhiteSpace(contract.Currency)
                    ? contract.Currency.ToUpperInvariant()
                    : (!string.IsNullOrWhiteSpace(tenantOrg?.Currency) ? tenantOrg.Currency.ToUpperInvariant() : "INR");

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrganizationId = orgId,
                    BranchId = contract.BranchId,
                    ChildId = contract.ChildId,
                    ContractId = contract.Id,
                    FeeMethod = contract.FeeMethod,
                    InvoiceNumber = invoiceNum,
                    Currency = tuitionCurrency,
                    IssueDate = asOfDate,
                    DueDate = asOfDate.AddDays(7),
                    Status = InvoiceStatus.Unpaid,
                    Subtotal = tuitionAmount,
                    TotalAmount = tuitionAmount,
                    AmountPaid = 0,
                    BillingContactEmail = contract.ParentEmail,
                    Description = $"Tuition Billing - {periodKey} for {contract.ParentName}",
                    Notes = $"Tuition Billing | {contractTag} | Child:{contract.ChildId}. Schedule: {contract.ScheduleType}.",
                    PaymentInstructions = "Please remit tuition payment online via the Parent Portal or at the daycare front desk.",
                    CreatedBy = "Automated Tuition Billing Engine",
                    PublishedAt = asOfDate,
                    PublishedBy = "Automated Tuition Billing Engine",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    LineItems = new List<InvoiceLineItem>
                    {
                        new InvoiceLineItem
                        {
                            Id = Guid.NewGuid(),
                            LineNumber = 1,
                            Description = lineDesc,
                            Quantity = 1,
                            UnitPrice = tuitionAmount,
                            Category = "Tuition",
                            CreatedAt = DateTimeOffset.UtcNow
                        }
                    }
                };

                await _billingRepository.CreateInvoiceAsync(invoice, ct);
                result.InvoicesGenerated++;
                result.TotalAmountBilled += tuitionAmount;

                if (_emailService != null && !string.IsNullOrWhiteSpace(contract.ParentEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(
                                toAddress: contract.ParentEmail,
                                subject: $"New Monthly Tuition Invoice ({invoiceNum}) \u2014 EnjoyEveryday",
                                htmlBody: $"<p>Dear {contract.ParentName},</p><p>Your monthly tuition invoice for <strong>{periodKey}</strong> ({invoiceNum}) has been generated for amount <strong>\u20b9{tuitionAmount:N2}</strong>.</p><p>You can view and securely pay this invoice online via your <a href=\"https://localhost:7005/invoices\">EnjoyEveryday Parent Portal</a>.</p><p>Thank you,<br/>EnjoyEveryday Daycare Management</p>",
                                notificationType: "TuitionBilling",
                                tenantId: tenantId,
                                metadata: new { InvoiceNumber = invoiceNum, Amount = tuitionAmount, Period = periodKey },
                                ct: CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send tuition invoice email to {Email}", contract.ParentEmail);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.ErrorDetails.Add($"Contract {contract.Id} ({contract.ParentName}): {ex.Message}");
                _logger.LogError(ex, "Error processing tuition billing for contract {ContractId}", contract.Id);
            }
        }

        return result;
    }

    private void EnsurePlatformBillingPermission()
    {
        if (!_userContext.HasPermission(Permissions.PlatformBillingManage))
        {
            throw new UnauthorizedAccessException("Unauthorized: Operation requires platform billing management permission.");
        }
    }

    private async Task<string> GenerateUniqueInvoiceNumberAsync(CancellationToken ct)
    {
        for (int i = 0; i < 10; i++)
        {
            var candidate = $"INV-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}";
            var existing = await _billingRepository.GetInvoiceByNumberAsync(candidate, ct);
            if (existing == null)
            {
                return candidate;
            }
        }
        return $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }
}

internal class DefaultFallbackRazorpayGateway : IRazorpayGateway
{
    public RazorpaySettings Settings { get; } = new RazorpaySettings { MockMode = true };

    public Task<RazorpayGatewayOrderResult> CreateOrderAsync(long amountInPaise, string currency, string receipt, Dictionary<string, string>? notes = null, CancellationToken ct = default)
    {
        return Task.FromResult(new RazorpayGatewayOrderResult
        {
            OrderId = "order_fallback_" + Guid.NewGuid().ToString("N")[..8],
            AmountInPaise = amountInPaise,
            Currency = currency,
            Receipt = receipt,
            Status = "created"
        });
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature) => signature == "mock_sig_valid" || signature == "mock_sig_test";
    public bool VerifyWebhookSignature(string rawRequestBody, string signatureHeader) => signatureHeader == "mock_webhook_valid";
}
