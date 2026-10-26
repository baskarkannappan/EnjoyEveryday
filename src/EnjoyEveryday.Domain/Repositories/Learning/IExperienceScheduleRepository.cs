using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceScheduleRepository
{
    Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<ExperienceSchedule>> GetByDateAsync(DateOnly date);
    Task<IEnumerable<ExperienceSchedule>> GetFilteredSchedulesAsync(Guid tenantId, DateOnly date, Guid? organizationId = null, Guid? branchId = null, Guid? classroomId = null)
        => Task.FromResult(Enumerable.Empty<ExperienceSchedule>());
    Task<ExperienceSchedule?> GetByIdAsync(Guid id);
    Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule);
    Task UpdateAsync(ExperienceSchedule schedule);
    Task DeleteAsync(Guid id);
}
