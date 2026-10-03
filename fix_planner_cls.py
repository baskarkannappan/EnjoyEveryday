import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\MonthlyPlanner.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Force _selectedClassroomId to be the same LIMIT 1 query used by Experiences.razor
old_init = '''        if (_classrooms.Any())
        {
            _selectedClassroomId = _classrooms.First().Id.ToString();
        }
        if (string.IsNullOrEmpty(_selectedClassroomId))
        {
            using var connection = await DbConnectionFactory.CreateConnectionAsync();
            var fallbackId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
            if (fallbackId.HasValue && fallbackId.Value != Guid.Empty)
            {
                _selectedClassroomId = fallbackId.Value.ToString();
            }
        }'''

new_init = '''        using var connection = await DbConnectionFactory.CreateConnectionAsync();
        var fallbackId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
        if (fallbackId.HasValue && fallbackId.Value != Guid.Empty)
        {
            _selectedClassroomId = fallbackId.Value.ToString();
        }'''

content = content.replace(old_init, new_init)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
