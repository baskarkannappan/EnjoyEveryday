using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.UI.Shared.Views.Organization;

public partial class ClassroomsDirectoryView : ComponentBase
{
    [Inject] private ClassroomService ClassroomService { get; set; } = default!;
    [Inject] private BranchService BranchService { get; set; } = default!;
    [Inject] private OrganizationService OrgService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IUserContext UserContext { get; set; } = default!;

    [Parameter] public bool UseModalForEdit { get; set; } = true;

    private IEnumerable<Classroom>? _classrooms;
    private Dictionary<Guid, Branch> _branches = new();
    private Guid _selectedBranchId = Guid.Empty;
    private string _searchQuery = "";

    // Modal state
    private Classroom _currentClassroom = new();
    private bool _isModalOpen = false;
    private bool _isEditing = false;
    private Guid _defaultBranchId = Guid.Empty;

    private Guid SelectedBranchId
    {
        get => _selectedBranchId;
        set
        {
            if (_selectedBranchId != value)
            {
                _selectedBranchId = value;
                _ = LoadClassroomsAsync();
            }
        }
    }

    private IEnumerable<Classroom> FilteredClassrooms =>
        (_classrooms ?? Enumerable.Empty<Classroom>()).Where(c =>
            string.IsNullOrWhiteSpace(_searchQuery) ||
            (c.Name?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.AgeGroup?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override async Task OnInitializedAsync()
    {
        var orgs = (await OrgService.GetOrganizationsAsync()).ToList();
        var defaultOrg = (UserContext.OrganizationId.HasValue
            ? orgs.FirstOrDefault(o => o.Id == UserContext.OrganizationId.Value)
            : null) ?? orgs.FirstOrDefault();

        if (defaultOrg != null)
        {
            var branchesList = (await BranchService.GetBranchesAsync(defaultOrg.Id)).ToList();
            _branches = branchesList.ToDictionary(b => b.Id);
            _defaultBranchId = branchesList.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        await LoadClassroomsAsync();
    }

    private async Task LoadClassroomsAsync()
    {
        if (_selectedBranchId != Guid.Empty)
        {
            _classrooms = await ClassroomService.GetClassroomsAsync(_selectedBranchId);
        }
        else
        {
            _classrooms = await ClassroomService.GetAllClassroomsAsync();
        }
        StateHasChanged();
    }

    private void HandleAddClassroom()
    {
        if (UseModalForEdit)
        {
            _currentClassroom = new Classroom
            {
                BranchId = _selectedBranchId != Guid.Empty ? _selectedBranchId : _defaultBranchId,
                AgeGroup = "3-4",
                Capacity = 18,
                Environment = "Indoor"
            };
            _isEditing = false;
            _isModalOpen = true;
        }
        else
        {
            NavigationManager.NavigateTo("/classrooms/new");
        }
    }

    private void HandleClassroomClick(Classroom classroom)
    {
        if (UseModalForEdit)
        {
            _currentClassroom = new Classroom
            {
                Id = classroom.Id,
                TenantId = classroom.TenantId,
                BranchId = classroom.BranchId,
                Name = classroom.Name,
                AgeGroup = classroom.AgeGroup,
                Capacity = classroom.Capacity,
                Environment = classroom.Environment,
                IsActive = classroom.IsActive
            };
            _isEditing = true;
            _isModalOpen = true;
        }
        else
        {
            NavigationManager.NavigateTo($"/classrooms/view/{classroom.Id}");
        }
    }

    private void CloseDialog()
    {
        _isModalOpen = false;
    }

    private async Task SaveClassroomAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentClassroom.Name))
        {
            return;
        }

        var targetBranch = _currentClassroom.BranchId != Guid.Empty
            ? _currentClassroom.BranchId
            : (_selectedBranchId != Guid.Empty ? _selectedBranchId : _defaultBranchId);

        if (_isEditing)
        {
            await ClassroomService.UpdateClassroomAsync(_currentClassroom);
        }
        else
        {
            await ClassroomService.CreateClassroomAsync(
                targetBranch,
                _currentClassroom.Name,
                _currentClassroom.AgeGroup,
                _currentClassroom.Capacity,
                _currentClassroom.Environment ?? "Indoor");
        }

        await LoadClassroomsAsync();
        CloseDialog();
    }

    private async Task DeleteClassroomAsync()
    {
        if (_currentClassroom.Id != Guid.Empty)
        {
            await ClassroomService.DeleteClassroomAsync(_currentClassroom.Id);
            await LoadClassroomsAsync();
            CloseDialog();
        }
    }
}
