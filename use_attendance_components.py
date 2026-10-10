import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Daycare.Web\Components'
overview_path = os.path.join(base_dir, 'Pages', 'Attendance', 'AttendanceOverview.razor')

with open(overview_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace KPIs
kpis_original = """<div class="kpis">
  <div class="kpi childkpi"><div class="klabel">Children present <span class="icon">♧</span></div><div class="kvalue">248 <span class="muted" style="font-size:14px">/ 286</span></div><div class="kfoot">86.7% of children expected</div></div>
  <div class="kpi teacherkpi"><div class="klabel">Teachers present <span class="icon">♙</span></div><div class="kvalue">42 <span class="muted" style="font-size:14px">/ 46</span></div><div class="kfoot">91.3% of teachers expected</div></div>
  <div class="kpi"><div class="klabel">Attendance to finish <span class="icon">◷</span></div><div class="kvalue">3 <span class="muted" style="font-size:14px">classrooms</span></div><div class="kfoot">Some people are not marked yet</div></div>
  <div class="kpi"><div class="klabel">Needs attention <span class="icon">!</span></div><div class="kvalue">2 <span class="muted" style="font-size:14px">classrooms</span></div><div class="kfoot">Review staffing coverage</div></div>
 </div>"""

kpis_new = """<div class="kpis">
  <KpiCard CssClass="childkpi" Label="Children present" Icon="♧" Value="248" ValueSuffix="/ 286" FooterText="86.7% of children expected" />
  <KpiCard CssClass="teacherkpi" Label="Teachers present" Icon="♙" Value="42" ValueSuffix="/ 46" FooterText="91.3% of teachers expected" />
  <KpiCard Label="Attendance to finish" Icon="◷" Value="3" ValueSuffix="classrooms" FooterText="Some people are not marked yet" />
  <KpiCard Label="Needs attention" Icon="!" Value="2" ValueSuffix="classrooms" FooterText="Review staffing coverage" />
 </div>"""

content = content.replace(kpis_original, kpis_new)

# Replace Branches
branches_original = """<div class="branches" id="branches">
   <div class="branch" data-branch="Downtown" data-search="downtown king street bluebirds butterflies"><div><div class="bname">🌿 Downtown</div><div class="address">72 King Street</div><div style="margin-top:8px"><span class="pill green">Healthy coverage</span></div></div><div><div class="mlabel">Children present</div><div class="mvalue">74 / 84</div><div class="track"><i style="width:88%"></i></div></div><div><div class="mlabel">Teachers present</div><div class="mvalue">12 / 13</div><div class="track"><i style="width:92%"></i></div></div><button class="link branchaction" onclick="openBranch('Downtown')">Open branch →</button></div>
   <div class="branch" data-branch="North" data-search="north maple avenue little robins"><div><div class="bname">🌳 North</div><div class="address">18 Maple Avenue</div><div style="margin-top:8px"><span class="pill green">Healthy coverage</span></div></div><div><div class="mlabel">Children present</div><div class="mvalue">68 / 76</div><div class="track"><i style="width:89%"></i></div></div><div><div class="mlabel">Teachers present</div><div class="mvalue">11 / 12</div><div class="track"><i style="width:92%"></i></div></div><button class="link branchaction" onclick="openBranch('North')">Open branch →</button></div>
   <div class="branch" data-branch="West" data-search="west cedar road rainbow"><div><div class="bname">🌞 West</div><div class="address">104 Cedar Road</div><div style="margin-top:8px"><span class="pill amber">Needs review</span></div></div><div><div class="mlabel">Children present</div><div class="mvalue">58 / 68</div><div class="track"><i style="width:85%"></i></div></div><div><div class="mlabel">Teachers present</div><div class="mvalue">9 / 10</div><div class="track"><i style="width:90%"></i></div></div><button class="link branchaction" onclick="openBranch('West')">Open branch →</button></div>
   <div class="branch" data-branch="South" data-search="south garden lane sunshine"><div><div class="bname">🌼 South</div><div class="address">6 Garden Lane</div><div style="margin-top:8px"><span class="pill gray">Attendance pending</span></div></div><div><div class="mlabel">Children present</div><div class="mvalue">48 / 58</div><div class="track"><i style="width:83%"></i></div></div><div><div class="mlabel">Teachers present</div><div class="mvalue">10 / 11</div><div class="track"><i style="width:91%"></i></div></div><button class="link branchaction" onclick="openBranch('South')">Open branch →</button></div>
  </div>"""

branches_new = """<div class="branches" id="branches">
   <BranchCard Name="Downtown" SearchTerm="downtown king street bluebirds butterflies" Icon="🌿" Address="72 King Street" CoverageCssClass="green" CoverageText="Healthy coverage" ChildrenPresent="74 / 84" ChildrenPercentage="88" TeachersPresent="12 / 13" TeachersPercentage="92" />
   <BranchCard Name="North" SearchTerm="north maple avenue little robins" Icon="🌳" Address="18 Maple Avenue" CoverageCssClass="green" CoverageText="Healthy coverage" ChildrenPresent="68 / 76" ChildrenPercentage="89" TeachersPresent="11 / 12" TeachersPercentage="92" />
   <BranchCard Name="West" SearchTerm="west cedar road rainbow" Icon="🌞" Address="104 Cedar Road" CoverageCssClass="amber" CoverageText="Needs review" ChildrenPresent="58 / 68" ChildrenPercentage="85" TeachersPresent="9 / 10" TeachersPercentage="90" />
   <BranchCard Name="South" SearchTerm="south garden lane sunshine" Icon="🌼" Address="6 Garden Lane" CoverageCssClass="gray" CoverageText="Attendance pending" ChildrenPresent="48 / 58" ChildrenPercentage="83" TeachersPresent="10 / 11" TeachersPercentage="91" />
  </div>"""

content = content.replace(branches_original, branches_new)

# Replace Alerts
alerts_original = """<div class="alerts">
   <div class="alert"><div class="alerticon">◷</div><div><div class="alerttitle">Attendance not completed</div><div class="alertdesc">South · Sunshine Room has 4 children not marked.</div><button class="link" onclick="openBranch('South')">Review classroom →</button></div></div>
   <div class="alert"><div class="alerticon">♙</div><div><div class="alerttitle">Teacher coverage needs review</div><div class="alertdesc">West · Rainbow Room's assigned lead teacher is not marked present.</div><button class="link" onclick="openBranch('West')">Review branch →</button></div></div>
   <div class="alert"><div class="alerticon">↗</div><div><div class="alerttitle">Child checkout missing</div><div class="alertdesc">2 children are checked in without a recorded departure.</div><button class="link" onclick="openBranch('Downtown')">View records →</button></div></div>
  </div>"""

alerts_new = """<div class="alerts">
   <NeedsAttentionAlert Icon="◷" Title="Attendance not completed" Description="South · Sunshine Room has 4 children not marked." Branch="South" ActionText="Review classroom" />
   <NeedsAttentionAlert Icon="♙" Title="Teacher coverage needs review" Description="West · Rainbow Room's assigned lead teacher is not marked present." Branch="West" ActionText="Review branch" />
   <NeedsAttentionAlert Icon="↗" Title="Child checkout missing" Description="2 children are checked in without a recorded departure." Branch="Downtown" ActionText="View records" />
  </div>"""

content = content.replace(alerts_original, alerts_new)

with open(overview_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Updated AttendanceOverview.razor to use components!")
