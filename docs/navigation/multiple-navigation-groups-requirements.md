# Multiple Menu Groups and Nested Submenus – Requirements & Proposal

> **Phase 2 Proposal**: Architectural design and mapping for splitting the navigation into lightweight, independent groups with expandable nested submenus across all four user roles.  
> **Status**: Ready for User Review (Awaiting confirmation before implementing in code).

---

## 1. Executive Summary & Goals

The previous role-based navigation unified all modules into role views. However, having large, continuous accordion blocks can feel heavy and cluttered. 

This enhancement introduces:
1. **Lightweight Functional Groups**:
   - Small, clean, uppercase group headers (e.g. `WORKSPACE`, `ORGANIZATION`, `PEOPLE & ADMISSIONS`, `DAYCARE OPERATIONS`).
   - Subtle typography (`10px`, uppercase, letter-spacing `0.5px`, muted slate color) without bulky container backgrounds.
   - Distinct vertical breathing room between groups (`margin-top: 14px`).
2. **Nested Expandable Submenus**:
   - Main menu items that contain related sub-features (e.g., `Branch Management`, `Admissions & Inquiries`, `Safety & Compliance`) can be expanded/collapsed independently.
   - Smooth chevron indicator (`▾` / `▸`), indented nested items (`padding-left: 26px`), and a subtle left guide line (`1px solid var(--border)`).
3. **Smart Auto-Expansion**:
   - When a user navigates directly to or bookmarks a child page (e.g., `/management/branches/new`), the parent submenu (`Branch Management`) and its group (`ORGANIZATION`) automatically unfold.
   - Opening one submenu never closes unrelated submenus.
4. **Clean Role Specialization**:
   - **Organization Admin**: 8 high-level functional groups covering multi-branch oversight, admissions pipeline, curriculum, and finance.
   - **Branch Admin**: 8 branch-scoped operational groups without organization-wide administrative overhead.
   - **Lead Teacher**: 7 classroom coordination and curriculum groups.
   - **Teacher**: 6 streamlined, daily care and activity groups designed for quick, touch-friendly access.

---

## 2. Visual Hierarchy & Styling Specifications

| UI Element | CSS Properties & Tokens | Behavior & Notes |
|:---|:---|:---|
| **Group Header** | `font-size: 10px; font-weight: 750; text-transform: uppercase; letter-spacing: 0.6px; color: var(--muted, #6c7870); padding: 8px 10px 4px;` | Purely visual section divider. No borders or heavy background containers. |
| **Main Menu Item** | `display: flex; align-items: center; gap: 9px; padding: 6px 10px; font-size: 12px; font-weight: 500; color: var(--text, #26332d); border-radius: 0px;` | Hover: `background: var(--accent-light, #e6eee9); color: var(--accent-dark, #31523c);` |
| **Active Menu Item** | `background: var(--accent-light, #e6eee9) !important; color: var(--accent-dark, #31523c) !important; font-weight: 700;` | Shows clear current-page indicator. |
| **Submenu Toggle Item**| `display: flex; justify-content: space-between; align-items: center; cursor: pointer;` | Contains label + icon on the left, and an animated chevron on the right. |
| **Submenu Container** | `display: flex; flex-direction: column; gap: 2px; margin-left: 14px; padding-left: 12px; border-left: 1.5px solid var(--border, #e4e7df); margin-top: 2px; margin-bottom: 4px;` | Visually nests children under parent item. |
| **Submenu Item** | `padding: 5px 8px; font-size: 11.5px; font-weight: 400; color: var(--text, #26332d); text-decoration: none; border-radius: 0px;` | Active: `font-weight: 700; color: var(--accent-dark); background: rgba(85, 124, 108, 0.08);` |
| **Unread Badges** | `background: var(--color-danger, #c53030); color: white; font-size: 10.5px; font-weight: 800; border-radius: 999px; padding: 1px 6px;` | Used for unread parent messages and urgent alerts. |

---

## 3. Complete Role Grouping & Nested Submenu Mapping

### 3.1 Organization Admin Navigation (8 Groups)

