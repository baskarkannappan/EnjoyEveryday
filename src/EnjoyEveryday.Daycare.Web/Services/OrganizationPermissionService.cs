using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Daycare.Web.Services;

public sealed class FeatureDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public IReadOnlyList<string> Routes { get; init; } = Array.Empty<string>();
    public HashSet<string> DefaultAllowedRoles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IOrganizationPermissionService
{
    IReadOnlyList<FeatureDefinition> GetAllFeatures();
    IReadOnlyList<string> GetConfigurableRoles();
    string GetRoleDisplayName(string roleName);
    Task<Dictionary<(string RoleName, string FeatureKey), bool>> GetPermissionsMatrixAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task SavePermissionsMatrixAsync(Guid tenantId, Guid organizationId, Dictionary<(string RoleName, string FeatureKey), bool> matrix, CancellationToken ct = default);
    Task ResetPermissionsMatrixAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<bool> IsFeatureAllowedAsync(Guid tenantId, Guid organizationId, string roleName, string featureKey, CancellationToken ct = default);
    Task<bool?> CheckRouteCustomAllowedAsync(Guid tenantId, Guid organizationId, string roleName, string relativePath, CancellationToken ct = default);
}

public sealed class OrganizationPermissionService : IOrganizationPermissionService
{
    private readonly IDbConnectionFactory _db;

    public OrganizationPermissionService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public static readonly string[] ConfigurableRoles = new[]
    {
        "OrganizationAdmin",
        "BranchAdmin",
        "LeadTeacher",
        "Teacher"
    };

    public IReadOnlyList<string> GetConfigurableRoles() => ConfigurableRoles;

    public string GetRoleDisplayName(string roleName)
    {
        return roleName switch
        {
            "OrganizationAdmin" => "Organization Admin",
            "BranchAdmin" => "Branch Admin",
            "LeadTeacher" => "Lead Teacher",
            "Teacher" => "Teacher",
            _ => roleName
        };
    }

