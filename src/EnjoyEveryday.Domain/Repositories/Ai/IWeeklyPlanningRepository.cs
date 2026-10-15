using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IWeeklyPlanningRepository
{
    Task<WeeklyPlanningDraftRequest> CreateDraftRequestAsync(WeeklyPlanningDraftRequest request, IEnumerable<WeeklyPlanningDraftItem> items, CancellationToken cancellationToken = default);
    Task<WeeklyPlanningDraftRequest?> GetDraftByIdAsync(Guid tenantId, Guid draftId, CancellationToken cancellationToken = default);
    Task<WeeklyPlanningDraftRequest?> GetLatestDraftForWeekAsync(Guid tenantId, Guid classroomId, DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<WeeklyPlanningDraftItem>> GetItemsByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default);
    Task UpdateDraftItemAsync(WeeklyPlanningDraftItem item, CancellationToken cancellationToken = default);
    Task UpdateDraftStatusAsync(Guid tenantId, Guid draftId, string status, CancellationToken cancellationToken = default);
}
