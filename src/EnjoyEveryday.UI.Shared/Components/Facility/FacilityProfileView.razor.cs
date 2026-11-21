using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.UI.Shared.Components.Facility;

public partial class FacilityProfileView
{
    [Parameter]
    public Guid OrganizationId { get; set; }

    [Parameter]
    public Guid? BranchId { get; set; }

    private FacilityProfileDto? _profile;
    private List<BranchOptionDto> _branches = new();
    private Guid? _activeBranchId;
    private bool _isLoading = true;
    private bool _isSubmittingInquiry = false;
    private bool _inquirySent = false;
    private string? _inquiryError;
    private string? _activeLightboxUrl;

    private CreateContactInquiryRequest _inquiryModel = new();

    private bool CanEdit =>
        UserContext.Roles.Any(r => r.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                  r.Equals("BranchAdmin", StringComparison.OrdinalIgnoreCase) ||
                                  r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) ||
        UserContext.HasPermission("FacilityProfile:Manage");

    private string FullAddress => string.Join(", ", new[]
    {
        _profile?.StreetAddress,
        _profile?.City,
        _profile?.State,
        _profile?.PostalCode,
        _profile?.Country
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

    protected override async Task OnInitializedAsync()
    {
        await LoadProfileAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (BranchId != _activeBranchId && BranchId.HasValue)
        {
            _activeBranchId = BranchId;
        }
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        _isLoading = true;
        try
        {
            _activeBranchId ??= (BranchId ?? UserContext.BranchId);
            var orgId = OrganizationId != Guid.Empty ? OrganizationId : (UserContext.OrganizationId ?? Guid.Empty);

            _profile = await FacilityProfileService.GetProfileAsync(orgId, _activeBranchId);
            if (_profile != null && _profile.OrganizationId != Guid.Empty)
            {
                orgId = _profile.OrganizationId;
            }
            else if (orgId == Guid.Empty)
            {
                orgId = Guid.Parse("20000000-0000-0000-0000-000000000001");
                _profile ??= await FacilityProfileService.GetProfileAsync(orgId, _activeBranchId);
            }

            _branches = (await FacilityProfileService.GetPublicBranchesAsync(orgId)).ToList();

            _inquiryModel.OrganizationId = orgId;
            _inquiryModel.BranchId = _activeBranchId;

            if (UserContext.IsAuthenticated)
            {
                _inquiryModel.SenderEmail = UserContext.Email;
                _inquiryModel.SenderUserId = UserContext.UserId;
            }
        }
        catch (Exception ex)
        {
            _inquiryError = "Could not load facility details: " + ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SwitchCampus(Guid? branchId)
    {
        _activeBranchId = branchId;
        _inquirySent = false;
        _inquiryError = null;
        await LoadProfileAsync();
    }

    private async Task HandleSubmitInquiry()
    {
        if (string.IsNullOrWhiteSpace(_inquiryModel.SenderName) ||
            string.IsNullOrWhiteSpace(_inquiryModel.SenderEmail) ||
            string.IsNullOrWhiteSpace(_inquiryModel.Subject) ||
            string.IsNullOrWhiteSpace(_inquiryModel.Message))
        {
            _inquiryError = "Please fill in all required fields.";
            return;
        }

        _isSubmittingInquiry = true;
        _inquiryError = null;

        try
        {
            await FacilityProfileService.SubmitInquiryAsync(_inquiryModel);
            _inquirySent = true;
            _inquiryModel = new CreateContactInquiryRequest
            {
                OrganizationId = _profile?.OrganizationId ?? OrganizationId,
                BranchId = _profile?.BranchId ?? BranchId,
                SenderEmail = UserContext.Email,
                SenderUserId = UserContext.UserId
            };
        }
        catch (Exception ex)
        {
            _inquiryError = "Failed to submit inquiry: " + ex.Message;
        }
        finally
        {
            _isSubmittingInquiry = false;
        }
    }

    private string GetDirectionsLink()
    {
        if (!string.IsNullOrEmpty(_profile?.MapUrl))
            return _profile.MapUrl;

        var encoded = Uri.EscapeDataString(FullAddress);
        return $"https://www.google.com/maps/search/?api=1&query={encoded}";
    }
}
