# EnjoyEveryday - Complete Blazor Page & Component Inventory

This document catalogs every Blazor page and significant Razor component across the three EnjoyEveryday host applications (`EnjoyEveryday.Daycare.Web`, `EnjoyEveryday.Admin.Web`, `EnjoyEveryday.Parent.Web`) and the shared component library (`EnjoyEveryday.UI.Shared`).

## Summary Statistics

- **Total Discovered Pages**: 102
- **Daycare.Web Pages**: 54
- **Admin.Web Pages**: 40
- **Parent.Web Pages**: 8
- **UI.Shared Reusable Components**: 147 components, 7 views

---

## Page Inventory Matrix

| # | Project | Page | Route(s) | Total Lines | Complexity | Risk | Status |
|---|---|---|---|---|---|---|---|
| 1 | Admin.Web | [BillingPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/BillingPage.razor) | `/billing` | 1253 | Very High | High | Refactoring Planned |
| 2 | Admin.Web | [WarehouseInventoryPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/WarehouseInventoryPage.razor) | `/warehouse-inventory` | 1033 | Very High | High | Refactoring Planned |
| 3 | Daycare.Web | [ExperiencePlanning.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperiencePlanning.razor) | `/planning` | 1020 | Very High | High | Refactoring Planned |
| 4 | Daycare.Web | [RhythmHistory.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/RhythmHistory.razor) | `/rhythm-history` | 877 | High | Medium | Refactoring Planned |
| 5 | Daycare.Web | [ExperienceStudio.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStudio.razor) | `/studio, /studio/{ExperienceId:guid}` | 870 | High | Medium | Refactoring Planned |
| 6 | Daycare.Web | [Intelligence.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Intelligence/Intelligence.razor) | `/intelligence` | 857 | High | Medium | Refactoring Planned |
| 7 | Daycare.Web | [MaterialOrdersPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MaterialOrdersPage.razor) | `/materials-orders` | 850 | High | Medium | Refactoring Planned |
| 8 | Admin.Web | [ExperienceStudio.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/ExperienceStudio.razor) | `/studio, /studio/{ExperienceId:guid}` | 832 | High | Medium | Refactoring Planned |
| 9 | Daycare.Web | [EnrollmentPipeline.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/EnrollmentPipeline.razor) | `/enrollment, /management/enrollment` | 805 | High | Medium | Refactoring Planned |
| 10 | Daycare.Web | [StaffRoster.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/StaffRoster.razor) | `/management/roster` | 805 | High | Medium | Refactoring Planned |
| 11 | Admin.Web | [MarketplaceOperationsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/MarketplaceOperationsPage.razor) | `/marketplace-operations` | 788 | High | Medium | Refactoring Planned |
| 12 | Daycare.Web | [Experiences.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Experiences.razor) | `/experiences` | 652 | High | Medium | Reviewed |
| 13 | Daycare.Web | [EventDetails.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/EventDetails.razor) | `/events/{Id:guid}` | 639 | High | Medium | Reviewed |
| 14 | Daycare.Web | [MaterialsMarketplace.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MaterialsMarketplace.razor) | `/materials-marketplace` | 637 | High | Medium | Reviewed |
| 15 | Daycare.Web | [Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Today.razor) | `/, /today` | 624 | High | Medium | Reviewed |
| 16 | Admin.Web | [PlatformDashboard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Dashboard/PlatformDashboard.razor) | `/, /dashboard` | 620 | High | Medium | Reviewed |
| 17 | Daycare.Web | [OrganizationBilling.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationBilling.razor) | `/management/billing, /billing` | 613 | High | Medium | Reviewed |
| 18 | Admin.Web | [AccountBookPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/AccountBookPage.razor) | `/account-book` | 596 | Medium | Low | Reviewed |
| 19 | Daycare.Web | [ChildEnrollment.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildEnrollment.razor) | `/children/add, /children/edit/{Id:guid}` | 595 | Medium | Low | Reviewed |
| 20 | Admin.Web | [OrganizationsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/OrganizationsPage.razor) | `/organizations` | 558 | Medium | Low | Reviewed |
| 21 | Daycare.Web | [Events.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Events.razor) | `/events` | 551 | Medium | Low | Reviewed |
| 22 | Daycare.Web | [ExperienceStorybook.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStorybook.razor) | `/experiences/{ExperienceId:guid}/story, /experiences/story/{ExperienceId:guid}` | 531 | Medium | Low | Reviewed |
| 23 | Daycare.Web | [Memories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Memories.razor) | `/memories` | 513 | Medium | Low | Reviewed |
| 24 | Admin.Web | [PlansPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/PlansPage.razor) | `/plans` | 462 | Medium | Low | Reviewed |
| 25 | Daycare.Web | [UsersByRole.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/UsersByRole.razor) | `/users-by-role, /management/users-by-role` | 453 | Medium | Low | Reviewed |
| 26 | Daycare.Web | [OrganizationTree.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationTree.razor) | `/organization` | 449 | Medium | Low | Reviewed |
| 27 | Daycare.Web | [UserProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/UserProfile.razor) | `/profile` | 448 | Medium | Low | Reviewed |
| 28 | Admin.Web | [PlatformCurationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/PlatformCurationPage.razor) | `/platform-curation` | 398 | Medium | Low | Reviewed |
| 29 | Admin.Web | [Experiences.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Experiences.razor) | `/experiences` | 377 | Medium | Low | Reviewed |
| 30 | Admin.Web | [MonthlyPlanner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/MonthlyPlanner.razor) | `/planner` | 352 | Medium | Low | Reviewed |
| 31 | Parent.Web | [Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Today.razor) | `/, /today` | 348 | Medium | Low | Reviewed |
| 32 | Daycare.Web | [ConsentSigningPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/ConsentSigningPage.razor) | `/consent/sign/{Token}` | 291 | Low | Low | Reviewed |
| 33 | Daycare.Web | [Marketplace.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Marketplace.razor) | `/marketplace` | 278 | Low | Low | Reviewed |
| 34 | Admin.Web | [PlatformMarketplacePage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/PlatformMarketplacePage.razor) | `/platform-marketplace` | 261 | Low | Low | Reviewed |
| 35 | Daycare.Web | [ClassroomProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/ClassroomProfile.razor) | `/classrooms/view/{Id:guid}` | 260 | Low | Low | Reviewed |
| 36 | Daycare.Web | [BranchDetail.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/BranchDetail.razor) | `/management/branches/{BranchId:guid}` | 255 | Low | Low | Reviewed |
| 37 | Daycare.Web | [TeacherProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/TeacherProfile.razor) | `/teachers/view/{Id:guid}` | 236 | Low | Low | Reviewed |
| 38 | Daycare.Web | [MonthlyPlanner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MonthlyPlanner.razor) | `/planner` | 230 | Low | Low | Reviewed |
| 39 | Daycare.Web | [DailyCarePlansPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePlansPage.razor) | `/daily-care/plans` | 223 | Low | Low | Reviewed |
| 40 | Daycare.Web | [DailyCareSuppliesPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCareSuppliesPage.razor) | `/daily-care/supplies` | 220 | Low | Low | Reviewed |
| 41 | Admin.Web | [ReportsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/ReportsPage.razor) | `/reports` | 201 | Low | Low | Reviewed |
| 42 | Parent.Web | [Events.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Learning/Events.razor) | `/events` | 201 | Low | Low | Reviewed |
| 43 | Daycare.Web | [TeacherWizard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/TeacherWizard.razor) | `/teachers/new, /teachers/edit/{Id:guid}` | 199 | Low | Low | Reviewed |
| 44 | Admin.Web | [AiCommunicationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/AiCommunicationPage.razor) | `/ai-communication` | 196 | Low | Low | Reviewed |
| 45 | Daycare.Web | [ClassroomWizard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/ClassroomWizard.razor) | `/classrooms/new, /management/branches/{BranchId:guid}/classrooms/new, /classrooms/edit/{Id:guid}` | 184 | Low | Low | Reviewed |
| 46 | Admin.Web | [Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Today.razor) | `/today-legacy` | 177 | Low | Low | Reviewed |
| 47 | Parent.Web | [ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Children/ChildJourney.razor) | `/journey` | 176 | Low | Low | Reviewed |
| 48 | Daycare.Web | [Management.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Management.razor) | `/management` | 172 | Low | Low | Reviewed |
| 49 | Daycare.Web | [SafetyTrust.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/SafetyTrust.razor) | `/safety-trust` | 169 | Low | Low | Reviewed |
| 50 | Parent.Web | [FamilyProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Children/FamilyProfile.razor) | `/family` | 156 | Low | Low | Reviewed |
| 51 | Daycare.Web | [CreateBranch.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/CreateBranch.razor) | `/management/branches/new` | 151 | Low | Low | Reviewed |
| 52 | Admin.Web | [Home.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Home.razor) | `/home-old` | 147 | Low | Low | Reviewed |
| 53 | Daycare.Web | [ParentStories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ParentStories.razor) | `/stories` | 139 | Low | Low | Reviewed |
| 54 | Daycare.Web | [CustomRoles.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/CustomRoles.razor) | `/management/roles` | 135 | Low | Low | Reviewed |
| 55 | Admin.Web | [ParentStories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/ParentStories.razor) | `/stories` | 125 | Low | Low | Reviewed |
| 56 | Admin.Web | [MonetizationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/MonetizationPage.razor) | `/monetization` | 120 | Low | Low | Reviewed |
| 57 | Admin.Web | [DaycareAdmins.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/DaycareAdmins.razor) | `/daycare-admins` | 114 | Low | Low | Reviewed |
| 58 | Daycare.Web | [AttendanceOverview.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/AttendanceOverview.razor) | `/management/attendance, /attendance` | 107 | Low | Low | Reviewed |
| 59 | Parent.Web | [Messages.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Communication/Messages.razor) | `/messages` | 99 | Low | Low | Reviewed |
| 60 | Admin.Web | [PaymentsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/PaymentsPage.razor) | `/payments` | 94 | Low | Low | Reviewed |
| 61 | Daycare.Web | [DailyCarePage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePage.razor) | `/daily-care` | 89 | Low | Low | Reviewed |
| 62 | Daycare.Web | [Settings.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Settings.razor) | `/settings` | 84 | Low | Low | Reviewed |
| 63 | Admin.Web | [AuditLogsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/AuditLogsPage.razor) | `/audit-logs` | 65 | Low | Low | Reviewed |
| 64 | Daycare.Web | [Home.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Home.razor) | `/home-old` | 57 | Low | Low | Reviewed |
| 65 | Admin.Web | [LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/LoginPrompt.razor) | `/login` | 48 | Low | Low | Reviewed |
| 66 | Parent.Web | [LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/LoginPrompt.razor) | `/login` | 47 | Low | Low | Reviewed |
| 67 | Admin.Web | [AccessDenied.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/AccessDenied.razor) | `/access-denied` | 47 | Low | Low | Reviewed |
| 68 | Daycare.Web | [AccessDenied.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/AccessDenied.razor) | `/access-denied` | 47 | Low | Low | Reviewed |
| 69 | Admin.Web | [SettingsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/SettingsPage.razor) | `/settings` | 46 | Low | Low | Reviewed |
| 70 | Admin.Web | [DiscountsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/DiscountsPage.razor) | `/discounts` | 46 | Low | Low | Reviewed |
| 71 | Daycare.Web | [LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/LoginPrompt.razor) | `/login` | 45 | Low | Low | Reviewed |
| 72 | Admin.Web | [AdminsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/AdminsPage.razor) | `/admins` | 45 | Low | Low | Reviewed |
| 73 | Admin.Web | [Settings.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Settings.razor) | `/organization-settings` | 43 | Low | Low | Reviewed |
| 74 | Admin.Web | [Management.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Management.razor) | `/management` | 41 | Low | Low | Reviewed |
| 75 | Admin.Web | [BranchesPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/BranchesPage.razor) | `/branches` | 39 | Low | Low | Reviewed |
| 76 | Daycare.Web | [Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Error.razor) | `/Error` | 30 | Low | Low | Reviewed |
| 77 | Admin.Web | [Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Error.razor) | `/Error` | 30 | Low | Low | Reviewed |
| 78 | Parent.Web | [Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Error.razor) | `/Error` | 30 | Low | Low | Reviewed |
| 79 | Daycare.Web | [ClassroomAttendance.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/ClassroomAttendance.razor) | `/management/attendance/classrooms/{ClassroomName}` | 27 | Low | Low | Reviewed |
| 80 | Daycare.Web | [BranchAttendance.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/BranchAttendance.razor) | `/management/attendance/branches/{BranchName}` | 25 | Low | Low | Reviewed |
| 81 | Daycare.Web | [EmergencyFloorMapsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/EmergencyFloorMapsPage.razor) | `/safety-trust/floor-maps` | 24 | Low | Low | Reviewed |
| 82 | Daycare.Web | [EmergencyContactsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/EmergencyContactsPage.razor) | `/safety-trust/emergency-contacts` | 24 | Low | Low | Reviewed |
| 83 | Daycare.Web | [Planner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Planner.razor) | `/planner-old` | 16 | Low | Low | Reviewed |
| 84 | Admin.Web | [Planner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Planner.razor) | `/planner-old` | 16 | Low | Low | Reviewed |
| 85 | Parent.Web | [NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/NotFound.razor) | `/not-found` | 15 | Low | Low | Reviewed |
| 86 | Admin.Web | [NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/NotFound.razor) | `/not-found` | 15 | Low | Low | Reviewed |
| 87 | Daycare.Web | [NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/NotFound.razor) | `/not-found` | 15 | Low | Low | Reviewed |
| 88 | Daycare.Web | [ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildJourney.razor) | `/journey, /journey/{ChildId:guid}` | 14 | Low | Low | Reviewed |
| 89 | Admin.Web | [ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Children/ChildJourney.razor) | `/journey, /journey/{ChildId:guid}` | 14 | Low | Low | Reviewed |
| 90 | Admin.Web | [Journey.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Journey.razor) | `/journey-old` | 11 | Low | Low | Reviewed |
| 91 | Daycare.Web | [Stories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Stories.razor) | `/stories-old` | 11 | Low | Low | Reviewed |
| 92 | Admin.Web | [Stories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Stories.razor) | `/stories-old` | 11 | Low | Low | Reviewed |
| 93 | Daycare.Web | [SelectOrganization.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SelectOrganization.razor) | `/select-organization` | 9 | Low | Low | Reviewed |
| 94 | Admin.Web | [SelectOrganization.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/SelectOrganization.razor) | `/select-organization` | 9 | Low | Low | Reviewed |
| 95 | Admin.Web | [Classrooms.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Classrooms.razor) | `/classrooms` | 8 | Low | Low | Reviewed |
| 96 | Daycare.Web | [Classrooms.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Classrooms.razor) | `/classrooms` | 8 | Low | Low | Reviewed |
| 97 | Daycare.Web | [Children.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/Children.razor) | `/children` | 7 | Low | Low | Reviewed |
| 98 | Admin.Web | [Teachers.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Teachers.razor) | `/teachers` | 7 | Low | Low | Reviewed |
| 99 | Admin.Web | [Harvest.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Harvest.razor) | `/harvest` | 7 | Low | Low | Reviewed |
| 100 | Daycare.Web | [Harvest.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Harvest.razor) | `/harvest` | 7 | Low | Low | Reviewed |
| 101 | Daycare.Web | [Teachers.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Teachers.razor) | `/teachers` | 7 | Low | Low | Reviewed |
| 102 | Admin.Web | [Children.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Children/Children.razor) | `/children` | 7 | Low | Low | Reviewed |

