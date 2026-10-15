using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class PlatformDashboardSummary
{
    public int TotalOrganizations { get; set; }
    public int ActiveOrganizations { get; set; }
    public int PendingApprovals { get; set; }
    public int SuspendedOrganizations { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int AvailablePlansCount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int OverdueInvoicesCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int PaymentsAwaitingVerificationCount { get; set; }
    public decimal CollectionsThisMonth { get; set; }
    public string Currency { get; set; } = "USD";
}

public class PlatformOrganizationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int BranchCount { get; set; }
    public AccountStatus AccountStatus { get; set; }
    public FinancialStatus FinancialStatus { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }
    public DateTimeOffset? NextBillingDate { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string LifecycleStageKey { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
}

public class PlatformHealthStatus
{
    public bool DatabaseHealthy { get; set; } = true;
    public bool AiGatewayHealthy { get; set; } = true;
    public bool ApiHealthy { get; set; } = true;
    public string Environment { get; set; } = "Production";
    public string Uptime { get; set; } = "99.98%";
    public DateTimeOffset LastChecked { get; set; } = DateTimeOffset.UtcNow;
}

public interface IPlatformAdminService
{
    Task<PlatformDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<PlatformOrganizationDto>> GetOrganizationsOverviewAsync(
        string? search = null, 
        string? accountStatus = null, 
        string? financialStatus = null, 
        string? lifecycleStage = null,
        CancellationToken cancellationToken = default);
    Task<PlatformOrganizationDto?> GetOrganizationDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<SubscriptionPlan>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetInvoicesAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetPaymentsAsync(bool? awaitingVerificationOnly = null, CancellationToken cancellationToken = default);
    Task<List<PlatformAuditLog>> GetAuditLogsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<PlatformHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    Task<PlatformOrganizationDto> CreateOrganizationWorkflowAsync(
        string name, 
        string location, 
        Guid planId, 
        string adminEmail, 
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAccountStatusAsync(Guid organizationId, AccountStatus newStatus, string reason, CancellationToken cancellationToken = default);
    Task<bool> VerifyPaymentWorkflowAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Invoice> IssueInvoiceWorkflowAsync(Guid organizationId, decimal amount, string description, CancellationToken cancellationToken = default);
}
