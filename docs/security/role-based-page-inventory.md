# EnjoyEveryday — Complete Page-by-Page Role-Based UI and Authorization Inventory

**Document Version:** 1.1.0  
**Phase:** Phase 6 Complete — Implemented & Verified  
**Date:** 2026-10-28  
**Scope:** `src/EnjoyEveryday.Daycare.Web` and referenced shared UI / domain libraries  
**Status:** Complete — All Role Gates, Route Guards, Scoped Queries, and Action Protections Active  

---

## 1. Executive Summary & Assessment Overview

EnjoyEveryday is a multi-tenant early-childhood education management platform. The application has four primary operational roles within any daycare organization:
1. **Teacher (`DaycareTeacher` / `ClassroomTeacher`)**: Cares for children, executes experiences, logs routines, marks attendance, and views journeys strictly for assigned classroom(s).
2. **Lead Teacher (`LeadTeacher`)**: Performs all Teacher activities plus coordinates classroom readiness, reviews daily logs, manages handover, oversees experience execution, and submits staff coverage requests for assigned classroom(s).
3. **Branch Admin (`BranchAdmin`)**: Manages operational execution, attendance, safety, staffing assignments, and admissions strictly for assigned branch(es).
4. **Organization Admin (`OrganizationAdmin` / `OrganizationOwner`)**: Governs organization-wide health, multi-branch comparisons, billing, staff role assignments, organization settings, and compliance.
5. **Platform Admin (`PlatformAdmin`)**: Platform-level infrastructure, tenant provisioning, and global system health; strictly segregated with no automatic tenant business data access.

### Critical Security Findings During Inspection

1. **Missing Page-Level & Route-Level Authorization Gates**:
   - `_Imports.razor` declares `@attribute [Authorize]`, which requires only that a user is logged in.
   - **Zero Razor pages enforce `@attribute [Authorize(Roles = ...)]` or declarative policy gates.**
   - Any authenticated user (e.g., a Teacher) who manually types administrative URLs (such as `/management/billing`, `/billing/invoices`, `/settings`, `/management/roles`, `/management/branches/new`, or `/classrooms/new`) is granted access to the UI and can invoke underlying backend methods.

2. **Severe Insecure Direct Object References (IDOR) & Unscoped Mutations**:
   - `ClassroomProfile.razor` (`/classrooms/view/{Id:guid}`): A `DeleteClassroom` button is exposed to any authenticated user with no role, permission, or branch scope verification.
   - `TeacherProfile.razor` (`/teachers/view/{Id:guid}`): A `DeleteTeacher` button is rendered for any authenticated user without permission checks.
   - `BranchDetail.razor` (`/management/branches/{BranchId:guid}`): Any user can inspect any branch by modifying the URL GUID and can toggle the branch's active status.
   - `BranchAttendance.razor` (`/management/attendance/branches/{BranchName}`) & `ClassroomAttendance.razor` (`/management/attendance/classrooms/{ClassroomName}`): Routes accept names directly from the path with no validation that the user is assigned to that branch or classroom.

3. **Cross-Classroom & Cross-Branch Data Leakage**:
   - `DailyCarePlansPage.razor` (`/daily-care/plans`): Iterates over all branches and classrooms in the tenant, exposing sensitive medical notes, allergy information, and dietary requirements org-wide.
   - `DailyCareSuppliesPage.razor` (`/daily-care/supplies`): Displays cubby supply inventory across all classrooms without branch or room scoping.
   - `Memories.razor` (`/memories`): Filter dropdowns allow any user to select any branch and classroom to view media, bypassing media consent checks and classroom assignment boundaries.

4. **Mock Components Requiring Real Data or Gating**:
   - `CustomRoles.razor` (`/management/roles`): Uses hardcoded static mockup cards without database persistence.
   - `OrganizationTree.razor` (`/organization`): Displays hardcoded mock children ("Butterflies", "Sarah", "Maya") rather than dynamic tenant branch hierarchies.

---

## 2. Complete Page-by-Page Inventory Matrix

The inventory below covers every Razor page and component route in `EnjoyEveryday.Daycare.Web`.

