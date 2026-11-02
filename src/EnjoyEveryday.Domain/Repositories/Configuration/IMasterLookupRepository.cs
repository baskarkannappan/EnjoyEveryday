using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IMasterLookupRepository
{
    Task<IReadOnlyList<MasterLookupItem>> GetActiveLookupsAsync(string? category = null, CancellationToken ct = default);
}
