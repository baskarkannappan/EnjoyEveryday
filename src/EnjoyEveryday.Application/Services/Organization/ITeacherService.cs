using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface ITeacherService
{
    Task<Teacher?> GetTeacherByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Teacher>> GetAllTeachersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Teacher>> GetAllTeachersAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default) => GetAllTeachersAsync(tenantId, cancellationToken);
    Task<Teacher> CreateTeacherAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task UpdateTeacherAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task DeleteTeacherAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
