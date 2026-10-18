# EnjoyEveryday — Maintainability & Modularity Findings

## 1. High-Impact Maintainability Issues

### Issue 1: 20 Entire Pages Duplicated Between Daycare.Web and Admin.Web
Rather than extracting reusable page templates into `EnjoyEveryday.UI.Shared`, 20 entire pages were copy-pasted between the daycare web application and the admin web application. Over time, edits have diverged between copies.

| Page Name | Daycare.Web Lines | Admin.Web Lines | Divergence / Risk |
|:---|---:|---:|:---|
| **Experiences/ExperienceStudio.razor** | 916 | 880 | Studio controls diverged; bug fixes in Daycare are missing in Admin. |
| **Experiences/Experiences.razor** | 755 | 682 | Filters and licensing actions diverged. |
| **Children/Children.razor** | 210 | 129 | Admin version lacks recent classroom filtering logic. |
| **Organization/Teachers.razor** | 442 | 412 | Teacher profile modal and role assignment logic duplicated. |
| **Organization/Classrooms.razor** | 385 | 350 | Capacity and ratio compliance logic duplicated. |
| **Organization/Management.razor** | 512 | 490 | Branch administration duplicated. |
| **Experiences/MonthlyPlanner.razor** | 620 | 580 | Calendar view duplicated. |
| **Experiences/Planner.razor** | 530 | 510 | Weekly schedule duplicated. |
| **Experiences/Harvest.razor** | 410 | 390 | Story harvesting duplicated. |
| **Experiences/Stories.razor** | 340 | 330 | Storybook viewer duplicated. |
| **Experiences/ParentStories.razor** | 310 | 310 | Parent stories duplicated. |
| **Children/ChildJourney.razor** | 741 | 710 | Milestone timeline duplicated. |
| **Organization/Settings.razor** | 290 | 280 | Tenant settings duplicated. |
| **Today.razor** | 640 | 620 | Daily rhythm dashboard duplicated. |
| **SelectOrganization.razor** | 180 | 180 | Org switcher duplicated. |
| **LoginPrompt.razor** | 95 | 95 | Login prompt duplicated. |
| **AccessDenied.razor** | 80 | 80 | Error page duplicated. |
| **NotFound.razor** | 75 | 75 | 404 page duplicated. |
| **Error.razor** | 60 | 60 | Error page duplicated. |
| **Home.razor** | 85 | 85 | Landing page duplicated. |

**Total Duplication Overhead**: Over **7,500 lines** of duplicate Razor markup and C# logic. Any bug fix or feature addition must be manually applied twice, guaranteeing future regressions.

---

### Issue 2: Extreme Razor Component Bloat & Zero Code-Behind Files
Across the entire repository (265 `.razor` files), there is **not a single `.razor.cs` code-behind file**. Every component inlines its markup, C# state, lifecycle overrides (`OnInitializedAsync`, `OnParametersSetAsync`), dialog event listeners, and CSS `<style>` blocks into a single file.

#### Top 10 Monolithic Razor Files:
1. `WarehouseInventoryPage.razor`: 1,418 lines (62.3 KB)
2. `MaterialOrdersPage.razor`: 1,313 lines (55.3 KB)
3. `BillingPage.razor`: 1,196 lines (65.1 KB)
4. `EnrollmentPipeline.razor`: 1,165 lines (49.8 KB)
5. `AccountBookPage.razor`: 1,116 lines (40.3 KB)
6. `ChildDailyCareSection.razor`: 1,094 lines (53.8 KB)
7. `Memories.razor`: 1,063 lines (34.8 KB)
8. `MarketplaceOperationsPage.razor`: 985 lines (43.6 KB)
9. `ExperiencePlanning.razor`: 965 lines (54.8 KB)
10. `EmergencyFloorMapsSection.razor`: 935 lines (45.9 KB)

**Impact on Maintainability**:
- Code reviews are unwieldy due to 1,000+ line diffs.
- Visual designers cannot edit HTML without risking broken C# syntax.
- Unit testing UI component logic without rendering the markup is impossible because the logic is trapped inside the `.razor` file.

---

### Issue 3: Architectural Duality & Direct SQL in 10 Application Services
The solution has two competing data access architectures:
1. **Repository Pattern (Canonical Clean Architecture)**:
   Used by `ChildService`, `BillingService`, `ConsentService`, `FloorMapService`, which depend on repository interfaces in `Domain` implemented in `Infrastructure`.
2. **Direct SQL in Application Services (Anti-Pattern)**:
   Ten major services bypass repositories completely, injecting `IDbConnectionFactory` and running inline Dapper queries:
   - `StaffRosterService.cs` (435 lines)
   - `DaycareOperationsBoardService.cs` (666 lines)
   - `DashboardService.cs` (380 lines)
   - `BranchBenchmarkingService.cs` (340 lines)
   - `MemoryService.cs` (497 lines)
   - `ExecutionService.cs` (390 lines)
   - `ChildDevelopmentPortfolioService.cs` (514 lines)
   - `EnrollmentPipelineService.cs` (476 lines)
   - `SecurePickupService.cs` (385 lines)
   - `PlatformAdminService.cs` (965 lines)

**Impact**:
- `EnjoyEveryday.Application` cannot be decoupled from PostgreSQL or Dapper.
- Unit testing these 10 services requires either mocking raw Dapper extension methods (notoriously difficult) or spinning up a live PostgreSQL database.
- Database schema changes require searching both `Infrastructure/Repositories` and `Application/Services` to find affected queries.

---

### Issue 4: Orphaned Python Scripts in Repository Root
The repository root contains **25+ ad-hoc Python scripts** used during past development sessions to generate code or apply patches:
`add_view_modal.py`, `create_attendance_components.py`, `create_test.py`, `fix_all.py`, `fix_api.py`, `fix_bugs.py`, `fix_cascade_delete.py`, `fix_duplicate.py`, `fix_exp_service.py`, `fix_modal_buttons.py`, `fix_modal_vars.py`, `fix_planner.py`, `fix_planner_cls.py`, `fix_planner_cls2.py`, `fix_planner_fallback.py`, `fix_refactor.py`, `patch_attendance.py`, `patch_attendance_phase2.py`, `patch_branch_wireframe.py`, `patch_classroom.py`, `patch_experiences.py`, `patch_overview_classrooms.py`, `patch_session_attendance.py`, `patch_wireframe.py`, `refactor.py`, `rewrite_modal.py`, `use_attendance_components.py`.

**Impact**:
- Creates extreme visual clutter for new developers.
- Unclear whether these scripts are required for deployment, database migrations, or maintenance.
- Pollutes Git commit history.
