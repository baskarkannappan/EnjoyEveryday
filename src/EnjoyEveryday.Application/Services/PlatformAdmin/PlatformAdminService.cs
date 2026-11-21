using System.Collections.Concurrent;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class PlatformAdminService : IPlatformAdminService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IPlatformAdminRepository? _platformAdminRepository;
    private readonly IEmailNotificationService? _emailNotificationService;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<PlatformAdminService> _logger;

    // Synchronized stores for commercial, lifecycle, billing, and audit records
    private static readonly ConcurrentDictionary<Guid, SubscriptionPlan> _plans = new();
    private static readonly ConcurrentDictionary<Guid, Tariff> _tariffs = new();
    private static readonly ConcurrentDictionary<Guid, PlatformOrganizationDto> _organizations = new();
    private static readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();
    private static readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private static readonly ConcurrentDictionary<Guid, ParentCommunication> _communications = new();
    private static readonly List<PlatformAuditLog> _auditLogs = new();
    private static readonly object _lock = new();

    static PlatformAdminService()
    {
        InitializeSeedData();
    }

    public PlatformAdminService(
        IOrganizationRepository organizationRepository,
        IBranchRepository branchRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        ILogger<PlatformAdminService> logger,
        IPlatformAdminRepository? platformAdminRepository = null,
        IEmailNotificationService? emailNotificationService = null,
        IConfiguration? configuration = null)
    {
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _logger = logger;
        _platformAdminRepository = platformAdminRepository;
        _emailNotificationService = emailNotificationService;
        _configuration = configuration;
    }


    #region Overview & Telemetry

    public Task<PlatformDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var orgs = _organizations.Values.ToList();
        var invoices = _invoices.Values.ToList();
        var payments = _payments.Values.ToList();

        var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();
        var pendingPayments = payments.Where(p => !p.IsVerified).ToList();

        var summary = new PlatformDashboardSummary
        {
            TotalOrganizations = orgs.Count,
            ActiveOrganizations = orgs.Count(o => o.AccountStatus == AccountStatus.Active),
            PendingApprovals = orgs.Count(o => o.AccountStatus == AccountStatus.PendingApproval || o.AccountStatus == AccountStatus.Onboarding),
            SuspendedOrganizations = orgs.Count(o => o.AccountStatus == AccountStatus.Suspended),
            ActiveSubscriptions = orgs.Count(o => o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial),
            AvailablePlansCount = _plans.Values.Count(p => p.IsActive),
            OutstandingBalance = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue),
            OverdueInvoicesCount = overdueInvoices.Count,
            OverdueAmount = overdueInvoices.Sum(i => i.BalanceDue),
            PaymentsAwaitingVerificationCount = pendingPayments.Count,
            CollectionsThisMonth = payments.Where(p => p.IsVerified && p.PaymentDate >= DateTimeOffset.UtcNow.AddDays(-30)).Sum(p => p.Amount),
            Currency = "INR"
        };

        return Task.FromResult(summary);
    }

    public async Task<List<PlatformOrganizationDto>> GetOrganizationsOverviewAsync(
        string? search = null,
        string? accountStatus = null,
        string? financialStatus = null,
        string? lifecycleStage = null,
        CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                var dbRows = await _platformAdminRepository.GetOrganizationsOverviewAsync(cancellationToken);
                if (dbRows.Any())
                {
                    foreach (var r in dbRows)
                    {
                        Guid orgId = r.Id;
                        if (!_organizations.ContainsKey(orgId))
                        {
                            _organizations[orgId] = new PlatformOrganizationDto
                            {
                                Id = orgId,
                                TenantId = r.TenantId,
                                Name = r.Name,
                                Location = r.Location ?? "Daycare Center",
                                BranchCount = r.BranchCount > 0 ? r.BranchCount : 1,
                                AccountStatus = Enum.TryParse<AccountStatus>(r.AccountStatus, true, out var parsedStatus)
                                    ? parsedStatus
                                    : (r.IsActive ? AccountStatus.Active : AccountStatus.Suspended),
                                FinancialStatus = FinancialStatus.Reconciled,
                                PlanId = r.PlanId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                                PlanName = !string.IsNullOrWhiteSpace(r.PlanName) ? r.PlanName : "Bloom (Growth)",
                                PricingRegionId = r.PricingRegionId,
                                PricingRegionCode = r.PricingRegionCode,
                                PricingRegionName = r.PricingRegionName,
                                TariffId = r.TariffId,
                                TariffName = r.TariffName ?? string.Empty,
                                MonthlyRate = r.MonthlyRate > 0 ? r.MonthlyRate : 299.00m,
                                NextBillingDate = DateTimeOffset.UtcNow.AddDays(15),
                                Currency = !string.IsNullOrWhiteSpace(r.Currency) ? r.Currency : "INR",
                                AdminEmail = !string.IsNullOrWhiteSpace(r.AdminEmail) ? r.AdminEmail : $"admin@{r.Name.ToLowerInvariant().Replace(" ", "").Replace("·", "")}.com",
                                LifecycleStageKey = "Active",
                                CreatedAt = r.CreatedAt != null ? (DateTimeOffset)r.CreatedAt.Value : DateTimeOffset.UtcNow.AddMonths(-1)
                            };
                        }
                        else
                        {
                            var existing = _organizations[orgId];
                            existing.Name = r.Name;
                            if (!string.IsNullOrWhiteSpace(r.Location)) existing.Location = r.Location;
                            if (r.BranchCount > 0) existing.BranchCount = r.BranchCount;
                            if (!string.IsNullOrWhiteSpace(r.AdminEmail)) existing.AdminEmail = r.AdminEmail;
                            if (r.PricingRegionId.HasValue) existing.PricingRegionId = r.PricingRegionId;
                            if (!string.IsNullOrWhiteSpace(r.PricingRegionCode)) existing.PricingRegionCode = r.PricingRegionCode;
                            if (!string.IsNullOrWhiteSpace(r.PricingRegionName)) existing.PricingRegionName = r.PricingRegionName;
                            if (r.PlanId.HasValue) existing.PlanId = r.PlanId.Value;
                            if (!string.IsNullOrWhiteSpace(r.PlanName)) existing.PlanName = r.PlanName;
                            if (r.TariffId.HasValue) existing.TariffId = r.TariffId;
                            if (!string.IsNullOrWhiteSpace(r.TariffName)) existing.TariffName = r.TariffName;
                            if (r.MonthlyRate > 0) existing.MonthlyRate = r.MonthlyRate;
                            if (!string.IsNullOrWhiteSpace(r.Currency)) existing.Currency = r.Currency;
                            if (Enum.TryParse<AccountStatus>(r.AccountStatus, true, out var parsedExistingStatus))
                            {
                                existing.AccountStatus = parsedExistingStatus;
                            }
                            else
                            {
                                existing.AccountStatus = r.IsActive ? AccountStatus.Active : AccountStatus.Suspended;
                            }
                        }
                    }

                    var validIds = dbRows.Select(r => r.Id).ToHashSet();
                    var dbFiltered = _organizations.Values.Where(o => validIds.Contains(o.Id)).AsEnumerable();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.Trim();
                        dbFiltered = dbFiltered.Where(o => o.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                                              || (o.Location != null && o.Location.Contains(s, StringComparison.OrdinalIgnoreCase))
                                              || o.AdminEmail.Contains(s, StringComparison.OrdinalIgnoreCase));
                    }

                    if (!string.IsNullOrWhiteSpace(accountStatus) && accountStatus != "All")
                    {
                        if (Enum.TryParse<AccountStatus>(accountStatus, true, out var status))
                        {
                            dbFiltered = dbFiltered.Where(o => o.AccountStatus == status);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(financialStatus) && financialStatus != "All")
                    {
                        if (Enum.TryParse<FinancialStatus>(financialStatus, true, out var status))
                        {
                            dbFiltered = dbFiltered.Where(o => o.FinancialStatus == status);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(lifecycleStage) && lifecycleStage != "All")
                    {
                        dbFiltered = dbFiltered.Where(o => string.Equals(o.LifecycleStageKey, lifecycleStage, StringComparison.OrdinalIgnoreCase));
                    }

                    return dbFiltered.OrderBy(o => o.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load organizations from database, falling back to static store.");
            }
        }

        var query = _organizations.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(o => o.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                                  || (o.Location != null && o.Location.Contains(s, StringComparison.OrdinalIgnoreCase))
                                  || o.AdminEmail.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(accountStatus) && accountStatus != "All")
        {
            if (Enum.TryParse<AccountStatus>(accountStatus, true, out var status))
            {
                query = query.Where(o => o.AccountStatus == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(financialStatus) && financialStatus != "All")
        {
            if (Enum.TryParse<FinancialStatus>(financialStatus, true, out var status))
            {
                query = query.Where(o => o.FinancialStatus == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(lifecycleStage) && lifecycleStage != "All")
        {
            query = query.Where(o => string.Equals(o.LifecycleStageKey, lifecycleStage, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(o => o.CreatedAt).ToList();
    }

    public Task<PlatformOrganizationDto?> GetOrganizationDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        return Task.FromResult(org);
    }

    public Task<PlatformHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PlatformHealthStatus());
    }

    public Task<List<PlatformAuditLog>> GetAuditLogsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_auditLogs.OrderByDescending(a => a.Timestamp).Take(limit).ToList());
        }
    }

    public async Task<List<PlatformBranchDto>> GetAllBranchesAsync(CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                var dbBranches = await _platformAdminRepository.GetAllBranchesAsync(cancellationToken);
                if (dbBranches.Any())
                {
                    return dbBranches.Select(b => new PlatformBranchDto
                    {
                        Id = b.Id,
                        OrganizationId = b.OrganizationId,
                        Name = b.Name,
                        Location = b.Location,
                        IsActive = b.IsActive,
                        OrganizationName = b.OrganizationName,
                        EffectiveLocation = b.EffectiveLocation,
                        ClassroomsCount = b.ClassroomsCount,
                        ChildrenCount = b.ChildrenCount
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load branches from database; falling back to organization projection.");
            }
        }

        // Fallback: project from existing organizations
        var fallbackList = new List<PlatformBranchDto>();
        foreach (var org in _organizations.Values)
        {
            int count = Math.Max(1, org.BranchCount);
            for (int i = 1; i <= count; i++)
            {
                string bName = i == 1 ? "Main Campus" : (i == 2 ? "Annex Campus" : $"Campus {i}");
                fallbackList.Add(new PlatformBranchDto
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org.Id,
                    Name = bName,
                    Location = org.Location,
                    IsActive = org.AccountStatus == AccountStatus.Active,
                    OrganizationName = org.Name,
                    EffectiveLocation = org.Location ?? "Location unspecified",
                    ClassroomsCount = 3,
                    ChildrenCount = 30
                });
            }
        }
        return fallbackList;
    }

    public async Task<PlatformBranchDto> CreateBranchAsync(Guid organizationId, string name, string? location, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            var quota = await _platformAdminRepository.GetOrganizationQuotaUsageAsync(organizationId, cancellationToken);
            if (quota != null && !quota.CanAddBranch)
            {
                throw new PlanQuotaExceededException("Branches", quota.CurrentBranches, quota.MaxBranches, quota.PlanName);
            }

            try
            {
                var added = await _platformAdminRepository.AddBranchAsync(organizationId, name, location, cancellationToken);
                if (_organizations.TryGetValue(organizationId, out var org))
                {
                    org.BranchCount++;
                }
                return new PlatformBranchDto
                {
                    Id = added.Id,
                    OrganizationId = added.OrganizationId,
                    Name = added.Name,
                    Location = added.Location,
                    IsActive = added.IsActive,
                    OrganizationName = added.OrganizationName,
                    EffectiveLocation = added.EffectiveLocation,
                    ClassroomsCount = added.ClassroomsCount,
                    ChildrenCount = added.ChildrenCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create branch '{BranchName}' in database for organization {OrganizationId}", name, organizationId);
                throw;
            }
        }

        var orgName = _organizations.TryGetValue(organizationId, out var o) ? o.Name : "Organization";
        if (o != null) o.BranchCount++;

        return new PlatformBranchDto
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Location = location,
            IsActive = true,
            OrganizationName = orgName,
            EffectiveLocation = location ?? "Location unspecified",
            ClassroomsCount = 0,
            ChildrenCount = 0
        };
    }

    public async Task<List<PlatformAdminUserDto>> GetOrganizationAdminsAsync(CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                var dbAdmins = await _platformAdminRepository.GetOrganizationAdminsAsync(cancellationToken);
                if (dbAdmins.Any())
                {
                    return dbAdmins.Select(a =>
                    {
                        var email = a.AdminEmail;
                        var name = a.AdminName;

                        if (_organizations.TryGetValue(a.OrganizationId, out var org) && !string.IsNullOrWhiteSpace(org.AdminEmail))
                        {
                            email = org.AdminEmail;
                        }

                        if (string.IsNullOrWhiteSpace(name) || name.Equals(a.OrganizationName, StringComparison.OrdinalIgnoreCase) || name == "Admin" || name == "Daycare Director")
                        {
                            name = DeriveDisplayNameFromEmail(email);
                        }

                        return new PlatformAdminUserDto
                        {
                            UserId = a.UserId,
                            AdminName = name,
                            AdminEmail = email,
                            OrganizationId = a.OrganizationId,
                            OrganizationName = a.OrganizationName,
                            RoleName = a.RoleName,
                            AccountStatus = a.AccountStatus,
                            FirstName = !string.IsNullOrWhiteSpace(a.FirstName) ? a.FirstName : (name.Split(' ').FirstOrDefault() ?? ""),
                            LastName = !string.IsNullOrWhiteSpace(a.LastName) ? a.LastName : (name.Contains(' ') ? string.Join(" ", name.Split(' ').Skip(1)) : "")
                        };
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load organization admins from database; falling back to in-memory projection.");
            }
        }

        // Fallback: project from organizations
        return _organizations.Values.Select(o => new PlatformAdminUserDto
        {
            UserId = Guid.NewGuid(),
            AdminName = DeriveDisplayNameFromEmail(o.AdminEmail),
            AdminEmail = o.AdminEmail,
            OrganizationId = o.Id,
            OrganizationName = o.Name,
            RoleName = "Organization Owner",
            AccountStatus = o.AccountStatus.ToString(),
            FirstName = DeriveDisplayNameFromEmail(o.AdminEmail).Split(' ').FirstOrDefault() ?? "",
            LastName = DeriveDisplayNameFromEmail(o.AdminEmail).Contains(' ') ? string.Join(" ", DeriveDisplayNameFromEmail(o.AdminEmail).Split(' ').Skip(1)) : ""
        }).OrderBy(a => a.OrganizationName).ToList();
    }

    public async Task<int> GetPlatformAdminCountAsync(CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                return await _platformAdminRepository.GetPlatformAdminCountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query platform admin count from database.");
            }
        }
        return 1;
    }

    public async Task CreateAdminUserAsync(string email, string firstName, string lastName, string password, string roleName, Guid? orgId = null, CancellationToken cancellationToken = default)
    {
        if (roleName.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) || roleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var currentCount = await GetPlatformAdminCountAsync(cancellationToken);
            if (currentCount >= 3)
            {
                throw new InvalidOperationException("A maximum of 3 platform administrators can exist at any point in time. Please remove an existing platform administrator before creating a new one.");
            }
        }

        if (_platformAdminRepository != null)
        {
            await _platformAdminRepository.CreateAdminUserAsync(email, firstName, lastName, password, roleName, orgId, cancellationToken);
        }
    }

    public async Task UpdateAdminUserAsync(Guid userId, string email, string firstName, string lastName, string? password, string roleName, string accountStatus, Guid? orgId = null, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            await _platformAdminRepository.UpdateAdminUserAsync(userId, email, firstName, lastName, password, roleName, accountStatus, orgId, cancellationToken);
        }
    }

    public async Task DeleteAdminUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            await _platformAdminRepository.DeleteAdminUserAsync(userId, cancellationToken);
        }
    }

    private static string DeriveDisplayNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Daycare Director";
        var localPart = email.Split('@')[0];
        var parts = localPart.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !int.TryParse(p, out _))
            .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : ""))
            .ToList();

        return parts.Count > 0 ? string.Join(" ", parts) : "Daycare Director";
    }

    #endregion

    #region Organization CRUD

    public async Task<PlatformOrganizationDto> CreateOrganizationWorkflowAsync(
        string name,
        string location,
        Guid planId,
        string adminEmail,
        int branchCount = 1,
        Guid? tariffId = null,
        Guid? pricingRegionId = null,
        string currency = "INR",
        CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(planId, out var plan);
        plan ??= _plans.Values.First();
        var resolvedCurrency = !string.IsNullOrWhiteSpace(currency) ? currency.ToUpperInvariant() : "INR";

        PricingRegion? region = null;
        if (pricingRegionId.HasValue && _platformAdminRepository != null)
        {
            var allRegions = await _platformAdminRepository.GetPricingRegionsAsync(cancellationToken);
            region = allRegions.FirstOrDefault(r => r.Id == pricingRegionId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(location) && _platformAdminRepository != null)
        {
            region = await _platformAdminRepository.ResolvePricingRegionByLocationAsync(location, cancellationToken);
        }

        Tariff? tariff = null;
        if (tariffId.HasValue)
        {
            _tariffs.TryGetValue(tariffId.Value, out tariff);
        }

        // Check if there is a regional tariff matching this region and plan
        decimal monthlyRate = plan.BasePrice;
        string tariffName = "Standard Monthly";
        if (region != null && region.Tariffs.Count > 0)
        {
            var regTariff = region.Tariffs.FirstOrDefault(t => t.PlanId == plan.Id && (tariffId == null || t.Id == tariffId));
            if (regTariff != null)
            {
                monthlyRate = regTariff.Price;
                tariffName = regTariff.Name;
            }
        }
        else if (tariff != null)
        {
            monthlyRate = tariff.Price;
            tariffName = tariff.Name;
        }

        var id = Guid.NewGuid();

        var dto = new PlatformOrganizationDto
        {
            Id = id,
            TenantId = id,
            Name = name,
            Location = location,
            BranchCount = branchCount > 0 ? branchCount : 1,
            AccountStatus = AccountStatus.Onboarding,
            FinancialStatus = FinancialStatus.Unbilled,
            PlanId = plan.Id,
            PlanName = plan.Name,
            PricingRegionId = region?.Id,
            PricingRegionCode = region?.Code,
            PricingRegionName = region?.Name,
            TariffId = tariffId ?? tariff?.Id,
            TariffName = tariffName,
            MonthlyRate = monthlyRate,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = resolvedCurrency,
            AdminEmail = adminEmail,
            LifecycleStageKey = "Onboarding",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _organizations[id] = dto;

        // Persist to relational database so Daycare Web and Api can discover and select this organization
        if (_platformAdminRepository != null)
        {
            try
            {
                await _platformAdminRepository.ProvisionOrganizationAsync(id, name, location, adminEmail, dto.BranchCount, cancellationToken);

                if (region != null)
                {
                    await _platformAdminRepository.AssignRegionalPlanToOrganizationAsync(id, region.Id, plan.Id, dto.TariffId, resolvedCurrency, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist new organization to database; continuing with platform in-memory store.");
            }
        }

        AddAuditLog("Organization Registered", $"New daycare organization '{name}' provisioned with plan '{plan.Name}' in region '{region?.Name ?? "Standard"}' ({monthlyRate:C0}/mo).", "Info", id);

        // Automatically dispatch onboarding invitation email with 1-tap activation token
        _ = SendOrganizationInvitationWorkflowAsync(id, adminEmail, cancellationToken);

        return dto;
    }


    public async Task<bool> UpdateOrganizationAsync(
        Guid id,
        string name,
        string? location,
        string adminEmail,
        int branchCount,
        CancellationToken cancellationToken = default)
    {
        var safeBranchCount = branchCount > 0 ? branchCount : 1;

        if (_organizations.TryGetValue(id, out var org))
        {
            org.Name = name;
            org.Location = location;
            org.AdminEmail = adminEmail;
            org.BranchCount = safeBranchCount;
        }
        else
        {
            _organizations[id] = new PlatformOrganizationDto
            {
                Id = id,
                TenantId = id,
                Name = name,
                Location = location ?? "Daycare Center",
                BranchCount = safeBranchCount,
                AdminEmail = adminEmail,
                AccountStatus = AccountStatus.Active,
                FinancialStatus = FinancialStatus.Reconciled,
                PlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PlanName = "Bloom (Growth)",
                MonthlyRate = 299.00m,
                NextBillingDate = DateTimeOffset.UtcNow.AddDays(15),
                LifecycleStageKey = "Active",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        if (_platformAdminRepository != null)
        {
            try
            {
                await _platformAdminRepository.UpdateOrganizationDetailsAsync(id, name, location, adminEmail, safeBranchCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist updated organization to database; updated in-memory store.");
            }
        }

        AddAuditLog("Organization Updated", $"Organization '{name}' updated (Location: '{location}', Branches: {safeBranchCount}, Admin: '{adminEmail}').", "Info", id);
        return true;
    }

    public async Task<bool> UpdateAccountStatusAsync(Guid organizationId, AccountStatus newStatus, string reason, CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(organizationId, out var org))
        {
            var oldStatus = org.AccountStatus;
            org.AccountStatus = newStatus;

            // Sync lifecycle stage key
            org.LifecycleStageKey = newStatus switch
            {
                AccountStatus.Onboarding => "Onboarding",
                AccountStatus.PendingApproval => "Onboarding",
                AccountStatus.Trial => "Active",
                AccountStatus.Active => "Active",
                AccountStatus.Suspended => "Invoiced",
                AccountStatus.Deactivated => "Invoiced",
                _ => "Active"
            };

            if (_platformAdminRepository != null)
            {
                try
                {
                    await _platformAdminRepository.UpdateOrganizationAccountStatusAsync(organizationId, newStatus.ToString(), reason, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist organization status update for org {OrganizationId}", organizationId);
                }
            }

            AddAuditLog("Account Status Changed", $"Organization '{org.Name}' transitioned from {oldStatus} to {newStatus}. Reason: {reason}", "Info", organizationId);

            if (_emailNotificationService != null && !string.IsNullOrWhiteSpace(org.AdminEmail))
            {
                var daycareUrl = GetDaycarePortalUrl();
                _ = _emailNotificationService.SendOrganizationLifecycleUpdateAsync(
                    org.AdminEmail,
                    org.Name,
                    newStatus.ToString(),
                    reason,
                    daycareUrl,
                    org.TenantId,
                    cancellationToken);
            }

            return true;
        }
        else if (_platformAdminRepository != null)
        {
            try
            {
                await _platformAdminRepository.UpdateOrganizationAccountStatusAsync(organizationId, newStatus.ToString(), reason, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist organization status update for org {OrganizationId}", organizationId);
            }
        }
        return false;
    }

    public Task<bool> DeactivateOrganizationAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default)
    {
        return UpdateAccountStatusAsync(organizationId, AccountStatus.Suspended, $"Deactivated by platform administrator: {reason}", cancellationToken);
    }

    #endregion

}
