using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class PipelineStageSummary
{
    public string Stage { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal ProjectedMonthlyValue { get; set; }
}

public class ClassroomVacancyStats
{
    public Guid ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = "Toddler";
    public int Capacity { get; set; } = 15;
    public int CurrentEnrolled { get; set; }
    public int PendingOffers { get; set; }
    public int AvailableSpots => Math.Max(0, Capacity - CurrentEnrolled - PendingOffers);
    public string StatusBadge => AvailableSpots == 0 ? "Full" : AvailableSpots <= 2 ? "Limited" : "Open";
}

public class EnrollmentPipelineApplicationDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildLastName { get; set; } = string.Empty;
    public string ChildFullName => $"{ChildFirstName} {ChildLastName}".Trim();
    public DateTime DateOfBirth { get; set; }
    public int AgeInMonths => (int)Math.Max(0, (DateTime.UtcNow - DateOfBirth).TotalDays / 30.4375);
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
    public decimal EffectiveMonthlyRate => Math.Round(TuitionMonthlyRate * (1 - (SiblingDiscountPercent / 100m)), 2);
    public string? InternalNotes { get; set; }
    public Guid? ChildId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PipelineOverviewDto
{
    public int TotalInPipeline { get; set; }
    public int EnquiriesCount { get; set; }
    public int ToursScheduledCount { get; set; }
    public int ApplicationsReviewCount { get; set; }
    public int OffersSentCount { get; set; }
    public int EnrolledCount { get; set; }
    public decimal TotalPipelineProjectedValue { get; set; }
    public List<PipelineStageSummary> Stages { get; set; } = new();
    public List<ClassroomVacancyStats> ClassroomVacancies { get; set; } = new();
    public List<EnrollmentPipelineApplicationDto> Applications { get; set; } = new();
}

public class CreateApplicationRequest
{
    public Guid? BranchId { get; set; }
    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildLastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-2);
    public string? Gender { get; set; }
    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;
    public Guid? TargetClassroomId { get; set; }
    public DateTime? DesiredStartDate { get; set; }
    public string ScheduleType { get; set; } = "FullTime";
    public string PipelineStage { get; set; } = "Enquiry";
    public DateTime? TourDate { get; set; }
    public decimal TuitionMonthlyRate { get; set; } = 850m;
    public decimal DepositAmount { get; set; } = 200m;
    public decimal SiblingDiscountPercent { get; set; } = 0m;
    public decimal RegistrationFee { get; set; } = 50m;
    public string? InternalNotes { get; set; }
}

public class FinalizeEnrollmentContractRequest
{
    public Guid ApplicationId { get; set; }
    public Guid ClassroomId { get; set; }
    public string ScheduleType { get; set; } = "FullTime";
    public string BillingCycle { get; set; } = "Monthly";
    public decimal BaseMonthlyRate { get; set; } = 850m;
    public decimal SiblingDiscountPercent { get; set; } = 0m;
    public decimal DepositHeld { get; set; } = 200m;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public bool CreateInitialTuitionInvoice { get; set; } = true;
    public string? ParentNotes { get; set; }
}

public interface IEnrollmentPipelineService
{
    Task<PipelineOverviewDto> GetPipelineOverviewAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<List<EnrollmentPipelineApplicationDto>> GetApplicationsAsync(Guid? branchId = null, string? stage = null, string? search = null, CancellationToken ct = default);
    Task<EnrollmentPipelineApplicationDto?> GetApplicationByIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<Guid> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default);
    Task UpdateStageAsync(Guid applicationId, string newStage, string? notes = null, DateTime? tourDate = null, CancellationToken ct = default);
    Task<Guid> FinalizeEnrollmentAndBillingAsync(FinalizeEnrollmentContractRequest request, CancellationToken ct = default);
    Task<List<ClassroomVacancyStats>> GetClassroomVacanciesAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid? branchId = null, CancellationToken ct = default);
}

public class EnrollmentPipelineService : IEnrollmentPipelineService
{
    private readonly IEnrollmentPipelineRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<EnrollmentPipelineService> _logger;

    public EnrollmentPipelineService(
        IEnrollmentPipelineRepository repository,
        ITenantContext tenantContext,
        IAuditService? auditService = null,
        ILogger<EnrollmentPipelineService>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _auditService = auditService ?? new NullAuditService();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EnrollmentPipelineService>.Instance;
    }

    public async Task<PipelineOverviewDto> GetPipelineOverviewAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var applications = await GetApplicationsAsync(branchId, null, null, ct);
        var vacancies = await GetClassroomVacanciesAsync(branchId, ct);