    private static readonly List<FeatureDefinition> Features = new()
    {
        // 1. Executive & Governance
        new FeatureDefinition
        {
            Key = "page.org_dashboard",
            Name = "Organization Dashboard",
            Description = "Executive macro metrics, branch roll-up stats, and operational overview",
            Category = "Executive & Governance",
            Routes = new[] { "/management/dashboard", "/org-dashboard" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.org_insights",
            Name = "Intelligence & Insights",
            Description = "Attendance trends, capacity forecasting, and AI weekly executive summaries",
            Category = "Executive & Governance",
            Routes = new[] { "/intelligence", "/insights" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.org_branches",
            Name = "Branch Directory & Overview",
            Description = "Directory of all physical daycare branches, capacity, and branch profiles",
            Category = "Executive & Governance",
            Routes = new[] { "/organization", "/organization/branches" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "action.branch_create",
            Name = "Create / Provision New Branch",
            Description = "Add a brand new daycare center branch to the organization",
            Category = "Executive & Governance",
            Routes = new[] { "/management/branches/new" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.org_billing",
            Name = "Billing, Fees & Invoicing",
            Description = "Organization fee structures, tuition invoices, payments, and financial summaries",
            Category = "Executive & Governance",
            Routes = new[] { "/management/billing", "/billing", "/billing/invoices", "/billing/reports" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.org_settings",
            Name = "Organization Settings & Profile",
            Description = "Daycare organization identity, operational hours, branding, and defaults",
            Category = "Executive & Governance",
            Routes = new[] { "/settings", "/organization/settings", "/organization/profile" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.roles_matrix",
            Name = "Role & Permission Matrix",
            Description = "Configure and manage role-based page and feature access across the organization",
            Category = "Executive & Governance",
            Routes = new[] { "/management/roles", "/roles" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        },

        // 2. Branch Operations
        new FeatureDefinition
        {
            Key = "page.branch_dashboard",
            Name = "Branch Operations Dashboard",
            Description = "Facility daily metrics, live teacher-to-child ratios, and room activity",
            Category = "Branch Operations",
            Routes = new[] { "/management/branch-dashboard", "/branch-dashboard" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.staff_roster",
            Name = "Staff Roster & Shift Schedules",
            Description = "Manage staff shift schedules, leave requests, and floating coverage",
            Category = "Branch Operations",
            Routes = new[] { "/management/roster" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.users_by_role",
            Name = "Staff Assignments & Roles",
            Description = "Manage user credentials, branch assignments, and role bindings",
            Category = "Branch Operations",
            Routes = new[] { "/users-by-role", "/management/users-by-role" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.safety_trust",
            Name = "Safety, Trust & Compliance",
            Description = "Facility emergency protocols, child safety documentation, and audit records",
            Category = "Branch Operations",
            Routes = new[] { "/safety-trust", "/safety-compliance", "/policies" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher" }
        },
        new FeatureDefinition
        {
            Key = "page.floor_maps",
            Name = "Emergency Floor Plans & Assembly",
            Description = "Interactive floor diagrams, fire evacuation routes, and designated assembly points",
            Category = "Branch Operations",
            Routes = new[] { "/safety-trust/floor-maps" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.emergency_contacts",
            Name = "Emergency Contacts & Responders",
            Description = "Facility emergency contacts, pediatric helpline, and local authorities",
            Category = "Branch Operations",
            Routes = new[] { "/safety-trust/emergency-contacts" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },

        // 3. Classrooms & Children
        new FeatureDefinition
        {
            Key = "page.children_directory",
            Name = "Children Directory & Profiles",
            Description = "Browse enrolled children, view contact summaries, and classroom placements",
            Category = "Classrooms & Children",
            Routes = new[] { "/children" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "action.child_enroll",
            Name = "Enroll New Child (Intake)",
            Description = "Complete child enrollment wizard, guardian registration, and health forms",
            Category = "Classrooms & Children",
            Routes = new[] { "/children/add" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "action.child_edit",
            Name = "Edit Child Profile & Placement",
            Description = "Update child medical alerts, contacts, classroom assignment, and dietary notes",
            Category = "Classrooms & Children",
            Routes = new[] { "/children/edit" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher" }
        },
        new FeatureDefinition
        {
            Key = "action.child_delete",
            Name = "Archive / Graduate Child Record",
            Description = "Deactivate enrollment, archive child records, and mark graduation status",
            Category = "Classrooms & Children",
            Routes = Array.Empty<string>(),
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.admissions_pipeline",
            Name = "Admissions Pipeline & Waitlist",
            Description = "Manage prospective inquiries, facility tours, and pending enrollment applications",
            Category = "Classrooms & Children",
            Routes = new[] { "/enrollment", "/management/enrollment" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.classrooms_directory",
            Name = "Classrooms Directory & Rooms",
            Description = "Browse active classrooms, designated age brackets, room capacity, and educators",
            Category = "Classrooms & Children",
            Routes = new[] { "/classrooms" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "action.classroom_create",
            Name = "Create New Classroom",
            Description = "Add a new room with age range, capacity limit, and default room settings",
            Category = "Classrooms & Children",
            Routes = new[] { "/classrooms/new" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "action.classroom_edit",
            Name = "Edit Classroom Details",
            Description = "Update classroom parameters, room capacity, and active educator assignments",
            Category = "Classrooms & Children",
            Routes = new[] { "/classrooms/edit" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.child_journey",
            Name = "Child Development Journey",
            Description = "Portfolio timelines, learning milestones, and educator observations",
            Category = "Classrooms & Children",
            Routes = new[] { "/journey" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },

        // 4. Daily Care & Learning
        new FeatureDefinition
        {
            Key = "page.daily_care_plans",
            Name = "Care Plans (Medical, Diets & Allergies)",
            Description = "Classroom medical board with critical allergy badges and dietary directives",
            Category = "Daily Care & Learning",
            Routes = new[] { "/daily-care/plans" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.daily_care_supplies",
            Name = "Cubby Inventory & Restock Checklist",
            Description = "Track personal cubby supplies (diapers, extra clothes, formula, wipes)",
            Category = "Daily Care & Learning",
            Routes = new[] { "/daily-care/supplies" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.daily_care_desk",
            Name = "Daily Care Activity Desk",
            Description = "Log daily diapering, bottles, meals, nap times, and mood observations",
            Category = "Daily Care & Learning",
            Routes = new[] { "/daily-care" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.teacher_workspace",
            Name = "Teacher Classroom Workspace",
            Description = "Dedicated tablet workflow for room educators to run daily routines",
            Category = "Daily Care & Learning",
            Routes = new[] { "/classroom", "/my-classroom" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.today_hub",
            Name = "Today Hub & Real-time Flow",
            Description = "Daily live schedule, current room activities, and quick check-in shortcuts",
            Category = "Daily Care & Learning",
            Routes = new[] { "/", "/today" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.experiences",
            Name = "Experience & Activity Library",
            Description = "Catalog of developmental learning activities, sensory experiences, and rhythm cards",
            Category = "Daily Care & Learning",
            Routes = new[] { "/experiences" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.experience_studio",
            Name = "Experience Studio (Authoring)",
            Description = "Design, craft, and publish original developmental curriculum experiences",
            Category = "Daily Care & Learning",
            Routes = new[] { "/studio" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher" }
        },
        new FeatureDefinition
        {
            Key = "page.experience_planning",
            Name = "Curriculum Weekly Planner",
            Description = "Plan and schedule developmental learning experiences on the weekly calendar",
            Category = "Daily Care & Learning",
            Routes = new[] { "/planning" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher" }
        },
        new FeatureDefinition
        {
            Key = "page.rhythm_history",
            Name = "Rhythm & Classroom Flow History",
            Description = "Historical records of daily activity execution, duration, and classroom cadence",
            Category = "Daily Care & Learning",
            Routes = new[] { "/rhythm-history" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher" }
        },

        // 5. Attendance Management
        new FeatureDefinition
        {
            Key = "page.attendance_overview",
            Name = "Daily Attendance & Roll Call",
            Description = "Live classroom attendance check-in/out, room headcounts, and absent tracker",
            Category = "Attendance Management",
            Routes = new[] { "/attendance", "/management/attendance" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "action.attendance_checkin",
            Name = "Record Child Check-In / Check-Out",
            Description = "Mark arrival timestamp, departing guardian, and reason for absence",
            Category = "Attendance Management",
            Routes = Array.Empty<string>(),
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "page.branch_attendance",
            Name = "Branch Attendance Roll-up",
            Description = "Facility-wide attendance summaries across all rooms and multi-day reports",
            Category = "Attendance Management",
            Routes = new[] { "/management/attendance/branches" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "page.classroom_attendance",
            Name = "Classroom Attendance Ledger",
            Description = "Room-specific attendance logs, historical records, and signature sheets",
            Category = "Attendance Management",
            Routes = new[] { "/management/attendance/classrooms" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },

        // 6. Staff & Educators
        new FeatureDefinition
        {
            Key = "page.teachers_directory",
            Name = "Teachers & Staff Directory",
            Description = "Browse teacher cards, assigned classrooms, qualifications, and specialties",
            Category = "Staff & Educators",
            Routes = new[] { "/teachers" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin", "LeadTeacher", "Teacher" }
        },
        new FeatureDefinition
        {
            Key = "action.teacher_create",
            Name = "Add New Teacher (Staff Wizard)",
            Description = "Onboard educators, submit credentials, and assign primary classrooms",
            Category = "Staff & Educators",
            Routes = new[] { "/teachers/new" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "action.teacher_edit",
            Name = "Edit Teacher Profile",
            Description = "Update educator credentials, background checks, and room assignments",
            Category = "Staff & Educators",
            Routes = new[] { "/teachers/edit" },
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin", "BranchAdmin" }
        },
        new FeatureDefinition
        {
            Key = "action.teacher_delete",
            Name = "Deactivate Teacher Access",
            Description = "Revoke system credentials and mark educator profile as deactivated",
            Category = "Staff & Educators",
            Routes = Array.Empty<string>(),
            DefaultAllowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OrganizationAdmin" }
        }
    };

    public IReadOnlyList<FeatureDefinition> GetAllFeatures() => Features;

    private sealed class PermissionRecord
    {
        public string role_name { get; set; } = string.Empty;
        public string feature_key { get; set; } = string.Empty;
        public bool is_allowed { get; set; }
    }

    public async Task<Dictionary<(string RoleName, string FeatureKey), bool>> GetPermissionsMatrixAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var result = new Dictionary<(string RoleName, string FeatureKey), bool>();

        // 1. Initialize with system defaults
        foreach (var role in ConfigurableRoles)
        {
            foreach (var feat in Features)
            {
                // Org Admin has access to all by default; other roles follow DefaultAllowedRoles
                bool defaultAllowed = role.Equals("OrganizationAdmin", StringComparison.OrdinalIgnoreCase)
                    || feat.DefaultAllowedRoles.Contains(role);

                result[(role, feat.Key)] = defaultAllowed;
            }
        }

        if (tenantId == Guid.Empty || organizationId == Guid.Empty)
        {
            return result;
        }

        // 2. Overlay organization-specific overrides from the database
        try
        {
            using var conn = await _db.CreateConnectionAsync(ct);
            var records = await conn.QueryFunctionAsync<PermissionRecord>(
                "fn_get_organization_role_permissions",
                new { p_tenant_id = tenantId, p_organization_id = organizationId },
                cancellationToken: ct);

            foreach (var row in records)
            {
                result[(row.role_name, row.feature_key)] = row.is_allowed;
            }
        }
        catch (Exception)
        {
            // If table/function is not reachable, fallback to default matrix smoothly
        }

        return result;
    }

    public async Task SavePermissionsMatrixAsync(
        Guid tenantId,
        Guid organizationId,
        Dictionary<(string RoleName, string FeatureKey), bool> matrix,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("TenantId and OrganizationId must be valid Guids to persist permissions.");
        }

        using var conn = await _db.CreateConnectionAsync(ct);

        foreach (var kvp in matrix)
        {
            await conn.ExecuteProcedureAsync(
                "sp_upsert_organization_role_permission",
                new
                {
                    p_tenant_id = tenantId,
                    p_organization_id = organizationId,
                    p_role_name = kvp.Key.RoleName,
                    p_feature_key = kvp.Key.FeatureKey,
                    p_is_allowed = kvp.Value
                },
                cancellationToken: ct);
        }
    }

    public async Task ResetPermissionsMatrixAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty || organizationId == Guid.Empty)
        {
            return;
        }

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_reset_organization_role_permissions",
            new
            {
                p_tenant_id = tenantId,
                p_organization_id = organizationId
            },
            cancellationToken: ct);
    }

    public async Task<bool> IsFeatureAllowedAsync(
        Guid tenantId,
        Guid organizationId,
        string roleName,
        string featureKey,
        CancellationToken ct = default)
    {
        var matrix = await GetPermissionsMatrixAsync(tenantId, organizationId, ct);
        if (matrix.TryGetValue((roleName, featureKey), out var allowed))
        {
            return allowed;
        }

        var feat = Features.FirstOrDefault(f => f.Key.Equals(featureKey, StringComparison.OrdinalIgnoreCase));
        if (feat == null) return true;

        return roleName.Equals("OrganizationAdmin", StringComparison.OrdinalIgnoreCase) 
            || feat.DefaultAllowedRoles.Contains(roleName);
    }

    public async Task<bool?> CheckRouteCustomAllowedAsync(
        Guid tenantId,
        Guid organizationId,
        string roleName,
        string relativePath,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty || organizationId == Guid.Empty) return null;

        var clean = relativePath.Split('?')[0].Trim().ToLowerInvariant();
        if (!clean.StartsWith('/')) clean = "/" + clean;

        // Find matching features for this route
        var matching = Features.Where(f => f.Routes.Any(r => 
            clean == r.ToLowerInvariant() || clean.StartsWith(r.ToLowerInvariant() + "/"))).ToList();

        if (matching.Count == 0) return null; // Not governed by matrix overrides

        var matrix = await GetPermissionsMatrixAsync(tenantId, organizationId, ct);

        // If any matching feature is explicitly configured for this role
        foreach (var feat in matching)
        {
            if (matrix.TryGetValue((roleName, feat.Key), out var allowed))
            {
                return allowed;
            }
        }

        return null;
    }
}
