import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add the new variables to the @code block
vars_to_add = '''
    private string experienceAge = "3-4";
    private string experienceDuration = "15 minutes";
    private string experienceType = "Explore";
    private string experienceEnvironment = "Indoor";
    private string experienceMaterials = "";

    private async Task SaveDraftAsync()
    {
        _currentExperience.Status = "Idea";
        await SaveExperienceAsync();
    }
'''

content = content.replace('private Experience _currentExperience = new();', vars_to_add + '\n    private Experience _currentExperience = new();')

# Update OpenAddExperienceModal to reset these
reset_vars = '''
        experienceAge = "3-4";
        experienceDuration = "15 minutes";
        experienceType = "Explore";
        experienceEnvironment = "Indoor";
        experienceMaterials = "";
'''
content = content.replace('_currentExperience = new Experience { DnaPayload = "{\\"type\\": \\"Experience\\"}" };', '_currentExperience = new Experience { DnaPayload = "{\\"type\\": \\"Experience\\"}" };' + reset_vars)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
