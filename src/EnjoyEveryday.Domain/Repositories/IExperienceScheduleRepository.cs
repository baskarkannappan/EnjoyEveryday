using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IExperienceScheduleRepository
{
    Task<IEnumerable<ExperienceSchedule>> GetByClassroomAndDateRangeAsync(Guid classroomId, DateOnly startDate, DateOnly endDate);
    Task<ExperienceSchedule?> GetByIdAsync(Guid id);
    Task<ExperienceSchedule> CreateAsync(ExperienceSchedule schedule);
    Task UpdateAsync(ExperienceSchedule schedule);
    Task DeleteAsync(Guid id);
}
