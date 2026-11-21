using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.UI.Shared.Components.ChildJourney;

namespace EnjoyEveryday.UI.Shared.Components.ActiveSessionModal;

public partial class ActiveSessionModal
{
    [Parameter]
    public bool IsOpen { get; set; }
    
    [Parameter]
    public Guid ScheduleId { get; set; }
    
    [Parameter]
    public Guid TeacherId { get; set; }
    
    [Parameter]
    public EventCallback OnSessionEnded { get; set; }
    
    [Parameter]
    public EventCallback OnClose { get; set; }

    private ExperienceSession? _session;
    private string _experienceTitle = "";
    private string _classroomName = "";
    private string _branchName = "";
    private List<Child> _children = new();
    private Dictionary<Guid, ExperienceParticipation> _participations = new();
    private HashSet<Guid> _presentChildIds = new();
    private HashSet<Guid> _loggedChildIds = new();
    
    private int _stars = 5;
    private string _rating = "Great";
    private string _notes = "";

    private int _currentModalStep = 1;
    private bool _isSavingJourneyMoments = false;
    private List<ChildJourneyDraftItem> _journeyDrafts = new();

    private string _activePivotSituation = string.Empty;
    private bool _isLoadingPivot = false;
    private List<InSessionAdaptationResult> _activePivots = new();

    private async Task RequestPivotAsync(string situation)
    {
        _activePivotSituation = situation;
        _isLoadingPivot = true;
        _activePivots.Clear();
        StateHasChanged();

        try
        {
            var pivots = await AiIdeaService.GenerateInSessionAdaptationsAsync(_experienceTitle, situation);
            _activePivots = pivots.ToList();
        }
        catch
        {
            // Fallback handled inside service
        }
        finally
        {
            _isLoadingPivot = false;
            StateHasChanged();
        }
    }

    private class ChildJourneyDraftItem
    {
        public Guid ChildId { get; set; }
        public string ChildName { get; set; } = "";
        public string ParticipationStatus { get; set; } = "Participated";
        public Guid ParticipationId { get; set; }
        public bool Selected { get; set; } = true;
        public string Title { get; set; } = "";
        public string Observation { get; set; } = "";
        public string? ChildVoice { get; set; }
        public string? TeacherReflection { get; set; }
        public bool AlreadyHasMoment { get; set; }
    }
    
    private string GetStarStyle(int starValue)
    {
        var color = _stars >= starValue ? "#f5a623" : "#e4e7df";
        return $"cursor: pointer; font-size: 24px; color: {color};";
    }

    private HashSet<Guid> _savedParticipationIds = new();

    private Guid? GetParticipationId(Guid? childId)
    {
        if (childId.HasValue && _participations.TryGetValue(childId.Value, out var p))
        {
            if (_savedParticipationIds.Contains(p.Id))
            {
                return p.Id;
            }
        }
        return null;
    }
    