        var stages = new List<(string Stage, string Name)>
        {
            ("Enquiry", "1. Inquiries & Leads"),
            ("TourScheduled", "2. Tours Scheduled"),
            ("ApplicationSubmitted", "3. Applications In"),
            ("UnderReview", "4. Under Review"),
            ("OfferSent", "5. Offers Extended"),
            ("Enrolled", "6. Enrolled & Active")
        };

        var stageSummaries = stages.Select(s =>
        {
            var match = applications.Where(a => a.PipelineStage == s.Stage).ToList();
            return new PipelineStageSummary
            {
                Stage = s.Stage,
                DisplayName = s.Name,
                Count = match.Count,
                ProjectedMonthlyValue = match.Sum(m => m.EffectiveMonthlyRate)
            };
        }).ToList();

        return new PipelineOverviewDto
        {
            TotalInPipeline = applications.Count(a => a.PipelineStage != "Withdrawn"),
            EnquiriesCount = applications.Count(a => a.PipelineStage == "Enquiry"),
            ToursScheduledCount = applications.Count(a => a.PipelineStage == "TourScheduled"),
            ApplicationsReviewCount = applications.Count(a => a.PipelineStage == "ApplicationSubmitted" || a.PipelineStage == "UnderReview"),
            OffersSentCount = applications.Count(a => a.PipelineStage == "OfferSent"),
            EnrolledCount = applications.Count(a => a.PipelineStage == "Enrolled"),
            TotalPipelineProjectedValue = stageSummaries.Where(s => s.Stage != "Withdrawn").Sum(s => s.ProjectedMonthlyValue),
            Stages = stageSummaries,
            ClassroomVacancies = vacancies,
            Applications = applications
        };
    }

    public async Task<List<EnrollmentPipelineApplicationDto>> GetApplicationsAsync(Guid? branchId = null, string? stage = null, string? search = null, CancellationToken ct = default)
    {
        var records = await _repository.GetApplicationsAsync(_tenantContext.TenantId, branchId, stage, ct);
        var list = records.Select(MapToDto).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            list = list.Where(a => 
                a.ChildFullName.ToLowerInvariant().Contains(s) ||
                a.ParentGuardianName.ToLowerInvariant().Contains(s) ||
                a.ParentEmail.ToLowerInvariant().Contains(s) ||
                (a.TargetClassroomName != null && a.TargetClassroomName.ToLowerInvariant().Contains(s))
            ).ToList();
        }

        return list;
    }

    public async Task<EnrollmentPipelineApplicationDto?> GetApplicationByIdAsync(Guid applicationId, CancellationToken ct = default)
    {
        var record = await _repository.GetApplicationByIdAsync(_tenantContext.TenantId, applicationId, ct);
        return record == null ? null : MapToDto(record);
    }

    public async Task<Guid> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var newId = Guid.NewGuid();

        var app = new EnrollmentPipelineApplication
        {
            Id = newId,
            TenantId = tenantId,
            BranchId = request.BranchId,
            ChildFirstName = request.ChildFirstName,
            ChildLastName = request.ChildLastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            ParentGuardianName = request.ParentGuardianName,
            ParentEmail = request.ParentEmail,
            ParentPhone = request.ParentPhone,
            TargetClassroomId = request.TargetClassroomId,
            DesiredStartDate = request.DesiredStartDate,
            ScheduleType = request.ScheduleType,
            PipelineStage = request.PipelineStage,
            TourDate = request.TourDate,
            TuitionMonthlyRate = request.TuitionMonthlyRate,
            DepositAmount = request.DepositAmount,
            SiblingDiscountPercent = request.SiblingDiscountPercent,
            RegistrationFee = request.RegistrationFee,
            InternalNotes = request.InternalNotes
        };

        await _repository.CreateApplicationAsync(app, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            Action = "EnrollmentInquiryCreated",
            EntityType = "EnrollmentApplication",
            EntityId = newId,
            Details = new { request.ChildFirstName, request.ChildLastName, request.ParentGuardianName }
        }, ct);

        return newId;
    }

    public async Task UpdateStageAsync(Guid applicationId, string newStage, string? notes = null, DateTime? tourDate = null, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _repository.UpdateStageAsync(tenantId, applicationId, newStage, notes, tourDate, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            Action = "EnrollmentStageUpdated",
            EntityType = "EnrollmentApplication",
            EntityId = applicationId,
            Details = new { newStage }
        }, ct);
    }

    public async Task<Guid> FinalizeEnrollmentAndBillingAsync(FinalizeEnrollmentContractRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var app = await GetApplicationByIdAsync(request.ApplicationId, ct);
        if (app == null) throw new InvalidOperationException("Application not found.");

        Guid childId = app.ChildId ?? Guid.NewGuid();

        // 1. Create or ensure child record in children table
        await _repository.EnsureChildEnrolledAsync(childId, tenantId, request.ClassroomId, app.ChildFirstName, app.ChildLastName, app.DateOfBirth, ct);

        // 2. Create family billing contract
        var contractId = Guid.NewGuid();
        var effectiveRate = Math.Round(request.BaseMonthlyRate * (1 - (request.SiblingDiscountPercent / 100m)), 2);

        var contract = new FamilyBillingContract
        {
            Id = contractId,
            TenantId = tenantId,
            BranchId = app.BranchId,
            ApplicationId = app.Id,
            ChildId = childId,
            ParentName = app.ParentGuardianName,
            ParentEmail = app.ParentEmail,
            ScheduleType = request.ScheduleType,
            BillingCycle = request.BillingCycle,
            BaseRate = request.BaseMonthlyRate,
            DiscountPercent = request.SiblingDiscountPercent,
            EffectiveRate = effectiveRate,
            DepositHeld = request.DepositHeld,
            Status = "Active",
            StartDate = request.StartDate,
            AutoInvoice = true
        };

        await _repository.CreateFamilyBillingContractAsync(contract, ct);

        // 3. Optional: Create initial invoice in invoices table for registration + deposit
        if (request.CreateInitialTuitionInvoice)
        {
            var invoiceId = Guid.NewGuid();
            var invoiceNum = $"INV-ENR-{DateTime.UtcNow:yyMM}-{new Random().Next(1000, 9999)}";
            var totalInvoiceAmount = app.RegistrationFee + request.DepositHeld;

            await _repository.CreateInitialTuitionInvoiceAsync(
                invoiceId,
                tenantId,
                invoiceNum,
                totalInvoiceAmount,
                $"Admissions Enrollment Packet for {app.ChildFullName}",
                $"Registration Fee: {app.RegistrationFee:C2}, Held Tuition Deposit: {request.DepositHeld:C2}. Primary Guardian: {app.ParentGuardianName}",
                ct);
        }

        // 4. Mark application as Enrolled
        var enrollmentLog = $"Enrolled on {DateTime.UtcNow:yyyy-MM-dd}. Contract ID {contractId}. Initial invoice created: {request.CreateInitialTuitionInvoice}.";
        await _repository.MarkApplicationEnrolledAsync(
            tenantId,
            request.ApplicationId,
            childId,
            request.ClassroomId,
            request.BaseMonthlyRate,
            request.DepositHeld,
            request.SiblingDiscountPercent,
            enrollmentLog,
            ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            Action = "EnrollmentFinalized",
            EntityType = "Child",
            EntityId = childId,
            Details = new { contractId, app.ChildFullName }
        }, ct);

        return contractId;
    }

    public async Task<List<ClassroomVacancyStats>> GetClassroomVacanciesAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var records = await _repository.GetClassroomVacanciesAsync(_tenantContext.TenantId, branchId, ct);
        return records.Select(r => new ClassroomVacancyStats
        {
            ClassroomId = r.ClassroomId,
            ClassroomName = r.ClassroomName,
            AgeGroup = r.AgeGroup,
            Capacity = r.Capacity,
            CurrentEnrolled = r.CurrentEnrolled,
            PendingOffers = r.PendingOffers
        }).ToList();
    }

    public async Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        return await _repository.GetActiveFamilyContractsAsync(_tenantContext.TenantId, branchId, ct);
    }

    private static EnrollmentPipelineApplicationDto MapToDto(EnrollmentApplicationRecord a) => new()
    {
        Id = a.Id,
        BranchId = a.BranchId,
        BranchName = a.BranchName,
        ChildFirstName = a.ChildFirstName,
        ChildLastName = a.ChildLastName,
        DateOfBirth = a.DateOfBirth,
        Gender = a.Gender,
        ParentGuardianName = a.ParentGuardianName,
        ParentEmail = a.ParentEmail,
        ParentPhone = a.ParentPhone,
        TargetClassroomId = a.TargetClassroomId,
        TargetClassroomName = a.TargetClassroomName,
        DesiredStartDate = a.DesiredStartDate,
        ScheduleType = a.ScheduleType,
        PipelineStage = a.PipelineStage,
        TourDate = a.TourDate,
        TuitionMonthlyRate = a.TuitionMonthlyRate,
        DepositAmount = a.DepositAmount,
        SiblingDiscountPercent = a.SiblingDiscountPercent,
        RegistrationFee = a.RegistrationFee,
        InternalNotes = a.InternalNotes,
        ChildId = a.ChildId,
        CreatedAt = a.CreatedAt
    };
}
