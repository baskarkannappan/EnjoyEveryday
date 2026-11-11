# Role-Based Navigation Menu Organization – Inventory & Proposal

> **Phase 1 Deliverable**: Complete inventory of all existing pages, routes, roles, and authorization requirements across EnjoyEveryday Daycare Web, together with the proposed 4-role navigation architecture.  
> **Status**: Ready for User Review (No application code modified).

---

## 1. Executive Summary & Objectives

EnjoyEveryday serves four core user roles across multi-branch childcare operations:
1. **Organization Admin**: Manages multi-branch organization strategy, cross-branch staff, billing, licensing, and overarching curriculum.
2. **Branch Admin**: Runs operational management of a specific branch—capacity, classrooms, daily staff rosters, safety compliance, and admissions.
3. **Lead Teacher**: Leads classroom pedagogical delivery, team handovers, daily schedules, coverage requests, and experience planning.
4. **Teacher (Educator)**: Focused squarely on direct childcare—routines (meals/naps/diapers), running experiences, logging moments, and communicating with parents.

### Core Problems Solved by this Reorganization
- **Elimination of Administrative Clutter**: Teachers will no longer see branch setup, billing, or complex admin tools.
- **Role Distinction**: Lead Teachers obtain dedicated coordination tools (handovers, coverage requests, planning) distinct from standard Teachers.
- **Logical Groupings**: All menus use simple, recognizable, action-oriented categories.
- **Context Clarity**: Active user, active role, selected organization, and selected branch are cleanly displayed with one-click switching.
- **Zero Breaking Changes**: All existing routes, bookmarks, query parameters, and backend authorization policies are strictly preserved.

---

## 2. Complete Page & Route Inventory

Below is the complete inventory of all Razor pages and routes in `EnjoyEveryday.Daycare.Web`, mapped to current placement, permissions, and target roles:

