using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MemoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<MemoryEvent>> GetChildMemoriesAsync(
        Guid tenantId,
        Guid childId,
        bool isParent,
        MemoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<MemoryEvent>(
            "public.fn_get_child_memories",
            new
            {
                TenantId = tenantId,
                ChildId = childId,
                IsParent = isParent,
                parameters.StartDate,
                parameters.EndDate,
                parameters.Category,
                parameters.SearchQuery,
                parameters.Limit,
                parameters.Offset
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetClassroomMemoriesAsync(
        Guid tenantId,
        Guid classroomId,
        bool isParent,
        MemoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<MemoryEvent>(
            "public.fn_get_classroom_memories",
            new
            {
                TenantId = tenantId,
                ClassroomId = classroomId,
                parameters.StartDate,
                parameters.EndDate,
                parameters.Limit,
                parameters.Offset
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetTeacherMemoriesAsync(
        Guid tenantId,
        Guid teacherId,
        bool isParent,
        MemoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<MemoryEvent>(
            "public.fn_get_teacher_memories",
            new
            {
                TenantId = tenantId,
                TeacherId = teacherId,
                parameters.StartDate,
                parameters.EndDate,
                parameters.Limit,
                parameters.Offset
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<MemoryEvent>> GetExperienceMemoriesAsync(
        Guid tenantId,
        Guid experienceId,
        bool isParent,
        MemoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await conn.QueryFunctionAsync<MemoryEvent>(
            "public.fn_get_experience_memories",
            new
            {
                TenantId = tenantId,
                ExperienceId = experienceId,
                parameters.StartDate,
                parameters.EndDate,
                parameters.Limit,
                parameters.Offset
            },
            cancellationToken: cancellationToken);
    }
}
