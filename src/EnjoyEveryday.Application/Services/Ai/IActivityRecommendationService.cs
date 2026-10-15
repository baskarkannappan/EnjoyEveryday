using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public record ActivityRecommendationResult(
    ActivityRecommendationRequest Request,
    List<ActivityRecommendationItem> Items
);

public interface IActivityRecommendationService
{
    Task<ActivityRecommendationResult> GenerateRecommendationsAsync(
        Guid classroomId,
        Guid? childId = null,
        RecommendationPreferencesDto? preferences = null,
        CancellationToken cancellationToken = default);

    Task<ActivityRecommendationDecision> AcceptRecommendationAsync(
        Guid requestId,
        Guid recommendationItemId,
        DateOnly scheduledDate,
        string timeOfDay,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ActivityRecommendationDecision> RejectRecommendationAsync(
        Guid requestId,
        Guid recommendationItemId,
        string? rejectionReason = null,
        CancellationToken cancellationToken = default);
}