| Page / Component | Primary Route(s) | Current Nav Menu Group | Target Role(s) | Target Proposed Group | Required Permissions / Auth Scope |
|:---|:---|:---|:---|:---|:---|
| **Today (Cockpit)** | `/`, `/today` | Overview / My Day / Operations | All Roles | Dashboard / My Dashboard | Authenticated |
| **Org Admin Dashboard** | `/management/dashboard`, `/org-dashboard` | Direct / Role redirect | Org Admin | Dashboard | `org.admin`, `org.manage` |
| **Branch Admin Dashboard** | `/management/branch-dashboard`, `/branch-dashboard` | Direct / Role redirect | Branch Admin | Dashboard | `branch.manage` |
| **Teacher Workspace** | `/classroom`, `/my-classroom` | Direct / Role redirect | Lead Teacher, Teacher | My Dashboard / My Classroom | Authenticated Educator |
| **Today's Activities** | `/today-activities` | Overview / Operations / My Day | All Roles | Dashboard / Operations / Activities | Authenticated |
| **Attendance & Staffing** | `/management/attendance`, `/attendance` | My Day / Operations / Daycare Operations | All Roles | Daycare Operations / Attendance | `attendance.view`, `attendance.manage` |
| **Children Monthly Register** | `/attendance/monthly-children`, `/management/attendance/monthly-children` | My Day / Operations / Daycare Operations | Org Admin, Branch Admin | Operations / Daycare Operations | `attendance.view` |
| **Staff Monthly Register** | `/attendance/monthly-teachers`, `/management/attendance/monthly-teachers` | My Day / Operations / Daycare Operations | Org Admin, Branch Admin | Operations / Daycare Operations | `attendance.manage` |
| **Monthly Attendance Hub** | `/attendance/monthly`, `/management/attendance/monthly` | Secondary route | Org Admin, Branch Admin | Reports / Operations | `attendance.view` |
| **Branch Attendance Drilldown** | `/management/attendance/branches/{BranchName}` | Sub-page | Org Admin, Branch Admin | Operations Drilldown | `branch.manage` |
| **Classroom Attendance Drilldown** | `/management/attendance/classrooms/{ClassroomName}` | Sub-page | All Roles | Operations Drilldown | Authenticated |
| **Children & Families** | `/children` | My Children / People / Daycare Operations | All Roles | People Management / Children & Care | `child.view` |
| **Child Profile / Add / Edit** | `/children/add`, `/children/edit/{Id:guid}` | Sub-page / Modal | Org Admin, Branch Admin | People Management | `child.manage` |
| **Admissions & Pipeline** | `/enrollment`, `/management/enrollment` | Growth & Finance / Branch Management | Org Admin, Branch Admin | People Management / Admissions | `enrollment.view`, `enrollment.manage` |
| **Daily Care & Routine Logs** | `/daily-care` | My Day / Operations / Coordination | All Roles | Daily Care / Children & Care | `child.care` |
| **Individual Care Plans** | `/daily-care/plans` | My Children / Operations / Daycare Operations | All Roles | Daily Care / Care Plans | `child.view` |
| **Cubby Supplies & Restock** | `/daily-care/supplies` | My Children / Operations / Daycare Operations | All Roles | Daily Care / Cubby Supplies | Authenticated |
| **Classrooms & Occupancy** | `/classrooms` | Coordination / Operations / Daycare Operations | Org Admin, Branch Admin, Lead Teacher | Daycare Operations / Classrooms | `classroom.view` |
| **Classroom Wizard / New** | `/classrooms/new`, `.../classrooms/new` | Sub-page | Org Admin, Branch Admin | Operations | `classroom.manage` |
| **Classroom Profile** | `/classrooms/view/{Id:guid}` | Sub-page | Org Admin, Branch Admin, Lead Teacher | Operations | `classroom.view` |
| **Teachers & Educators** | `/teachers` | People / Daycare Operations | Org Admin, Branch Admin | People Management | `staff.view` |
| **Teacher Profile / View** | `/teachers/view/{Id:guid}` | Sub-page | Org Admin, Branch Admin | People Management | `staff.view` |
| **Teacher Wizard / Add / Edit** | `/teachers/new`, `/teachers/edit/{Id:guid}` | Sub-page | Org Admin, Branch Admin | People Management | `staff.manage` |
| **Staff Roster & Coverage** | `/management/roster` | Coordination / People / Organization | Org Admin, Branch Admin, Lead Teacher | Operations / Coordination | `staff.schedule` |
| **Staff & Role Assignments** | `/users-by-role`, `/management/users-by-role` | Organization / People | Org Admin, Branch Admin | Org Management / Branch Mgmt | `user.manage` |
| **Role & Permission Matrix** | `/roles`, `/management/roles` | Organization | Org Admin | Organization Management | `role.manage` |
| **Organization Profile** | `/organization/profile` | Organization | Org Admin | Organization Management | `org.manage` |
| **Branch Management** | `/organization/branches`, `/management` | Branch Management / Configuration | Org Admin, Branch Admin | Org Management / Branch Mgmt | `branch.view`, `org.manage` |
| **Branch Setup (Create Branch)** | `/management/branches/new` | Sub-page | Org Admin | Organization Management | `branch.create`, `org.manage` |
| **Branch Detail** | `/management/branches/{BranchId:guid}` | Sub-page | Org Admin, Branch Admin | Branch Management | `branch.view` |
| **Organization Tree** | `/organization/tree` | Secondary | Org Admin | Organization Management | `org.view` |
| **Organization Settings** | `/settings`, `/organization/settings` | Configuration | Org Admin | Finance & Administration | `org.settings` |
| **AI Agent Settings & Keys** | `/settings/ai-agents`, `/settings/agent-access` | Configuration | Org Admin | Finance & Administration | `org.admin` |
| **Policies & Notifications** | `/policies`, `/safety-compliance` | Configuration / Operations | Org Admin, Branch Admin | Daycare Operations / Safety | `safety.manage` |
| **Emergency Floor Maps** | `/safety-trust/floor-maps` | Safety & Emergency / Configuration | All Roles | Safety & Emergency / My Work | Authenticated |
| **Emergency Contacts (ERC)** | `/safety-trust/emergency-contacts` | Safety & Emergency / Configuration | All Roles | Safety & Emergency / My Work | Authenticated |
| **Safety & Trust Center** | `/safety-trust` | Operations / Configuration | Org Admin, Branch Admin | Operations / Safety | `safety.view` |
| **Experience Library & Storybooks** | `/experiences` | Experiences / Learning & Care / Operations | All Roles | Experiences & Activities | Authenticated |
| **Experience Marketplace** | `/marketplace` | Direct route / Operations | Org Admin, Branch Admin, Lead Teacher | Experiences & Activities | `curriculum.manage` |
| **Experience Studio** | `/studio`, `/studio/{ExperienceId:guid}` | Experiences / Learning & Care / Operations | Org Admin, Branch Admin, Lead Teacher | Experiences & Activities | `curriculum.create` |
| **Experience Planning & Rhythm** | `/planning` | Experiences / Learning & Care / Operations | Org Admin, Branch Admin, Lead Teacher | Experiences & Activities | `curriculum.plan` |
| **Harvest & Reflections** | `/harvest` | Experiences / Learning & Care / Operations | All Roles | Experiences & Activities | Authenticated |
| **Materials Marketplace & Orders** | `/materials-marketplace` | Coordination / Experiences / Learning | All Roles | Experiences & Activities / Operations | Authenticated |
| **Material Orders & Loans** | `/materials-orders` | Learning & Care | Branch Admin, Org Admin | Operations / Experiences | `orders.manage` |
| **Experience Storybook Viewer** | `/experiences/{ExperienceId:guid}/story` | Sub-page | All Roles | Experiences & Activities | Authenticated |
| **Branch Schedules & Events** | `/events`, `/events/{Id:guid}` | My Day / Operations / Daycare Operations | All Roles | Daycare Operations / Events | Authenticated |
| **Child Journey & Milestones** | `/journey`, `/journey/{ChildId:guid}` | My Children / Learning & Care / Operations | All Roles | Child Development | `child.view` |
| **Memories & Moments** | `/memories` | My Children / Operations | All Roles | Child Development | `child.view` |
| **Reports & Intelligence** | `/insights`, `/intelligence` | Overview / Branch Management | Org Admin, Branch Admin | Dashboard / Reports | `reports.view` |
| **Classroom Messages (Family)** | `/classroom-messages`, `/messages` | Family Connection / Learning & Care | All Roles | Family Engagement | `communication.view` |
| **Daycare Announcements** | `/announcements`, `/management/announcements`| Family Connection / Operations | All Roles | Family Engagement | `announcements.view` |
| **Notifications Center** | `/notifications` | Family Connection | All Roles | Family Engagement / Header | Authenticated |
| **Family Stories & Feedback** | `/stories` | Secondary route | Org Admin, Branch Admin | Family Engagement | Authenticated |
| **Account Book & Audit Log** | `/audit`, `/rhythm-history` | Growth & Finance / Configuration | Org Admin | Finance & Administration | `audit.view`, `org.admin` |
| **Billing & Invoices** | `/management/billing`, `/billing`, `/billing/invoices` | Growth & Finance | Org Admin | Finance & Administration | `billing.manage` |
| **Financial Reports** | `/billing/reports` | Growth & Finance | Org Admin | Finance & Administration | `billing.reports` |
| **Select Organization** | `/select-organization` | Profile Footer | All Multi-Org Users | Context Switcher | Authenticated |
| **Select Branch** | `/select-branch` | Profile Footer | All Multi-Branch Users | Context Switcher | Authenticated |
| **My Profile** | `/profile` | My Account | All Roles | My Work / My Account | Authenticated |
| **Help & Support** | `/support` | My Account | All Roles | My Work / My Account | Authenticated |