| Page / Component | Route | Purpose | Current Access | Teacher Access | Lead Teacher Access | Branch Admin Access | Organization Admin Access | Platform Admin Access | Data Scope | Required Permissions | Services / Repositories | Issues & Risks |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| **Today** (`Today.razor`) | `/`, `/today` | Dynamic home dashboard resolving user's primary persona | Authenticated | Allowed (Teacher Workspace) | Allowed (Lead Teacher Workspace) | Allowed (Branch Dashboard) | Allowed (Org Dashboard) | Restricted (redirect or system view) | User Role & Assigned Scope | `attendance.view`, `care.view` | `ICurrentUserService`, `ITenantService`, `IClassroomRepository`, `IBranchRepository`, `IChildRepository` | Home page view switcher allows manual URL overriding; must restrict view selector to user's authorized roles only. |
| **TeacherWorkspace** (`TeacherWorkspace.razor`) | `/classroom`, `/my-classroom` | Daily classroom execution hub (attendance, activities, routines, moments) | Authenticated | Allowed | Allowed (with coordination shortcuts) | Restricted (view-only if assigned) | Restricted (read-only audit) | Denied | Assigned Classroom(s) | `attendance.record`, `care.record`, `experience.execute` | `ICurrentUserService`, `IClassroomRepository`, `IChildRepository`, `IAttendanceRepository` | If teacher is unassigned, needs clear empty state. Direct navigation currently allows any user. |
| **BranchAdminDashboard** (`BranchAdminDashboard.razor`) | `/management/branch-dashboard`, `/branch-dashboard` | Operational hub for branch attendance, safety, staffing, and issues | Authenticated | Denied | Denied | Allowed | Allowed (can select any branch) | Denied | Assigned Branch(es) | `branch.manage`, `attendance.view`, `staff.manage` | `IBranchRepository`, `IClassroomRepository`, `IAttendanceRepository`, `ISafetyAuditService` | Unassigned branch admin or unauthorized branch access must be blocked. Direct URL access not restricted. |
| **OrgAdminDashboard** (`OrgAdminDashboard.razor`) | `/management/dashboard`, `/org-dashboard` | Executive governance summary, branch health, admissions, compliance | Authenticated | Denied | Denied | Denied | Allowed | Denied | Active Organization | `organization.manage`, `insights.org.view`, `billing.view` | `IOrganizationRepository`, `IBranchRepository`, `IBillingService`, `IChildRepository` | Teachers and Branch Admins can type `/org-dashboard` directly and see org-wide high-level metrics. |
| **AttendanceOverview** (`AttendanceOverview.razor`) | `/attendance`, `/management/attendance` | Daily attendance roll call, summaries across classrooms and branches | Authenticated | Restricted (Assigned room only) | Restricted (Assigned room only) | Allowed (Assigned branch) | Allowed (Org-wide) | Denied | Role-based: Classroom / Branch / Org | `attendance.view`, `attendance.record` | `IAttendanceRepository`, `IClassroomRepository`, `IBranchRepository` | Currently loads org-wide or branch-wide data for any user without filtering by assigned room. |
| **BranchAttendance** (`BranchAttendance.razor`) | `/management/attendance/branches/{BranchName}` | Specific branch attendance ledger and logs | Authenticated | Denied | Denied | Allowed (Assigned branch only) | Allowed (All branches) | Denied | Branch Scope | `attendance.view`, `branch.manage` | `IAttendanceRepository`, `IBranchRepository` | Accepts `BranchName` from route without validating whether caller has assignment to that branch. |
| **ClassroomAttendance** (`ClassroomAttendance.razor`) | `/management/attendance/classrooms/{ClassroomName}` | Specific classroom check-in, check-out, and absence recording | Authenticated | Allowed (Assigned room only) | Allowed (Assigned room only) | Allowed (Assigned branch rooms) | Allowed (All rooms) | Denied | Classroom Scope | `attendance.record`, `attendance.view` | `IAttendanceRepository`, `IClassroomRepository` | Classroom name string parameter in URL; no verification that caller is assigned to classroom. |
| **Children** (`Children.razor`) | `/children` | Roster and directory of enrolled children | Authenticated | Restricted (Assigned classroom children only) | Restricted (Assigned classroom children only) | Allowed (Branch children only) | Allowed (Org-wide children) | Denied | Classroom / Branch / Org | `child.view` | `IChildRepository`, `IClassroomRepository` | Renders all children across the entire organization regardless of teacher classroom assignment. |
| **ChildEnrollment** (`ChildEnrollment.razor`) | `/children/add`, `/children/edit/{Id:guid}` | Register new child or edit existing child demographics and medical info | Authenticated | Denied | Denied | Allowed (Assigned branch) | Allowed (Org-wide) | Denied | Branch / Org Scope | `child.manage`, `child.enrollment.manage` | `IChildRepository`, `IClassroomRepository`, `IBranchRepository` | Teachers can navigate directly to `/children/add` and create or edit child records without permission. |
| **ChildJourney** (`ChildJourney.razor`) | `/journey`, `/journey/{ChildId:guid}` | Child developmental milestones, observations, learning portfolio | Authenticated | Allowed (Assigned child only) | Allowed (Assigned room children) | Allowed (Branch children) | Allowed (Org children with business reason) | Denied | Child / Classroom Scope | `child.view`, `journey.view.internal` | `IChildRepository`, `IObservationRepository`, `IJourneyRepository` | Direct ID parameter allows viewing journey of any child in tenant, bypassing classroom boundaries. |
| **EnrollmentPipeline** (`EnrollmentPipeline.razor`) | `/enrollment`, `/management/enrollment` | Admissions waitlist, tour schedules, application statuses | Authenticated | Denied | Denied | Allowed (Assigned branch) | Allowed (Org-wide) | Denied | Branch / Org Scope | `child.enrollment.manage` | `IEnrollmentRepository`, `IBranchRepository` | Teachers can view family inquiries, financial prospect info, and lead notes. |
| **Classrooms** (`Classrooms.razor`) | `/classrooms` | Directory of classrooms, capacity, age groups | Authenticated | Restricted (Assigned room only) | Restricted (Assigned room only) | Allowed (Branch rooms) | Allowed (Org rooms) | Denied | Classroom / Branch / Org | `classroom.manage` (edit), `child.view` (view) | `IClassroomRepository`, `IBranchRepository` | Shows all classrooms org-wide with "New Classroom" button visible to all. |
| **ClassroomProfile** (`ClassroomProfile.razor`) | `/classrooms/view/{Id:guid}` | Detailed classroom view: children roster, teacher team, schedules | Authenticated | Allowed (Assigned room only) | Allowed (Assigned room) | Allowed (Branch rooms) | Allowed (Org rooms) | Denied | Classroom / Branch / Org | `classroom.manage` (manage), `child.view` (view) | `IClassroomRepository`, `IChildRepository`, `ITeacherRepository` | Critical: "Delete Classroom" button rendered with no role/permission check. |
| **ClassroomWizard** (`ClassroomWizard.razor`) | `/classrooms/new`, `/classrooms/edit/{Id:guid}`, `/management/branches/{BranchId:guid}/classrooms/new` | Create or update classroom specifications, age ranges, capacity | Authenticated | Denied | Denied | Allowed (Assigned branch) | Allowed (Org-wide) | Denied | Branch / Org Scope | `classroom.manage` | `IClassroomRepository`, `IBranchRepository` | Any user can navigate to `/classrooms/new` and instantiate rooms in any branch. |
| **Teachers** (`Teachers.razor`) | `/teachers` | Directory of teachers, credentials, assignments | Authenticated | Restricted (Directory view only, no edit) | Restricted (Directory view only) | Allowed (Branch teachers) | Allowed (Org teachers) | Denied | Branch / Org Scope | `user.manage`, `staff.roster.view` | `ITeacherRepository`, `IBranchRepository` | Displays all teachers org-wide and provides "Add Teacher" button to unauthorized users. |
| **TeacherProfile** (`TeacherProfile.razor`) | `/teachers/view/{Id:guid}` | Teacher credentials, assigned classrooms, contact info, actions | Authenticated | Restricted (Self profile or peer view) | Restricted (Peer view) | Allowed (Branch teachers) | Allowed (Org teachers) | Denied | Branch / Org Scope | `user.manage` | `ITeacherRepository`, `IClassroomRepository` | Critical: "Delete Teacher" button is visible and actionable for any authenticated user. |
| **TeacherWizard** (`TeacherWizard.razor`) | `/teachers/new`, `/teachers/edit/{Id:guid}` | Create or edit teacher records, certifications, and room allocations | Authenticated | Denied | Denied | Allowed (Assigned branch) | Allowed (Org-wide) | Denied | Branch / Org Scope | `user.manage` | `ITeacherRepository`, `IBranchRepository`, `IClassroomRepository` | Teachers can create new teacher profiles or modify peer data directly. |
| **StaffRoster** (`StaffRoster.razor`) | `/management/roster` | Weekly shift schedules, classroom coverage, ratio compliance | Authenticated | Allowed (View own shift) | Allowed (View room team roster) | Allowed (Manage branch roster) | Allowed (Manage org roster) | Denied | Classroom / Branch / Org | `staff.roster.view` (view), `staff.leave.manage` (edit) | `ITeacherRepository`, `IClassroomRepository` | Teachers can see all branch staffing and administrative coverage notes. |
| **UsersByRole** (`UsersByRole.razor`) | `/users-by-role`, `/management/users-by-role` | User accounts, role assignments, security status | Authenticated | Denied | Denied | Restricted (View branch staff only) | Allowed (Full org user management) | Denied | Organization Scope | `user.manage`, `role.assign` | `ICurrentUserService`, `IOrganizationRepository` | Teachers can view all system users and roles; permission to reassign roles must be restricted to Org Admin / Branch Admin. |
| **CustomRoles** (`CustomRoles.razor`) | `/management/roles`, `/roles` | Role definition and permission configuration | Authenticated | Denied | Denied | Denied | Allowed (Org-level custom roles) | Denied | Organization Scope | `role.assign`, `platform.roles.manage` | In-memory mock cards | In-memory mockup component; unauthenticated access to modify roles must be gated to Org Admin only. |
| **BranchDetail** (`BranchDetail.razor`) | `/management/branches/{BranchId:guid}` | Branch facility info, operating hours, active status toggle | Authenticated | Denied | Denied | Allowed (Assigned branch only) | Allowed (All branches) | Denied | Branch Scope | `branch.manage` | `IBranchRepository`, `IClassroomRepository` | Critical: Any user can toggle active status of any branch via URL parameter tampering. |
| **CreateBranch** (`CreateBranch.razor`) | `/management/branches/new` | Multi-branch expansion: establish new daycare facility | Authenticated | Denied | Denied | Denied | Allowed | Denied | Organization Scope | `branch.manage`, `organization.manage` | `IBranchRepository`, `IOrganizationRepository` | Any user can navigate to route and create new branches. |
| **OrganizationTree** (`OrganizationTree.razor`) | `/organization`, `/organization/branches` | Organization visual tree showing branches, classrooms, and teachers | Authenticated | Denied | Denied | Restricted (Assigned branch sub-tree) | Allowed (Complete tree) | Denied | Organization Scope | `organization.manage`, `branch.manage` | In-memory mock hierarchy | Currently renders hardcoded mock tree; lacks dynamic database binding and role-based branch scoping. |
| **Management** (`Management.razor`) | `/management` | Administrative hub directing to branches, roster, billing, roles | Authenticated | Denied | Denied | Allowed (Branch tools) | Allowed (Org tools) | Denied | Organization Scope | `organization.manage`, `branch.manage` | N/A (Navigation Hub) | Teachers can open this hub and see administrative links. |
| **Settings** (`Settings.razor`) | `/settings`, `/organization/settings`, `/organization/profile` | Organization profile, brand theme, timezones, operating policies | Authenticated | Denied | Denied | Denied | Allowed | Denied | Organization Scope | `organization.manage` | `IOrganizationRepository` | Any authenticated user can modify daycare brand settings, address, and legal details. |
| **OrganizationBilling** (`OrganizationBilling.razor`) | `/management/billing`, `/billing`, `/billing/invoices`, `/billing/reports` | Invoicing, fee structures, parent payments, cash flow summaries | Authenticated | Denied | Denied | Denied | Allowed | Denied | Organization Scope | `billing.view` | `IBillingService` | Financial records and billing plans are fully accessible to teachers via direct URL. |
| **Experiences** (`Experiences.razor`) | `/experiences` | Pedagogical activity library, curriculum catalog | Authenticated | Allowed | Allowed | Allowed | Allowed | Denied | Organization Scope | `experience.view` | `IExperienceRepository` | All roles may view library; curriculum editing and approval should be restricted. |
| **ExperienceStudio** (`ExperienceStudio.razor`) | `/studio`, `/studio/{ExperienceId:guid}` | Curriculum authoring tool, lesson step creator, media attachment | Authenticated | Restricted (Draft own activities) | Allowed (Draft & submit) | Allowed (Approve & schedule) | Allowed (Full authoring) | Denied | Organization Scope | `experience.create`, `experience.plan.approve` | `IExperienceRepository` | Regular teachers can edit or publish shared organization curriculum without approval. |
| **ExperiencePlanning** (`ExperiencePlanning.razor`) | `/planning` | Weekly classroom activity schedule and lesson plan submission | Authenticated | Allowed (Assigned room) | Allowed (Assigned room, coordinate) | Allowed (Branch overview) | Allowed (Org overview) | Denied | Classroom / Branch Scope | `experience.schedule`, `experience.plan.submit` | `IExperienceRepository`, `IClassroomRepository` | Should lock scheduling to assigned classrooms for Teachers and Lead Teachers. |
| **MonthlyPlanner** (`MonthlyPlanner.razor`) | `/planner` | Monthly curriculum calendar and theme planning | Authenticated | Restricted (View only) | Allowed (Classroom schedule) | Allowed (Branch calendar) | Allowed (Org calendar) | Denied | Classroom / Branch Scope | `experience.schedule` | `IExperienceRepository` | Lacks classroom and branch filter boundary checks. |
| **Planner** (`Planner.razor`) | `/planner-old` | Legacy curriculum planner | Authenticated | Restricted (Deprecated) | Restricted (Deprecated) | Restricted (Deprecated) | Restricted (Deprecated) | Denied | Organization Scope | `experience.schedule` | `IExperienceRepository` | Legacy component; should redirect to `/planning` with appropriate access checks. |
| **Harvest** (`Harvest.razor`) | `/harvest` | Learning reflection, child milestone tagging, outcome documentation | Authenticated | Allowed (Assigned children) | Allowed (Assigned children) | Allowed (Branch children) | Allowed (Org children) | Denied | Classroom Scope | `child.observe`, `experience.execute` | `IExperienceRepository`, `IChildRepository` | Needs validation that child records being tagged belong to the teacher's classroom. |
| **EventDetails** (`EventDetails.razor`) | `/events/{Id:guid}` | Event specifications, parent RSVPs, staffing for events | Authenticated | Allowed (Assigned branch events) | Allowed (Assigned branch events) | Allowed (Manage branch events) | Allowed (Manage org events) | Denied | Branch / Org Scope | `event.manage`, `experience.view` | `IEventRepository` | Event creation/cancellation not protected against unauthorized role execution. |
| **Events** (`Events.razor`) | `/events` | Center calendar: field trips, parent meetings, holidays | Authenticated | Allowed (View branch events) | Allowed (View branch events) | Allowed (Manage branch events) | Allowed (Manage org events) | Denied | Branch / Org Scope | `experience.view` | `IEventRepository` | "Add Event" button rendered for teachers without `event.create` permission check. |
| **ExperienceStorybook** (`ExperienceStorybook.razor`) | `/experiences/{ExperienceId:guid}/story`, `/experiences/story/{ExperienceId:guid}` | Activity storybook narrative and photos | Authenticated | Allowed | Allowed | Allowed | Allowed | Denied | Organization Scope | `experience.view` | `IExperienceRepository` | Read-only pedagogical content; safe for all daycare roles. |
| **ParentStories** (`ParentStories.razor`) | `/stories` | Published classroom stories shared with parents | Authenticated | Allowed (Assigned room stories) | Allowed (Assigned room stories) | Allowed (Branch stories) | Allowed (Org stories) | Denied | Classroom / Branch Scope | `journey.view.internal`, `journey.publish` | `IStoryRepository` | Stories from all classrooms visible; must scope to teacher's room or branch. |
| **Stories** (`Stories.razor`) | `/stories-old` | Legacy parent stories component | Authenticated | Deprecated | Deprecated | Deprecated | Deprecated | Denied | Classroom Scope | `journey.view.internal` | `IStoryRepository` | Deprecated page; should be retired or gated. |
| **Memories** (`Memories.razor`) | `/memories` | Photo gallery, moment captures, parent-facing timeline | Authenticated | Allowed (Assigned room moments) | Allowed (Assigned room moments) | Allowed (Branch moments) | Allowed (Org moments) | Denied | Classroom / Branch Scope | `child.observe`, `consent.view` | `IMediaRepository`, `IChildRepository` | Critical: Dropdown permits selecting any branch and classroom; photo consent status is not verified prior to rendering photos. |
| **Marketplace** (`Marketplace.razor`) | `/marketplace` | Learning materials exchange & ideas | Authenticated | Allowed | Allowed | Allowed | Allowed | Denied | Platform / Organization | `materials.view` | `IMaterialsRepository` | Safe for all roles to view; orders must be restricted. |
| **MaterialsMarketplace** (`MaterialsMarketplace.razor`) | `/materials-marketplace` | Daycare supply catalog and curriculum kits | Authenticated | Allowed (View kits) | Allowed (Requisition request) | Allowed (Approve orders) | Allowed (Approve orders) | Denied | Organization Scope | `materials.view`, `materials.order` | `IMaterialsRepository` | Order approval should be restricted to Branch Admin and Org Admin. |
| **MaterialOrdersPage** (`MaterialOrdersPage.razor`) | `/materials-orders` | Purchase orders, shipment tracking, classroom allocations | Authenticated | Restricted (View own orders) | Allowed (Classroom requisitions) | Allowed (Branch orders) | Allowed (Org orders) | Denied | Branch / Org Scope | `materials.order` | `IMaterialsRepository` | Teachers can see financial costs and invoices for all material orders. |
| **DailyCarePage** (`DailyCarePage.razor`) | `/daily-care` | Daily routine logger: feeding, diapering, naps, medications | Authenticated | Allowed (Assigned room) | Allowed (Assigned room, review team logs) | Allowed (Branch overview) | Allowed (Org audit) | Denied | Classroom Scope | `care.record`, `care.view` | `IDailyCareRepository`, `IClassroomRepository`, `IChildRepository` | Must scope classroom selector to teacher's assigned classroom(s). |
| **DailyCarePlansPage** (`DailyCarePlansPage.razor`) | `/daily-care/plans` | Individual care plans, allergies, dietary constraints | Authenticated | Allowed (Assigned room children) | Allowed (Assigned room children) | Allowed (Branch children) | Allowed (Org children) | Denied | Classroom / Branch Scope | `care.view`, `care.plan.manage` | `IDailyCareRepository`, `IChildRepository` | Critical: Iterates over all branches and displays medical/allergy data for all children across the entire organization. |
| **DailyCareSuppliesPage** (`DailyCareSuppliesPage.razor`) | `/daily-care/supplies` | Diaper, wipe, and extra clothing inventory by child cubby | Authenticated | Allowed (Assigned room) | Allowed (Assigned room) | Allowed (Branch classrooms) | Allowed (Org classrooms) | Denied | Classroom / Branch Scope | `care.view` | `IDailyCareRepository`, `IClassroomRepository` | Shows all classrooms and children across the tenant with no room filter enforcement. |
| **ClassroomMessages** (`ClassroomMessages.razor`) | `/classroom-messages`, `/messages` | Direct messaging between teachers and parents, classroom broadcasts | Authenticated | Allowed (Assigned room families) | Allowed (Assigned room families) | Allowed (Branch families) | Allowed (Audit view) | Denied | Classroom / Family Scope | `comms.view`, `comms.send` | `IMessageRepository`, `IClassroomRepository` | Must ensure teachers cannot view conversations or send messages outside their assigned room. |
| **SafetyTrust** (`SafetyTrust.razor`) | `/safety-trust`, `/safety-compliance`, `/policies` | Safety checklists, compliance audit status, fire drills | Authenticated | Restricted (View protocols) | Allowed (Classroom safety checklist) | Allowed (Branch compliance manager) | Allowed (Org compliance officer) | Denied | Branch / Org Scope | `safety.report`, `safety.manage` | `ISafetyAuditService` | Teachers can modify or sign off on center-wide safety compliance audits. |
| **EmergencyContactsPage** (`EmergencyContactsPage.razor`) | `/safety-trust/emergency-contacts` | Emergency phone numbers, hospitals, police, pediatric contacts | Authenticated | Allowed (View branch contacts) | Allowed (View branch contacts) | Allowed (Manage branch contacts) | Allowed (Manage org contacts) | Denied | Branch Scope | `safety.map.view`, `emergency.contacts.manage` | `ISafetyAuditService` | All staff may view emergency numbers; editing must be restricted to Branch and Org Admins. |
| **EmergencyFloorMapsPage** (`EmergencyFloorMapsPage.razor`) | `/safety-trust/floor-maps` | Evacuation routes, fire extinguisher maps, storm shelters | Authenticated | Allowed (Assigned branch floor maps) | Allowed (Assigned branch floor maps) | Allowed (Manage branch maps) | Allowed (Manage org maps) | Denied | Branch Scope | `safety.map.view`, `safety.map.manage` | `ISafetyAuditService` | Staff must only see floor maps for their assigned branch. Map uploading must be restricted. |
| **ConsentSigningPage** (`ConsentSigningPage.razor`) | `/consent/sign/{Token}` | Parent public token signing page for photo and emergency consent | Anonymous / Token | Allowed (via valid Token) | Allowed (via valid Token) | Allowed (via valid Token) | Allowed (via valid Token) | N/A | Specific Consent Token | Token Validation | `IConsentRepository` | Must ensure cryptographic token validation cannot be bypassed or tampered with. |
| **Intelligence** (`Intelligence.razor`) | `/intelligence`, `/insights` | Classroom sentiment analysis, AI recommendations, attendance forecasts | Authenticated | Denied | Restricted (Classroom insights) | Allowed (Branch insights) | Allowed (Org-wide insights) | Denied | Classroom / Branch / Org | `insights.org.view`, `insights.branch.view`, `insights.classroom.view` | `IIntelligenceService` | High-level organizational intelligence currently accessible to all users. |
| **RhythmHistory** (`RhythmHistory.razor`) | `/rhythm-history`, `/audit` | System audit trail, access logs, critical operational changes | Authenticated | Denied | Denied | Restricted (Branch audit logs) | Allowed (Org audit logs) | Denied | Organization Scope | `audit.view` | `IAuditRepository` | Teachers can currently access security and administrative audit trails. |
| **Notifications** (`Notifications.razor`) | `/notifications` | Notification inbox for user-specific events and alerts | Authenticated | Allowed (Personal notifications) | Allowed (Personal & room notifications) | Allowed (Personal & branch notifications) | Allowed (Personal & org notifications) | Denied | User / Scope | Authenticated | `INotificationService` | Scoped by user ID; safe. |
| **UserProfile** (`UserProfile.razor`) | `/profile` | Current user profile, credentials, password reset | Authenticated | Allowed (Self) | Allowed (Self) | Allowed (Self) | Allowed (Self) | Allowed (Self) | User Scope | Authenticated | `ICurrentUserService` | Personal profile; safe. |
| **SelectOrganization** (`SelectOrganization.razor`) | `/select-organization` | Multi-organization switcher for users with multiple memberships | Authenticated | Allowed (Assigned orgs) | Allowed (Assigned orgs) | Allowed (Assigned orgs) | Allowed (Assigned orgs) | Allowed (All orgs) | User Memberships | Authenticated | `ITenantService`, `ICurrentUserService` | Must validate that switched tenant belongs to user's active memberships. |
| **Support** (`Support.razor`) | `/support` | Platform support documentation and contact | Authenticated | Allowed | Allowed | Allowed | Allowed | Allowed | Public / Tenant | Authenticated | N/A | General help page; safe for all users. |
| **LoginPrompt** (`LoginPrompt.razor`) | `/login` | Authentication challenge and login redirect | Anonymous | Allowed | Allowed | Allowed | Allowed | Allowed | Platform | Anonymous | Authentication Provider | Standard login page; safe. |
| **AccessDenied** (`AccessDenied.razor`) | `/access-denied` | 403 Forbidden explanation with friendly back navigation | Authenticated | Allowed | Allowed | Allowed | Allowed | Allowed | N/A | N/A | N/A | Standard error page; safe. |
| **NotFound** (`NotFound.razor`) | `/not-found` | 404 Route Not Found page | Authenticated / Anonymous | Allowed | Allowed | Allowed | Allowed | Allowed | N/A | N/A | N/A | Standard 404 page; safe. |
| **GridDemo** (`GridDemo.razor`) | `/grid-demo` | Internal UI grid layout testing and styling sandbox | Authenticated | Denied | Denied | Denied | Denied | Allowed (Development only) | Development | `platform.configure` | N/A | Test harness; should be gated or excluded from production builds. |
| **Error** (`Error.razor`) | `/Error` | Unhandled exception page | Authenticated / Anonymous | Allowed | Allowed | Allowed | Allowed | Allowed | N/A | N/A | N/A | Standard error page; safe. |

