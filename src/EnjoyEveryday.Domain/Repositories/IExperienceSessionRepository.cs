using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceSessionRepository
{
    Task<ExperienceSession?> GetSessionByIdAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExperienceSession>> GetSessionsForClassroomAsync(Guid tenantId, Guid classroomId, CancellationToken cancellationToken = default);
    Task<ExperienceSession?> GetSessionByScheduleIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(ExperienceSession session, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ExperienceParticipation>> GetParticipationsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SaveParticipationsAsync(Guid sessionId, IEnumerable<ExperienceParticipation> participations, CancellationToken cancellationToken = default);
}
