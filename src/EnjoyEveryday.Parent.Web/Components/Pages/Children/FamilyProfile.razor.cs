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

namespace EnjoyEveryday.Parent.Web.Components.Pages.Children;

public partial class FamilyProfile
{
    private Person? _person;
    private List<ChildConsentDocument> _consentDocuments = new();
    private List<AuthorizedPickupDto> _pickups = new();
    private List<DaycareAnnouncement> _announcements = new();

    private bool _loadingProfile = true;
    private bool _loadingConsent = true;
    private bool _loadingPickups = true;
    private bool _loadingAnnouncements = true;
    private bool _isSaving = false;

    private bool _isPrivacyApproved = false;
    private DateTimeOffset? _privacyApprovedDate;
    private bool _isApprovingPrivacy = false;

    private bool _showPickupModal = false;
    private bool _isEditingPickup = false;
    private bool _isSavingPickup = false;
    private string? _pickupModalError;
    private AuthorizedPickupDto _pickupForm = new();

    private string? _mobilePhone;
    private string _preferredContactMethod = "Mobile";
    private string _preferredLanguage = "en";
    private string? _statusMessage;

    protected override async Task OnInitializedAsync()
    {
        SessionState.OnChildChanged += OnChildSelectionChanged;
        Localization.OnLanguageChanged += StateHasChanged;
        if (SessionState.SelectedChild == null)
        {
            var children = await ParentService.GetAuthorizedChildrenAsync();
            SessionState.SetAuthorizedChildren(children);
        }
        await LoadProfile();
        await LoadChildDataAsync();
    }

    private async void OnChildSelectionChanged()
    {
        await LoadChildDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadChildDataAsync()
    {
        await LoadConsentDocuments();
        await LoadPickupsAsync();
        await LoadPrivacyStatusAsync();
        await LoadAnnouncementsAsync();
    }

    private async Task LoadProfile()
    {
        try
        {
            _loadingProfile = true;
            _person = await ParentService.GetParentProfileAsync();
            if (_person != null)
            {
                _mobilePhone = _person.MobilePhone;
                _preferredContactMethod = _person.PreferredContactMethod ?? "Mobile";
                _preferredLanguage = !string.IsNullOrWhiteSpace(_person.PreferredLanguage) ? _person.PreferredLanguage : "en";
            }
        }
        finally
        {
            _loadingProfile = false;
        }
    }

    private async Task LoadConsentDocuments()
    {
        if (SessionState.SelectedChild == null)
        {
            _loadingConsent = false;
            return;
        }

        try
        {
            _loadingConsent = true;
            var docs = await ConsentService.GetDocumentsAsync(childId: SessionState.SelectedChild.ChildId);
            _consentDocuments = docs.ToList();
        }
        catch (Exception)
        {
            _consentDocuments = new List<ChildConsentDocument>();
        }
        finally
        {
            _loadingConsent = false;
        }
    }

    private async Task LoadPickupsAsync()
    {
        if (SessionState.SelectedChild == null)
        {
            _loadingPickups = false;
            return;
        }

        try
        {
            _loadingPickups = true;
            var profile = await ParentService.GetChildPickupSecurityProfileAsync(SessionState.SelectedChild.ChildId);
            _pickups = profile?.AuthorizedPickups ?? new List<AuthorizedPickupDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FamilyProfile] Error loading pickups: {ex.Message}");
            _pickups = new List<AuthorizedPickupDto>();
        }
        finally
        {
            _loadingPickups = false;
        }
    }

    private async Task LoadPrivacyStatusAsync()
    {
        if (SessionState.SelectedChild == null) return;
        try
        {
            _isPrivacyApproved = await ParentService.IsPrivacyAndTrustApprovedAsync(SessionState.SelectedChild.ChildId);
            if (_isPrivacyApproved)
            {
                try
                {
                    _privacyApprovedDate = await ParentService.GetPrivacyAndTrustApprovalDateAsync(SessionState.SelectedChild.ChildId) ?? DateTimeOffset.UtcNow;
                }
                catch
                {
                    _privacyApprovedDate = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                _privacyApprovedDate = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FamilyProfile] Error loading privacy status: {ex.Message}");
            _isPrivacyApproved = false;
        }
    }

    private async Task LoadAnnouncementsAsync()
    {
        try
        {
            _loadingAnnouncements = true;
            var tenantId = SessionState.SelectedChild?.TenantId ?? Guid.Parse("10000000-0000-0000-0000-000000000001");
            if (tenantId == Guid.Empty) tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var orgId = SessionState.SelectedChild?.OrganizationId;
            var branchId = SessionState.SelectedChild?.BranchId;

            var items = await AnnouncementService.GetAnnouncementsAsync(tenantId, orgId, branchId);
            _announcements = items.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FamilyProfile] Error loading announcements: {ex.Message}");
            _announcements = new List<DaycareAnnouncement>();
        }
        finally
        {
            _loadingAnnouncements = false;
        }
    }

