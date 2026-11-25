using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.UI.Shared.Components;
using EnjoyEveryday.UI.Shared.Components.ActiveSessionModal;
using EnjoyEveryday.UI.Shared.Components.Attendance;
using EnjoyEveryday.UI.Shared.Components.ChildJourney;
using EnjoyEveryday.UI.Shared.Components.OperationsBoard;

namespace EnjoyEveryday.Daycare.Web.Components.Pages;

public partial class TeacherWorkspace
{
    [Parameter] public IReadOnlySet<Guid> AssignedClassroomIds { get; set; } = new HashSet<Guid>();
    [Parameter] public IReadOnlyList<string> AvailableViews { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback<string> OnChangeView { get; set; }
    [Parameter] public bool IsLeadTeacher { get; set; } = false;

    private bool _loading = true;
    private OperationsBoardModel _board = new();
    private List<Classroom> _teacherClassrooms = new();
    private Classroom? _activeClassroom => _teacherClassrooms.FirstOrDefault(c => c.Id == _selectedClassroomId);
    private Guid? _selectedClassroomId;
    private int _chatUnreadCount = 0;

    // Classroom Live Cockpit State
    private List<ClassroomChildCareStatusDto> _classroomChildren = new();
    private string _rosterFilter = "All";
    private string _childSearchQuery = string.Empty;
    private Guid? _activeDiaperChildId;
    private Guid? _activeMealChildId;

    // Classroom Gifting Modal State
    private bool _showClassroomGiftsModal = false;
    private bool _isLoadingClassroomGifts = false;
    private List<ClassroomGiftingOrder> _classroomGifts = new();

    private async Task OpenClassroomGiftsModal()
    {
        _showClassroomGiftsModal = true;
        await LoadClassroomGiftsAsync();
    }

    private async Task LoadClassroomGiftsAsync()
    {
        if (!_selectedClassroomId.HasValue) return;
        try
        {
            _isLoadingClassroomGifts = true;
            var orgId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            _classroomGifts = await GiftingService.GetOrdersForAdminAsync(orgId, null, _selectedClassroomId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load classroom gifts");
            _classroomGifts = new();
        }
        finally
        {
            _isLoadingClassroomGifts = false;
        }
    }

    private async Task ConfirmReceiptAtClassroomAsync(Guid orderId)
    {
        try
        {
            await GiftingService.UpdateFulfilmentStatusAsync(orderId, GiftingFulfilmentStatuses.ReceivedAtDestination, "Physically received at classroom by educator");
            await LoadClassroomGiftsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to confirm receipt");
        }
    }

    private async Task ConfirmDistributionAsync(Guid orderId)
    {
        try
        {
            await GiftingService.UpdateFulfilmentStatusAsync(orderId, GiftingFulfilmentStatuses.Distributed, "Gift handed over to child / distributed for classroom learning");
            await LoadClassroomGiftsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to confirm distribution");
        }
    }

    // Feature 7: Zero-Click Adaptive Classroom Routine (Lazy Teacher Mode)
    public enum ClassroomRoutineMode
    {
        Auto,
        Arrival,
        Meal,
        Rest,
        Diaper,
        Pickup
    }

    private ClassroomRoutineMode _currentRoutineMode = ClassroomRoutineMode.Auto;

    private ClassroomRoutineMode EffectiveRoutine
    {
        get
        {
            if (_currentRoutineMode != ClassroomRoutineMode.Auto)
                return _currentRoutineMode;

            var time = DateTime.Now.TimeOfDay;
            if (time < new TimeSpan(10, 30, 0)) return ClassroomRoutineMode.Arrival;
            if (time < new TimeSpan(12, 45, 0)) return ClassroomRoutineMode.Meal;
            if (time < new TimeSpan(15, 0, 0)) return ClassroomRoutineMode.Rest;
            if (time < new TimeSpan(16, 30, 0)) return ClassroomRoutineMode.Diaper;
            return ClassroomRoutineMode.Pickup;
        }
    }

    private string GetCurrentRoutineTitle() => EffectiveRoutine switch
    {
        ClassroomRoutineMode.Arrival => "Morning Arrival & Gate",
        ClassroomRoutineMode.Meal => "Midday Meal & Table Lunch",
        ClassroomRoutineMode.Rest => "Nap Time & Rest Sessions",
        ClassroomRoutineMode.Diaper => "Diaper & Potty Routine",
        ClassroomRoutineMode.Pickup => "Evening Pickup & Handoff",
        _ => "Classroom Daily Flow"
    };

    private string GetCurrentRoutineDescription() => EffectiveRoutine switch
    {
        ClassroomRoutineMode.Arrival => "Tap 'Check-In All' or tap child cards to log morning arrivals.",
        ClassroomRoutineMode.Meal => "1-Tap 'Record Whole Table Lunch' logs everyone, then tap any child who ate less.",
        ClassroomRoutineMode.Rest => "1-Tap 'Rest All Awake' starts sleep timers. Tap resting cots to wake up.",
        ClassroomRoutineMode.Diaper => "Use direct 1-tap chips below on each child card (Wet / BM / Dry).",
        ClassroomRoutineMode.Pickup => "Tap 'Cheat-Sheet' for 15-second parent conversation briefings at the door.",
        _ => "Contextual daily routine"
    };

    private void SetRoutineMode(ClassroomRoutineMode mode)
    {
        _currentRoutineMode = mode;
    }

    // Daily Operational Gates State
    private ClassroomDailyStatusDto? _classroomOpsStatus;
    private BranchOperationalStatusDto? _branchOperationalStatus;
    private bool _showCloseClassroomModal = false;
    private TimeOnly _closeClassroomTime = TimeOnly.FromDateTime(DateTime.Now);
    private bool _isClosingClassroom = false;

    // Feature 1: Pickup Cheat-Sheet
    private ClassroomChildCareStatusDto? _activeCheatSheetChild;

    // Feature 4: Whole-Table Sync & Potty Line Checklist
    private bool _showPottyChecklistModal = false;

    // Feature 5: Transition Chime & Visual Gentle Timer
    private bool _showTimerModal = false;
    private int _timerTotalSeconds = 300;
    private int _timerRemainingSeconds = 300;
    private bool _isTimerRunning = false;
    private System.Threading.Timer? _transitionCountdownTimer;

    // Feature 6: Co-Teacher Live Sync
    private bool _showLiveSyncFlash = false;
    private System.Threading.Timer? _coTeacherSyncTimer;
    private DateTimeOffset _lastSyncCheckTime = DateTimeOffset.UtcNow;

    // Staff Break
    private bool _isOnBreak = false;
    private DateTimeOffset? _breakStartTime;

    // Journey Moment Dialog
    private AddJourneyMomentDialog? _momentDialog;

    // Group Snack Modal
    private bool _showGroupSnackModal = false;
    private string _groupSnackType = "MorningSnack";
    private string _groupSnackAmount = "All (100%)";
    private string _groupSnackNotes = "Classroom fresh fruit & water";

    // Modals
    private bool _isSessionModalOpen = false;
    private Guid _activeSessionScheduleId;
    private bool _isSessionPhotoModalOpen = false;
    private Guid _sessionPhotoScheduleId;
    private string _sessionPhotoTitle = "";
    private string[] _sessionPhotoUrls = Array.Empty<string>();
    private static readonly Dictionary<Guid, string[]> _sessionPhotoStore = new();

    private RhythmScheduleItem? ActiveRunningExperience => _board.TodayRhythm?.FirstOrDefault(r => 
        !r.IsCelebration && (r.Status == "Started" || r.Status == "Active" || r.Status == "InProgress"));

    private RhythmScheduleItem? NextPlannedExperience => _board.TodayRhythm?.FirstOrDefault(r => 
        !r.IsCelebration && (r.Status == "Scheduled" || r.Status == "Planned" || r.Status == "Confirmed"));

    private System.Threading.Timer? _autoCloseCheckTimer;

    protected override void OnInitialized()
    {
        _autoCloseCheckTimer = new System.Threading.Timer(async _ =>
        {
            if (_isDaycareClosed)
            {
                await InvokeAsync(async () =>
                {
                    await CheckAndAutoCloseEndOfDayAsync();
                    await RefreshBoardAsync();
                    StateHasChanged();
                });
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _coTeacherSyncTimer = new System.Threading.Timer(async _ =>
        {
            if (!_isDaycareClosed && _selectedClassroomId.HasValue)
            {
                await InvokeAsync(async () =>
                {
                    await PerformBackgroundCoTeacherSyncAsync();
                });
            }
        }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    public void Dispose()
    {
        _autoCloseCheckTimer?.Dispose();
        _coTeacherSyncTimer?.Dispose();
        _transitionCountdownTimer?.Dispose();
    }

    private Guid? _loadedOrgId;
    private bool _workspaceInitialized;

    protected override async Task OnParametersSetAsync()
    {
        var currentOrgId = UserContext.OrganizationId;
        if (!_workspaceInitialized || _loadedOrgId != currentOrgId)
        {
            _workspaceInitialized = true;
            _loadedOrgId = currentOrgId;
            await LoadTeacherClassroomsAsync();
            await RefreshBoardAsync();
        }
    }

    private async Task LoadTeacherClassroomsAsync()
    {
        try
        {
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            var allClassrooms = (await ClassroomService.GetAllClassroomsAsync()).ToList();

            var userRoles = (UserContext.Roles ?? Enumerable.Empty<string>()).ToList();
            bool isOrgAdmin = userRoles.Any(r =>
                r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
                r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
                r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase));
            bool isBranchAdmin = userRoles.Any(r => r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase));

            if (AssignedClassroomIds != null && AssignedClassroomIds.Any())
            {
                _teacherClassrooms = allClassrooms.Where(c => AssignedClassroomIds.Contains(c.Id)).ToList();
            }
            else if (isOrgAdmin || isBranchAdmin)
            {
                // Administrators previewing educator view may view all classrooms
                _teacherClassrooms = allClassrooms;
            }
            else
            {
                // Pure teachers with no assigned classrooms cannot access any classroom data
                _teacherClassrooms = new List<Classroom>();
            }

            if (_teacherClassrooms.Any())
            {
                if (!_selectedClassroomId.HasValue || !_teacherClassrooms.Any(c => c.Id == _selectedClassroomId.Value))
                {
                    _selectedClassroomId = _teacherClassrooms.First().Id;
                }
            }
            else
            {
                _selectedClassroomId = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading teacher classrooms");
        }
    }

    private async Task RefreshBoardAsync()
    {
        _loading = true;
        try
        {
            if (_isDaycareClosed)
            {
                await CheckAndAutoCloseEndOfDayAsync();
            }

            if (_selectedClassroomId.HasValue)
            {
                var filter = new OperationsBoardFilter
                {
                    OrganizationId = UserContext.OrganizationId,
                    ClassroomId = _selectedClassroomId,
                    RoleOverride = RoleTemplates.ClassroomTeacher,
                    Date = DateTime.Today
                };

                _board = await OperationsBoardService.GetOperationsBoardAsync(filter);
                if (_board.TodayRhythm != null)
                {
                    foreach (var r in _board.TodayRhythm)
                    {
                        if (_sessionPhotoStore.TryGetValue(r.Id, out var photos) && photos.Length > 0)
                        {
                            r.PhotoUrls = photos;
                        }
                    }
                }

                await LoadClassroomLiveFeedAsync();

                try
                {
                    _classroomOpsStatus = await DailyOpsService.GetClassroomDailyStatusAsync(_selectedClassroomId.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Could not fetch classroom daily status");
                }

                if (_activeClassroom != null)
                {
                    try
                    {
                        _branchOperationalStatus = await BranchOperationalStatusService.GetBranchStatusAsync(TenantContext.TenantId, _activeClassroom.BranchId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Could not fetch branch operational status");
                    }
                }
            }

            try
            {
                var feed = await CommunicationService.GetConversationsFeedAsync(classroomId: _selectedClassroomId);
                _chatUnreadCount = feed.TotalUnreadCount;
            }
            catch
            {
                _chatUnreadCount = 0;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing teacher board");
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleClassroomSelected(ChangeEventArgs e)
    {
        var val = e.Value?.ToString();
        if (Guid.TryParse(val, out var cId) && _teacherClassrooms.Any(c => c.Id == cId))
        {
            _selectedClassroomId = cId;
            await RefreshBoardAsync();
        }
    }

    private int GetPendingTasksCount()
    {
        int tasks = 0;
        if (_board.ExpectedNotArrivedCount > 0) tasks++;
        if (_board.TeacherChildrenCareStatus != null && _board.TeacherChildrenCareStatus.Any(c => c.NeedsAttention)) tasks++;
        if (_board.Alerts != null && _board.Alerts.Any(a => a.Severity == "High")) tasks++;
        return Math.Max(tasks, 1);
    }

    private async Task CheckAndAutoCloseEndOfDayAsync()
    {
        if (!_isDaycareClosed) return;

        try
        {
            var summary = await EndOfDayService.GetPendingSummaryAsync(
                branchId: _activeClassroom?.BranchId,
                date: DateTime.Today);

            if (summary.TotalPendingCount > 0)
            {
                var closingTime = new TimeSpan(19, 0, 0); // 7:00 PM End of Day time
                var report = await EndOfDayService.ExecuteEndOfDayProcessAsync(
                    branchId: _activeClassroom?.BranchId,
                    date: DateTime.Today,
                    checkoutTime: closingTime);

                Logger.LogInformation("End of day auto-close executed: {Message}", report.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking or executing auto close at end of day");
        }
    }

    private int GetChildrenPresentCount()
    {
        if (_classroomChildren != null && _classroomChildren.Any())
        {
            return _classroomChildren.Count(c => c.IsCheckedIn);
        }
        return _board.CheckedInChildrenCount;
    }

    private int GetTeachersPresentCount()
    {
        if (_selectedClassroomId.HasValue && _board.Classrooms != null)
        {
            var room = _board.Classrooms.FirstOrDefault(c => c.ClassroomId == _selectedClassroomId.Value);
            if (room != null && room.PresentStaff > 0)
            {
                return room.PresentStaff;
            }
        }
        if (_board.PresentStaffCount > 0)
        {
            return _board.PresentStaffCount;
        }
        return 1;
    }

    private int GetPlannedActivitiesCount()
    {
        if (_board.TodayRhythm != null && _board.TodayRhythm.Any())
        {
            return _board.TodayRhythm.Count;
        }
        return Math.Max(_board.PlannedExperiencesCount, 0);
    }

    private int GetStartedActivitiesCount()
    {
        if (_board.TodayRhythm != null && _board.TodayRhythm.Any())
        {
            return _board.TodayRhythm.Count(r => r.Status == "InProgress" || r.Status == "Started" || r.Status == "Active");
        }
        return _board.InProgressExperiencesCount;
    }

    private string GetAttendanceTimeDisplay()
    {
        if (_classroomChildren != null && _classroomChildren.Any(c => c.IsCheckedIn && c.ArrivalTime.HasValue))
        {
            var earliest = _classroomChildren.Where(c => c.IsCheckedIn && c.ArrivalTime.HasValue).Min(c => c.ArrivalTime!.Value);
            return FormatTimeSpan(earliest);
        }
        return "8:30 AM";
    }

    private string GetActivityTimeDisplay()
    {
        if (ActiveRunningExperience != null && !string.IsNullOrWhiteSpace(ActiveRunningExperience.TimeDisplay))
        {
            return ActiveRunningExperience.TimeDisplay;
        }
        if (NextPlannedExperience != null && !string.IsNullOrWhiteSpace(NextPlannedExperience.TimeDisplay) && NextPlannedExperience.TimeDisplay != "Today")
        {
            return NextPlannedExperience.TimeDisplay;
        }
        if (_board.TodayRhythm != null && _board.TodayRhythm.Any(r => !string.IsNullOrWhiteSpace(r.TimeDisplay)))
        {
            var first = _board.TodayRhythm.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.TimeDisplay));
            if (first != null && !string.IsNullOrWhiteSpace(first.TimeDisplay))
            {
                return first.TimeDisplay;
            }
        }
        return "10:30 AM";
    }

    private async Task HandleTimelineEndedClickAsync()
    {
        if (_isDaycareClosed)
        {
            await NotificationService.ShowInfoAsync("Daycare closed at 7:00 PM. Any active activities and attendances were auto-closed.");
        }
        else
        {
            await NotificationService.ShowInfoAsync("Daycare operating hours end at 7:00 PM. At end of day, any active activity is auto-closed and any open attendance is auto-marked with checkout time.");
        }
    }

    private string GetGreeting()
    {
        int hour = TimeProvider.GetLocalNow().Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    private string GetTeacherDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(UserContext.Email))
        {
            var name = UserContext.Email.Split('@')[0];
            return char.ToUpper(name[0]) + name.Substring(1);
        }
        return "Teacher";
    }

    private string GetViewDisplayName(string roleKey) => roleKey switch
    {
        "Teacher" or "ClassroomTeacher" => "Educator",
        "LeadTeacher" => "Lead Teacher",
        "BranchAdmin" => "Branch Admin",
        "OrgAdmin" or "OrganizationAdmin" => "Organization Admin",
        _ => roleKey
    };

    private async Task StartSession(Guid scheduleId)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }

        if (_isDaycareClosed)
        {
            _ = NotificationService.ShowWarningAsync("Daycare is closed for the day (operating hours ended at 7:00 PM). New activity sessions cannot be started until tomorrow morning.");
            return;
        }

        try
        {
            var teacherId = UserContext.UserId != Guid.Empty ? UserContext.UserId : Guid.Parse("50000000-0000-0000-0000-000000000001");
            await ExecutionService.StartExperienceAsync(scheduleId, teacherId);
            await RefreshBoardAsync();
            OpenActiveSession(scheduleId);
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task CloseSession(Guid scheduleId)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }

        if (_isDaycareClosed)
        {
            _ = NotificationService.ShowWarningAsync("Daycare is closed. Activities cannot be started or ended while the daycare is closed.");
            return;
        }

        try
        {
            var teacherId = UserContext.UserId != Guid.Empty ? UserContext.UserId : Guid.Parse("50000000-0000-0000-0000-000000000001");
            await ExecutionService.CompleteExperienceAsync(scheduleId, teacherId, 5, "Completed", "Closed from Teacher Workspace");
            await RefreshBoardAsync();
            await NotificationService.ShowSuccessAsync("Experience session completed.");
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private void OpenActiveSession(Guid scheduleId)
    {
        _activeSessionScheduleId = scheduleId;
        _isSessionModalOpen = true;
    }

    private async Task OnSessionEnded()
    {
        _isSessionModalOpen = false;
        await RefreshBoardAsync();
    }

    private void OpenSessionPhotoModal(RhythmScheduleItem item)
    {
        _sessionPhotoScheduleId = item.Id;
        _sessionPhotoTitle = item.Title;
        if (!_sessionPhotoStore.TryGetValue(item.Id, out var photos))
        {
            photos = item.PhotoUrls ?? Array.Empty<string>();
        }
        _sessionPhotoUrls = photos;
        _isSessionPhotoModalOpen = true;
    }

    private async Task OnSessionPhotosSaved(string[] newPhotos)
    {
        _sessionPhotoUrls = newPhotos;
        _sessionPhotoStore[_sessionPhotoScheduleId] = newPhotos;
        var target = _board.TodayRhythm.FirstOrDefault(r => r.Id == _sessionPhotoScheduleId);
        if (target != null)
        {
            target.PhotoUrls = newPhotos;
        }
        await NotificationService.ShowSuccessAsync("Session photos saved.");
        StateHasChanged();
    }

    // =========================================================================
    // CLASSROOM LIVE COCKPIT ACTION HANDLERS
    // =========================================================================

    private async Task LoadClassroomLiveFeedAsync()
    {
        if (_activeClassroom == null) return;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var feed = await DailyCareService.GetClassroomDailyCareFeedAsync(_activeClassroom.BranchId, _activeClassroom.Id, today);
            _classroomChildren = feed.ClassroomOverview ?? new();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading live classroom care feed");
        }
    }

    private void ToggleStaffBreak()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Teacher attendance, duty, and breaks cannot be recorded while the branch is closed.");
            return;
        }

        _isOnBreak = !_isOnBreak;
        if (_isOnBreak)
        {
            _breakStartTime = TimeProvider.GetLocalNow();
            _ = NotificationService.ShowInfoAsync("Staff break started. Take a moment to recharge! ☕");
        }
        else
        {
            _breakStartTime = null;
            _ = NotificationService.ShowSuccessAsync("Welcome back to your classroom! 🌿");
        }
    }

    private void HandleClassroomReadinessNavigation()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Classroom readiness inspections and openings are locked until the Branch Admin opens the branch.");
            return;
        }
        Navigation.NavigateTo("/classrooms");
    }

    private void HandleTeamTasksNavigation()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Classroom shift handovers and tasks are locked until the Branch Admin opens the branch.");
            return;
        }
        Navigation.NavigateTo("/daily-care");
    }

    private void HandleReviewLogsNavigation()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Daily care logs cannot be modified while the branch is closed.");
            return;
        }
        Navigation.NavigateTo("/daily-care");
    }

    private void HandleExperienceFeedbackNavigation()
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        Navigation.NavigateTo("/experiences");
    }

