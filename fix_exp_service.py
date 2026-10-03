import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Application\Services\ExperienceService.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace(
    'public async Task<Experience> CreateExperienceAsync(string title, string description, string dnaPayload, Guid createdByUserId, CancellationToken cancellationToken = default)',
    'public async Task<Experience> CreateExperienceAsync(string title, string description, string dnaPayload, Guid createdByUserId, string? status = null, CancellationToken cancellationToken = default)'
)

content = content.replace(
    'Status = ExperienceStatus.Idea.ToString(),',
    'Status = status ?? ExperienceStatus.Idea.ToString(),'
)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
