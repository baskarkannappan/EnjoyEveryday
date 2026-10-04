namespace EnjoyEveryday.Application.Services;

using EnjoyEveryday.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IClassroomService
{
    Task<Classroom?> GetClassroomByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Classroom>> GetAllClassroomsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Classroom>> GetClassroomsAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<Classroom> CreateClassroomAsync(Classroom classroom, CancellationToken cancellationToken = default);
    Task<Classroom> CreateClassroomAsync(Guid branchId, string name, string? ageGroup, int? capacity, string environment, CancellationToken cancellationToken = default);
    Task UpdateClassroomAsync(Classroom classroom, CancellationToken cancellationToken = default);
    Task DeleteClassroomAsync(Guid id, CancellationToken cancellationToken = default);
}
