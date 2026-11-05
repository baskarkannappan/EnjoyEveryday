using System;

namespace EnjoyEveryday.Domain.Entities.Attendance;

public class BranchLocationContextDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

