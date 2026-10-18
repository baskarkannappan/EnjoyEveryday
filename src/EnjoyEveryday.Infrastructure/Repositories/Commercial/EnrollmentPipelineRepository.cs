using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EnrollmentPipelineRepository : IEnrollmentPipelineRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EnrollmentPipelineRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<List<EnrollmentApplicationRecord>> GetApplicationsAsync(Guid tenantId, Guid? branchId, string? stage, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
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

        var results = await conn.QueryAsync<EnrollmentApplicationRecord>(new CommandDefinition(sql, new { tenantId, branchId, stage }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<EnrollmentApplicationRecord?> GetApplicationByIdAsync(Guid tenantId, Guid applicationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
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

        return await conn.QuerySingleOrDefaultAsync<EnrollmentApplicationRecord>(new CommandDefinition(sql, new { tenantId, applicationId }, cancellationToken: ct));
    }

    public async Task<Guid> CreateApplicationAsync(EnrollmentPipelineApplication app, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO enrollment_pipeline_applications (
                id, tenant_id, branch_id, child_first_name, child_last_name, date_of_birth, gender,
                parent_guardian_name, parent_email, parent_phone, target_classroom_id, desired_start_date,
                schedule_type, pipeline_stage, tour_date, tuition_monthly_rate, deposit_amount,
                sibling_discount_percent, registration_fee, internal_notes
            ) VALUES (
                @Id, @TenantId, @BranchId, @ChildFirstName, @ChildLastName, @DateOfBirth, @Gender,
                @ParentGuardianName, @ParentEmail, @ParentPhone, @TargetClassroomId, @DesiredStartDate,
                @ScheduleType, @PipelineStage, @TourDate, @TuitionMonthlyRate, @DepositAmount,
                @SiblingDiscountPercent, @RegistrationFee, @InternalNotes
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            app.Id,
            app.TenantId,
            app.BranchId,
            app.ChildFirstName,
            app.ChildLastName,
            app.DateOfBirth,
            app.Gender,
            app.ParentGuardianName,
            app.ParentEmail,
            app.ParentPhone,
            app.TargetClassroomId,
            app.DesiredStartDate,
            app.ScheduleType,
            app.PipelineStage,
            app.TourDate,
            app.TuitionMonthlyRate,
            app.DepositAmount,
            app.SiblingDiscountPercent,
            app.RegistrationFee,
            app.InternalNotes
        }, cancellationToken: ct));

        return app.Id;
    }

    public async Task UpdateStageAsync(Guid tenantId, Guid applicationId, string newStage, string? notes, DateTime? tourDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE enrollment_pipeline_applications
            SET pipeline_stage = @newStage,
                tour_date = COALESCE(@tourDate, tour_date),
                internal_notes = CASE WHEN @notes IS NOT NULL THEN @notes ELSE internal_notes END,
                updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @applicationId";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { tenantId, applicationId, newStage, tourDate, notes }, cancellationToken: ct));
    }

    public async Task EnsureChildEnrolledAsync(Guid childId, Guid tenantId, Guid classroomId, string firstName, string lastName, DateTime dateOfBirth, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO children (id, tenant_id, classroom_id, first_name, last_name, date_of_birth, enrollment_status, is_active, created_at, updated_at)
            VALUES (@childId, @tenantId, @classroomId, @firstName, @lastName, @dateOfBirth, 'Enrolled', TRUE, NOW(), NOW())
            ON CONFLICT (id) DO UPDATE SET
                classroom_id = EXCLUDED.classroom_id,
                enrollment_status = 'Enrolled',
                updated_at = NOW()";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            childId,
            tenantId,
            classroomId,
            firstName,
            lastName,
            dateOfBirth
        }, cancellationToken: ct));
    }

    public async Task CreateFamilyBillingContractAsync(FamilyBillingContract contract, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO family_billing_contracts (
                id, tenant_id, branch_id, application_id, child_id, parent_name, parent_email,
                schedule_type, billing_cycle, base_rate, discount_percent, effective_rate,
                deposit_held, status, start_date, auto_invoice
            ) VALUES (
                @Id, @TenantId, @BranchId, @ApplicationId, @ChildId, @ParentName, @ParentEmail,
                @ScheduleType, @BillingCycle, @BaseRate, @DiscountPercent, @EffectiveRate,
                @DepositHeld, 'Active', @StartDate, TRUE
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            contract.Id,
            contract.TenantId,
            contract.BranchId,
            contract.ApplicationId,
            contract.ChildId,
            contract.ParentName,
            contract.ParentEmail,
            contract.ScheduleType,
            contract.BillingCycle,
            contract.BaseRate,
            contract.DiscountPercent,
            contract.EffectiveRate,
            contract.DepositHeld,
            contract.StartDate
        }, cancellationToken: ct));
    }

    public async Task CreateInitialTuitionInvoiceAsync(Guid invoiceId, Guid tenantId, string invoiceNum, decimal totalAmount, string desc, string notes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            INSERT INTO invoices (
                id, tenant_id, invoice_number, currency, issue_date, due_date, status,
                subtotal, tax_amount, total_amount, amount_paid, description, notes, created_by, created_at, updated_at
            ) VALUES (
                @invoiceId, @tenantId, @invoiceNum, 'INR', NOW(), NOW() + INTERVAL '7 days', 'Issued',
                @totalAmount, 0, @totalAmount, 0,
                @desc, @notes, 'Enrollment System', NOW(), NOW()
            )";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            invoiceId,
            tenantId,
            invoiceNum,
            totalAmount,
            desc,
            notes
        }, cancellationToken: ct));
    }

    public async Task MarkApplicationEnrolledAsync(Guid tenantId, Guid applicationId, Guid childId, Guid classroomId, decimal baseRate, decimal depositHeld, decimal discountPercent, string enrollmentLog, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            UPDATE enrollment_pipeline_applications
            SET pipeline_stage = 'Enrolled',
                child_id = @childId,
                target_classroom_id = @classroomId,
                tuition_monthly_rate = @baseRate,
                deposit_amount = @depositHeld,
                sibling_discount_percent = @discountPercent,
                internal_notes = COALESCE(internal_notes || E'\n', '') || @enrollmentLog,
                updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @applicationId";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            tenantId,
            applicationId,
            childId,
            classroomId,
            baseRate,
            depositHeld,
            discountPercent,
            enrollmentLog
        }, cancellationToken: ct));
    }

    public async Task<List<ClassroomVacancyRecord>> GetClassroomVacanciesAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
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

        var results = await conn.QueryAsync<ClassroomVacancyRecord>(new CommandDefinition(sql, new { tenantId, branchId }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
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

        var results = await conn.QueryAsync<FamilyBillingContract>(new CommandDefinition(sql, new { tenantId, branchId }, cancellationToken: ct));
        return results.ToList();
    }
}
