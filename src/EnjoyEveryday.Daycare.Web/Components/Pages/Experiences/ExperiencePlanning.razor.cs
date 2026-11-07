using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.UI.Shared.Components.Planning;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

public partial class ExperiencePlanning : ComponentBase
{
    [Inject]
    private PlannerService PlannerService { get; set; } = default!;

    [Inject]
    private ClassroomService ClassroomService { get; set; } = default!;

    [Inject]
    private BranchService BranchService { get; set; } = default!;

    [Inject]
    private OrganizationService OrganizationService { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    [Inject]
    private EventService EventService { get; set; } = default!;

    [Inject]
    private IMaterialMarketplaceService MaterialMarketplaceService { get; set; } = default!;

    [Inject]
    private IUserContext UserContext { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.UI.Shared.Services.IAppNotificationService NotificationService { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.Daycare.Web.Services.IPageAuthorizationService PageAuth { get; set; } = default!;

    private bool _canPlan = false;

    private bool _isMaterialReadinessModalOpen = false;
    private ExperienceSchedule? _selectedReadinessSchedule;
    private Experience? _selectedReadinessExperience;
    private ExperienceMaterialReadinessSummary? _readinessSummary;
    private bool _isLoadingReadiness = false;
    private bool _isSubmittingOrder = false;
    private string? _readinessActionMessage;

    private async Task OpenMaterialReadiness(ExperienceSchedule schedule, Experience experience)
    {
        _selectedReadinessSchedule = schedule;
        _selectedReadinessExperience = experience;
        _readinessActionMessage = null;
        _isLoadingReadiness = true;
        _isMaterialReadinessModalOpen = true;

        try
        {
            _readinessSummary = await MaterialMarketplaceService.CheckExperienceMaterialReadinessAsync(experience.Id, _classroom?.Id ?? Guid.Empty);
        }
        catch (Exception ex)
        {
            _readinessActionMessage = $"Error checking stock: {ex.Message}";
        }
        finally
        {
            _isLoadingReadiness = false;
        }
    }

    private void CloseMaterialReadiness()
    {
        _isMaterialReadinessModalOpen = false;
        _selectedReadinessSchedule = null;
        _selectedReadinessExperience = null;
        _readinessSummary = null;
        _readinessActionMessage = null;
    }

    private async Task OneClickOrderMissingMaterials()
    {
        if (_selectedReadinessSchedule == null || _selectedReadinessExperience == null) return;

        _isSubmittingOrder = true;
        _readinessActionMessage = null;
        try
        {
            var req = new EnjoyEveryday.Domain.Entities.Learning.OrderMissingMaterialsRequest
            {
                ScheduleId = _selectedReadinessSchedule.Id,
                ExperienceId = _selectedReadinessExperience.Id,
                ClassroomId = _classroom?.Id,
                OrganizationId = UserContext.OrganizationId ?? _classroom?.BranchId ?? Guid.Empty,
                DeliveryAddress = _classroom?.Name ?? "Main Daycare Center"
            };

            var result = await PlannerService.OrderMissingMaterialsForScheduleAsync(req);
            _readinessActionMessage = result.Message;
            if (result.UpdatedReadiness is ExperienceMaterialReadinessSummary updated)
            {
                _readinessSummary = updated;
            }
            else
            {
                _readinessSummary = await MaterialMarketplaceService.CheckExperienceMaterialReadinessAsync(_selectedReadinessExperience.Id, _classroom?.Id ?? Guid.Empty);
            }
        }
        catch (Exception ex)
        {
            _readinessActionMessage = $"Unable to place order: {ex.Message}";
        }
        finally
        {
            _isSubmittingOrder = false;
        }
    }

    private string _activeFilter = "all";
    private bool _isPlanWizardOpen = false;
    private Guid? _wizardScheduleId;
    private Classroom? _classroom;
    private List<ExperienceSchedule> _schedules = new();
    private Dictionary<Guid, Experience> _experiences = new();
    private List<Event> _events = new();
    private bool _isCalendarEventModalOpen = false;
    private string? _calendarEventErrorMessage;
    private Event? _selectedCalendarEvent;
    private List<EventTask> _selectedCalendarEventTasks = new();

    private bool IsAdmin =>
        UserContext.Roles.Any(r => 
            r.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("OrganizationOwner", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("OrganizationAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BranchAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Director", StringComparison.OrdinalIgnoreCase))
        || (!UserContext.Roles.Any(r => r.Equals("Teacher", StringComparison.OrdinalIgnoreCase) || r.Equals("AssistantTeacher", StringComparison.OrdinalIgnoreCase) || r.Equals("Parent", StringComparison.OrdinalIgnoreCase)) && UserContext.HasPermission("event.manage"));

    private List<PlanningControlBar.FilterItem> _filters = new()
    {
        new PlanningControlBar.FilterItem { Text = "All", Value = "all" },
        new PlanningControlBar.FilterItem { Text = "Management", Value = "management" },
        new PlanningControlBar.FilterItem { Text = "Teacher", Value = "teacher" },
        new PlanningControlBar.FilterItem { Text = "AI", Value = "ai" },
        new PlanningControlBar.FilterItem { Text = "Free exploration", Value = "free" }
    };

    private void FilterSelected(string filter)
    {
        _activeFilter = filter;
    }

    private bool _isWeeklyAssistantOpen = false;
    private DateTime _currentDate;
    private DateOnly? _wizardDefaultDate;
    private List<Classroom> _allClassrooms = new();
    private List<Branch> _allBranches = new();

    private Guid _selectedBranchId;
    private Guid SelectedBranchId
    {
        get => _selectedBranchId;
        set
        {
            if (_selectedBranchId != value)
            {
                _selectedBranchId = value;
                var cls = _allClassrooms.FirstOrDefault(c => !_allBranches.Any() || c.BranchId == value);
                SelectedClassroomId = cls?.Id;
            }
        }
    }

    private Guid? _selectedClassroomId;
    private Guid? SelectedClassroomId
    {
        get => _selectedClassroomId;
        set
        {
            if (_selectedClassroomId != value)
            {
                _selectedClassroomId = value;
                _classroom = _allClassrooms.FirstOrDefault(c => c.Id == value);
                if (_classroom != null && _classroom.BranchId != _selectedBranchId && _allBranches.Any())
                {
                    _selectedBranchId = _classroom.BranchId;
                }
                _ = RefreshSchedules();
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var profile = await PageAuth.GetUserProfileAsync();
        _canPlan = profile.IsOrgAdmin || profile.IsBranchAdmin || profile.IsLeadTeacher || UserContext.HasPermission(EnjoyEveryday.Shared.Authorization.Permissions.ExperienceSchedule);
        if (!_canPlan)
        {
            NavigationManager.NavigateTo("/today-activities");
            return;
        }

        var today = TimeProvider.GetLocalNow().Date;
        _currentDate = new DateTime(today.Year, today.Month, 1);

        var orgs = await OrganizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org != null)
        {
            _allBranches = (await BranchService.GetBranchesAsync(org.Id)).ToList();
        }

        _allClassrooms = (await ClassroomService.GetAllClassroomsAsync()).ToList();
        
        // Select first branch and its first classroom by default
        var defaultBranch = _allBranches.FirstOrDefault();
        if (defaultBranch != null)
        {
            _selectedBranchId = defaultBranch.Id;
            _classroom = _allClassrooms.FirstOrDefault(c => c.BranchId == _selectedBranchId);
            _selectedClassroomId = _classroom?.Id;
        }

        await LoadSchedules();
    }

    private async Task PreviousMonth()
    {
        _currentDate = _currentDate.AddMonths(-1);
        await LoadSchedules();
    }

    private async Task NextMonth()
    {
        _currentDate = _currentDate.AddMonths(1);
        await LoadSchedules();
    }

    private Dictionary<Guid, HashSet<Guid>> _eventClassroomsMap = new();

    private IEnumerable<Event> GetEventsForDateAndScope(DateOnly date)
    {
        return _events.Where(e => e.EventDate == date && 
            (e.Status == "Planned" || e.Status == "Approved" || e.Status == "Published" || e.Status == "InProgress" || e.Status == "Completed") &&
            DoesEventApplyToCurrentScope(e));
    }

    private bool DoesEventApplyToCurrentScope(Event evt)
    {
        if (evt.Scope == "Organization" || evt.IsOrganizationWide) return true;
        if (_classroom != null)
        {
            if (evt.Scope == "Branch" && evt.BranchId == _classroom.BranchId) return true;
            if (evt.Scope == "Classroom" && _eventClassroomsMap.TryGetValue(evt.Id, out var crIds) && crIds.Contains(_classroom.Id)) return true;
            return false;
        }
        else if (SelectedBranchId != Guid.Empty)
        {
            if (evt.Scope == "Branch" && evt.BranchId == SelectedBranchId) return true;
            return false;
        }
        return true;
    }

    private async Task LoadSchedules()
    {
        if (_classroom != null)
        {
            var summary = await PlannerService.GetMonthlyCalendarSummaryAsync(_classroom.Id, _currentDate.Year, _currentDate.Month);
            _schedules = summary.Schedules;
            _experiences = summary.Experiences.ToDictionary(e => e.Id);
            _exploreCount = summary.ExploreCount;
            _createCount = summary.CreateCount;
            _togetherCount = summary.TogetherCount;
            _moveCount = summary.MoveCount;
            _totalRhythmCount = summary.TotalRhythmCount;
            _topGoal = summary.TopGoal;
            _topGoalsList = summary.TopGoalsList;
            _commonMaterial = summary.CommonMaterial;
            _aiSuggestionsList = summary.AiSuggestionsList;
            _adaptationsList = summary.AdaptationsList;
        }

        try
        {
            _events = (await EventService.GetEventsAsync()).ToList();
            _eventClassroomsMap.Clear();
            var classroomEvents = _events.Where(e => e.Scope == "Classroom" && e.EventDate.Year == _currentDate.Year && e.EventDate.Month == _currentDate.Month).ToList();
            foreach (var ev in classroomEvents)
            {
                var crIds = await EventService.GetEventClassroomsAsync(ev.Id);
                _eventClassroomsMap[ev.Id] = crIds.ToHashSet();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading events for planning calendar: {ex.Message}");
        }
    }

    private int _exploreCount = 0;
    private int _createCount = 0;
    private int _togetherCount = 0;
    private int _moveCount = 0;
    private int _totalRhythmCount = 1;

    private string _topGoal = "Community Building";
    private List<string> _topGoalsList = new() { "Collaboration", "Empathy" };
    
    private string _commonMaterial = "natural materials";
    private List<string> _aiSuggestionsList = new() { "Add more materials to the block area", "Plan a collaborative outdoor experience" };

    private List<string> _adaptationsList = new() { "Indoor obstacle course (Move)", "Storytime with props (Together)" };

    private async Task RefreshSchedules()
    {
        await LoadSchedules();
        StateHasChanged();
    }

    private async Task OnPlanCreatedFromWizard(Guid classroomId)
    {
        if (SelectedClassroomId != classroomId)
        {
            SelectedClassroomId = classroomId; // This setter already calls RefreshSchedules
        }
        else
        {
            await RefreshSchedules();
        }
    }

    private void OpenPlanWizard()
    {
        _wizardDefaultDate = GetDefaultPlanDate();
        _wizardScheduleId = null;
        _isPlanWizardOpen = true;
    }

    private void OpenPlanWizardForDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(TimeProvider.GetLocalNow().Date);
        if (date < today)
        {
            _ = NotificationService.ShowWarningAsync("Cannot create or modify experiences for past dates.");
            return;
        }
        if (PlannerService.IsDaycareClosedForPlanning(date, out var closedReason))
        {
            _ = NotificationService.ShowWarningAsync(closedReason);
            return;
        }

        _wizardDefaultDate = date;
        _wizardScheduleId = null;
        _isPlanWizardOpen = true;
    }

    private void EditSchedule(ExperienceSchedule schedule)
    {
        var today = DateOnly.FromDateTime(TimeProvider.GetLocalNow().Date);
        if (schedule.ScheduledDate < today)
        {
            _ = NotificationService.ShowWarningAsync(
                $"Historical experiences ({schedule.ScheduledDate:MMM d}) are locked and cannot be edited.");
            return;
        }

        _wizardDefaultDate = schedule.ScheduledDate;
        _wizardScheduleId = schedule.Id;
        _isPlanWizardOpen = true;
    }

    private void DraftPlanAroundAiSuggestion()
    {
        if (SelectedClassroomId.HasValue)
        {
            _isWeeklyAssistantOpen = true;
            _ = NotificationService.ShowInfoAsync(
                $"AI Weekly Assistant opened with focus on '{_commonMaterial}'. Click '✦ Generate Weekly Draft' to build experiences around this interest.");
        }
        else
        {
            OpenPlanWizard();
        }
    }

    private DateOnly GetDefaultPlanDate()
    {
        var today = DateTime.Today;
        DateOnly candidate = (today.Year == _currentDate.Year && today.Month == _currentDate.Month)
            ? DateOnly.FromDateTime(today)
            : new DateOnly(_currentDate.Year, _currentDate.Month, 1);

        for (int i = 0; i < 7; i++)
        {
            if (!PlannerService.IsDaycareClosedForPlanning(candidate, out _))
            {
                return candidate;
            }
            candidate = candidate.AddDays(1);
        }
        return candidate;
    }

    private void ClosePlanWizard()
    {
        _isPlanWizardOpen = false;
        _wizardScheduleId = null;
    }

    private IEnumerable<ExperienceSchedule> GetSchedulesForDayAndPeriod(DateTime date, string period)
    {
        return _schedules.Where(s => s.ScheduledDate == DateOnly.FromDateTime(date) && s.TimeOfDay == period);
    }

    private string GetExperienceTitle(Guid expId)
    {
        return _experiences.ContainsKey(expId) ? _experiences[expId].Title : "Unknown";
    }

    private string GetExperienceType(Experience experience)
    {
        if (string.IsNullOrEmpty(experience.DnaPayload)) return "Experience";
        try 
        {
            var json = System.Text.Json.JsonDocument.Parse(experience.DnaPayload);
            if (json.RootElement.TryGetProperty("type", out var typeElement))
            {
                return typeElement.GetString() ?? "Experience";
            }
        }
        catch { }
        return "Experience";
    }

    private async Task CancelSchedule(Guid scheduleId)
    {
        var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (schedule != null && schedule.Status != "Cancelled")
        {
            await PlannerService.CancelScheduleAsync(scheduleId);
            await RefreshSchedules();
        }
    }

    private async Task OpenEventFromCalendar(Event evt)
    {
        _selectedCalendarEvent = evt;
        try
        {
            _selectedCalendarEventTasks = (await EventService.GetTasksAsync(evt.Id)).ToList();
        }
        catch
        {
            _selectedCalendarEventTasks = new();
        }
        _isCalendarEventModalOpen = true;
    }

    private void CloseCalendarEventModal()
    {
        _calendarEventErrorMessage = null;
        _isCalendarEventModalOpen = false;
        _selectedCalendarEvent = null;
        _selectedCalendarEventTasks.Clear();
    }

    private async Task ToggleCalendarEventTask(EventTask task)
    {
        if (!IsAdmin) return;
        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTimeOffset.UtcNow : null;
        await EventService.UpdateTaskAsync(task);
    }

    private async Task SaveCalendarEvent()
    {
        if (!IsAdmin || _selectedCalendarEvent == null) return;
        _calendarEventErrorMessage = null;
        try
        {
            await EventService.UpdateEventAsync(_selectedCalendarEvent);
            await LoadSchedules();
            CloseCalendarEventModal();
        }
        catch (Exception ex)
        {
            _calendarEventErrorMessage = ex.Message;
            StateHasChanged();
        }
    }

    private async Task DeleteCalendarEvent()
    {
        if (!IsAdmin || _selectedCalendarEvent == null) return;
        await EventService.DeleteEventAsync(_selectedCalendarEvent.Id);
        await LoadSchedules();
        CloseCalendarEventModal();
    }

    private string FormatEventTiming(Event evt)
    {
        if (evt.StartTime == null) return "All Day Celebration";
        var start = evt.StartTime.Value.ToString(@"hh\:mm");
        if (evt.EndTime == null) return start;
        var end = evt.EndTime.Value.ToString(@"hh\:mm");
        return $"{start} - {end}";
    }

    private void NavigateToCalendarEvent(Guid id)
    {
        NavigationManager.NavigateTo($"/events/{id}");
    }
}