```markdown
✨ EnjoyEveryday — Organization Management

▼ GROUP 1: WORKSPACE
   ├── 📊 Dashboard                   (/)
   ├── 📈 Organization Overview       (/insights)
   └── 🔔 Notifications              (/notifications)

▼ GROUP 2: ORGANIZATION
   ├── 🏢 Organization Profile        (/organization/profile)
   ├── 🏛️ Branch Management           [Submenu]
   │     ├── View All Branches        (/organization/branches)
   │     ├── Create New Branch        (/management/branches/new)
   │     └── Branch Settings          (/organization/branches)
   ├── 👥 Organization Staff & Roles  [Submenu]
   │     ├── All Staff & Assignments  (/users-by-role)
   │     ├── Teachers & Educators     (/teachers)
   │     └── Staff Coverage Roster    (/management/roster)
   └── 🔑 Role & Permission Matrix    (/roles)

▼ GROUP 3: PEOPLE & ADMISSIONS
   ├── 👶 Children & Families         (/children)
   ├── 📋 Admissions & Inquiries      [Submenu]
   │     ├── New Inquiry              (/enrollment?action=new-inquiry)
   │     ├── Admission Pipeline       (/enrollment)
   │     └── Enrolled Children        (/children)
   └── 💬 Parent & Family Management  (/classroom-messages)

▼ GROUP 4: DAYCARE OPERATIONS
   ├── 🏫 Classrooms & Capacity       (/classrooms)
   ├── ✅ Attendance & Staffing       (/management/attendance)
   ├── 🗓️ Children Monthly Register   (/attendance/monthly-children)
   ├── 👥 Staff Monthly Register      (/attendance/monthly-teachers)
   ├── ❤️ Child Care Plans & Dietaries(/daily-care/plans)
   ├── 📦 Cubby Supplies & Restock    (/daily-care/supplies)
   └── 🗓️ Branch Schedules & Events   (/events)

▼ GROUP 5: EXPERIENCES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── ✨ Experience Studio           (/studio)
   ├── 🗓️ Experience Planning         (/planning)
   ├── ☀️ Experience Execution Live   (/today-activities)
   ├── 🌾 Harvest & Insights          (/harvest)
   └── 🛍️ Materials & Orders         (/materials-marketplace)

▼ GROUP 6: CHILD DEVELOPMENT
   ├── 🌿 Child Journey               (/journey)
   ├── 📸 Child Memories              (/memories)
   └── 💡 Progress & Insights         (/insights)

▼ GROUP 7: FAMILY ENGAGEMENT
   ├── 💬 Parent Communication        (/classroom-messages)
   ├── 📢 Daycare Announcements       (/announcements)
   └── 💌 Family Stories & Feedback   (/stories)

▼ GROUP 8: BUSINESS & SETTINGS
   ├── 💳 Billing & Payments          (/management/billing)
   ├── 📊 Financial Reports           (/billing/reports)
   ├── 📖 Account Book & Audit        (/audit)
   ├── ⚙️ Organization Settings       (/organization/settings)
   └── 🤖 AI Agent Settings           (/settings/ai-agents)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.2 Branch Admin Navigation (8 Groups)

```markdown
✨ EnjoyEveryday — Branch Operations

▼ GROUP 1: WORKSPACE
   ├── 📊 Branch Dashboard            (/)
   ├── ☀️ Today's Live Activities     (/today-activities)
   └── 🔔 Notifications              (/notifications)

▼ GROUP 2: BRANCH MANAGEMENT
   ├── 🏢 Branch Profile & Settings   (/organization/branches)
   ├── 🏫 Classrooms & Capacity       (/classrooms)
   ├── 📅 Staff Coverage & Roster     (/management/roster)
   └── 👥 Staff & Role Assignments    (/users-by-role)

▼ GROUP 3: PEOPLE & ADMISSIONS
   ├── 👶 Children Directory          (/children)
   ├── 📋 Admissions & Inquiries      [Submenu]
   │     ├── New Inquiry              (/enrollment?action=new-inquiry)
   │     ├── Admission Pipeline       (/enrollment)
   │     └── Enrolled Children        (/children)
   └── 🧑‍🏫 Teachers & Staff            (/teachers)

