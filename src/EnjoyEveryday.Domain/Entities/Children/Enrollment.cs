namespace EnjoyEveryday.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ChildId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Current enrollment status.
    /// See <see cref="Constants.EnrollmentStatuses"/> for standard constants.
    /// </summary>
    public string Status { get; set; } = Constants.EnrollmentStatuses.Enrolled;
    public string ScheduleType { get; set; } = "Full Time";
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Withdraw(DateTime endDate)
    {
        Status = Constants.EnrollmentStatuses.Withdrawn;
        EndDate = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Graduate(DateTime endDate)
    {
        Status = Constants.EnrollmentStatuses.Graduated;
        EndDate = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

