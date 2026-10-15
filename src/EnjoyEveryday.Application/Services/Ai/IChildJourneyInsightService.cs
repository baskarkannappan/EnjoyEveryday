using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IChildJourneyInsightService
{
    Task<ChildJourneyInsightRequest> GenerateJourneyInsightsAsync(
        GenerateJourneyInsightInput input, 
        CancellationToken cancellationToken = default);

    Task<ChildJourneyInsightRequest?> GetLatestInsightForChildAsync(
        Guid childId, 
        CancellationToken cancellationToken = default);
}
