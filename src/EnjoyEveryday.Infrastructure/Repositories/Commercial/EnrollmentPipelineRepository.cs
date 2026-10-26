using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;

namespace EnjoyEveryday.Infrastructure.Repositories;

public class EnrollmentPipelineRepository : IEnrollmentPipelineRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EnrollmentPipelineRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    private static readonly string[] CreateApplicationParams =
    [
        "Id", "TenantId", "BranchId", "ChildFirstName", "ChildLastName", "DateOfBirth", "Gender",
        "ParentGuardianName", "ParentEmail", "ParentPhone", "TargetClassroomId", "DesiredStartDate",
        "ScheduleType", "PipelineStage", "TourDate", "TuitionMonthlyRate", "DepositAmount",
        "SiblingDiscountPercent", "RegistrationFee", "InternalNotes"
    ];

    private static readonly string[] CreateContractParams =
    [
        "Id", "TenantId", "BranchId", "ApplicationId", "ChildId", "ParentName", "ParentEmail",
        "ScheduleType", "BillingCycle", "BaseRate", "DiscountPercent", "EffectiveRate",
        "DepositHeld", "StartDate"
    ];

    private static readonly string[] MarkEnrolledParams =
    [
        "TenantId", "ApplicationId", "ChildId", "ClassroomId",
        "BaseRate", "DepositHeld", "DiscountPercent", "EnrollmentLog"
    ];

    public async Task<List<EnrollmentApplicationRecord>> GetApplicationsAsync(Guid tenantId, Guid? branchId, string? stage, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<EnrollmentApplicationRecord>(
            "fn_get_enrollment_applications",
            new { TenantId = tenantId, BranchId = branchId, Stage = stage },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<EnrollmentApplicationRecord?> GetApplicationByIdAsync(Guid tenantId, Guid applicationId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QuerySingleFunctionAsync<EnrollmentApplicationRecord>(
            "fn_get_enrollment_application_by_id",
            new { TenantId = tenantId, ApplicationId = applicationId },
            cancellationToken: ct);
    }

    public async Task<Guid> CreateApplicationAsync(EnrollmentPipelineApplication app, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_enrollment_application",
            app,
            parameterNames: CreateApplicationParams,
            cancellationToken: ct);

        return app.Id;
    }

    public async Task UpdateStageAsync(Guid tenantId, Guid applicationId, string newStage, string? notes, DateTime? tourDate, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_update_enrollment_application_stage",
            new { TenantId = tenantId, ApplicationId = applicationId, NewStage = newStage, Notes = notes, TourDate = tourDate },
            cancellationToken: ct);
    }

    public async Task EnsureChildEnrolledAsync(Guid childId, Guid tenantId, Guid classroomId, string firstName, string lastName, DateTime dateOfBirth, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_ensure_child_enrolled",
            new
            {
                ChildId = childId,
                TenantId = tenantId,
                ClassroomId = classroomId,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth
            },
            cancellationToken: ct);
    }

    public async Task CreateFamilyBillingContractAsync(FamilyBillingContract contract, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_family_billing_contract",
            contract,
            parameterNames: CreateContractParams,
            cancellationToken: ct);
    }

    public async Task CreateInitialTuitionInvoiceAsync(Guid invoiceId, Guid tenantId, Guid organizationId, string invoiceNum, decimal totalAmount, string desc, string notes, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_create_initial_tuition_invoice",
            new
            {
                InvoiceId = invoiceId,
                TenantId = tenantId,
                OrganizationId = organizationId,
                InvoiceNum = invoiceNum,
                TotalAmount = totalAmount,
                Desc = desc,
                Notes = notes
            },
            cancellationToken: ct);
    }

    public async Task MarkApplicationEnrolledAsync(Guid tenantId, Guid applicationId, Guid childId, Guid classroomId, decimal baseRate, decimal depositHeld, decimal discountPercent, string enrollmentLog, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteProcedureAsync(
            "sp_mark_application_enrolled",
            new
            {
                TenantId = tenantId,
                ApplicationId = applicationId,
                ChildId = childId,
                ClassroomId = classroomId,
                BaseRate = baseRate,
                DepositHeld = depositHeld,
                DiscountPercent = discountPercent,
                EnrollmentLog = enrollmentLog
            },
            parameterNames: MarkEnrolledParams,
            cancellationToken: ct);
    }

    public async Task<List<ClassroomVacancyRecord>> GetClassroomVacanciesAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<ClassroomVacancyRecord>(
            "fn_get_classroom_vacancies",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<List<FamilyBillingContract>> GetActiveFamilyContractsAsync(Guid tenantId, Guid? branchId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var results = await conn.QueryFunctionAsync<FamilyBillingContract>(
            "fn_get_active_family_contracts",
            new { TenantId = tenantId, BranchId = branchId },
            cancellationToken: ct);
        return results.ToList();
    }

    public async Task<(Guid? BranchId, Guid? OrganizationId)> GetClassroomScopeAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var res = await conn.QuerySingleFunctionAsync<(Guid? BranchId, Guid? OrganizationId)>(
            "fn_get_classroom_scope",
            new { TenantId = tenantId, ClassroomId = classroomId },
            cancellationToken: ct);

        return res;
    }
}
