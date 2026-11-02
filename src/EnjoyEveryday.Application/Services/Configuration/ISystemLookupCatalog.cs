using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface ISystemLookupCatalog
{
    Task<IReadOnlyList<MasterLookupItem>> GetLookupsByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<MasterLookupItem>> GetAllergensAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MasterLookupItem>> GetDevelopmentDomainsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MasterLookupItem>> GetEventCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MasterLookupItem>> GetIncidentSeveritiesAsync(CancellationToken ct = default);
    void InvalidateCache();
}
