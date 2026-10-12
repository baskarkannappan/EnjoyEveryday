using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IOrganizationIntelligenceService
{
    Task<OrganizationIntelligenceReport> GetIntelligenceReportAsync(
        OrganizationIntelligenceFilter filter, 
        CancellationToken cancellationToken = default);

    Task<OrganizationIntelligenceReport> GenerateInsightsAsync(
        OrganizationIntelligenceFilter filter, 
        CancellationToken cancellationToken = default);
}
