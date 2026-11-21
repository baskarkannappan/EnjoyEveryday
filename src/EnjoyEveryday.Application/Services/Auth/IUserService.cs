using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public interface IUserService
{
    Task AssignOrganizationRoleAsync(Guid targetUserId, string roleName, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetTeachersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetParentsAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(string email, string firstName, string lastName, string passwordHash, string? roleName = null, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveUserFromOrganizationAsync(Guid targetUserId, Guid? organizationId = null, CancellationToken cancellationToken = default);
    Task PromoteTeacherToLeadAsync(Guid targetUserId, Guid branchId, IReadOnlyList<Guid> classroomIds, CancellationToken cancellationToken = default);
    Task UpdateTeacherClassroomsAsync(Guid targetUserId, Guid branchId, IReadOnlyList<Guid> classroomIds, CancellationToken cancellationToken = default);
}
