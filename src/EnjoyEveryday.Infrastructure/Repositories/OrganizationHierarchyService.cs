using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class OrganizationHierarchyService : IOrganizationHierarchyService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public OrganizationHierarchyService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<OrganizationHierarchyDto> GetOrganizationHierarchyAsync(
        Guid tenantId,
        Guid organizationId,
        Guid requestingUserId,
        bool isOrgAdmin,
        CancellationToken cancellationToken = default)
    {
        var result = new OrganizationHierarchyDto
        {
            OrganizationId = organizationId
        };

        if (organizationId == Guid.Empty)
            return result;

        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch organization details
        var orgRow = await conn.QuerySingleOrDefaultAsync<OrgRow>(
            @"SELECT id, name, location FROM organizations WHERE id = @OrgId",
            new { OrgId = organizationId });

        if (orgRow == null)
            return result;

        result.OrganizationName = orgRow.name;
        result.Location = orgRow.location;

        // 2. Fetch Organization Admins
        var orgAdmins = (await conn.QueryAsync<StaffRow>(
            @"SELECT DISTINCT u.id as user_id, 
                     COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
                     u.email,
                     'Organization Admin' as role_name
              FROM users u
              JOIN user_organization_roles uor ON uor.user_id = u.id AND uor.organization_id = @OrgId
              JOIN roles r ON uor.role_id = r.id
              WHERE r.name IN ('OrganizationAdmin', 'OrganizationOwner', 'SuperAdmin')
              ORDER BY name",
            new { OrgId = organizationId })).ToList();

        result.OrganizationAdmins = orgAdmins.Select(MapStaffRow).ToList();

        // 3. Fetch Authorized Branches
        List<BranchRow> branches;
        if (isOrgAdmin)
        {
            branches = (await conn.QueryAsync<BranchRow>(
                @"SELECT id, name, location 
                  FROM branches 
                  WHERE organization_id = @OrgId AND is_active = true 
                  ORDER BY name",
                new { OrgId = organizationId })).ToList();
        }
        else
        {
            branches = (await conn.QueryAsync<BranchRow>(
                @"SELECT b.id, b.name, b.location 
                  FROM branches b
                  JOIN user_branch_assignments uba ON uba.branch_id = b.id
                  WHERE b.organization_id = @OrgId 
                    AND uba.user_id = @UserId 
                    AND uba.revoked_at IS NULL 
                    AND b.is_active = true 
                  ORDER BY b.name",
                new { OrgId = organizationId, UserId = requestingUserId })).ToList();
        }

        if (branches.Count == 0)
        {
            result.TotalBranches = 0;
            result.TotalStaff = result.OrganizationAdmins.Count;
            result.TotalStudents = 0;
            return result;
        }

        var branchIds = branches.Select(b => b.id).ToArray();

        // 4. Fetch Branch Admins
        var branchAdmins = (await conn.QueryAsync<BranchStaffRow>(
            @"SELECT DISTINCT ON (uba.branch_id, u.id)
                     uba.branch_id, u.id as user_id,
                     COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
                     u.email,
                     'Campus Branch Director' as role_name
              FROM user_branch_assignments uba
              JOIN users u ON uba.user_id = u.id
              JOIN user_organization_roles uor ON uor.user_id = uba.user_id AND uor.organization_id = uba.organization_id
              JOIN roles r ON uor.role_id = r.id AND r.name = 'BranchAdmin'
              WHERE uba.branch_id = ANY(@BranchIds) AND uba.revoked_at IS NULL
              ORDER BY uba.branch_id, u.id, name",
            new { BranchIds = branchIds })).ToList();

        // 5. Fetch Classrooms for authorized branches
        var classrooms = (await conn.QueryAsync<ClassroomRow>(
            @"SELECT id, branch_id, name, code as room_number, COALESCE(capacity, 0) as capacity
              FROM classrooms
              WHERE branch_id = ANY(@BranchIds) AND is_active = true
              ORDER BY name",
            new { BranchIds = branchIds })).ToList();

        var classroomIds = classrooms.Select(c => c.id).ToArray();

        // 6. Fetch Classroom Teachers (Lead vs Educator)
        var teachers = new List<ClassroomTeacherRow>();
        if (classroomIds.Length > 0)
        {
            teachers = (await conn.QueryAsync<ClassroomTeacherRow>(
                @"SELECT ct.classroom_id, u.id as user_id,
                         COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
                         u.email,
                         ct.is_primary,
                         CASE WHEN ct.is_primary THEN 'Lead Teacher' ELSE 'Educator' END as role_name
                  FROM classroom_teachers ct
                  JOIN users u ON ct.user_id = u.id
                  WHERE ct.classroom_id = ANY(@ClassroomIds)
                  ORDER BY ct.is_primary DESC, name",
                new { ClassroomIds = classroomIds })).ToList();
        }

        // 7. Fetch Children counts and initial children per classroom
        var children = new List<ChildRow>();
        if (classroomIds.Length > 0)
        {
            children = (await conn.QueryAsync<ChildRow>(
                @"SELECT ch.id as child_id, ch.classroom_id,
                         TRIM(ch.first_name || ' ' || ch.last_name) as full_name,
                         ch.photo_url,
                         c.name as classroom_name
                  FROM children ch
                  JOIN classrooms c ON ch.classroom_id = c.id
                  WHERE ch.classroom_id = ANY(@ClassroomIds) 
                    AND ch.is_active = true
                    AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
                  ORDER BY ch.first_name, ch.last_name",
                new { ClassroomIds = classroomIds })).ToList();
        }

        // 8. Fetch branch-assigned staff who are not yet assigned to any classroom
        var assignedStaffAcrossBranches = (await conn.QueryAsync<BranchStaffRow>(
            @"SELECT DISTINCT ON (uba.branch_id, u.id)
                     uba.branch_id, u.id as user_id,
                     COALESCE(NULLIF(TRIM(u.display_name), ''), TRIM(u.first_name || ' ' || u.last_name), u.email) as name,
                     u.email,
                     CASE 
                         WHEN r.name = 'LeadTeacher' THEN 'Lead Teacher'
                         WHEN r.name = 'BranchAdmin' THEN 'Campus Branch Director'
                         WHEN r.name = 'ClassroomTeacher' THEN 'Classroom Teacher'
                         WHEN r.name = 'DaycareTeacher' THEN 'Assistant Teacher'
                         ELSE COALESCE(r.name, 'Teacher')
                     END as role_name
              FROM user_branch_assignments uba
              JOIN users u ON uba.user_id = u.id
              LEFT JOIN user_organization_roles uor ON uor.user_id = uba.user_id AND uor.organization_id = uba.organization_id
              LEFT JOIN roles r ON uor.role_id = r.id
              WHERE uba.branch_id = ANY(@BranchIds) AND uba.revoked_at IS NULL
                AND NOT EXISTS (
                    SELECT 1 FROM classroom_teachers ct 
                    JOIN classrooms cl ON ct.classroom_id = cl.id 
                    WHERE ct.user_id = u.id AND cl.branch_id = uba.branch_id
                )
                AND (r.name IS NULL OR r.name NOT IN ('OrganizationAdmin', 'OrganizationOwner', 'BranchAdmin'))
              ORDER BY uba.branch_id, u.id, (CASE WHEN r.name = 'LeadTeacher' THEN 1 WHEN r.name = 'ClassroomTeacher' THEN 2 ELSE 3 END)",
            new { BranchIds = branchIds })).ToList();

        // 9. Assemble Hierarchy
        var branchAdminLookup = branchAdmins.GroupBy(a => a.branch_id).ToDictionary(g => g.Key, g => g.ToList());
        var classroomLookup = classrooms.GroupBy(c => c.branch_id).ToDictionary(g => g.Key, g => g.ToList());
        var teacherLookup = teachers.GroupBy(t => t.classroom_id).ToDictionary(g => g.Key, g => g.ToList());
        var childLookup = children.GroupBy(ch => ch.classroom_id).ToDictionary(g => g.Key, g => g.ToList());
        var unassignedStaffLookup = assignedStaffAcrossBranches.GroupBy(s => s.branch_id).ToDictionary(g => g.Key, g => g.ToList());

        var allUniqueStaffIds = new HashSet<Guid>(result.OrganizationAdmins.Select(a => a.UserId));
        int totalEnrolledStudents = 0;

        foreach (var branch in branches)
        {
            var branchDto = new BranchHierarchyNodeDto
            {
                BranchId = branch.id,
                BranchName = branch.name,
                Location = branch.location
            };

            if (branchAdminLookup.TryGetValue(branch.id, out var bAdmins))
            {
                branchDto.BranchAdmins = bAdmins.Select(a =>
                {
                    allUniqueStaffIds.Add(a.user_id);
                    return new StaffNodeDto
                    {
                        UserId = a.user_id,
                        Name = a.name,
                        Email = a.email,
                        RoleName = "Campus Branch Director"
                    };
                }).ToList();
            }

            int branchLeadCount = 0;
            int branchTeacherCount = 0;
            int branchStudentCount = 0;

            if (classroomLookup.TryGetValue(branch.id, out var branchClassrooms))
            {
                foreach (var cl in branchClassrooms)
                {
                    var clDto = new ClassroomHierarchyNodeDto
                    {
                        ClassroomId = cl.id,
                        BranchId = branch.id,
                        ClassroomName = cl.name,
                        RoomNumber = cl.room_number,
                        Capacity = cl.capacity
                    };

                    if (teacherLookup.TryGetValue(cl.id, out var clTeachers))
                    {
                        foreach (var t in clTeachers)
                        {
                            allUniqueStaffIds.Add(t.user_id);
                            var staffDto = new StaffNodeDto
                            {
                                UserId = t.user_id,
                                Name = t.name,
                                Email = t.email,
                                RoleName = t.role_name,
                                AssignedClassroomName = cl.name
                            };

                            if (t.is_primary)
                            {
                                clDto.LeadTeachers.Add(staffDto);
                                branchLeadCount++;
                            }
                            else
                            {
                                clDto.Teachers.Add(staffDto);
                                branchTeacherCount++;
                            }
                        }
                    }

                    if (childLookup.TryGetValue(cl.id, out var clChildren))
                    {
                        clDto.StudentCount = clChildren.Count;
                        clDto.ChildrenLoaded = true;
                        var primaryTeacher = clDto.LeadTeachers.FirstOrDefault() ?? clDto.Teachers.FirstOrDefault();
                        clDto.Students = clChildren.Select(ch => new StudentNodeDto
                        {
                            ChildId = ch.child_id,
                            FullName = ch.full_name,
                            PhotoUrl = ch.photo_url,
                            ClassroomName = cl.name,
                            AssignedTeacherName = primaryTeacher?.Name
                        }).ToList();

                        branchStudentCount += clChildren.Count;
                        totalEnrolledStudents += clChildren.Count;
                    }

                    branchDto.Classrooms.Add(clDto);
                }
            }

            if (unassignedStaffLookup.TryGetValue(branch.id, out var unassigned))
            {
                var adminUserIds = new HashSet<Guid>(branchDto.BranchAdmins.Select(a => a.UserId));
                branchDto.UnassignedStaff = unassigned
                    .Where(s => !adminUserIds.Contains(s.user_id))
                    .Select(s =>
                    {
                        allUniqueStaffIds.Add(s.user_id);
                        if (s.role_name.Contains("Lead", StringComparison.OrdinalIgnoreCase))
                        {
                            branchLeadCount++;
                        }
                        else if (!s.role_name.Contains("Director", StringComparison.OrdinalIgnoreCase) && !s.role_name.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            branchTeacherCount++;
                        }
                        return new StaffNodeDto
                        {
                            UserId = s.user_id,
                            Name = s.name,
                            Email = s.email,
                            RoleName = s.role_name,
                            AssignedClassroomName = "Unassigned Classroom"
                        };
                    }).ToList();
            }

            branchDto.LeadTeacherCount = branchLeadCount;
            branchDto.TeacherCount = branchTeacherCount;
            branchDto.StudentCount = branchStudentCount;

            result.Branches.Add(branchDto);
        }

        result.TotalBranches = result.Branches.Count;
        result.TotalStaff = allUniqueStaffIds.Count;
        result.TotalStudents = totalEnrolledStudents;

        return result;
    }

    public async Task<List<StudentNodeDto>> GetClassroomStudentsAsync(
        Guid tenantId,
        Guid classroomId,
        CancellationToken cancellationToken = default)
    {
        using var conn = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var children = await conn.QueryAsync<ChildRow>(
            @"SELECT ch.id as child_id, ch.classroom_id,
                     TRIM(ch.first_name || ' ' || ch.last_name) as full_name,
                     ch.photo_url,
                     c.name as classroom_name
              FROM children ch
              JOIN classrooms c ON ch.classroom_id = c.id
              WHERE ch.classroom_id = @ClassroomId 
                AND ch.is_active = true
                AND (ch.enrollment_status IS NULL OR ch.enrollment_status != 'Withdrawn')
              ORDER BY ch.first_name, ch.last_name",
            new { ClassroomId = classroomId });

        return children.Select(ch => new StudentNodeDto
        {
            ChildId = ch.child_id,
            FullName = ch.full_name,
            PhotoUrl = ch.photo_url,
            ClassroomName = ch.classroom_name
        }).ToList();
    }

    private static StaffNodeDto MapStaffRow(StaffRow r) => new()
    {
        UserId = r.user_id,
        Name = r.name,
        Email = r.email,
        RoleName = r.role_name
    };

    private sealed class OrgRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? location { get; set; }
    }

    private sealed class BranchRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? location { get; set; }
    }

    private sealed class ClassroomRow
    {
        public Guid id { get; set; }
        public Guid branch_id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? room_number { get; set; }
        public int capacity { get; set; }
    }

    private sealed class StaffRow
    {
        public Guid user_id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role_name { get; set; } = string.Empty;
    }

    private sealed class BranchStaffRow
    {
        public Guid branch_id { get; set; }
        public Guid user_id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role_name { get; set; } = string.Empty;
    }

    private sealed class ClassroomTeacherRow
    {
        public Guid classroom_id { get; set; }
        public Guid user_id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool is_primary { get; set; }
        public string role_name { get; set; } = string.Empty;
    }

    private sealed class ChildRow
    {
        public Guid child_id { get; set; }
        public Guid classroom_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string? photo_url { get; set; }
        public string classroom_name { get; set; } = string.Empty;
    }
}