---

## 3. Existing Identity, Role, and Membership Architecture

### 3.1 Role Hierarchy & Templates

EnjoyEveryday defines the following canonical role templates in `src/EnjoyEveryday.Shared/Authorization/RoleTemplates.cs`:

1. **`PlatformAdmin`** (Scope: `RoleScope.Platform`)
   - Platform operations, tenant onboarding, global health.
   - Strictly isolated from tenant business data.
2. **`OrganizationOwner` & `OrganizationAdmin`** (Scope: `RoleScope.Organization`)
   - Complete governance across all branches in the active organization.
   - Permissions: `organization.manage`, `billing.view`, `branch.manage`, `classroom.manage`, `user.manage`, `role.assign`, `audit.view`, `child.manage`, `experience.create`, `experience.approve`, `attendance.approve`, `safety.manage`, etc.
3. **`BranchAdmin`** (Scope: `RoleScope.Branch`)
   - Daily operational management for assigned branches.
   - Permissions: `branch.manage`, `classroom.manage`, `staff.manage`, `child.manage`, `child.enrollment.manage`, `attendance.view`, `attendance.record`, `attendance.approve`, `care.plan.manage`, `safety.report`, `consent.issue`, `insights.branch.view`, etc.
4. **`LeadTeacher`** (Scope: `RoleScope.Classroom`)
   - Classroom teaching plus team coordination, readiness, and daily log review.
   - Permissions: `child.view`, `experience.view`, `experience.schedule`, `experience.plan.submit`, `experience.plan.approve`, `experience.execute`, `child.observe`, `observation.approve`, `ai.use`, `attendance.view`, `attendance.record`, `care.view`, `care.record`, `care.plan.manage`, `care.handover`, `safety.map.view`, `safety.report`, `consent.view`, `comms.draft`, `comms.view`, `materials.view`, `staff.roster.view`.
