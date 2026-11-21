using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.UI.Shared.Components;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

public partial class ExperienceStudio
{
    [Parameter]
    public Guid? ExperienceId { get; set; }

    private bool _isLoading = true;
    private Experience _experience = new();
    private ExperienceDna _dna = new();
    private List<ExperienceVersion> _versions = new();
    private Guid _currentUserId;
    private string? _saveMessage;
    private string? _validationMessage;
    private bool _isImproving = false;
    private ExperienceImprovementResult? _pendingImprovement;

    private string _activeStudioTab = "all";

    private IEnumerable<FilterTabs.TabItem> StudioTabs => new List<FilterTabs.TabItem>
    {
        new() { Text = "All Views", Value = "all", Count = null },
        new() { Text = "DNA Canvas", Value = "canvas", Count = 7 },
        new() { Text = "Activity Timeline", Value = "timeline", Count = _dna.ExecutionGuide?.Phases?.Count ?? 0 },
        new() { Text = "AI Idea Explorer", Value = "ai", Count = _aiSuggestions.Count },
        new() { Text = "Details & Materials", Value = "details", Count = _dna.Materials?.Count ?? 0 }
    };

    private void SetStudioTab(string tab)
    {
        _activeStudioTab = tab;
    }

    private string _ideaInput = string.Empty;
    private string _selectedFocusArea = string.Empty;
    private string _selectedAgeTier = "Preschool (3-5y)";
    private string _selectedEnvironmentSetting = "Indoor";
    private string _materialsOnHandInput = string.Empty;
    private bool _showAdvancedGrounding = false;
    private bool _isGeneratingIdeas = false;
    private bool _isSuggestingMaterials = false;
    private List<ExperienceFullData> _aiSuggestions = new();
    private string _newMaterial = string.Empty;
    private bool _showStudioStatusLog = true;
    private bool _isDiagnosticsExpanded = true;
    private List<string> _diagnosticsLogs = new();
    private string _diagnosticsBadge = "Ready";
    private string _diagnosticsStatusType = "success";

    private void SelectFocusChip(string focusArea, string promptSnippet)
    {
        if (_selectedFocusArea == focusArea)
        {
            _selectedFocusArea = string.Empty;
        }
        else
        {
            _selectedFocusArea = focusArea;
            if (string.IsNullOrWhiteSpace(_ideaInput))
            {
                _ideaInput = promptSnippet;
            }
            else if (!_ideaInput.Contains(promptSnippet, StringComparison.OrdinalIgnoreCase))
            {
                _ideaInput = $"{_ideaInput.Trim()} ({promptSnippet})";
            }
        }
    }

    private async Task GenerateStorybookInteractiveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_experience.Title) || _experience.Title == "New Experience")
            {
                await ToastService.ShowErrorToastAsync("Please provide an experience title first.");
                return;
            }

            // Ensure draft is saved so experience has an ID
            if (_experience.Id == Guid.Empty)
            {
                await SaveDraftAsync();
            }

            var generated = StorybookGenerationService.GenerateStorybook(_experience, _dna);
            if (generated.Validation.SafetyFlags.Count > 0)
            {
                foreach (var flag in generated.Validation.SafetyFlags)
                {
                    await ToastService.ShowWarningToastAsync(flag);
                }
            }

            await ToastService.ShowSuccessToastAsync($"Generated '{generated.Title}' ({generated.ExperienceType}) Storybook with {generated.Chapters.Count} chapters!");

            // Open the rich Storybook Viewer
            NavigationManager.NavigateTo($"/experiences/{_experience.Id}/story");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorToastAsync($"Could not generate storybook: {ex.Message}");
        }
    }

    private async Task CopyGeminiStorybookPromptAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_experience.Title) || _experience.Title == "New Experience")
            {
                await ToastService.ShowErrorToastAsync("Please provide an experience title first.");
                return;
            }

            var prompt = StorybookGenerationService.BuildGeminiStorybookPrompt(_experience, _dna);
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", prompt);
            await ToastService.ShowSuccessToastAsync("✨ Gemini Storybook Prompt copied! Paste into gemini.google.com");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorToastAsync($"Error copying prompt: {ex.Message}");
        }
    }

    private async Task GenerateIdeasAsync()
    {
        if (string.IsNullOrWhiteSpace(_ideaInput)) return;
        _isGeneratingIdeas = true;
        _diagnosticsLogs.Clear();
        _diagnosticsBadge = "Connecting to Gemini...";
        _diagnosticsStatusType = "connecting";
        _isDiagnosticsExpanded = true;
        StateHasChanged();

        try
        {
            var availableMats = string.IsNullOrWhiteSpace(_materialsOnHandInput)
                ? new List<string>()
                : _materialsOnHandInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            List<TopExperienceSummaryDto> topExperiences = new();
            try
            {
                var existingExps = await ExperienceService.GetExperiencesAsync();
                topExperiences = existingExps
                    .Where(e => (e.Status == "Active" || e.Status == "Published") && e.Id != _experience.Id)
                    .Take(3)
                    .Select(e => new TopExperienceSummaryDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description
                    }).ToList();
            }
            catch
            {
                // Non-blocking fallback if query fails
            }

            var options = new IdeaGenerationOptions
            {
                AgeGroup = _selectedAgeTier,
                FocusArea = !string.IsNullOrWhiteSpace(_selectedFocusArea) ? _selectedFocusArea : null,
                Environment = _selectedEnvironmentSetting,
                AvailableMaterials = availableMats,
                TopExperiences = topExperiences
            };

            var genResult = await AiIdeaService.GenerateIdeasWithDiagnosticsAsync(_ideaInput, options, logLine =>
            {
                _diagnosticsLogs.Add(logLine);
                InvokeAsync(StateHasChanged);
            });

            _aiSuggestions = genResult.Suggestions.ToList();
            _diagnosticsLogs = genResult.Diagnostics.Logs;
            _diagnosticsBadge = genResult.Diagnostics.StatusBadge;
            _diagnosticsStatusType = genResult.Diagnostics.StatusType;
        }
        catch (Exception ex)
        {
            _diagnosticsBadge = "AI Generation Error";
            _diagnosticsStatusType = "error";
            _diagnosticsLogs.Add($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            await ToastService.ShowErrorToastAsync("Failed to generate ideas.");
        }
        finally
        {
            _isGeneratingIdeas = false;
            StateHasChanged();
        }
    }

    private string GetDiagnosticsAccentColor() => _diagnosticsStatusType switch
    {
        "connecting" => "#d97706",
        "success" => "#31523c",
        "fallback" => "#b45309",
        "error" => "#9b2c2c",
        _ => "#557c6c"
    };

    private async Task ApplySuggestion(ExperienceFullData suggestion)
    {
        suggestion.ApplyTo(_experience, _dna);
        StateHasChanged();
        await SaveDraftAsync();
    }

    private void AddMaterial()
    {
        if (!string.IsNullOrWhiteSpace(_newMaterial) && !_dna.Materials.Contains(_newMaterial))
        {
            _dna.Materials.Add(_newMaterial.Trim());
            _newMaterial = string.Empty;
            _ = SaveDraftAsync();
        }
    }

    private void RemoveMaterial(string material)
    {
        _dna.Materials.Remove(material);
        _ = SaveDraftAsync();
    }

    private async Task SuggestMaterialsAsync()
    {
        _isSuggestingMaterials = true;
        StateHasChanged();
        try
        {
            var payload = new
            {
                Challenge = _dna.Challenge,
                Together = _dna.Together,
                Create = _dna.Create,
                ChildChoice = _dna.ChildChoice,
                TeacherGuidance = _dna.TeacherGuidance,
                ExecutionGuide = _dna.ExecutionGuide
            };
            var suggested = await AiIdeaService.SuggestMaterialsAsync(payload);
            bool changed = false;
            foreach (var item in suggested)
            {
                if (!_dna.Materials.Any(m => m.Equals(item, StringComparison.OrdinalIgnoreCase)))
                {
                    _dna.Materials.Add(item);
                    changed = true;
                }
            }
            if (changed)
            {
                _ = SaveDraftAsync();
            }
        }
        catch (Exception)
        {
            await ToastService.ShowErrorToastAsync("Failed to suggest materials.");
        }
        finally
        {
            _isSuggestingMaterials = false;
            StateHasChanged();
        }
    }

    private void AddGoal()
    {
        if (!string.IsNullOrWhiteSpace(_newGoal) && !_dna.DevelopmentalGoals.Contains(_newGoal))
        {
            _dna.DevelopmentalGoals.Add(_newGoal.Trim());
            _newGoal = string.Empty;
            _ = SaveDraftAsync();
        }
    }

    private void RemoveGoal(string goal)
    {
        _dna.DevelopmentalGoals.Remove(goal);
        _ = SaveDraftAsync();
    }

    private void AddPhase()
    {
        _dna.ExecutionGuide.Phases.Add(new ExecutionPhase { Name = "New Phase", DurationMinutes = 5 });
        _ = SaveDraftAsync();
    }

    private ExecutionPhase? _draggedPhase;

    private void HandleDragStart(ExecutionPhase phase)
    {
        _draggedPhase = phase;
    }

    private void HandleDragEnter(DragEventArgs e, ExecutionPhase targetPhase)
    {
        if (_draggedPhase == null || _draggedPhase == targetPhase)
            return;

        int oldIndex = _dna.ExecutionGuide.Phases.IndexOf(_draggedPhase);
        int newIndex = _dna.ExecutionGuide.Phases.IndexOf(targetPhase);
        if (oldIndex >= 0 && newIndex >= 0)
        {
            _dna.ExecutionGuide.Phases.RemoveAt(oldIndex);
            _dna.ExecutionGuide.Phases.Insert(newIndex, _draggedPhase);
        }
    }

    private async Task HandleDragEnd()
    {
        _draggedPhase = null;
        await SaveDraftAsync();
    }

    private void RemovePhase(ExecutionPhase phase)
    {
        _dna.ExecutionGuide.Phases.Remove(phase);
        _ = SaveDraftAsync();
    }

    private bool _isPreviewOpen = false;
    private string _customImprovePrompt = string.Empty;
    private string _newGoal = string.Empty;

    private void OpenPreview()
    {
        _isPreviewOpen = true;
    }

    private void ClosePreview()
    {
        _isPreviewOpen = false;
    }

    private async Task PrintExperience()
    {
        var js = @"
            var content = document.querySelector('.modal').innerHTML;
            var printWindow = window.open('', '', 'height=800,width=800');
            printWindow.document.write('<html><head><title>Print Experience</title>');
            printWindow.document.write('<style>body { font-family: system-ui, -apple-system, sans-serif; padding: 40px; line-height: 1.6; color: #333; } h2, h3 { color: #111; } .eyebrow { font-size: 12px; font-weight: bold; color: #666; text-transform: uppercase; margin-bottom: 10px; } .detail { margin-top: 20px; margin-bottom: 20px; } .detail label { font-weight: bold; font-size: 14px; color: #555; display: block; margin-bottom: 5px; } .modal-meta { font-weight: bold; color: #444; margin-bottom: 20px; } .modal-footer, .modal-close, button { display: none !important; } ul { padding-left: 20px; } hr { border: none; border-top: 1px dashed #ccc; margin: 30px 0; } </style>');
            printWindow.document.write('</head><body>');
            printWindow.document.write(content);
            printWindow.document.write('</body></html>');
            printWindow.document.close();
            printWindow.focus();
            setTimeout(() => { printWindow.print(); printWindow.close(); }, 500);
        ";
        await JS.InvokeVoidAsync("eval", js);
    }

    // History & Compare
    private string _orgName = "EnjoyEveryday";
    private bool _hideHistoryDialog = true;

    private async Task OpenHistoryDialog()
    {
        var vlist = await ExperienceService.GetExperienceHistoryAsync(_experience!.Id);
        _versions = vlist.ToList();
        _hideHistoryDialog = false;
    }

    private void CloseHistoryDialog()
    {
        _hideHistoryDialog = true;
    }

    protected override async Task OnInitializedAsync()
    {
        _showStudioStatusLog = Configuration.GetValue<bool>("AiAgent:ShowStudioStatusLog", true);
        var profile = await PageAuth.GetUserProfileAsync();
        if (!string.IsNullOrWhiteSpace(profile.OrganizationName))
        {
            _orgName = profile.OrganizationName;
        }
        if (!profile.IsOrgAdmin && !profile.IsBranchAdmin && !UserContext.HasPermission(EnjoyEveryday.Shared.Authorization.Permissions.ExperienceCreate))
        {
            NavigationManager.NavigateTo("/experiences");
            return;
        }

        _currentUserId = UserContext.UserId;
        if (_currentUserId == Guid.Empty)
        {
            var teachers = await UserService.GetTeachersAsync();
            _currentUserId = teachers.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (ExperienceId.HasValue)
        {
            var exp = await ExperienceService.GetExperienceByIdAsync(ExperienceId.Value);
            if (exp != null)
            {
                _experience = exp;
                if (!string.IsNullOrEmpty(_experience.DnaPayload) && _experience.DnaPayload != "{}")
                {
                    try
                    {
                        _dna = JsonSerializer.Deserialize<ExperienceDna>(_experience.DnaPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ExperienceDna();
                    }
                    catch
                    {
                        _dna = new ExperienceDna();
                    }
                }
                var vlist = await ExperienceService.GetExperienceHistoryAsync(ExperienceId.Value);
                _versions = vlist.OrderByDescending(v => v.VersionNumber).ToList();
            }
        }
        else
        {
            _experience = new Experience
            {
                Title = "New Experience",
                Status = "Draft",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dna = new ExperienceDna
            {
                Materials = new List<string> { "Blocks", "Paper", "Cups", "Water tray" }
            };
        }

        _isLoading = false;
    }

    private async Task SaveDraftAsync()
    {
        try
        {
            if (_currentUserId == Guid.Empty)
            {
                _currentUserId = UserContext.UserId;
            }

            var dnaJson = JsonSerializer.Serialize(_dna);

            if (_experience.Id == Guid.Empty)
            {
                _experience = await ExperienceService.CreateExperienceAsync(
                    _experience.Title,
                    _experience.Description,
                    dnaJson,
                    _currentUserId,
                    "Draft"
                );
                NavigationManager.NavigateTo($"/studio/{_experience.Id}", replace: true);
            }
            else
            {
                await ExperienceService.UpdateExperienceAsync(
                    _experience.Id,
                    _experience.Title,
                    _experience.Description,
                    dnaJson,
                    _experience.Status,
                    "Draft Saved",
                    _currentUserId
                );
                if (string.Equals(_experience.Status, "Published", StringComparison.OrdinalIgnoreCase))
                {
                    await StorybookStorageService.StoreStorybookAsync(_experience);
                }
            }

            _saveMessage = "Draft saved";
            _validationMessage = null;
            StateHasChanged();

            await Task.Delay(3000);
            _saveMessage = null;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _validationMessage = $"Unable to save: {ex.Message}";
            _saveMessage = null;
            StateHasChanged();
        }
    }

    private async Task PublishAsync()
    {
        try
        {
            _validationMessage = null;
            if (string.IsNullOrWhiteSpace(_experience.Title) || _experience.Title == "New Experience")
            {
                _validationMessage = "Title is required to publish.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_dna.Challenge))
            {
                _validationMessage = "Challenge is required to publish.";
                return;
            }

            if (_currentUserId == Guid.Empty)
            {
                _currentUserId = UserContext.UserId;
            }

            var dnaJson = JsonSerializer.Serialize(_dna);
            var publishResult = await ExperienceService.PublishExperienceAsync(
                _experience.Id,
                _experience.Title,
                _experience.Description,
                dnaJson,
                _currentUserId);

            _experience = publishResult.Experience;
            _versions = publishResult.Versions.ToList();

            await StorybookStorageService.StoreStorybookAsync(_experience);

            _saveMessage = "Experience published!";
            StateHasChanged();

            await Task.Delay(3000);
            _saveMessage = null;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _validationMessage = $"Unable to publish: {ex.Message}";
            _experience.Status = "Draft";
            StateHasChanged();
        }
    }

    private void PlanExperience()
    {
        NavigationManager.NavigateTo($"/planning");
    }

    private async Task ImproveAsync(string requestContext)
    {
        _isImproving = true;
        _diagnosticsLogs.Clear();
        _diagnosticsBadge = "Enhancing with Gemini...";
        _diagnosticsStatusType = "connecting";
        _isDiagnosticsExpanded = true;
        StateHasChanged();

        try
        {
            var payload = new
            {
                Title = _experience.Title,
                Description = _experience.Description,
                Challenge = _dna.Challenge,
                Together = _dna.Together,
                Create = _dna.Create,
                ChildChoice = _dna.ChildChoice,
                MagicMoment = _dna.MagicMoment,
                Accomplishment = _dna.Accomplishment,
                Reflection = _dna.Reflection,
                TeacherGuidance = _dna.TeacherGuidance,
                AgeMin = _dna.AgeMin,
                AgeMax = _dna.AgeMax,
                GroupSizeMin = _dna.GroupSizeMin,
                GroupSizeMax = _dna.GroupSizeMax,
                Environment = _dna.Environment,
                DevelopmentalGoals = _dna.DevelopmentalGoals,
                Materials = _dna.Materials,
                ExecutionGuide = _dna.ExecutionGuide
            };
            var result = await AiIdeaService.ImproveExperienceAsync(payload, requestContext, logLine =>
            {
                _diagnosticsLogs.Add(logLine);
                InvokeAsync(StateHasChanged);
            });
            if (result != null)
            {
                _pendingImprovement = result;
                _diagnosticsLogs = result.Diagnostics.Logs;
                _diagnosticsBadge = result.Diagnostics.StatusBadge;
                _diagnosticsStatusType = result.Diagnostics.StatusType;
            }
            else
            {
                _diagnosticsBadge = "Improvement Failed";
                _diagnosticsStatusType = "error";
                await ToastService.ShowErrorToastAsync("Failed to get improvement suggestions. Please check your connection and API key.");
            }
        }
        catch (Exception ex)
        {
            _diagnosticsBadge = "Improvement Error";
            _diagnosticsStatusType = "error";
            _diagnosticsLogs.Add($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            await ToastService.ShowErrorToastAsync($"An error occurred while connecting to the AI: {ex.Message}");
        }
        finally
        {
            _isImproving = false;
            StateHasChanged();
        }
    }

    private void RejectImprovement()
    {
        _pendingImprovement = null;
    }

    private async Task AcceptImprovement()
    {
        if (_pendingImprovement != null)
        {
            _pendingImprovement.UpdatedData.ApplyTo(_experience, _dna);

            await SaveDraftAsync();
            _pendingImprovement = null;
        }
    }

    private bool _isDeployModalOpen = false;
    private string _selectedDeployTier = "All";
    private DateOnly _deployDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    private string _deployTimeOfDay = "Morning";
    private bool _isDeploying = false;

    private void OpenDeployModal()
    {
        _selectedDeployTier = "All";
        _deployDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        _deployTimeOfDay = "Morning";
        _isDeployModalOpen = true;
    }

    private async Task DeployToClassroomsAsync()
    {
        _isDeploying = true;
        try
        {
            var orgId = UserContext.OrganizationId ?? Guid.Empty;
            var classrooms = (await ClassroomService.GetClassroomsByOrganizationAsync(orgId)).ToList();
            if (!classrooms.Any())
            {
                classrooms = (await ClassroomService.GetAllClassroomsAsync()).ToList();
            }

            var matching = classrooms.Where(c => 
                _selectedDeployTier == "All" || 
                (!string.IsNullOrEmpty(c.AgeGroup) && c.AgeGroup.Contains(_selectedDeployTier, StringComparison.OrdinalIgnoreCase))).ToList();

            if (!matching.Any())
            {
                await ToastService.ShowWarningToastAsync($"No classrooms found matching age tier '{_selectedDeployTier}'.");
                return;
            }

            int count = 0;
            foreach (var room in matching)
            {
                try
                {
                    await PlannerService.ScheduleExperienceAsync(_experience.Id, room.Id, _deployDate, _deployTimeOfDay);
                    count++;
                }
                catch { }
            }

            _isDeployModalOpen = false;
            await ToastService.ShowSuccessToastAsync($"✓ Successfully scheduled '{_experience.Title}' to {count} classroom(s) for {_deployDate:MMM dd} ({_deployTimeOfDay})!");
        }
        catch (Exception ex)
        {
            await ToastService.ShowErrorToastAsync($"Scheduling error: {ex.Message}");
        }
        finally
        {
            _isDeploying = false;
        }
    }
}
