using System;

namespace EnjoyEveryday.Domain.Entities;

public class StaffLeave
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid TeacherId { get; set; }
    public string LeaveType { get; set; } = "Vacation"; // Vacation, Sick, Personal, Training, MaternityPaternity
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Approved"; // Pending, Approved, Rejected, Cancelled
    public string CoverageStatus { get; set; } = "Uncovered"; // Covered, Uncovered, AgencyTempNeeded
    public Guid? SubstituteTeacherId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
