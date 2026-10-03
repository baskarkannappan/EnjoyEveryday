import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Api\Controllers\ExperiencesController.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix CreateExperienceAsync call
# Usually it looks like: await _experienceService.CreateExperienceAsync(request.Title, request.Description, request.DnaPayload, request.CreatedByUserId, cancellationToken);
# We need to change it to: await _experienceService.CreateExperienceAsync(request.Title, request.Description, request.DnaPayload, request.CreatedByUserId, null, cancellationToken);

import re
content = re.sub(r'CreateExperienceAsync\(([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*cancellationToken\)', r'CreateExperienceAsync(\1, \2, \3, \4, null, cancellationToken)', content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
