import os
import re

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Inject IDbConnectionFactory and Dapper
if '@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory' not in content:
    content = content.replace('@inject PlannerService PlannerService', '@inject PlannerService PlannerService\n@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory\n@using Dapper')

# 2. Add SaveToLibraryAsync and update the button
if 'private async Task SaveToLibraryAsync()' not in content:
    methods_to_add = '''
    private async Task SaveToLibraryAsync()
    {
        _currentExperience.Status = "Idea";
        await SaveExperienceAsync();
    }
'''
    content = content.replace('private async Task SaveDraftAsync()', methods_to_add + '\n    private async Task SaveDraftAsync()')

# Update the button
content = content.replace(
    '<button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveExperienceAsync">Save to my library</button>',
    '<button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveToLibraryAsync">Save to my library</button>'
)

# 3. Add Delete button to Edit modal
old_footer = '''<div style="border-top: 1px solid #e1e6df; padding-top: 1.5rem; display: flex; justify-content: flex-end; gap: 1rem;">
            <button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveDraftAsync">Save draft</button>
            <button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveToLibraryAsync">Save to my library</button>
        </div>'''

new_footer = '''<div style="border-top: 1px solid #e1e6df; padding-top: 1.5rem; display: flex; justify-content: flex-end; gap: 1rem;">
            @if (_isEditing)
            {
                <button type="button" class="secondary" style="border: 1px solid #ff4d4d; color: #ff4d4d; background: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer; margin-right: auto;" @onclick="DeleteExperienceAsync">Delete</button>
            }
            <button type="button" class="secondary" style="border: 1px solid #e1e6df; background: white; color: #26332d; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveDraftAsync">Save draft</button>
            <button type="button" class="primary" style="background: #557c6c; border: none; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 800; font-size: 0.95rem; cursor: pointer;" @onclick="SaveToLibraryAsync">Save to my library</button>
        </div>'''

content = content.replace(old_footer, new_footer)

# 4. Fix SaveScheduleAsync classroom constraint issue
old_save_schedule = '''    private async Task SaveScheduleAsync()
    {
        // Using a dummy classroom ID for now since we don't have classroom selection built in
        var classroomId = Guid.NewGuid(); 
        await PlannerService.ScheduleExperienceAsync(_currentExperience.Id, classroomId, DateOnly.FromDateTime(_scheduleDate), _scheduleTimeOfDay);
        _isScheduleModalOpen = false;
    }'''

new_save_schedule = '''    private async Task SaveScheduleAsync()
    {
        using var connection = await DbConnectionFactory.CreateConnectionAsync();
        var classroomId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM classrooms LIMIT 1");
        
        if (!classroomId.HasValue || classroomId.Value == Guid.Empty)
        {
            var branchId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM branches LIMIT 1");
            if (!branchId.HasValue || branchId.Value == Guid.Empty)
            {
                branchId = Guid.NewGuid();
                var tenantId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM tenants LIMIT 1") ?? Guid.NewGuid();
                var orgId = await connection.QueryFirstOrDefaultAsync<Guid?>("SELECT id FROM organizations LIMIT 1") ?? Guid.NewGuid();
                await connection.ExecuteAsync("INSERT INTO branches (id, tenant_id, organization_id, name, is_active, created_at, updated_at) VALUES (@Id, @TenantId, @OrgId, 'Main Branch', true, @Now, @Now)", new { Id = branchId.Value, TenantId = tenantId, OrgId = orgId, Now = DateTimeOffset.UtcNow });
            }
            
            classroomId = Guid.NewGuid();
            var tId = await connection.QueryFirstOrDefaultAsync<Guid>("SELECT tenant_id FROM branches WHERE id = @Id", new { Id = branchId.Value });
            await connection.ExecuteAsync("INSERT INTO classrooms (id, tenant_id, branch_id, name, is_active, created_at, updated_at) VALUES (@Id, @TenantId, @BranchId, 'Room A', true, @Now, @Now)", new { Id = classroomId.Value, TenantId = tId, BranchId = branchId.Value, Now = DateTimeOffset.UtcNow });
        }

        await PlannerService.ScheduleExperienceAsync(_currentExperience.Id, classroomId.Value, DateOnly.FromDateTime(_scheduleDate), _scheduleTimeOfDay);
        _isScheduleModalOpen = false;
    }'''

content = content.replace(old_save_schedule, new_save_schedule)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
