namespace EnjoyEveryday.Shared.Authorization;

/// <summary>
/// Explicit permission definitions. Existing names (tenant.manage, child.manage, ...) are kept
/// because services already check them; new fine-grained names separate create / submit /
/// review / approve / publish / export / send as required by the role matrix.
/// A permission says WHAT may be done; resource scope (see IResourceAuthorizer) says WHERE.
/// </summary>
public static class Permissions
{
    // ---- Platform (never grants tenant business data) ----
    public const string PlatformConfigure = "platform.configure";
    public const string TenantManage = "tenant.manage";
    public const string PlatformPlans = "platform.plans";
    public const string PlatformHealth = "platform.health";
    public const string PlatformSecurityEvents = "platform.security.view";
    public const string PlatformRolesManage = "platform.roles.manage";
    public const string PlatformBillingManage = "platform.billing.manage";
    public const string PlatformCashCollect = "platform.billing.cash_collect";
    public const string PlatformWarehouseManage = "platform.warehouse.manage";
    public const string PlatformMarketplaceOperations = "platform.marketplace.operations";
    /// <summary>Required (plus an active, purpose-limited support grant) for any tenant business-data access.</summary>
    public const string PlatformSupportAccess = "platform.support.access";

    // ---- Materials Marketplace & Warehouse ----
    public const string MaterialsView = "materials.view";
    public const string MaterialsOrder = "materials.order";

    // ---- Organization / branch / classroom administration ----
    public const string OrganizationManage = "organization.manage";
    public const string OrganizationBillingView = "billing.view";
    public const string BranchManage = "branch.manage";
    public const string ClassroomManage = "classroom.manage";
    public const string StaffManage = "user.manage";
    public const string RoleAssign = "role.assign";
    public const string StaffHrView = "staff.hr.view";
    public const string AuditView = "audit.view";

    // ---- Children and families ----
    public const string ChildView = "child.view";
    public const string ChildManage = "child.manage";
    public const string ChildEnrollmentManage = "child.enrollment.manage";
    public const string FamilyManage = "family.manage";
    public const string FamilyVerify = "family.verify";
    public const string FamilyView = "family.view"; // parent portal access (still needs verified relationship)

    // ---- Learning ----
    public const string ExperienceView = "experience.view";
    public const string ExperienceCreate = "experience.create";
    public const string ExperienceApprove = "experience.approve";
    public const string ExperienceSchedule = "experience.schedule";
    public const string ExperiencePlanSubmit = "experience.plan.submit";
    public const string ExperiencePlanApprove = "experience.plan.approve";
    public const string ExperienceExecute = "experience.execute";
    public const string ChildObserve = "child.observe";
    public const string ObservationApprove = "observation.approve";
    public const string JourneyViewInternal = "journey.view.internal";
    public const string JourneyPublish = "journey.publish";
    public const string AiUse = "ai.use";

    // ---- Attendance and care ----
    public const string AttendanceView = "attendance.view";
    public const string AttendanceRecord = "attendance.record";
    public const string AttendanceCorrect = "attendance.correct";
    public const string AttendanceApprove = "attendance.approve";
    public const string CareRecord = "care.record";
    public const string CareView = "care.view";
    public const string CarePlanManage = "care.plan.manage";
    /// <summary>Medication / incident records. Never granted by a default template (explicit grant only).</summary>
    public const string CareSensitiveManage = "care.sensitive.manage";

    // ---- Safety and consent ----
    public const string SafetyManage = "safety.manage";
    public const string SafetyMapManage = "safety.map.manage";
    public const string SafetyMapPublish = "safety.map.publish";
    public const string SafetyMapView = "safety.map.view";
    public const string EmergencyContactsManage = "emergency.contacts.manage";
    public const string SafetyReport = "safety.report";
    public const string ConsentManage = "consent.manage";
    public const string ConsentIssue = "consent.issue";
    public const string ConsentReview = "consent.review";
    public const string ConsentView = "consent.view";

    // ---- Events ----
    public const string EventCreate = "event.create";
    public const string EventManage = "event.manage";
    public const string EventApproved = "event.approved";
    public const string EventPublished = "event.published";
    public const string EventCancelled = "event.cancelled";

    // ---- Communication ----
    public const string CommsDraft = "comms.draft";
    public const string CommsSend = "comms.send";
    public const string CommsApprove = "comms.approve";
    public const string CommsView = "comms.view";

    // ---- Insights and export ----
    public const string InsightsOrgView = "insights.org.view";
    public const string InsightsBranchView = "insights.branch.view";
    public const string InsightsClassroomView = "insights.classroom.view";
    public const string InsightsOrgGenerate = "insights.org.generate";
    public const string ExportOrgReports = "export.org";
    public const string ExportChildReports = "export.child";

    public static IReadOnlyList<string> All { get; } = typeof(Permissions)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();

    /// <summary>Permissions that only ever make sense at platform level.</summary>
    public static IReadOnlySet<string> PlatformOnly { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PlatformConfigure, TenantManage, PlatformPlans, PlatformHealth,
        PlatformSecurityEvents, PlatformRolesManage, PlatformBillingManage, PlatformSupportAccess,
        PlatformWarehouseManage, PlatformMarketplaceOperations
    };
}