---

## 3. Proposed Navigation Structures

### 3.1 Organization Admin Navigation (8 Groups)

Designed for multi-branch executives and headquarters administrators:

```markdown
✨ EnjoyEveryday — Organization Management

▼ A. DASHBOARD
   ├── 📊 Organization Overview       (/)
   ├── 📈 Organization Reports        (/insights)
   └── ☀️ Today's Activity Summary    (/today-activities)

▼ B. ORGANIZATION MANAGEMENT
   ├── 🏢 Organization Profile        (/organization/profile)
   ├── 🏛️ Branch Management           (/organization/branches)
   ├── ➕ Add New Branch              (/management/branches/new)
   ├── 👥 Organization Staff & Roles  (/users-by-role)
   └── 🔑 Role & Permission Matrix    (/roles)

▼ C. PEOPLE MANAGEMENT
   ├── 👶 Children & Families         (/children)
   ├── 📋 Admissions & Inquiries      (/enrollment)
   ├── 🧑‍🏫 Teachers & Educators        (/teachers)
   ├── ⭐ Lead Teachers               (/teachers?filter=lead)
   └── 💬 Family & Parent Contacts    (/classroom-messages)

▼ D. DAYCARE OPERATIONS
   ├── 🏫 Classrooms & Capacity       (/classrooms)
   ├── 📅 Staff Coverage & Ratio      (/management/roster)
   ├── ✅ Attendance & Staffing       (/management/attendance)
   ├── 🗓️ Children Monthly Register   (/attendance/monthly-children)
   ├── 👥 Staff Monthly Register      (/attendance/monthly-teachers)
   ├── ❤️ Child Care Plans            (/daily-care/plans)
   ├── 📦 Cubby Supplies & Restock    (/daily-care/supplies)
   └── 🗓️ Branch Schedules & Events   (/events)

▼ E. EXPERIENCES & ACTIVITIES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── ✨ Experience Studio           (/studio)
   ├── 🗓️ Experience Planning         (/planning)
   ├── ☀️ Today's Activities Live     (/today-activities)
   └── 🌾 Harvest & Reflections       (/harvest)

▼ F. CHILD DEVELOPMENT
   ├── 🌿 Child Journey & Milestones  (/journey)
   ├── 📸 Memories & Moments          (/memories)
   └── 💡 Progress & Insights         (/insights)

▼ G. FAMILY ENGAGEMENT
   ├── 💬 Parent Communication        (/classroom-messages)
   ├── 📢 Daycare Announcements       (/announcements)
   └── 🔔 System Notifications        (/notifications)

▼ H. FINANCE & ADMINISTRATION
   ├── 💳 Billing & Invoices          (/management/billing)
   ├── 📊 Financial Reports           (/billing/reports)
   ├── 📖 Account Book & Audit        (/audit)
   ├── ⚙️ Organization Settings       (/organization/settings)
   └── 🤖 AI Agent Settings           (/settings/ai-agents)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.2 Branch Admin Navigation (8 Groups)

Designed for branch directors focused on local branch operations:

```markdown
✨ EnjoyEveryday — Branch Operations

