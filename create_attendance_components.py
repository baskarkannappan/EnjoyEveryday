import os
import shutil

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Daycare.Web\Components'
pages_dir = os.path.join(base_dir, 'Pages', 'Attendance')
shared_dir = os.path.join(base_dir, 'Shared', 'Attendance')

os.makedirs(shared_dir, exist_ok=True)

# 1. Create KpiCard.razor
kpi_card_razor = """
<div class="kpi @CssClass" style="@Style">
    <div class="klabel">@Label <span class="icon">@Icon</span></div>
    <div class="kvalue">@Value <span class="muted" style="font-size:14px">@ValueSuffix</span></div>
    <div class="kfoot">@FooterText</div>
</div>

@code {
    [Parameter] public string CssClass { get; set; } = "";
    [Parameter] public string Style { get; set; } = "";
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string ValueSuffix { get; set; } = "";
    [Parameter] public string FooterText { get; set; } = "";
}
"""
with open(os.path.join(shared_dir, 'KpiCard.razor'), 'w', encoding='utf-8') as f: f.write(kpi_card_razor)

kpi_card_css = """.kpi{background:var(--paper);border:1px solid var(--line);border-radius:17px;box-shadow:var(--shadow);padding:17px}
.klabel{font-size:12px;color:var(--muted);display:flex;justify-content:space-between}
.kvalue{font-size:29px;font-weight:800;margin:9px 0 2px;letter-spacing:-.8px}
.kfoot{font-size:11px;color:var(--muted)}
.icon{background:#f0f4ef;border-radius:9px;padding:6px}
@media(max-width:730px){.kpi{padding:13px}.kvalue{font-size:24px}}"""
with open(os.path.join(shared_dir, 'KpiCard.razor.css'), 'w', encoding='utf-8') as f: f.write(kpi_card_css)

# 2. Create BranchCard.razor
branch_card_razor = """
<div class="branch" data-branch="@Name" data-search="@SearchTerm">
    <div>
        <div class="bname">@Icon @Name</div>
        <div class="address">@Address</div>
        <div style="margin-top:8px"><span class="pill @CoverageCssClass">@CoverageText</span></div>
    </div>
    <div>
        <div class="mlabel">Children present</div>
        <div class="mvalue">@ChildrenPresent</div>
        <div class="track"><i style="width:@ChildrenPercentage%"></i></div>
    </div>
    <div>
        <div class="mlabel">Teachers present</div>
        <div class="mvalue">@TeachersPresent</div>
        <div class="track"><i style="width:@TeachersPercentage%"></i></div>
    </div>
    <button class="link branchaction" onclick="openBranch('@Name')">Open branch →</button>
</div>

@code {
    [Parameter] public string Name { get; set; } = "";
    [Parameter] public string SearchTerm { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string Address { get; set; } = "";
    [Parameter] public string CoverageCssClass { get; set; } = "";
    [Parameter] public string CoverageText { get; set; } = "";
    [Parameter] public string ChildrenPresent { get; set; } = "";
    [Parameter] public string ChildrenPercentage { get; set; } = "0";
    [Parameter] public string TeachersPresent { get; set; } = "";
    [Parameter] public string TeachersPercentage { get; set; } = "0";
}
"""
with open(os.path.join(shared_dir, 'BranchCard.razor'), 'w', encoding='utf-8') as f: f.write(branch_card_razor)

branch_card_css = """.branch{border:1px solid var(--line);border-radius:13px;padding:14px;display:grid;grid-template-columns:1.05fr 1fr 1fr 78px;gap:12px;align-items:center}
.bname{font-weight:800}.address{font-size:11px;color:var(--muted);margin-top:3px}
.mlabel{font-size:11px;color:var(--muted)}.mvalue{font-weight:750;margin-top:3px}
.track{height:5px;border-radius:6px;background:#edf0ec;margin-top:7px;overflow:hidden}
.track i{display:block;background:#5b8268;height:100%}
.pill{display:inline-block;border-radius:7px;padding:5px 8px;font-size:10px;font-weight:800;white-space:nowrap}
.green{background:var(--soft);color:#356448}.amber{background:var(--amber);color:#88651d}.red{background:var(--red);color:#9b4437}.gray{background:#f0f2ef;color:#66746b}
.link{background:none;border:0;color:var(--green);font-size:12px;font-weight:800;text-align:right}
@media(max-width:1150px){.branch{grid-template-columns:1fr 1fr 1fr 75px}}
@media(max-width:730px){.branch{grid-template-columns:1fr 1fr}.branch :deep(.branchaction){grid-column:1/-1;text-align:left}}"""
with open(os.path.join(shared_dir, 'BranchCard.razor.css'), 'w', encoding='utf-8') as f: f.write(branch_card_css)

# 3. Create NeedsAttentionAlert.razor
alert_razor = """
<div class="alert">
    <div class="alerticon">@Icon</div>
    <div>
        <div class="alerttitle">@Title</div>
        <div class="alertdesc">@Description</div>
        <button class="link" onclick="openBranch('@Branch')">@ActionText →</button>
    </div>
</div>

@code {
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string Branch { get; set; } = "";
    [Parameter] public string ActionText { get; set; } = "Review branch";
}
"""
with open(os.path.join(shared_dir, 'NeedsAttentionAlert.razor'), 'w', encoding='utf-8') as f: f.write(alert_razor)

alert_css = """.alert{display:flex;gap:10px;padding:12px;border:1px solid #eee8d6;background:#fbfaf5;border-radius:12px}
.alerticon{background:#f5edcf;border-radius:9px;padding:8px;height:34px}
.alerttitle{font-size:12px;font-weight:800}
.alertdesc{font-size:11px;color:var(--muted);margin:4px 0}
.link{background:none;border:0;color:var(--green);font-size:12px;font-weight:800;text-align:right}"""
with open(os.path.join(shared_dir, 'NeedsAttentionAlert.razor.css'), 'w', encoding='utf-8') as f: f.write(alert_css)

# 4. Modify AttendanceOverview.razor to use the components
overview_path = os.path.join(pages_dir, 'AttendanceOverview.razor')
with open(overview_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Make sure _Imports.razor has the using statement so components are recognized
imports_path = os.path.join(base_dir, '_Imports.razor')
with open(imports_path, 'r', encoding='utf-8') as f:
    imports_content = f.read()
if 'EnjoyEveryday.Daycare.Web.Components.Shared.Attendance' not in imports_content:
    with open(imports_path, 'a', encoding='utf-8') as f:
        f.write('\n@using EnjoyEveryday.Daycare.Web.Components.Shared.Attendance')

print("Components created successfully!")
