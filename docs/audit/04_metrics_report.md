# EnjoyEveryday — Metrics Report

## 1. Measurement Methodology & Scope

All metrics in this report were gathered directly from the codebase on **October 18, 2026** using PowerShell 7 scripts, .NET 10.0 CLI build tools, and static inspection.
- **Exclusions**: Build output directories (`bin/`, `obj/`), IDE state (`.vs/`, `.git/`), Python virtual environments (`venv/`, `.venv/`), and third-party vendored assets (`wwwroot/lib/bootstrap`).
- **Scope**: All custom C#, Razor, CSS, SQL, Python, JavaScript, and configuration files under `src/` and `database/`.

---

## 2. Repository Size & Structure Metrics

### Source Code by Language (Excluding Vendored Assets):
| Language / File Type | Extension | File Count | Lines of Code | Percentage of Code |
|:---|:---:|---:|---:|---:|
| **Razor Components & Pages** | `.razor` | 265 | 44,863 | 40.7% |
| **C# Source Code** | `.cs` | 277 | 35,174 | 31.9% |
| **Custom CSS Stylesheets** | `.css` | 127 | 23,653 | 21.5% |
| **SQL Migrations** | `.sql` | 62 | 3,973 | 3.6% |
| **Python AI Agent Code** | `.py` | 38 | 1,989 | 1.8% |
| **Custom JavaScript** | `.js` | 6 | 346 | 0.3% |
| **Configuration Files** | `.json` | 12 | 219 | 0.2% |
| **Total Custom Code** | — | **787** | **110,217** | **100.0%** |

*Note: Third-party vendored assets in `wwwroot/lib/bootstrap` contain an additional 122,970 lines of CSS and 42,222 lines of JS across the three web applications.*

### Project Line Count Distribution:
| Project Name | Path | File Count | Lines of Code | Primary Role |
|:---|:---|---:|---:|:---|
| **EnjoyEveryday.Daycare.Web** | `src/EnjoyEveryday.Daycare.Web` | 105 | 31,200 | Teacher/staff Blazor Server application |
| **EnjoyEveryday.UI.Shared** | `src/EnjoyEveryday.UI.Shared` | 223 | 20,899 | Reusable Blazor components & modals |
| **EnjoyEveryday.Admin.Web** | `src/EnjoyEveryday.Admin.Web` | 70 | 16,232 | Platform & org admin Blazor application |
| **EnjoyEveryday.Application** | `src/EnjoyEveryday.Application` | 64 | 15,564 | Business services and orchestration |
| **EnjoyEveryday.Infrastructure** | `src/EnjoyEveryday.Infrastructure` | 35 | 6,406 | Repositories, database migrations, gateways |
| **EnjoyEveryday.Application.Tests** | `src/EnjoyEveryday.Application.Tests` | 16 | 5,914 | Unit tests (NUnit) |
| **EnjoyEveryday.Parent.Web** | `src/EnjoyEveryday.Parent.Web` | 28 | 3,737 | Parent portal Blazor application |
| **EnjoyEveryday.Domain** | `src/EnjoyEveryday.Domain` | 120 | 3,310 | Entity models and repository interfaces |
| **EnjoyEveryday.AiAgent** | `src/EnjoyEveryday.AiAgent` | 38 | 1,989 | Standalone Python FastAPI AI sidecar |
| **EnjoyEveryday.Api** | `src/EnjoyEveryday.Api` | 16 | 573 | REST API controllers (abandoned) |
| **EnjoyEveryday.Shared** | `src/EnjoyEveryday.Shared` | 7 | 332 | Common tenancy and authorization models |
| **UpdatePerms** | `src/UpdatePerms` | 2 | 75 | Scratch permissions migration utility |
| **EnjoyEveryday.MemoryLayer.Tests** | `src/EnjoyEveryday.MemoryLayer.Tests` | 1 | 13 | Placeholder scaffold test project |

---

## 3. Structural & Domain Metrics

