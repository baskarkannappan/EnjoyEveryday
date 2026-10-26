using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ExperienceMarketplaceRepository : IExperienceMarketplaceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExperienceMarketplaceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreatePlatformExperienceParams =
    [
        "Id", "Title", "Description", "Category", "AgeRangeMinMonths", "AgeRangeMaxMonths",
        "DurationMinutes", "LearningObjectives::jsonb", "RequiredMaterials::jsonb", "Instructions", "SafetyNotes",
        "DnaPayload", "PricingModel", "PriceAmount", "Currency", "PublicationStatus", "OwnershipType",
        "CurrentVersion", "LicenseId", "SourceHarvestRequestId", "SourceExperienceId", "SourceOrganizationId",
        "CreatorAttribution", "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateHarvestRequestParams =
    [
        "Id", "Status", "ReviewNotes", "ReviewedByUserId", "ReviewedAt",
        "SafetySuitabilityVerified", "LearningObjectivesVerified", "InstructionsVerified",
        "MaterialsVerified", "MediaRightsVerified", "CommercialRightsVerified"
    ];

    private static readonly string[] UpdatePlatformExperienceParams =
    [
        "Id", "Title", "Description", "Category", "AgeRangeMinMonths", "AgeRangeMaxMonths",
        "DurationMinutes", "Instructions", "SafetyNotes",
        "DnaPayload", "PricingModel", "PriceAmount", "Currency", "PublicationStatus", "OwnershipType",
        "CurrentVersion", "CreatorAttribution"
    ];

    private static readonly string[] LogAuditParams =
    [
        "Id", "TenantId", "OrganizationId", "PlatformExperienceId", "Action", "PerformedByUserId", "Details::jsonb", "CreatedAt"
    ];

    public async Task<ExperienceLicense> CreateLicenseAsync(ExperienceLicense license, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<ExperienceLicense>(
            "fn_create_experience_license",
            license,
            cancellationToken: ct);
        return created ?? license;
    }

    public async Task<ExperienceLicense?> GetActiveLicenseAsync(Guid experienceId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleFunctionAsync<ExperienceLicense>(
            "fn_get_active_experience_license",
            new { ExperienceId = experienceId },
            cancellationToken: ct);
    }

    public async Task<ExperienceHarvestRequest> SubmitHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<ExperienceHarvestRequest>(
            "fn_submit_experience_harvest_request",
            request,
            cancellationToken: ct);
        return created ?? request;
    }

    public async Task<ExperienceHarvestRequest?> GetHarvestRequestByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleFunctionAsync<ExperienceHarvestRequest>(
            "fn_get_experience_harvest_request_by_id",
            new { Id = id },
            cancellationToken: ct);
    }

    public async Task<IEnumerable<ExperienceHarvestRequest>> GetHarvestRequestsAsync(Guid? organizationId = null, string? status = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<ExperienceHarvestRequest>(
            "fn_get_experience_harvest_requests",
            new { OrganizationId = organizationId, Status = status },
            cancellationToken: ct);
    }

    public async Task UpdateHarvestRequestAsync(ExperienceHarvestRequest request, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "sp_update_experience_harvest_request",
            request,
            parameterNames: UpdateHarvestRequestParams,
            cancellationToken: ct);
    }

    public async Task<PlatformExperience> CreatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<PlatformExperience>(
            "fn_create_platform_experience",
            experience,
            parameterNames: CreatePlatformExperienceParams,
            cancellationToken: ct);
        return created ?? experience;
    }

    public async Task<PlatformExperience?> GetPlatformExperienceByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleFunctionAsync<PlatformExperience>(
            "fn_get_platform_experience_by_id",
            new { Id = id },
            cancellationToken: ct);
    }

    public async Task<IEnumerable<PlatformExperience>> GetMarketplaceCatalogAsync(string? category = null, string? pricingModel = null, string? searchQuery = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<PlatformExperience>(
            "fn_get_platform_marketplace_catalog",
            new { Category = category, PricingModel = pricingModel, SearchQuery = searchQuery },
            cancellationToken: ct);
    }

    public async Task<IEnumerable<PlatformExperience>> GetAllPlatformExperiencesAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<PlatformExperience>(
            "fn_get_all_platform_experiences",
            cancellationToken: ct);
    }

    public async Task UpdatePlatformExperienceAsync(PlatformExperience experience, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "sp_update_platform_experience",
            experience,
            parameterNames: UpdatePlatformExperienceParams,
            cancellationToken: ct);
    }

    public async Task<PlatformExperienceVersion> AddPlatformVersionAsync(PlatformExperienceVersion version, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<PlatformExperienceVersion>(
            "fn_add_platform_experience_version",
            version,
            cancellationToken: ct);
        return created ?? version;
    }

    public async Task<IEnumerable<PlatformExperienceVersion>> GetPlatformVersionsAsync(Guid platformExperienceId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<PlatformExperienceVersion>(
            "fn_get_platform_experience_versions",
            new { PlatformExperienceId = platformExperienceId },
            cancellationToken: ct);
    }

    public async Task<MarketplaceOrder> CreateOrderAsync(MarketplaceOrder order, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<MarketplaceOrder>(
            "fn_create_marketplace_order",
            order,
            cancellationToken: ct);
        return created ?? order;
    }

    public async Task<MarketplaceOrder?> GetOrderByTransactionRefAsync(Guid organizationId, string transactionReference, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleFunctionAsync<MarketplaceOrder>(
            "fn_get_marketplace_order_by_transaction_ref",
            new { OrganizationId = organizationId, TransactionReference = transactionReference },
            cancellationToken: ct);
    }

    public async Task<IEnumerable<MarketplaceOrder>> GetOrdersAsync(Guid? organizationId = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<MarketplaceOrder>(
            "fn_get_marketplace_orders",
            new { OrganizationId = organizationId },
            cancellationToken: ct);
    }

    public async Task<OrganizationEntitlement> CreateOrUpdateEntitlementAsync(OrganizationEntitlement entitlement, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var created = await connection.QuerySingleFunctionAsync<OrganizationEntitlement>(
            "fn_create_or_update_organization_entitlement",
            entitlement,
            cancellationToken: ct);
        return created ?? entitlement;
    }

    public async Task<OrganizationEntitlement?> GetEntitlementAsync(Guid organizationId, Guid platformExperienceId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QuerySingleFunctionAsync<OrganizationEntitlement>(
            "fn_get_organization_entitlement",
            new { OrganizationId = organizationId, PlatformExperienceId = platformExperienceId },
            cancellationToken: ct);
    }

    public async Task<IEnumerable<OrganizationEntitlement>> GetOrganizationEntitlementsAsync(Guid organizationId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<OrganizationEntitlement>(
            "fn_get_organization_entitlements",
            new { OrganizationId = organizationId },
            cancellationToken: ct);
    }

    public async Task LogAuditEventAsync(ExperienceAuditEvent auditEvent, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteProcedureAsync(
            "sp_log_experience_audit_event",
            auditEvent,
            parameterNames: LogAuditParams,
            cancellationToken: ct);
    }

    public async Task<IEnumerable<ExperienceAuditEvent>> GetAuditEventsAsync(Guid? organizationId = null, Guid? platformExperienceId = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await connection.QueryFunctionAsync<ExperienceAuditEvent>(
            "fn_get_experience_audit_events",
            new { OrganizationId = organizationId, PlatformExperienceId = platformExperienceId },
            cancellationToken: ct);
    }
}
