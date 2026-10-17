using System.Collections.Concurrent;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IDbConnectionFactory? _dbConnectionFactory;
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
        IDbConnectionFactory? dbConnectionFactory = null)
    {
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _logger = logger;
        _dbConnectionFactory = dbConnectionFactory;
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
        if (_dbConnectionFactory != null)
        {
            try
            {
                using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                const string dbSql = @"
                    SELECT o.id, o.tenant_id, o.name, o.location, o.is_active, o.created_at,
                           (SELECT COUNT(*) FROM branches b WHERE b.organization_id = o.id) AS branch_count
                    FROM organizations o
                    ORDER BY o.name";
                var dbRows = (await conn.QueryAsync(dbSql)).ToList();
                if (dbRows.Any())
                {
                    foreach (var r in dbRows)
                    {
                        Guid orgId = r.id;
                        if (!_organizations.ContainsKey(orgId))
                        {
                            _organizations[orgId] = new PlatformOrganizationDto
                            {
                                Id = orgId,
                                TenantId = r.tenant_id,
                                Name = r.name,
                                Location = r.location ?? "Daycare Center",
                                BranchCount = (int)(r.branch_count ?? 1),
                                AccountStatus = r.is_active ? AccountStatus.Active : AccountStatus.Suspended,
                                FinancialStatus = FinancialStatus.Reconciled,
                                PlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                                PlanName = "Bloom (Growth)",
                                MonthlyRate = 299.00m,
                                NextBillingDate = DateTimeOffset.UtcNow.AddDays(15),
                                AdminEmail = $"admin@{((string)r.name).ToLowerInvariant().Replace(" ", "").Replace("·", "")}.com",
                                LifecycleStageKey = "Active",
                                CreatedAt = r.created_at != null ? (DateTimeOffset)r.created_at : DateTimeOffset.UtcNow.AddMonths(-1)
                            };
                        }
                    }

                    var validIds = dbRows.Select(r => (Guid)r.id).ToHashSet();
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

    #endregion

    #region Organization CRUD

    public async Task<PlatformOrganizationDto> CreateOrganizationWorkflowAsync(
        string name,
        string location,
        Guid planId,
        string adminEmail,
        int branchCount = 1,
        Guid? tariffId = null,
        CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(planId, out var plan);
        plan ??= _plans.Values.First();

        Tariff? tariff = null;
        if (tariffId.HasValue)
        {
            _tariffs.TryGetValue(tariffId.Value, out tariff);
        }
        tariff ??= plan.Tariffs.FirstOrDefault();

        var id = Guid.NewGuid();
        var monthlyRate = tariff != null ? tariff.Price : plan.BasePrice;

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
            TariffId = tariff?.Id,
            TariffName = tariff?.Name ?? "Standard Monthly",
            MonthlyRate = monthlyRate,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(30),
            AdminEmail = adminEmail,
            LifecycleStageKey = "Onboarding",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _organizations[id] = dto;

        // Persist to relational database so Daycare Web and Api can discover and select this organization
        if (_dbConnectionFactory != null)
        {
            try
            {
                using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                
                // Check if admin email belongs to an existing user
                var existingUser = await conn.QuerySingleOrDefaultAsync<(Guid Id, Guid TenantId)?>(
                    "SELECT id as Id, tenant_id as TenantId FROM users WHERE LOWER(email) = LOWER(@Email) OR normalized_email = UPPER(@Email) LIMIT 1",
                    new { Email = adminEmail.Trim() });

                Guid orgTenantId;
                Guid? adminUserId = null;

                if (existingUser.HasValue)
                {
                    orgTenantId = existingUser.Value.TenantId;
                    adminUserId = existingUser.Value.Id;
                }
                else
                {
                    orgTenantId = id;
                    var rawSlug = System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
                    var slug = string.IsNullOrWhiteSpace(rawSlug) ? $"org-{id.ToString()[..8]}" : rawSlug;

                    var existingSlugCount = await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM tenants WHERE slug = @Slug", 
                        new { Slug = slug });

                    if (existingSlugCount > 0)
                    {
                        slug = $"{slug}-{id.ToString()[..4]}";
                    }

                    await conn.ExecuteAsync(@"
                        INSERT INTO tenants (id, name, slug) 
                        VALUES (@TenantId, @Name, @Slug) 
                        ON CONFLICT (id) DO NOTHING",
                        new { TenantId = orgTenantId, Name = name, Slug = slug });

                    // Create new admin user
                    adminUserId = Guid.NewGuid();
                    await conn.ExecuteAsync(@"
                        INSERT INTO users (id, tenant_id, email, normalized_email, email_confirmed, first_name, last_name, display_name, is_active)
                        VALUES (@UserId, @TenantId, @Email, UPPER(@Email), TRUE, 'Admin', @OrgName, @OrgName, TRUE)
                        ON CONFLICT (normalized_email) DO NOTHING",
                        new { UserId = adminUserId.Value, TenantId = orgTenantId, Email = adminEmail.Trim(), OrgName = name });
                }

                // Insert Organization
                await conn.ExecuteAsync(@"
                    INSERT INTO organizations (id, tenant_id, name, location, is_active)
                    VALUES (@Id, @TenantId, @Name, @Location, TRUE)
                    ON CONFLICT (id) DO NOTHING",
                    new { Id = id, TenantId = orgTenantId, Name = name, Location = location });

                // Insert Default Main Campus Branch
                var branchId = Guid.NewGuid();
                await conn.ExecuteAsync(@"
                    INSERT INTO branches (id, tenant_id, organization_id, name, location, is_active)
                    VALUES (@BranchId, @TenantId, @OrgId, 'Main Campus', @Location, TRUE)
                    ON CONFLICT (id) DO NOTHING",
                    new { BranchId = branchId, TenantId = orgTenantId, OrgId = id, Location = location });

                // Link admin user membership to this organization
                if (adminUserId.HasValue)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO organization_memberships (user_id, organization_id, tenant_id, status)
                        VALUES (@UserId, @OrgId, @TenantId, 'Active')
                        ON CONFLICT (user_id, organization_id) DO NOTHING",
                        new { UserId = adminUserId.Value, OrgId = id, TenantId = orgTenantId });

                    // Assign OrganizationAdmin role
                    var roleId = await conn.ExecuteScalarAsync<Guid?>(
                        @"SELECT id FROM roles 
                          WHERE name IN ('OrganizationAdmin', 'OrganizationOwner') 
                            AND (tenant_id = @TenantId OR tenant_id IS NULL) 
                          ORDER BY tenant_id NULLS LAST LIMIT 1",
                        new { TenantId = orgTenantId });

                    if (roleId.HasValue)
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO user_organization_roles (user_id, role_id, organization_id, tenant_id)
                            VALUES (@UserId, @RoleId, @OrgId, @TenantId)
                            ON CONFLICT (user_id, role_id, organization_id) DO NOTHING",
                            new { UserId = adminUserId.Value, RoleId = roleId.Value, OrgId = id, TenantId = orgTenantId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist new organization to database; continuing with platform in-memory store.");
            }
        }

        AddAuditLog("Organization Registered", $"New daycare organization '{name}' provisioned with plan '{plan.Name}' ({monthlyRate:C0}/mo).", "Info", id);

        return dto;
    }


    public Task<bool> UpdateOrganizationAsync(
        Guid id,
        string name,
        string? location,
        string adminEmail,
        int branchCount,
        CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(id, out var org))
        {
            var oldName = org.Name;
            org.Name = name;
            org.Location = location;
            org.AdminEmail = adminEmail;
            org.BranchCount = branchCount > 0 ? branchCount : 1;

            AddAuditLog("Organization Updated", $"Organization '{oldName}' updated (Name: '{name}', Location: '{location}', Branches: {branchCount}).", "Info", id);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> UpdateAccountStatusAsync(Guid organizationId, AccountStatus newStatus, string reason, CancellationToken cancellationToken = default)
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

            AddAuditLog("Account Status Changed", $"Organization '{org.Name}' transitioned from {oldStatus} to {newStatus}. Reason: {reason}", "Info", organizationId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DeactivateOrganizationAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default)
    {
        return UpdateAccountStatusAsync(organizationId, AccountStatus.Suspended, $"Deactivated by platform administrator: {reason}", cancellationToken);
    }

    #endregion

    #region Plan CRUD

    public Task<List<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var plans = _plans.Values.AsEnumerable();
        if (!includeInactive)
        {
            plans = plans.Where(p => p.IsActive);
        }

        foreach (var p in plans)
        {
            p.Tariffs = _tariffs.Values.Where(t => t.PlanId == p.Id && t.IsActive).OrderBy(t => t.Price).ToList();
        }

        return Task.FromResult(plans.OrderBy(p => p.BasePrice).ToList());
    }

    public Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.Tariffs = _tariffs.Values.Where(t => t.PlanId == plan.Id && t.IsActive).OrderBy(t => t.Price).ToList();
            return Task.FromResult<SubscriptionPlan?>(plan);
        }
        return Task.FromResult<SubscriptionPlan?>(null);
    }

    public Task<SubscriptionPlan> CreatePlanAsync(
        string name,
        string code,
        string description,
        decimal basePrice,
        int maxBranches,
        int maxClassrooms,
        int maxChildren,
        bool includesAi,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = id,
            Name = name,
            Code = code.ToUpperInvariant().Trim(),
            Description = description,
            BasePrice = basePrice,
            MaxBranches = maxBranches,
            MaxClassrooms = maxClassrooms,
            MaxChildren = maxChildren,
            IncludesAiFeatures = includesAi,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Create a default monthly tariff for this plan
        var tariffId = Guid.NewGuid();
        var defaultTariff = new Tariff
        {
            Id = tariffId,
            PlanId = id,
            Name = "Standard Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = basePrice,
            DiscountPercentage = 0,
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _tariffs[tariffId] = defaultTariff;
        plan.Tariffs.Add(defaultTariff);
        _plans[id] = plan;

        AddAuditLog("Plan Created", $"New subscription plan '{name}' ({code}) created at {basePrice:C0}/mo.", "Info", id);

        return Task.FromResult(plan);
    }

    public Task<bool> UpdatePlanAsync(
        Guid planId,
        string name,
        string description,
        decimal basePrice,
        int maxBranches,
        int maxClassrooms,
        int maxChildren,
        bool includesAi,
        CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.Name = name;
            plan.Description = description;
            plan.BasePrice = basePrice;
            plan.MaxBranches = maxBranches;
            plan.MaxClassrooms = maxClassrooms;
            plan.MaxChildren = maxChildren;
            plan.IncludesAiFeatures = includesAi;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Plan Updated", $"Subscription plan '{name}' updated (Price: {basePrice:C0}, AI: {includesAi}).", "Info", planId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DeactivatePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.IsActive = false;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Plan Deactivated", $"Subscription plan '{plan.Name}' archived/deactivated.", "Warning", planId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    #endregion

    #region Tariff CRUD

    public Task<List<Tariff>> GetTariffsByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var tariffs = _tariffs.Values.Where(t => t.PlanId == planId && t.IsActive).OrderBy(t => t.Price).ToList();
        return Task.FromResult(tariffs);
    }

    public Task<Tariff> CreateTariffAsync(
        Guid planId,
        string name,
        BillingFrequency frequency,
        decimal price,
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var tariff = new Tariff
        {
            Id = id,
            PlanId = planId,
            Name = name,
            Frequency = frequency,
            Price = price,
            DiscountPercentage = discountPercentage,
            EffectiveDate = DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _tariffs[id] = tariff;

        if (_plans.TryGetValue(planId, out var plan))
        {
            plan.Tariffs.Add(tariff);
            AddAuditLog("Tariff Created", $"New tariff '{name}' ({frequency}: {price:C0}) added to plan '{plan.Name}'.", "Info", id);
        }

        return Task.FromResult(tariff);
    }

    public Task<bool> UpdateTariffAsync(
        Guid tariffId,
        string name,
        BillingFrequency frequency,
        decimal price,
        decimal discountPercentage,
        CancellationToken cancellationToken = default)
    {
        if (_tariffs.TryGetValue(tariffId, out var tariff))
        {
            tariff.Name = name;
            tariff.Frequency = frequency;
            tariff.Price = price;
            tariff.DiscountPercentage = discountPercentage;
            tariff.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Tariff Updated", $"Tariff '{name}' updated to {price:C0} ({frequency}).", "Info", tariffId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DeactivateTariffAsync(Guid tariffId, CancellationToken cancellationToken = default)
    {
        if (_tariffs.TryGetValue(tariffId, out var tariff))
        {
            tariff.IsActive = false;
            tariff.UpdatedAt = DateTimeOffset.UtcNow;

            AddAuditLog("Tariff Deactivated", $"Tariff '{tariff.Name}' deactivated.", "Warning", tariffId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> AssignPlanAndTariffToOrganizationAsync(
        Guid organizationId,
        Guid planId,
        Guid? tariffId,
        CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(organizationId, out var org) && _plans.TryGetValue(planId, out var plan))
        {
            Tariff? tariff = null;
            if (tariffId.HasValue)
            {
                _tariffs.TryGetValue(tariffId.Value, out tariff);
            }
            tariff ??= _tariffs.Values.FirstOrDefault(t => t.PlanId == planId && t.IsActive);

            var oldPlan = org.PlanName;
            org.PlanId = plan.Id;
            org.PlanName = plan.Name;
            org.TariffId = tariff?.Id;
            org.TariffName = tariff?.Name ?? "Standard";
            org.MonthlyRate = tariff != null ? tariff.Price : plan.BasePrice;

            AddAuditLog("Plan/Tariff Changed", $"Organization '{org.Name}' changed from '{oldPlan}' to '{plan.Name}' ({org.TariffName} - {org.MonthlyRate:C0}).", "Info", organizationId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    #endregion

    #region Commercial & Billing Operations

    public Task<List<Invoice>> GetInvoicesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _invoices.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            if (Enum.TryParse<InvoiceStatus>(status, true, out var parsedInvoiceStatus))
            {
                query = query.Where(i => i.Status == parsedInvoiceStatus);
            }
            else if (Enum.TryParse<FinancialStatus>(status, true, out var parsedFinancialStatus))
            {
                query = query.Where(i => i.FinancialStatus == parsedFinancialStatus);
            }
        }
        return Task.FromResult(query.OrderByDescending(i => i.DueDate).ToList());
    }

    public Task<List<Payment>> GetPaymentsAsync(bool? awaitingVerificationOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _payments.Values.AsEnumerable();
        if (awaitingVerificationOnly == true)
        {
            query = query.Where(p => !p.IsVerified);
        }
        return Task.FromResult(query.OrderByDescending(p => p.PaymentDate).ToList());
    }

    public Task<bool> VerifyPaymentWorkflowAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (_payments.TryGetValue(paymentId, out var payment))
        {
            payment.IsVerified = true;
            payment.VerifiedBy = _userContext.UserId != Guid.Empty ? _userContext.UserId.ToString() : "Platform Admin";
            payment.VerifiedAt = DateTimeOffset.UtcNow;

            if (payment.InvoiceId.HasValue && _invoices.TryGetValue(payment.InvoiceId.Value, out var invoice))
            {
                invoice.AmountPaid += payment.Amount;
                if (invoice.BalanceDue <= 0)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                // Update organization financial status
                if (_organizations.TryGetValue(payment.OrganizationId, out var org))
                {
                    org.FinancialStatus = invoice.FinancialStatus;
                }
            }

            AddAuditLog("Payment Verified", $"Payment {payment.PaymentReference} of {payment.Amount:C0} reconciled and verified.", "Info", paymentId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Invoice> IssueInvoiceWorkflowAsync(Guid organizationId, decimal amount, string description, CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        var orgName = org?.Name ?? "Organization";

        var id = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = id,
            TenantId = org?.TenantId ?? organizationId,
            OrganizationId = organizationId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}",
            Subtotal = amount,
            DiscountAmount = 0,
            TotalAmount = amount,
            AmountPaid = 0,
            Currency = "INR",
            Status = InvoiceStatus.Unpaid,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Description = description,
            BillingContactEmail = org?.AdminEmail ?? "billing@daycare.com",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _invoices[id] = invoice;

        if (org != null)
        {
            org.FinancialStatus = FinancialStatus.InvoiceDue;
            org.LifecycleStageKey = "Invoiced";
        }

        AddAuditLog("Invoice Issued", $"Invoice {invoice.InvoiceNumber} for {amount:C0} issued to '{orgName}'.", "Info", id);

        return Task.FromResult(invoice);
    }

    public Task<List<ParentCommunication>> GetRecentParentCommunicationsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var list = _communications.Values.OrderByDescending(c => c.CreatedAt).Take(limit).ToList();
        return Task.FromResult(list);
    }

    #endregion

    private static void AddAuditLog(string action, string details, string severity, Guid? entityId = null)
    {
        lock (_lock)
        {
            _auditLogs.Insert(0, new PlatformAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Actor = "Platform Superadmin",
                Action = action,
                EntityType = "Platform",
                EntityId = entityId,
                Details = details,
                Severity = severity
            });
            if (_auditLogs.Count > 100)
            {
                _auditLogs.RemoveAt(_auditLogs.Count - 1);
            }
        }
    }

    private static void InitializeSeedData()
    {
        // Plans
        var starterPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Sprout (Starter)",
            Code = "PLAN-SPROUT",
            Description = "Ideal for single-branch nurseries and boutique preschools.",
            BasePrice = 149.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 1,
            MaxClassrooms = 3,
            MaxChildren = 35,
            IncludesAiFeatures = false
        };

        var growthPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Bloom (Growth)",
            Code = "PLAN-BLOOM",
            Description = "Comprehensive suite with AI assistants and multiple classrooms.",
            BasePrice = 299.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 2,
            MaxClassrooms = 8,
            MaxChildren = 90,
            IncludesAiFeatures = true
        };

        var canopyPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Canopy (Multi-Branch)",
            Code = "PLAN-CANOPY",
            Description = "Full multi-branch daycare network with organization intelligence.",
            BasePrice = 599.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 6,
            MaxClassrooms = 24,
            MaxChildren = 300,
            IncludesAiFeatures = true
        };

        _plans[starterPlan.Id] = starterPlan;
        _plans[growthPlan.Id] = growthPlan;
        _plans[canopyPlan.Id] = canopyPlan;

        // Tariffs
        var t1 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = starterPlan.Id,
            Name = "Starter Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 149.00m,
            DiscountPercentage = 0,
            IsActive = true
        };
        var t2 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = starterPlan.Id,
            Name = "Starter Annual (Save 15%)",
            Frequency = BillingFrequency.Annual,
            Price = 126.00m,
            DiscountPercentage = 15,
            IsActive = true
        };

        var t3 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = growthPlan.Id,
            Name = "Growth Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 299.00m,
            DiscountPercentage = 0,
            IsActive = true
        };
        var t4 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = growthPlan.Id,
            Name = "Growth Annual (Save 20%)",
            Frequency = BillingFrequency.Annual,
            Price = 239.00m,
            DiscountPercentage = 20,
            IsActive = true
        };

        var t5 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = canopyPlan.Id,
            Name = "Canopy Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 599.00m,
            DiscountPercentage = 0,
            IsActive = true
        };

        _tariffs[t1.Id] = t1;
        _tariffs[t2.Id] = t2;
        _tariffs[t3.Id] = t3;
        _tariffs[t4.Id] = t4;
        _tariffs[t5.Id] = t5;

        starterPlan.Tariffs.AddRange(new[] { t1, t2 });
        growthPlan.Tariffs.AddRange(new[] { t3, t4 });
        canopyPlan.Tariffs.Add(t5);

        // Seed Organizations matching database seeds (002_seed_data.sql)
        var org1Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var org1Tenant = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var org1 = new PlatformOrganizationDto
        {
            Id = org1Id,
            TenantId = org1Tenant,
            Name = "Little Stars Daycare",
            Location = "Toronto · 3 branches",
            BranchCount = 3,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanId = growthPlan.Id,
            PlanName = "Bloom (Growth)",
            TariffId = t3.Id,
            TariffName = "Growth Monthly",
            MonthlyRate = 299.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(18),
            AdminEmail = "admin@littlestars.com",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
        };

        var org2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var org2Tenant = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var org2 = new PlatformOrganizationDto
        {
            Id = org2Id,
            TenantId = org2Tenant,
            Name = "Sunshine Garden Preschool",
            Location = "Toronto · 2 branches",
            BranchCount = 2,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanId = starterPlan.Id,
            PlanName = "Sprout (Starter)",
            TariffId = t1.Id,
            TariffName = "Starter Monthly",
            MonthlyRate = 149.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(22),
            AdminEmail = "daniel@sunshinegarden.com",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3)
        };

        var org3Id = Guid.Parse("20000000-0000-0000-0000-000000000003");
        var org3Tenant = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var org3 = new PlatformOrganizationDto
        {
            Id = org3Id,
            TenantId = org3Tenant,
            Name = "Happy Roots Early Learning",
            Location = "Vancouver · 1 branch",
            BranchCount = 1,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.InvoiceDue,
            PlanId = canopyPlan.Id,
            PlanName = "Canopy (Multi-Branch)",
            TariffId = t5.Id,
            TariffName = "Canopy Monthly",
            MonthlyRate = 599.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(5),
            AdminEmail = "director@happyroots.com",
            LifecycleStageKey = "Invoiced",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-11)
        };

        _organizations[org1Id] = org1;
        _organizations[org2Id] = org2;
        _organizations[org3Id] = org3;

        // Invoices
        var inv1Id = Guid.NewGuid();
        _invoices[inv1Id] = new Invoice
        {
            Id = inv1Id,
            TenantId = org3Tenant,
            OrganizationId = org3Id,
            InvoiceNumber = "INV-202610-8491",
            Subtotal = 599.00m,
            TotalAmount = 599.00m,
            AmountPaid = 0,
            Status = InvoiceStatus.Unpaid,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(5),
            Description = "Canopy Tier Subscription - October 2026",
            BillingContactEmail = "director@happyroots.com"
        };

        var inv2Id = Guid.NewGuid();
        _invoices[inv2Id] = new Invoice
        {
            Id = inv2Id,
            TenantId = org2Tenant,
            OrganizationId = org2Id,
            InvoiceNumber = "INV-202609-3120",
            Subtotal = 149.00m,
            TotalAmount = 149.00m,
            AmountPaid = 0,
            Status = InvoiceStatus.Overdue,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-38),
            DueDate = DateTimeOffset.UtcNow.AddDays(-8),
            Description = "Sprout Tier Subscription - September 2026",
            BillingContactEmail = "daniel@sunshinegarden.com"
        };

        // Payments
        var pay1Id = Guid.NewGuid();
        _payments[pay1Id] = new Payment
        {
            Id = pay1Id,
            TenantId = org1Tenant,
            OrganizationId = org1Id,
            PaymentReference = "PAY-ACH-99410",
            Amount = 299.00m,
            Method = "ACH",
            IsVerified = false,
            PaymentDate = DateTimeOffset.UtcNow.AddDays(-1),
            Status = "Pending Verification",
            TransactionId = "txn_ach_88319041"
        };

        // Seed AI Parent Communications (Grounded, Teacher-Approved)
        var comm1Id = Guid.NewGuid();
        _communications[comm1Id] = new ParentCommunication
        {
            Id = comm1Id,
            TenantId = org1Id,
            OrganizationId = org1Id,
            ChildId = Guid.NewGuid(),
            ChildName = "Aarav Patel",
            ClassroomName = "Bluebirds",
            RecipientName = "Sunita Patel",
            RecipientRelationship = "Mother",
            RecipientEmail = "sunita.patel@test.com",
            CommunicationType = ParentCommunicationTypes.DailyUpdate,
            Title = "Aarav's Day in Bluebirds — Oct 14",
            ContentDraft = "Dear Sunita,\n\nAarav had a wonderful morning of cooperative play. He gently observed a ladybug during our outdoor nature walk and helped his friends build a bridge with wooden blocks.\n\nWarmly,\nRachel",
            ContentFinal = "Dear Sunita,\n\nAarav had a wonderful morning of cooperative play. He gently observed a ladybug during our outdoor nature walk and helped his friends build a bridge with wooden blocks.\n\nWarmly,\nRachel",
            FamilyConversationStarter = "Ask Aarav how the ladybug crawled across the leaf today!",
            Status = ParentCommunicationStatus.Sent,
            TeacherName = "Rachel Green",
            ConsentVerified = true,
            ApprovedAt = DateTimeOffset.UtcNow.AddHours(-3),
            SentAt = DateTimeOffset.UtcNow.AddHours(-3),
            DeliveryChannel = "PortalAndEmail",
            DeliveryStatus = "Delivered",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-4)
        };

        var comm2Id = Guid.NewGuid();
        _communications[comm2Id] = new ParentCommunication
        {
            Id = comm2Id,
            TenantId = org2Id,
            OrganizationId = org2Id,
            ChildId = Guid.NewGuid(),
            ChildName = "Ella Lindqvist",
            ClassroomName = "Pinecones",
            RecipientName = "Karin Lindqvist",
            RecipientRelationship = "Mother",
            RecipientEmail = "karin@nordicpine.edu",
            CommunicationType = ParentCommunicationTypes.WeeklySummary,
            Title = "Ella's Weekly Learning Journey in Pinecones",
            ContentDraft = "Dear Karin,\n\nElla showed great focus this week exploring textures and autumn leaves in our sensory tray. She enjoyed singing rhythmic songs with her peers.\n\nWarmly,\nTeacher Sarah",
            ContentFinal = "Dear Karin,\n\nElla showed great focus this week exploring textures and autumn leaves in our sensory tray. She enjoyed singing rhythmic songs with her peers.\n\nWarmly,\nTeacher Sarah",
            FamilyConversationStarter = "Ask Ella about the crunchy brown leaf she collected yesterday!",
            Status = ParentCommunicationStatus.PendingReview,
            TeacherName = "Sarah Connor",
            ConsentVerified = true,
            ApprovedAt = null,
            SentAt = null,
            DeliveryChannel = "PortalOnly",
            DeliveryStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        // Audit Logs
        AddAuditLog("System Boot", "EnjoyEveryday Platform Administration services initialized.", "Info");
        AddAuditLog("Organization Created", "Sunshine Garden Preschool initiated registration.", "Info", org2Id);
        AddAuditLog("Payment Submitted", "ACH payment PAY-ACH-99410 for $299.00 awaiting administrative reconciliation.", "Warning", pay1Id);
        AddAuditLog("AI Parent Communication Sent", "Daily update draft approved by Teacher Rachel Green and dispatched to parent Sunita Patel.", "Info", comm1Id);
    }
}
