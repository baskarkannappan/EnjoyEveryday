import os
import re

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'
wireframe_path = os.path.join(base_dir, r'requirements\requirement\enjoyeveryday_attendance_wireframe.html')
razor_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor')
css_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor.css')

with open(wireframe_path, 'r', encoding='utf-8') as f:
    wireframe = f.read()

# Extract styles
styles_match = re.search(r'<style>(.*?)</style>', wireframe, re.DOTALL)
if styles_match:
    styles = styles_match.group(1).strip()
    # Remove body/app/side/top/main generic styles to not break the rest of the app, 
    # but keep the variables and content styles.
    styles = styles.replace("body{margin:0;background:var(--bg);color:var(--ink);font:14px/1.45 Inter,system-ui,Segoe UI,sans-serif}", "")
    styles = styles.replace(".app{display:grid;grid-template-columns:225px 1fr;min-height:100vh}", "")
    styles = styles.replace(".side{background:#fbfcf9;border-right:1px solid var(--line);padding:24px 14px;position:sticky;top:0;height:100vh}", "")
    styles = styles.replace(".main{min-width:0}", "")
    styles = styles.replace(".top{height:74px;background:#ffffffb8;border-bottom:1px solid var(--line);display:flex;justify-content:space-between;align-items:center;padding:0 32px}", "")
    # Add ::deep where necessary or just rely on CSS isolation for the classes.
    with open(css_path, 'w', encoding='utf-8') as f:
        f.write(styles)

