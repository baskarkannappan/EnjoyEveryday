namespace EnjoyEveryday.Domain.Entities;

public class EnrollmentPipelineApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildLastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;
    public Guid? TargetClassroomId { get; set; }
    public DateTime? DesiredStartDate { get; set; }
    public string ScheduleType { get; set; } = "FullTime"; // FullTime, PartTime_MWF, PartTime_TTh, HalfDay_Morning
    public string PipelineStage { get; set; } = "Enquiry";  // Enquiry, TourScheduled, ApplicationSubmitted, UnderReview, OfferSent, Enrolled, Waitlisted, Withdrawn
    public DateTime? TourDate { get; set; }
    public decimal TuitionMonthlyRate { get; set; } = 850.00m;
    public decimal DepositAmount { get; set; } = 200.00m;
    public decimal SiblingDiscountPercent { get; set; } = 0m;
    public decimal RegistrationFee { get; set; } = 50.00m;
    public string? InternalNotes { get; set; }
    public Guid? ChildId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed helpers
    public string ChildFullName => $"{ChildFirstName} {ChildLastName}".Trim();
    public int AgeInMonths => (int)Math.Max(0, (DateTime.UtcNow - DateOfBirth).TotalDays / 30.4375);
    public decimal EffectiveMonthlyRate => TuitionMonthlyRate * (1 - (SiblingDiscountPercent / 100m));
}

public class FamilyBillingContract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid ChildId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "FullTime";
    public string BillingCycle { get; set; } = "Monthly";
    public decimal BaseRate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal EffectiveRate { get; set; }
    public decimal DepositHeld { get; set; }
    public string Status { get; set; } = "Active"; // Active, Paused, Terminated
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool AutoInvoice { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