▼ GROUP 4: DAYCARE OPERATIONS
   ├── ✅ Daily Attendance            (/management/attendance)
   ├── 🗓️ Children Monthly Register   (/attendance/monthly-children)
   ├── 👥 Staff Monthly Register      (/attendance/monthly-teachers)
   ├── ❤️ Care Routines & Logs        (/daily-care)
   ├── 📋 Child Care Plans & Dietaries(/daily-care/plans)
   ├── 📦 Cubby Supplies & Restock    (/daily-care/supplies)
   ├── 🗓️ Branch Events & Schedule    (/events)
   └── 🛡️ Safety & Compliance         [Submenu]
         ├── Compliance Checks        (/safety-compliance)
         ├── Emergency Floor Maps     (/safety-trust/floor-maps)
         └── Emergency Contacts (ERC) (/safety-trust/emergency-contacts)

▼ GROUP 5: EXPERIENCES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── ✨ Experience Studio           (/studio)
   ├── 🗓️ Experience Planning         (/planning)
   ├── 🌾 Harvest & Reflections       (/harvest)
   └── 🛍️ Materials Marketplace       (/materials-marketplace)

▼ GROUP 6: CHILD DEVELOPMENT
   ├── 🌿 Child Journey               (/journey)
   └── 📸 Child Memories              (/memories)

▼ GROUP 7: FAMILY ENGAGEMENT
   ├── 💬 Parent Communication        (/classroom-messages)
   └── 📢 Daycare Announcements       (/announcements)

▼ GROUP 8: REPORTS & SETTINGS
   ├── 📊 Branch Operational Reports  (/insights)
   ├── 🗓️ Monthly Attendance Reports  (/attendance/monthly)
   ├── 👤 My Profile                  (/profile)
   └── ❓ Help & Support              (/support)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.3 Lead Teacher Navigation (7 Groups)

```markdown
✨ EnjoyEveryday — Lead Classroom Coordination

▼ GROUP 1: MY WORKSPACE
   ├── ☀️ My Dashboard                (/)
   ├── 🗓️ Today's Schedule            (/today-activities)
   └── 🔔 Notifications              (/notifications)

▼ GROUP 2: CLASSROOM MANAGEMENT
   ├── 🏫 My Classrooms & Readiness   (/classrooms)
   ├── 🤝 Team Handover & Logs        (/daily-care)
   └── 👥 Request Staff Coverage      (/management/roster)

▼ GROUP 3: CHILD CARE
   ├── 👶 Classroom Children          (/children)
   ├── ✅ Children's Attendance       (/management/attendance)
   ├── ❤️ Daily Care & Wellbeing      (/daily-care)
   ├── 📋 Child Care Plans & Dietaries(/daily-care/plans)
   └── 📦 Cubby Supplies & Restock    (/daily-care/supplies)

▼ GROUP 4: EXPERIENCES & ACTIVITIES
   ├── 📚 Experience Library          (/experiences)
   ├── 🛒 Experience Marketplace      (/marketplace)
   ├── ✨ Experience Studio           (/studio)
   ├── 🗓️ Experience Planning         (/planning)
   ├── 🎨 Classroom Activities (Run)  (/today-activities)
   ├── 🌾 Harvest & Reflections       (/harvest)
   └── 🛍️ Materials & Supply Kits     (/materials-marketplace)

▼ GROUP 5: CHILD DEVELOPMENT
   ├── 🌿 Child Journey               (/journey)
   ├── 📸 Child Memories              (/memories)
   └── 💡 Progress & Insights         (/insights)

▼ GROUP 6: FAMILY ENGAGEMENT
   ├── 💬 Parent Communication        (/classroom-messages)
   └── 📢 Daycare Announcements       (/announcements)

▼ GROUP 7: MY ACCOUNT
   ├── 🛡️ Safety & Emergency          [Submenu]
   │     ├── Emergency Floor Maps     (/safety-trust/floor-maps)
   │     └── Emergency Contacts (ERC) (/safety-trust/emergency-contacts)
   ├── 👤 My Profile                  (/profile)
   └── ❓ Help & Support              (/support)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

### 3.4 Teacher Navigation (6 Groups)

```markdown
✨ EnjoyEveryday — My Classroom

