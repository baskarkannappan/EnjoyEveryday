using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IChildDailyCareRepository
{
    // Care Events
    Task<Guid> AddCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default);
    Task UpdateCareEventAsync(ChildCareEvent ev, CancellationToken cancellationToken = default);
    Task<ChildCareEvent?> GetCareEventByIdAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCareEvent>> GetEventsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCareEvent>> GetEventsByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCareEvent>> GetEventsByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCareEvent>> GetEventsByBranchAndDateAsync(Guid tenantId, Guid branchId, DateOnly date, CancellationToken cancellationToken = default);

    // Nap Sessions
    Task<Guid> StartNapSessionAsync(ChildNapSession session, CancellationToken cancellationToken = default);
    Task EndNapSessionAsync(Guid tenantId, Guid sessionId, DateTime endTime, int durationMinutes, string? outcome, string? notes, Guid endedByUserId, string? endedByName, CancellationToken cancellationToken = default);
    Task<ChildNapSession?> GetActiveNapSessionAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildNapSession>> GetActiveNapsByClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildNapSession>> GetNapsByChildAndDateAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken cancellationToken = default);

    // Care Plans
    Task<ChildCarePlan?> GetActiveCarePlanAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCarePlan>> GetCarePlansByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);

    // Care Supplies
    Task<IEnumerable<ChildCareSupply>> GetSuppliesByChildAsync(Guid tenantId, Guid childId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildCareSupply>> GetLowSuppliesByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    Task<Guid> SaveSupplyStatusAsync(ChildCareSupply supply, CancellationToken cancellationToken = default);

    // Teacher Handovers
    Task<Guid> CreateHandoverAsync(TeacherHandover handover, CancellationToken cancellationToken = default);
    Task AcknowledgeHandoverAsync(Guid tenantId, Guid handoverId, Guid acknowledgedByUserId, string acknowledgedByName, string? notes, CancellationToken cancellationToken = default);
    Task<TeacherHandover?> GetHandoverByIdAsync(Guid tenantId, Guid handoverId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeacherHandover>> GetHandoversByClassroomAndDateAsync(Guid tenantId, Guid classroomId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeacherHandover>> GetPendingHandoversByBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
}
