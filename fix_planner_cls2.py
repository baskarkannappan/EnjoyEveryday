import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\MonthlyPlanner.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# I will find OnInitializedAsync and rewrite it
old_method = '''    protected override async Task OnInitializedAsync()
    {
        // Set to current week's Monday
        var today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        _currentDate = DateOnly.FromDateTime(today.AddDays(-1 * diff));
        _currentDateTime = _currentDate.ToDateTime(TimeOnly.MinValue);
        var orgs = await OrganizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org != null)
        {
            var branches = await BranchService.GetBranchesAsync(org.Id);
            var branch = branches.FirstOrDefault();
            if (branch != null)
            {
                _classrooms = (await ClassroomService.GetClassroomsAsync(branch.Id)).ToList();
            }
        }
        if (_classrooms.Any())
        {
            _selectedClassroomId = _classrooms.First().Id.ToString();
        }
        _experiences = (await ExperienceService.GetExperiencesAsync()).ToList();
                if (string.IsNullOrEmpty(_selectedClassroomId))
        {
            using var connection = await DbConnectionFactory.CreateConnectionAsync();
            var fallbackId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
            if (fallbackId.HasValue && fallbackId.Value != Guid.Empty)
            {
                _selectedClassroomId = fallbackId.Value.ToString();
            }
        }

        await LoadSchedules();
        CloseDialog();
    }'''

new_method = '''    protected override async Task OnInitializedAsync()
    {
        // Set to current week's Monday
        var today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        _currentDate = DateOnly.FromDateTime(today.AddDays(-1 * diff));
        _currentDateTime = _currentDate.ToDateTime(TimeOnly.MinValue);
        
        using var connection = await DbConnectionFactory.CreateConnectionAsync();
        var fallbackId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
        if (fallbackId.HasValue && fallbackId.Value != Guid.Empty)
        {
            _selectedClassroomId = fallbackId.Value.ToString();
        }

        _experiences = (await ExperienceService.GetExperiencesAsync()).ToList();
        
        await LoadSchedules();
        CloseDialog();
    }'''

content = content.replace(old_method, new_method)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
