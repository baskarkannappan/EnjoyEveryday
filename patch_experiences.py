import sys, re

with open('src/EnjoyEveryday.Daycare.Web/Components/Pages/ExperienceStudio.razor', 'r', encoding='utf-8') as f:
    studio_code = f.read()

# Extract modal html
modal_match = re.search(r'(@if \(!_hideHistoryDialog\)\s*\{\s*<div style=\"position: fixed;.*?\}\n)', studio_code, re.DOTALL)
if modal_match:
    modal_html = modal_match.group(1).replace('_experience', '_currentExperience').replace('_dna', '_parsedDna')
else:
    print("Could not find modal HTML")
    sys.exit(1)

# Extract code
code_match = re.search(r'(// History & Compare.*?return true;\s*\n    })', studio_code, re.DOTALL)
if code_match:
    code_logic = code_match.group(1).replace('_experience', '_currentExperience').replace('_dna', '_parsedDna')
else:
    print("Could not find code logic")
    sys.exit(1)

with open('src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences.razor', 'r', encoding='utf-8') as f:
    exp_code = f.read()

# Inject html
exp_code = exp_code.replace('<ExperienceScheduleModal', modal_html + '\n  <ExperienceScheduleModal')

# Inject code
exp_code = exp_code.replace('private Experience _currentExperience', code_logic + '\n\n    private Experience _currentExperience')

# Add OnCompare
exp_code = exp_code.replace('OnImprove="ImproveExperience" />', 'OnImprove="ImproveExperience"\n      OnCompare="OpenHistoryDialog" />')

with open('src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences.razor', 'w', encoding='utf-8') as f:
    f.write(exp_code)

print('Done!')
