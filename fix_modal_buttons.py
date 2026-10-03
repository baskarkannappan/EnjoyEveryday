import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add type="button" to prevent accidental form submits (even though there's no form, Blazor sometimes is weird with buttons)
# and add the onclicks.

content = content.replace(
    '<button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;">Schedule</button>',
    '<button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="ScheduleExperience">Schedule</button>'
)

content = content.replace(
    '<button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="() => { CloseViewDialog(); OpenEditExperienceModal(_currentExperience); }">Edit</button>',
    '<button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="() => { CloseViewDialog(); OpenEditExperienceModal(_currentExperience); }">Edit</button>'
)

content = content.replace(
    '<button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;">Duplicate</button>',
    '<button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="DuplicateExperience">Duplicate</button>'
)

# Also ensure Create/Edit modal buttons have type="button"
content = content.replace(
    '<button class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveDraftAsync">Save draft</button>',
    '<button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveDraftAsync">Save draft</button>'
)
content = content.replace(
    '<button class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveExperienceAsync">Save to my library</button>',
    '<button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveExperienceAsync">Save to my library</button>'
)

# Add the methods to @code
methods = '''
    private void DuplicateExperience()
    {
        var sourceExp = _currentExperience;
        CloseViewDialog();
        OpenAddExperienceModal();
        _currentExperience.Title = sourceExp.Title + " (Copy)";
        _currentExperience.Description = sourceExp.Description;
        _currentExperience.DnaPayload = sourceExp.DnaPayload;
    }

    private void ScheduleExperience()
    {
        CloseViewDialog();
        // Since we don't have a scheduling popup ready yet, we will just close for now.
    }
'''

content = content.replace('private void CloseViewDialog()', methods + '\n    private void CloseViewDialog()')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
