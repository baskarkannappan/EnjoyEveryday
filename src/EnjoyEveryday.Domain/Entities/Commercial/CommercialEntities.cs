namespace EnjoyEveryday.Domain.Entities;

public enum AccountStatus
{
    PendingApproval,
    Onboarding,
    Trial,
    Active,
    Suspended,
    Deactivated
}

public enum FinancialStatus
{
    Unbilled,
    InvoiceDue,
    PartiallyPaid,
    PaymentAwaitingVerification,
    Reconciled,
    Overdue,
    Disputed
}

public enum BillingFrequency
{
    Monthly,
    Quarterly,
    Annual
}

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public BillingFrequency Frequency { get; set; } = BillingFrequency.Monthly;
    public int MaxBranches { get; set; } = 1;
    public int MaxClassrooms { get; set; } = 5;
    public int MaxChildren { get; set; } = 50;
    public bool IncludesAiFeatures { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class OrganizationSubscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Onboarding;
    public FinancialStatus FinancialStatus { get; set; } = FinancialStatus.Unbilled;
    public decimal AgreedPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? NextBillingDate { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => TotalAmount - AmountPaid;
    public string Currency { get; set; } = "USD";
    public FinancialStatus Status { get; set; } = FinancialStatus.InvoiceDue;
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? Description { get; set; }
    public string? BillingContactEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Method { get; set; } = "BankTransfer"; // Card, ACH, BankTransfer
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string Status { get; set; } = "Completed";
    public string? TransactionId { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DiscountOffer
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
}

public class PlatformAuditLog
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Actor { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
}
