namespace EnjoyEveryday.Domain.Entities;

public class UserHierarchyRecord
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public string? OrganizationName { get; set; }
    public string? ClassroomName { get; set; }
    public int AssociatedChildrenCount { get; set; }
    public List<string> AssignedRoles { get; set; } = new();
    public List<string> CanonicalRoles { get; set; } = new();
}

public class UserProfileSecurityDetails
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> AssignedRoleNames { get; set; } = new();
    public bool IsClassroomTeacher { get; set; }
    public bool IsParent { get; set; }
}
