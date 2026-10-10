using System.Security.Claims;

namespace EnjoyEveryday.Shared.Tenancy;

public interface IUserContext
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    Guid? OrganizationId { get; }
    bool HasPermission(string permission);
}

public class UserContext : IUserContext
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsAuthenticated => UserId != Guid.Empty;
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    public Guid? OrganizationId { get; set; }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