| Metric | Measured Value | Measurement Source | Interpretation |
|:---|---:|:---|:---|
| **Projects in Solution** | 9 | `EnjoyEveryday.slnx` | Includes Web, App, Domain, Infra. Excludes test projects. |
| **Projects in Repository** | 13 | Directory scan of `src/` | 4 projects exist outside the active solution file. |
| **Database Migrations** | 62 | `database/migrations/*.sql` | Spanning initial schema to advanced warehouse ops. |
| **Database Tables** | 132 | `CREATE TABLE` pattern search | Highly granular relational model. |
| **API Controllers** | 11 | `EnjoyEveryday.Api/Controllers` | Early version controllers; none updated since Oct 9. |
| **Minimal API Endpoints** | 18 | `MapAuthEndpoints`, `MapBillingEndpoints`, etc. | Direct ASP.NET Core endpoints hosted inside Web apps. |
| **Total Automated Tests** | 129 | NUnit `[Test]` & `[TestCase]` | 128 in Application.Tests, 1 in MemoryLayer.Tests. |
| **Test Compilation Status** | **Failing (9 errors)** | `dotnet build Application.Tests` | Build broken due to outdated repository mocks. |
| **Compiler Warnings** | 8 warnings | `dotnet build EnjoyEveryday.slnx` | All CS8600/CS8602 nullability warnings in `AuthEndpoints.cs`. |

---

## 4. Complexity Hotspots: Top 15 Largest C# Files

| File Path | Lines | Size (KB) | Responsibilities / Complexity Indicators |
|:---|---:|---:|:---|
| `src/EnjoyEveryday.Application/Services/Commercial/MaterialMarketplaceService.cs` | 1,510 | 71.7 | Catalog management, checkout, warehouse dispatches, loans, cancellations. |
| `src/EnjoyEveryday.Daycare.Web/Components/Pages/Experiences/StandaloneHtmlGenerator.cs` | 1,028 | 47.2 | Generates complete offline HTML/CSS documents for experience storybooks. |
| `src/EnjoyEveryday.Application/Services/PlatformAdmin/PlatformAdminService.cs` | 965 | 43.2 | Platform stats, tenant provisioning, system audit logs, direct SQL queries. |
| `src/EnjoyEveryday.Infrastructure/Repositories/Commercial/MaterialMarketplaceRepository.cs` | 898 | 46.7 | Inventory balances, multi-item order persistence, stock reservations. |
| `src/EnjoyEveryday.Application/Services/Commercial/BillingService.cs` | 826 | 41.9 | Invoicing, payment ledger, Razorpay integration, webhook processing. |
| `src/EnjoyEveryday.Infrastructure/Repositories/Commercial/BillingRepository.cs` | 727 | 34.2 | Invoices, payments, line items, N+1 query patterns. |
| `src/EnjoyEveryday.Application/Services/Children/ChildDailyCareService.cs` | 682 | 32.9 | Nap tracking, meals, medications, diaper logs, handovers. |
| `src/EnjoyEveryday.Application/Services/SafetyTrust/ConsentService.cs` | 667 | 32.8 | Multi-signature workflows, consent withdrawal, audit logging. |
| `src/EnjoyEveryday.Application/Services/Organization/DaycareOperationsBoardService.cs` | 666 | 32.3 | Live classroom dashboard, direct SQL queries across 6 tables. |
| `src/EnjoyEveryday.Application.Tests/RazorpayAndCashPaymentLedgerTests.cs` | 584 | 28.4 | Test suite with outdated `IRazorpayGateway` mock. |
| `src/EnjoyEveryday.Application.Tests/FloorMapAndEmergencyContactTests.cs` | 566 | 24.4 | Comprehensive authorization and file validation tests. |
| `src/EnjoyEveryday.Application.Tests/ManualAndAutomaticBillingSecurityTests.cs` | 558 | 26.7 | Multi-tenant billing isolation test cases. |
| `src/EnjoyEveryday.Application/Services/Learning/ChildDevelopmentPortfolioService.cs` | 514 | 24.7 | Milestone aggregation, direct SQL queries with Dapper. |
| `src/EnjoyEveryday.Application.Tests/ChildDailyCareTests.cs` | 512 | 23.8 | Test suite with outdated `IChildDailyCareRepository` mock. |
| `src/EnjoyEveryday.Application/Services/Learning/MemoryService.cs` | 497 | 22.3 | Memory timeline management, direct Dapper SQL queries. |

---

