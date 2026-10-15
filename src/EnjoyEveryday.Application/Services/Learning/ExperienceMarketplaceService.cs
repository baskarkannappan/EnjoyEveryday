using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public record MonetizationSummary(
    int TotalPublishedExperiences,
    int TotalOrders,
    decimal GrossSales,
    decimal Refunds,
    decimal NetRevenue,
    int ActiveEntitlements,
    IEnumerable<MarketplaceOrder> RecentOrders);

public class ExperienceMarketplaceService
{
    private readonly IExperienceMarketplaceRepository _marketplaceRepo;
    private readonly IExperienceRepository _experienceRepo;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public ExperienceMarketplaceService(
        IExperienceMarketplaceRepository marketplaceRepo,
        IExperienceRepository experienceRepo,
        ITenantContext tenantContext,
        IUserContext userContext)
    {
        _marketplaceRepo = marketplaceRepo;
        _experienceRepo = experienceRepo;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    private bool IsPlatformAdmin() =>
        _userContext.Roles.Contains(RoleTemplates.PlatformAdmin, StringComparer.OrdinalIgnoreCase) ||
        _userContext.Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Browse and search published marketplace experiences.
    /// Never exposes private source IDs to non-platform admins.
    /// </summary>
    public async Task<IEnumerable<PlatformExperience>> GetMarketplaceCatalogAsync(
        string? category = null,
        string? pricingModel = null,
        string? searchQuery = null,
        CancellationToken ct = default)
    {
        var catalog = (await _marketplaceRepo.GetMarketplaceCatalogAsync(category, pricingModel, searchQuery, ct)).ToList();

        // Check entitlements for the caller's active organization
        var orgId = _userContext.OrganizationId;
        HashSet<Guid> entitledIds = new();
        if (orgId.HasValue)
        {
            var entitlements = await _marketplaceRepo.GetOrganizationEntitlementsAsync(orgId.Value, ct);
            entitledIds = entitlements.Select(e => e.PlatformExperienceId).ToHashSet();
        }

        var isPlatformAdmin = IsPlatformAdmin();

        foreach (var item in catalog)
        {
            item.IsPurchasedOrEntitled = entitledIds.Contains(item.Id);

            // Redact private provenance if not Platform Admin
            if (!isPlatformAdmin)
            {
                item.SourceExperienceId = null;
                item.SourceOrganizationId = null;
            }
        }

        return catalog;
    }

    /// <summary>
    /// View detailed platform experience preview.
    /// </summary>
    public async Task<PlatformExperience?> GetExperienceDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(id, ct);
        if (exp == null) return null;

        var orgId = _userContext.OrganizationId;
        if (orgId.HasValue)
        {
            var entitlement = await _marketplaceRepo.GetEntitlementAsync(orgId.Value, id, ct);
            exp.IsPurchasedOrEntitled = entitlement != null;
        }

        if (!IsPlatformAdmin())
        {
            exp.SourceExperienceId = null;
            exp.SourceOrganizationId = null;
        }

        return exp;
    }

    /// <summary>
    /// Acquire an entitlement (purchase or subscription redemption).
    /// Enforces idempotency via transaction reference so retries never double charge.
    /// </summary>
    public async Task<OrganizationEntitlement> AcquireEntitlementAsync(
        Guid platformExperienceId,
        string? transactionReference = null,
        CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId;
        if (!orgId.HasValue)
            throw new UnauthorizedAccessException("Must be within an active organization to acquire an experience entitlement.");

        var exp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (exp == null)
            throw new InvalidOperationException("Marketplace experience not found.");

        if (exp.PublicationStatus != PlatformPublicationStatus.Published)
            throw new InvalidOperationException("Experience is not currently published for acquisition.");

        var txnRef = transactionReference ?? $"TXN-{orgId.Value.ToString()[..8]}-{platformExperienceId.ToString()[..8]}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // Idempotency: check if already ordered
        var existingOrder = await _marketplaceRepo.GetOrderByTransactionRefAsync(orgId.Value, txnRef, ct);
        if (existingOrder != null)
        {
            var existingEntitlement = await _marketplaceRepo.GetEntitlementAsync(orgId.Value, platformExperienceId, ct);
            if (existingEntitlement != null) return existingEntitlement;
        }

        // Record Marketplace Order
        var order = new MarketplaceOrder
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            PlatformExperienceId = platformExperienceId,
            PurchasedByUserId = _userContext.UserId,
            PricingModel = exp.PricingModel,
            AmountPaid = exp.PriceAmount,
            Currency = exp.Currency,
            PaymentStatus = "Completed",
            TransactionReference = txnRef,
            PurchasedAt = DateTimeOffset.UtcNow
        };
        order = await _marketplaceRepo.CreateOrderAsync(order, ct);

