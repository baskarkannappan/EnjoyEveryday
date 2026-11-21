using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.UI.Shared.Components;
using EnjoyEveryday.UI.Shared.Components.ActiveSessionModal;
using EnjoyEveryday.UI.Shared.Components.Attendance;
using EnjoyEveryday.UI.Shared.Components.OperationsBoard;

namespace EnjoyEveryday.Daycare.Web.Components.Pages;

public partial class BranchAdminDashboard
{
    [Parameter] public IReadOnlySet<Guid> AssignedBranchIds { get; set; } = new HashSet<Guid>();
    [Parameter] public IReadOnlyList<string> AvailableViews { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback<string> OnChangeView { get; set; }

    private bool _loading = true;
    private OperationsBoardModel _board = new();
    private List<Branch> _branches = new();
    private Branch? _activeBranch => _branches.FirstOrDefault(b => b.Id.ToString() == _selectedBranchId);
    private string _selectedBranchId = "";
    private BranchDailyStatusDto? _branchOpsStatus;
    private string? _previewPhoto;
    private int _chatUnreadCount = 0;

    // Modals
    private bool _isMaterialsBasketOpen = false;
    private bool _isEndOfDayModalOpen = false;
    private bool _isSessionModalOpen = false;
    private Guid _activeSessionScheduleId;
    private bool _isPhotoModalOpen = false;
    private Guid _photoModalEventId;
    private string _photoModalEventTitle = "";
    private string[] _photoModalEventPhotos = Array.Empty<string>();

    private bool _isSessionPhotoModalOpen = false;
    private Guid _sessionPhotoScheduleId;
    private string _sessionPhotoTitle = "";
    private string[] _sessionPhotoUrls = Array.Empty<string>();
    private static readonly Dictionary<Guid, string[]> _sessionPhotoStore = new();

    private RhythmScheduleItem? ActiveRunningExperience => _board.TodayRhythm?.FirstOrDefault(r => 
        !r.IsCelebration && (r.Status == "Started" || r.Status == "Active" || r.Status == "InProgress"));

    private RhythmScheduleItem? NextPlannedExperience => _board.TodayRhythm?.FirstOrDefault(r => 
        !r.IsCelebration && (r.Status == "Scheduled" || r.Status == "Planned" || r.Status == "Confirmed"));

    protected override async Task OnInitializedAsync()
    {
        if (!await PageAuth.EnsureAuthorizedAsync("/management/branch-dashboard"))
        {
            return;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadBranchesAsync();
        await RefreshBoardAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var profile = await PageAuth.GetUserProfileAsync();
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                var allBranches = (await BranchService.GetBranchesAsync(orgId)).ToList();
                if (profile.IsOrgAdmin)
                {
                    _branches = allBranches;
                }
                else
                {
                    var allowedIds = AssignedBranchIds != null && AssignedBranchIds.Any()
                        ? AssignedBranchIds
                        : profile.AssignedBranchIds;
                    _branches = allBranches.Where(b => allowedIds.Contains(b.Id)).ToList();
                }

                if (_branches.Any())
                {
                    if (ViewPreference.ActiveBranchId.HasValue && _branches.Any(b => b.Id == ViewPreference.ActiveBranchId.Value))
                    {
                        _selectedBranchId = ViewPreference.ActiveBranchId.Value.ToString();
                    }
                    else if (UserContext.BranchId.HasValue && _branches.Any(b => b.Id == UserContext.BranchId.Value))
                    {
                        _selectedBranchId = UserContext.BranchId.Value.ToString();
                        var match = _branches.First(b => b.Id == UserContext.BranchId.Value);
                        ViewPreference.SetActiveBranch(match.Id, match.Name);
                    }
                    else if (string.IsNullOrEmpty(_selectedBranchId) || !_branches.Any(b => b.Id.ToString() == _selectedBranchId))
                    {
                        _selectedBranchId = _branches.First().Id.ToString();
                        ViewPreference.SetActiveBranch(_branches.First().Id, _branches.First().Name);
                    }
                }
                else
                {
                    _selectedBranchId = "";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading branch admin branches: {ex.Message}");
        }
    }

    private async Task RefreshBoardAsync()
    {
        _loading = true;
        try
        {
            Guid? branchId = Guid.TryParse(_selectedBranchId, out var bId) ? bId : null;
            var filter = new OperationsBoardFilter
            {
                OrganizationId = UserContext.OrganizationId,
                BranchId = branchId,
                RoleOverride = RoleTemplates.BranchAdmin,
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

            try
            {
                _chatUnreadCount = await CommunicationRepo.GetUnreadCountForEducatorAsync(TenantContext.TenantId);
            }
            catch
            {
                _chatUnreadCount = 0;
            }

            if (branchId.HasValue && branchId.Value != Guid.Empty)
            {
                try
                {
                    _branchOpsStatus = await DailyOpsService.GetBranchDailyStatusAsync(branchId.Value, DateOnly.FromDateTime(DateTime.Today));
                }
                catch
                {
                    // non-blocking telemetry
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading branch board: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleBranchSelected(ChangeEventArgs e)
    {
        _selectedBranchId = e.Value?.ToString() ?? "";
        if (Guid.TryParse(_selectedBranchId, out var bId))
        {
            var b = _branches.FirstOrDefault(x => x.Id == bId);
            if (b != null)
            {
                ViewPreference.SetActiveBranch(b.Id, b.Name);
            }
        }
        await RefreshBoardAsync();
    }

    private async Task HandleOpenBranchDayAsync()
    {
        if (Guid.TryParse(_selectedBranchId, out var bId))
        {
            var res = await DailyOpsService.OpenBranchDayAsync(bId, DateOnly.FromDateTime(DateTime.Today));
            if (res.Success)
            {
                await NotificationService.ShowSuccessAsync(res.Message);
                await RefreshBoardAsync();
            }
            else
            {
                await NotificationService.ShowErrorAsync(res.Message);
            }
        }
    }

    private async Task HandleCloseBranchDayAsync()
    {
        if (Guid.TryParse(_selectedBranchId, out var bId))
        {
            var nowTime = TimeProvider.GetLocalNow().TimeOfDay;
            var res = await DailyOpsService.CloseBranchDayAsync(bId, DateOnly.FromDateTime(DateTime.Today), nowTime);
            if (res.Success)
            {
                await NotificationService.ShowSuccessAsync(res.Message);
                await RefreshBoardAsync();
            }
            else
            {
                await NotificationService.ShowWarningAsync(res.Message);
            }
        }
    }

    private string GetGreeting()
    {
        int hour = TimeProvider.GetLocalNow().Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    private string GetBranchAdminDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(UserContext.Email))
        {
            var name = UserContext.Email.Split('@')[0];
            return char.ToUpper(name[0]) + name.Substring(1);
        }
        return "Branch Director";
    }

    private string GetViewDisplayName(string roleKey) => roleKey switch
    {
        "Teacher" or "ClassroomTeacher" => "Educator",
        "OrgAdmin" or "OrganizationAdmin" => "Organization Admin",
        _ => roleKey
    };

    private async Task ToggleChecklistItem(ReadinessChecklistItem item)
    {
        if (!DailyOpsService.IsBypassedForTesting() && (_branchOpsStatus == null || !_branchOpsStatus.IsOpen))
        {
            await NotificationService.ShowWarningAsync("Branch facility is closed. Readiness checklist items cannot be updated while the branch is closed. Please open the branch for operations first.");
            return;
        }

        item.IsCompleted = !item.IsCompleted;
        try
        {
            await OperationsBoardService.SetReadinessChecklistItemAsync(
                item.Id,
                item.IsCompleted,
                branchId: _activeBranch?.Id,
                classroomId: null,
                date: DateTime.Today);
            await NotificationService.ShowSuccessAsync($"Updated '{item.Label}' to {(item.IsCompleted ? "Completed" : "Pending")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating readiness checklist item: {ex.Message}");
        }
    }

    private List<AttentionItem> GetAttentionItems()
    {
        var items = new List<AttentionItem>();

        if (_board.ExpectedNotArrivedCount > 0)
        {
            items.Add(new AttentionItem
            {
                Icon = "📋",
                Title = "Attendance Incomplete",
                Description = $"{_board.ExpectedNotArrivedCount} expected children have not yet arrived or been marked.",
                ActionText = "Confirm Attendance",
                ActionUrl = "/attendance",
                SeverityClass = "attention-amber"
            });
        }

        if (_chatUnreadCount > 0)
        {
            items.Add(new AttentionItem
            {
                Icon = "💬",
                Title = "Unread Parent Messages",
                Description = $"You have {_chatUnreadCount} unread parent inquiry awaiting response.",
                ActionText = "View Messages",
                ActionUrl = "/classroom-messages",
                SeverityClass = "attention-purple"
            });
        }

        if (_board.TeacherChildrenCareStatus != null)
        {
            int pendingCare = _board.TeacherChildrenCareStatus.Count(c => c.NeedsAttention);
            if (pendingCare > 0)
            {
                items.Add(new AttentionItem
                {
                    Icon = "🍼",
                    Title = "Routine Care Logs Needed",
                    Description = $"{pendingCare} child(ren) currently in the room have no meal/nap logs entered today.",
                    ActionText = "Log Daily Care",
                    ActionUrl = "/daily-care",
                    SeverityClass = "attention-rose"
                });
            }
        }

        if (ActiveRunningExperience != null)
        {
            items.Add(new AttentionItem
            {
                Icon = "⚡",
                Title = "Live Experience In Progress",
                Description = $"'{ActiveRunningExperience.Title}' is currently running. Close when concluded.",
                ActionText = "Resume Session",
                ActionUrl = "/today",
                SeverityClass = "attention-emerald"
            });
        }

        return items;
    }

    private class AttentionItem
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ActionText { get; set; } = "";
        public string ActionUrl { get; set; } = "";
        public string SeverityClass { get; set; } = "";
    }

    private async Task StartSession(Guid scheduleId)
    {
        if (!DailyOpsService.IsBypassedForTesting() && (_branchOpsStatus == null || !_branchOpsStatus.IsOpen))
        {
            await NotificationService.ShowWarningAsync("Cannot start activity: Branch facility is currently closed for daily operations. Please open the branch first.");
            return;
        }

        if (ExecutionService.IsDaycareClosedForActivity(DateOnly.FromDateTime(DateTime.Today), out var closedReason))
        {
            await NotificationService.ShowWarningAsync(closedReason);
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
        if (!DailyOpsService.IsBypassedForTesting() && (_branchOpsStatus == null || !_branchOpsStatus.IsOpen))
        {
            await NotificationService.ShowWarningAsync("Cannot close activity: Branch facility is currently closed for daily operations.");
            return;
        }

        if (ExecutionService.IsDaycareClosedForActivity(DateOnly.FromDateTime(DateTime.Today), out var closedReason))
        {
            await NotificationService.ShowWarningAsync(closedReason);
            return;
        }
        try
        {
            var teacherId = UserContext.UserId != Guid.Empty ? UserContext.UserId : Guid.Parse("50000000-0000-0000-0000-000000000001");
            await ExecutionService.CompleteExperienceAsync(scheduleId, teacherId, 5, "Completed", "Closed from Branch Operations");
            await RefreshBoardAsync();
            await NotificationService.ShowSuccessAsync("Experience session completed and closed.");
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

    private async Task StartCelebration(Guid eventId)
    {
        if (ExecutionService.IsDaycareClosedForActivity(DateOnly.FromDateTime(DateTime.Today), out var closedReason))
        {
            await NotificationService.ShowWarningAsync(closedReason);
            return;
        }
        try
        {
            await EventService.UpdateStatusAsync(eventId, "InProgress");
            await RefreshBoardAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting celebration: {ex.Message}");
        }
    }

    private async Task FinishCelebration(Guid eventId)
    {
        if (ExecutionService.IsDaycareClosedForActivity(DateOnly.FromDateTime(DateTime.Today), out var closedReason))
        {
            await NotificationService.ShowWarningAsync(closedReason);
            return;
        }
        try
        {
            await EventService.UpdateStatusAsync(eventId, "Completed");
            await RefreshBoardAsync();
            await OpenEventPhotoModal(eventId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finishing celebration: {ex.Message}");
        }
    }

    private async Task OpenEventPhotoModal(Guid eventId)
    {
        var ev = await EventService.GetEventByIdAsync(eventId);
        if (ev != null)
        {
            _photoModalEventId = ev.Id;
            _photoModalEventTitle = ev.Title;
            _photoModalEventPhotos = ev.PhotoUrls ?? Array.Empty<string>();
            _isPhotoModalOpen = true;
            StateHasChanged();
        }
    }

    private async Task OnEventPhotosSaved(string[] newPhotos)
    {
        _photoModalEventPhotos = newPhotos;
        await EventService.UpdateEventPhotosAsync(_photoModalEventId, newPhotos);
        await RefreshBoardAsync();
    }

    private async Task HandleEndOfDayCompleted(EndOfDayProcessReport report)
    {
        await RefreshBoardAsync();
        await NotificationService.ShowSuccessAsync($"Branch day closed successfully. Autoclosed {report.TeachersCheckedOutCount} educators and {report.ChildrenCheckedOutCount} children.");
    }
}
