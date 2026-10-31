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
    private ExperienceService ExperienceService { get; set; } = default!;

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
        if (_readinessSummary == null || _selectedReadinessExperience == null) return;
        var missing = _readinessSummary.Materials.Where(m => !m.IsAvailableInStock && !m.IsOrdered).ToList();
        if (!missing.Any())
        {
            _readinessActionMessage = "All materials are already accounted for!";
            return;
        }

        _isSubmittingOrder = true;
        _readinessActionMessage = null;
        try
        {
            var targetOrgId = UserContext.OrganizationId ?? _classroom?.BranchId ?? Guid.Empty;
            var req = new CreateMaterialOrderRequest
            {
                OrganizationId = targetOrgId,
                ClassroomId = _classroom?.Id,
                PlannedExperienceId = _selectedReadinessExperience.Id,
                RequiredDeliveryDate = _selectedReadinessSchedule?.ScheduledDate != null
                    ? new DateTimeOffset(_selectedReadinessSchedule.ScheduledDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                    : DateTimeOffset.UtcNow.AddDays(3),
                DeliveryAddress = _classroom?.Name ?? "Main Daycare Center",
                Notes = $"Order generated from Experience Planning for activity '{_selectedReadinessExperience.Title}'",
                Items = missing.Select(m => new CreateMaterialOrderItemRequest
                {
                    ProductId = m.MatchedProductId ?? Guid.NewGuid(),
                    Quantity = 1,
                    FulfilmentMode = m.CanBorrow ? MaterialFulfilmentMode.Borrow : MaterialFulfilmentMode.Purchase,
                    LoanDurationDays = m.CanBorrow ? 14 : 0
                }).ToList()
            };

            var order = await MaterialMarketplaceService.SubmitMaterialOrderAsync(req);
            _readinessActionMessage = $"Order {order.OrderNumber} submitted successfully! Central warehouse has received your request.";
            _readinessSummary = await MaterialMarketplaceService.CheckExperienceMaterialReadinessAsync(_selectedReadinessExperience.Id, _classroom?.Id ?? Guid.Empty);
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

        var exps = await ExperienceService.GetExperiencesAsync();
        _experiences = exps.ToDictionary(e => e.Id);

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
            var schedules = await PlannerService.GetMonthlyScheduleAsync(_classroom.Id, _currentDate.Year, _currentDate.Month);
            _schedules = schedules.ToList();
            CalculateDynamicCards();
        }

        try
        {
            _events = (await EventService.GetEventsAsync()).ToList();
            _eventClassroomsMap.Clear();
            foreach (var ev in _events.Where(e => e.Scope == "Classroom"))
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

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private void CalculateDynamicCards()
    {
        _exploreCount = 0; _createCount = 0; _togetherCount = 0; _moveCount = 0;
        var allGoals = new List<string>();
        var allMaterials = new List<string>();
        var allAdaptations = new List<string>();

        foreach (var s in _schedules)
        {
            if (_experiences.TryGetValue(s.ExperienceId, out var exp))
            {
                if (!string.IsNullOrWhiteSpace(exp.DnaPayload))
                {
                    try 
                    {
                        var dna = JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload, _jsonOptions);
                        if (dna != null)
                        {
                            // Count dimensions present in this experience
                            if (!string.IsNullOrWhiteSpace(dna.Challenge)) _exploreCount++;
                            if (!string.IsNullOrWhiteSpace(dna.Create)) _createCount++;
                            if (!string.IsNullOrWhiteSpace(dna.Together)) _togetherCount++;
                            if ((dna.Environment != null && dna.Environment.Contains("Outdoor", StringComparison.OrdinalIgnoreCase)) ||
                                (exp.Title != null && (exp.Title.Contains("Scavenger", StringComparison.OrdinalIgnoreCase) || exp.Title.Contains("Dance", StringComparison.OrdinalIgnoreCase) || exp.Title.Contains("Forest", StringComparison.OrdinalIgnoreCase))))
                            {
                                _moveCount++;
                            }

                            if (dna.DevelopmentalGoals != null && dna.DevelopmentalGoals.Any()) 
                                allGoals.AddRange(dna.DevelopmentalGoals);
                            if (dna.Materials != null && dna.Materials.Any()) 
                                allMaterials.AddRange(dna.Materials);
                            if (dna.ExecutionGuide?.Adaptations != null && dna.ExecutionGuide.Adaptations.Any()) 
                                allAdaptations.AddRange(dna.ExecutionGuide.Adaptations);
                        }

                        // Also parse any lowercase json properties if not captured
                        using var doc = JsonDocument.Parse(exp.DnaPayload);
                        if (doc.RootElement.TryGetProperty("materials", out var matArr) && matArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in matArr.EnumerateArray())
                            {
                                var mStr = item.GetString();
                                if (!string.IsNullOrWhiteSpace(mStr)) allMaterials.Add(mStr);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        // Total rhythm sum used for relative mix percentages
        _totalRhythmCount = _exploreCount + _createCount + _togetherCount + _moveCount;
        if (_totalRhythmCount == 0) _totalRhythmCount = 1;

        if (allGoals.Any())
        {
            var top = allGoals.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key).ToList();
            _topGoal = top.First();
            _topGoalsList = top;
        }
        else
        {
            _topGoal = "Tactile exploration and sensory vocabulary";
            _topGoalsList = new List<string> { "Color recognition and sorting", "Fine motor coordination using tools", "Cooperative play and turn-taking" };
        }

        if (allMaterials.Any())
        {
            _commonMaterial = allMaterials.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "craft materials";
            _aiSuggestionsList = new List<string> 
            { 
                $"Plan an activity centered around {_commonMaterial}", 
                "Combine this with a new environment to spark curiosity" 
            };
        }
        else
        {
            _commonMaterial = "natural materials";
            _aiSuggestionsList = new List<string> 
            { 
                "Plan an activity centered around sensory textures", 
                "Combine this with a new environment to spark curiosity" 
            };
        }

        if (allAdaptations.Any())
        {
            _adaptationsList = allAdaptations.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        }
        else
        {
            _adaptationsList = new List<string> { "Indoor obstacle course (Move)", "Storytime with props (Together)", "Sensory bins with water/sand (Explore)" };
        }
    }

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
        _wizardDefaultDate = date;
        _wizardScheduleId = null;
        _isPlanWizardOpen = true;
    }

    private void EditSchedule(ExperienceSchedule schedule)
    {
        _wizardDefaultDate = schedule.ScheduledDate;
        _wizardScheduleId = schedule.Id;
        _isPlanWizardOpen = true;
    }

    private DateOnly GetDefaultPlanDate()
    {
        var today = DateTime.Today;
        if (today.Year == _currentDate.Year && today.Month == _currentDate.Month)
        {
            return DateOnly.FromDateTime(today);
        }
        return new DateOnly(_currentDate.Year, _currentDate.Month, 1);
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
