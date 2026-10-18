using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public class EnrollmentApplicationRecord
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildLastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;
    public Guid? TargetClassroomId { get; set; }
    public string? TargetClassroomName { get; set; }
    public DateTime? DesiredStartDate { get; set; }
    public string ScheduleType { get; set; } = "FullTime";
    public string PipelineStage { get; set; } = "Enquiry";
    public DateTime? TourDate { get; set; }
    public decimal TuitionMonthlyRate { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal SiblingDiscountPercent { get; set; }
    public decimal RegistrationFee { get; set; }
    public string? InternalNotes { get; set; }
    public Guid? ChildId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClassroomVacancyRecord
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = "Toddler";
    public int Capacity { get; set; } = 15;
    public int CurrentEnrolled { get; set; }
    public int PendingOffers { get; set; }
}

public interface IEnrollmentPipelineRepository
{
    Task<List<EnrollmentApplicationRecord>> GetApplicationsAsync(Guid tenantId, Guid? branchId, string? stage, CancellationToken ct = default);
    Task<EnrollmentApplicationRecord?> GetApplicationByIdAsync(Guid tenantId, Guid applicationId, CancellationToken ct = default);
    Task<Guid> CreateApplicationAsync(EnrollmentPipelineApplication app, CancellationToken ct = default);
    Task UpdateStageAsync(Guid tenantId, Guid applicationId, string newStage, string? notes, DateTime? tourDate, CancellationToken ct = default);
    Task EnsureChildEnrolledAsync(Guid childId, Guid tenantId, Guid classroomId, string firstName, string lastName, DateTime dateOfBirth, CancellationToken ct = default);
    Task CreateFamilyBillingContractAsync(FamilyBillingContract contract, CancellationToken ct = default);
    Task CreateInitialTuitionInvoiceAsync(Guid invoiceId, Guid tenantId, string invoiceNum, decimal totalAmount, string desc, string notes, CancellationToken ct = default);
    Task MarkApplicationEnrolledAsync(Guid tenantId, Guid applicationId, Guid childId, Guid classroomId, decimal baseRate, decimal depositHeld, decimal discountPercent, string enrollmentLog, CancellationToken ct = default);
    Task<List<ClassroomVacancyRecord>> GetClassroomVacanciesAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default);
    Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default);
}
