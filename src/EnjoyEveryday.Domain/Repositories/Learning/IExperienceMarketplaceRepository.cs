using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceMarketplaceRepository
{
    // Licenses
    Task<ExperienceLicense> CreateLicenseAsync(ExperienceLicense license, CancellationToken ct = default);
    Task<ExperienceLicense?> GetActiveLicenseAsync(Guid experienceId, CancellationToken ct = default);

    // Harvest Requests
    Task<ExperienceHarvestRequest> SubmitHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default);
    Task<ExperienceHarvestRequest?> GetHarvestRequestByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ExperienceHarvestRequest>> GetHarvestRequestsAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default);
    Task UpdateHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default);

    // Platform Experiences
    Task<PlatformExperience> CreatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default);
    Task<PlatformExperience?> GetPlatformExperienceByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<PlatformExperience>> GetMarketplaceCatalogAsync(string? category = null, string? pricingModel = null, string? searchQuery = null, CancellationToken ct = default);
    Task<IEnumerable<PlatformExperience>> GetAllPlatformExperiencesAsync(CancellationToken ct = default);
    Task UpdatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default);

    // Platform Versions
    Task<PlatformExperienceVersion> AddPlatformVersionAsync(PlatformExperienceVersion version, CancellationToken ct = default);
    Task<IEnumerable<PlatformExperienceVersion>> GetPlatformVersionsAsync(Guid platformExperienceId, CancellationToken ct = default);

    // Orders & Entitlements
    Task<MarketplaceOrder> CreateOrderAsync(MarketplaceOrder order, CancellationToken ct = default);
    Task<MarketplaceOrder?> GetOrderByTransactionRefAsync(Guid organizationId, string transactionReference, CancellationToken ct = default);
    Task<IEnumerable<MarketplaceOrder>> GetOrdersAsync(Guid? organizationId = null, CancellationToken ct = default);
    Task<OrganizationEntitlement> CreateOrUpdateEntitlementAsync(OrganizationEntitlement entitlement, CancellationToken ct = default);
    Task<OrganizationEntitlement?> GetEntitlementAsync(Guid organizationId, Guid platformExperienceId, CancellationToken ct = default);
    Task<IEnumerable<OrganizationEntitlement>> GetOrganizationEntitlementsAsync(Guid organizationId, CancellationToken ct = default);

    // Audit Events
    Task LogAuditEventAsync(ExperienceAuditEvent auditEvent, CancellationToken ct = default);
    Task<IEnumerable<ExperienceAuditEvent>> GetAuditEventsAsync(Guid? organizationId = null, Guid? platformExperienceId = null, CancellationToken ct = default);
}
