using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.UI.Shared.Components.ChildJourney;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.UI.Shared.Views.Children;

public partial class ChildJourneyView : ComponentBase
{
    [Inject] private IChildJourneyService JourneyService { get; set; } = default!;
    [Inject] private IChildDailyCareService DailyCareService { get; set; } = default!;
    [Inject] private ExperienceService ExperienceService { get; set; } = default!;
    [Inject] private BranchService BranchService { get; set; } = default!;
    [Inject] private ClassroomService ClassroomService { get; set; } = default!;
    [Inject] private ChildService ChildService { get; set; } = default!;
    [Inject] private OrganizationService OrgService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IChildDevelopmentPortfolioService PortfolioService { get; set; } = default!;
    [Inject] private IUserContext UserContext { get; set; } = default!;

    [Parameter] public Guid? InitialChildId { get; set; }

    private bool _isCommAssistantOpen = false;
    private bool _isConferenceModalOpen = false;
    private AddJourneyMomentDialog _addMomentDialog = default!;
    
    private ChildPortfolioSummary? _portfolio;
    private string _selectedDomainFilter = "all";
    private string _selectedVisibilityFilter = "all";

    private Guid _childId => _selectedChildId != Guid.Empty ? _selectedChildId : (InitialChildId ?? Guid.Empty);
    private Child? _selectedChild;
    private List<ChildJourneyEntry>? _allEntries;
    private List<ChildCareEvent> _allCareEvents = new();
    private List<ChildCareEvent> _careEvents = new();
    private List<JourneyItemViewModel> _displayTimeline = new();
    private Dictionary<Guid, List<DevelopmentArea>> _entryAreas = new();
    private Dictionary<Guid, List<ChildJourneyEvidence>> _entryEvidences = new();
    private Dictionary<Guid, string> _experienceTitles = new();
    
    private Dictionary<Guid, Branch> _branches = new();
    private Dictionary<Guid, Classroom> _classrooms = new();
    private List<Child> _children = new();
    
    private Guid _selectedBranchId = Guid.Empty;
    private Guid SelectedBranchId
    {
        get => _selectedBranchId;
        set
        {
            if (_selectedBranchId != value)
            {
                _selectedBranchId = value;
                SelectedClassroomId = Guid.Empty;
            }
        }
    }

    private Guid _selectedClassroomId = Guid.Empty;
    private Guid SelectedClassroomId
    {
        get => _selectedClassroomId;
        set
        {
            if (_selectedClassroomId != value)
            {
                _selectedClassroomId = value;
                SelectedChildId = Guid.Empty;
            }
        }
    }

    private Guid _selectedChildId = Guid.Empty;
    private Guid SelectedChildId
    {
        get => _selectedChildId;
        set
        {
            if (_selectedChildId != value)
            {
                _selectedChildId = value;
                _selectedChild = _children.FirstOrDefault(c => c.Id == _selectedChildId);
                _ = LoadJourneyDataAsync();
            }
        }
    }
    
    private string _currentFilter = "all";
    private bool _isModalOpen = false;
    private string _modalTitle = "";
    private string _modalText = "";
    private DateTime? _selectedDate = DateTime.Today;

    private int _totalMoments = 0;
    private string _currentFocus = "Exploration";
    private int _recentActivity = 0;
    private int _uniqueAreas = 0;
    private List<(string Name, int Score, string Note)> _growingInterests = new();
    private List<(string Title, string Description)> _recentAccomplishments = new();
    private string? _notableMemory = null;

    protected override async Task OnInitializedAsync()
    {
        var orgs = await OrgService.GetOrganizationsAsync();
        var defaultOrg = (UserContext.OrganizationId.HasValue
            ? orgs.FirstOrDefault(o => o.Id == UserContext.OrganizationId.Value)
            : null) ?? orgs.FirstOrDefault();

        if (defaultOrg != null)
        {
            var branchesList = await BranchService.GetBranchesAsync(defaultOrg.Id);
            _branches = branchesList.ToDictionary(b => b.Id);
        }

        var classList = await ClassroomService.GetAllClassroomsAsync();
        _classrooms = classList.ToDictionary(c => c.Id);

        var childrenList = await ChildService.GetChildrenAsync();
        _children = childrenList.ToList();

        if (InitialChildId.HasValue && InitialChildId.Value != Guid.Empty)
        {
            // Pre-select filters based on passed InitialChildId
            var child = _children.FirstOrDefault(c => c.Id == InitialChildId.Value);
            if (child != null && child.ClassroomId.HasValue)
            {
                if (_classrooms.TryGetValue(child.ClassroomId.Value, out var cls))
                {
                    _selectedBranchId = cls.BranchId;
                    _selectedClassroomId = cls.Id;
                }
            }
            SelectedChildId = InitialChildId.Value; // This triggers LoadJourneyDataAsync
        }
        else if (_children.Count > 0)
        {
            var first = _children[0];
            if (first.ClassroomId.HasValue && _classrooms.TryGetValue(first.ClassroomId.Value, out var cls))
            {
                _selectedBranchId = cls.BranchId;
                _selectedClassroomId = cls.Id;
            }
            SelectedChildId = first.Id;
        }
    }

