using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public interface IOrganizationHierarchyService
{
    Task<OrganizationHierarchyDto> GetOrganizationHierarchyAsync(
        Guid tenantId,
        Guid organizationId,
        Guid requestingUserId,
        bool isOrgAdmin,
        CancellationToken cancellationToken = default);

    Task<List<StudentNodeDto>> GetClassroomStudentsAsync(
        Guid tenantId,
        Guid classroomId,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationHierarchyDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public List<StaffNodeDto> OrganizationAdmins { get; set; } = new();
    public int TotalBranches { get; set; }
    public int TotalStaff { get; set; }
    public int TotalStudents { get; set; }
    public List<BranchHierarchyNodeDto> Branches { get; set; } = new();
}

public sealed class BranchHierarchyNodeDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public List<StaffNodeDto> BranchAdmins { get; set; } = new();
    public int LeadTeacherCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<ClassroomHierarchyNodeDto> Classrooms { get; set; } = new();
    public List<StaffNodeDto> UnassignedStaff { get; set; } = new();
}

public sealed class ClassroomHierarchyNodeDto
{
    public Guid ClassroomId { get; set; }
    public Guid BranchId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public int Capacity { get; set; }
    public List<StaffNodeDto> LeadTeachers { get; set; } = new();
    public List<StaffNodeDto> Teachers { get; set; } = new();
    public int StudentCount { get; set; }
    public bool IsExpanded { get; set; } = false;
    public bool ChildrenLoaded { get; set; } = false;
    public List<StudentNodeDto> Students { get; set; } = new();
}

public sealed class StaffNodeDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? AssignedClassroomName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Initials => !string.IsNullOrWhiteSpace(Name) ? Name.Trim()[..1].ToUpperInvariant() : (!string.IsNullOrWhiteSpace(Email) ? Email.Trim()[..1].ToUpperInvariant() : "?");
}

public sealed class StudentNodeDto
{
    public Guid ChildId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string? AssignedTeacherName { get; set; }
    public string Initials => !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim()[..1].ToUpperInvariant() : "?";
}

