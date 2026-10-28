using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Authorization;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Children;

public partial class EnrollmentPipeline : ComponentBase
{
    [Inject]
    private IEnrollmentPipelineService PipelineService { get; set; } = default!;

    [Inject]
    private ClassroomService ClassroomService { get; set; } = default!;

    [Inject]
    private BranchService BranchService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.Shared.Tenancy.IUserContext UserContext { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.Domain.Repositories.IAccessScopeRepository AccessScopeRepo { get; set; } = default!;

    [Inject]
    private EnjoyEveryday.Daycare.Web.Services.IPageAuthorizationService PageAuth { get; set; } = default!;

    private PipelineOverviewDto? _overview;
    private List<EnrollmentPipelineApplicationDto> _filteredApplications = new();
    private List<FamilyBillingContract> _familyContracts = new();
    private List<Classroom> _classrooms = new();

    private bool _isOrgAdmin = false;
    private bool _isBranchAdmin = false;
    private HashSet<Guid> _assignedBranchIds = new();

    private string _activeView = "Pipeline";
    private string? _selectedStage = null;
    private string _searchQuery = string.Empty;
    private bool _isLoading = true;
    private string? _statusMessage;
    private bool _isError;

    // Modal state
    private bool _isNewInquiryModalOpen;
    private CreateApplicationRequest _newInquiry = new();

    private bool _isTourModalOpen;
    private EnrollmentPipelineApplicationDto? _selectedApp;
    private DateTime _tourDateTime = DateTime.UtcNow.AddDays(2).Date.AddHours(10);
    private string _tourNotes = string.Empty;

    private bool _isEnrollModalOpen;
    private FinalizeEnrollmentContractRequest _enrollContract = new();

    protected override async Task OnInitializedAsync()
    {
        if (!await PageAuth.EnsureAuthorizedAsync("/enrollment"))
        {
            return;
        }

        var tenantId = TenantContext.TenantId;
        var userId = UserContext.UserId;
        var userRoles = (UserContext.Roles ?? Enumerable.Empty<string>()).ToList();
        _isOrgAdmin = userRoles.Any(r =>
            r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationOwner, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(RoleTemplates.OrganizationAdmin, StringComparison.OrdinalIgnoreCase));
        _isBranchAdmin = !_isOrgAdmin && userRoles.Any(r => r.Equals(RoleTemplates.BranchAdmin, StringComparison.OrdinalIgnoreCase));

        var orgId = UserContext.OrganizationId ?? Guid.Parse("20000000-0000-0000-0000-000000000001");
        if (tenantId != Guid.Empty && userId != Guid.Empty)
        {
            var branchIds = await AccessScopeRepo.GetAssignedBranchIdsAsync(tenantId, orgId, userId);
            _assignedBranchIds = new HashSet<Guid>(branchIds);
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        try
        {
            _overview = await PipelineService.GetPipelineOverviewAsync();
            _familyContracts = await PipelineService.GetActiveFamilyContractsAsync();
            var allClasses = (await ClassroomService.GetAllClassroomsAsync()).ToList();

            if (_isBranchAdmin && _assignedBranchIds.Count > 0)
            {
                _classrooms = allClasses.Where(c => _assignedBranchIds.Contains(c.BranchId)).ToList();
            }
            else
            {
                _classrooms = allClasses;
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            _isError = true;
            _statusMessage = $"Error loading admissions pipeline: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        if (_overview?.Applications == null)
        {
            _filteredApplications = new();
            return;
        }

        var list = _overview.Applications.AsEnumerable();

        if (_isBranchAdmin && _assignedBranchIds.Count > 0)
        {
            var branchClassroomIds = _classrooms.Select(c => c.Id).ToHashSet();
            list = list.Where(a =>
                (a.BranchId.HasValue && _assignedBranchIds.Contains(a.BranchId.Value)) ||
                (a.TargetClassroomId.HasValue && branchClassroomIds.Contains(a.TargetClassroomId.Value)) ||
                (!a.BranchId.HasValue && !a.TargetClassroomId.HasValue));
        }

        if (!string.IsNullOrWhiteSpace(_selectedStage))
        {
            list = list.Where(a => a.PipelineStage == _selectedStage);
        }

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            var q = _searchQuery.Trim().ToLowerInvariant();
            list = list.Where(a =>
                a.ChildFullName.ToLowerInvariant().Contains(q) ||
                a.ParentGuardianName.ToLowerInvariant().Contains(q) ||
                a.ParentEmail.ToLowerInvariant().Contains(q) ||
                (a.TargetClassroomName != null && a.TargetClassroomName.ToLowerInvariant().Contains(q))
            );
        }

        _filteredApplications = list.ToList();
    }

    private void SetView(string view)
    {
        _activeView = view;
    }

    private void FilterByStage(string? stage)
    {
        _selectedStage = stage;
        ApplyFilters();
    }

    private void OpenNewInquiryModal()
    {
        _newInquiry = new CreateApplicationRequest
        {
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-2),
            TuitionMonthlyRate = 850m,
            DepositAmount = 200m,
            RegistrationFee = 50m,
            PipelineStage = "Enquiry",
            DesiredStartDate = DateTime.UtcNow.Date.AddDays(14),
            BranchId = _isBranchAdmin && _assignedBranchIds.Count > 0 ? _assignedBranchIds.First() : null
        };
        _isNewInquiryModalOpen = true;
    }

    private async Task SubmitNewInquiry()
    {
        if (string.IsNullOrWhiteSpace(_newInquiry.ChildFirstName) || string.IsNullOrWhiteSpace(_newInquiry.ChildLastName))
        {
            _isError = true;
            _statusMessage = "Child first and last name are required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_newInquiry.ParentGuardianName) || string.IsNullOrWhiteSpace(_newInquiry.ParentEmail))
        {
            _isError = true;
            _statusMessage = "Parent guardian name and email are required.";
            return;
        }

        try
        {
            await PipelineService.CreateApplicationAsync(_newInquiry);
            _isNewInquiryModalOpen = false;
            _isError = false;
            _statusMessage = $"Admissions inquiry registered for {_newInquiry.ChildFirstName} {_newInquiry.ChildLastName}.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _isError = true;
            _statusMessage = $"Failed to create inquiry: {ex.Message}";
        }
    }

    private async Task AdvanceStage(Guid appId, string nextStage)
    {
        try
        {
            await PipelineService.UpdateStageAsync(appId, nextStage);
            _isError = false;
            _statusMessage = $"Application moved to {GetStageDisplayName(nextStage)}.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _isError = true;
            _statusMessage = $"Failed to update stage: {ex.Message}";
        }
    }

    private Task HandleAdvanceStage((Guid AppId, string NextStage) args) => AdvanceStage(args.AppId, args.NextStage);

    private void OpenTourModal(EnrollmentPipelineApplicationDto app)
    {
        _selectedApp = app;
        _tourDateTime = app.TourDate ?? DateTime.UtcNow.Date.AddDays(2).AddHours(10);
        _tourNotes = app.InternalNotes ?? string.Empty;
        _isTourModalOpen = true;
    }

    private async Task SaveTour()
    {
        if (_selectedApp == null) return;
        try
        {
            await PipelineService.UpdateStageAsync(_selectedApp.Id, "TourScheduled", _tourNotes, _tourDateTime);
            _isTourModalOpen = false;
            _isError = false;
            _statusMessage = $"Campus tour scheduled for {_selectedApp.ChildFullName} on {_tourDateTime:MMM dd @ h:mm tt}.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _isError = true;
            _statusMessage = $"Failed to schedule tour: {ex.Message}";
        }
    }

    private void OpenEnrollmentModal(EnrollmentPipelineApplicationDto app)
    {
        _selectedApp = app;
        _enrollContract = new FinalizeEnrollmentContractRequest
        {
            ApplicationId = app.Id,
            ClassroomId = app.TargetClassroomId ?? (_classrooms.FirstOrDefault()?.Id ?? Guid.Empty),
            ScheduleType = app.ScheduleType,
            BillingCycle = "Monthly",
            BaseMonthlyRate = app.TuitionMonthlyRate,
            SiblingDiscountPercent = app.SiblingDiscountPercent,
            DepositHeld = app.DepositAmount,
            StartDate = app.DesiredStartDate ?? DateTime.UtcNow.Date,
            CreateInitialTuitionInvoice = true
        };
        _isEnrollModalOpen = true;
    }

    private async Task ConfirmEnrollment()
    {
        if (_selectedApp == null) return;
        try
        {
            await PipelineService.FinalizeEnrollmentAndBillingAsync(_enrollContract);
            _isEnrollModalOpen = false;
            _isError = false;
            _statusMessage = $"Congratulations! {_selectedApp.ChildFullName} is officially enrolled and family tuition contract has been recorded.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _isError = true;
            _statusMessage = $"Failed to finalize enrollment: {ex.Message}";
        }
    }

    private string GetStageDisplayName(string stage) => stage switch
    {
        "Enquiry" => "Inquiry / Lead",
        "TourScheduled" => "Tour Scheduled",
        "ApplicationSubmitted" => "Application In",
        "UnderReview" => "Under Review",
        "OfferSent" => "Offer Extended",
        "Enrolled" => "Enrolled",
        "Waitlisted" => "Waitlisted",
        "Withdrawn" => "Withdrawn",
        _ => stage
    };

    private string FormatSchedule(string schedule) => schedule switch
    {
        "FullTime" => "Full-Time (M-F)",
        "PartTime_MWF" => "Part-Time (MWF)",
        "PartTime_TTh" => "Part-Time (T/Th)",
        "HalfDay_Morning" => "Half-Day Morning",
        _ => schedule
    };
}
