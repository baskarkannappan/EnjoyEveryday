import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'
wireframe_path = os.path.join(base_dir, r'requirements\requirement\enjoyeveryday_attendance_wireframe.html')
razor_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\BranchAttendance.razor')
css_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\BranchAttendance.razor.css')
overview_css = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor.css')

# Reuse the CSS we generated for AttendanceOverview
with open(overview_css, 'r', encoding='utf-8') as f:
    styles = f.read()

with open(css_path, 'w', encoding='utf-8') as f:
    f.write(styles)

razor_content = """@page "/management/attendance/branches/{BranchId:guid}"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Entities
@inject BranchService BranchService
@inject ClassroomService ClassroomService
@inject AttendanceService AttendanceService
@inject NavigationManager Nav
@rendermode InteractiveServer

<div class="content">
    <section id="detail">
        <div class="detailhead">
            <button class="back" @onclick="BackToOverview">←</button>
            <div>
                <div class="crumb" id="detailCrumb">Attendance / Branch</div>
                <h1 id="detailTitle" style="font-size:27px;margin:3px 0">@(Branch?.Name ?? "Loading...")</h1>
                <div class="sub" id="detailSub">Attendance across classrooms, children and teachers.</div>
            </div>
        </div>
        
        <div class="filters">
            <input class="control" type="date" value="@DateTime.Today.ToString("yyyy-MM-dd")" disabled>
            <select class="control" @bind="SelectedType">
                <option value="both">Children &amp; teachers</option>
                <option value="children">Children only</option>
                <option value="teachers">Teachers only</option>
            </select>
            <input class="control search" @bind="SearchQuery" @bind:event="oninput" placeholder="Search people...">
        </div>
        
        <div class="stats">
            <div class="stat">
                <div class="mlabel">Children present</div>
                <div class="statnum" id="childTotal">- / -</div>
                <span class="pill green">Attendance</span>
            </div>
            <div class="stat">
                <div class="mlabel">Teachers present</div>
                <div class="statnum" id="teacherTotal">- / -</div>
                <span class="pill blue">Staff</span>
            </div>
            <div class="stat">
                <div class="mlabel">Not marked</div>
                <div class="statnum">-</div>
                <span class="pill amber">Action needed</span>
            </div>
            <div class="stat">
                <div class="mlabel">Missing check-out</div>
                <div class="statnum">-</div>
                <span class="pill red">Review</span>
            </div>
        </div>
        
        <div class="card">
            <div class="tabs">
                <button class="tab @(CurrentTab == "classrooms" ? "active" : "")" @onclick='() => CurrentTab = "classrooms"'>Classrooms</button>
                <button class="tab @(CurrentTab == "children" ? "active" : "")" @onclick='() => CurrentTab = "children"'>Children</button>
                <button class="tab @(CurrentTab == "teachers" ? "active" : "")" @onclick='() => CurrentTab = "teachers"'>Teachers</button>
            </div>
            
            @if (CurrentTab == "classrooms")
            {
                <div id="classPanel">
                    <div class="tablewrap">
                        <table>
                            <thead>
                                <tr>
                                    <th>Classroom</th>
                                    <th>Children present</th>
                                    <th>Teachers present</th>
                                    <th>Status</th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                @if (Classrooms != null)
                                {
                                    @foreach (var c in Classrooms)
                                    {
                                        <tr>
                                            <td><b>@c.Description</b></td>
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
            }
            else
            {
                <div id="peoplePanel">
                    <div style="display:flex;gap:9px;flex-wrap:wrap;margin-bottom:13px">
                        <select class="control" @bind="StatusFilter">
                            <option value="all">All statuses</option>
                            <option value="Present">Present</option>
                            <option value="Late">Late</option>
                            <option value="Absent">Absent</option>
                            <option value="Not marked">Not marked</option>
                        </select>
                        <button class="btn primary">Mark selected present</button>
                        <button class="btn">Save changes</button>
                    </div>
                    
                    <div class="tablewrap">
                        <table>
                            <thead>
                                <tr>
                                    <th><input type="checkbox" aria-label="Select all"></th>
                                    <th>Person</th>
                                    <th>Classroom / Role</th>
                                    <th>Status</th>
                                    <th>Arrival / Clock-in</th>
                                    <th>Departure / Clock-out</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td colspan="6" style="text-align: center; color: #888;">(Details for @CurrentTab will load here)</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            }
        </div>
    </section>
</div>

@code {
    [Parameter] public Guid BranchId { get; set; }
    
    private Branch Branch;
    private IEnumerable<Classroom> Classrooms = new List<Classroom>();
    
    private string CurrentTab = "classrooms";
    private string SelectedType = "both";
    private string SearchQuery = "";
    private string StatusFilter = "all";

    protected override async Task OnInitializedAsync()
    {
        var branches = await BranchService.GetBranchesAsync(Guid.Empty); // Using Guid.Empty as a bypass for this demo if needed, or we just get it another way.
        Branch = branches.FirstOrDefault(b => b.Id == BranchId);
        
        Classrooms = await ClassroomService.GetClassroomsAsync(BranchId);
    }

    private void BackToOverview()
    {
        Nav.NavigateTo("/management/attendance");
    }

    private void GoToClassroom(Guid id)
    {
        Nav.NavigateTo($"/management/attendance/classrooms/{id}");
    }
}
"""

with open(razor_path, 'w', encoding='utf-8') as f:
    f.write(razor_content)

print("Branch detail wireframe applied!")
