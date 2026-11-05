using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class DailyAttendanceCell
{
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }
    public DayOfWeek DayOfWeek => Date.DayOfWeek;
    public string DayOfWeekAbbr => Date.ToString("ddd");
    public bool IsWeekend => DayOfWeek == DayOfWeek.Saturday || DayOfWeek == DayOfWeek.Sunday;
    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsFuture => Date.Date > DateTime.Today;

    public string? RawStatus { get; set; }
    public TimeSpan? InTime { get; set; }
    public TimeSpan? OutTime { get; set; }
    public string? Notes { get; set; }

    public string Code
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RawStatus))
            {
                return IsWeekend ? "-" : (IsFuture ? "" : "-");
            }

            var st = RawStatus.Trim().ToLowerInvariant();
            if (st == "present" || st == "checkedout" || st == "clockedout" || st == "completed") return "P";
            if (st == "late" || st == "tardy") return "L";
            if (st == "absent") return "A";
            if (st == "excused" || st == "sick") return "E";
            if (st == "leave" || st == "onleave" || st == "vacation") return "LV";
            return st.Length > 2 ? st[..2].ToUpperInvariant() : st.ToUpperInvariant();
        }
    }

    public string CssClass
    {
        get
        {
            if (IsWeekend) return "cell-weekend";
            if (string.IsNullOrWhiteSpace(RawStatus))
            {
                return IsFuture ? "cell-future" : "cell-empty";
            }

            var code = Code;
            return code switch
            {
                "P" => "cell-present",
                "L" => "cell-late",
                "A" => "cell-absent",
                "E" => "cell-excused",
                "LV" => "cell-leave",
                _ => "cell-other"
            };
        }
    }

    public string TooltipText
    {
        get
        {
            var dateStr = Date.ToString("MMM d (ddd)");
            if (string.IsNullOrWhiteSpace(RawStatus))
            {
                return IsWeekend ? $"{dateStr}: Weekend" : (IsFuture ? $"{dateStr}: Scheduled" : $"{dateStr}: No record");
            }

            var parts = new List<string> { $"{dateStr}: {RawStatus}" };
            if (InTime.HasValue) parts.Add($"In: {InTime.Value:hh\\:mm}");
            if (OutTime.HasValue) parts.Add($"Out: {OutTime.Value:hh\\:mm}");
            if (!string.IsNullOrWhiteSpace(Notes)) parts.Add(Notes);
            return string.Join(" • ", parts);
        }
    }
}

public class ChildMonthlyRegisterRow
{
    public int SerialNumber { get; set; }
    public Guid ChildId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string ClassroomName { get; set; } = "General";

    public List<DailyAttendanceCell> DailyCells { get; set; } = new();

    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    public int WorkingDaysElapsed { get; set; }

    public double AttendanceRatePercent =>
        WorkingDaysElapsed > 0
            ? Math.Round(((double)(PresentCount + LateCount) / WorkingDaysElapsed) * 100.0, 1)
            : (DailyCells.Count > 0 && (PresentCount + LateCount) > 0 ? 100.0 : 0.0);
}

public class TeacherMonthlyRegisterRow
{
    public int SerialNumber { get; set; }
    public Guid TeacherId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = "Educator";
    public string? ProfilePhotoUrl { get; set; }

    public List<DailyAttendanceCell> DailyCells { get; set; } = new();

    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int LeaveCount { get; set; }
    public int WorkingDaysElapsed { get; set; }

    public double AttendanceRatePercent =>
        WorkingDaysElapsed > 0
            ? Math.Round(((double)(PresentCount + LateCount) / WorkingDaysElapsed) * 100.0, 1)
            : (DailyCells.Count > 0 && (PresentCount + LateCount) > 0 ? 100.0 : 0.0);
}

public class MonthlyRegisterSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int DaysInMonth { get; set; }
    public int WorkingDaysInMonth { get; set; }
    public int WorkingDaysElapsed { get; set; }

    public int TotalEnrolledOrStaff { get; set; }
    public int TotalPresentMarks { get; set; }
    public int TotalAbsentMarks { get; set; }
    public int TotalLateMarks { get; set; }
    public int TotalLeaveOrExcusedMarks { get; set; }
    public double OverallAttendanceRatePercent { get; set; }
}

