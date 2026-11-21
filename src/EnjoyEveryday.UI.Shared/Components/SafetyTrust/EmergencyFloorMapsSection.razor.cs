using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IO;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace EnjoyEveryday.UI.Shared.Components.SafetyTrust;

public partial class EmergencyFloorMapsSection
{
    private bool _loading = true;
    private bool _showWorkflowInfo = false;
    private List<BranchFloorMap> _maps = new();
    private List<BranchFloorMap> _filteredMaps = new();
    private List<Branch> _branches = new();

    private string _selectedBranchFilter = "";
    private string _selectedFloorFilter = "";
    private string _selectedStatusFilter = "";

    private string? _errorMessage;
    private string? _successMessage;

    // Editor modal state
    private bool _showEditorModal = false;
    private bool _isCreatingNew = false;
    private BranchFloorMap _editingMap = new();
    private List<FloorMapMarker> _editingMarkers = new();
    private string _selectedMarkerTool = SafetyMarkerType.EmergencyExit;
    private IBrowserFile? _uploadedFile;
    private string? _previewImageSource;
    private bool _selectedFileIsPdf;

    // Viewer modal state
    private bool _showViewerModal = false;
    private BranchFloorMap? _viewingMap;

    private readonly List<string> _availableMarkerTypes = new()
    {
        SafetyMarkerType.EmergencyExit,
        SafetyMarkerType.FirstAid,
        SafetyMarkerType.FireExtinguisher,
        SafetyMarkerType.FireAlarm,
        SafetyMarkerType.AssemblyPoint,
        SafetyMarkerType.AED,
        SafetyMarkerType.EvacuationRoute,
        SafetyMarkerType.AccessibleRoute,
        SafetyMarkerType.EmergencyEquipment,
        SafetyMarkerType.Other
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;
        try
        {
            var orgs = (await OrganizationService.GetOrganizationsAsync()).ToList();
            var primaryOrg = (UserContext.OrganizationId.HasValue
                ? orgs.FirstOrDefault(o => o.Id == UserContext.OrganizationId.Value)
                : null) ?? orgs.FirstOrDefault();

            if (primaryOrg != null)
            {
                _branches = (await BranchService.GetBranchesAsync(primaryOrg.Id)).ToList();
            }

            _maps = (await FloorMapService.GetMapsAsync()).ToList();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load floor maps: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplyFilters()
    {
        _filteredMaps = _maps.Where(m =>
            (string.IsNullOrEmpty(_selectedBranchFilter) || m.BranchId.ToString() == _selectedBranchFilter) &&
            (string.IsNullOrEmpty(_selectedFloorFilter) || m.FloorLevel.Equals(_selectedFloorFilter, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(_selectedStatusFilter) || m.Status.Equals(_selectedStatusFilter, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    private void OnFilterChanged()
    {
        ApplyFilters();
    }

    private void ToggleWorkflowInfo()
    {
        _showWorkflowInfo = !_showWorkflowInfo;
    }

    private void OpenCreateModal()
    {
        _isCreatingNew = true;
        _uploadedFile = null;
        _previewImageSource = null;
        _selectedFileIsPdf = false;
        _editingMarkers.Clear();
        _editingMap = new BranchFloorMap
        {
            BranchId = _branches.FirstOrDefault()?.Id ?? Guid.Empty,
            FloorLevel = "Floor 1",
            Name = "Ground Floor Evacuation Plan",
            ReviewDueAt = DateTime.UtcNow.AddMonths(12)
        };
        _showEditorModal = true;
    }

    private void OpenEditorModal(BranchFloorMap map)
    {
        _isCreatingNew = false;
        _uploadedFile = null;
        _previewImageSource = null;
        _selectedFileIsPdf = map.FileType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
        _editingMap = new BranchFloorMap
        {
            Id = map.Id,
            TenantId = map.TenantId,
            OrganizationId = map.OrganizationId,
            BranchId = map.BranchId,
            Name = map.Name,
            BuildingArea = map.BuildingArea,
            FloorLevel = map.FloorLevel,
            Description = map.Description,
            FileStorageKey = map.FileStorageKey,
            FileType = map.FileType,
            FileSize = map.FileSize,
            ContentHash = map.ContentHash,
            Version = map.Version,
            Status = map.Status,
            ReviewDueAt = map.ReviewDueAt,
            EmergencyInstructions = map.EmergencyInstructions
        };

        _editingMarkers = map.Markers.Select(m => new FloorMapMarker
        {
            Id = m.Id,
            FloorMapId = m.FloorMapId,
            MarkerType = m.MarkerType,
            Label = m.Label,
            Instructions = m.Instructions,
            NormalizedX = m.NormalizedX,
            NormalizedY = m.NormalizedY,
            DisplayOrder = m.DisplayOrder
        }).ToList();

        _showEditorModal = true;
    }

    private void CloseEditorModal()
    {
        _showEditorModal = false;
    }

    private void OpenViewerModal(BranchFloorMap map)
    {
        _viewingMap = map;
        _showViewerModal = true;
    }

    private void CloseViewerModal()
    {
        _showViewerModal = false;
        _viewingMap = null;
    }

    private void SelectMarkerTool(string type)
    {
        _selectedMarkerTool = type;
    }

    private async Task OnMapFileSelected(InputFileChangeEventArgs e)
    {
        _uploadedFile = e.File;
        _selectedFileIsPdf = _uploadedFile.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        if (!_selectedFileIsPdf)
        {
            try
            {
                using var stream = _uploadedFile.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                _previewImageSource = $"data:{_uploadedFile.ContentType};base64,{base64}";
            }
            catch (Exception ex)
            {
                _errorMessage = $"Error previewing image: {ex.Message}";
            }
        }
        else
        {
            _previewImageSource = null;
        }
    }

    private void OnReviewDateChanged(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out var dt))
        {
            _editingMap.ReviewDueAt = dt;
        }
    }

    private void OnCanvasClick(MouseEventArgs e)
    {
        // Coordinate calculation: mouse relative to container width & height
        // In Blazor Server, we can calculate normalized (0.0 to 1.0) using client coordinates or approx layout
        // For reliable precision, default to normalized values or calculated relative offset
        var normX = Math.Clamp(e.OffsetX / 800.0, 0.05, 0.95);
        var normY = Math.Clamp(e.OffsetY / 500.0, 0.05, 0.95);

        var newMarker = new FloorMapMarker
        {
            Id = Guid.NewGuid(),
            MarkerType = _selectedMarkerTool,
            Label = $"{FormatMarkerTypeName(_selectedMarkerTool)} #{_editingMarkers.Count + 1}",
            Instructions = GetDefaultInstructions(_selectedMarkerTool),
            NormalizedX = Math.Round(normX, 3),
            NormalizedY = Math.Round(normY, 3),
            DisplayOrder = _editingMarkers.Count + 1
        };

        _editingMarkers.Add(newMarker);
    }

    private void SelectActiveMarker(FloorMapMarker marker)
    {
        _selectedMarkerTool = marker.MarkerType;
    }

    private void RemoveMarker(FloorMapMarker marker)
    {
        _editingMarkers.Remove(marker);
    }

    private async Task SaveMapDraft()
    {
        try
        {
            _errorMessage = null;

            if (_isCreatingNew)
            {
                if (_uploadedFile == null)
                {
                    _errorMessage = "Please choose a floor plan file (PDF, PNG, JPG).";
                    return;
                }

                using var stream = _uploadedFile.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
                var request = new CreateFloorMapRequest
                {
                    BranchId = _editingMap.BranchId,
                    Name = _editingMap.Name,
                    BuildingArea = _editingMap.BuildingArea,
                    FloorLevel = _editingMap.FloorLevel,
                    Description = _editingMap.Description,
                    EmergencyInstructions = _editingMap.EmergencyInstructions,
                    ReviewDueAt = _editingMap.ReviewDueAt,
                    FileStream = stream,
                    FileName = _uploadedFile.Name,
                    FileContentType = _uploadedFile.ContentType
                };

                var created = await FloorMapService.CreateDraftMapAsync(request);

                if (_editingMarkers.Count > 0)
                {
                    await FloorMapService.SaveMarkersAsync(created.Id, _editingMarkers);
                }

                _successMessage = $"Draft floor map '{created.Name}' created successfully.";
            }
            else
            {
                Stream? stream = null;
                if (_uploadedFile != null)
                {
                    stream = _uploadedFile.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
                }

                var updateReq = new UpdateFloorMapRequest
                {
                    Id = _editingMap.Id,
                    Name = _editingMap.Name,
                    BuildingArea = _editingMap.BuildingArea,
                    FloorLevel = _editingMap.FloorLevel,
                    Description = _editingMap.Description,
                    EmergencyInstructions = _editingMap.EmergencyInstructions,
                    ReviewDueAt = _editingMap.ReviewDueAt,
                    NewFileStream = stream,
                    NewFileName = _uploadedFile?.Name,
                    NewFileContentType = _uploadedFile?.ContentType,
                    Markers = _editingMarkers
                };

                await FloorMapService.UpdateDraftMapAsync(updateReq);
                _successMessage = "Draft floor map and markers updated.";
            }

            CloseEditorModal();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Save failed: {ex.Message}";
        }
    }

    private async Task SaveAndSubmitReview()
    {
        await SaveMapDraft();
        var map = _maps.FirstOrDefault(m => m.Name == _editingMap.Name);
        if (map != null && map.Status == FloorMapStatus.Draft)
        {
            await SubmitForReview(map);
        }
    }

    private async Task SubmitForReview(BranchFloorMap map)
    {
        try
        {
            _errorMessage = null;
            await FloorMapService.SubmitForReviewAsync(map.Id);
            _successMessage = $"Floor map '{map.Name}' submitted for safety review.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to submit for review: {ex.Message}";
        }
    }

    private async Task PublishMap(BranchFloorMap map)
    {
        try
        {
            _errorMessage = null;
            await FloorMapService.PublishMapAsync(map.Id);
            _successMessage = $"Floor map '{map.Name}' officially published. Prior version (if any) superseded.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Publication failed: {ex.Message}";
        }
    }

    private async Task CreateNewDraftRevision(BranchFloorMap map)
    {
        try
        {
            _errorMessage = null;
            var newDraft = await FloorMapService.CreateNewDraftFromPublishedAsync(map.Id);
            _successMessage = $"Created draft revision v{newDraft.Version}.0 from published map.";
            await LoadDataAsync();
            OpenEditorModal(newDraft);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create revision: {ex.Message}";
        }
    }

    private async Task RetireMap(BranchFloorMap map)
    {
        try
        {
            _errorMessage = null;
            await FloorMapService.RetireMapAsync(map.Id);
            _successMessage = $"Floor map '{map.Name}' retired.";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Retire failed: {ex.Message}";
        }
    }

    private async Task TriggerBrowserPrint()
    {
        await JSRuntime.InvokeVoidAsync("window.print");
    }

    private static string FormatStatus(string status) => status switch
    {
        FloorMapStatus.Published => "Published Active",
        FloorMapStatus.Draft => "Draft",
        FloorMapStatus.PendingReview => "Pending Review",
        FloorMapStatus.Superseded => "Superseded",
        FloorMapStatus.Retired => "Retired",
        _ => status
    };

    private static string FormatMarkerTypeName(string type) => type switch
    {
        SafetyMarkerType.EmergencyExit => "Emergency Exit",
        SafetyMarkerType.FirstAid => "First Aid Station",
        SafetyMarkerType.FireExtinguisher => "Fire Extinguisher",
        SafetyMarkerType.FireAlarm => "Fire Alarm Pull",
        SafetyMarkerType.AssemblyPoint => "Assembly Point",
        SafetyMarkerType.AED => "AED / Defibrillator",
        SafetyMarkerType.EvacuationRoute => "Evacuation Route",
        SafetyMarkerType.AccessibleRoute => "Accessible Route",
        SafetyMarkerType.EmergencyEquipment => "Emergency Equipment",
        _ => "Other Safety Location"
    };

    private static string GetDefaultInstructions(string type) => type switch
    {
        SafetyMarkerType.EmergencyExit => "Primary evacuation exit — maintain clear corridor.",
        SafetyMarkerType.FirstAid => "Wall-mounted standard pediatric kit and eye-wash station.",
        SafetyMarkerType.FireExtinguisher => "Class ABC multi-purpose extinguisher; inspect monthly.",
        SafetyMarkerType.FireAlarm => "Manual pull station; activates building-wide strobe & siren.",
        SafetyMarkerType.AssemblyPoint => "Primary muster area in courtyard; report to Floor Marshal.",
        SafetyMarkerType.AED => "Automated External Defibrillator with pediatric pads.",
        SafetyMarkerType.EvacuationRoute => "Follow illuminated exit signage to exterior.",
        _ => "Safety marker position."
    };
}
