namespace EnjoyEveryday.Domain.Repositories;

public record EndOfDayPendingItem(Guid Id, string Name);

public interface IEndOfDayRepository
{
    Task<List<EndOfDayPendingItem>> GetPendingTeachersAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default);
    Task CloseTeachersAsync(Guid[] ids, TimeSpan clockOut, CancellationToken ct = default);

    Task<List<EndOfDayPendingItem>> GetPendingChildrenAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default);
    Task CloseChildrenAsync(Guid[] ids, TimeSpan departure, Guid userId, CancellationToken ct = default);

    Task<List<EndOfDayPendingItem>> GetPendingActivitiesAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default);
    Task CloseActivitiesAsync(Guid tenantId, DateTime date, Guid[] scheduleIds, TimeSpan endTime, CancellationToken ct = default);

    Task<List<EndOfDayPendingItem>> GetPendingEventsAsync(Guid tenantId, DateTime date, Guid? branchId, CancellationToken ct = default);
    Task CloseEventsAsync(Guid[] ids, CancellationToken ct = default);
}
