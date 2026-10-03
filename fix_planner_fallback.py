import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\MonthlyPlanner.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Inject IDbConnectionFactory
if '@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory' not in content:
    content = content.replace('@inject ExperienceService ExperienceService', '@inject ExperienceService ExperienceService\n@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory\n@using Dapper')

# Fallback for _selectedClassroomId
fallback = '''        if (string.IsNullOrEmpty(_selectedClassroomId))
        {
            using var connection = await DbConnectionFactory.CreateConnectionAsync();
            var fallbackId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
            if (fallbackId.HasValue && fallbackId.Value != Guid.Empty)
            {
                _selectedClassroomId = fallbackId.Value.ToString();
            }
        }
'''

content = content.replace('await LoadSchedules();', fallback + '\n        await LoadSchedules();')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