# Create the new razor content
razor_content = """@page "/management/attendance"
@using EnjoyEveryday.Application.Services
@using EnjoyEveryday.Domain.Repositories
@inject OrganizationService OrganizationService
@inject BranchService BranchService
@inject AttendanceService AttendanceService
@inject NavigationManager Nav
@rendermode InteractiveServer

<div class="content">
    <section id="overview">
        <div class="heading">
            <div>
                <h1>Attendance</h1>
                <div class="sub">A clear picture of who's here, who's expected, and where a team may need support.</div>
            </div>
            <button class="btn" @onclick="ExportReport">↓ Export report</button>
        </div>
        
        <div class="filters">
            <input class="control" type="date" @bind="SelectedDate" @bind:after="LoadData" aria-label="Attendance date">
            <select class="control" @bind="SelectedBranchFilter">
                <option value="all">All branches</option>
                @if (Branches != null)
                {
                    @foreach (var b in Branches)
                    {
                        <option value="@b.Name">@b.Name</option>
                    }
                }
            </select>
            <select class="control" @bind="SelectedTypeFilter">
                <option value="both">Children &amp; teachers</option>
                <option value="children">Children only</option>
                <option value="teachers">Teachers only</option>
            </select>
            <input class="control search" @bind="SearchQuery" @bind:event="oninput" placeholder="Search branches...">
        </div>

        @if (Summary != null)
        {
            <div class="kpis">
                @if (SelectedTypeFilter != "teachers")
                {
                    <div class="kpi childkpi">
                        <div class="klabel">Children present <span class="icon">♧</span></div>
                        <div class="kvalue">@Summary.PresentChildren <span class="muted" style="font-size:14px">/ @Summary.ExpectedChildren</span></div>
                        <div class="kfoot">@(Summary.ExpectedChildren > 0 ? (Summary.PresentChildren * 100 / Summary.ExpectedChildren) : 0)% of children expected</div>
                    </div>
                }
                
                @if (SelectedTypeFilter != "children")
                {
                    <div class="kpi teacherkpi">
                        <div class="klabel">Teachers present <span class="icon">♙</span></div>
                        <div class="kvalue">@Summary.PresentTeachers <span class="muted" style="font-size:14px">/ @Summary.ExpectedTeachers</span></div>
                        <div class="kfoot">@(Summary.ExpectedTeachers > 0 ? (Summary.PresentTeachers * 100 / Summary.ExpectedTeachers) : 0)% of teachers expected</div>
                    </div>
                }
                
                <div class="kpi">
                    <div class="klabel">Unmarked children <span class="icon">◷</span></div>
                    <div class="kvalue">@Summary.UnmarkedChildren</div>
                    <div class="kfoot">Some children are not marked yet</div>
                </div>
                
                <div class="kpi">
                    <div class="klabel">Unmarked teachers <span class="icon">!</span></div>
                    <div class="kvalue">@Summary.UnmarkedTeachers</div>
                    <div class="kfoot">Review staffing coverage</div>
                </div>
            </div>
        }

        <div class="cols">
            <div class="card">
                <div class="cardhead">
                    <div>
                        <h2>Attendance by branch</h2>
                        <div class="note">Select a branch to see classrooms and people</div>
                    </div>
                    <span class="pill gray">Today</span>
                </div>
                <div class="branches" id="branches">
                    @if (Branches != null)
                    {
                        @foreach (var b in Branches.Where(FilterBranch))
                        {
                            <div class="branch" @onclick="() => GoToBranch(b.Id)" style="cursor: pointer;">
                                <div>
                                    <div class="bname">@b.Name</div>
                                    <div class="address">@b.Location</div>
                                    <div style="margin-top:8px">
                                        <span class="pill green">Healthy coverage</span>
                                    </div>
                                </div>
                                <div>
                                    <div class="mlabel">Children present</div>
                                    <div class="mvalue">- / -</div>
                                    <div class="track"><i style="width:0%"></i></div>
                                </div>
                                <div>
                                    <div class="mlabel">Teachers present</div>
                                    <div class="mvalue">- / -</div>
                                    <div class="track"><i style="width:0%"></i></div>
                                </div>
                                <button class="link branchaction" @onclick:stopPropagation="true" @onclick="() => GoToBranch(b.Id)">Open branch →</button>
                            </div>
                        }
                    }
                </div>
            </div>
            
            <div class="card">
                <div class="cardhead">
                    <h2>Needs attention</h2>
                </div>
                <div class="alerts">
                    <div class="alert">
                        <div class="alerticon">◷</div>
                        <div>
                            <div class="alerttitle">Attendance not completed</div>
                            <div class="alertdesc">@Summary?.UnmarkedChildren children and @Summary?.UnmarkedTeachers teachers not marked.</div>
                        </div>
                    </div>
                </div>
                <div style="margin-top:18px;padding:13px;border-radius:12px;background:#f2f6f1">
                    <b style="font-size:12px">A useful daily habit</b>
                    <div class="note" style="margin-top:5px">Start with unmarked attendance, then review staffing and missing check-outs.</div>
                </div>
            </div>
        </div>
    </section>
</div>

@code {
    private IEnumerable<EnjoyEveryday.Domain.Entities.Branch> Branches = new List<EnjoyEveryday.Domain.Entities.Branch>();
    private AttendanceSummary? Summary;
    private DateTime SelectedDate = DateTime.Today;
    private string SelectedBranchFilter = "all";
    private string SelectedTypeFilter = "both";
    private string SearchQuery = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        
        var orgs = await OrganizationService.GetOrganizationsAsync();
        var org = orgs.FirstOrDefault();
        if (org != null)
        {
            Branches = await BranchService.GetBranchesAsync(org.Id);
        }
    }

    private async Task LoadData()
    {
        Summary = await AttendanceService.GetOrganizationSummaryAsync(SelectedDate);
    }

    private bool FilterBranch(EnjoyEveryday.Domain.Entities.Branch b)
    {
        if (SelectedBranchFilter != "all" && b.Name != SelectedBranchFilter)
            return false;
            
        if (!string.IsNullOrWhiteSpace(SearchQuery) && 
            !b.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) && 
            !b.Location.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            return false;
            
        return true;
    }

    private void GoToBranch(Guid id)
    {
        Nav.NavigateTo($"/management/attendance/branches/{id}");
    }
    
    private void ExportReport()
    {
        // Prototype logic
    }
}
"""

with open(razor_path, 'w', encoding='utf-8') as f:
    f.write(razor_content)

print("Wireframe applied successfully!")