▼ A. DASHBOARD
   ├── 📊 Branch Dashboard            (/)
   ├── ☀️ Today's Live Activities     (/today-activities)
   └── ✅ Live Attendance Status      (/management/attendance)

▼ B. BRANCH MANAGEMENT
   ├── 🏢 Branch Settings & Details   (/organization/branches)
   ├── 🏫 Classrooms & Capacity       (/classrooms)
   ├── 📅 Staff Coverage & Roster     (/management/roster)
   └── 👥 Staff & Role Assignments    (/users-by-role)

▼ C. PEOPLE & ADMISSIONS
   ├── 👶 Children & Families         (/children)
   ├── 📋 Admissions & Enquiries      (/enrollment)
   └── 🧑‍🏫 Teachers & Educators        (/teachers)

▼ D. ATTENDANCE & DAILY OPERATIONS
   ├── ✅ Daily Attendance            (/management/attendance)
   ├── 🗓️ Children Monthly Register   (/attendance/monthly-children)
   ├── 👥 Staff Monthly Register      (/attendance/monthly-teachers)
   ├── ❤️ Care & Routine Logs         (/daily-care)
   ├── 📋 Individual Care Plans       (/daily-care/plans)
   ├── 📦 Cubby Supplies & Restock    (/daily-care/supplies)
   ├── 🗓️ Branch Events & Schedule    (/events)
   └── 🛡️ Safety & Compliance         (/safety-compliance)

