using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<TeacherService> _logger;

    public TeacherService(ITeacherRepository teacherRepository, ILogger<TeacherService> logger)
    {
        _teacherRepository = teacherRepository;
        _logger = logger;
    }

    public async Task<Teacher?> GetTeacherByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await _teacherRepository.GetByIdAsync(tenantId, id, cancellationToken);
    }

    public async Task<IEnumerable<Teacher>> GetAllTeachersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _teacherRepository.GetAllAsync(tenantId, cancellationToken);
    }

    public async Task<Teacher> CreateTeacherAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.Id = Guid.NewGuid();
        teacher.CreatedAt = DateTimeOffset.UtcNow;
        teacher.UpdatedAt = DateTimeOffset.UtcNow;
        
        _logger.LogInformation("Creating new teacher profile {TeacherId} for tenant {TenantId}", teacher.Id, teacher.TenantId);
        
        return await _teacherRepository.AddAsync(teacher, cancellationToken);
    }

    public async Task UpdateTeacherAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        teacher.UpdatedAt = DateTimeOffset.UtcNow;
        
        _logger.LogInformation("Updating teacher profile {TeacherId} for tenant {TenantId}", teacher.Id, teacher.TenantId);
        
        await _teacherRepository.UpdateAsync(teacher, cancellationToken);
    }

    public async Task DeleteTeacherAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating teacher profile {TeacherId} for tenant {TenantId}", id, tenantId);
        
        await _teacherRepository.DeleteAsync(tenantId, id, cancellationToken);
    }
}
