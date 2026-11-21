using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Attendance;

namespace EnjoyEveryday.Application.Services;

public interface IMonthlyRegisterService
{
    Task<(List<ChildMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyChildRegisterAsync(
        Guid branchId, int year, int month, Guid? classroomId = null, CancellationToken ct = default);

    Task<(List<TeacherMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyTeacherRegisterAsync(
        Guid branchId, int year, int month, CancellationToken ct = default);
}
