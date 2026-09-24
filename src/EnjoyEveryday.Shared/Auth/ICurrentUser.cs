namespace EnjoyEveryday.Shared.Auth;

/// <summary>
/// Represents the current authenticated user.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string DisplayName { get; }
    Guid TenantId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
