import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# We need to find the modal block and replace it.
# It starts at: @if (_isModalOpen)
# and ends at: } before @code

start_str = '@if (_isModalOpen)'
end_str = '}

@code {'

start_idx = content.find(start_str)
end_idx = content.find(end_str)

if start_idx != -1 and end_idx != -1:
    new_modal = '''@if (_isModalOpen)
{
    <div class="modal-backdrop" @onclick="CloseDialog" style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 999;"></div>
    <dialog open class="modal" style="display: block; position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); z-index: 1000; background: white; padding: 2.5rem; border-radius: 20px; box-shadow: 0 20px 40px rgba(0,0,0,0.1); border: none; width: 750px; max-width: 95vw;">
        
        <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 2rem;">
            <div>
                <div style="color: #557c6c; font-size: 10px; font-weight: 800; letter-spacing: 1.2px; text-transform: uppercase; margin-bottom: 8px;">@(_isEditing ? "EDIT" : "CREATE")</div>
                <h3 style="margin: 0 0 8px 0; font-size: 2rem; font-weight: 800; color: #26332d; letter-spacing: -0.5px;">@(_isEditing ? "Edit Experience" : "Create an Experience")</h3>
                <p style="margin: 0; color: #7c877f; font-size: 0.95rem;">Capture what you already know works with your children.</p>
            </div>
            <button style="background: transparent; border: none; font-size: 1.5rem; cursor: pointer; color: #7c877f; padding: 0;" @onclick="CloseDialog">✕</button>
        </div>

        <div class="field" style="margin-bottom: 1.5rem;">
            <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Experience Name</label>
            <input @bind="_currentExperience.Title" placeholder="Build a Rainy-Day City" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; outline: none;" />
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-bottom: 1.5rem;">
            <div class="field">
                <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Age</label>
                <select @bind="experienceAge" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; background: white; outline: none; cursor: pointer;">
                    <option value="3-4">3–4</option>
                    <option value="4-5">4–5</option>
                    <option value="5-6">5–6</option>
                </select>
            </div>
            <div class="field">
                <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Duration</label>
                <select @bind="experienceDuration" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; background: white; outline: none; cursor: pointer;">
                    <option value="15 minutes">15 minutes</option>
                    <option value="30 minutes">30 minutes</option>
                    <option value="45 minutes">45 minutes</option>
                </select>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-bottom: 1.5rem;">
            <div class="field">
                <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Type</label>
                <select @bind="experienceType" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; background: white; outline: none; cursor: pointer;">
                    <option value="Explore">Explore</option>
                    <option value="Create">Create</option>
                    <option value="Move">Move</option>
                </select>
            </div>
            <div class="field">
                <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Environment</label>
                <select @bind="experienceEnvironment" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; background: white; outline: none; cursor: pointer;">
                    <option value="Indoor">Indoor</option>
                    <option value="Outdoor">Outdoor</option>
                </select>
            </div>
        </div>

        <div class="field" style="margin-bottom: 1.5rem;">
            <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Experience</label>
            <textarea @bind="_currentExperience.Description" placeholder="Describe what children will experience..." style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; height: 120px; font-size: 1.05rem; resize: vertical; color: #26332d; outline: none; font-family: inherit;"></textarea>
        </div>

        <div class="field" style="margin-bottom: 2rem;">
            <label style="display: block; font-size: 0.75rem; font-weight: 800; color: #26332d; margin-bottom: 8px; text-transform: uppercase;">Materials</label>
            <input @bind="experienceMaterials" style="width: 100%; padding: 14px 16px; border: 1px solid #e1e6df; border-radius: 10px; font-size: 1.05rem; color: #26332d; outline: none;" />
        </div>

        <div style="border-top: 1px solid #e1e6df; padding-top: 1.5rem; display: flex; justify-content: flex-end; gap: 1rem;">
            <button style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer; transition: 0.2s;" onmouseover="this.style.background='#f8f9f5'" onmouseout="this.style.background='white'" @onclick="SaveDraftAsync">Save draft</button>
            <button style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer; transition: 0.2s;" onmouseover="this.style.background='#3e6253'" onmouseout="this.style.background='#557c6c'" @onclick="SaveExperienceAsync">Save to my library</button>
        </div>
    </dialog>
'''
    new_content = content[:start_idx] + new_modal + content[end_idx:]
    with open(path, 'w', encoding='utf-8') as f:
        f.write(new_content)