    private AddJourneyMomentDialog _momentDialog = default!;
    private Guid? _selectedChildId;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && ScheduleId != Guid.Empty && (_session == null || _session.ExperienceScheduleId != ScheduleId))
        {
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        _currentModalStep = 1;
        _journeyDrafts.Clear();
        _session = await SessionRepository.GetSessionByScheduleIdAsync(ScheduleId);
        
        if (_session != null)
        {
            var expList = await ExperienceService.GetExperiencesAsync();
            var exp = expList.FirstOrDefault(e => e.Id == _session.ExperienceId);
            _experienceTitle = exp?.Title ?? "Unknown";

            var classroom = await ClassroomService.GetClassroomByIdAsync(_session.ClassroomId);
            _classroomName = classroom?.Name ?? "Unknown Classroom";
            
            if (classroom != null)
            {
                var branch = await BranchService.GetBranchByIdAsync(classroom.BranchId);
                _branchName = branch?.Name ?? "Unknown Branch";
            }
            
            try
            {
                _children = (await ChildService.GetChildrenByClassroomAsync(_session.ClassroomId)).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                // If caller is in cross-tenant preview or role override, retrieve visible children without throwing unhandled UI crash
                var allChildren = await ChildService.GetChildrenAsync();
                _children = allChildren.Where(c => c.ClassroomId == _session.ClassroomId).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving children for session modal: {ex.Message}");
                _children = new List<Child>();
            }
            
            _presentChildIds.Clear();
            _loggedChildIds.Clear();
            
            _participations.Clear();
            foreach (var child in _children)
            {
                _participations[child.Id] = new ExperienceParticipation
                {
                    Id = Guid.NewGuid(),
                    ExperienceSessionId = _session.Id,
                    ChildId = child.Id,
                    ParticipationStatus = "Participated"
                };
            }
            
            var todayDate = TimeProvider.GetLocalNow().Date;
            var presentIds = await AttendanceService.GetPresentChildIdsForClassroomAsync(_session.ClassroomId, todayDate);
            foreach (var id in presentIds) { _presentChildIds.Add(id); }

            var loggedIds = await ExecutionService.GetLoggedChildIdsForSessionAsync(_session.Id);
            foreach (var id in loggedIds) { _loggedChildIds.Add(id); }

            foreach (var child in _children)
            {
                if (!_presentChildIds.Contains(child.Id))
                {
                    _participations[child.Id].ParticipationStatus = "Absent";
                }
            }

            _savedParticipationIds.Clear();
            var existing = await SessionRepository.GetParticipationsForSessionAsync(_session.Id);
            foreach (var part in existing)
            {
                if (_participations.ContainsKey(part.ChildId))
                {
                    _participations[part.ChildId].Id = part.Id;
                    _participations[part.ChildId].ParticipationStatus = part.ParticipationStatus;
                    _savedParticipationIds.Add(part.Id);
                }
            }
        }
        
        StateHasChanged();
    }

    private string GetParticipationStatus(Guid childId)
    {
        if (_participations.TryGetValue(childId, out var p)) return p.ParticipationStatus;
        return "Participated";
    }

    private void UpdateStatus(Guid childId, string status)
    {
        if (!_presentChildIds.Contains(childId) && status == "Participated")
        {
            // Absent or not checked in children cannot participate
            return;
        }

        if (_participations.TryGetValue(childId, out var p))
        {
            p.ParticipationStatus = status;
        }
    }

    private async Task OpenMomentDialog(Guid? childId = null)
    {
        if (childId.HasValue && !_presentChildIds.Contains(childId.Value))
        {
            // Cannot log observation for absent or non-checked-in child
            return;
        }
        _selectedChildId = childId;
        await _momentDialog.OpenAsync();
    }

    private bool _showParticipationWarning = false;
    private string _sessionClosedError = "";
    private bool IsDaycareClosedForModal => ExecutionService.IsDaycareClosedForActivity(null, out _);

    private async Task TryEndSession()
    {
        _sessionClosedError = "";
        if (ExecutionService.IsDaycareClosedForActivity(null, out var closedReason))
        {
            _sessionClosedError = closedReason;
            return;
        }
        bool hasParticipants = _participations.Values.Any(p => p.ParticipationStatus == "Participated");
        if (!hasParticipants && !_showParticipationWarning && _children.Any())
        {
            _showParticipationWarning = true;
            return;
        }

        await EndSession();
    }

    private async Task EndSession()
    {
        if (_session != null)
        {
            try
            {
                await ExecutionService.CompleteExperienceWithParticipationsAsync(ScheduleId, TeacherId, _stars, _rating, _notes, _participations.Values);
            }
            catch (InvalidOperationException ex)
            {
                _sessionClosedError = ex.Message;
                return;
            }
            catch (Exception)
            {
                // Already completed or closed by another teacher/action
            }

            // Prepare Journey moments review step for participating children
            var participatingChildren = _children
                .Where(c => _participations.TryGetValue(c.Id, out var p) && p.ParticipationStatus != "Absent" && _presentChildIds.Contains(c.Id))
                .ToList();

            if (participatingChildren.Any())
            {
                _journeyDrafts.Clear();
                foreach (var child in participatingChildren)
                {
                    var p = _participations[child.Id];
                    bool hasExisting = p.Id != Guid.Empty && await JourneyService.HasJourneyEntryForParticipationAsync(p.Id);

                    string baseObs = !string.IsNullOrWhiteSpace(p.ParticipationNotes)
                        ? p.ParticipationNotes
                        : (!string.IsNullOrWhiteSpace(_notes)
                            ? $"{child.FirstName} actively engaged in '{_experienceTitle}'. {_notes}"
                            : $"{child.FirstName} engaged enthusiastically in today's '{_experienceTitle}' experience ({p.ParticipationStatus}).");

                    _journeyDrafts.Add(new ChildJourneyDraftItem
                    {
                        ChildId = child.Id,
                        ChildName = $"{child.FirstName} {child.LastName}".Trim(),
                        ParticipationStatus = p.ParticipationStatus,
                        ParticipationId = p.Id,
                        Selected = !hasExisting,
                        Title = _experienceTitle,
                        Observation = baseObs,
                        ChildVoice = "",
                        TeacherReflection = _rating switch
                        {
                            "Great" => "Child thoroughly enjoyed the activity and engaged well with peers.",
                            "Good" => "Child followed along and met session expectations.",
                            _ => ""
                        },
                        AlreadyHasMoment = hasExisting
                    });
                }

                _currentModalStep = 2;
                StateHasChanged();
                return;
            }

            if (OnSessionEnded.HasDelegate)
            {
                await OnSessionEnded.InvokeAsync();
            }

            CloseDialog();
        }
    }

    private void ToggleSelectAllJourneys()
    {
        bool allSelected = _journeyDrafts.Where(d => !d.AlreadyHasMoment).All(d => d.Selected);
        foreach (var draft in _journeyDrafts.Where(d => !d.AlreadyHasMoment))
        {
            draft.Selected = !allSelected;
        }
    }

    private async Task SkipJourneyAndFinish()
    {
        _currentModalStep = 1;
        if (OnSessionEnded.HasDelegate)
        {
            await OnSessionEnded.InvokeAsync();
        }
        CloseDialog();
    }

    private async Task SaveJourneyMomentsAndFinish()
    {
        if (_session == null) return;
        _isSavingJourneyMoments = true;
        try
        {
            var selectedDrafts = _journeyDrafts.Where(d => d.Selected && !d.AlreadyHasMoment).ToList();
            foreach (var draft in selectedDrafts)
            {
                var entry = new ChildJourneyEntry
                {
                    Id = Guid.NewGuid(),
                    ChildId = draft.ChildId,
                    ClassroomId = _session.ClassroomId,
                    TeacherId = TeacherId,
                    ExperienceId = _session.ExperienceId,
                    ExperienceScheduleId = _session.ExperienceScheduleId,
                    ExperienceSessionId = _session.Id,
                    ExperienceParticipationId = draft.ParticipationId != Guid.Empty ? draft.ParticipationId : (Guid?)null,
                    JourneyType = "ActivityObservation",
                    Title = !string.IsNullOrWhiteSpace(draft.Title) ? draft.Title : _experienceTitle,
                    Observation = !string.IsNullOrWhiteSpace(draft.Observation) ? draft.Observation : $"Participated in {_experienceTitle}.",
                    ChildVoice = draft.ChildVoice,
                    TeacherReflection = draft.TeacherReflection,
                    ObservationDate = DateTimeOffset.UtcNow,
                    ParticipationType = draft.ParticipationStatus,
                    IsParentVisible = false,
                    ParentVisibilityStatus = "PendingReview",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await JourneyService.CreateJourneyEntryAsync(entry, Enumerable.Empty<Guid>());
            }

            if (OnSessionEnded.HasDelegate)
            {
                await OnSessionEnded.InvokeAsync();
            }

            CloseDialog();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving journey moments from session: {ex.Message}");
            CloseDialog();
        }
        finally
        {
            _isSavingJourneyMoments = false;
            _currentModalStep = 1;
        }
    }

    private void CloseDialog()
    {
        if (OnClose.HasDelegate)
        {
            OnClose.InvokeAsync();
        }
    }
}