▼ E. EXPERIENCES & ACTIVITIES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── ✨ Experience Studio           (/studio)
   ├── 🗓️ Experience Planning         (/planning)
   ├── 🌾 Harvest & Reflections       (/harvest)
   └── 🛍️ Materials Marketplace       (/materials-marketplace)

▼ F. CHILD DEVELOPMENT
   ├── 🌿 Child Journey & Milestones  (/journey)
   └── 📸 Memories & Moments          (/memories)

▼ G. FAMILY ENGAGEMENT
   ├── 💬 Parent Communication        (/classroom-messages)
   ├── 📢 Daycare Announcements       (/announcements)
   └── 🔔 Notifications              (/notifications)

▼ H. REPORTS
   ├── 📊 Branch Operational Reports  (/insights)
   └── 🗓️ Monthly Attendance Reports  (/attendance/monthly)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.3 Lead Teacher Navigation (7 Groups)

Designed for pedagogical leaders guiding classrooms and coordinating care:

```markdown
✨ EnjoyEveryday — Lead Classroom Coordination

▼ A. MY DASHBOARD
   ├── ☀️ Today's Overview            (/)
   ├── 🗓️ Today's Schedule & Run      (/today-activities)
   └── ✅ Classroom Attendance        (/management/attendance)

▼ B. CLASSROOM MANAGEMENT
   ├── 🏫 My Classrooms & Readiness   (/classrooms)
   ├── 🤝 Team Handover & Logs        (/daily-care)
   └── 👥 Request Staff Coverage      (/management/roster)

▼ C. CHILDREN & CARE
   ├── 👶 Classroom Children          (/children)
   ├── ❤️ Child Daily Care            (/daily-care)
   ├── 📋 Individual Care Plans       (/daily-care/plans)
   └── 📦 Cubby Supplies & Restock    (/daily-care/supplies)

▼ D. EXPERIENCES & ACTIVITIES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── 🗓️ Activity Planning           (/planning)
   ├── ✨ Experience Studio           (/studio)
   ├── 🌾 Harvest & Reflections       (/harvest)
   └── 🛍️ Materials & Supply Kits     (/materials-marketplace)

▼ E. CHILD DEVELOPMENT
   ├── 🌿 Child Journey & Milestones  (/journey)
   └── 📸 Memories & Moments          (/memories)

▼ F. FAMILY ENGAGEMENT
   ├── 💬 Parent Messages             (/classroom-messages)
   ├── 📢 Daycare Announcements       (/announcements)
   └── 🔔 Notifications              (/notifications)

▼ G. MY WORK
   ├── 🛡️ Emergency Floor Maps        (/safety-trust/floor-maps)
   ├── 📞 Emergency Contacts (ERC)    (/safety-trust/emergency-contacts)
   ├── 👤 My Profile                  (/profile)
   └── ❓ Help & Support              (/support)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.4 Teacher Navigation (6 Groups)

Designed for daily classroom care and instruction with minimal friction:

```markdown
✨ EnjoyEveryday — My Classroom

▼ A. MY DASHBOARD
   ├── ☀️ Today's Overview            (/)
   ├── 🎨 Today's Activities          (/today-activities)
   └── ✅ Daily Attendance            (/management/attendance)

▼ B. MY CLASSROOM
   ├── 👶 My Classroom Children       (/children)
   └── 🗓️ Daily Schedule & Events     (/events)