5. **`ClassroomTeacher` / `DaycareTeacher`** (Scope: `RoleScope.Classroom`)
   - Direct teaching and child care in assigned classrooms.
   - Permissions: `child.view`, `experience.view`, `experience.schedule`, `experience.plan.submit`, `experience.execute`, `child.observe`, `ai.use`, `attendance.view`, `attendance.record`, `care.view`, `care.record`, `safety.map.view`, `safety.report`, `consent.view`, `comms.draft`, `comms.view`, `materials.view`.
6. **`Parent`** (Scope: `RoleScope.Family`)
   - Parent portal access strictly bounded to verified family relationships.
   - Permissions: `child.view`, `family.view`, `safety.report`, `consent.view`, `comms.view`.

### 3.2 Assignment & Boundary Models

Access boundaries depend on three database relationship tables:
1. **Branch Assignment**: `user_branch_assignments` links a user to one or more `branches` in an organization.
2. **Classroom Assignment**: `classroom_teachers` links a teacher to one or more `classrooms` in a branch.
3. **Family Relationship**: `child_families` / `family_guardians` links parents to specific children with consent flags.

---

## 4. Key Security Gaps & Identified Risks

| Risk Category | Severity | Description | Recommended Mitigation |
|:---|:---:|:---|:---|
| **Route Authorization Bypass** | **Critical** | Razor pages only check authentication (`[Authorize]`), not role or permission. Any user typing a URL accesses restricted administrative pages. | Add declarative `@attribute [Authorize(Policy = ...)]` or enforce programmatic permission checks in `OnInitializedAsync` redirecting unauthorized users to `/access-denied`. |
| **Insecure Direct Object Reference (IDOR)** | **Critical** | Pages accepting `{Id:guid}` or `{BranchName}` perform actions (such as deletion, active status toggling, or roster changes) without verifying caller ownership. | Validate that the target resource belongs to the user's active organization and authorized branch/classroom before executing queries or mutations. |
| **Unrestricted Navigation Menus** | **High** | Menus and buttons (e.g., "Add Branch", "Add Classroom", "Delete Teacher", "Billing") are visible to users lacking required permissions. | Wrap navigation items and sensitive action buttons in `<AuthorizeView>` or conditional blocks based on effective permissions. |
| **Cross-Classroom PII Exposure** | **High** | Care plans, medical notes, allergies, and cubby supplies are rendered org-wide without classroom assignment boundaries. | Enforce classroom-scoped queries for Teacher and Lead Teacher personas. |
| **Media Consent Bypass** | **High** | The Memories gallery allows browsing photos across all branches and classrooms without verifying parental photo consent. | Filter memories by classroom assignment and verify `consent.view` and parental consent flags before rendering images. |
| **Role-Switching Tampering** | **Medium** | The URL query `?view=...` in `Today.razor` allows any user to simulate another role's dashboard. | Restrict view-switching strictly to roles the authenticated user actually holds in the active organization. |