    private async Task LoadJourneyDataAsync()
    {
        if (_childId == Guid.Empty) return;
        try
        {
            _portfolio = await PortfolioService.GetChildPortfolioAsync(_childId);
        }
        catch
        {
            _portfolio = null;
        }

        var entries = await JourneyService.GetChildJourneyAsync(_childId);
        _allEntries = entries.ToList();

        try
        {
            var careList = await DailyCareService.GetChildCareEventsAsync(_childId);
            _allCareEvents = careList?.ToList() ?? new List<ChildCareEvent>();
        }
        catch
        {
            _allCareEvents = new List<ChildCareEvent>();
        }
        _careEvents = _allCareEvents;
        
        var allAreas = await JourneyService.GetDevelopmentAreasAsync();
        var areaDict = allAreas.ToDictionary(a => a.Id);
        
        _entryAreas.Clear();
        _entryEvidences.Clear();
        foreach (var entry in _allEntries)
        {
            var areaIds = await JourneyService.GetDevelopmentAreasForEntryAsync(entry.Id);
            var areas = areaIds.Where(areaDict.ContainsKey).Select(id => areaDict[id]).ToList();
            _entryAreas[entry.Id] = areas;

            var evidences = await JourneyService.GetEvidenceForJourneyEntryAsync(entry.Id);
            _entryEvidences[entry.Id] = evidences.ToList();
        }
        
        var allExperiences = await ExperienceService.GetExperiencesAsync();
        _experienceTitles = allExperiences.ToDictionary(e => e.Id, e => e.Title);
        
        CalculateDynamicCards();
        ApplyFilter();
    }
    
    private string GetAgeString(DateTime? dob)
    {
        if (!dob.HasValue) return "Unknown Age";
        var today = DateTime.Today;
        var age = today.Year - dob.Value.Year;
        var months = today.Month - dob.Value.Month;
        if (today.Day < dob.Value.Day) months--;
        if (months < 0)
        {
            age--;
            months += 12;
        }
        return $"Age {age} Years, {months} Months";
    }

