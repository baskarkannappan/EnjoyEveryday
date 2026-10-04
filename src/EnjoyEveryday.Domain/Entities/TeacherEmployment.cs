namespace EnjoyEveryday.Domain.Entities;
public class TeacherEmployment
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? Department { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PrimaryClassroomId { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public string? WorkLocation { get; set; }
    public string? PayType { get; set; }
    public string? Notes { get; set; }
}
