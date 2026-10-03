import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Inject PlannerService
if '@inject PlannerService PlannerService' not in content:
    content = content.replace('@inject UserService UserService', '@inject UserService UserService\n@inject PlannerService PlannerService')

# 2. Update SaveDraftAsync to use "Draft"
content = content.replace('_currentExperience.Status = "Idea";', '_currentExperience.Status = "Draft";')

# 3. Update SaveExperienceAsync to pass status
content = content.replace(
'''            await ExperienceService.CreateExperienceAsync(
                _currentExperience.Title,
                _currentExperience.Description ?? "",
                _currentExperience.DnaPayload ?? "{}",
                _defaultUserId);''',
'''            await ExperienceService.CreateExperienceAsync(
                _currentExperience.Title,
                _currentExperience.Description ?? "",
                _currentExperience.DnaPayload ?? "{}",
                _defaultUserId,
                _currentExperience.Status);'''
)

# 4. Update DuplicateExperience
old_duplicate = '''    private void DuplicateExperience()
    {
        var sourceExp = _currentExperience;
        CloseViewDialog();
        OpenAddExperienceModal();
        _currentExperience.Title = sourceExp.Title + " (Copy)";
        _currentExperience.Description = sourceExp.Description;
        _currentExperience.DnaPayload = sourceExp.DnaPayload;
    }'''

new_duplicate = '''    private async Task DuplicateExperience()
    {
        var sourceExp = _currentExperience;
        await ExperienceService.CreateExperienceAsync(
            sourceExp.Title + " (Copy)",
            sourceExp.Description ?? "",
            sourceExp.DnaPayload ?? "{}",
            _defaultUserId,
            "Idea"
        );
        await LoadExperiencesAsync();
        CloseViewDialog();
    }'''
content = content.replace(old_duplicate, new_duplicate)
content = content.replace('@onclick="DuplicateExperience"', '@onclick="DuplicateExperience"') # It should be an async handler now, which Blazor handles nicely without change

# 5. Disable Schedule button if Draft
content = content.replace(
'''<button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="ScheduleExperience">Schedule</button>''',
'''@if (_currentExperience.Status != "Draft")
            {
                <button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="ScheduleExperience">Schedule</button>
            }
            else
            {
                <button type="button" class="secondary" style="border: 1px solid #e1e6df; background: #f0f0f0; color: #a0a0a0; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: not-allowed;" disabled title="Cannot schedule a draft">Schedule</button>
            }'''
)

# 6. Schedule Modal UI & Logic
old_schedule = '''    private void ScheduleExperience()
    {
        CloseViewDialog();
        # Since we don't have a scheduling popup ready yet, we will just close for now.
    }'''
# Wait, my previous injected string used '//' not '#' for comments. Let's do a safe replace
old_schedule_block = '''    private void ScheduleExperience()
    {
        CloseViewDialog();
        // Since we don't have a scheduling popup ready yet, we will just close for now.
    }'''

new_schedule_logic = '''
    private bool _isScheduleModalOpen = false;
    private DateTime _scheduleDate = DateTime.Today;
    private string _scheduleTimeOfDay = "Morning";

    private void ScheduleExperience()
    {
        CloseViewDialog();
        _scheduleDate = DateTime.Today;
        _scheduleTimeOfDay = "Morning";
        _isScheduleModalOpen = true;
    }

    private void CloseScheduleModal()
    {
        _isScheduleModalOpen = false;
    }

    private async Task SaveScheduleAsync()
    {
        // Using a dummy classroom ID for now since we don't have classroom selection built in
        var classroomId = Guid.NewGuid(); 
        await PlannerService.ScheduleExperienceAsync(_currentExperience.Id, classroomId, DateOnly.FromDateTime(_scheduleDate), _scheduleTimeOfDay);
        _isScheduleModalOpen = false;
    }
'''
if 'private void ScheduleExperience()' in content:
    content = content.replace(old_schedule_block, new_schedule_logic)

schedule_modal_html = '''
@if (_isScheduleModalOpen)
{
    <div class="modal-backdrop" @onclick="CloseScheduleModal" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 999;"></div>
    <dialog open class="modal" style="display: block; position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 1000; background: white; padding: 2.5rem; border-radius: 20px; box-shadow: 0 20px 40px rgba(0,0,0,0.1); border: none; width: 500px; max-width: 95vw;">
        
        <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 2rem;">
            <div>
                <h3 style="margin: 0 0 8px 0; font-size: 1.5rem; font-weight: 800; color: #26332d; letter-spacing: -0.5px;">Schedule Experience</h3>
                <p style="margin: 0; color: #7c877f; font-size: 0.95rem;">When would you like to run @_currentExperience.Title?</p>
            </div>
            <button style="background: transparent; border: none; font-size: 1.5rem; cursor: pointer; color: #7c877f; padding: 0;" @onclick="CloseScheduleModal">✕</button>
        </div>

        <div class="field" style="margin-bottom: 1.5rem;">
            <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Date</label>
            <input type="date" @bind="_scheduleDate" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; outline: none;" />
        </div>

        <div class="field" style="margin-bottom: 2rem;">
            <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Time of Day</label>
            <select @bind="_scheduleTimeOfDay" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; background: white; outline: none; cursor: pointer;">
                <option value="Morning">Morning</option>
                <option value="Afternoon">Afternoon</option>
            </select>
        </div>

        <div style="border-top: 1px solid #e1e6df; padding-top: 1.5rem; display: flex; justify-content: flex-end; gap: 1rem;">
            <button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="CloseScheduleModal">Cancel</button>
            <button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveScheduleAsync">Add to Calendar</button>
        </div>
    </dialog>
}
'''

if 'private bool _isScheduleModalOpen = false;' in content and '<div class="modal-backdrop" @onclick="CloseScheduleModal"' not in content:
    idx = content.find('@code {')
    if idx != -1:
        content = content[:idx] + schedule_modal_html + '\n\n' + content[idx:]

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
