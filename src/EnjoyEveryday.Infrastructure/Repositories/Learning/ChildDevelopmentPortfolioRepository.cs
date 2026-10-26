using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class ChildDevelopmentPortfolioRepository : IChildDevelopmentPortfolioRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChildDevelopmentPortfolioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<PortfolioConsentDocumentRecord?> GetLatestMediaConsentDocumentAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<PortfolioConsentDocumentRecord>(
            "fn_get_latest_media_consent_document",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
    }

    public async Task<List<string>> GetConsentChoicesAsync(Guid tenantId, Guid consentDocumentId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var choices = await conn.QueryFunctionAsync<string>(
            "fn_get_portfolio_consent_choices",
            new { TenantId = tenantId, DocId = consentDocumentId },
            cancellationToken: ct);

        return choices.ToList();
    }

    public async Task<PortfolioChildProfileRecord?> GetChildProfileAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<PortfolioChildProfileRecord>(
            "fn_get_portfolio_child_profile",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);
    }

    public async Task<List<DevelopmentAreaRecord>> GetDevelopmentAreasAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<DevelopmentAreaRecord>(
            "fn_get_portfolio_development_areas",
            new { TenantId = tenantId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<PortfolioJourneyEntryRecord>> GetJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<PortfolioJourneyEntryRecord>(
            "fn_get_portfolio_journey_entries",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<EntryDevelopmentAreaRecord>> GetEntryDevelopmentAreasAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<EntryDevelopmentAreaRecord>(
            "fn_get_portfolio_entry_development_areas",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<EntryEvidenceRecord>> GetEntryEvidenceAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<EntryEvidenceRecord>(
            "fn_get_portfolio_entry_evidence",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<PortfolioMilestoneRecord>> GetMilestonesAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<PortfolioMilestoneRecord>(
            "fn_get_portfolio_milestones",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);

        return results.ToList();
    }

    public async Task<List<PortfolioExperienceParticipationRecord>> GetExperienceParticipationsAsync(Guid tenantId, Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<PortfolioExperienceParticipationRecord>(
            "fn_get_portfolio_experience_participations",
            new { TenantId = tenantId, ChildId = childId },
            cancellationToken: ct);

        return results.ToList();
    }
}
