import re
import os

filepath = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.UI.Shared\Components\TeacherWizard\TeacherCreationWizard.razor'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

pattern = re.compile(r'(@?if \(_currentStep == (\d+)\)\s*\{)(.*?)(?=\}\s*else if \(_currentStep == |\}\s*else\s*\{|\}\s*<div style=\"border-top:)', re.DOTALL)

matches = pattern.findall(content)

steps = [
    'PersonalInfo', 'ContactInfo', 'EmergencyContact', 'EmploymentInfo',
    'Qualifications', 'Education', 'Certifications', 'Skills', 'Experience', 'Languages',
    'ChildcareSpecialties', 'ClassroomAssignments', 'WorkSchedule', 'Availability',
    'ProfessionalDevelopment', 'Documents', 'Training', 'Permissions', 'Status',
    'PerformanceObservations', 'TeacherPreferences'
]

out_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.UI.Shared\Components\TeacherWizard\Steps'
os.makedirs(out_dir, exist_ok=True)

new_content = content

# Variables for Education, Certification, Experience, Language that are in the TeacherCreationWizard code block
# We need to make sure the sub-components have access to them if they use them.
# Ah, I added `_newEducation`, `_newCertification`, etc. inside the `@code` block of TeacherCreationWizard.razor.
# Wait, if I extract them to sub-components, those variables need to be in the sub-component's `@code` block instead!

for match in matches:
    full_if_decl = match[0] # e.g. '@if (_currentStep == 1) {'
    step_num = int(match[1])
    step_body = match[2].strip()
    
    if step_num <= len(steps):
        comp_name = steps[step_num - 1] + 'Step'
        comp_path = os.path.join(out_dir, comp_name + '.razor')
        
        code_block = ""
        # Add new variables if needed
        if comp_name == 'EducationStep':
            code_block = """
@code {
    [Parameter] public TeacherCreationWizard.TeacherWizardModel Model { get; set; } = default!;
    private TeacherCreationWizard.EducationEntry _newEducation = new();
}"""
        elif comp_name == 'CertificationsStep':
            code_block = """
@code {
    [Parameter] public TeacherCreationWizard.TeacherWizardModel Model { get; set; } = default!;
    private TeacherCreationWizard.CertificationEntry _newCertification = new();
}"""
        elif comp_name == 'ExperienceStep':
            code_block = """
@code {
    [Parameter] public TeacherCreationWizard.TeacherWizardModel Model { get; set; } = default!;
    private TeacherCreationWizard.ExperienceEntry _newExperience = new();
}"""
        elif comp_name == 'LanguagesStep':
            code_block = """
@code {
    [Parameter] public TeacherCreationWizard.TeacherWizardModel Model { get; set; } = default!;
    private TeacherCreationWizard.LanguageEntry _newLanguage = new();
}"""
        else:
            code_block = """
@code {
    [Parameter] public TeacherCreationWizard.TeacherWizardModel Model { get; set; } = default!;
}"""

        with open(comp_path, 'w', encoding='utf-8') as cf:
            cf.write('@namespace EnjoyEveryday.UI.Shared.Components.TeacherWizard.Steps\n')
            cf.write('@using EnjoyEveryday.UI.Shared.Components.TeacherWizard\n\n')
            cf.write(step_body + '\n')
            cf.write(code_block)
        
        replacement = f'\n            <EnjoyEveryday.UI.Shared.Components.TeacherWizard.Steps.{comp_name} Model="Model" />\n        '
        new_content = new_content.replace(match[2], replacement)

# We should also remove the `_newEducation` etc from TeacherCreationWizard.razor
new_content = re.sub(r'private EducationEntry _newEducation = new\(\);\s*', '', new_content)
new_content = re.sub(r'private CertificationEntry _newCertification = new\(\);\s*', '', new_content)
new_content = re.sub(r'private ExperienceEntry _newExperience = new\(\);\s*', '', new_content)
new_content = re.sub(r'private LanguageEntry _newLanguage = new\(\);\s*', '', new_content)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(new_content)

print('Done extracting components!')
