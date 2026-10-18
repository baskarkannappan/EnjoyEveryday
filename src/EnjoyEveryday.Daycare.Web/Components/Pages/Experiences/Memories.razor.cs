using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

public partial class Memories : ComponentBase
{
    [Inject]
    private IMemoryService MemoryService { get; set; } = default!;

    [Inject]
    private ChildService ChildService { get; set; } = default!;

    [Inject]
    private ClassroomService ClassroomService { get; set; } = default!;

    [Inject]
    private BranchService BranchService { get; set; } = default!;

    [Inject]
    private IUserContext UserContext { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private List<Branch> _branches = new();
    private List<Classroom> _classrooms = new();
    private List<Child> _children = new();
    private List<MemoryEvent> _memories = new();

    private Guid _selectedBranchId = Guid.Empty;
    private Guid _selectedClassroomId = Guid.Empty;
    private Guid _selectedChildId = Guid.Empty;

    private Child? _selectedChild;
    private string _selectedClassroomName = "";
    private string _selectedCategory = "All";
    private string _searchQuery = "";
    private bool _isLoading = false;
    private string? _errorMessage;

    private MemoryEvent? _selectedMemory;

    private readonly List<string> _categories = new()
    {
        "All",
        "Learning Experiences",
        "Observations",
        "Milestones",
        "Celebrations",
        "Daily Care"
    };

    private Guid SelectedBranchId
    {
        get => _selectedBranchId;
        set
        {
            if (_selectedBranchId != value)
            {
                _selectedBranchId = value;
                _selectedClassroomId = Guid.Empty;
                _selectedChildId = Guid.Empty;
                _selectedChild = null;
                _memories.Clear();
            }
        }
    }

    private Guid SelectedClassroomId
    {
        get => _selectedClassroomId;
        set
        {
            if (_selectedClassroomId != value)
            {
                _selectedClassroomId = value;
                _selectedChildId = Guid.Empty;
                _selectedChild = null;
                _memories.Clear();
            }
        }
    }

    private Guid SelectedChildId
    {
        get => _selectedChildId;
        set
        {
            if (_selectedChildId != value)
            {
                _selectedChildId = value;
                _selectedChild = _children.FirstOrDefault(c => c.Id == value);
                _selectedClassroomName = _classrooms.FirstOrDefault(c => c.Id == _selectedChild?.ClassroomId)?.Name ?? "";
                _ = LoadMemoriesForChildAsync();
            }
        }
    }

    private IEnumerable<Classroom> _filteredClassrooms =>
        _selectedBranchId == Guid.Empty 
            ? _classrooms 
            : _classrooms.Where(c => c.BranchId == _selectedBranchId);

    private IEnumerable<Child> _filteredChildren =>
        _children.Where(c => 
            (_selectedClassroomId == Guid.Empty || c.ClassroomId == _selectedClassroomId) &&
            (_selectedBranchId == Guid.Empty || _classrooms.Any(cls => cls.Id == c.ClassroomId && cls.BranchId == _selectedBranchId)));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            if (orgId != Guid.Empty)
            {
                var branchList = await BranchService.GetBranchesAsync(orgId);
                _branches = branchList.ToList();
            }

            var classroomList = await ClassroomService.GetAllClassroomsAsync();
            _classrooms = classroomList.ToList();

            var childList = await ChildService.GetChildrenAsync();
            _children = childList.ToList();

            if (_children.Any())
            {
                SelectedChildId = _children.First().Id;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private async Task LoadMemoriesForChildAsync()
    {
        if (_selectedChildId == Guid.Empty)
        {
            _memories.Clear();
            return;
        }

        _isLoading = true;
        _errorMessage = null;

        try
        {
            var query = new MemoryQueryParameters
            {
                Category = _selectedCategory == "All" ? null : _selectedCategory,
                SearchQuery = string.IsNullOrWhiteSpace(_searchQuery) ? null : _searchQuery.Trim(),
                Limit = 100,
                Offset = 0
            };

            var results = await MemoryService.GetChildMemoriesAsync(_selectedChildId, query);
            _memories = results.ToList();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task FilterByCategoryAsync(string category)
    {
        _selectedCategory = category;
        await LoadMemoriesForChildAsync();
    }

    private async Task HandleSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoadMemoriesForChildAsync();
        }
    }

    private async Task ClearSearchAsync()
    {
        _searchQuery = "";
        await LoadMemoriesForChildAsync();
    }

    private void ShowDetails(MemoryEvent mem)
    {
        _selectedMemory = mem;
    }

    private void CloseDetails()
    {
        _selectedMemory = null;
    }

    private string GetCategoryCssClass(string category) => category switch
    {
        "Learning Experiences" => "cat-exp",
        "Observations" => "cat-obs",
        "Milestones" => "cat-mls",
        "Celebrations" => "cat-cel",
        "Daily Care" => "cat-care",
        _ => "cat-obs"
    };

    private string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text.Substring(0, maxLength) + "…";
    }

    private string CalculateAge(DateTime dob)
    {
        var today = DateTime.Today;
        var age = today.Year - dob.Year;
        if (dob.Date > today.AddYears(-age)) age--;
        
        if (age == 0)
        {
            var months = ((today.Year - dob.Year) * 12) + today.Month - dob.Month;
            if (today.Day < dob.Day) months--;
            return $"{Math.Max(1, months)} months";
        }
        
        return $"{age} yrs";
    }
}
