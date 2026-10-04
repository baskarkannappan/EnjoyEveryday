import re
import os

filepath = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.UI.Shared\Components\TeacherWizard\TeacherCreationWizard.razor'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

steps = [
    'PersonalInfo', 'ContactInfo', 'EmergencyContact', 'EmploymentInfo',
    'Qualifications', 'Education', 'Certifications', 'Skills', 'Experience', 'Languages',
    'ChildcareSpecialties', 'ClassroomAssignments', 'WorkSchedule', 'Availability',
    'ProfessionalDevelopment', 'Documents', 'Training', 'Permissions', 'Status',
    'PerformanceObservations', 'TeacherPreferences'
]

# We will manually replace the step bodies for the ones that were missed.
for i, comp in enumerate(steps):
    step_num = i + 1
    # Look for `@if (_currentStep == X) { ... }` or `else if (_currentStep == X) { ... }`
    # and replace the content inside `{ ... }` with `<EnjoyEveryday.UI.Shared.Components.TeacherWizard.Steps.CompName Model="Model" />`
    
    # We can use a simple regex for this that doesn't rely on lookaheads to the next step
    # but finds the matching brace.
    
    pattern_str = r'(else if \(_currentStep == ' + str(step_num) + r'\)\s*\{)(.*?)(?=\}\s*else if \(_currentStep == |\}\s*else\s*\{|\}\s*<div style=\"border-top:)'
    if step_num == 1:
        pattern_str = r'(@if \(_currentStep == 1\)\s*\{)(.*?)(?=\}\s*else if \(_currentStep == )'
        
    matches = list(re.finditer(pattern_str, content, re.DOTALL))
    for match in matches:
        # Check if it already has the component call (i.e. we already fixed it)
        if f'Steps.{comp}Step Model="Model"' in match.group(2) or f'<{comp}Step Model="Model"' in match.group(2):
            continue
            
        replacement = f'\n            <EnjoyEveryday.UI.Shared.Components.TeacherWizard.Steps.{comp}Step Model="Model" />\n        '
        content = content.replace(match.group(2), replacement)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
print("Fixed!")
