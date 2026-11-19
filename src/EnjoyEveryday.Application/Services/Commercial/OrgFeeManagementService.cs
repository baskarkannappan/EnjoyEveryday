using System.Text.Json;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class OrgFeeManagementService : IOrgFeeManagementService
{
    private readonly IOrgFeeRepository _feeRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IChildRepository _childRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IAccessScopeRepository _accessScopeRepository;
    private readonly IParentRepository _parentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IRazorpayGateway? _razorpayGateway;
    private readonly ICredentialProtector? _credentialProtector;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OrgFeeManagementService(
        IOrgFeeRepository feeRepository,
        IBillingRepository billingRepository,
        IOrganizationRepository organizationRepository,
        IChildRepository childRepository,
        IClassroomRepository classroomRepository,
        IAccessScopeRepository accessScopeRepository,
        IParentRepository parentRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IRazorpayGateway? razorpayGateway = null,
        ICredentialProtector? credentialProtector = null)
    {
        _feeRepository = feeRepository;
        _billingRepository = billingRepository;
        _organizationRepository = organizationRepository;
        _childRepository = childRepository;
        _classroomRepository = classroomRepository;
        _accessScopeRepository = accessScopeRepository;
        _parentRepository = parentRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _razorpayGateway = razorpayGateway;
        _credentialProtector = credentialProtector;
    }

    public async Task<(bool IsOrgAdmin, bool IsBranchAdmin, List<Guid> AllowedBranchIds)> GetCurrentFinancialScopeAsync(CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated)
            return (false, false, new List<Guid>());

        var tenantId = RequireTenantId();
        var orgId = _userContext.OrganizationId;

        var roles = new HashSet<string>(_userContext.Roles, StringComparer.OrdinalIgnoreCase);
        if (orgId.HasValue && orgId.Value != Guid.Empty && _userContext.UserId != Guid.Empty)
        {
            var orgRoles = await _accessScopeRepository.GetOrganizationRoleNamesAsync(tenantId, orgId.Value, _userContext.UserId, ct);
            foreach (var r in orgRoles) roles.Add(r);
        }

        bool hasOrgPermission = _userContext.Permissions.Contains(Permissions.OrganizationManage, StringComparer.OrdinalIgnoreCase);
        bool hasBranchPermission = _userContext.Permissions.Contains(Permissions.BranchManage, StringComparer.OrdinalIgnoreCase);

        bool isOrgAdmin = hasOrgPermission
            || roles.Contains("PlatformAdmin")
            || roles.Contains("TenantOwner")
            || roles.Contains("OrganizationOwner")
            || roles.Contains("OrganizationAdmin")
            || roles.Contains("Org Admin")
            || roles.Contains("Daycare Admin");

        bool isBranchAdmin = !isOrgAdmin && (hasBranchPermission
            || roles.Contains("BranchAdmin")
            || roles.Contains("Branch Admin")
            || roles.Contains("Branch Director"));

        var allowedBranches = new List<Guid>();
        if (isBranchAdmin && _userContext.UserId != Guid.Empty && orgId.HasValue && orgId.Value != Guid.Empty)
        {
            var branchIds = await _accessScopeRepository.GetAssignedBranchIdsAsync(tenantId, orgId.Value, _userContext.UserId, ct);
            allowedBranches = branchIds.ToList();
        }

        return (isOrgAdmin, isBranchAdmin, allowedBranches);
    }

    public async Task<List<OrgFeePlan>> GetFeePlansAsync(CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        return await _feeRepository.GetFeePlansAsync(tenantId, orgId, ct);
    }

    public async Task<OrgFeePlan> UpsertFeePlanAsync(UpsertFeePlanRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Fee plan name is required.", nameof(request.Name));

        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can create or modify organization fee plans.");

        var org = await _organizationRepository.GetByIdAsync(orgId, ct);
        var currency = !string.IsNullOrWhiteSpace(request.Currency)
            ? request.Currency.Trim().ToUpperInvariant()
            : (!string.IsNullOrWhiteSpace(org?.Currency) ? org.Currency.ToUpperInvariant() : "INR");

        var plan = new OrgFeePlan
        {
            Id = request.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = orgId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            FeeCategory = string.IsNullOrWhiteSpace(request.FeeCategory) ? "Tuition" : request.FeeCategory.Trim(),
            BillingFrequency = string.IsNullOrWhiteSpace(request.BillingFrequency) ? FeeBillingFrequencies.Monthly : request.BillingFrequency.Trim(),
            Currency = currency,
            IsActive = request.IsActive,
            AllowPartialPayment = request.AllowPartialPayment,
            MinimumPartialPayment = request.MinimumPartialPayment.HasValue ? Math.Round(Math.Max(0, request.MinimumPartialPayment.Value), 2) : null,
            AllowParentCashDeclaration = request.AllowParentCashDeclaration,
            CreatedBy = _userContext.Email ?? "Org Admin"
        };

        var saved = await _feeRepository.UpsertFeePlanAsync(plan, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = request.Id.HasValue ? "FeePlan.Updated" : "FeePlan.Created",
            EntityType = nameof(OrgFeePlan),
            EntityId = saved.Id,
            Details = new { saved.OrganizationId, saved.Name, saved.FeeCategory, saved.BillingFrequency, saved.Currency, saved.IsActive }
        }, ct);

        return saved;
    }

    public async Task<bool> ToggleFeePlanActiveAsync(Guid planId, bool isActive, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can activate or deactivate fee plans.");

        var updated = await _feeRepository.SetFeePlanActiveAsync(tenantId, orgId, planId, isActive, ct);
        if (updated)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = isActive ? "FeePlan.Activated" : "FeePlan.Deactivated",
                EntityType = nameof(OrgFeePlan),
                EntityId = planId,
                Details = new { OrganizationId = orgId, IsActive = isActive }
            }, ct);
        }
        return updated;
    }

    public async Task<List<OrgTariffRule>> GetTariffRulesAsync(Guid? feePlanId = null, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, allowedBranches) = await RequireStaffFinancialScopeAsync(null, ct);
        var rules = await _feeRepository.GetTariffRulesAsync(tenantId, orgId, feePlanId, ct);
        if (isOrgAdmin) return rules;
        return rules.Where(r => !r.BranchId.HasValue || allowedBranches.Contains(r.BranchId.Value)).ToList();
    }

    public async Task<OrgTariffRule> UpsertTariffRuleAsync(UpsertTariffRuleRequest request, CancellationToken ct = default)
    {
        if (request.FeePlanId == Guid.Empty)
            throw new ArgumentException("A fee plan must be selected.", nameof(request.FeePlanId));
        if (request.Amount < 0)
            throw new ArgumentException("Tariff amount cannot be negative.", nameof(request.Amount));

        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(request.BranchId, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can define tariff layer plans and prices.");

        var org = await _organizationRepository.GetByIdAsync(orgId, ct);
        var currency = !string.IsNullOrWhiteSpace(request.Currency)
            ? request.Currency.Trim().ToUpperInvariant()
            : (!string.IsNullOrWhiteSpace(org?.Currency) ? org.Currency.ToUpperInvariant() : "INR");

        var rule = new OrgTariffRule
        {
            Id = request.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = orgId,
            FeePlanId = request.FeePlanId,
            ScopeType = request.ScopeType,
            BranchId = request.BranchId,
            ClassroomId = request.ClassroomId,
            ChildId = request.ChildId,
            LocationTier = request.LocationTier?.Trim(),
            ChildCategory = request.ChildCategory?.Trim(),
            Amount = Math.Round(request.Amount, 2),
            Currency = currency,
            EffectiveDate = request.EffectiveDate.Date,
            ExpiryDate = request.ExpiryDate?.Date,
            IsActive = request.IsActive,
            Notes = request.Notes?.Trim(),
            CreatedBy = _userContext.Email ?? "Administrator"
        };

        var saved = await _feeRepository.UpsertTariffRuleAsync(rule, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = request.Id.HasValue ? "TariffRule.Updated" : "TariffRule.Created",
            EntityType = nameof(OrgTariffRule),
            EntityId = saved.Id,
            Details = new { saved.OrganizationId, saved.FeePlanId, saved.ScopeType, saved.BranchId, saved.ClassroomId, saved.ChildId, saved.LocationTier, saved.Amount, saved.Currency }
        }, ct);

        return saved;
    }

    public async Task<bool> ToggleTariffRuleActiveAsync(Guid ruleId, bool isActive, CancellationToken ct = default)
    {
        var (tenantId, orgId, isOrgAdmin, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        if (!isOrgAdmin)
            throw new UnauthorizedAccessException("Only Organization Admins can toggle tariff rule status.");

        var updated = await _feeRepository.SetTariffRuleActiveAsync(tenantId, orgId, ruleId, isActive, ct);
        if (updated)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                UserId = _userContext.UserId,
                Action = isActive ? "TariffRule.Activated" : "TariffRule.Deactivated",
                EntityType = nameof(OrgTariffRule),
                EntityId = ruleId,
                Details = new { OrganizationId = orgId, IsActive = isActive }
            }, ct);
        }
        return updated;
    }

    public async Task<ResolvedTariffResult?> ResolveChildTariffAsync(
        Guid? branchId,
        Guid? classroomId,
        Guid? childId,
        string? locationTier,
        string? billingFrequency,
        DateTime? asOfDate = null,
        CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(branchId, ct);
        return await _feeRepository.ResolveChildTariffAsync(
            tenantId,
            orgId,
            branchId,
            classroomId,
            childId,
            locationTier,
            billingFrequency,
            (asOfDate ?? DateTime.UtcNow).Date,
            ct);
    }

    public async Task<FamilyBillingContract> AssignChildFeeAsync(AssignChildFeeRequest request, CancellationToken ct = default)
    {
        if (request.ChildId == Guid.Empty)
            throw new ArgumentException("ChildId is required.", nameof(request.ChildId));
        if (request.AgreedAmount < 0)
            throw new ArgumentException("Agreed fee amount cannot be negative.", nameof(request.AgreedAmount));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(request.BranchId, ct);
        var child = await _childRepository.GetByIdAsync(tenantId, request.ChildId, ct)
            ?? throw new KeyNotFoundException("Child record was not found.");

        Guid? effectiveBranchId = request.BranchId;
        if (!effectiveBranchId.HasValue && child.ClassroomId.HasValue)
        {
            var room = await _classroomRepository.GetByIdAsync(tenantId, child.ClassroomId.Value, ct);
            effectiveBranchId = room?.BranchId;
        }
        await EnsureBranchAccessIfRestrictedAsync(effectiveBranchId, ct);

        var org = await _organizationRepository.GetByIdAsync(orgId, ct);
        var currency = !string.IsNullOrWhiteSpace(org?.Currency) ? org.Currency.ToUpperInvariant() : "INR";

        decimal baseRate = Math.Round(request.AgreedAmount, 2);
        Guid? matchedRuleId = request.TariffRuleId;
        Guid? matchedPlanId = request.FeePlanId;

        if (string.Equals(request.FeeMethod, FeeMethods.TariffBased, StringComparison.OrdinalIgnoreCase))
        {
            var resolved = await _feeRepository.ResolveChildTariffAsync(
                tenantId,
                orgId,
                effectiveBranchId,
                request.ClassroomId ?? child.ClassroomId,
                child.Id,
                locationTier: null,
                billingFrequency: request.FeePeriod,
                asOfDate: request.StartDate.Date,
                ct);

            if (resolved != null)
            {
                baseRate = resolved.Amount;
                matchedRuleId = resolved.RuleId;
                matchedPlanId = resolved.FeePlanId;
            }
        }
        else
        {
            // Flat-Rate Fee: independent of tariffs; do not substitute tariff calculation
            matchedRuleId = null;
        }

        var discount = Math.Clamp(request.DiscountPercent, 0, 100);
        var effectiveRate = Math.Round(baseRate * (1m - discount / 100m), 2);

        var contract = new FamilyBillingContract
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = orgId,
            BranchId = effectiveBranchId,
            ApplicationId = request.ApplicationId,
            ChildId = child.Id,
            ParentName = string.IsNullOrWhiteSpace(request.ParentName) ? "Parent Guardian" : request.ParentName.Trim(),
            ParentEmail = request.ParentEmail?.Trim() ?? string.Empty,
            ScheduleType = request.ScheduleType,
            BillingCycle = request.FeePeriod,
            BaseRate = baseRate,
            DiscountPercent = discount,
            EffectiveRate = effectiveRate,
            DepositHeld = 0,
            Status = "Active",
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate?.Date,
            AutoInvoice = request.AutoInvoice && !string.Equals(request.FeePeriod, FeeBillingFrequencies.OneTime, StringComparison.OrdinalIgnoreCase),
            FeeMethod = string.Equals(request.FeeMethod, FeeMethods.FlatRate, StringComparison.OrdinalIgnoreCase)
                ? FeeMethods.FlatRate
                : FeeMethods.TariffBased,
            FeePlanId = matchedPlanId,
            TariffRuleId = matchedRuleId,
            FeePeriod = request.FeePeriod,
            PaymentArrangement = request.PaymentArrangement,
            Currency = currency,
            Remarks = request.Remarks?.Trim(),
            AllowParentCashDeclaration = request.AllowParentCashDeclaration,
            MinimumPartialPayment = request.MinimumPartialPayment
        };

        var saved = await _feeRepository.UpsertFamilyContractAsync(contract, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = saved.FeeMethod == FeeMethods.FlatRate ? "ChildFee.FlatRateAssigned" : "ChildFee.TariffAssigned",
            EntityType = nameof(FamilyBillingContract),
            EntityId = saved.Id,
            Details = new
            {
                saved.OrganizationId,
                saved.BranchId,
                saved.ChildId,
                saved.FeeMethod,
                saved.FeePeriod,
                saved.BaseRate,
                saved.EffectiveRate,
                saved.PaymentArrangement,
                saved.Remarks
            }
        }, ct);

        return saved;
    }

    public async Task<FamilyBillingContract> AdjustChildFlatRateAsync(AdjustChildFlatRateRequest request, CancellationToken ct = default)
    {
        if (request.NewAmount < 0)
            throw new ArgumentException("Adjusted amount cannot be negative.", nameof(request.NewAmount));
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("An adjustment reason is required for audit compliance.", nameof(request.Reason));

        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        var contract = await _feeRepository.GetFamilyContractByIdAsync(tenantId, request.ContractId, ct)
            ?? throw new KeyNotFoundException("Fee contract not found.");

        if (contract.OrganizationId.HasValue && contract.OrganizationId.Value != orgId)
            throw new UnauthorizedAccessException("Cannot adjust a contract belonging to another organization.");

        await EnsureBranchAccessIfRestrictedAsync(contract.BranchId, ct);

        var previousAmount = contract.EffectiveRate;
        var newAmount = Math.Round(request.NewAmount, 2);
        var delta = newAmount - previousAmount;

        contract.BaseRate = newAmount;
        contract.EffectiveRate = newAmount;
        contract.DiscountPercent = 0;

        var updated = await _feeRepository.UpsertFamilyContractAsync(contract, ct);

        await _feeRepository.RecordChildFeeAdjustmentAsync(new ChildFeeAdjustment
        {
            TenantId = tenantId,
            OrganizationId = orgId,
            BranchId = contract.BranchId,
            ChildId = contract.ChildId,
            ContractId = contract.Id,
            AdjustmentType = "FlatRateChange",
            PreviousAmount = previousAmount,
            NewAmount = newAmount,
            AdjustmentDelta = delta,
            Reason = request.Reason.Trim(),
            EffectiveDate = request.EffectiveDate.Date,
            AdjustedByUserId = _userContext.UserId,
            AdjustedByEmail = _userContext.Email ?? "Administrator"
        }, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "ChildFee.FlatRateAdjusted",
            EntityType = nameof(FamilyBillingContract),
            EntityId = contract.Id,
            Details = new
            {
                OrganizationId = orgId,
                contract.BranchId,
                contract.ChildId,
                PreviousAmount = previousAmount,
                NewAmount = newAmount,
                request.Reason,
                request.EffectiveDate
            }
        }, ct);

        return updated;
    }

    public async Task<List<FamilyBillingContract>> GetChildContractsAsync(Guid childId, CancellationToken ct = default)
    {
        var (tenantId, _, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        return await _feeRepository.GetFamilyContractsForChildAsync(tenantId, childId, ct);
    }

    public async Task<List<ChildFeeAdjustment>> GetChildFeeAdjustmentsAsync(Guid childId, CancellationToken ct = default)
    {
        var (tenantId, orgId, _, _, _) = await RequireStaffFinancialScopeAsync(null, ct);
        return await _feeRepository.GetChildFeeAdjustmentsAsync(tenantId, orgId, childId, ct);
    }

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