---

## Detailed Page Breakdown

### 1. BillingPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\BillingPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/BillingPage.razor)
- **Code-Behind**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\BillingPage.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/BillingPage.razor.cs) (455 lines)
- **Route**: `/billing`
- **Page Purpose**: Commercial Billing & Invoices - Platform Admin: Administer daycare organization invoices, configure manual vs. automatic billing modes, record payments, and manage ledgers.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Very High (1253 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `EmptyState`, `StatusBadge`
- **Recommended Refactoring**: Extract large sub-sections (modals, filters, detail panels, action forms) into feature-specific or shared UI components; separate orchestration from presentation.
- **Dependencies & Risk**: High (Stateful orchestration, critical workflows)
- **Current Status**: `Refactoring Planned`

### 2. WarehouseInventoryPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\WarehouseInventoryPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/WarehouseInventoryPage.razor)
- **Code-Behind**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\WarehouseInventoryPage.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/WarehouseInventoryPage.razor.cs) (425 lines)
- **Route**: `/warehouse-inventory`
- **Page Purpose**: Warehouse Inventory & Stock - EnjoyEveryday: Track material quantities across multi-state inventory: available, reserved, dispatched, on-loan, cleaning, and damaged.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Very High (1033 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`
- **Recommended Refactoring**: Extract large sub-sections (modals, filters, detail panels, action forms) into feature-specific or shared UI components; separate orchestration from presentation.
- **Dependencies & Risk**: High (Stateful orchestration, critical workflows)
- **Current Status**: `Refactoring Planned`

### 3. ExperiencePlanning.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\ExperiencePlanning.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperiencePlanning.razor)
- **Code-Behind**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\ExperiencePlanning.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperiencePlanning.razor.cs) (466 lines)
- **Route**: `/planning`
- **Page Purpose**: ExperiencePlanning
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Very High (1020 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageTopbar`, `RightContent`, `WeeklyPlanningAssistantModal`, `PlanningPhilosophy`, `PlanningControlBar`, `SidePanel`, `ManagementDirection`, `AiPlan`, `RhythmRow`, `FlexibilityCard`, `PlanExperienceDialog`
- **Recommended Refactoring**: Extract large sub-sections (modals, filters, detail panels, action forms) into feature-specific or shared UI components; separate orchestration from presentation.
- **Dependencies & Risk**: High (Stateful orchestration, critical workflows)
- **Current Status**: `Refactoring Planned`

### 4. RhythmHistory.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\RhythmHistory.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/RhythmHistory.razor)
- **Route**: `/rhythm-history`
- **Page Purpose**: RhythmHistory
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (877 lines)
- **Injections & Dependencies**: `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `ExperienceService ExperienceService`, `EventService EventService`, `IDbConnectionFactory DbConnectionFactory`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `NavigationManager NavigationManager`, `TimeProvider TimeProvider`
- **Child Components Already Used**: `FluentSpinner`, `EventPhotoUploader`, `Organization`, `Branch`, `Classroom`, `Experience`, `HistoricalRhythmItem`, `ExperienceSchedule`, `Guid`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 5. ExperienceStudio.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\ExperienceStudio.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStudio.razor)
- **Route**: `/studio, /studio/{ExperienceId:guid}`
- **Page Purpose**: Print Experience: Design and refine developmental experiences with structured DNA, timeline phases, and AI co-creation.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (870 lines)
- **Injections & Dependencies**: `ExperienceService ExperienceService`, `UserService UserService`, `NavigationManager NavigationManager`, `IAiIdeaService AiIdeaService`, `INotificationService ToastService`, `IJSRuntime JS`, `EnjoyEveryday.Daycare.Web.Services.StorybookStorageService StorybookStorageService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `FilterTabs`, `StudioDnaCard`, `StudioMagicMoment`, `ExperienceViewModal`, `ExperienceVersionCompareModal`, `ExperienceVersion`, `ExperienceFullData`, `ExperienceDna`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 6. Intelligence.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Intelligence\Intelligence.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Intelligence/Intelligence.razor)
- **Route**: `/intelligence`
- **Page Purpose**: Intelligence
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (857 lines)
- **Injections & Dependencies**: `IOrganizationIntelligenceService IntelService`, `IBranchBenchmarkingService BenchmarkingService`, `OrganizationService OrgService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `LoadingState`, `EmptyState`, `AddOperationalGoalModal`, `Organization`, `Branch`, `Classroom`, `BranchHealthScorecard`, `BranchOperationalGoal`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 7. MaterialOrdersPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\MaterialOrdersPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MaterialOrdersPage.razor)
- **Code-Behind**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\MaterialOrdersPage.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MaterialOrdersPage.razor.cs) (323 lines)
- **Route**: `/materials-orders`
- **Page Purpose**: Orders, Loans & Deliveries - EnjoyEveryday: Track your material requests, view quotation terms, verify deliveries, and manage reusable kits and return deadlines.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (850 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `MaterialOrderTimeline`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 8. ExperienceStudio.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\ExperienceStudio.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/ExperienceStudio.razor)
- **Route**: `/studio, /studio/{ExperienceId:guid}`
- **Page Purpose**: Print Experience: Design and refine developmental experiences with structured DNA, timeline phases, and AI co-creation.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (832 lines)
- **Injections & Dependencies**: `ExperienceService ExperienceService`, `UserService UserService`, `NavigationManager NavigationManager`, `IAiIdeaService AiIdeaService`, `INotificationService ToastService`, `IJSRuntime JS`
- **Child Components Already Used**: `PageHero`, `RightContent`, `FilterTabs`, `StudioDnaCard`, `StudioMagicMoment`, `ExperienceViewModal`, `ExperienceVersionCompareModal`, `ExperienceVersion`, `ExperienceFullData`, `ExperienceDna`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 9. EnrollmentPipeline.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Children\EnrollmentPipeline.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/EnrollmentPipeline.razor)
- **Code-Behind**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Children\EnrollmentPipeline.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/EnrollmentPipeline.razor.cs) (225 lines)
- **Route**: `/enrollment, /management/enrollment`
- **Page Purpose**: Admissions & Family Accounts - EnjoyEveryday: Track inbound family inquiries, schedule tours, review applications, and convert accepted offers into active tuition contracts.
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (805 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `EmptyState`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 10. StaffRoster.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\StaffRoster.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/StaffRoster.razor)
- **Route**: `/management/roster`
- **Page Purpose**: Staff Roster & Coverage - EnjoyEveryday: Statutory educator-to-child ratios, staggered shifts, and leave coverage in real time.
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (805 lines)
- **Injections & Dependencies**: `IStaffRosterService RosterService`, `BranchService BranchService`, `OrganizationService OrganizationService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `Branch`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 11. MarketplaceOperationsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\MarketplaceOperationsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/MarketplaceOperationsPage.razor)
- **Code-Behind**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\MarketplaceOperationsPage.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/MarketplaceOperationsPage.razor.cs) (301 lines)
- **Route**: `/marketplace-operations`
- **Page Purpose**: Marketplace Operations - EnjoyEveryday: Central operations center for incoming material orders, quotations, picking & dispatch, delivery tracking, and loan return inspections.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (788 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `MaterialOrderTimeline`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Refactoring Planned`

### 12. Experiences.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Experiences.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Experiences.razor)
- **Route**: `/experiences`
- **Page Purpose**: My Experiences - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (652 lines)
- **Injections & Dependencies**: `ExperienceService ExperienceService`, `ExperienceCurationService CurationService`, `UserService UserService`, `PlannerService PlannerService`, `EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory`, `EnjoyEveryday.Daycare.Web.Services.StorybookStorageService StorybookStorageService`, `NavigationManager NavigationManager`, `IJSRuntime JS`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`
- **Child Components Already Used**: `FilterTabs`, `SearchFilterRow`, `Filters`, `ExperienceCard`, `ExperienceCreateModal`, `ExperienceViewModal`, `ExperienceCurationSubmitModal`, `ExperienceVersionCompareModal`, `ExperienceScheduleModal`, `Experience`, `ExperienceVersion`, `ExperienceFeedback`, `ExperienceDna`, `Guid`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 13. EventDetails.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\EventDetails.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/EventDetails.razor)
- **Route**: `/events/{Id:guid}`
- **Page Purpose**: EventDetails
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (639 lines)
- **Injections & Dependencies**: `EventService EventService`, `ClassroomService ClassroomService`, `BranchService BranchService`, `OrganizationService OrganizationService`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `FluentSpinner`, `EventPhotoUploader`, `EventTask`, `Branch`, `Classroom`, `Guid`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 14. MaterialsMarketplace.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\MaterialsMarketplace.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MaterialsMarketplace.razor)
- **Route**: `/materials-marketplace`
- **Page Purpose**: Materials Marketplace - EnjoyEveryday: Equip your classroom with consumable arts & crafts, or borrow specialized early-learning kits without purchasing expensive inventory.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (637 lines)
- **Injections & Dependencies**: `IMaterialMarketplaceService MaterialService`, `ClassroomService ClassroomService`, `BranchService BranchService`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `PageHero`, `RightContent`, `MaterialProduct`, `Classroom`, `CartItemModel`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 15. Today.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Today.razor)
- **Route**: `/, /today`
- **Page Purpose**: Live Daycare Operations Board - EnjoyEveryday: @($
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (624 lines)
- **Injections & Dependencies**: `IDaycareOperationsBoardService OperationsBoardService`, `ClassroomService ClassroomService`, `BranchService BranchService`, `ExecutionService ExecutionService`, `EventService EventService`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `NavigationManager Navigation`, `TimeProvider TimeProvider`
- **Child Components Already Used**: `LoadingState`, `PageHero`, `RightContent`, `ActiveSessionModal`, `EventPhotoUploader`, `Branch`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 16. PlatformDashboard.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Dashboard\PlatformDashboard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Dashboard/PlatformDashboard.razor)
- **Route**: `/, /dashboard`
- **Page Purpose**: Platform Admin Dashboard - EnjoyEveryday: Monitor daycare organizations, subscriptions, billing health, and platform operations from one place.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (620 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`, `NavigationManager Navigation`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `MetricCard`, `LifecyclePipeline`, `SearchFilterRow`, `Filters`, `EmptyState`, `StatusBadge`, `PlatformOrganizationDto`, `SubscriptionPlan`, `PlatformAuditLog`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 17. OrganizationBilling.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\OrganizationBilling.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationBilling.razor)
- **Route**: `/management/billing, /billing`
- **Page Purpose**: Billing & Invoices - EnjoyEveryday: Track issued daycare subscription invoices, payment receipts, due dates, and review your organization's ledger balance.
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: High (613 lines)
- **Injections & Dependencies**: `IBillingService BillingService`, `IUserContext UserContext`, `ITenantContext TenantContext`, `IJSRuntime JS`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `EmptyState`, `StatusBadge`, `Invoice`, `OrganizationBilling`, `Payment`
- **Recommended Refactoring**: Decompose large card grids, forms, or sub-panels into modular components to improve single-developer maintainability.
- **Dependencies & Risk**: Medium (Domain logic mixed with markup)
- **Current Status**: `Reviewed`

### 18. AccountBookPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\AccountBookPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/AccountBookPage.razor)
- **Code-Behind**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\AccountBookPage.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/AccountBookPage.razor.cs) (172 lines)
- **Route**: `/account-book`
- **Page Purpose**: Account Book & Financial Investigation - Platform Admin: Unified financial control centre, multi-category ledger, and transaction investigation.
- **Intended User or Role**: Roles = $"{RoleTemplates.PlatformAdmin},{RoleTemplates.OrganizationOwner},{RoleTemplates.OrganizationAdmin}"
- **Authorization & Tenant-Scope**: Auth: `Roles = $"{RoleTemplates.PlatformAdmin},{RoleTemplates.OrganizationOwner},{RoleTemplates.OrganizationAdmin}"`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (596 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 19. ChildEnrollment.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Children\ChildEnrollment.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildEnrollment.razor)
- **Route**: `/children/add, /children/edit/{Id:guid}`
- **Page Purpose**: ChildEnrollment
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (595 lines)
- **Injections & Dependencies**: `ChildService ChildService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `OrganizationService OrganizationService`, `SystemProvisioningService SystemProvisioning`, `NavigationManager NavigationManager`, `IJSRuntime JSRuntime`
- **Child Components Already Used**: `WizardSidebar`, `InputFile`, `Branch`, `Classroom`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 20. OrganizationsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organizations\OrganizationsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/OrganizationsPage.razor)
- **Route**: `/organizations`
- **Page Purpose**: Organizations - Platform Admin: Create, configure, monitor multi-branch daycare organizations and lifecycle states.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (558 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`, `NavigationManager Navigation`
- **Child Components Already Used**: `PageHero`, `RightContent`, `SearchFilterRow`, `Filters`, `LoadingState`, `EmptyState`, `StatusBadge`, `PlatformOrganizationDto`, `SubscriptionPlan`, `Tariff`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 21. Events.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Events.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Events.razor)
- **Route**: `/events`
- **Page Purpose**: Events
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (551 lines)
- **Injections & Dependencies**: `EventService EventService`, `ClassroomService ClassroomService`, `BranchService BranchService`, `OrganizationService OrganizationService`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `FilterTabs`, `FluentSpinner`, `Event`, `Classroom`, `Branch`, `Guid`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 22. ExperienceStorybook.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\ExperienceStorybook.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ExperienceStorybook.razor)
- **Route**: `/experiences/{ExperienceId:guid}/story, /experiences/story/{ExperienceId:guid}`
- **Page Purpose**: @(string.IsNullOrEmpty(_experience?.Title) ? "Activity Playbook" : $"{_experience.Title} - Playbook") - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (531 lines)
- **Injections & Dependencies**: `ExperienceService ExperienceService`, `NavigationManager NavigationManager`, `IJSRuntime JS`, `EnjoyEveryday.Daycare.Web.Services.StorybookStorageService StorybookStorageService`
- **Child Components Already Used**: `StoryChapter`, `ExperienceDna`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 23. Memories.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Memories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Memories.razor)
- **Code-Behind**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Memories.razor.cs](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Memories.razor.cs) (207 lines)
- **Route**: `/memories`
- **Page Purpose**: Memories - EnjoyEveryday: Every meaningful moment helps tell a child's story. A chronological stream of discoveries, milestones, and shared experiences.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (513 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `EmptyState`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 24. PlansPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\PlansPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/PlansPage.razor)
- **Route**: `/plans`
- **Page Purpose**: Plans & Tariffs - Platform Admin: Create and maintain global SaaS plans, classroom/branch limits, and commercial tariff pricing schedules.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (462 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `StatusBadge`, `SubscriptionPlan`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 25. UsersByRole.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Users\UsersByRole.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/UsersByRole.razor)
- **Route**: `/users-by-role, /management/users-by-role`
- **Page Purpose**: Users by Role Hierarchy - EnjoyEveryday: All daycare team members, educators, administrators, and families organized by role hierarchy.
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (453 lines)
- **Injections & Dependencies**: `IDbConnectionFactory DbConnectionFactory`, `ITenantContext TenantContext`, `IUserContext UserContext`
- **Child Components Already Used**: `PageHero`, `RightContent`, `FluentIcon`, `FilterTabs`, `LoadingState`, `EmptyState`, `PersonCard`, `UserRecord`, `RoleGroup`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 26. OrganizationTree.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\OrganizationTree.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/OrganizationTree.razor)
- **Route**: `/organization`
- **Page Purpose**: OrganizationTree
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (449 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageTopbar`, `RightContent`, `OrgClassroom`, `OrgTeacher`, `OrgChild`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 27. UserProfile.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Users\UserProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/UserProfile.razor)
- **Route**: `/profile`
- **Page Purpose**: User Profile - EnjoyEveryday: Manage your identity, active organization credentials, and role permissions.
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (448 lines)
- **Injections & Dependencies**: `AuthenticationStateProvider AuthStateProvider`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`, `IDbConnectionFactory DbConnectionFactory`
- **Child Components Already Used**: `PageHero`, `RightContent`, `FluentIcon`, `LoadingState`, `Claim`, `UserRoleDetail`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 28. PlatformCurationPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\PlatformAdmin\PlatformCurationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/PlatformCurationPage.razor)
- **Route**: `/platform-curation`
- **Page Purpose**: Experience Curation - EnjoyEveryday: Review submitted daycare activities, verify licensing and safety, and curate into the Platform Experience Library.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (398 lines)
- **Injections & Dependencies**: `ExperienceCurationService CurationService`, `EnjoyEveryday.Domain.Repositories.IExperienceRepository ExperienceRepo`, `EnjoyEveryday.Domain.Repositories.IOrganizationRepository OrgRepo`, `ITenantContext TenantContext`, `IUserContext UserContext`
- **Child Components Already Used**: `PageHero`, `RightContent`, `ExperienceHarvestRequest`, `Organization`, `Experience`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 29. Experiences.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\Experiences.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Experiences.razor)
- **Route**: `/experiences`
- **Page Purpose**: Print Experience
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (377 lines)
- **Injections & Dependencies**: `ExperienceService ExperienceService`, `UserService UserService`, `PlannerService PlannerService`, `EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory`, `NavigationManager NavigationManager`, `IJSRuntime JS`, `EnjoyEveryday.Shared.Tenancy.IUserContext UserContext`
- **Child Components Already Used**: `PageTopbar`, `RightContent`, `PageHeader`, `FilterTabs`, `SearchFilterRow`, `Filters`, `ExperienceCard`, `ExperienceCreateModal`, `ExperienceViewModal`, `ExperienceVersionCompareModal`, `ExperienceScheduleModal`, `Experience`, `ExperienceVersion`, `ExperienceDna`, `Guid`
- **Recommended Refactoring**: Evaluate shared component extraction between Daycare.Web and Admin.Web versions.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 30. MonthlyPlanner.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\MonthlyPlanner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/MonthlyPlanner.razor)
- **Route**: `/planner`
- **Page Purpose**: MonthlyPlanner: Give the month a rhythm. Leave room for children to lead.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (352 lines)
- **Injections & Dependencies**: `PlannerService PlannerService`, `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `ExperienceService ExperienceService`, `EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory`
- **Child Components Already Used**: `PageHero`, `RightContent`, `BalanceRow`, `ExperienceScheduleFormModal`, `Classroom`, `Experience`, `ExperienceSchedule`, `Guid`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 31. Today.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Today.razor)
- **Route**: `/, /today`
- **Page Purpose**: My Child Today - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `Authenticated`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Medium (348 lines)
- **Injections & Dependencies**: `IParentService ParentService`, `ParentSessionState SessionState`, `NavigationManager Navigation`
- **Child Components Already Used**: `FluentSpinner`, `FluentCard`, `FluentText`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 32. ConsentSigningPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\SafetyTrust\ConsentSigningPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/ConsentSigningPage.razor)
- **Route**: `/consent/sign/{Token}`
- **Page Purpose**: ConsentSigningPage
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (291 lines)
- **Injections & Dependencies**: `IConsentService ConsentService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 33. Marketplace.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Marketplace.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Marketplace.razor)
- **Route**: `/marketplace`
- **Page Purpose**: Experience Marketplace - EnjoyEveryday: Discover expertly crafted, developmental activities. Acquire entitlements and add them directly to your organization's private library.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (278 lines)
- **Injections & Dependencies**: `ExperienceMarketplaceService MarketplaceService`, `IUserContext UserContext`, `NavigationManager Navigation`
- **Child Components Already Used**: `PageHero`, `RightContent`, `FilterTabs`, `PlatformExperience`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 34. PlatformMarketplacePage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\PlatformAdmin\PlatformMarketplacePage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/PlatformMarketplacePage.razor)
- **Route**: `/platform-marketplace`
- **Page Purpose**: Platform Experience Library - EnjoyEveryday: Manage curated, versioned platform experiences, set commercial pricing models, and publish items to all daycares.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (261 lines)
- **Injections & Dependencies**: `IExperienceMarketplaceRepository MarketplaceRepo`, `ExperienceCurationService CurationService`, `IUserContext UserContext`
- **Child Components Already Used**: `PageHero`, `RightContent`, `PlatformExperience`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 35. ClassroomProfile.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\ClassroomProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/ClassroomProfile.razor)
- **Route**: `/classrooms/view/{Id:guid}`
- **Page Purpose**: ClassroomProfile: Read-only view of the classroom's configuration.
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (260 lines)
- **Injections & Dependencies**: `ClassroomService ClassroomService`, `ExecutionService ExecutionService`, `ExperienceService ExperienceService`, `NavigationManager NavigationManager`, `IJSRuntime JSRuntime`
- **Child Components Already Used**: `PageHero`, `RightContent`, `ExperienceSession`, `Guid`, `ClassroomWizardModel`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 36. BranchDetail.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\BranchDetail.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/BranchDetail.razor)
- **Route**: `/management/branches/{BranchId:guid}`
- **Page Purpose**: BranchDetail
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (255 lines)
- **Injections & Dependencies**: `BranchService BranchService`, `ClassroomService ClassroomService`, `ChildService ChildService`, `ITeacherService TeacherService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `PageTopbar`, `RightContent`, `Classroom`, `Child`, `Teacher`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 37. TeacherProfile.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\TeacherProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/TeacherProfile.razor)
- **Route**: `/teachers/view/{Id:guid}`
- **Page Purpose**: TeacherProfile: Read-only view of the teacher's profile.
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (236 lines)
- **Injections & Dependencies**: `UserService UserService`, `ITeacherService TeacherService`, `NavigationManager NavigationManager`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`, `EnjoyEveryday.Application.Services.ExecutionService ExecutionService`, `EnjoyEveryday.Application.Services.ExperienceService ExperienceService`, `IJSRuntime JSRuntime`
- **Child Components Already Used**: `PageHero`, `RightContent`, `TeacherCreationWizard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 38. MonthlyPlanner.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\MonthlyPlanner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/MonthlyPlanner.razor)
- **Route**: `/planner`
- **Page Purpose**: MonthlyPlanner
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (230 lines)
- **Injections & Dependencies**: `PlannerService PlannerService`, `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `ExperienceService ExperienceService`, `EnjoyEveryday.Shared.Data.IDbConnectionFactory DbConnectionFactory`
- **Child Components Already Used**: `PlannerCalendarGrid`, `IntelligenceCard`, `MonthBalanceCard`, `ExperienceScheduleFormModal`, `CalendarItem`, `BalanceStat`, `Classroom`, `Experience`, `ExperienceSchedule`, `Guid`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 39. DailyCarePlansPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\DailyCare\DailyCarePlansPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePlansPage.razor)
- **Route**: `/daily-care/plans`
- **Page Purpose**: Individual Child Care Plans - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (223 lines)
- **Injections & Dependencies**: `IChildDailyCareService DailyCareService`, `BranchService BranchService`, `OrganizationService OrganizationService`
- **Child Components Already Used**: `ChildCarePlan`, `Branch`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 40. DailyCareSuppliesPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\DailyCare\DailyCareSuppliesPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCareSuppliesPage.razor)
- **Route**: `/daily-care/supplies`
- **Page Purpose**: Personal Care Supplies - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (220 lines)
- **Injections & Dependencies**: `IChildDailyCareService DailyCareService`, `BranchService BranchService`, `OrganizationService OrganizationService`
- **Child Components Already Used**: `ChildCareSupply`, `Branch`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 41. ReportsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\ReportsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/ReportsPage.razor)
- **Route**: `/reports`
- **Page Purpose**: Organization Intelligence & Reports - Platform Admin: SaaS-wide platform telemetry, organization performance metrics, and evidence-grounded insights.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (201 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `PlatformOrganizationDto`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 42. Events.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Learning\Events.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Learning/Events.razor)
- **Route**: `/events`
- **Page Purpose**: Events & Celebrations - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `Authenticated`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (201 lines)
- **Injections & Dependencies**: `IParentService ParentService`, `ParentSessionState SessionState`
- **Child Components Already Used**: `FluentSpinner`, `FluentCard`, `FluentText`, `Event`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 43. TeacherWizard.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\TeacherWizard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/TeacherWizard.razor)
- **Route**: `/teachers/new, /teachers/edit/{Id:guid}`
- **Page Purpose**: TeacherWizard: @(Id.HasValue ?
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (199 lines)
- **Injections & Dependencies**: `UserService UserService`, `ITeacherService TeacherService`, `ClassroomService ClassroomService`, `BranchService BranchService`, `OrganizationService OrganizationService`, `NavigationManager NavigationManager`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`, `SystemProvisioningService SystemProvisioning`
- **Child Components Already Used**: `PageHero`, `TeacherCreationWizard`, `Classroom`, `Branch`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 44. AiCommunicationPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\AiCommunicationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/AiCommunicationPage.razor)
- **Route**: `/ai-communication`
- **Page Purpose**: AI Communication Gateway - Platform Admin: Monitor AI draft generation, evidence grounding, teacher approval gates, and parent delivery channels.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (196 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `EmptyState`, `StatusBadge`, `ParentCommunication`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 45. ClassroomWizard.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\ClassroomWizard.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/ClassroomWizard.razor)
- **Route**: `/classrooms/new, /management/branches/{BranchId:guid}/classrooms/new, /classrooms/edit/{Id:guid}`
- **Page Purpose**: ClassroomWizard: Let's configure the learning environment.
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (184 lines)
- **Injections & Dependencies**: `ClassroomService ClassroomService`, `BranchService BranchService`, `OrganizationService OrgService`, `NavigationManager NavigationManager`, `SystemProvisioningService SystemProvisioning`
- **Child Components Already Used**: `PageHero`, `ClassroomCreationWizard`, `Branch`, `ClassroomWizardModel`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 46. Today.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Today.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Today.razor)
- **Route**: `/today-legacy`
- **Page Purpose**: Today: Bluebirds classroom &middot; 18 children &middot; Ages 3&ndash;4
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (177 lines)
- **Injections & Dependencies**: `PlannerService PlannerService`, `ClassroomService ClassroomService`, `OrganizationService OrganizationService`, `BranchService BranchService`, `ExecutionService ExecutionService`, `ExperienceService ExperienceService`, `ClassroomContextService ContextService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `RhythmCard`, `FeelingButton`, `ExperienceSchedule`, `Experience`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 47. ChildJourney.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Children\ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Children/ChildJourney.razor)
- **Route**: `/journey`
- **Page Purpose**: Child Journey - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `Authenticated`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (176 lines)
- **Injections & Dependencies**: `IParentService ParentService`, `ParentSessionState SessionState`
- **Child Components Already Used**: `FluentSpinner`, `FluentCard`, `FluentText`, `ChildJourneyEntry`, `ChildMilestone`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 48. Management.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\Management.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Management.razor)
- **Route**: `/management`
- **Page Purpose**: Management
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (172 lines)
- **Injections & Dependencies**: `DashboardService DashboardService`, `OrganizationService OrganizationService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `PageTopbar`, `RightContent`, `OrgSummaryCard`, `LoadingState`, `OrgPulse`, `FilterTabs`, `OrgBranchCard`, `OrgPanel`, `EnjoyEveryday`, `InsightRow`, `AlertRow`, `BranchMetrics`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 49. SafetyTrust.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\SafetyTrust\SafetyTrust.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/SafetyTrust.razor)
- **Route**: `/safety-trust`
- **Page Purpose**: SafetyTrust
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (169 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `TrustBanner`, `SafetySummaryCard`, `FilterTabs`, `ConsentManagementSection`, `EmergencyFloorMapsSection`, `EmergencyContactsSection`, `SafetyDomainRow`, `BranchSafetyRow`, `PrivacyControlsCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 50. FamilyProfile.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Children\FamilyProfile.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Children/FamilyProfile.razor)
- **Route**: `/family`
- **Page Purpose**: Family Profile - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `Authenticated`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (156 lines)
- **Injections & Dependencies**: `IParentService ParentService`, `ParentSessionState SessionState`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 51. CreateBranch.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\CreateBranch.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/CreateBranch.razor)
- **Route**: `/management/branches/new`
- **Page Purpose**: CreateBranch
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (151 lines)
- **Injections & Dependencies**: `BranchService BranchService`, `OrganizationService OrganizationService`, `NavigationManager NavigationManager`
- **Child Components Already Used**: `PageTopbar`, `RightContent`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 52. Home.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Home.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Home.razor)
- **Route**: `/home-old`
- **Page Purpose**: Today - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (147 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentCard`, `FluentText`, `FluentGrid`, `FluentGridItem`, `FluentButton`, `FluentIcon`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 53. ParentStories.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\ParentStories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/ParentStories.razor)
- **Route**: `/stories`
- **Page Purpose**: ParentStories: Let parents feel their child's day.
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (139 lines)
- **Injections & Dependencies**: `ChildService ChildService`, `JourneyService JourneyService`, `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`
- **Child Components Already Used**: `PageHero`, `StoryCard`, `ParentStoryFormModal`, `ParentCommunicationAssistantModal`, `Child`, `ChildStory`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 54. CustomRoles.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Users\CustomRoles.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Users/CustomRoles.razor)
- **Route**: `/management/roles`
- **Page Purpose**: Manage Roles - EnjoyEveryday
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (135 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentButton`, `FluentDialog`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 55. ParentStories.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\ParentStories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/ParentStories.razor)
- **Route**: `/stories`
- **Page Purpose**: ParentStories: Let parents feel their child's day.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (125 lines)
- **Injections & Dependencies**: `ChildService ChildService`, `JourneyService JourneyService`, `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`
- **Child Components Already Used**: `PageHero`, `StoryCard`, `ParentStoryFormModal`, `Child`, `ChildStory`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 56. MonetizationPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\PlatformAdmin\MonetizationPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/PlatformAdmin/MonetizationPage.razor)
- **Route**: `/monetization`
- **Page Purpose**: Monetization & Revenue - EnjoyEveryday: Comprehensive financial reporting for marketplace experiences, entitlement tracking, gross sales, and net revenue.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (120 lines)
- **Injections & Dependencies**: `ExperienceMarketplaceService MarketplaceService`
- **Child Components Already Used**: `PageHero`, `RightContent`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 57. DaycareAdmins.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organization\DaycareAdmins.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/DaycareAdmins.razor)
- **Route**: `/daycare-admins`
- **Page Purpose**: Manage Daycare Admins
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (114 lines)
- **Injections & Dependencies**: `UserService UserService`
- **Child Components Already Used**: `FluentButton`, `FluentDataGrid`, `PropertyColumn`, `TemplateColumn`, `FluentDialog`, `User`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 58. AttendanceOverview.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\AttendanceOverview.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/AttendanceOverview.razor)
- **Route**: `/management/attendance, /attendance`
- **Page Purpose**: AttendanceOverview
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (107 lines)
- **Injections & Dependencies**: `OrganizationService OrganizationService`, `BranchService BranchService`, `ClassroomService ClassroomService`, `AttendanceService AttendanceService`, `NavigationManager Nav`
- **Child Components Already Used**: `AttendanceOverviewHeader`, `AttendanceFilterBar`, `AttendanceKpiSection`, `BranchAttendanceSection`, `NeedsAttentionSection`, `ClassroomAttendanceSection`, `ToastNotification`, `EnjoyEveryday`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 59. Messages.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Communication\Messages.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Communication/Messages.razor)
- **Route**: `/messages`
- **Page Purpose**: Messages - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `Authenticated`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (99 lines)
- **Injections & Dependencies**: `ParentSessionState SessionState`
- **Child Components Already Used**: `ChatMessage`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 60. PaymentsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\PaymentsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/PaymentsPage.razor)
- **Route**: `/payments`
- **Page Purpose**: Payments & Reconciliation - Platform Admin: Review recorded payments, verify transactions, and reconcile ledger balances.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (94 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `StatusBadge`, `Payment`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 61. DailyCarePage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\DailyCare\DailyCarePage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/DailyCare/DailyCarePage.razor)
- **Route**: `/daily-care`
- **Page Purpose**: Child Daily Care & Wellbeing - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (89 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ChildDailyCareSection`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 62. Settings.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\Settings.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Settings.razor)
- **Route**: `/settings`
- **Page Purpose**: Settings: Configure your daycare experience environment.
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (84 lines)
- **Injections & Dependencies**: `OrganizationService OrgService`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`, `TenantSettingsService TenantSettingsService`
- **Child Components Already Used**: `PageHero`, `SettingCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 63. AuditLogsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\AuditLogsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/AuditLogsPage.razor)
- **Route**: `/audit-logs`
- **Page Purpose**: Audit Logs - Platform Admin: Immutable audit trail of organization status changes, financial reconciliations, and platform actions.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (65 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `LoadingState`, `PlatformAuditLog`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 64. Home.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Home.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Home.razor)
- **Route**: `/home-old`
- **Page Purpose**: Today - EnjoyEveryday day care view
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (57 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `WelcomeHeader`, `DaycarePulse`, `LiveClassroomView`, `DayRhythm`, `AiInsight`, `LittleMoments`, `WorthNoticing`, `QuickActions`, `ClassroomsSection`, `ExperiencesSection`, `CalendarSection`, `ChildrenSection`, `MemoriesSection`, `InsightsSection`, `TeamSection`, `SettingsSection`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 65. LoginPrompt.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/LoginPrompt.razor)
- **Route**: `/login`
- **Page Purpose**: Sign In - EnjoyEveryday Admin
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (48 lines)
- **Injections & Dependencies**: `IConfiguration Configuration`, `IWebHostEnvironment Environment`, `NavigationManager Navigation`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 66. LoginPrompt.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/LoginPrompt.razor)
- **Route**: `/login`
- **Page Purpose**: Sign In - EnjoyEveryday Family
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (47 lines)
- **Injections & Dependencies**: `IConfiguration Configuration`, `IWebHostEnvironment Environment`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 67. AccessDenied.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\AccessDenied.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/AccessDenied.razor)
- **Route**: `/access-denied`
- **Page Purpose**: Access Denied - EnjoyEveryday Admin
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (47 lines)
- **Injections & Dependencies**: `NavigationManager Navigation`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 68. AccessDenied.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\AccessDenied.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/AccessDenied.razor)
- **Route**: `/access-denied`
- **Page Purpose**: Access Denied - EnjoyEveryday Daycare
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (47 lines)
- **Injections & Dependencies**: `NavigationManager Navigation`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 69. SettingsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Operations\SettingsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Operations/SettingsPage.razor)
- **Route**: `/settings`
- **Page Purpose**: System Settings - Platform Admin: Multi-tenant authentication parameters, AI provider keys, and backup policies.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (46 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 70. DiscountsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Billing\DiscountsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Billing/DiscountsPage.razor)
- **Route**: `/discounts`
- **Page Purpose**: Discounts & Offers - Platform Admin: Approved discount programs, promotional periods, and financial fee exemptions.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (46 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `PageHero`, `RightContent`, `StatusBadge`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 71. LoginPrompt.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\LoginPrompt.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/LoginPrompt.razor)
- **Route**: `/login`
- **Page Purpose**: Sign In - EnjoyEveryday Daycare
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (45 lines)
- **Injections & Dependencies**: `IConfiguration Configuration`, `IWebHostEnvironment Environment`
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 72. AdminsPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organizations\AdminsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/AdminsPage.razor)
- **Route**: `/admins`
- **Page Purpose**: Organization Admins - Platform Admin: Assigned daycare directors, owners, and primary billing administrators.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (45 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`, `StatusBadge`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 73. Settings.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organization\Settings.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Settings.razor)
- **Route**: `/organization-settings`
- **Page Purpose**: Settings: Configure your daycare experience environment.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (43 lines)
- **Injections & Dependencies**: `OrganizationService OrgService`, `EnjoyEveryday.Shared.Tenancy.ITenantContext TenantContext`
- **Child Components Already Used**: `PageHero`, `SettingCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 74. Management.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organization\Management.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Management.razor)
- **Route**: `/management`
- **Page Purpose**: Management: A calm view of what is happening.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (41 lines)
- **Injections & Dependencies**: `OrganizationService OrganizationService`, `DashboardService DashboardService`
- **Child Components Already Used**: `PageHero`, `MetricCard`, `BranchMetrics`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 75. BranchesPage.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organizations\BranchesPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organizations/BranchesPage.razor)
- **Route**: `/branches`
- **Page Purpose**: Branches - Platform Admin: Multi-branch configuration and physical campus directory across all daycare organizations.
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (39 lines)
- **Injections & Dependencies**: `IPlatformAdminService PlatformService`
- **Child Components Already Used**: `PageHero`, `RightContent`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 76. Error.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Error.razor)
- **Route**: `/Error`
- **Page Purpose**: Error
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (30 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 77. Error.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Error.razor)
- **Route**: `/Error`
- **Page Purpose**: Error
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (30 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 78. Error.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\Error.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/Error.razor)
- **Route**: `/Error`
- **Page Purpose**: Error
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (30 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: Standard HTML controls
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 79. ClassroomAttendance.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\ClassroomAttendance.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/ClassroomAttendance.razor)
- **Route**: `/management/attendance/classrooms/{ClassroomName}`
- **Page Purpose**: ClassroomAttendance
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (27 lines)
- **Injections & Dependencies**: `NavigationManager Nav`
- **Child Components Already Used**: `BranchDetailSection`, `ToastNotification`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 80. BranchAttendance.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\BranchAttendance.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Attendance/BranchAttendance.razor)
- **Route**: `/management/attendance/branches/{BranchName}`
- **Page Purpose**: BranchAttendance
- **Intended User or Role**: Daycare Director / Center Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (25 lines)
- **Injections & Dependencies**: `NavigationManager Nav`
- **Child Components Already Used**: `BranchDetailSection`, `ToastNotification`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 81. EmergencyFloorMapsPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\SafetyTrust\EmergencyFloorMapsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/EmergencyFloorMapsPage.razor)
- **Route**: `/safety-trust/floor-maps`
- **Page Purpose**: Emergency Floor Maps - Safety & Trust | EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (24 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `EmergencyFloorMapsSection`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 82. EmergencyContactsPage.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\SafetyTrust\EmergencyContactsPage.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SafetyTrust/EmergencyContactsPage.razor)
- **Route**: `/safety-trust/emergency-contacts`
- **Page Purpose**: Emergency Contacts & Printable Card - Safety & Trust | EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (24 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `EmergencyContactsSection`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 83. Planner.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Planner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Planner.razor)
- **Route**: `/planner-old`
- **Page Purpose**: Monthly Planner - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (16 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 84. Planner.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\Planner.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Planner.razor)
- **Route**: `/planner-old`
- **Page Purpose**: Monthly Planner - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (16 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 85. NotFound.razor (Parent.Web)

- **Project**: `EnjoyEveryday.Parent.Web`
- **File Path**: [src\EnjoyEveryday.Parent.Web\Components\Pages\NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Parent.Web/Components/Pages/NotFound.razor)
- **Route**: `/not-found`
- **Page Purpose**: Not Found - EnjoyEveryday
- **Intended User or Role**: Parent / Guardian
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (15 lines)
- **Injections & Dependencies**: `NavigationManager Navigation`
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentButton`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 86. NotFound.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/NotFound.razor)
- **Route**: `/not-found`
- **Page Purpose**: Not Found - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (15 lines)
- **Injections & Dependencies**: `NavigationManager Navigation`
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentButton`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 87. NotFound.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\NotFound.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/NotFound.razor)
- **Route**: `/not-found`
- **Page Purpose**: Not Found - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (15 lines)
- **Injections & Dependencies**: `NavigationManager Navigation`
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentButton`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 88. ChildJourney.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Children\ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/ChildJourney.razor)
- **Route**: `/journey, /journey/{ChildId:guid}`
- **Page Purpose**: Child Journey - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (14 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ChildJourneyView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 89. ChildJourney.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Children\ChildJourney.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Children/ChildJourney.razor)
- **Route**: `/journey, /journey/{ChildId:guid}`
- **Page Purpose**: Child Journey - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (14 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ChildJourneyView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 90. Journey.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\Journey.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Journey.razor)
- **Route**: `/journey-old`
- **Page Purpose**: Child Journey - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (11 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 91. Stories.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Stories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Stories.razor)
- **Route**: `/stories-old`
- **Page Purpose**: Parent Stories - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (11 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 92. Stories.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\Stories.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Stories.razor)
- **Route**: `/stories-old`
- **Page Purpose**: Parent Stories - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (11 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `FluentStack`, `FluentText`, `FluentCard`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 93. SelectOrganization.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\SelectOrganization.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/SelectOrganization.razor)
- **Route**: `/select-organization`
- **Page Purpose**: Select Organization - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (9 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `SelectOrganizationView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 94. SelectOrganization.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\SelectOrganization.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/SelectOrganization.razor)
- **Route**: `/select-organization`
- **Page Purpose**: Select Organization - EnjoyEveryday Platform
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (9 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `SelectOrganizationView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 95. Classrooms.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organization\Classrooms.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Classrooms.razor)
- **Route**: `/classrooms`
- **Page Purpose**: Classrooms - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (8 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ClassroomsDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 96. Classrooms.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\Classrooms.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Classrooms.razor)
- **Route**: `/classrooms`
- **Page Purpose**: Classrooms - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (8 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ClassroomsDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 97. Children.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Children\Children.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Children/Children.razor)
- **Route**: `/children`
- **Page Purpose**: Children Directory - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ChildrenDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 98. Teachers.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Organization\Teachers.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Organization/Teachers.razor)
- **Route**: `/teachers`
- **Page Purpose**: Teachers Directory - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `TeachersDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 99. Harvest.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences\Harvest.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Experiences/Harvest.razor)
- **Route**: `/harvest`
- **Page Purpose**: Harvest & Insights - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `HarvestView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 100. Harvest.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Experiences\Harvest.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/Harvest.razor)
- **Route**: `/harvest`
- **Page Purpose**: Harvest & Insights - EnjoyEveryday
- **Intended User or Role**: Educator / Lead Teacher / Staff
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `HarvestView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 101. Teachers.razor (Daycare.Web)