▼ C. DAILY CARE
   ├── ❤️ Routine Logs (Meals/Naps)   (/daily-care)
   ├── 📋 Individual Care Plans       (/daily-care/plans)
   └── 📦 Cubby Supplies & Restock    (/daily-care/supplies)

▼ D. ACTIVITIES & EXPERIENCES
   ├── 📚 Experience Library          (/experiences)
   ├── ☀️ Run Today's Activities      (/today-activities)
   └── 🌾 Harvest & Reflections       (/harvest)

▼ E. CHILD DEVELOPMENT
   ├── 🌿 Child Journey & Milestones  (/journey)
   └── 📸 Memories & Moments          (/memories)

▼ F. MY WORK
   ├── 💬 Parent Messages             (/classroom-messages)
   ├── 🔔 Notifications              (/notifications)
   ├── 📢 Announcements              (/announcements)
   ├── 🛡️ Emergency Floor Maps        (/safety-trust/floor-maps)
   ├── 📞 Emergency Contacts (ERC)    (/safety-trust/emergency-contacts)
   ├── 👤 My Profile                  (/profile)
   └── ❓ Help & Support              (/support)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

## 4. Context Display & Workspace Switcher Architecture

At the bottom of the navigation sidebar (`nav-profile-footer`), the user context card will display:
1. **User Identity & Avatar**: Circle with initial, user full name / email.
2. **Current Active Role Label**:
   - `Org Admin`
   - `Branch Admin`
   - `Lead Teacher`
   - `Educator`
3. **Workspace Switching Icons**:
   - 🏢 **Branch Switcher** (`/select-branch`) — visible when user is assigned to >1 branch.
   - 🏛️ **Organization Switcher** (`/select-organization`) — visible for multi-organization users.
   - 🚪 **Sign Out** (`/auth/logout`)
4. **Active Role / View Selector Dropdown**:
   - If user possesses multiple roles (e.g. Org Admin + Lead Teacher), the dropdown allows switching between authorized view experiences:
     ```html
     VIEW: [ Org Admin ▾ ]
     Options:
       • Org Admin
       • Branch Admin
       • Lead Teacher
       • Educator
     ```
   - Persisted seamlessly via `IUserViewPreferenceService` and query parameter `?view=...`.

---

## 5. Security & Authorization Guarantees

1. **Defense-in-Depth**:
   - Hiding or reorganizing links does **not** bypass security. Every backend service (e.g., `ChildService`, `ClassroomService`, `EnrollmentService`, `ExperienceService`) validates authorization scopes (`child.manage`, `staff.schedule`, etc.) using `TenantContext`, `UserContext`, and `AccessScopeRepo`.
2. **Backward-Compatibility**:
   - No route is deleted, deprecated, or renamed. Direct URL access, existing bookmarks, and redirects will continue functioning seamlessly.
3. **Design System Integrity**:
   - All styling conforms strictly to `GEMINI.md`:
     - Font: `--ee-font` (Roboto / system sans-serif)
     - Sage green accents: `--accent` (`#557c6c`), `--accent-dark` (`#31523c`), `--accent-light` (`#e6eee9`)
     - Borders & text: `--border` (`#e4e7df`), `--text` (`#26332d`), `--muted` (`#6c7870`)
     - Clean chevron accordion collapsible groups with auto-expansion for active routes.

---

## 6. Implementation Roadmap

- [x] **Phase 1: Inventory & Proposal**: Catalog all routes and define the 4-role navigation architecture (Completed in this document).
- [ ] **Phase 2: NavMenu Refactoring**: Update `NavMenu.razor` to implement the proposed role sections, toggle states, icons, and context badges.
- [ ] **Phase 3: Testing & Visual Verification**: Verify each role view (`OrgAdmin`, `BranchAdmin`, `LeadTeacher`, `Teacher`) in the browser with live credentials, checking responsiveness and mobile drawers.

