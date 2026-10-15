using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IActivityRecommendationRepository
{
    Task<ActivityRecommendationRequest> CreateRequestAsync(ActivityRecommendationRequest request, IEnumerable<ActivityRecommendationItem> items, CancellationToken cancellationToken = default);
    Task<ActivityRecommendationRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityRecommendationItem>> GetItemsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<ActivityRecommendationDecision> RecordDecisionAsync(ActivityRecommendationDecision decision, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityRecommendationDecision>> GetDecisionsByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);
}