    private void CalculateDynamicCards()
    {
        int obsCount = _allEntries?.Count ?? 0;
        int careCount = _allCareEvents?.Count ?? 0;
        _totalMoments = obsCount + careCount;

        var now = DateTime.UtcNow;
        int obsRecent = _allEntries?.Count(e => e.ObservationDate >= now.AddDays(-7)) ?? 0;
        int careRecent = _allCareEvents?.Count(c => c.EventTime >= now.AddDays(-7)) ?? 0;
        _recentActivity = obsRecent + careRecent;

        if (_allEntries == null || !_allEntries.Any())
        {
            _currentFocus = "Daily Care & Wellbeing";
            _uniqueAreas = 0;
            _growingInterests.Clear();
            _recentAccomplishments.Clear();
            if (_allCareEvents != null && _allCareEvents.Any())
            {
                var latestCare = _allCareEvents.OrderByDescending(c => c.EventTime).First();
                _notableMemory = $"{latestCare.EventType} recorded: {latestCare.Notes ?? "Routine care on schedule."}";
            }
            else
            {
                _notableMemory = null;
            }
            return;
        }
        
        var allTags = _entryAreas.SelectMany(x => x.Value).Select(a => a.Name).ToList();
        _uniqueAreas = allTags.Distinct().Count();

        if (allTags.Any())
        {
            var topTags = allTags.GroupBy(t => t).OrderByDescending(g => g.Count()).ToList();
            _currentFocus = topTags.First().Key;

            _growingInterests.Clear();
            int totalTagCount = allTags.Count;
            foreach (var tagGroup in topTags.Take(4))
            {
                int score = (int)Math.Clamp(Math.Round((double)tagGroup.Count() / totalTagCount * 100), 10, 100);
                string note = score >= 40 
                    ? "Primary learning focus with strong engagement." 
                    : (score >= 25 ? "Consistently explores during routine activities." : "Emerging interest in small-group activities.");
                _growingInterests.Add((tagGroup.Key, score, note));
            }
        }

        _recentAccomplishments.Clear();
        var recentEntries = _allEntries.OrderByDescending(e => e.ObservationDate).Take(3).ToList();
        foreach (var entry in recentEntries)
        {
            // Derive clean meaningful title from linked experience, tag area, or milestone
            string title;
            if (entry.ExperienceId.HasValue && _experienceTitles.TryGetValue(entry.ExperienceId.Value, out var expName) && !string.IsNullOrWhiteSpace(expName))
            {
                title = expName;
            }
            else if (_entryAreas.TryGetValue(entry.Id, out var areas) && areas.Any())
            {
                title = areas.First().Name;
            }
            else if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                title = entry.Title;
            }
            else
            {
                title = "Developmental Milestone";
            }

            // Clean up description: remove loose emoji prefixes and provide clear text
            string rawDesc = entry.Observation ?? "Active engagement in classroom learning.";
            string cleanDesc = rawDesc
                .Replace("🌱", "")
                .Replace("❤️", "")
                .Replace("🤝", "")
                .Replace("tes ", "")
                .Replace("test test", "")
                .Trim();
            
            if (string.IsNullOrWhiteSpace(cleanDesc))
            {
                cleanDesc = "Demonstrated growth and positive curiosity.";
            }
            else if (cleanDesc.Length > 55)
            {
                cleanDesc = cleanDesc.Substring(0, 52) + "...";
            }

            _recentAccomplishments.Add((title, cleanDesc));
        }

