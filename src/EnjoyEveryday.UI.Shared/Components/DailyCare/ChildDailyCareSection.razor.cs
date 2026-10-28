using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.UI.Shared.Components.DailyCare;

public partial class ChildDailyCareSection : ComponentBase
{
    [Inject] private IChildDailyCareService DailyCareService { get; set; } = default!;
    [Inject] private ClassroomService ClassroomService { get; set; } = default!;
    [Inject] private BranchService BranchService { get; set; } = default!;
    [Inject] private OrganizationService OrganizationService { get; set; } = default!;
    [Inject] private ITenantContext TenantContext { get; set; } = default!;
    [Inject] private IUserContext UserContext { get; set; } = default!;
    [Inject] private EnjoyEveryday.Domain.Repositories.IAccessScopeRepository AccessScopeRepo { get; set; } = default!;

    private bool _isTeacherOnly = false;
    private HashSet<Guid> _assignedClassroomIds = new();

    private List<Branch> _branches = new();
    private List<Classroom> _classrooms = new();
    private Guid _selectedBranchId = Guid.Empty;
    private Guid? _selectedClassroomId;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);

    private List<ClassroomChildCareStatus> _classroomOverview = new();
    private Guid _selectedChildId = Guid.Empty;
    private ClassroomChildCareStatus? _selectedChildStatus;
    private List<ChildTimelineEntry> _childTimeline = new();

    private string _selectedActivity = CareEventTypes.DiaperChange;
    private string _selectedDetail = "Wet";
    private string _entryNote = string.Empty;
    private bool _parentVisible = true;
    private bool _isSubmitting = false;
    private string? _statusMessage;

    private int _totalRecordedEventsToday = 0;
    private List<ChildCareSupply> _lowSupplies = new();
    private List<ChildCareSupply> _childSupplies = new();
    private ChildCarePlan? _currentPlan;

    private bool _isBulkMode = false;
    private HashSet<Guid> _selectedChildIds = new();

    private bool _showCarePlanModal = false;
    private bool _showSupplyModal = false;

    // Handover state
    private bool _showHandoverModal = false;
    private string _activeHandoverTab = "submit";
    private List<TeacherHandover> _pendingHandovers = new();
    private List<TeacherHandover> _classroomHandovers = new();
    private string _newHandoverShiftType = "MorningToAfternoon";
    private string _newHandoverIncomingTeacher = "";
    private string _newHandoverSummary = "";
    private string _newHandoverChildNotes = "";
    private string _newHandoverCareTasks = "";
    private string _newHandoverFollowups = "";
    private bool _newHandoverIsCritical = false;
    private bool _isSubmittingHandover = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadBranchesAndClassrooms();
        await RefreshClassroomData();
    }

    private async Task LoadBranchesAndClassrooms()
    {
        var tenantId = TenantContext.TenantId;
        var userId = UserContext.UserId;
        var userRoles = (UserContext.Roles ?? Enumerable.Empty<string>()).ToList();
        bool isOrgAdmin = userRoles.Any(r =>
            r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase));
        bool isBranchAdmin = userRoles.Any(r => r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase));

        if (tenantId != Guid.Empty && userId != Guid.Empty)
        {
            var classroomIds = await AccessScopeRepo.GetAssignedClassroomIdsAsync(tenantId, userId);
            _assignedClassroomIds = new HashSet<Guid>(classroomIds);
        }

        _isTeacherOnly = !isOrgAdmin && !isBranchAdmin;

        var orgs = (await OrganizationService.GetOrganizationsAsync()).ToList();
        var primaryOrg = (UserContext.OrganizationId.HasValue
            ? orgs.FirstOrDefault(o => o.Id == UserContext.OrganizationId.Value)
            : null) ?? orgs.FirstOrDefault();

        if (primaryOrg != null)
        {
            _branches = (await BranchService.GetBranchesAsync(primaryOrg.Id)).ToList();
        }
        else
        {
            _branches = new List<Branch>();
        }

        if (_isTeacherOnly)
        {
            // Load all classrooms across org branches and filter by teacher assignment
            var teacherRooms = new List<Classroom>();
            foreach (var b in _branches)
            {
                var rooms = await ClassroomService.GetClassroomsAsync(b.Id);
                teacherRooms.AddRange(rooms.Where(r => _assignedClassroomIds.Contains(r.Id)));
            }
            _classrooms = teacherRooms;

            if (_classrooms.Count > 0)
            {
                _selectedClassroomId = _classrooms[0].Id;
                _selectedBranchId = _classrooms[0].BranchId;
                _branches = _branches.Where(b => b.Id == _selectedBranchId).ToList();
            }
            else
            {
                _selectedClassroomId = null;
                _selectedBranchId = Guid.Empty;
            }
        }
        else
        {
            if (_branches.Count > 0 && _selectedBranchId == Guid.Empty)
            {
                _selectedBranchId = _branches[0].Id;
            }

            if (_selectedBranchId != Guid.Empty)
            {
                _classrooms = (await ClassroomService.GetClassroomsAsync(_selectedBranchId)).ToList();
                if (_classrooms.Count > 0 && !_selectedClassroomId.HasValue)
                {
                    _selectedClassroomId = _classrooms[0].Id;
                }
            }
        }
    }

    private async Task RefreshClassroomData()
    {
        if (_selectedBranchId == Guid.Empty) return;

        _classroomOverview = (await DailyCareService.GetClassroomOverviewAsync(
            _selectedBranchId,
            _selectedClassroomId,
            _selectedDate)).ToList();

        _lowSupplies = (await DailyCareService.GetLowSuppliesAsync(_selectedBranchId)).ToList();
        _totalRecordedEventsToday = _classroomOverview.Sum(c => c.TodayEventsCount);

        try
        {
            _pendingHandovers = (await DailyCareService.GetPendingHandoversAsync(_selectedBranchId)).ToList();
            if (_selectedClassroomId.HasValue)
            {
                _classroomHandovers = (await DailyCareService.GetClassroomHandoversAsync(_selectedClassroomId.Value, _selectedDate)).ToList();
            }
            else
            {
                _classroomHandovers = _pendingHandovers.ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading handovers: {ex.Message}");
        }

        if (_selectedChildId == Guid.Empty && _classroomOverview.Count > 0)
        {
            _selectedChildId = _classroomOverview[0].ChildId;
        }

        await LoadSelectedChildDetails();
    }

    private async Task LoadSelectedChildDetails()
    {
        if (_selectedChildId == Guid.Empty)
        {
            _selectedChildStatus = null;
            _childTimeline = new();
            return;
        }

        _selectedChildStatus = _classroomOverview.FirstOrDefault(c => c.ChildId == _selectedChildId);
        _childTimeline = (await DailyCareService.GetChildCareTimelineAsync(_selectedChildId, _selectedDate)).ToList();
        _childSupplies = (await DailyCareService.GetChildSuppliesAsync(_selectedChildId)).ToList();
        _currentPlan = await DailyCareService.GetChildCarePlanAsync(_selectedChildId);
    }

    private async Task OnBranchChanged(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var bId))
        {
            _selectedBranchId = bId;
            _classrooms = (await ClassroomService.GetClassroomsAsync(_selectedBranchId)).ToList();
            _selectedClassroomId = _classrooms.FirstOrDefault()?.Id;
            _selectedChildId = Guid.Empty;
            await RefreshClassroomData();
        }
    }

    private async Task OnClassroomChanged(ChangeEventArgs e)
    {
        var val = e.Value?.ToString();
        _selectedClassroomId = string.IsNullOrEmpty(val) ? null : Guid.Parse(val);
        _selectedChildId = Guid.Empty;
        await RefreshClassroomData();
    }

    private async Task OnDateChanged(ChangeEventArgs e)
    {
        if (DateOnly.TryParse(e.Value?.ToString(), out var d))
        {
            _selectedDate = d;
            await RefreshClassroomData();
        }
    }

    private async Task OnSelectedChildChanged(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var cId))
        {
            _selectedChildId = cId;
            await LoadSelectedChildDetails();
        }
    }

    private async Task SelectChild(Guid childId)
    {
        _selectedChildId = childId;
        await LoadSelectedChildDetails();
    }

    private void SelectActivity(string activity)
    {
        _selectedActivity = activity;
        var options = GetDetailsOptionsForActivity(activity);
        _selectedDetail = options.FirstOrDefault() ?? string.Empty;
    }

    private List<string> GetDetailsOptionsForActivity(string activity) => activity switch
    {
        CareEventTypes.DiaperChange => new() { "Wet", "Soiled", "Wet & Soiled", "Dry check" },
        CareEventTypes.Meal => new() { "Lunch (Ate most)", "Lunch (Ate all)", "Lunch (Ate some)", "Morning snack (All)", "Morning snack (Some)", "Afternoon snack", "Bottle (6 oz)", "Water intake" },
        CareEventTypes.SleepRest => new() { "Fell asleep", "Rested peacefully", "Had difficulty settling", "Woke naturally", "Awakened per routine" },
        CareEventTypes.Toileting => new() { "Successful toilet use", "Attempted toilet use", "Accident & change", "Assistance provided" },
        CareEventTypes.Hygiene => new() { "Handwashing routine", "Face & hand clean", "Clothing change", "Cleaned after accident" },
        CareEventTypes.Comfort => new() { "Happy & playful", "Calm and settled", "Needed comfort / hug", "Appeared tired", "Upset & comforted" },
        _ => new() { "Completed" }
    };

    private async Task SubmitQuickCareEntry()
    {
        if (_selectedChildId == Guid.Empty) return;

        _isSubmitting = true;
        try
        {
            var ev = new ChildCareEvent
            {
                ChildId = _selectedChildId,
                EventType = _selectedActivity,
                EventTime = DateTime.UtcNow,
                Notes = _entryNote,
                ParentVisible = _parentVisible
            };

            switch (_selectedActivity)
            {
                case CareEventTypes.DiaperChange:
                    ev.DiaperCondition = _selectedDetail.Replace(" & ", "And");
                    break;
                case CareEventTypes.Meal:
                    ev.MealType = _selectedDetail.Contains("Lunch") ? "Lunch" : (_selectedDetail.Contains("snack") ? "Snack" : "Feeding");
                    ev.AmountConsumed = _selectedDetail.Contains("all") ? "All" : (_selectedDetail.Contains("most") ? "Most" : "Some");
                    break;
                case CareEventTypes.SleepRest:
                    ev.RestStatus = _selectedDetail.Replace(" ", "");
                    break;
                case CareEventTypes.Toileting:
                    ev.ToiletingActivity = _selectedDetail.Contains("Successful") ? "Success" : (_selectedDetail.Contains("Attempted") ? "Attempt" : "Assistance");
                    break;
                case CareEventTypes.Hygiene:
                    ev.HygieneType = _selectedDetail.Replace(" ", "");
                    break;
                case CareEventTypes.Comfort:
                    ev.ComfortMood = _selectedDetail.Contains("Happy") ? "Happy" : (_selectedDetail.Contains("Calm") ? "Settled" : "NeededComfort");
                    break;
            }

            await DailyCareService.RecordCareEventAsync(ev);
            _statusMessage = $"Care event '{_selectedActivity}' recorded successfully for {_selectedChildStatus?.ChildName}!";
            _entryNote = string.Empty;

            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task StartNapForChild()
    {
        if (_selectedChildId == Guid.Empty) return;
        await DailyCareService.StartNapAsync(_selectedChildId, _selectedClassroomId, "Rest session started by teacher.");
        _statusMessage = $"Nap session started for {_selectedChildStatus?.ChildName}.";
        await RefreshClassroomData();
    }

    private async Task WakeActiveChild()
    {
        if (_selectedChildId == Guid.Empty) return;
        var ended = await DailyCareService.WakeChildAsync(_selectedChildId, "WokeNaturally", "Woke naturally refreshed.");
        _statusMessage = $"Wake-up recorded. Duration: {ended?.DurationMinutes} minutes.";
        await RefreshClassroomData();
    }

    private async Task MarkSupplyLow(ChildCareSupply supply)
    {
        supply.Status = "Low";
        await DailyCareService.UpdateSupplyStatusAsync(supply);
        await RefreshClassroomData();
    }

    private async Task MarkSupplyAdequate(ChildCareSupply supply)
    {
        supply.Status = "Adequate";
        await DailyCareService.UpdateSupplyStatusAsync(supply);
        await RefreshClassroomData();
    }

    private void SetEntryMode(bool isBulk)
    {
        _isBulkMode = isBulk;
        if (_isBulkMode && _selectedChildIds.Count == 0)
        {
            // Default select all checked-in children
            SelectAllCheckedInChildren();
        }
    }

    private void ToggleChildSelection(Guid childId)
    {
        if (_selectedChildIds.Contains(childId))
            _selectedChildIds.Remove(childId);
        else
            _selectedChildIds.Add(childId);
    }

    private void SelectAllCheckedInChildren()
    {
        _selectedChildIds.Clear();
        foreach (var c in _classroomOverview.Where(c => c.IsCheckedIn))
        {
            _selectedChildIds.Add(c.ChildId);
        }
    }

    private void SelectAllChildren()
    {
        _selectedChildIds.Clear();
        foreach (var c in _classroomOverview)
        {
            _selectedChildIds.Add(c.ChildId);
        }
    }

    private void ClearSelectedChildren()
    {
        _selectedChildIds.Clear();
    }

    private async Task SubmitBulkCareEntry()
    {
        if (_selectedChildIds.Count == 0 || _isSubmitting) return;

        _isSubmitting = true;
        _statusMessage = null;

        try
        {
            var recorded = await DailyCareService.RecordBatchCareEventsAsync(
                _selectedChildIds,
                _selectedActivity,
                _selectedDetail,
                _entryNote,
                _parentVisible);

            _statusMessage = $"Bulk care event '{_selectedActivity}' ({_selectedDetail}) recorded successfully for {recorded} children!";
            _entryNote = string.Empty;

            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error recording bulk care: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task StartBulkNap()
    {
        if (_selectedChildIds.Count == 0 || _isSubmitting) return;

        _isSubmitting = true;
        try
        {
            var count = await DailyCareService.StartBatchNapAsync(
                _selectedChildIds,
                _selectedClassroomId,
                "Classroom rest period started.");

            _statusMessage = $"Rest period started for {count} children.";
            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error starting bulk rest: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task WakeBulkChildren()
    {
        if (_selectedChildIds.Count == 0 || _isSubmitting) return;

        _isSubmitting = true;
        try
        {
            var count = await DailyCareService.WakeBatchChildrenAsync(
                _selectedChildIds,
                "WokeNaturally",
                "Woke refreshed from group rest.");

            _statusMessage = $"Wake-up and duration recorded for {count} children.";
            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error waking bulk children: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task SubmitHandover()
    {
        if (!_selectedClassroomId.HasValue || _isSubmittingHandover) return;

        _isSubmittingHandover = true;
        try
        {
            var handover = new TeacherHandover
            {
                ClassroomId = _selectedClassroomId.Value,
                HandoverDate = _selectedDate,
                ShiftType = _newHandoverShiftType,
                IncomingTeacherName = _newHandoverIncomingTeacher,
                SummaryNotes = _newHandoverSummary,
                ChildSpecificNotes = _newHandoverChildNotes,
                OutstandingCareTasks = _newHandoverCareTasks,
                PendingFollowups = _newHandoverFollowups,
                IsCritical = _newHandoverIsCritical,
                Status = "PendingAcknowledgement"
            };

            await DailyCareService.SubmitHandoverAsync(handover);
            _statusMessage = $"Shift handover for {_newHandoverShiftType} recorded successfully.";
            _newHandoverSummary = "";
            _newHandoverChildNotes = "";
            _newHandoverCareTasks = "";
            _newHandoverFollowups = "";
            _newHandoverIsCritical = false;
            _activeHandoverTab = "history";
            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error recording handover: {ex.Message}";
        }
        finally
        {
            _isSubmittingHandover = false;
        }
    }

    private async Task AcknowledgeHandover(Guid handoverId)
    {
        try
        {
            await DailyCareService.AcknowledgeHandoverAsync(handoverId, "Shift handover acknowledged.");
            _statusMessage = "Shift handover acknowledged successfully.";
            await RefreshClassroomData();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error acknowledging handover: {ex.Message}";
        }
    }
}