---

## 5. Proposed Implementation Plan (Phases 1 – 6)

### Phase 1: Define Role and Permission Enforcement Infrastructure
1. Implement a unified `IPageAuthorizationService` in `EnjoyEveryday.Daycare.Web` that checks effective user permissions and resource scopes (`Classroom`, `Branch`, `Organization`).
2. Define authorization policies matching the permission matrix (`RequireOrgAdmin`, `RequireBranchAdmin`, `RequireLeadTeacher`, `RequireTeacher`, `RequireBillingView`, `RequireStaffManage`).

### Phase 2: Refactor Role-Specific Dashboards & Dynamic Dispatcher
1. Secure `Today.razor` so that view-switching validates the caller's actual roles.
2. Provide personalized, friendly empty states when a Teacher has no assigned classroom or a Branch Admin has no assigned branch.
3. Verify that each of the four role-specific dashboards (`TeacherWorkspace`, `LeadTeacher`, `BranchAdminDashboard`, `OrgAdminDashboard`) loads only scoped data.

### Phase 3: Dynamic Role-Based Navigation (`NavMenu.razor`)
1. Update `NavMenu.razor` so menu groups and individual links render conditionally based on user permissions:
   - **Teacher**: My Classroom, Attendance, Daily Care, Messages, Experience Planning, Memories.
   - **Lead Teacher**: Teacher items + Coordination (Classroom Readiness, Team Tasks, Daily Log Review).
   - **Branch Admin**: Branch Dashboard, Branch Attendance, Classrooms & Roster, Enrollment, Safety Checks.
   - **Organization Admin**: Full administrative menu (Org Dashboard, All Branches, Billing, Staff & Roles, Settings, Audit).
