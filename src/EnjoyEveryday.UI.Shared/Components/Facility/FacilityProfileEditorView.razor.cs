using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.UI.Shared.Components;

namespace EnjoyEveryday.UI.Shared.Components.Facility;

public partial class FacilityProfileEditorView
{
    [Parameter]
    public Guid? DefaultBranchId { get; set; }

    [Parameter]
    public string InitialTab { get; set; } = "editor";

    private List<BranchOptionDto> _branches = new();
    private SaveFacilityProfileRequest _editModel = new();
    private List<ContactInquiryDto> _inquiries = new();
    private List<ContactInquiryDto> _filteredInquiries => 
        string.IsNullOrEmpty(_inquiryStatusFilter) 
            ? _inquiries 
            : _inquiries.Where(i => i.Status.Equals(_inquiryStatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

    private string _activeTab = "editor";
    private string _selectedScopeKey = "org";
    private string? _inquiryStatusFilter = null;
    private string? _notificationMessage;
    private bool _isError = false;
    private bool _isSaving = false;
    private string? _newTagText;
    private string? _newGalleryPhotoUrl;
    private int _newInquiryCount => _inquiries.Count(i => i.Status == "New");

    private Guid _currentOrgId => UserContext.OrganizationId ?? (_branches.FirstOrDefault(b => b.Id == _currentBranchId)?.OrganizationId ?? Guid.Parse("20000000-0000-0000-0000-000000000001"));
    private Guid? _currentBranchId => _selectedScopeKey == "org" ? null : Guid.Parse(_selectedScopeKey);

    private bool IsOrgAdmin => UserContext.Roles.Any(r =>
        r.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
        r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        r.Equals("Superadmin", StringComparison.OrdinalIgnoreCase)) ||
        UserContext.HasPermission("organization.manage");

    private bool HasAccess => IsOrgAdmin || UserContext.Roles.Any(r => r.Equals("BranchAdmin", StringComparison.OrdinalIgnoreCase)) || UserContext.BranchId.HasValue;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(InitialTab))
        {
            _activeTab = InitialTab;
        }
        await LoadBranchesAndProfileAsync();
    }

    private async Task LoadBranchesAndProfileAsync()
    {
        try
        {
            var orgId = _currentOrgId;
            _branches = (await FacilityProfileService.GetEditableBranchesAsync(orgId)).ToList();

            if (DefaultBranchId.HasValue && _branches.Any(b => b.Id == DefaultBranchId.Value))
            {
                _selectedScopeKey = DefaultBranchId.Value.ToString();
            }
            else if (!IsOrgAdmin && _branches.Count > 0)
            {
                _selectedScopeKey = _branches.First().Id.ToString();
            }
            else
            {
                _selectedScopeKey = "org";
            }

            await LoadProfileForCurrentScopeAsync();
            await LoadInquiriesAsync();
        }
        catch (Exception ex)
        {
            _notificationMessage = "Failed to load facility configuration: " + ex.Message;
            _isError = true;
        }
    }

    private async Task LoadProfileForCurrentScopeAsync()
    {
        var existing = await FacilityProfileService.GetExactProfileAsync(_currentOrgId, _currentBranchId);
        if (existing != null)
        {
            _editModel = new SaveFacilityProfileRequest
            {
                Id = existing.Id,
                OrganizationId = existing.OrganizationId,
                BranchId = existing.BranchId,
                Title = existing.Title,
                Motto = existing.Motto,
                AboutStory = existing.AboutStory,
                LogoMediaUrl = existing.LogoMediaUrl,
                CoverMediaUrl = existing.CoverMediaUrl,
                GalleryMediaUrls = existing.GalleryMediaUrls ?? new(),
                Phone = existing.Phone,
                SecondaryPhone = existing.SecondaryPhone,
                EmergencyPhone = existing.EmergencyPhone,
                Email = existing.Email,
                InquiryEmail = existing.InquiryEmail,
                OperatingHours = existing.OperatingHours,
                StreetAddress = existing.StreetAddress,
                City = existing.City,
                State = existing.State,
                PostalCode = existing.PostalCode,
                Country = existing.Country ?? "Canada",
                MapUrl = existing.MapUrl,
                DirectionsNotes = existing.DirectionsNotes,
                CoreValues = existing.CoreValues ?? new()
            };
        }
        else
        {
            // Initialize fresh template
            var branchName = _branches.FirstOrDefault(b => b.Id == _currentBranchId)?.Name;
            _editModel = new SaveFacilityProfileRequest
            {
                OrganizationId = _currentOrgId,
                BranchId = _currentBranchId,
                Title = _currentBranchId.HasValue ? $"{branchName} Campus" : "Early Learning Center",
                Motto = "Nurturing curious minds through wonder, play, and nature.",
                Country = "Canada",
                OperatingHours = "Monday – Friday: 7:00 AM – 6:00 PM"
            };
        }
    }

    private async Task LoadInquiriesAsync()
    {
        _inquiries = (await FacilityProfileService.GetInquiriesAsync(_currentOrgId, _currentBranchId)).ToList();
    }

    private async Task HandleScopeChanged(ChangeEventArgs e)
    {
        _selectedScopeKey = e.Value?.ToString() ?? "org";
        await LoadProfileForCurrentScopeAsync();
        await LoadInquiriesAsync();
    }

    private async Task HandleSaveProfile()
    {
        // Auto-commit any pending core value tag or gallery photo before saving
        AddValueTag();
        ConfirmAddGalleryPhoto();

        if (string.IsNullOrWhiteSpace(_editModel.Title))
        {
            _notificationMessage = "Display Title is required.";
            _isError = true;
            return;
        }

        _isSaving = true;
        _notificationMessage = null;

        try
        {
            _editModel.OrganizationId = _currentOrgId;
            _editModel.BranchId = _currentBranchId;

            await FacilityProfileService.SaveProfileAsync(_editModel);
            _notificationMessage = "Profile updated and published successfully!";
            _isError = false;
        }
        catch (Exception ex)
        {
            _notificationMessage = "Error saving profile: " + ex.Message;
            _isError = true;
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void AddValueTag()
    {
        if (!string.IsNullOrWhiteSpace(_newTagText) && !_editModel.CoreValues.Contains(_newTagText.Trim()))
        {
            _editModel.CoreValues.Add(_newTagText.Trim());
            _newTagText = string.Empty;
        }
    }

    private void HandleTagKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddValueTag();
        }
    }

    private void RemoveValueTag(string tag)
    {
        _editModel.CoreValues.Remove(tag);
    }

    private void ConfirmAddGalleryPhoto()
    {
        if (!string.IsNullOrWhiteSpace(_newGalleryPhotoUrl))
        {
            _editModel.GalleryMediaUrls.Add(_newGalleryPhotoUrl);
            _newGalleryPhotoUrl = null;
        }
    }

    private void RemoveGalleryPhoto(string photoUrl)
    {
        _editModel.GalleryMediaUrls.Remove(photoUrl);
    }

    private void SetInquiryFilter(string? status)
    {
        _inquiryStatusFilter = status;
    }

    private async Task UpdateStatus(Guid inquiryId, string newStatus)
    {
        try
        {
            await FacilityProfileService.UpdateInquiryStatusAsync(new UpdateInquiryStatusRequest
            {
                InquiryId = inquiryId,
                Status = newStatus
            });
            await LoadInquiriesAsync();
            _notificationMessage = $"Inquiry updated to {newStatus}.";
            _isError = false;
        }
        catch (Exception ex)
        {
            _notificationMessage = "Failed to update inquiry: " + ex.Message;
            _isError = true;
        }
    }
}
