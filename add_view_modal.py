import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add _isViewModalOpen and related methods to @code
code_addition = '''
    private bool _isViewModalOpen = false;

    private void OpenViewExperienceModal(Experience exp)
    {
        _currentExperience = exp;
        _isViewModalOpen = true;
    }

    private void CloseViewDialog()
    {
        _isViewModalOpen = false;
    }
'''
if '_isViewModalOpen' not in content:
    content = content.replace('private bool _isModalOpen = false;', 'private bool _isViewModalOpen = false;\n    private bool _isModalOpen = false;' + code_addition)

# 2. Change the onclick handler for the card
content = content.replace('@onclick="() => OpenEditExperienceModal(context)"', '@onclick="() => OpenViewExperienceModal(context)"')

# 3. Inject the view modal HTML right after the edit modal's closing bracket
view_modal_html = '''
@if (_isViewModalOpen)
{
    <div class="modal-backdrop" @onclick="CloseViewDialog" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 999;"></div>
    <dialog open class="modal" style="display: block; position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 1000; background: white; padding: 2.5rem; border-radius: 20px; box-shadow: 0 20px 40px rgba(0,0,0,0.1); border: none; width: 750px; max-width: 95vw;">
        
        <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.5rem;">
            <div>
                <div style="color: #557c6c; font-size: 10px; font-weight: 800; letter-spacing: 1.2px; text-transform: uppercase; margin-bottom: 8px;">EXPERIENCE</div>
                <h3 style="margin: 0; font-size: 2rem; font-weight: 800; color: #26332d; letter-spacing: -0.5px;">@_currentExperience.Title</h3>
            </div>
            <button style="background: transparent; border: none; font-size: 1.5rem; cursor: pointer; color: #7c877f; padding: 0;" @onclick="CloseViewDialog">✕</button>
        </div>

        <div style="display: flex; gap: 0.5rem; margin-bottom: 2rem; flex-wrap: wrap;">
            <span style="background: #f8f9f5; color: #7c877f; padding: 6px 12px; border-radius: 8px; font-size: 0.85rem; font-weight: 600;">Age 3–4</span>
            <span style="background: #f8f9f5; color: #7c877f; padding: 6px 12px; border-radius: 8px; font-size: 0.85rem; font-weight: 600;">25 min</span>
            <span style="background: #f8f9f5; color: #7c877f; padding: 6px 12px; border-radius: 8px; font-size: 0.85rem; font-weight: 600;">Indoor</span>
            <span style="background: #f8f9f5; color: #7c877f; padding: 6px 12px; border-radius: 8px; font-size: 0.85rem; font-weight: 600;">6–18 children</span>
        </div>

        <div style="margin-bottom: 1.5rem;">
            <div style="font-size: 0.75rem; font-weight: 800; color: #7c877f; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 1px;">Today's Mission</div>
            <div style="font-size: 1.05rem; color: #26332d;">@(_currentExperience.Description ?? "No description available.")</div>
        </div>

        <div style="margin-bottom: 1.5rem;">
            <div style="font-size: 0.75rem; font-weight: 800; color: #7c877f; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 1px;">Discover</div>
            <div style="font-size: 1.05rem; color: #26332d;">What does our city need?</div>
        </div>

        <div style="margin-bottom: 1.5rem;">
            <div style="font-size: 0.75rem; font-weight: 800; color: #7c877f; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 1px;">Challenge</div>
            <div style="font-size: 1.05rem; color: #26332d;">Can we build a bridge?</div>
        </div>

        <div style="margin-bottom: 2rem;">
            <div style="font-size: 0.75rem; font-weight: 800; color: #7c877f; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 1px;">Joy Moment</div>
            <div style="font-size: 1.05rem; color: #26332d;">The whole city comes together.</div>
        </div>

        <div style="border-top: 1px solid #e1e6df; padding-top: 1.5rem; display: flex; justify-content: flex-end; gap: 1rem;">
            <button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;">Schedule</button>
            <button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="() => { CloseViewDialog(); OpenEditExperienceModal(_currentExperience); }">Edit</button>
            <button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;">Duplicate</button>
            <button class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer; display: flex; align-items: center; gap: 6px;">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 0L14.59 9.41L24 12L14.59 14.59L12 24L9.41 14.59L0 12L9.41 9.41L12 0Z" fill="currentColor"/>
                </svg>
                Improve with AI
            </button>
        </div>
    </dialog>
}
'''

if 'class="modal-backdrop" @onclick="CloseViewDialog"' not in content:
    # Insert right before @code {
    idx = content.find('@code {')
    if idx != -1:
        content = content[:idx] + view_modal_html + '\n\n' + content[idx:]

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