    private void HandleRequestCoverageNavigation()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Staff coverage requests are locked until the Branch Admin opens the branch.");
            return;
        }
        Navigation.NavigateTo("/management/roster");
    }

    private void HandleTakeAttendanceNavigation()
    {
        if (IsBranchClosed)
        {
            _ = NotificationService.ShowWarningAsync("Branch facility is closed. Attendance desk operations are locked until the Branch Admin opens the branch.");
            return;
        }
        Navigation.NavigateTo("/management/attendance");
    }

    private void HandleStartActivityNavigation()
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        Navigation.NavigateTo("/today-activities");
    }

    private bool _isDaycareClosed => CheckIfDaycareClosed();

    private bool CheckIfDaycareClosed()
    {
        var localNow = TimeProvider.GetLocalNow();
        return EndOfDayService.IsDaycareClosedForDate(DateOnly.FromDateTime(localNow.Date), _activeClassroom?.BranchId, includeTimeOfDayCheckForToday: true, out _);
    }

    private async Task SelectClassroomAsync(Guid roomId)
    {
        if (_selectedClassroomId != roomId && _teacherClassrooms.Any(c => c.Id == roomId))
        {
            _selectedClassroomId = roomId;
            await RefreshBoardAsync();
        }
    }

    private bool IsBranchClosed => 
        (_branchOperationalStatus != null && !_branchOperationalStatus.IsOperational) ||
        (!DailyOpsService.IsBypassedForTesting() && 
         (_classroomOpsStatus == null || !_classroomOpsStatus.IsBranchOpen));

    private bool IsClassroomOperationsLocked => 
        (_branchOperationalStatus != null && !_branchOperationalStatus.IsOperational) ||
        (!DailyOpsService.IsBypassedForTesting() && 
         (_classroomOpsStatus == null || !_classroomOpsStatus.IsOpen || !_classroomOpsStatus.IsBranchOpen));

    private string GetClassroomLockedMessage()
    {
        if (_branchOperationalStatus != null && !_branchOperationalStatus.IsOperational)
        {
            return $"Branch '{_branchOperationalStatus.BranchName}' is currently {_branchOperationalStatus.OperationalStatus.ToLowerInvariant()}. No classroom activities may be conducted until the Branch Admin activates the branch.";
        }
        if (_classroomOpsStatus == null || !_classroomOpsStatus.IsBranchOpen)
        {
            return "Branch facility is closed for operations. No teacher attendance, classroom readiness, student arrivals, learning activities, or care routines may be conducted until the Branch Admin opens the branch.";
        }
        if (!_classroomOpsStatus.IsOpen && _classroomOpsStatus.ClosedAt != null)
        {
            return "Classroom is closed for the day. Routine care and attendance are sealed.";
        }
        return "Classroom has not been opened yet. Please click 'Open Classroom' in the header to start today's session.";
    }

    private async Task HandleOpenClassroomAsync()
    {
        if (!_selectedClassroomId.HasValue) return;
        try
        {
            var res = await DailyOpsService.OpenClassroomDayAsync(_selectedClassroomId.Value);
            if (res.Success)
            {
                await NotificationService.ShowSuccessAsync("Classroom successfully opened! Child arrivals and learning activities are now active.");
                await RefreshBoardAsync();
            }
            else
            {
                await NotificationService.ShowWarningAsync(res.Message);
            }
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private void OpenClassroomCloseModal()
    {
        _closeClassroomTime = TimeOnly.FromDateTime(DateTime.Now);
        _showCloseClassroomModal = true;
    }

    private async Task HandleConfirmCloseClassroomAsync()
    {
        if (!_selectedClassroomId.HasValue) return;
        _isClosingClassroom = true;
        try
        {
            var checkoutTime = _closeClassroomTime.ToTimeSpan();
            var res = await DailyOpsService.CloseClassroomDayAsync(_selectedClassroomId.Value, checkoutTime: checkoutTime);
            if (res.Success)
            {
                _showCloseClassroomModal = false;
                await NotificationService.ShowSuccessAsync(res.Message);
                await RefreshBoardAsync();
            }
            else
            {
                await NotificationService.ShowWarningAsync(res.Message);
            }
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
        finally
        {
            _isClosingClassroom = false;
        }
    }

    private async Task CheckInChildAsync(Guid childId)
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (_isDaycareClosed)
        {
            _ = NotificationService.ShowWarningAsync("Daycare is closed for the day. New arrivals cannot be checked in after 7:00 PM.");
            return;
        }

        try
        {
            var nowTime = TimeProvider.GetLocalNow().TimeOfDay;
            await AttendanceService.MarkChildAttendanceAsync(new ChildAttendance
            {
                Id = Guid.NewGuid(),
                TenantId = TenantContext.TenantId,
                BranchId = _activeClassroom.BranchId,
                ClassroomId = _activeClassroom.Id,
                ChildId = childId,
                AttendanceDate = DateTime.Today,
                Status = "Present",
                ArrivalTime = nowTime,
                RecordedByUserId = UserContext.UserId
            });

            await NotificationService.ShowSuccessAsync("Child checked in.");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private bool _showSecurePickupModal = false;
    private Guid _pickupChildId = Guid.Empty;
    private string _pickupChildName = "";

    private void CheckOutChildAsync(Guid childId)
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (_isDaycareClosed)
        {
            _ = NotificationService.ShowWarningAsync("Daycare is closed. Child attendance and departure cannot be performed while the daycare is closed.");
            return;
        }

        var child = _classroomChildren.FirstOrDefault(c => c.ChildId == childId);
        _pickupChildId = childId;
        _pickupChildName = child?.ChildName ?? "Child";
        _showSecurePickupModal = true;
    }

    private async Task HandleSecurePickupCompleted()
    {
        _showSecurePickupModal = false;
        await NotificationService.ShowSuccessAsync("Child departure and authorized pickup verified.");
        await RefreshBoardAsync();
    }

    private async Task MarkAllScheduledPresentAsync()
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (_isDaycareClosed)
        {
            _ = NotificationService.ShowWarningAsync("Daycare is closed for the day. Batch check-in cannot be recorded after 7:00 PM.");
            return;
        }

        try
        {
            var notCheckedIn = _classroomChildren.Where(c => !c.IsCheckedIn).ToList();
            if (!notCheckedIn.Any())
            {
                await NotificationService.ShowInfoAsync("All scheduled children are already checked in.");
                return;
            }

            var nowTime = TimeProvider.GetLocalNow().TimeOfDay;
            foreach (var ch in notCheckedIn)
            {
                await AttendanceService.MarkChildAttendanceAsync(new ChildAttendance
                {
                    Id = Guid.NewGuid(),
                    TenantId = TenantContext.TenantId,
                    BranchId = _activeClassroom.BranchId,
                    ClassroomId = _activeClassroom.Id,
                    ChildId = ch.ChildId,
                    AttendanceDate = DateTime.Today,
                    Status = "Present",
                    ArrivalTime = nowTime,
                    RecordedByUserId = UserContext.UserId
                });
            }

            await NotificationService.ShowSuccessAsync($"Checked in {notCheckedIn.Count} children.");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private void ToggleDiaperPopover(Guid childId)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _activeMealChildId = null;
        _activeDiaperChildId = _activeDiaperChildId == childId ? null : childId;
    }

    private void ToggleMealPopover(Guid childId)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _activeDiaperChildId = null;
        _activeMealChildId = _activeMealChildId == childId ? null : childId;
    }

    private async Task QuickRecordDiaperAsync(Guid childId, string condition)
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        var child = _classroomChildren.FirstOrDefault(c => c.ChildId == childId);
        if (child == null || !child.IsCheckedIn)
        {
            _activeDiaperChildId = null;
            await NotificationService.ShowWarningAsync($"{child?.ChildName ?? "Child"} is {child?.AttendanceStatus ?? "not checked in"}. Care logs can only be recorded for checked-in children.");
            return;
        }

        try
        {
            await DailyCareService.RecordCareEventAsync(new ChildCareEvent
            {
                ChildId = childId,
                ClassroomId = _activeClassroom.Id,
                BranchId = _activeClassroom.BranchId,
                EventType = CareEventTypes.DiaperChange,
                DiaperCondition = condition,
                EventTime = DateTime.UtcNow,
                ParentVisible = true
            });

            _activeDiaperChildId = null;
            await NotificationService.ShowSuccessAsync($"Diaper logged: {condition}");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task QuickRecordMealAsync(Guid childId, string amount)
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        var child = _classroomChildren.FirstOrDefault(c => c.ChildId == childId);
        if (child == null || !child.IsCheckedIn)
        {
            _activeMealChildId = null;
            await NotificationService.ShowWarningAsync($"{child?.ChildName ?? "Child"} is {child?.AttendanceStatus ?? "not checked in"}. Care logs can only be recorded for checked-in children.");
            return;
        }

        try
        {
            await DailyCareService.RecordCareEventAsync(new ChildCareEvent
            {
                ChildId = childId,
                ClassroomId = _activeClassroom.Id,
                BranchId = _activeClassroom.BranchId,
                EventType = CareEventTypes.Meal,
                MealType = "Snack",
                AmountConsumed = amount,
                EventTime = DateTime.UtcNow,
                ParentVisible = true
            });

            _activeMealChildId = null;
            await NotificationService.ShowSuccessAsync($"Meal logged: {amount}");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task ToggleNapAsync(ClassroomChildCareStatusDto child)
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (!child.IsCheckedIn)
        {
            await NotificationService.ShowWarningAsync($"{child.ChildName} is {child.AttendanceStatus}. Rest logs can only be recorded for checked-in children.");
            return;
        }

        try
        {
            if (child.ActiveNapSession == null)
            {
                await DailyCareService.StartNapAsync(child.ChildId, _activeClassroom.Id, null);
                await NotificationService.ShowSuccessAsync($"{child.ChildName} rest started.");
            }
            else
            {
                await DailyCareService.WakeChildAsync(child.ChildId, "Woke naturally", null);
                await NotificationService.ShowSuccessAsync($"{child.ChildName} woke up.");
            }
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task<List<EnjoyEveryday.Domain.Entities.Learning.TodayActivityItemDto>> GetCurrentClassroomActivitiesAsync()
    {
        if (_activeClassroom == null) return new();
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var feed = await PlannerService.GetTodayActivitiesFeedAsync(today, classroomId: _activeClassroom.Id);
            if (feed.Activities != null && feed.Activities.Any())
            {
                return feed.Activities;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not fetch planned activities feed for classroom");
        }

        // Fallback from TodayRhythm
        return _board.TodayRhythm?
            .Where(r => !r.IsCelebration)
            .Select(r => new EnjoyEveryday.Domain.Entities.Learning.TodayActivityItemDto
            {
                ScheduleId = r.Id,
                Title = r.Title,
                TimeOfDay = r.TimeDisplay,
                TimeRangeDisplay = r.TimeDisplay,
                Status = r.Status,
                ClassroomId = _activeClassroom.Id
            }).ToList() ?? new();
    }

    private async Task OpenQuickMomentModalAsync(ClassroomChildCareStatusDto child)
    {
        _activeDiaperChildId = null;
        _activeMealChildId = null;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (!child.IsCheckedIn)
        {
            await NotificationService.ShowWarningAsync($"{child.ChildName} is {child.AttendanceStatus}. Observations can only be recorded for checked-in children.");
            return;
        }

        if (_momentDialog != null)
        {
            var activities = await GetCurrentClassroomActivitiesAsync();
            var activeExp = ActiveRunningExperience ?? NextPlannedExperience ?? _board.TodayRhythm?.FirstOrDefault(r => !r.IsCelebration);

            await _momentDialog.OpenForChildAsync(
                child.ChildId,
                child.ChildName,
                _activeClassroom?.Id,
                experienceTitle: activeExp?.Title,
                experienceScheduleId: activeExp?.Id,
                plannedExperiences: activities);
        }
    }

    private async Task OpenGeneralMomentModalAsync()
    {
        _activeDiaperChildId = null;
        _activeMealChildId = null;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        if (_momentDialog != null)
        {
            var activities = await GetCurrentClassroomActivitiesAsync();
            var activeExp = ActiveRunningExperience ?? NextPlannedExperience ?? _board.TodayRhythm?.FirstOrDefault(r => !r.IsCelebration);

            await _momentDialog.OpenGeneralAsync(
                _activeClassroom?.Id,
                experienceTitle: activeExp?.Title,
                experienceScheduleId: activeExp?.Id,
                plannedExperiences: activities);
        }
    }

    private async Task OnMomentSavedAsync()
    {
        await NotificationService.ShowSuccessAsync("Journey moment and observation recorded!");
        await RefreshBoardAsync();
    }

    private void OpenGroupSnackModal()
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _showGroupSnackModal = true;
    }

    private void OpenPottyChecklistModal()
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _showPottyChecklistModal = true;
    }

    private void OpenTimerModal()
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _showTimerModal = true;
    }

    private async Task SaveGroupSnackAsync()
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        var presentIds = _classroomChildren.Where(c => c.IsCheckedIn).Select(c => c.ChildId).ToList();
        if (!presentIds.Any())
        {
            await NotificationService.ShowInfoAsync("No children are currently checked in.");
            _showGroupSnackModal = false;
            return;
        }

        try
        {
            await DailyCareService.RecordBatchCareEventsAsync(
                presentIds,
                CareEventTypes.Meal,
                _groupSnackAmount,
                _groupSnackNotes,
                parentVisible: true);

            _showGroupSnackModal = false;
            await NotificationService.ShowSuccessAsync($"Group snack recorded for {presentIds.Count} children.");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task StartGroupNapAsync()
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        var awakeIds = _classroomChildren.Where(c => c.IsCheckedIn && c.ActiveNapSession == null).Select(c => c.ChildId).ToList();
        if (!awakeIds.Any())
        {
            await NotificationService.ShowInfoAsync("No awake children are checked in right now.");
            return;
        }

        try
        {
            await DailyCareService.StartBatchNapAsync(awakeIds, _activeClassroom.Id, "Classroom rest time");
            await NotificationService.ShowSuccessAsync($"Rest time started for {awakeIds.Count} children.");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private IEnumerable<ClassroomChildCareStatusDto> GetFilteredChildren()
    {
        IEnumerable<ClassroomChildCareStatusDto> query = _rosterFilter switch
        {
            "Present" => _classroomChildren.Where(c => c.IsCheckedIn),
            "Expected" => _classroomChildren.Where(c => !c.IsCheckedIn && c.AttendanceStatus != "Absent" && c.AttendanceStatus != "CheckedOut"),
            "OutAbsent" => _classroomChildren.Where(c => c.AttendanceStatus == "Absent" || c.AttendanceStatus == "CheckedOut"),
            "Resting" => _classroomChildren.Where(c => c.IsCheckedIn && c.ActiveNapSession != null),
            _ => _classroomChildren
        };

        if (!string.IsNullOrWhiteSpace(_childSearchQuery))
        {
            var term = _childSearchQuery.Trim();
            query = query.Where(c => !string.IsNullOrWhiteSpace(c.ChildName) && c.ChildName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private string GetChildInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "C";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return $"{char.ToUpper(parts[0][0])}";
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        var dt = DateTime.Today.Add(ts);
        return dt.ToString("h:mm tt");
    }

    // =========================================================================
    // FEATURE 1: 15-SECOND PICKUP CHEAT-SHEET
    // =========================================================================
    private void OpenPickupCheatSheet(ClassroomChildCareStatusDto child)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _activeCheatSheetChild = child;
    }

    private string GetChildNapSummary(ClassroomChildCareStatusDto child)
    {
        if (child.ActiveNapSession != null)
        {
            return $"Currently resting since {child.ActiveNapSession.StartTime.ToLocalTime():h:mm tt}";
        }
        if (child.LastCompletedNapSession != null)
        {
            var endStr = child.LastCompletedNapSession.EndTime.HasValue ? child.LastCompletedNapSession.EndTime.Value.ToLocalTime().ToString("h:mm tt") : "woke up";
            var outcome = !string.IsNullOrWhiteSpace(child.LastCompletedNapSession.Outcome) ? child.LastCompletedNapSession.Outcome : (!string.IsNullOrWhiteSpace(child.LastCompletedNapSession.Notes) ? child.LastCompletedNapSession.Notes : "Rested well");
            return $"Rested {child.LastCompletedNapSession.StartTime.ToLocalTime():h:mm tt} – {endStr} ({outcome})";
        }
        return "Active and awake all day (no scheduled nap)";
    }

    private string GetChildMealSummary(ClassroomChildCareStatusDto child)
    {
        if (child.LastMealEvent != null)
        {
            return $"{child.LastMealEvent.MealType ?? "Lunch"}: Ate {child.LastMealEvent.AmountConsumed ?? "well"} ({child.LastMealEvent.EventTime.ToLocalTime():h:mm tt})";
        }
        return "Provided classroom snack & water hydration";
    }

    private string GetChildExperienceSummary(ClassroomChildCareStatusDto child)
    {
        var activeExp = ActiveRunningExperience ?? NextPlannedExperience ?? _board.TodayRhythm?.FirstOrDefault();
        if (activeExp != null)
        {
            return $"{activeExp.Title} — {(!string.IsNullOrWhiteSpace(activeExp.ScopeDisplay) ? activeExp.ScopeDisplay : "Creative Exploration")}";
        }
        return "Engaged in hands-on classroom exploration, story time, and motor play";
    }

    private string GetSuggestedParentGreeting(ClassroomChildCareStatusDto child)
    {
        var activeExp = ActiveRunningExperience ?? NextPlannedExperience ?? _board.TodayRhythm?.FirstOrDefault();
        if (activeExp != null)
        {
            return $"Ask {child.ChildName} about the \"{activeExp.Title}\" activity we explored together today!";
        }
        return $"{child.ChildName} had a fantastic day playing and discovering with friends!";
    }

    private void CheckOutFromCheatSheet(Guid childId)
    {
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            _ = NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        _activeCheatSheetChild = null;
        CheckOutChildAsync(childId);
    }

    // =========================================================================
    // FEATURE 4: GROUP CARE QUICK-TICKERS (WHOLE-TABLE SYNC)
    // =========================================================================
    private async Task RecordWholeTableLunchAsync()
    {
        if (_activeClassroom == null) return;
        if (IsBranchClosed || IsClassroomOperationsLocked)
        {
            await NotificationService.ShowWarningAsync(GetClassroomLockedMessage());
            return;
        }
        var checkedIn = _classroomChildren.Where(c => c.IsCheckedIn).ToList();
        if (!checkedIn.Any())
        {
            await NotificationService.ShowWarningAsync("No children currently checked in to record whole-table lunch.");
            return;
        }

        try
        {
            foreach (var child in checkedIn)
            {
                await DailyCareService.RecordCareEventAsync(new ChildCareEvent
                {
                    ChildId = child.ChildId,
                    ClassroomId = _activeClassroom.Id,
                    BranchId = _activeClassroom.BranchId,
                    EventType = CareEventTypes.Meal,
                    MealType = "Lunch",
                    AmountConsumed = "Most (75%)",
                    EventTime = DateTime.UtcNow,
                    ParentVisible = true
                });
            }

            await NotificationService.ShowSuccessAsync($"Recorded lunch (75% Most) for all {checkedIn.Count} seated children.");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    // =========================================================================
    // FEATURE 5: TRANSITION CHIME & VISUAL GENTLE TIMER
    // =========================================================================
    private void SetTimerPreset(int seconds)
    {
        _timerTotalSeconds = seconds;
        _timerRemainingSeconds = seconds;
        _isTimerRunning = false;
        _transitionCountdownTimer?.Dispose();
    }

    private void StartTransitionTimer()
    {
        if (_timerRemainingSeconds <= 0)
        {
            _timerRemainingSeconds = _timerTotalSeconds;
        }

        _isTimerRunning = true;
        _transitionCountdownTimer?.Dispose();
        _transitionCountdownTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                if (!_isTimerRunning) return;

                if (_timerRemainingSeconds > 0)
                {
                    _timerRemainingSeconds--;
                    StateHasChanged();
                }

                if (_timerRemainingSeconds == 0)
                {
                    _isTimerRunning = false;
                    _transitionCountdownTimer?.Dispose();
                    StateHasChanged();
                    try
                    {
                        await JS.InvokeVoidAsync("voiceRecognition.playGentleChime");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Could not play gentle chime.");
                    }
                }
            });
        }, null, 1000, 1000);
    }

    private void PauseTransitionTimer()
    {
        _isTimerRunning = false;
        _transitionCountdownTimer?.Dispose();
    }

    private void ResetTransitionTimer()
    {
        _isTimerRunning = false;
        _transitionCountdownTimer?.Dispose();
        _timerRemainingSeconds = _timerTotalSeconds;
    }

    private async Task PlayChimeSampleAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("voiceRecognition.playGentleChime");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not play gentle chime.");
        }
    }

    private string FormatTimerSeconds(int totalSec)
    {
        return $"{totalSec / 60:D2}:{totalSec % 60:D2}";
    }

    private int GetTimerProgressPercent()
    {
        if (_timerTotalSeconds <= 0) return 0;
        var elapsed = _timerTotalSeconds - _timerRemainingSeconds;
        return Math.Clamp((int)((elapsed / (double)_timerTotalSeconds) * 100), 0, 100);
    }

    // =========================================================================
    // FEATURE 6: REAL-TIME CO-TEACHER LIVE SYNC
    // =========================================================================
    private async Task PerformBackgroundCoTeacherSyncAsync()
    {
        if (_activeClassroom == null) return;
        try
        {
            var date = DateOnly.FromDateTime(DateTime.Today);
            var feed = await DailyCareService.GetClassroomDailyCareFeedAsync(_activeClassroom.BranchId, _activeClassroom.Id, date);
            var updatedFeed = feed.ClassroomOverview ?? new();

            bool hasChanges = updatedFeed.Count != _classroomChildren.Count;
            if (!hasChanges)
            {
                foreach (var child in updatedFeed)
                {
                    var existing = _classroomChildren.FirstOrDefault(c => c.ChildId == child.ChildId);
                    if (existing == null) { hasChanges = true; break; }
                    if ((child.LastDiaperEvent?.EventTime > _lastSyncCheckTime) ||
                        (child.LastMealEvent?.EventTime > _lastSyncCheckTime))
                    {
                        hasChanges = true;
                        break;
                    }
                }
            }

            if (hasChanges)
            {
                _classroomChildren = updatedFeed;
                _lastSyncCheckTime = DateTimeOffset.UtcNow;
                _showLiveSyncFlash = true;
                StateHasChanged();
                _ = Task.Delay(3500).ContinueWith(_ => InvokeAsync(() =>
                {
                    _showLiveSyncFlash = false;
                    StateHasChanged();
                }));
            }
        }
        catch
        {
            // Background sync silent on transient errors
        }
    }

    private async Task ManualCoTeacherSyncAsync()
    {
        await RefreshBoardAsync();
        _showLiveSyncFlash = true;
        await NotificationService.ShowSuccessAsync("Classroom roster & care logs synchronized with co-teachers.");
        _ = Task.Delay(3500).ContinueWith(_ => InvokeAsync(() =>
        {
            _showLiveSyncFlash = false;
            StateHasChanged();
        }));
    }
}