- **Project**: `EnjoyEveryday.Daycare.Web`
- **File Path**: [src\EnjoyEveryday.Daycare.Web\Components\Pages\Organization\Teachers.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Daycare.Web/Components/Pages/Organization/Teachers.razor)
- **Route**: `/teachers`
- **Page Purpose**: Teachers Directory - EnjoyEveryday
- **Intended User or Role**: Daycare Staff / Educator
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `TeachersDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`

### 102. Children.razor (Admin.Web)

- **Project**: `EnjoyEveryday.Admin.Web`
- **File Path**: [src\EnjoyEveryday.Admin.Web\Components\Pages\Children\Children.razor](file:///C:/MyDrive/ProjectDrive/EnjoyEveryDay/src/EnjoyEveryday.Admin.Web/Components/Pages/Children/Children.razor)
- **Route**: `/children`
- **Page Purpose**: Children Directory - EnjoyEveryday
- **Intended User or Role**: Platform Admin / Organization Owner / Admin
- **Authorization & Tenant-Scope**: Auth: `None`; Scoped via `IUserContext`/`ITenantContext` where applicable.
- **Approximate Size & Complexity**: Low (7 lines)
- **Injections & Dependencies**: None directly injected in .razor
- **Child Components Already Used**: `ChildrenDirectoryView`
- **Recommended Refactoring**: Page is focused and concise; retain current structure without fragmentation.
- **Dependencies & Risk**: Low
- **Current Status**: `Reviewed`