    private void OpenAddPickupModal()
    {
        if (SessionState.SelectedChild == null) return;
        _isEditingPickup = false;
        _pickupModalError = null;
        _pickupForm = new AuthorizedPickupDto
        {
            Id = Guid.NewGuid(),
            ChildId = SessionState.SelectedChild.ChildId,
            Relationship = "Grandparent",
            IsActive = true
        };
        _showPickupModal = true;
    }

    private void EditPickup(AuthorizedPickupDto pickup)
    {
        _isEditingPickup = true;
        _pickupModalError = null;
        _pickupForm = new AuthorizedPickupDto
        {
            Id = pickup.Id,
            ChildId = pickup.ChildId,
            PersonId = pickup.PersonId,
            FullName = pickup.FullName,
            Relationship = pickup.Relationship,
            Phone = pickup.Phone,
            PickupCode = pickup.PickupCode,
            Notes = pickup.Notes,
            IsActive = pickup.IsActive,
            IsPrimaryGuardian = pickup.IsPrimaryGuardian
        };
        _showPickupModal = true;
    }

    private void ClosePickupModal()
    {
        _showPickupModal = false;
        _pickupModalError = null;
    }

    private async Task SavePickup()
    {
        if (SessionState.SelectedChild == null) return;

        if (string.IsNullOrWhiteSpace(_pickupForm.FullName))
        {
            _pickupModalError = "Full Legal Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_pickupForm.Phone))
        {
            _pickupModalError = "Mobile Phone Number is required for checkout security verification.";
            return;
        }

        // Enforce mandatory primary guardian if none exists
        if (!_pickups.Any(p => p.IsPrimaryGuardian && p.Id != _pickupForm.Id))
        {
            _pickupForm.IsPrimaryGuardian = true;
        }

        try
        {
            _isSavingPickup = true;
            _pickupModalError = null;
            await ParentService.SaveAuthorizedPickupAsync(SessionState.SelectedChild.ChildId, _pickupForm);
            _showPickupModal = false;
            await LoadPickupsAsync();
            _statusMessage = $"Authorized pickup '{_pickupForm.FullName}' saved and synchronized across the daycare system.";
        }
        catch (Exception ex)
        {
            _pickupModalError = $"Failed to save: {ex.Message}";
        }
        finally
        {
            _isSavingPickup = false;
        }
    }

    private async Task DeletePickup(Guid pickupId)
    {
        if (SessionState.SelectedChild == null) return;

        try
        {
            var success = await ParentService.DeleteAuthorizedPickupAsync(SessionState.SelectedChild.ChildId, pickupId);
            if (success)
            {
                await LoadPickupsAsync();
                _statusMessage = "Authorized pickup contact removed.";
            }
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task ApprovePrivacyTrust()
    {
        if (SessionState.SelectedChild == null) return;

        try
        {
            _isApprovingPrivacy = true;
            var approver = _person != null ? $"{_person.FirstName} {_person.LastName}".Trim() : "Parent Guardian";
            await ParentService.ApprovePrivacyAndTrustAsync(SessionState.SelectedChild.ChildId, approver);
            _isPrivacyApproved = true;
            _privacyApprovedDate = DateTimeOffset.UtcNow;
            await LoadPrivacyStatusAsync();
            _isPrivacyApproved = true;
            _statusMessage = "Privacy & Trust terms approved! Your verified compliance status has synced across all daycare portals.";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Failed to approve: {ex.Message}";
        }
        finally
        {
            _isApprovingPrivacy = false;
        }
    }

    private async Task SaveContactDetails()
    {
        try
        {
            _isSaving = true;
            await ParentService.UpdateParentContactAsync(_mobilePhone, _preferredContactMethod, _preferredLanguage);
            Localization.SetLanguage(_preferredLanguage);
            _statusMessage = "Contact preferences and language updated successfully.";
            await LoadProfile();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void HandleReviewConsent(Guid documentId)
    {
        Navigation.NavigateTo($"/consent/sign/{documentId}");
    }

    public void Dispose()
    {
        SessionState.OnChildChanged -= OnChildSelectionChanged;
        Localization.OnLanguageChanged -= StateHasChanged;
    }
}
