namespace EnjoyEveryday.Shared.Authorization;

/// <summary>How far a role's permissions reach. Scope is enforced by IResourceAuthorizer, never by the role name alone.</summary>
public enum RoleScope
{
    /// <summary>Platform operations only. Grants NO tenant business data.</summary>
    Platform,
    /// <summary>Every branch/classroom/child inside the ACTIVE organization only.</summary>
    Organization,
    /// <summary>Only branches assigned to the user (user_branch_assignments).</summary>
    Branch,
    /// <summary>Only classrooms assigned to the user (classroom_teachers) and the children in them.</summary>
    Classroom,
    /// <summary>Only children reached through a verified, non-revoked family relationship.</summary>
    Family
}

/// <summary>
/// Code mirror of the role→permission seed in migration 044. Used for privilege-escalation checks
/// (a caller can never grant a role that holds a permission they do not hold) and by tests.
/// Matrix markers F/M/E/V/S/A are granted; "C" (conditional) items are deliberately NOT in a default
/// template — they need an explicit grant — except where scope enforcement is the condition
/// (e.g. teachers viewing children in their own classroom).
/// </summary>
public static class RoleTemplates
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string OrganizationOwner = "OrganizationOwner";
    public const string OrganizationAdmin = "OrganizationAdmin";
    public const string BranchAdmin = "BranchAdmin";
    public const string DaycareTeacher = "DaycareTeacher";
    public const string ClassroomTeacher = "ClassroomTeacher";
    public const string Parent = "Parent";

    // Legacy role names that already exist in data. They resolve to the nearest persona template.
    private static readonly Dictionary<string, string> LegacyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Daycare Admin"] = OrganizationAdmin,
        ["Admin"] = OrganizationAdmin,
        ["Teacher"] = ClassroomTeacher,
        ["AssistantTeacher"] = DaycareTeacher
    };

    private static readonly string[] OrganizationAdminPermissions =
    {
        Permissions.OrganizationManage, Permissions.OrganizationBillingView, Permissions.BranchManage, Permissions.ClassroomManage,
        Permissions.StaffManage, Permissions.RoleAssign, Permissions.AuditView,
        Permissions.ChildView, Permissions.ChildManage, Permissions.ChildEnrollmentManage,
        Permissions.FamilyManage, Permissions.FamilyVerify,
        Permissions.ExperienceView, Permissions.ExperienceCreate, Permissions.ExperienceApprove,
        Permissions.ExperienceSchedule, Permissions.ExperiencePlanSubmit, Permissions.ExperiencePlanApprove,
        Permissions.ExperienceExecute, Permissions.ChildObserve, Permissions.ObservationApprove,
        Permissions.JourneyViewInternal, Permissions.JourneyPublish, Permissions.AiUse,
        Permissions.AttendanceView, Permissions.AttendanceRecord, Permissions.AttendanceCorrect, Permissions.AttendanceApprove,
        Permissions.CareView, Permissions.CarePlanManage,
        Permissions.SafetyManage, Permissions.SafetyMapManage, Permissions.SafetyMapPublish, Permissions.SafetyMapView,
        Permissions.EmergencyContactsManage, Permissions.SafetyReport,
        Permissions.ConsentManage, Permissions.ConsentIssue, Permissions.ConsentReview, Permissions.ConsentView,
        Permissions.EventCreate, Permissions.EventManage, Permissions.EventApproved, Permissions.EventPublished, Permissions.EventCancelled,
        Permissions.CommsDraft, Permissions.CommsSend, Permissions.CommsApprove, Permissions.CommsView,
        Permissions.InsightsOrgView, Permissions.InsightsBranchView, Permissions.InsightsClassroomView,
        Permissions.InsightsOrgGenerate, Permissions.ExportOrgReports, Permissions.ExportChildReports
    };

    private static readonly string[] BranchAdminPermissions =
    {
        Permissions.BranchManage, Permissions.ClassroomManage, Permissions.StaffManage, Permissions.AuditView,
        Permissions.ChildView, Permissions.ChildManage, Permissions.ChildEnrollmentManage,
        Permissions.FamilyManage, Permissions.FamilyVerify,
        Permissions.ExperienceView, Permissions.ExperienceSchedule, Permissions.ExperiencePlanSubmit, Permissions.ExperiencePlanApprove,
        Permissions.ExperienceExecute, Permissions.ChildObserve, Permissions.ObservationApprove,
        Permissions.JourneyViewInternal, Permissions.JourneyPublish, Permissions.AiUse,
        Permissions.AttendanceView, Permissions.AttendanceRecord, Permissions.AttendanceCorrect, Permissions.AttendanceApprove,
        Permissions.CareView, Permissions.CarePlanManage,
        Permissions.SafetyMapManage, Permissions.SafetyMapPublish, Permissions.SafetyMapView,
        Permissions.EmergencyContactsManage, Permissions.SafetyReport,
        Permissions.ConsentIssue, Permissions.ConsentReview, Permissions.ConsentView,
        Permissions.EventCreate, Permissions.EventManage, Permissions.EventApproved, Permissions.EventPublished, Permissions.EventCancelled,
        Permissions.CommsDraft, Permissions.CommsSend, Permissions.CommsApprove, Permissions.CommsView,
        Permissions.InsightsBranchView, Permissions.InsightsClassroomView
    };

    // Daycare and classroom teachers share the same default template in the matrix; they differ only
    // by which assignment records (branch work vs classroom) scope them.
    private static readonly string[] TeacherPermissions =
    {
        Permissions.ChildView, Permissions.ExperienceView, Permissions.ExperienceSchedule,
        Permissions.ExperiencePlanSubmit, Permissions.ExperienceExecute,
        Permissions.ChildObserve, Permissions.AiUse,
        Permissions.AttendanceView, Permissions.AttendanceRecord,
        Permissions.CareView, Permissions.CareRecord,
        Permissions.SafetyMapView, Permissions.SafetyReport,
        Permissions.ConsentView,
        Permissions.CommsDraft, Permissions.CommsView
    };

    private static readonly string[] ParentPermissions =
    {
        Permissions.ChildView, Permissions.FamilyView, Permissions.SafetyReport, Permissions.ConsentView, Permissions.CommsView
    };

    private static readonly string[] PlatformAdminPermissions =
    {
        Permissions.PlatformConfigure, Permissions.TenantManage, Permissions.PlatformPlans, Permissions.PlatformHealth,
        Permissions.PlatformSecurityEvents, Permissions.PlatformRolesManage, Permissions.PlatformBillingManage
        // Permissions.PlatformSupportAccess is deliberately absent: it is only usable with a time-boxed support grant.
    };

    private static readonly Dictionary<string, (RoleScope Scope, HashSet<string> Permissions)> Templates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [PlatformAdmin] = (RoleScope.Platform, Set(PlatformAdminPermissions)),
            [OrganizationOwner] = (RoleScope.Organization, Set(OrganizationAdminPermissions)),
            [OrganizationAdmin] = (RoleScope.Organization, Set(OrganizationAdminPermissions)),
            [BranchAdmin] = (RoleScope.Branch, Set(BranchAdminPermissions)),
            [DaycareTeacher] = (RoleScope.Classroom, Set(TeacherPermissions)),
            [ClassroomTeacher] = (RoleScope.Classroom, Set(TeacherPermissions)),
            [Parent] = (RoleScope.Family, Set(ParentPermissions))
        };

    private static HashSet<string> Set(IEnumerable<string> p) => new(p, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> RoleNames => Templates.Keys;

    public static string Resolve(string roleName) =>
        LegacyAliases.TryGetValue(roleName, out var canonical) ? canonical : roleName;

    public static bool IsKnown(string roleName) => Templates.ContainsKey(Resolve(roleName));

    /// <summary>All stored names (canonical + legacy aliases) that resolve to the same persona as <paramref name="roleName"/>.</summary>
    public static string[] EquivalentNames(string roleName)
    {
        var canonical = Resolve(roleName);
        return Templates.Keys.Concat(LegacyAliases.Keys)
            .Where(n => string.Equals(Resolve(n), canonical, StringComparison.OrdinalIgnoreCase))
            .Append(roleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static RoleScope? ScopeOf(string roleName) =>
        Templates.TryGetValue(Resolve(roleName), out var t) ? t.Scope : null;

    /// <summary>Default permissions of a role; unknown roles have NONE (deny by default).</summary>
    public static IReadOnlySet<string> PermissionsOf(string roleName) =>
        Templates.TryGetValue(Resolve(roleName), out var t) ? t.Permissions : new HashSet<string>();
}
