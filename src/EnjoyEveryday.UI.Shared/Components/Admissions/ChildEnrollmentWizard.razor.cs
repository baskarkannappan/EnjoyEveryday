using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using EnjoyEveryday.Shared.Authorization;

namespace EnjoyEveryday.UI.Shared.Components.Admissions;

public partial class ChildEnrollmentWizard
{
    [Parameter] public Guid? Id { get; set; }

    private int currentStep = 1;
    private int maxAllowedStep = 1;
    private Child? _draftChild;
    
    private List<string> wizardSteps = new() 
    {
        "Child Information", "Parent / Guardian", "Address", "Emergency Contacts",
        "Medical & Allergies", "Dietary & Routine", "Enrollment", "Authorized Pickup",
        "Consents", "Review"
    };

    private IReadOnlyList<string> _guardianRelationships = new[] { "Mother", "Father", "Guardian", "Grandparent", "Other" };
    private IReadOnlyList<string> _toiletTrainingStatuses = new[] { "Not Started", "In Progress", "Fully Trained" };
    private IReadOnlyList<string> _scheduleTypes = new[] { "Full Time", "Part Time", "Drop In" };

    private void NavigateToStep(int step)
    {
        currentStep = step;
    }

    protected override async Task OnInitializedAsync()
    {
        // Dynamically load lookup catalogs from the database
        try
        {
            var relLookups = await LookupCatalog.GetLookupsByCategoryAsync("GuardianRelationship");
            if (relLookups.Any()) _guardianRelationships = relLookups.Select(l => l.DisplayName).ToList();

            var ttLookups = await LookupCatalog.GetLookupsByCategoryAsync("ToiletTrainingStatus");
            if (ttLookups.Any()) _toiletTrainingStatuses = ttLookups.Select(l => l.DisplayName).ToList();

            var schedLookups = await LookupCatalog.GetLookupsByCategoryAsync("EnrollmentScheduleType");
            if (schedLookups.Any()) _scheduleTypes = schedLookups.Select(l => l.DisplayName).ToList();
        }
        catch { }

        var orgs = await OrganizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org != null)
        {
            _branches = (await BranchService.GetBranchesAsync(org.Id)).ToList();
            _classrooms.Clear();
            foreach (var branch in _branches)
            {
                var classrooms = await ClassroomService.GetClassroomsAsync(branch.Id);
                _classrooms.AddRange(classrooms);
            }

            if (!Id.HasValue)
            {
                try
                {
                    var quota = await QuotaService.GetQuotaUsageAsync(org.Id);
                    if (quota != null && !quota.CanAddChild)
                    {
                        _stepErrorMessage = $"You are exceeding your quota limit. Your current plan '{quota.PlanName}' allows a maximum of {quota.MaxChildren} children (currently using {quota.CurrentChildren}/{quota.MaxChildren}). Please upgrade your subscription plan to enroll more children.";
                    }
                }
                catch { }
            }
        }

