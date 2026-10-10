import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'
razor_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor')

with open(razor_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add ClassroomService injection
if "@inject ClassroomService ClassroomService" not in content:
    content = content.replace("@inject BranchService BranchService", "@inject BranchService BranchService\n@inject ClassroomService ClassroomService")

# Add the Classroom attendance section HTML before </section>
classroom_html = """
        <div class="section">
            <div>
                <h2>Classroom attendance</h2>
                <div class="note">Quick view of attendance status across the organization</div>
            </div>
        </div>
        
        <div class="card" style="margin-bottom: 40px;">
            <div class="tabs">
                <button class="tab @(ClassroomTab == "all" ? "active" : "")" @onclick='() => ClassroomTab = "all"'>All classrooms</button>
                <button class="tab @(ClassroomTab == "pending" ? "active" : "")" @onclick='() => ClassroomTab = "pending"'>Needs marking</button>
                <button class="tab @(ClassroomTab == "attention" ? "active" : "")" @onclick='() => ClassroomTab = "attention"'>Staffing attention</button>
            </div>
            
            <div class="tablewrap">
                <table>
                    <thead>
                        <tr>
                            <th>Classroom</th>
                            <th>Branch</th>
                            <th>Children present</th>
                            <th>Teachers present</th>
                            <th>Attendance status</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        @if (AllClassrooms != null)
                        {
                            @foreach (var c in AllClassrooms)
                            {
                                <tr>
                                    <td>
                                        <b>@c.Description</b>
                                        <div class="note">Capacity: @c.Capacity</div>
                                    </td>
                                    <td>@(Branches?.FirstOrDefault(b => b.Id == c.BranchId)?.Name ?? "Unknown")</td>
                                    <td>- / @c.CurrentEnrollment</td>
                                    <td>- / -</td>
                                    <td><span class="pill gray">Pending</span></td>
                                    <td><button class="link" @onclick="() => GoToClassroom(c.Id)">Open →</button></td>
                                </tr>
                            }
                        }
                    </tbody>
                </table>
            </div>
        </div>
"""

content = content.replace("    </section>\n</div>", classroom_html + "    </section>\n</div>")

# Add state variables
if "private string ClassroomTab = \"all\";" not in content:
    content = content.replace("private string SearchQuery = \"\";", "private string SearchQuery = \"\";\n    private string ClassroomTab = \"all\";\n    private List<EnjoyEveryday.Domain.Entities.Classroom> AllClassrooms = new();")

# Add classroom loading logic
load_logic = """
        if (org != null)
        {
            Branches = await BranchService.GetBranchesAsync(org.Id);
            AllClassrooms.Clear();
            foreach (var b in Branches)
            {
                var classrooms = await ClassroomService.GetClassroomsAsync(b.Id);
                AllClassrooms.AddRange(classrooms);
            }
        }
"""
content = content.replace("""        if (org != null)
        {
            Branches = await BranchService.GetBranchesAsync(org.Id);
        }""", load_logic)

# Add GoToClassroom method
if "private void GoToClassroom(Guid id)" not in content:
    content = content.replace("private void ExportReport()", "private void GoToClassroom(Guid id)\n    {\n        Nav.NavigateTo($\"/management/attendance/classrooms/{id}\");\n    }\n\n    private void ExportReport()")

with open(razor_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Added Classroom attendance section to Overview!")