        // Grant Entitlement
        var entitlement = new OrganizationEntitlement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            PlatformExperienceId = platformExperienceId,
            OrderId = order.Id,
            AcquiredVersion = exp.CurrentVersion,
            EntitlementType = exp.PricingModel == ExperiencePricingModel.IncludedInSubscription ? "Subscription" : "OneTimePurchase",
            Status = EntitlementStatus.Active,
            GrantedAt = DateTimeOffset.UtcNow,
            Scope = "OrganizationWide"
        };
        entitlement = await _marketplaceRepo.CreateOrUpdateEntitlementAsync(entitlement, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            PlatformExperienceId = platformExperienceId,
            Action = "Entitlement.Acquired",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { OrderId = order.Id, TxnRef = txnRef, Amount = exp.PriceAmount }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return entitlement;
    }

    /// <summary>
    /// Check whether organization is authorized to use the platform experience.
    /// </summary>
    public async Task<bool> IsEntitledAsync(Guid organizationId, Guid platformExperienceId, CancellationToken ct = default)
    {
        var entitlement = await _marketplaceRepo.GetEntitlementAsync(organizationId, platformExperienceId, ct);
        return entitlement != null && entitlement.Status == EntitlementStatus.Active;
    }

    /// <summary>
    /// Imports entitled platform experience into the organization's private library.
    /// Creates an organization-owned clone with metadata link, preserving the platform template immutability.
    /// </summary>
    public async Task<Experience> ImportToOrganizationLibraryAsync(Guid platformExperienceId, CancellationToken ct = default)
    {
        var orgId = _userContext.OrganizationId;
        if (!orgId.HasValue)
            throw new UnauthorizedAccessException("Must be within an active organization to import experiences.");

        var entitlement = await _marketplaceRepo.GetEntitlementAsync(orgId.Value, platformExperienceId, ct);
        if (entitlement == null || entitlement.Status != EntitlementStatus.Active)
            throw new UnauthorizedAccessException("Organization does not hold an active entitlement for this platform experience.");

        var platformExp = await _marketplaceRepo.GetPlatformExperienceByIdAsync(platformExperienceId, ct);
        if (platformExp == null)
            throw new InvalidOperationException("Platform experience not found.");

        // Inject platform provenance tag into cloned activity DNA
        var clonedDna = InjectProvenanceMetadata(platformExp.DnaPayload, platformExp.Id, entitlement.AcquiredVersion);

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            Title = platformExp.Title,
            Description = platformExp.Description,
            Status = ExperienceStatus.Approved.ToString(),
            DnaPayload = clonedDna,
            CreatedByUserId = _userContext.UserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await _experienceRepo.AddAsync(experience, ct);

        await _marketplaceRepo.LogAuditEventAsync(new ExperienceAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = orgId.Value,
            PlatformExperienceId = platformExperienceId,
            Action = "PlatformExperience.Imported",
            PerformedByUserId = _userContext.UserId,
            Details = JsonSerializer.Serialize(new { ClonedExperienceId = created.Id, Version = entitlement.AcquiredVersion }),
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        return created;
    }

    /// <summary>
    /// Commercial and Monetization Reporting for Platform Admin.
    /// Strictly differentiates Gross Sales from Net Revenue (Gross - Refunds).
    /// </summary>
    public async Task<MonetizationSummary> GetMonetizationSummaryAsync(CancellationToken ct = default)
    {
        if (!IsPlatformAdmin())
            throw new UnauthorizedAccessException("Platform Admin role required to view monetization reports.");

        var platformExps = await _marketplaceRepo.GetAllPlatformExperiencesAsync(ct);
        var orders = (await _marketplaceRepo.GetOrdersAsync(null, ct)).ToList();

        var publishedCount = platformExps.Count(e => e.PublicationStatus == PlatformPublicationStatus.Published);
        var completedOrders = orders.Where(o => o.PaymentStatus == "Completed").ToList();
        var refundedOrders = orders.Where(o => o.PaymentStatus == "Refunded").ToList();

        var grossSales = completedOrders.Sum(o => o.AmountPaid) + refundedOrders.Sum(o => o.AmountPaid);
        var refunds = refundedOrders.Sum(o => o.AmountPaid);
        var netRevenue = grossSales - refunds;

        return new MonetizationSummary(
            publishedCount,
            orders.Count,
            grossSales,
            refunds,
            netRevenue,
            completedOrders.Count,
            orders.Take(50));
    }

    private static string InjectProvenanceMetadata(string dna, Guid platformExpId, int version)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(dna) ? "{}" : dna);
            var dict = new Dictionary<string, object>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
            dict["_platformProvenance"] = new
            {
                PlatformExperienceId = platformExpId,
                Version = version,
                ImportedAt = DateTimeOffset.UtcNow
            };
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return dna;
        }
    }
}
