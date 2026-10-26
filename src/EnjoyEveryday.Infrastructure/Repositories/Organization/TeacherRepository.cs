using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TeacherRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private static readonly string[] CreateTeacherParams =
    [
        "Id", "TenantId", "PersonId", "UserId", "EmployeeNumber", "FirstName", "MiddleName", "LastName",
        "PreferredName", "DisplayName", "ProfilePhotoUrl", "DateOfBirth", "Gender", "PreferredLanguage",
        "OtherLanguages", "Pronouns", "Bio", "Status", "DraftData::jsonb", "ProfileCompletionPercentage",
        "CreatedAt", "UpdatedAt"
    ];

    private static readonly string[] UpdateTeacherParams =
    [
        "Id", "TenantId", "PersonId", "UserId", "EmployeeNumber", "FirstName", "MiddleName", "LastName",
        "PreferredName", "DisplayName", "ProfilePhotoUrl", "DateOfBirth", "Gender", "PreferredLanguage",
        "OtherLanguages", "Pronouns", "Bio", "Status", "DraftData::jsonb", "ProfileCompletionPercentage",
        "UpdatedAt"
    ];

    public async Task<Teacher?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleFunctionAsync<Teacher>(
            "public.fn_get_teacher_by_id",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public Task<IEnumerable<Teacher>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetAllAsync(tenantId, null, cancellationToken);

    public async Task<IEnumerable<Teacher>> GetAllAsync(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFunctionAsync<Teacher>(
            "public.fn_get_all_teachers",
            new { TenantId = tenantId, OrganizationId = organizationId },
            cancellationToken: cancellationToken);
    }

    public async Task<Teacher> AddAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var created = await connection.QuerySingleFunctionAsync<Teacher>(
            "public.fn_create_teacher",
            new
            {
                teacher.Id,
                teacher.TenantId,
                teacher.PersonId,
                teacher.UserId,
                teacher.EmployeeNumber,
                teacher.FirstName,
                teacher.MiddleName,
                teacher.LastName,
                teacher.PreferredName,
                teacher.DisplayName,
                teacher.ProfilePhotoUrl,
                teacher.DateOfBirth,
                teacher.Gender,
                teacher.PreferredLanguage,
                teacher.OtherLanguages,
                teacher.Pronouns,
                teacher.Bio,
                teacher.Status,
                DraftData = teacher.DraftData ?? "{}",
                teacher.ProfileCompletionPercentage,
                teacher.CreatedAt,
                teacher.UpdatedAt
            },
            parameterNames: CreateTeacherParams,
            cancellationToken: cancellationToken);

        return created ?? teacher;
    }

    public async Task UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_update_teacher",
            new
            {
                teacher.Id,
                teacher.TenantId,
                teacher.PersonId,
                teacher.UserId,
                teacher.EmployeeNumber,
                teacher.FirstName,
                teacher.MiddleName,
                teacher.LastName,
                teacher.PreferredName,
                teacher.DisplayName,
                teacher.ProfilePhotoUrl,
                teacher.DateOfBirth,
                teacher.Gender,
                teacher.PreferredLanguage,
                teacher.OtherLanguages,
                teacher.Pronouns,
                teacher.Bio,
                teacher.Status,
                DraftData = teacher.DraftData ?? "{}",
                teacher.ProfileCompletionPercentage,
                teacher.UpdatedAt
            },
            parameterNames: UpdateTeacherParams,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteProcedureAsync(
            "public.sp_delete_teacher",
            new { TenantId = tenantId, Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> IsTeacherAssignedToClassroomAsync(
        Guid tenantId,
        Guid classroomId,
        Guid teacherId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarFunctionAsync<bool>(
            "public.fn_is_teacher_assigned_to_classroom",
            new
            {
                TenantId = tenantId,
                ClassroomId = classroomId,
                TeacherId = teacherId,
                UserId = userId
            },
            cancellationToken: cancellationToken);
    }
}
