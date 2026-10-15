using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IChildJourneyInsightRepository
{
    Task<ChildJourneyInsightRequest> CreateRequestAsync(ChildJourneyInsightRequest request, IEnumerable<ChildJourneyInsightItem> items, CancellationToken cancellationToken = default);
    Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<ChildJourneyInsightRequest?> GetRequestByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildJourneyInsightItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default);
}
