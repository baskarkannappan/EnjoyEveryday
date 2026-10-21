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
    public string Currency { get; set; } = "INR";
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
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Guid? TariffId { get; set; }
    public string TariffName { get; set; } = string.Empty;
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

public class PlatformBranchDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string EffectiveLocation { get; set; } = string.Empty;
    public int ClassroomsCount { get; set; }
    public int ChildrenCount { get; set; }
}

public class PlatformAdminUserDto
{
    public Guid UserId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string RoleName { get; set; } = "Tenant Admin";
    public string AccountStatus { get; set; } = "Active";
}

public interface IPlatformAdminService
{
    // Overview & Telemetry
    Task<PlatformDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<PlatformOrganizationDto>> GetOrganizationsOverviewAsync(
        string? search = null, 
        string? accountStatus = null, 
        string? financialStatus = null, 
        string? lifecycleStage = null,
        CancellationToken cancellationToken = default);
    Task<PlatformOrganizationDto?> GetOrganizationDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<PlatformHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    Task<List<PlatformAuditLog>> GetAuditLogsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<List<PlatformBranchDto>> GetAllBranchesAsync(CancellationToken cancellationToken = default);
    Task<PlatformBranchDto> CreateBranchAsync(Guid organizationId, string name, string? location, CancellationToken cancellationToken = default);
    Task<List<PlatformAdminUserDto>> GetOrganizationAdminsAsync(CancellationToken cancellationToken = default);

    // Organization CRUD
    Task<PlatformOrganizationDto> CreateOrganizationWorkflowAsync(
        string name, 
        string location, 
        Guid planId, 
        string adminEmail, 
        int branchCount = 1,
        Guid? tariffId = null,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateOrganizationAsync(
        Guid id, 
        string name, 
        string? location, 
        string adminEmail, 
        int branchCount, 
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAccountStatusAsync(
        Guid organizationId, 
        AccountStatus newStatus, 
        string reason, 
        CancellationToken cancellationToken = default);
    Task<bool> DeactivateOrganizationAsync(
        Guid organizationId, 
        string reason, 
        CancellationToken cancellationToken = default);

    // Plan CRUD
    Task<List<SubscriptionPlan>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan> CreatePlanAsync(
        string name, 
        string code, 
        string description, 
        decimal basePrice, 
        int maxBranches, 
        int maxClassrooms, 
        int maxChildren, 
        bool includesAi, 
        CancellationToken cancellationToken = default);
    Task<bool> UpdatePlanAsync(
        Guid planId, 
        string name, 
        string description, 
        decimal basePrice, 
        int maxBranches, 
        int maxClassrooms, 
        int maxChildren, 
        bool includesAi, 
        CancellationToken cancellationToken = default);
    Task<bool> DeactivatePlanAsync(Guid planId, CancellationToken cancellationToken = default);

    // Tariff CRUD
    Task<List<Tariff>> GetTariffsByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<Tariff> CreateTariffAsync(
        Guid planId, 
        string name, 
        BillingFrequency frequency, 
        decimal price, 
        decimal discountPercentage, 
        CancellationToken cancellationToken = default);
    Task<bool> UpdateTariffAsync(
        Guid tariffId, 
        string name, 
        BillingFrequency frequency, 
        decimal price, 
        decimal discountPercentage, 
        CancellationToken cancellationToken = default);
    Task<bool> DeactivateTariffAsync(Guid tariffId, CancellationToken cancellationToken = default);
    Task<bool> AssignPlanAndTariffToOrganizationAsync(
        Guid organizationId, 
        Guid planId, 
        Guid? tariffId, 
        CancellationToken cancellationToken = default);

    // Commercial & Billing Operations
    Task<List<Invoice>> GetInvoicesAsync(string? status = null, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetPaymentsAsync(bool? awaitingVerificationOnly = null, CancellationToken cancellationToken = default);
    Task<bool> VerifyPaymentWorkflowAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Invoice> IssueInvoiceWorkflowAsync(Guid organizationId, decimal amount, string description, CancellationToken cancellationToken = default);

    // AI Parent Communication Gateway
    Task<List<ParentCommunication>> GetRecentParentCommunicationsAsync(int limit = 20, CancellationToken cancellationToken = default);
}