▼ GROUP 1: MY WORKSPACE
   ├── ☀️ My Dashboard                (/)
   ├── 🗓️ Today's Schedule            (/today-activities)
   └── 🔔 Notifications              (/notifications)

▼ GROUP 2: MY CLASSROOM
   ├── 👶 Classroom Children          (/children)
   ├── ✅ Children's Attendance       (/management/attendance)
   └── 🗓️ Classroom Schedule & Events (/events)

▼ GROUP 3: DAILY CARE
   ├── ❤️ Daily Care & Wellbeing      (/daily-care)
   ├── 📋 Child Care Plans & Dietaries(/daily-care/plans)
   └── 📦 Cubby Supplies & Restock    (/daily-care/supplies)

▼ GROUP 4: ACTIVITIES
   ├── 🎨 Assigned Activities (Run)   (/today-activities)
   ├── 📚 Experience Library          (/experiences)
   └── 🌾 Harvest & Reflections       (/harvest)

▼ GROUP 5: CHILD DEVELOPMENT
   ├── 🌿 Child Journey               (/journey)
   └── 📸 Child Memories              (/memories)

▼ GROUP 6: MY ACCOUNT
   ├── 💬 Parent Communication        (/classroom-messages)
   ├── 📢 Daycare Announcements       (/announcements)
   ├── 🛡️ Safety & Emergency          [Submenu]
   │     ├── Emergency Floor Maps     (/safety-trust/floor-maps)
   │     └── Emergency Contacts (ERC) (/safety-trust/emergency-contacts)
   ├── 👤 My Profile                  (/profile)
   └── ❓ Help & Support              (/support)

───────────────────────────────────────────────────────
👤 User Profile & Workspace Switcher (Footer)
```

---

## 4. Technical Implementation Design for `NavMenu.razor`

### 4.1 Submenu State Management
Rather than ad-hoc individual boolean flags, submenus will be managed via a clean, reusable state container:
```csharp
private readonly HashSet<string> _openSubmenus = new(StringComparer.OrdinalIgnoreCase);

private bool IsSubmenuOpen(string submenuKey) => _openSubmenus.Contains(submenuKey);

private void ToggleSubmenu(string submenuKey)
{
    if (!_openSubmenus.Add(submenuKey))
    {
        _openSubmenus.Remove(submenuKey);
    }
}
```

### 4.2 Auto-Expansion on Location Changed
Whenever the user visits a URL, `AutoExpandCurrentRoute()` checks the active relative path:
1. If the route matches any child item in a submenu, that submenu key is added to `_openSubmenus`.
2. Existing open submenus remain open unless the user explicitly toggles them.
3. This guarantees zero jarring collapses when navigating between related pages.

### 4.3 Mobile Drawer Integration
All main menu links and submenu child links invoke `HandleNavItemClickAsync()`, cleanly triggering the cascading `CloseMobileMenu` callback so that mobile drawers close automatically upon selection.

---

## 5. Backward Compatibility & Authorization

1. **No Backend Security Alterations**: Every backend service continues to evaluate `TenantContext`, `UserContext`, and `AccessScopeRepo`.
2. **Zero Missing Routes**: All existing paths remain functional and reachable.
3. **Empty Group Suppression**: If a user role does not possess permissions for any item within a group, that group heading and its contents are omitted from the DOM.

---

## 6. Comparison: Previous vs Proposed Sidebar

| Feature | Previous Version | Proposed Multiple Groups & Submenus |
|:---|:---|:---|
| **Visual Weight** | Large accordion blocks with heavy header borders | Clean, airy section labels with generous vertical spacing |
| **Submenus** | Flat list inside each accordion group | Multi-level hierarchical grouping with subtle indentation and guide rails |
| **Discoverability**| 12-15 items stacked in one list | 3-5 tightly related items per group; complex workflows nested under submenus |
| **State Handling** | Monolithic boolean toggles | Independent `HashSet<string>` submenu state with persistent expandability |
| **Mobile Experience**| Requires scrolling through long accordion lists | Compact, lightweight sections with collapsible branch/admissions branches |