2. Hide action buttons (e.g., "+ Add Teacher", "+ Add Classroom", "+ Create Branch") from unauthorized users.

### Phase 4: Secure Every Page and Mutation Action
1. Add server-side authorization checks to all ~55 routes:
   - For administrative pages (`/management/*`, `/billing/*`, `/settings/*`), deny access to Teachers and Lead Teachers.
   - For branch-scoped pages (`/management/branches/{BranchId}`, `/management/attendance/branches/{BranchName}`), verify caller branch assignment.
   - For classroom-scoped pages (`/classrooms/view/{Id}`, `/management/attendance/classrooms/{ClassroomName}`), verify caller classroom assignment.
2. Secure deletion and mutation buttons (`DeleteClassroom`, `DeleteTeacher`, `ToggleActiveBranch`).
3. Fix cross-classroom data leaks in `DailyCarePlansPage.razor`, `DailyCareSuppliesPage.razor`, and `Memories.razor`.

### Phase 5: Handle Edge Cases
1. Teacher with zero classroom assignments sees a warm empty state ("No classroom assigned yet. Please contact your Branch Administrator.").
2. Multi-classroom teachers have a clean room selector dropdown scoped strictly to their assigned rooms.
3. Multi-branch administrators can switch only between authorized branches.
4. Revoked or suspended memberships immediately trigger redirect to `/access-denied`.

### Phase 6: Verification & Protection
1. Verify targeted builds using `dotnet build src/EnjoyEveryday.Daycare.Web/EnjoyEveryday.Daycare.Web.csproj --no-restore`.
2. Conduct manual role-based walkthroughs for Teacher, Lead Teacher, Branch Admin, and Org Admin.
3. Update security documentation and verify that existing working features remain fully intact.