        if (Id.HasValue)
        {
            _draftChild = await ChildService.GetChildByIdAsync(Id.Value);
            if (_draftChild != null)
            {
                childFirstName = _draftChild.FirstName;
                childLastName = _draftChild.LastName;
                childDob = _draftChild.DateOfBirth;
                childPhotoPreview = _draftChild.PhotoUrl ?? "";
                if (_draftChild.ClassroomId.HasValue)
                {
                    selectedClassroomId = _draftChild.ClassroomId.Value.ToString();
                    var cls = _classrooms.FirstOrDefault(c => c.Id == _draftChild.ClassroomId.Value);
                    if (cls != null)
                    {
                        selectedBranchId = cls.BranchId.ToString();
                    }
                }
                
                if (!string.IsNullOrEmpty(_draftChild.DraftData))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(_draftChild.DraftData);
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("parentName", out var pName)) parentName = pName.GetString() ?? "";
                        if (root.TryGetProperty("parentRelationship", out var pRel)) parentRelationship = pRel.GetString() ?? "";
                        if (root.TryGetProperty("parentEmail", out var pEmail)) parentEmail = pEmail.GetString() ?? "";
                        if (root.TryGetProperty("parentPhone", out var pPhone)) parentPhone = pPhone.GetString() ?? "";
                        if (root.TryGetProperty("parentPhotoPreview", out var pPhoto)) parentPhotoPreview = pPhoto.GetString() ?? "";
                        
                        if (root.TryGetProperty("addressLine1", out var sAddr)) addressLine1 = sAddr.GetString() ?? "";
                        if (root.TryGetProperty("addressCity", out var cty)) addressCity = cty.GetString() ?? "";
                        if (root.TryGetProperty("addressProvince", out var st)) addressProvince = st.GetString() ?? "";
                        if (root.TryGetProperty("addressPostalCode", out var zp)) addressPostalCode = zp.GetString() ?? "";
                        
                        if (root.TryGetProperty("emergencyName", out var eName)) emergencyName = eName.GetString() ?? "";
                        if (root.TryGetProperty("emergencyPhone", out var ePhone)) emergencyPhone = ePhone.GetString() ?? "";
                        if (root.TryGetProperty("emergencyRelationship", out var eRel)) emergencyRelationship = eRel.GetString() ?? "";
                        
                        if (root.TryGetProperty("physicianName", out var pedName)) physicianName = pedName.GetString() ?? "";
                        if (root.TryGetProperty("physicianPhone", out var pedPhone)) physicianPhone = pedPhone.GetString() ?? "";
                        if (root.TryGetProperty("allergies", out var algs)) allergies = algs.GetString() ?? "";
                        if (root.TryGetProperty("medications", out var meds)) medications = meds.GetString() ?? "";
                        
                        if (root.TryGetProperty("dietaryRestrictions", out var diet)) dietaryRestrictions = diet.GetString() ?? "";
                        if (root.TryGetProperty("napRoutine", out var nap)) napRoutine = nap.GetString() ?? "";
                        if (root.TryGetProperty("toiletTraining", out var tt)) toiletTraining = tt.GetString() ?? "Not Started";
                        
                        if (root.TryGetProperty("enrollmentStartDate", out var eStart) && eStart.ValueKind != System.Text.Json.JsonValueKind.Null) 
                            enrollmentStartDate = eStart.GetDateTime();
                            
                        if (root.TryGetProperty("scheduleType", out var sched)) scheduleType = sched.GetString() ?? "Full Time";
                        if (root.TryGetProperty("selectedBranchId", out var brId)) selectedBranchId = brId.GetString() ?? "";
                        if (root.TryGetProperty("selectedClassroomId", out var clId)) selectedClassroomId = clId.GetString() ?? "";
                        
                        if (root.TryGetProperty("pickupName", out var pkName)) pickupName = pkName.GetString() ?? "";
                        if (root.TryGetProperty("pickupRelationship", out var pkRel)) pickupRelationship = pkRel.GetString() ?? "";
                        if (root.TryGetProperty("pickupCode", out var pkCode)) pickupCode = pkCode.GetString() ?? "";
                        
                        if (root.TryGetProperty("consentPhoto", out var cPhoto)) consentPhoto = cPhoto.GetBoolean();
                        if (root.TryGetProperty("consentMedical", out var cMed)) consentMedical = cMed.GetBoolean();
                        if (root.TryGetProperty("consentMarketing", out var cMkt)) consentMarketing = cMkt.GetBoolean();
                    }
                    catch { }
                }

                // Carry over and pre-populate onboarding details if not already in draft_data
                try
                {
                    // 1. Fallback to linked guardians
                    var guardians = (await ChildService.GetChildGuardiansAsync(_draftChild.Id)).ToList();
                    var primaryGuardian = guardians.FirstOrDefault(g => g.IsPrimary) ?? guardians.FirstOrDefault();
                    if (primaryGuardian != null)
                    {
                        if (string.IsNullOrWhiteSpace(parentName)) parentName = primaryGuardian.FullName;
                        if (string.IsNullOrWhiteSpace(parentEmail)) parentEmail = primaryGuardian.Email;
                        if (string.IsNullOrWhiteSpace(parentPhone)) parentPhone = primaryGuardian.Phone;
                        if (string.IsNullOrWhiteSpace(parentRelationship)) parentRelationship = primaryGuardian.Relationship;

                        if (string.IsNullOrWhiteSpace(emergencyName))
                        {
                            emergencyName = primaryGuardian.FullName;
                            emergencyRelationship = primaryGuardian.Relationship;
                            emergencyPhone = primaryGuardian.Phone;
                        }

                        if (string.IsNullOrWhiteSpace(pickupName))
                        {
                            pickupName = primaryGuardian.FullName;
                            pickupRelationship = primaryGuardian.Relationship;
                            pickupPhone = primaryGuardian.Phone;
                            pickupIsPrimary = true;
                        }
                    }

                    // 2. Fallback to Admissions Inquiry Pipeline record
                    var app = await PipelineService.GetApplicationByChildIdAsync(_draftChild.Id);
                    if (app != null)
                    {
                        if (string.IsNullOrWhiteSpace(parentName)) parentName = app.ParentGuardianName;
                        if (string.IsNullOrWhiteSpace(parentEmail)) parentEmail = app.ParentEmail;
                        if (string.IsNullOrWhiteSpace(parentPhone)) parentPhone = app.ParentPhone;

                        if (string.IsNullOrWhiteSpace(emergencyName))
                        {
                            emergencyName = app.ParentGuardianName;
                            emergencyRelationship = "Parent";
                            emergencyPhone = app.ParentPhone;
                        }

                        if (string.IsNullOrWhiteSpace(pickupName))
                        {
                            pickupName = app.ParentGuardianName;
                            pickupRelationship = "Parent";
                            pickupPhone = app.ParentPhone;
                            pickupIsPrimary = true;
                        }

                        if (app.DesiredStartDate.HasValue && (enrollmentStartDate == null || enrollmentStartDate == DateTime.Today.AddDays(7)))
                        {
                            enrollmentStartDate = app.DesiredStartDate.Value;
                        }

                        if (!string.IsNullOrWhiteSpace(app.ScheduleType) && (string.IsNullOrWhiteSpace(scheduleType) || scheduleType == "Full Time"))
                        {
                            scheduleType = app.ScheduleType;
                        }

                        if (string.IsNullOrWhiteSpace(allergies) && !string.IsNullOrWhiteSpace(app.InternalNotes))
                        {
                            allergies = ExtractAllergies(app.InternalNotes);
                        }
                    }
                }
                catch { }

                int calculatedStep = (_draftChild.ProfileCompletionPercentage / 10) + 1;
                if (calculatedStep <= 0) calculatedStep = 1;
                currentStep = Math.Min(calculatedStep, 10);
                maxAllowedStep = 10; // Allow free navigation across all steps for enrolled/editing child
            }
            await LoadPickupsAsync();
        }

        if (string.IsNullOrEmpty(selectedBranchId) && _branches.Count == 1)
        {
            selectedBranchId = _branches[0].Id.ToString();
        }

        if (!string.IsNullOrEmpty(selectedBranchId) && Guid.TryParse(selectedBranchId, out var initialBid))
        {
            await LoadReadinessForBranchAsync(initialBid);
        }
    }

    private string childFirstName = "";
    private string childLastName = "";
    private string childPreferredName = "";
    private DateTime? childDob = DateTime.Today.AddYears(-3);
    private string childPhotoPreview = "";

    private string parentName = "";
    private string parentRelationship = "Mother";
    private string parentEmail = "";
    private string parentPhone = "";
    private string parentPhotoPreview = "";

    private string addressLine1 = "";
    private string addressCity = "";
    private string addressProvince = "";
    private string addressPostalCode = "";

    private string emergencyName = "";
    private string emergencyRelationship = "";
    private string emergencyPhone = "";

    private string physicianName = "";
    private string physicianPhone = "";
    private string allergies = "";
    private string hasEpiPen = "false";
    private string medications = "";

    private string dietaryRestrictions = "";
    private string napRoutine = "";
    private string toiletTraining = "Not Started";

    private DateTime? enrollmentStartDate = DateTime.Today.AddDays(7);
    private string scheduleType = "Full Time";
    private string selectedBranchId = "";
    private string selectedClassroomId = "";

    private string pickupName = "";
    private string pickupRelationship = "";
    private string pickupPhone = "";
    private string pickupCode = "";
    private bool pickupIsPrimary = false;
    private List<AuthorizedPickupDto> _authorizedPickupsList = new();

    private bool consentPhoto = false;
    private bool consentMedical = false;
    private bool consentMarketing = false;

    private List<Branch> _branches = new();
    private List<Classroom> _classrooms = new();
    private ClassroomReadinessCheckResult? _branchReadiness;
    private Dictionary<Guid, ClassroomReadinessCheckResult> _classroomReadinessMap = new();
    private ClassroomReadinessCheckResult? _selectedClassroomReadiness;
    private string? _stepErrorMessage;
    private bool _isLoadingReadiness;

    private async Task LoadReadinessForBranchAsync(Guid branchId)
    {
        try
        {
            _isLoadingReadiness = true;
            _branchReadiness = await ClassroomReadinessService.ValidateBranchReadinessAsync(TenantContext.TenantId, branchId);
            _classroomReadinessMap = await ClassroomReadinessService.GetBranchClassroomsReadinessAsync(TenantContext.TenantId, branchId);
            if (Guid.TryParse(selectedClassroomId, out var cid) && _classroomReadinessMap.TryGetValue(cid, out var r))
            {
                _selectedClassroomReadiness = r;
            }
            else
            {
                _selectedClassroomReadiness = null;
            }
        }
        catch (Exception ex)
        {
            _stepErrorMessage = ex.Message;
        }
        finally
        {
            _isLoadingReadiness = false;
            StateHasChanged();
        }
    }

    private async Task OnBranchChanged(ChangeEventArgs e)
    {
        selectedBranchId = e.Value?.ToString() ?? "";
        selectedClassroomId = "";
        _selectedClassroomReadiness = null;
        _stepErrorMessage = null;
        if (Guid.TryParse(selectedBranchId, out var bid))
        {
            await LoadReadinessForBranchAsync(bid);
        }
        else
        {
            _branchReadiness = null;
            _classroomReadinessMap.Clear();
        }
    }

    private void OnClassroomChanged(ChangeEventArgs e)
    {
        selectedClassroomId = e.Value?.ToString() ?? "";
        _stepErrorMessage = null;
        if (Guid.TryParse(selectedClassroomId, out var cid) && _classroomReadinessMap.TryGetValue(cid, out var r))
        {
            _selectedClassroomReadiness = r;
        }
        else
        {
            _selectedClassroomReadiness = null;
        }
    }

    private string GetClassroomBadgeText(Guid classroomId)
    {
        if (_classroomReadinessMap.TryGetValue(classroomId, out var r))
        {
            if (r.IsReady) return $"— [Ready: {r.AvailableSpots} spots left]";
            if (!r.ClassroomActive) return "— [Inactive]";
            if (!r.HasAssignedTeacher) return "— [No Teacher Assigned]";
            if (!r.HasAvailableCapacity) return "— [Full: At Capacity]";
            return $"— [{r.Status}]";
        }
        return "";
    }
    
    private async Task NextStep()
    {
        _stepErrorMessage = null;
        if (currentStep == 7)
        {
            if (string.IsNullOrWhiteSpace(selectedBranchId))
            {
                _stepErrorMessage = "Please select an assigned branch.";
                return;
            }
            if (_branchReadiness != null && !_branchReadiness.IsReady)
            {
                _stepErrorMessage = "This branch is not ready for student onboarding. Please complete the missing setup before proceeding.";
                return;
            }
            if (string.IsNullOrWhiteSpace(selectedClassroomId))
            {
                _stepErrorMessage = "Please select an assigned classroom.";
                return;
            }
            if (Guid.TryParse(selectedClassroomId, out var cidVal) && _classroomReadinessMap.TryGetValue(cidVal, out var r) && !r.IsReady)
            {
                _stepErrorMessage = "The selected classroom is not available for enrollment. Please resolve the classroom issues or select an available room.";
                return;
            }
        }

        try
        {
            if (currentStep == 1 && _draftChild == null)
            {
                _draftChild = await ChildService.CreateChildAsync(childFirstName, childLastName, childDob, null, childPhotoPreview, isOfficialAdmission: true);
                _draftChild.EnrollmentStatus = "Draft";
            }
            
            if (_draftChild != null)
            {
                _draftChild.FirstName = childFirstName;
                _draftChild.LastName = childLastName;
                _draftChild.DateOfBirth = childDob;
                _draftChild.PhotoUrl = childPhotoPreview;
                if (Guid.TryParse(selectedClassroomId, out var cid))
                {
                    _draftChild.ClassroomId = cid;
                }
                else
                {
                    _draftChild.ClassroomId = await SystemProvisioning.EnsureWaitingPoolAsync();
                }
                _draftChild.ProfileCompletionPercentage = currentStep * 10;
                if (currentStep == 10) _draftChild.EnrollmentStatus = "Enrolled";
                
                var draftState = new
                {
                    parentName, parentRelationship, parentEmail, parentPhone, parentPhotoPreview,
                    addressLine1, addressCity, addressProvince, addressPostalCode,
                    emergencyName, emergencyPhone, emergencyRelationship,
                    physicianName, physicianPhone, allergies, medications,
                    dietaryRestrictions, napRoutine, toiletTraining,
                    enrollmentStartDate, scheduleType, selectedBranchId, selectedClassroomId,
                    pickupName, pickupRelationship, pickupCode,
                    consentPhoto, consentMedical, consentMarketing
                };
                
                _draftChild.DraftData = System.Text.Json.JsonSerializer.Serialize(draftState);
                
                await ChildService.UpdateChildAsync(_draftChild);

                if (!string.IsNullOrWhiteSpace(parentEmail))
                {
                    await ChildService.SyncParentGuardianAsync(_draftChild.Id, parentEmail, parentName, parentRelationship, parentPhone);
                }
            }

            if (currentStep == 8 && !string.IsNullOrWhiteSpace(pickupName))
            {
                await SaveCurrentPickupContactAsync();
            }

            if (currentStep < 10) 
            {
                currentStep++;
                if (currentStep > maxAllowedStep) maxAllowedStep = currentStep;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _stepErrorMessage = $"Permission Denied: {ex.Message}";
            await NotificationService.ShowWarningAsync($"Permission Denied: {ex.Message}");
        }
        catch (ClassroomReadinessException ex)
        {
            _stepErrorMessage = $"Classroom readiness check failed: {ex.Message}";
            await NotificationService.ShowWarningAsync(ex.Message);
        }
        catch (PlanQuotaExceededException ex)
        {
            _stepErrorMessage = ex.Message;
            await NotificationService.ShowWarningAsync(ex.Message);
        }
        catch (Exception ex)
        {
            _stepErrorMessage = $"Unable to save progress: {ex.Message}";
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private void PreviousStep()
    {
        if (currentStep > 1) currentStep--;
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/children");
    }

    private async Task HandleChildPhotoSelected(InputFileChangeEventArgs e)
    {
        try
        {
            var format = "image/avif";
            var resizedImage = await e.File.RequestImageFileAsync(format, 200, 200);
            var buffer = new byte[resizedImage.Size];
            await resizedImage.OpenReadStream().ReadAsync(buffer);
            childPhotoPreview = $"data:{format};base64,{Convert.ToBase64String(buffer)}";
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task HandleParentPhotoSelected(InputFileChangeEventArgs e)
    {
        try
        {
            var format = "image/avif";
            var resizedImage = await e.File.RequestImageFileAsync(format, 200, 200);
            var buffer = new byte[resizedImage.Size];
            await resizedImage.OpenReadStream().ReadAsync(buffer);
            parentPhotoPreview = $"data:{format};base64,{Convert.ToBase64String(buffer)}";
        }
        catch (Exception ex)
        {
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task CompleteEnrollment()
    {
        _stepErrorMessage = null;
        if (_draftChild != null)
        {
            try
            {
                _draftChild.ProfileCompletionPercentage = 100;
                _draftChild.EnrollmentStatus = "Enrolled";
                await ChildService.UpdateChildAsync(_draftChild);

                if (!string.IsNullOrWhiteSpace(parentEmail))
                {
                    await ChildService.SyncParentGuardianAsync(_draftChild.Id, parentEmail, parentName, parentRelationship, parentPhone);
                }

                await NotificationService.ShowSuccessAsync("Child enrollment completed successfully!");
                NavigationManager.NavigateTo("/children");
            }
            catch (ClassroomReadinessException ex)
            {
                _stepErrorMessage = $"Classroom readiness check failed: {ex.Message}";
                currentStep = 7;
                await NotificationService.ShowWarningAsync(ex.Message);
            }
            catch (PlanQuotaExceededException ex)
            {
                _stepErrorMessage = ex.Message;
                await NotificationService.ShowWarningAsync(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _stepErrorMessage = $"Permission Denied: {ex.Message}";
                await NotificationService.ShowWarningAsync($"Permission Denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                _stepErrorMessage = $"Unable to complete enrollment: {ex.Message}";
                await NotificationService.HandleExceptionAsync(ex);
            }
        }
        else
        {
            NavigationManager.NavigateTo("/children");
        }
    }

    private async Task LoadPickupsAsync()
    {
        var childId = Id ?? _draftChild?.Id;
        if (childId.HasValue && childId.Value != Guid.Empty)
        {
            try
            {
                var tenantId = _draftChild?.TenantId ?? TenantContext.TenantId;
                if (tenantId == Guid.Empty) tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
                var profile = await SecurePickupService.GetChildPickupSecurityProfileAsync(tenantId, childId.Value);
                if (profile != null)
                {
                    _authorizedPickupsList = profile.AuthorizedPickups.ToList();
                }
            }
            catch { }
        }
    }

    private async Task SaveCurrentPickupContactAsync()
    {
        var childId = Id ?? _draftChild?.Id;
        if (!childId.HasValue || string.IsNullOrWhiteSpace(pickupName)) return;

        try
        {
            var dto = new AuthorizedPickupDto
            {
                Id = Guid.NewGuid(),
                ChildId = childId.Value,
                FullName = pickupName.Trim(),
                Relationship = string.IsNullOrWhiteSpace(pickupRelationship) ? "Authorized Pickup" : pickupRelationship.Trim(),
                Phone = pickupPhone?.Trim() ?? "",
                PickupCode = pickupCode?.Trim() ?? "",
                IsActive = true,
                IsPrimaryGuardian = pickupIsPrimary || !_authorizedPickupsList.Any(p => p.IsPrimaryGuardian)
            };

            var tenantId = _draftChild?.TenantId ?? TenantContext.TenantId;
            if (tenantId == Guid.Empty) tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

            await SecurePickupService.SaveAuthorizedPickupAsync(tenantId, childId.Value, dto);
            await LoadPickupsAsync();
            pickupName = "";
            pickupRelationship = "";
            pickupPhone = "";
            pickupCode = "";
            pickupIsPrimary = false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _stepErrorMessage = $"Permission Denied: {ex.Message}";
            await NotificationService.ShowWarningAsync($"Permission Denied: {ex.Message}");
        }
        catch (Exception ex)
        {
            _stepErrorMessage = $"Unable to save pickup contact: {ex.Message}";
            await NotificationService.HandleExceptionAsync(ex);
        }
    }

    private async Task DeleteChild()
    {
        if (Id.HasValue)
        {
            bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this child? This action cannot be undone.");
            if (confirmed)
            {
                try
                {
                    await ChildService.DeleteChildAsync(Id.Value);
                    await NotificationService.ShowSuccessAsync("Child deleted successfully.");
                    NavigationManager.NavigateTo("/children");
                }
                catch (UnauthorizedAccessException ex)
                {
                    _stepErrorMessage = $"Permission Denied: {ex.Message}";
                    await NotificationService.ShowWarningAsync($"Permission Denied: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _stepErrorMessage = $"Unable to delete child: {ex.Message}";
                    await NotificationService.HandleExceptionAsync(ex);
                }
            }
        }
    }

    private void CopyParentToEmergencyContact()
    {
        if (!string.IsNullOrWhiteSpace(parentName)) emergencyName = parentName;
        if (!string.IsNullOrWhiteSpace(parentRelationship)) emergencyRelationship = parentRelationship;
        if (!string.IsNullOrWhiteSpace(parentPhone)) emergencyPhone = parentPhone;
    }

    private void CopyParentToPickupContact()
    {
        if (!string.IsNullOrWhiteSpace(parentName)) pickupName = parentName;
        if (!string.IsNullOrWhiteSpace(parentRelationship)) pickupRelationship = parentRelationship;
        if (!string.IsNullOrWhiteSpace(parentPhone)) pickupPhone = parentPhone;
        pickupIsPrimary = true;
    }

    private static string ExtractAllergies(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
        var lower = notes.ToLowerInvariant();
        var knownAllergens = new[] { "dairy", "peanut", "peanuts", "nut", "nuts", "egg", "eggs", "gluten", "milk", "wheat", "soy", "fish", "shellfish", "sesame", "strawberries", "latex", "bee" };
        var matched = new List<string>();
        foreach (var allergen in knownAllergens)
        {
            if (lower.Contains(allergen))
            {
                matched.Add(char.ToUpperInvariant(allergen[0]) + allergen.Substring(1));
            }
        }
        if (matched.Any()) return string.Join(", ", matched.Distinct());
        if (lower.Contains("allerg") || lower.Contains("asthma")) return notes.Trim();
        return string.Empty;
    }
}
