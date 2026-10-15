using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IWeeklyPlanningAssistantService
{
    Task<WeeklyPlanningDraftRequest> GenerateWeeklyPlanDraftAsync(
        GenerateWeeklyPlanInput input, 
        CancellationToken cancellationToken = default);

    Task<WeeklyPlanningDraftRequest?> GetLatestDraftForWeekAsync(
        Guid classroomId, 
        DateOnly weekStartDate, 
        CancellationToken cancellationToken = default);

    Task<WeeklyPlanningDraftItem> UpdateDraftItemAsync(
        WeeklyPlanningDraftItem item, 
        CancellationToken cancellationToken = default);

    Task<WeeklyPlanningDraftRequest> SavePlanToExperiencePlanningAsync(
        SubmitWeeklyPlanInput input, 
        CancellationToken cancellationToken = default);
}
