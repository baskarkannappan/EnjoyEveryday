namespace EnjoyEveryday.Daycare.Web.Services;

public enum BranchRouteCategory
{
    Operational,
    ReadOnly,
    Administrative,
    EmergencySafeguarding,
    PublicAccount
}

public static class BranchRoutePolicy
{
    public static string NormalizePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return "/";
        var path = relativePath.Split('?')[0].Split('#')[0].Trim();
        if (!path.StartsWith('/')) path = "/" + path;
        if (path.Length > 1) path = path.TrimEnd('/');
        return path.ToLowerInvariant();
    }

    public static BranchRouteCategory ClassifyRoute(string? relativePath, string? activeView = null)
    {
        var path = NormalizePath(relativePath);

        // 1. Public / Account routes (always accessible)
        if (path is "/profile" or "/select-branch" or "/select-organization" or "/support"
            or "/version" or "/release-notes"
            or "/about-us" or "/about-and-contact" or "/contact-us"
            or "/login" or "/auth/activate" or "/auth/logout" or "/logout"
            or "/access-denied" or "/account-suspended" or "/not-found" or "/error")
        {
            return BranchRouteCategory.PublicAccount;
        }

        // 2. Emergency / Safeguarding routes (always accessible)
        if (path is "/safety-trust" or "/safety-compliance" or "/policies"
            or "/safety-trust/emergency-contacts" or "/safety-trust/floor-maps"
            || path.StartsWith("/consent/sign/", StringComparison.OrdinalIgnoreCase))
        {
            return BranchRouteCategory.EmergencySafeguarding;
        }

        // 3. Home / Today depends on active view
        if (path is "/" or "/today")
        {
            if (string.Equals(activeView, "BranchAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(activeView, "OrgAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return BranchRouteCategory.Administrative;
            }
            return BranchRouteCategory.Operational;
        }

        // 4. Historical & Read-Only routes
        if (path is "/notifications" or "/announcements" or "/management/announcements"
            or "/audit" or "/rhythm-history"
            or "/attendance/monthly" or "/attendance/monthly-children" or "/attendance/monthly-teachers"
            or "/management/attendance/monthly" or "/management/attendance/monthly-children" or "/management/attendance/monthly-teachers"
            or "/daily-care/plans"
            or "/experiences" or "/memories" or "/insights" or "/intelligence"
            or "/stories" or "/stories-old" or "/children" or "/classrooms"
            || path.StartsWith("/experiences/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/storybook/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/classrooms/view/", StringComparison.OrdinalIgnoreCase))
        {
            return BranchRouteCategory.ReadOnly;
        }

        // 5. Classroom Operational routes (strictly blocked when branch is closed)
        if (path is "/classroom" or "/my-classroom" or "/home-old"
            or "/attendance" or "/management/attendance"
            or "/daily-care" or "/daily-care/supplies"
            or "/today-activities"
            or "/planning" or "/planner" or "/planner-old"
            or "/events" or "/harvest" or "/journey"
            || path.StartsWith("/management/attendance/classrooms/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/events/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/journey/", StringComparison.OrdinalIgnoreCase))
        {
            return BranchRouteCategory.Operational;
        }

        if (path.StartsWith("/management/attendance/branches/", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(activeView, "BranchAdmin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(activeView, "OrgAdmin", StringComparison.OrdinalIgnoreCase)
                ? BranchRouteCategory.Administrative
                : BranchRouteCategory.Operational;
        }

        // 6. Family comms: kept accessible for communication/announcements, operational actions inside are guarded
        if (path is "/classroom-messages" or "/messages")
        {
            return BranchRouteCategory.ReadOnly;
        }

        // 7. Administrative routes
        if (path is "/branch-dashboard" or "/management/branch-dashboard"
            or "/org-dashboard" or "/management/dashboard"
            or "/management" or "/management/branches" or "/management/branches/new"
            or "/organization" or "/organization/branches" or "/organization/branch-info"
            or "/organization/inquiries" or "/organization/profile" or "/organization/profile-customizer"
            or "/organization/settings" or "/organization/storage-metrics" or "/organization/tree"
            or "/management/enrollment" or "/enrollment"
            or "/management/roles" or "/roles"
            or "/management/roster" or "/management/users-by-role" or "/users-by-role"
            or "/management/billing" or "/billing" or "/billing/invoices" or "/billing/reports"
            or "/teachers" or "/teachers/new" or "/children/add" or "/classrooms/new"
            or "/marketplace" or "/materials-marketplace" or "/materials-orders"
            or "/studio" or "/settings" or "/settings/agent-access" or "/settings/ai-agents"
            || path.StartsWith("/management/branches/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/organization/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/teachers/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/children/edit/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/classrooms/edit/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/studio/", StringComparison.OrdinalIgnoreCase))
        {
            return BranchRouteCategory.Administrative;
        }

        // Default safe fallback: ReadOnly
        return BranchRouteCategory.ReadOnly;
    }
}

