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
using EnjoyEveryday.UI.Shared.Components.OperationsBoard;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace EnjoyEveryday.Daycare.Web.Components.Pages;

public partial class OrgAdminDashboard
{
    [Parameter] public IReadOnlyList<string> AvailableViews { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback<string> OnChangeView { get; set; }

    private OperationsBoardModel _board = new();
    private List<Branch> _branches = new();
    private string _selectedBranchId = "";
    private string? _previewPhoto;
    private int _chatUnreadCount = 0;
    private string? _adminDisplayName;

    // Modals
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
        if (!await PageAuth.EnsureAuthorizedAsync("/management/dashboard"))
        {
            return;
        }

        try
        {
            var profile = await PageAuth.GetUserProfileAsync();
            if (!string.IsNullOrWhiteSpace(profile?.DisplayName))
            {
                _adminDisplayName = profile.DisplayName;
            }
        }
        catch { }

        await LoadBranchesAsync();
        await RefreshBoardAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                _branches = (await BranchService.GetBranchesAsync(orgId)).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading branches: {ex.Message}");
        }
    }

    private async Task RefreshBoardAsync()
    {
        try
        {
            Guid? branchId = Guid.TryParse(_selectedBranchId, out var bId) ? bId : null;
            var filter = new OperationsBoardFilter
            {
                OrganizationId = UserContext.OrganizationId,
                BranchId = branchId,
                RoleOverride = RoleTemplates.OrganizationAdmin,
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading org workspace: {ex.Message}");
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleBranchChanged(ChangeEventArgs e)
    {
        _selectedBranchId = e.Value?.ToString() ?? "";
        await RefreshBoardAsync();
    }

    private string GetGreeting()
    {
        int hour = TimeProvider.GetLocalNow().Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    private string GetAdminDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(_adminDisplayName))
        {
            return _adminDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(UserContext.Email))
        {
            var raw = UserContext.Email.Split('@')[0];
            // Clean up dot notation like "sandhya.vijayabasker.1" -> "Sandhya"
            var parts = raw.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var firstWord = parts.FirstOrDefault(p => !int.TryParse(p, out _));
            if (!string.IsNullOrEmpty(firstWord))
            {
                return char.ToUpper(firstWord[0]) + firstWord.Substring(1);
            }
        }
        return "Administrator";
    }

    private string GetViewDisplayName(string roleKey) => roleKey switch
    {
        "Teacher" or "ClassroomTeacher" => "Educator",
        "BranchAdmin" => "Branch Director",
        _ => roleKey
    };

    private async Task ToggleChecklistItem(ReadinessChecklistItem item)
    {
        item.IsCompleted = !item.IsCompleted;
        try
        {
            Guid? bId = Guid.TryParse(_selectedBranchId, out var parsedBId) ? parsedBId : null;
            await OperationsBoardService.SetReadinessChecklistItemAsync(
                item.Id,
                item.IsCompleted,
                branchId: bId,
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
        if (ExecutionService.IsDaycareClosedForActivity(DateOnly.FromDateTime(DateTime.Today), out var closedReason))
        {
            await NotificationService.ShowWarningAsync(closedReason);
            return;
        }
        try
        {
            var teacherId = UserContext.UserId != Guid.Empty ? UserContext.UserId : Guid.Parse("50000000-0000-0000-0000-000000000001");
            await ExecutionService.CompleteExperienceAsync(scheduleId, teacherId, 5, "Completed", "Closed from Management Workspace");
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
        await NotificationService.ShowSuccessAsync($"Day closed successfully. Autoclosed {report.TeachersCheckedOutCount} educators and {report.ChildrenCheckedOutCount} children.");
    }
}
