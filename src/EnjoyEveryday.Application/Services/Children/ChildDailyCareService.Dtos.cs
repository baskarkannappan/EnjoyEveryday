using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Constants;
using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ClassroomChildCareStatus
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public bool IsCheckedIn { get; set; }
    public string AttendanceStatus { get; set; } = "Not Checked In";
    public TimeSpan? ArrivalTime { get; set; }
    public TimeSpan? DepartureTime { get; set; }

    // Last recorded summaries
    public ChildCareEvent? LastDiaperEvent { get; set; }
    public ChildCareEvent? LastMealEvent { get; set; }
    public ChildNapSession? ActiveNapSession { get; set; }
    public ChildCareEvent? LastComfortMood { get; set; }
    public int TodayEventsCount { get; set; }
    public bool HasLowSupplies { get; set; }
    public string? ImportantCareAlert { get; set; }
}

public class ChildTimelineEntry
{
    public Guid? Id { get; set; }
    public DateTime EventTime { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Attendance, Diaper, Meal, Sleep, Toileting, Hygiene, Comfort, Experience
    public string Icon { get; set; } = "•";
    public string? StaffName { get; set; }
    public bool IsParentVisible { get; set; } = true;
    public bool IsCorrected { get; set; }
}

public class ParentDailyCareReport
{
    public Guid ChildId { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }

    // 4 KPI Cards matching Screenshot 3
    public int DiaperChangesCount { get; set; }
    public int TotalRestMinutes { get; set; }
    public string RestTimeFormatted => TotalRestMinutes > 0 ? $"{TotalRestMinutes / 60}h {TotalRestMinutes % 60}m" : "None recorded";
    public string LunchMealStatus { get; set; } = "Not recorded";
    public string PrimaryMoodObservation { get; set; } = "Content & Settled";

    // Factual reassurance summary paragraph
    public string DailyReassuranceText { get; set; } = string.Empty;

    // Filtered timeline of parent-visible items
    public List<ChildTimelineEntry> Timeline { get; set; } = new();

    // Supply alerts for parent
    public List<ChildCareSupply> LowSupplies { get; set; } = new();
}

public interface IChildDailyCareService
{
    Task<Guid> RecordCareEventAsync(ChildCareEvent ev, CancellationToken ct = default);
    Task<int> RecordBatchCareEventsAsync(IEnumerable<Guid> childIds, string eventType, string detailOption, string? notes, bool parentVisible, CancellationToken ct = default);
    Task<int> StartBatchNapAsync(IEnumerable<Guid> childIds, Guid? classroomId, string? notes, CancellationToken ct = default);
    Task<int> WakeBatchChildrenAsync(IEnumerable<Guid> childIds, string? outcome, string? notes, CancellationToken ct = default);
    Task RecordCorrectionAsync(Guid eventId, string correctionReason, Action<ChildCareEvent> applyCorrection, CancellationToken ct = default);
    Task<ChildNapSession> StartNapAsync(Guid childId, Guid? classroomId, string? notes, CancellationToken ct = default);
    Task<ChildNapSession?> WakeChildAsync(Guid childId, string? outcome, string? notes, CancellationToken ct = default);
    Task<ChildNapSession?> GetActiveNapAsync(Guid childId, CancellationToken ct = default);
    Task<IEnumerable<ClassroomChildCareStatus>> GetClassroomOverviewAsync(Guid branchId, Guid? classroomId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid childId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ChildTimelineEntry>> GetChildCareTimelineAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ChildCareEvent>> GetChildCareEventsAsync(Guid childId, CancellationToken ct = default);
    Task<ChildCarePlan?> GetChildCarePlanAsync(Guid childId, CancellationToken ct = default);
    Task<Guid> SaveCarePlanAsync(ChildCarePlan plan, CancellationToken ct = default);
    Task<IEnumerable<ChildCareSupply>> GetChildSuppliesAsync(Guid childId, CancellationToken ct = default);
    Task<IEnumerable<ChildCareSupply>> GetLowSuppliesAsync(Guid branchId, CancellationToken ct = default);
    Task<Guid> UpdateSupplyStatusAsync(ChildCareSupply supply, CancellationToken ct = default);
    Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid childId, DateOnly date, CancellationToken ct = default);
    Task<ParentDailyCareReport> GetParentDailyCareReportAsync(Guid tenantId, Guid childId, DateOnly date, CancellationToken ct = default);
    Task<Guid> SubmitHandoverAsync(TeacherHandover handover, CancellationToken ct = default);
    Task AcknowledgeHandoverAsync(Guid handoverId, string? notes, CancellationToken ct = default);
    Task<IEnumerable<TeacherHandover>> GetClassroomHandoversAsync(Guid classroomId, DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<TeacherHandover>> GetPendingHandoversAsync(Guid branchId, CancellationToken ct = default);
    Task<ClassroomDailyCareFeedDto> GetClassroomDailyCareFeedAsync(Guid branchId, Guid? classroomId, DateOnly date, CancellationToken ct = default);
    Task<ChildCarePackageDto> GetChildCarePackageAsync(Guid childId, DateOnly date, CancellationToken ct = default);
}

