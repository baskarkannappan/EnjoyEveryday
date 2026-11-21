using System.Text.Json;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public partial class OrgFeeManagementService : IOrgFeeManagementService
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

}
