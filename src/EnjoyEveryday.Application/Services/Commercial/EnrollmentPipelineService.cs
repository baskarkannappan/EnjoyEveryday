using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Data;
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
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<EnrollmentPipelineService> _logger;

    public EnrollmentPipelineService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        IAuditService? auditService = null,
        ILogger<EnrollmentPipelineService>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var sql = @"
            SELECT 
                a.id,
                a.branch_id AS BranchId,
                b.name AS BranchName,
                a.child_first_name AS ChildFirstName,
                a.child_last_name AS ChildLastName,
                a.date_of_birth AS DateOfBirth,
                a.gender AS Gender,
                a.parent_guardian_name AS ParentGuardianName,
                a.parent_email AS ParentEmail,
                a.parent_phone AS ParentPhone,
                a.target_classroom_id AS TargetClassroomId,
                c.name AS TargetClassroomName,
                a.desired_start_date AS DesiredStartDate,
                a.schedule_type AS ScheduleType,
                a.pipeline_stage AS PipelineStage,
                a.tour_date AS TourDate,
                a.tuition_monthly_rate AS TuitionMonthlyRate,
                a.deposit_amount AS DepositAmount,
                a.sibling_discount_percent AS SiblingDiscountPercent,
                a.registration_fee AS RegistrationFee,
                a.internal_notes AS InternalNotes,
                a.child_id AS ChildId,
                a.created_at AS CreatedAt
            FROM enrollment_pipeline_applications a
            LEFT JOIN branches b ON a.branch_id = b.id
            LEFT JOIN classrooms c ON a.target_classroom_id = c.id
            WHERE a.tenant_id = @tenantId
              AND (@branchId IS NULL OR a.branch_id = @branchId)
              AND (@stage IS NULL OR a.pipeline_stage = @stage)
            ORDER BY a.created_at DESC";

        var list = (await conn.QueryAsync<EnrollmentPipelineApplicationDto>(sql, new { tenantId, branchId, stage })).ToList();

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
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var sql = @"
            SELECT 
                a.id,
                a.branch_id AS BranchId,
                b.name AS BranchName,
                a.child_first_name AS ChildFirstName,
                a.child_last_name AS ChildLastName,
                a.date_of_birth AS DateOfBirth,
                a.gender AS Gender,
                a.parent_guardian_name AS ParentGuardianName,
                a.parent_email AS ParentEmail,
                a.parent_phone AS ParentPhone,
                a.target_classroom_id AS TargetClassroomId,
                c.name AS TargetClassroomName,
                a.desired_start_date AS DesiredStartDate,
                a.schedule_type AS ScheduleType,
                a.pipeline_stage AS PipelineStage,
                a.tour_date AS TourDate,
                a.tuition_monthly_rate AS TuitionMonthlyRate,
                a.deposit_amount AS DepositAmount,
                a.sibling_discount_percent AS SiblingDiscountPercent,
                a.registration_fee AS RegistrationFee,
                a.internal_notes AS InternalNotes,
                a.child_id AS ChildId,
                a.created_at AS CreatedAt
            FROM enrollment_pipeline_applications a
            LEFT JOIN branches b ON a.branch_id = b.id
            LEFT JOIN classrooms c ON a.target_classroom_id = c.id
            WHERE a.tenant_id = @tenantId AND a.id = @applicationId";

        return await conn.QuerySingleOrDefaultAsync<EnrollmentPipelineApplicationDto>(sql, new { tenantId, applicationId });
    }

    public async Task<Guid> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;
        var newId = Guid.NewGuid();

        var sql = @"
            INSERT INTO enrollment_pipeline_applications (
                id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
                parent_guardian_name, parent_email, parent_phone, target_classroom_id, desired_start_date,
                schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
                sibling_discount_percent, registration_fee, internal_notes
            ) VALUES (
                @newId, @tenantId, @BranchId, @ChildFirstName, @ChildLastName, @DateOfBirth, @Gender,
                @ParentGuardianName, @ParentEmail, @ParentPhone, @TargetClassroomId, @DesiredStartDate,
                @ScheduleType, @PipelineStage, @TourDate, @TuitionMonthlyRate, @DepositAmount,
                @SiblingDiscountPercent, @RegistrationFee, @InternalNotes
            )";

        await conn.ExecuteAsync(sql, new
        {
            newId,
            tenantId,
            request.BranchId,
            request.ChildFirstName,
            request.ChildLastName,
            request.DateOfBirth,
            request.Gender,
            request.ParentGuardianName,
            request.ParentEmail,
            request.ParentPhone,
            request.TargetClassroomId,
            request.DesiredStartDate,
            request.ScheduleType,
            request.PipelineStage,
            request.TourDate,
            request.TuitionMonthlyRate,
            request.DepositAmount,
            request.SiblingDiscountPercent,
            request.RegistrationFee,
            request.InternalNotes
        });

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            Action = "EnrollmentInquiryCreated",
            EntityType = "EnrollmentApplication",
            EntityId = newId,
            Details = new { request.ChildFirstName, request.ChildLastName, request.ParentGuardianName }
        }, ct);

        return newId;
    }

    public async Task UpdateStageAsync(Guid applicationId, string newStage, string? notes = null, DateTime? tourDate = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var sql = @"
            UPDATE enrollment_pipeline_applications
            SET pipeline_stage = @newStage,
                tour_date = COALESCE(@tourDate, tour_date),
                internal_notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE internal_notes END,
                updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @applicationId";

        await conn.ExecuteAsync(sql, new { tenantId, applicationId, newStage, tourDate, notes });

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            Action = "EnrollmentStageUpdated",
            EntityType = "EnrollmentApplication",
            EntityId = applicationId,
            Details = new { newStage }
        }, ct);
    }

    public async Task<Guid> FinalizeEnrollmentAndBillingAsync(FinalizeEnrollmentContractRequest request, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var app = await GetApplicationByIdAsync(request.ApplicationId, ct);
        if (app == null) throw new InvalidOperationException("Application not found.");

        Guid childId = app.ChildId ?? Guid.NewGuid();

        // 1. Create or ensure child record in children table
        var ensureChildSql = @"
            INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth, enrollment_status, is_active, created_at, updated_at)
            VALUES (@childId, @tenantId, @ClassroomId, @ChildFirstName, @ChildLastName, @DateOfBirth, 'Enrolled', TRUE, NOW(), NOW())
            ON CONFLICT (id) DO UPDATE SET
                classroom_id = EXCLUDED.classroom_id,
                enrollment_status = 'Enrolled',
                updated_at = NOW()";

        await conn.ExecuteAsync(ensureChildSql, new
        {
            childId,
            tenantId,
            request.ClassroomId,
            app.ChildFirstName,
            app.ChildLastName,
            app.DateOfBirth
        });

        // 2. Create family billing contract
        var contractId = Guid.NewGuid();
        var effectiveRate = Math.Round(request.BaseMonthlyRate * (1 - (request.SiblingDiscountPercent / 100m)), 2);

        var contractSql = @"
            INSERT INTO family_billing_contracts (
                id, tenant_id, branch_id, application_id, child_id, parent_name, parent_email,
                schedule_type, billing_cycle, base_rate, discount_percent, effective_rate,
                deposit_held, status, start_date, auto_invoice
            ) VALUES (
                @contractId, @tenantId, @BranchId, @ApplicationId, @childId, @ParentName, @ParentEmail,
                @ScheduleType, @BillingCycle, @BaseMonthlyRate, @SiblingDiscountPercent, @effectiveRate,
                @DepositHeld, 'Active', @StartDate, TRUE
            )";

        await conn.ExecuteAsync(contractSql, new
        {
            contractId,
            tenantId,
            BranchId = app.BranchId,
            ApplicationId = app.Id,
            childId,
            ParentName = app.ParentGuardianName,
            ParentEmail = app.ParentEmail,
            request.ScheduleType,
            request.BillingCycle,
            request.BaseMonthlyRate,
            request.SiblingDiscountPercent,
            effectiveRate,
            request.DepositHeld,
            request.StartDate
        });

        // 3. Optional: Create initial invoice in invoices table for registration + deposit
        if (request.CreateInitialTuitionInvoice)
        {
            var invoiceId = Guid.NewGuid();
            var invoiceNum = $"INV-ENR-{DateTime.UtcNow:yyMM}-{new Random().Next(1000, 9999)}";
            var totalInvoiceAmount = app.RegistrationFee + request.DepositHeld;

            var insertInvoiceSql = @"
                INSERT INTO invoices (
                    id, tenant_id, invoice_number, currency, issue_date, due_date, status,
                    subtotal, tax_amount, total_amount, amount_paid, description, notes, created_by, created_at, updated_at
                ) VALUES (
                    @invoiceId, @tenantId, @invoiceNum, 'INR', NOW(), NOW() + INTERVAL '7 days', 'Issued',
                    @totalInvoiceAmount, 0, @totalInvoiceAmount, 0,
                    @desc, @notes, 'Enrollment System', NOW(), NOW()
                )";

            await conn.ExecuteAsync(insertInvoiceSql, new
            {
                invoiceId,
                tenantId,
                invoiceNum,
                totalInvoiceAmount,
                desc = $"Admissions Enrollment Packet for {app.ChildFullName}",
                notes = $"Registration Fee: {app.RegistrationFee:C2}, Held Tuition Deposit: {request.DepositHeld:C2}. Primary Guardian: {app.ParentGuardianName}"
            });
        }

        // 4. Mark application as Enrolled
        var updateAppSql = @"
            UPDATE enrollment_pipeline_applications
            SET pipeline_stage = 'Enrolled',
                child_id = @childId,
                target_classroom_id = @ClassroomId,
                tuition_monthly_rate = @BaseMonthlyRate,
                deposit_amount = @DepositHeld,
                sibling_discount_percent = @SiblingDiscountPercent,
                internal_notes = COALESCE(internal_notes || E'\n', '') || @enrollmentLog,
                updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @ApplicationId";

        await conn.ExecuteAsync(updateAppSql, new
        {
            tenantId,
            request.ApplicationId,
            childId,
            request.ClassroomId,
            request.BaseMonthlyRate,
            request.DepositHeld,
            request.SiblingDiscountPercent,
            enrollmentLog = $"Enrolled on {DateTime.UtcNow:yyyy-MM-dd}. Contract ID {contractId}. Initial invoice created: {request.CreateInitialTuitionInvoice}."
        });

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            Action = "EnrollmentFinalized",
            EntityType = "Child",
            EntityId = childId,
            Details = new { contractId, app.ChildFullName }
        }, ct);

        return contractId;
    }

    public async Task<List<ClassroomVacancyStats>> GetClassroomVacanciesAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var sql = @"
            SELECT 
                c.id AS ClassroomId,
                c.name AS ClassroomName,
                COALESCE(c.age_group, c.classroom_type, 'Toddler') AS AgeGroup,
                COALESCE(c.capacity, 15) AS Capacity,
                (SELECT COUNT(*) FROM children ch WHERE ch.classroom_id = c.id AND ch.is_active = TRUE) AS CurrentEnrolled,
                (SELECT COUNT(*) FROM enrollment_pipeline_applications a WHERE a.target_classroom_id = c.id AND a.pipeline_stage IN ('UnderReview', 'OfferSent')) AS PendingOffers
            FROM classrooms c
            WHERE c.tenant_id = @tenantId
              AND (@branchId IS NULL OR c.branch_id = @branchId)
            ORDER BY c.name";

        return (await conn.QueryAsync<ClassroomVacancyStats>(sql, new { tenantId, branchId })).ToList();
    }

    public async Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var tenantId = _tenantContext.TenantId;

        var sql = @"
            SELECT 
                id, tenant_id AS TenantId, branch_id AS BranchId, application_id AS ApplicationId,
                child_id AS ChildId, parent_name AS ParentName, parent_email AS ParentEmail,
                schedule_type AS ScheduleType, billing_cycle AS BillingCycle,
                base_rate AS BaseRate, discount_percent AS DiscountPercent, effective_rate AS EffectiveRate,
                deposit_held AS DepositHeld, status AS Status, start_date AS StartDate, end_date AS EndDate,
                auto_invoice AS AutoInvoice, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM family_billing_contracts
            WHERE tenant_id = @tenantId
              AND (@branchId IS NULL OR branch_id = @branchId)
            ORDER BY created_at DESC";

        return (await conn.QueryAsync<FamilyBillingContract>(sql, new { tenantId, branchId })).ToList();
    }
}
