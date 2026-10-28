using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Authorization;
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

    [Inject] private EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext { get; set; } = default!;
    [Inject] private EnjoyEveryday.Domain.Repositories.IAccessScopeRepository AccessScopeRepo { get; set; } = default!;

    [Parameter] public bool UseModalForEdit { get; set; } = true;

    private IEnumerable<Classroom>? _classrooms;
    private Dictionary<Guid, Branch> _branches = new();
    private Guid _selectedBranchId = Guid.Empty;
    private string _searchQuery = "";

    private bool _isOrgAdmin = false;
    private bool _isBranchAdmin = false;
    private bool _isTeacherOnly = false;
    private HashSet<Guid> _assignedBranchIds = new();
    private HashSet<Guid> _assignedClassroomIds = new();

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
        var tenantId = TenantContext.TenantId;
        var userId = UserContext.UserId;
        var userRoles = (UserContext.Roles ?? Enumerable.Empty<string>()).ToList();
        _isOrgAdmin = userRoles.Any(r =>
            r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase));
        _isBranchAdmin = !_isOrgAdmin && userRoles.Any(r => r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase));

        var orgs = (await OrgService.GetOrganizationsAsync()).ToList();
        var defaultOrg = (UserContext.OrganizationId.HasValue
            ? orgs.FirstOrDefault(o => o.Id == UserContext.OrganizationId.Value)
            : null) ?? orgs.FirstOrDefault();

        if (tenantId != Guid.Empty && userId != Guid.Empty)
        {
            var classroomIds = await AccessScopeRepo.GetAssignedClassroomIdsAsync(tenantId, userId);
            _assignedClassroomIds = new HashSet<Guid>(classroomIds);

            if (defaultOrg != null)
            {
                var branchIds = await AccessScopeRepo.GetAssignedBranchIdsAsync(tenantId, defaultOrg.Id, userId);
                _assignedBranchIds = new HashSet<Guid>(branchIds);
            }
        }

        _isTeacherOnly = !_isOrgAdmin && !_isBranchAdmin;

        if (defaultOrg != null)
        {
            var branchesList = (await BranchService.GetBranchesAsync(defaultOrg.Id)).ToList();
            _branches = branchesList.ToDictionary(b => b.Id);
            _defaultBranchId = branchesList.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (_isBranchAdmin && _assignedBranchIds.Count > 0)
        {
            _branches = _branches.Where(kvp => _assignedBranchIds.Contains(kvp.Key))
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _selectedBranchId = _assignedBranchIds.First();
            _defaultBranchId = _selectedBranchId;
        }

        await LoadClassroomsAsync();
    }

    private async Task LoadClassroomsAsync()
    {
        if (_isBranchAdmin && _assignedBranchIds.Count > 0)
        {
            var branchId = _selectedBranchId != Guid.Empty ? _selectedBranchId : _assignedBranchIds.First();
            _classrooms = await ClassroomService.GetClassroomsAsync(branchId);
        }
        else if (_isTeacherOnly)
        {
            var allClasses = await ClassroomService.GetAllClassroomsAsync();
            _classrooms = allClasses.Where(c => _assignedClassroomIds.Contains(c.Id)).ToList();
        }
        else if (_selectedBranchId != Guid.Empty)
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
