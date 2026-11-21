using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Attendance;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public sealed class MonthlyRegisterService : IMonthlyRegisterService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ITenantContext _tenantContext;

    public MonthlyRegisterService(IAttendanceRepository attendanceRepository, ITenantContext tenantContext)
    {
        _attendanceRepository = attendanceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<(List<ChildMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyChildRegisterAsync(
        Guid branchId, int year, int month, Guid? classroomId = null, CancellationToken ct = default)
    {
        var rawRows = (await _attendanceRepository.GetMonthlyChildAttendanceAsync(
            _tenantContext.TenantId, branchId, year, month, classroomId, ct)).ToList();

        var (daysInMonth, workingDaysInMonth, workingDaysElapsed) = ComputeWorkingDays(year, month);

        var childGroups = rawRows
            .GroupBy(r => r.ChildId)
            .OrderBy(g => g.First().FullName)
            .ToList();

        var registerRows = new List<ChildMonthlyRegisterRow>();
        int sNo = 1;
        int totalPresent = 0, totalAbsent = 0, totalLate = 0, totalExcused = 0;

        foreach (var group in childGroups)
        {
            var first = group.First();
            var attByDay = group
                .Where(x => x.DayOfMonth.HasValue && x.DayOfMonth.Value >= 1 && x.DayOfMonth.Value <= daysInMonth)
                .GroupBy(x => x.DayOfMonth!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var row = new ChildMonthlyRegisterRow
            {
                SerialNumber = sNo++,
                ChildId = first.ChildId,
                FirstName = first.FirstName,
                LastName = first.LastName,
                FullName = first.FullName,
                PhotoUrl = first.PhotoUrl,
                ClassroomId = first.ClassroomId,
                ClassroomName = string.IsNullOrWhiteSpace(first.ClassroomName) ? "General" : first.ClassroomName,
                WorkingDaysElapsed = workingDaysElapsed
            };

            for (int day = 1; day <= daysInMonth; day++)
            {
                var cell = new DailyAttendanceCell
                {
                    DayNumber = day,
                    Date = new DateTime(year, month, day)
                };

                if (attByDay.TryGetValue(day, out var record))
                {
                    cell.RawStatus = record.Status;
                    cell.InTime = record.ArrivalTime;
                    cell.OutTime = record.DepartureTime;
                    cell.Notes = record.Notes;

                    switch (cell.Code)
                    {
                        case "P": row.PresentCount++; break;
                        case "L": row.LateCount++; break;
                        case "A": row.AbsentCount++; break;
                        case "E": row.ExcusedCount++; break;
                    }
                }

                row.DailyCells.Add(cell);
            }

            totalPresent += row.PresentCount;
            totalAbsent += row.AbsentCount;
            totalLate += row.LateCount;
            totalExcused += row.ExcusedCount;

            registerRows.Add(row);
        }

        var summary = BuildSummary(year, month, daysInMonth, workingDaysInMonth, workingDaysElapsed, registerRows.Count, totalPresent, totalAbsent, totalLate, totalExcused);
        return (registerRows, summary);
    }

    public async Task<(List<TeacherMonthlyRegisterRow> Rows, MonthlyRegisterSummary Summary)> GetMonthlyTeacherRegisterAsync(
        Guid branchId, int year, int month, CancellationToken ct = default)
    {
        var rawRows = (await _attendanceRepository.GetMonthlyTeacherAttendanceAsync(
            _tenantContext.TenantId, branchId, year, month, ct)).ToList();

        var (daysInMonth, workingDaysInMonth, workingDaysElapsed) = ComputeWorkingDays(year, month);

        var teacherGroups = rawRows
            .GroupBy(r => r.TeacherId)
            .OrderBy(g => g.First().FullName)
            .ToList();

        var registerRows = new List<TeacherMonthlyRegisterRow>();
        int sNo = 1;
        int totalPresent = 0, totalAbsent = 0, totalLate = 0, totalLeave = 0;

        foreach (var group in teacherGroups)
        {
            var first = group.First();
            var attByDay = group
                .Where(x => x.DayOfMonth.HasValue && x.DayOfMonth.Value >= 1 && x.DayOfMonth.Value <= daysInMonth)
                .GroupBy(x => x.DayOfMonth!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var row = new TeacherMonthlyRegisterRow
            {
                SerialNumber = sNo++,
                TeacherId = first.TeacherId,
                EmployeeNumber = first.EmployeeNumber,
                FirstName = first.FirstName,
                LastName = first.LastName,
                FullName = first.FullName,
                JobTitle = string.IsNullOrWhiteSpace(first.JobTitle) ? "Educator" : first.JobTitle,
                ProfilePhotoUrl = first.ProfilePhotoUrl,
                WorkingDaysElapsed = workingDaysElapsed
            };

            for (int day = 1; day <= daysInMonth; day++)
            {
                var cell = new DailyAttendanceCell
                {
                    DayNumber = day,
                    Date = new DateTime(year, month, day)
                };

                if (attByDay.TryGetValue(day, out var record))
                {
                    cell.RawStatus = record.Status;
                    cell.InTime = record.ClockInTime;
                    cell.OutTime = record.ClockOutTime;
                    cell.Notes = record.Notes;

                    switch (cell.Code)
                    {
                        case "P": row.PresentCount++; break;
                        case "L": row.LateCount++; break;
                        case "A": row.AbsentCount++; break;
                        case "LV": row.LeaveCount++; break;
                    }
                }

                row.DailyCells.Add(cell);
            }

            totalPresent += row.PresentCount;
            totalAbsent += row.AbsentCount;
            totalLate += row.LateCount;
            totalLeave += row.LeaveCount;

            registerRows.Add(row);
        }

        var summary = BuildSummary(year, month, daysInMonth, workingDaysInMonth, workingDaysElapsed, registerRows.Count, totalPresent, totalAbsent, totalLate, totalLeave);
        return (registerRows, summary);
    }

    private static (int DaysInMonth, int WorkingDaysInMonth, int WorkingDaysElapsed) ComputeWorkingDays(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateTime.Today;
        int workingDaysInMonth = 0;
        int workingDaysElapsed = 0;

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(year, month, d);
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            if (!isWeekend)
            {
                workingDaysInMonth++;
                if (date <= today) workingDaysElapsed++;
            }
        }

        return (daysInMonth, workingDaysInMonth, workingDaysElapsed);
    }

    private static MonthlyRegisterSummary BuildSummary(
        int year,
        int month,
        int daysInMonth,
        int workingDaysInMonth,
        int workingDaysElapsed,
        int totalEnrolledOrStaff,
        int totalPresent,
        int totalAbsent,
        int totalLate,
        int totalLeaveOrExcused)
    {
        int totalPossibleMarks = totalEnrolledOrStaff * Math.Max(1, workingDaysElapsed);
        double overallPct = totalPossibleMarks > 0
            ? Math.Round(((double)(totalPresent + totalLate) / totalPossibleMarks) * 100.0, 1)
            : 0.0;

        return new MonthlyRegisterSummary
        {
            Year = year,
            Month = month,
            DaysInMonth = daysInMonth,
            WorkingDaysInMonth = workingDaysInMonth,
            WorkingDaysElapsed = workingDaysElapsed,
            TotalEnrolledOrStaff = totalEnrolledOrStaff,
            TotalPresentMarks = totalPresent,
            TotalAbsentMarks = totalAbsent,
            TotalLateMarks = totalLate,
            TotalLeaveOrExcusedMarks = totalLeaveOrExcused,
            OverallAttendanceRatePercent = overallPct
        };
    }
}