        var voiceEntry = _allEntries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.ChildVoice));
        if (voiceEntry != null)
        {
            _notableMemory = $"\"{voiceEntry.ChildVoice}\" - {voiceEntry.ObservationDate:MMM dd}";
        }
        else
        {
            var first = _allEntries.LastOrDefault(); // Oldest entry
            if (first != null && !string.IsNullOrWhiteSpace(first.Observation))
            {
                _notableMemory = first.Observation.Length > 60 ? first.Observation.Substring(0, 57) + "..." : first.Observation;
            }
        }
    }

    private List<string> GetTagsForEntry(Guid entryId)
    {
        if (_entryAreas.TryGetValue(entryId, out var areas))
        {
            return areas.Select(a => a.Name).ToList();
        }
        return new List<string>();
    }

    private List<string> GetPhotosForEntry(Guid entryId)
    {
        if (_entryEvidences.TryGetValue(entryId, out var evidences))
        {
            return evidences.Where(e => e.EvidenceType == "Photo" && !string.IsNullOrEmpty(e.TextContent))
                            .Select(e => e.TextContent!)
                            .ToList();
        }
        return new List<string>();
    }

    private void SetDomainFilter(string domain)
    {
        _selectedDomainFilter = domain;
        ApplyFilter();
    }

    private void SetFilter(string filter)
    {
        _currentFilter = filter;
        ApplyFilter();
    }
    
    private void ClearDateFilter()
    {
        _selectedDate = null;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var items = new List<JourneyItemViewModel>();
        var filteredEntries = (_allEntries ?? Enumerable.Empty<ChildJourneyEntry>()).AsEnumerable();
        var filteredCare = (_allCareEvents ?? Enumerable.Empty<ChildCareEvent>()).AsEnumerable();

        if (_selectedDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.ObservationDate.ToLocalTime().Date == _selectedDate.Value.Date);
            filteredCare = filteredCare.Where(c => c.EventTime.ToLocalTime().Date == _selectedDate.Value.Date);
        }

        // Apply visibility filter
        if (_selectedVisibilityFilter == "parent")
        {
            filteredEntries = filteredEntries.Where(e => e.IsParentVisible);
        }
        else if (_selectedVisibilityFilter == "internal")
        {
            filteredEntries = filteredEntries.Where(e => !e.IsParentVisible);
            filteredCare = Enumerable.Empty<ChildCareEvent>();
        }

        // Apply domain filter
        if (_selectedDomainFilter == "all")
        {
            items.AddRange(filteredEntries.Select(MapEntryToTimelineItem));
            if (_selectedVisibilityFilter != "internal")
            {
                items.AddRange(filteredCare.Select(MapCareEventToTimelineItem));
            }
        }
        else if (_selectedDomainFilter == "DailyCare")
        {
            if (_selectedVisibilityFilter != "internal")
            {
                items.AddRange(filteredCare.Select(MapCareEventToTimelineItem));
            }
        }
        else
        {
            items.AddRange(filteredEntries
                .Where(e => GetTagsForEntry(e.Id).Any(t => t.Contains(_selectedDomainFilter, StringComparison.OrdinalIgnoreCase)
                                                       || _selectedDomainFilter.Contains(t, StringComparison.OrdinalIgnoreCase)))
                .Select(MapEntryToTimelineItem));
        }

        _displayTimeline = items.OrderByDescending(i => i.Timestamp).ToList();
        _careEvents = filteredCare.ToList();
    }

    private JourneyItemViewModel MapCareEventToTimelineItem(ChildCareEvent ev)
    {
        string badgeType = ev.EventType switch
        {
            CareEventTypes.DiaperChange => "Diaper",
            CareEventTypes.Meal => "Meal",
            CareEventTypes.SleepRest => "Sleep",
            CareEventTypes.Toileting => "Toileting",
            CareEventTypes.Hygiene => "Hygiene",
            CareEventTypes.Comfort => "Comfort",
            _ => "Care"
        };

        string title = ev.EventType switch
        {
            CareEventTypes.DiaperChange => $"Diaper Change — {ev.DiaperCondition ?? "Checked"}",
            CareEventTypes.Meal => $"Meal: {ev.MealType ?? "Nutrition"} ({ev.AmountConsumed ?? "Recorded"})",
            CareEventTypes.SleepRest => $"Sleep & Rest — {ev.RestStatus ?? "Rested"}",
            CareEventTypes.Toileting => $"Toileting — {ev.ToiletingActivity ?? "Routine"}",
            CareEventTypes.Hygiene => $"Hygiene — {ev.HygieneType ?? "Routine"}",
            CareEventTypes.Comfort => $"Comfort & Wellbeing — {ev.ComfortMood ?? "Observed"}",
            _ => $"Daily Care — {ev.EventType}"
        };

        var tags = new List<string> { ev.EventType };
        if (!string.IsNullOrWhiteSpace(ev.RecordedByName))
        {
            tags.Add($"Teacher: {ev.RecordedByName}");
        }

        var descParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ev.Notes))
        {
            descParts.Add(ev.Notes);
        }

        if (ev.EventType == CareEventTypes.Meal)
        {
            if (!string.IsNullOrWhiteSpace(ev.FoodOffered))
                descParts.Add($"Offered: {ev.FoodOffered}");
            if (ev.FluidOz.HasValue)
                descParts.Add($"Fluids: {ev.FluidOz.Value:0.#} oz");
            if (!string.IsNullOrWhiteSpace(ev.AmountConsumed))
                tags.Add($"Consumed: {ev.AmountConsumed}");
        }
        else if (ev.EventType == CareEventTypes.DiaperChange)
        {
            if (!string.IsNullOrWhiteSpace(ev.SkinObservation))
                descParts.Add($"Skin check: {ev.SkinObservation}");
            if (!string.IsNullOrWhiteSpace(ev.DiaperCondition))
                tags.Add($"Condition: {ev.DiaperCondition}");
        }
        else if (ev.EventType == CareEventTypes.SleepRest)
        {
            if (!string.IsNullOrWhiteSpace(ev.RestStatus))
                tags.Add($"Status: {ev.RestStatus}");
        }
        else if (ev.EventType == CareEventTypes.Toileting)
        {
            if (!string.IsNullOrWhiteSpace(ev.ToiletingActivity))
                tags.Add($"Activity: {ev.ToiletingActivity}");
        }
        else if (ev.EventType == CareEventTypes.Hygiene)
        {
            if (!string.IsNullOrWhiteSpace(ev.HygieneType))
                tags.Add($"Type: {ev.HygieneType}");
        }
        else if (ev.EventType == CareEventTypes.Comfort)
        {
            if (!string.IsNullOrWhiteSpace(ev.ComfortMood))
                tags.Add($"Mood: {ev.ComfortMood}");
        }

        string desc = descParts.Any() ? string.Join(" • ", descParts) : $"Routine {ev.EventType} recorded for child.";

        return new JourneyItemViewModel
        {
            Id = ev.Id,
            Timestamp = ev.EventTime,
            Category = "DailyCare",
            BadgeType = badgeType,
            Title = title,
            Description = desc,
            Quote = "",
            IsParentVisible = true,
            IsMediaBlockedByConsent = false,
            TeacherName = ev.RecordedByName,
            Tags = tags,
            Photos = new List<string>(),
            CanDelete = false
        };
    }

    private JourneyItemViewModel MapEntryToTimelineItem(ChildJourneyEntry entry)
    {
        string title = entry.ExperienceId.HasValue 
            ? ("Experience: " + (!string.IsNullOrEmpty(entry.Title) ? entry.Title : (_experienceTitles.ContainsKey(entry.ExperienceId.Value) ? _experienceTitles[entry.ExperienceId.Value] : "Teacher Observation")))
            : (string.IsNullOrEmpty(entry.Title) ? "Teacher Observation" : entry.Title);

        var photos = GetPhotosForEntry(entry.Id);
        bool isBlocked = photos.Count > 0 && (_portfolio != null && !_portfolio.Consent.CanSharePhotosWithParents);

        return new JourneyItemViewModel
        {
            Id = entry.Id,
            Timestamp = entry.ObservationDate.UtcDateTime,
            Category = "Observation",
            BadgeType = entry.JourneyType ?? "Discovery",
            Title = title,
            Description = entry.Observation ?? "",
            Quote = entry.ChildVoice ?? "",
            TeacherReflection = entry.TeacherReflection,
            IsParentVisible = entry.IsParentVisible,
            IsMediaBlockedByConsent = isBlocked,
            Tags = GetTagsForEntry(entry.Id),
            Photos = photos,
            CanDelete = true
        };
    }

    private string GetConsentClass(MediaConsentStatus status) => status switch
    {
        MediaConsentStatus.Granted => "granted",
        MediaConsentStatus.Restricted => "restricted",
        _ => "pending"
    };

    private string GetConsentIcon(MediaConsentStatus status) => status switch
    {
        MediaConsentStatus.Granted => "bi-shield-check",
        MediaConsentStatus.Restricted => "bi-shield-slash",
        _ => "bi-shield-exclamation"
    };

    private void OpenModal(string type)
    {
        if (type == "share")
        {
            _modalTitle = $"Share {_selectedChild?.FirstName ?? "Child"}'s Journey";
            _modalText = "Create a parent-friendly version of the child's journey. Only approved memories, experiences and observations will be included.";
        }
        else if (type == "memory")
        {
            _modalTitle = "Add a Memory";
            _modalText = "Capture something that happened today that feels worth remembering. A memory can be a teacher note, a child's words, a creation or an approved photo.";
        }
        else if (type == "idea")
        {
            _modalTitle = $"{_selectedChild?.FirstName ?? "Child"}'s Possible Next Experience";
            _modalText = $"An experience based on {_currentFocus} could extend this learning experience further while creating an opportunity for collaboration. The teacher decides what happens next.";
        }
        _isModalOpen = true;
    }

    private async Task OpenAddMomentDialog()
    {
        if (_addMomentDialog != null)
        {
            await _addMomentDialog.OpenAsync();
        }
    }

    private async Task OnMomentSaved()
    {
        await LoadJourneyDataAsync();
        StateHasChanged();
    }

    private void CloseModal()
    {
        _isModalOpen = false;
    }
    
    private async Task HandleDelete(JourneyItemViewModel item)
    {
        if (item.CanDelete)
        {
            await DeleteEntry(item.Id);
        }
    }
    
    private async Task DeleteEntry(Guid entryId)
    {
        await JourneyService.DeleteJourneyEntryAsync(entryId);
        await LoadJourneyDataAsync();
    }

    public class JourneyItemViewModel
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string DateString => Timestamp.ToLocalTime().ToString("MMM dd");
        public string TimeString => Timestamp.ToLocalTime().ToString("t");
        public string Category { get; set; } = "Observation";
        public string BadgeType { get; set; } = "Discovery";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Quote { get; set; } = "";
        public string? TeacherReflection { get; set; }
        public string? TeacherName { get; set; }
        public bool? IsParentVisible { get; set; }
        public bool IsMediaBlockedByConsent { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> Photos { get; set; } = new();
        public bool CanDelete { get; set; }
    }
}