## 5. UI Complexity Hotspots: Top 15 Largest Razor Components

| Component / Page Path | Lines | Size (KB) | Complexity Characteristics |
|:---|---:|---:|:---|
| `src/EnjoyEveryday.Admin.Web/.../WarehouseInventoryPage.razor` | 1,418 | 62.3 | Monolithic inventory management, batch adjustments, inline filters. |
| `src/EnjoyEveryday.Daycare.Web/.../MaterialOrdersPage.razor` | 1,313 | 55.3 | Order tracker, return flow, drawer modals, inlined C# state. |
| `src/EnjoyEveryday.Admin.Web/.../BillingPage.razor` | 1,196 | 65.1 | Invoice creation modal, line items table, payment recording. |
| `src/EnjoyEveryday.Daycare.Web/.../EnrollmentPipeline.razor` | 1,165 | 49.8 | Kanban board, lead conversion, pipeline stage modals. |
| `src/EnjoyEveryday.Admin.Web/.../AccountBookPage.razor` | 1,116 | 40.3 | 250+ lines inline `<style>`, transaction investigation drawer. |
| `src/EnjoyEveryday.UI.Shared/.../ChildDailyCareSection.razor` | 1,094 | 53.8 | Multi-tab daily care entry (meals, sleep, toilet, medication). |
| `src/EnjoyEveryday.Daycare.Web/.../Memories.razor` | 1,063 | 34.8 | Timeline view, filtering, photo lightbox, inline script interop. |
| `src/EnjoyEveryday.Admin.Web/.../MarketplaceOperationsPage.razor` | 985 | 43.6 | Catalog management, fulfillment actions, stock alerts. |
| `src/EnjoyEveryday.Daycare.Web/.../ExperiencePlanning.razor` | 965 | 54.8 | Calendar grid, drag-and-drop slots, quick edit modals. |
| `src/EnjoyEveryday.UI.Shared/.../EmergencyFloorMapsSection.razor` | 935 | 45.9 | Canvas floor map marker placement, image pan/zoom. |
| `src/EnjoyEveryday.Daycare.Web/.../ExperienceStudio.razor` | 916 | 50.1 | Experience editor (duplicated in Admin.Web with 880 lines). |
| `src/EnjoyEveryday.Daycare.Web/.../MaterialsMarketplace.razor` | 915 | 35.7 | Product catalog, cart drawer, Razorpay modal integration. |
| `src/EnjoyEveryday.Daycare.Web/.../Intelligence.razor` | 847 | 42.1 | Analytics cards, operational recommendations, charts. |
| `src/EnjoyEveryday.Daycare.Web/.../RhythmHistory.razor` | 787 | 29.7 | Historical timeline explorer, date picker, classroom filters. |
| `src/EnjoyEveryday.Daycare.Web/.../StaffRoster.razor` | 768 | 28.0 | Staff scheduling grid, shift compliance, leave approvals. |

---

## 6. Query Pattern & Database Access Analysis

1. **N+1 Query Pattern in Billing Repository**:
   - Location: `BillingRepository.cs` (lines 39–45 and 78–83).
   - Mechanism: Loops over fetched invoices and makes two sequential database roundtrips for each:
     ```csharp
     foreach (var row in invoices) {
         inv.LineItems = await GetLineItemsForInvoiceInternalAsync(conn, inv.Id);
         inv.Payments = await GetPaymentsForInvoiceInternalAsync(conn, inv.Id);
     }
     ```
   - Scale: 100 invoices = 201 database queries; 1,000 invoices = 2,001 database queries.
2. **Direct Application Layer Database Access**:
   - 10 application services bypass repositories and execute raw Dapper queries.
   - Example (`StaffRosterService.cs` lines 121–130):
     `conn.QueryFirstOrDefaultAsync<BranchRow>("SELECT id AS Id, name AS Name FROM branches WHERE id = @BranchId", ...);`
3. **Leaked Presentation Layer Database Access**:
   - Location: `ActiveSessionModal.razor` lines 253–259.
   - Presentation component runs raw Dapper SQL without tenant scoping:
     `conn.QueryAsync<Guid>("SELECT child_id FROM child_attendance WHERE classroom_id = @Id AND attendance_date = @Date ...")`
