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

public enum InvoiceStatus
{
    Draft,
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue,
    Void
}

public enum BillingFrequency
{
    Monthly,
    Quarterly,
    Annual
}

public enum BillingMode
{
    Manual,
    Automatic
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
    public List<Tariff> Tariffs { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Tariff
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BillingFrequency Frequency { get; set; } = BillingFrequency.Monthly;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal DiscountPercentage { get; set; }
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiryDate { get; set; }
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

public class InvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
    public string? Category { get; set; } // Subscription, BranchFee, AdditionalService, Adjustment
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? BillingPeriod { get; set; } // e.g. "2026-10" for duplicate prevention in scheduled billing
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => Math.Max(0, TotalAmount - AmountPaid);
    public string Currency { get; set; } = "USD";

    private InvoiceStatus _status = InvoiceStatus.Draft;
    public InvoiceStatus Status
    {
        get
        {
            // Derived Overdue status: if due date passed, has balance due, and not draft/void/paid
            if (_status != InvoiceStatus.Draft &&
                _status != InvoiceStatus.Void &&
                _status != InvoiceStatus.Paid &&
                BalanceDue > 0 &&
                DueDate < DateTimeOffset.UtcNow)
            {
                return InvoiceStatus.Overdue;
            }
            return _status;
        }
        set => _status = value;
    }

    public FinancialStatus FinancialStatus => Status switch
    {
        InvoiceStatus.Draft => FinancialStatus.Unbilled,
        InvoiceStatus.Unpaid => FinancialStatus.InvoiceDue,
        InvoiceStatus.PartiallyPaid => FinancialStatus.PartiallyPaid,
        InvoiceStatus.Paid => FinancialStatus.Reconciled,
        InvoiceStatus.Overdue => FinancialStatus.Overdue,
        InvoiceStatus.Void => FinancialStatus.Disputed,
        _ => FinancialStatus.InvoiceDue
    };

    public DateTimeOffset IssueDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueDate { get; set; } = DateTimeOffset.UtcNow.AddDays(14);
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public string? CreatedBy { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? PaymentInstructions { get; set; }
    public string? BillingContactEmail { get; set; }
    public string? DocumentStorageKey { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class PaymentMethods
{
    public const string Razorpay = "Razorpay";
    public const string Cash = "Cash";
    public const string BankTransfer = "BankTransfer";
    public const string Check = "Check";
}

public static class PaymentStatus
{
    public const string Completed = "Completed";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Reversed = "Reversed";
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = PaymentMethods.BankTransfer; // Razorpay, Cash, BankTransfer, Check
    public string PaymentMethod { get => Method; set => Method = value; }
    public bool IsVerified { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string Status { get; set; } = PaymentStatus.Completed; // Completed, Pending, Failed, Reversed
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }

    // Razorpay Integration references
    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderSignature { get; set; }

    // In-Person Cash / Manual Receipt tracking
    public string? ReceiptNumber { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string? RecordedByEmail { get; set; }

    // Audited Financial Reversals & Adjustments
    public bool IsReversed { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public string? ReversedBy { get; set; }
    public string? ReversalReason { get; set; }
    public string? ReversalReference { get; set; }

    public string? AuditMetadata { get; set; }

    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrganizationBillingSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public BillingMode Mode { get; set; } = BillingMode.Manual;
    public BillingFrequency Frequency { get; set; } = BillingFrequency.Monthly;
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextBillingDate { get; set; }
    public bool AutoChargeEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
