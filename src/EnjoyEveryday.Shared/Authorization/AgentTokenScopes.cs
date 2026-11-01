using System.Collections.Generic;

namespace EnjoyEveryday.Shared.Authorization;

/// <summary>
/// Standard scopes for AI Personal Access Tokens (PAT).
/// </summary>
public static class AgentTokenScopes
{
    // Parent scopes
    public const string ParentRead = "parent:read";
    public const string ParentWrite = "parent:write";

    // Teacher scopes
    public const string TeacherRead = "teacher:read";
    public const string TeacherWrite = "teacher:write";

    // Branch Admin scopes
    public const string BranchAdminRead = "branch:read";
    public const string BranchAdminWrite = "branch:write";

    // Organization Admin scopes
    public const string OrgAdminRead = "org:read";
    public const string OrgAdminWrite = "org:write";

    // Platform Admin scope
    public const string PlatformAdmin = "platform:admin";

    // MCP Protocol Access
    public const string McpAccess = "mcp:access";

    public static readonly IReadOnlyList<string> AllScopes = new[]
    {
        ParentRead, ParentWrite,
        TeacherRead, TeacherWrite,
        BranchAdminRead, BranchAdminWrite,
        OrgAdminRead, OrgAdminWrite,
        PlatformAdmin,
        McpAccess
    };

    public static List<string> GetDefaultScopesForRole(string role)
    {
        var scopes = new List<string> { McpAccess };
        switch (role)
        {
            case RoleTemplates.Parent:
                scopes.Add(ParentRead);
                scopes.Add(ParentWrite);
                break;
            case RoleTemplates.ClassroomTeacher:
            case RoleTemplates.DaycareTeacher:
            case RoleTemplates.LeadTeacher:
            case "Teacher":
                scopes.Add(TeacherRead);
                scopes.Add(TeacherWrite);
                break;
            case RoleTemplates.BranchAdmin:
                scopes.Add(TeacherRead);
                scopes.Add(BranchAdminRead);
                scopes.Add(BranchAdminWrite);
                break;
            case RoleTemplates.OrganizationAdmin:
                scopes.Add(TeacherRead);
                scopes.Add(BranchAdminRead);
                scopes.Add(OrgAdminRead);
                scopes.Add(OrgAdminWrite);
                break;
            case "PlatformAdmin":
                scopes.AddRange(AllScopes);
                break;
        }
        return scopes;
    }
}
