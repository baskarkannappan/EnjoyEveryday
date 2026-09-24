using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;

    public UserService(IUserRepository userRepository, ITenantContext tenantContext)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetAllAsync(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetTeachersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetUsersByRoleAsync(tenantId, "Teacher", cancellationToken);
    }

    public async Task<IEnumerable<User>> GetParentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetUsersByRoleAsync(tenantId, "Parent", cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _userRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<User> CreateUserAsync(string email, string firstName, string lastName, string passwordHash, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _userRepository.AddAsync(user, passwordHash, cancellationToken);
    }
}
