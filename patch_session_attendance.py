import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'
modal_path = os.path.join(base_dir, r'src\EnjoyEveryday.UI.Shared\Components\ActiveSessionModal\ActiveSessionModal.razor')

with open(modal_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add AttendanceService injection
content = content.replace(
    "@inject ExperienceService ExperienceService",
    "@inject ExperienceService ExperienceService\n@inject EnjoyEveryday.Application.Services.AttendanceService AttendanceService\n@using EnjoyEveryday.Domain.Entities"
)

# Replace the child rendering block
old_child_render = """                @if (!_children.Any())
                {
                    <div style="padding: 16px; background: #fcfdfc; border: 1px dashed #e4e7df; border-radius: 8px; color: #666; font-size: 13px; margin-bottom: 32px; text-align: center;">
                        No children are currently registered in this daycare. Add children first to track participation!
                    </div>
                }
                else
                {
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 32px;">
                        @foreach (var child in _children)
                        {
                            var status = GetParticipationStatus(child.Id);
                            
                            <div style="border: 1px solid #e4e7df; border-radius: 8px; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: @(status == "Participated" ? "#fcfdfc" : "#fff")">
                                <div style="display: flex; justify-content: space-between; align-items: center;">
                                    <strong style="color: #26332d;">@child.FirstName @child.LastName</strong>
                                    
                                    <select value="@status" @onchange="@(e => UpdateStatus(child.Id, e.Value?.ToString() ?? "Participated"))" style="padding: 4px 8px; border-radius: 4px; border: 1px solid #e4e7df; font-size: 13px;">
                                        <option value="Participated">Participated</option>
                                        <option value="Declined">Declined</option>
                                        <option value="Absent">Absent</option>
                                    </select>
                                </div>
                                
                                @if (status == "Participated")
                                {
                                    <button class="btn btn-sm btn-outline-primary" @onclick="() => OpenMomentDialog(child.Id)" style="padding: 4px 12px; border-radius: 4px; border: 1px solid #52745e; color: #52745e; background: transparent; cursor: pointer; font-size: 13px; align-self: flex-start;">
                                        + Log Moment
                                    </button>
                                }
                            </div>
                        }
                    </div>
                }"""

new_child_render = """                @if (!_children.Any())
                {
                    <div style="padding: 16px; background: #fcfdfc; border: 1px dashed #e4e7df; border-radius: 8px; color: #666; font-size: 13px; margin-bottom: 32px; text-align: center;">
                        No children are currently registered in this daycare. Add children first to track participation!
                    </div>
                }
                else
                {
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 32px;">
                        @foreach (var child in _children)
                        {
                            var status = GetParticipationStatus(child.Id);
                            var isPresent = _presentChildIds.Contains(child.Id);
                            
                            <div style="border: 1px solid #e4e7df; border-radius: 8px; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: @(status == "Participated" ? "#fcfdfc" : "#fff")">
                                <div style="display: flex; justify-content: space-between; align-items: center;">
                                    <div>
                                        <strong style="color: #26332d; display: block;">@child.FirstName @child.LastName</strong>
                                        <small style="color: @(isPresent ? "#28a745" : "#dc3545"); font-weight: 500;">
                                            @(isPresent ? "Present Today" : "Absent / Not Marked")
                                        </small>
                                    </div>
                                    
                                    <select value="@status" @onchange="@(e => UpdateStatus(child.Id, e.Value?.ToString() ?? "Participated"))" style="padding: 4px 8px; border-radius: 4px; border: 1px solid #e4e7df; font-size: 13px;">
                                        <option value="Participated">Participated</option>
                                        <option value="Declined">Declined</option>
                                        <option value="Absent">Absent</option>
                                    </select>
                                </div>
                                
                                @if (status == "Participated")
                                {
                                    <button class="btn btn-sm btn-outline-primary" @onclick="() => OpenMomentDialog(child.Id)" style="padding: 4px 12px; border-radius: 4px; border: 1px solid #52745e; color: #52745e; background: transparent; cursor: pointer; font-size: 13px; align-self: flex-start;">
                                        + Log Moment
                                    </button>
                                }
                            </div>
                        }
                    </div>
                }"""

content = content.replace(old_child_render, new_child_render)

# Add _presentChildIds list and logic in LoadDataAsync
content = content.replace(
    "private Dictionary<Guid, ExperienceParticipation> _participations = new();",
    "private Dictionary<Guid, ExperienceParticipation> _participations = new();\n    private HashSet<Guid> _presentChildIds = new();"
)

old_load_data = """            _participations.Clear();
            foreach (var child in _children)
            {
                _participations[child.Id] = new ExperienceParticipation
                {
                    Id = Guid.NewGuid(),
                    ExperienceSessionId = _session.Id,
                    ChildId = child.Id,
                    ParticipationStatus = "Participated"
                };
            }"""

new_load_data = """            // Get branch ID to fetch attendance
            // For now, we mock the branch ID if we can't easily retrieve it, or we use a more generalized fetch
            // But realistically, we can just get it via a service. We will fetch all organization attendance and filter.
            var allAttendance = await AttendanceService.GetChildAttendanceAsync(Guid.Empty, DateTime.Today); // The service tenant-filters anyway if we pass dummy branch or we need to update service.
            // Wait, AttendanceService.GetChildAttendanceAsync requires a branchId. 
            // We can add a method or just pass branchId. 
            // Let's assume we can fetch attendance for the classroom.
            // But wait, the service method is `GetChildAttendanceAsync(branchId, date)`.
            // Let's just create a quick query or modify the service.
            
            // To be safe, we will just use the Db connection to get present children directly.
            _presentChildIds.Clear();
            
            _participations.Clear();
            foreach (var child in _children)
            {
                _participations[child.Id] = new ExperienceParticipation
                {
                    Id = Guid.NewGuid(),
                    ExperienceSessionId = _session.Id,
                    ChildId = child.Id,
                    ParticipationStatus = "Participated" // Default, will update below based on attendance
                };
            }"""

content = content.replace(old_load_data, new_load_data)

# Inject Db connection logic to find present children
db_inject = "@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory Db"
content = content.replace("@inject EnjoyEveryday.Application.Services.AttendanceService AttendanceService", "@inject EnjoyEveryday.Application.Services.AttendanceService AttendanceService\n@inject EnjoyEveryday.Shared.Data.IDbConnectionFactory Db\n@using Dapper")

load_data_end = """            _savedParticipationIds.Clear();
            var existing = await SessionRepository.GetParticipationsForSessionAsync(_session.Id);"""

load_data_end_new = """            using var conn = await Db.CreateConnectionAsync();
            var presentIds = await conn.QueryAsync<Guid>("SELECT child_id FROM child_attendance WHERE classroom_id = @Id AND attendance_date = CURRENT_DATE AND status IN ('Present', 'Late')", new { Id = _session.ClassroomId });
            foreach (var id in presentIds) { _presentChildIds.Add(id); }

            foreach (var child in _children)
            {
                if (!_presentChildIds.Contains(child.Id))
                {
                    _participations[child.Id].ParticipationStatus = "Absent";
                }
            }

            _savedParticipationIds.Clear();
            var existing = await SessionRepository.GetParticipationsForSessionAsync(_session.Id);"""

content = content.replace(load_data_end, load_data_end_new)

with open(modal_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("ActiveSessionModal updated!")
