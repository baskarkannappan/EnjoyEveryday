using System.Text.Json.Serialization;

namespace EnjoyEveryday.Domain.Entities;

public static class FeeMethods
{
    public const string TariffBased = "TariffBased";
    public const string FlatRate = "FlatRate";
}

public static class TariffScopeTypes
{
    public const string Child = "Child";
    public const string Classroom = "Classroom";
    public const string Branch = "Branch";
    public const string LocationTier = "LocationTier";
    public const string OrgDefault = "OrgDefault";
}

public static class FeeBillingFrequencies
{
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string HalfYearly = "HalfYearly";
    public const string Yearly = "Yearly";
    public const string OneTime = "OneTime";
}

public static class PaymentArrangements
{
    public const string Full = "Full";
    public const string PartialAllowed = "PartialAllowed";
    public const string Installments = "Installments";
}

public class OrgFeePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FeeCategory { get; set; } = "Tuition";
    public string BillingFrequency { get; set; } = FeeBillingFrequencies.Monthly;
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
    public bool AllowPartialPayment { get; set; } = true;
    public decimal? MinimumPartialPayment { get; set; }
    public bool AllowParentCashDeclaration { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrgTariffRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FeePlanId { get; set; }
    public string? PlanName { get; set; }
    public string? BillingFrequency { get; set; }
    public string? FeeCategory { get; set; }
    public string ScopeType { get; set; } = TariffScopeTypes.OrgDefault;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public Guid? ChildId { get; set; }
    public string? ChildName { get; set; }
    public string? LocationTier { get; set; }
    public string? ChildCategory { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ResolvedTariffResult
{
    public Guid RuleId { get; set; }
    public Guid FeePlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string FeeCategory { get; set; } = string.Empty;
    public string BillingFrequency { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public int PrecedenceRank { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public bool AllowPartialPayment { get; set; }
    public decimal? MinimumPartialPayment { get; set; }
    public bool AllowParentCashDeclaration { get; set; }
    public string? LocationTier { get; set; }
    public string? Notes { get; set; }
}

public class ChildFeeAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string AdjustmentType { get; set; } = "FlatRateChange"; // FlatRateChange, Waiver, Credit, Proration, Discount
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public decimal AdjustmentDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
    public Guid? AdjustedByUserId { get; set; }
    public string? AdjustedByEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class InvoiceInstallment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public bool AllowCustomAmount { get; set; } = true;
    public bool AllowMultipleTransactions { get; set; } = true;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrgBankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public string? MicrCode { get; set; }
    public string AccountType { get; set; } = "Current";
    public string? AdditionalInstructions { get; set; }
    public bool AllowParentCashDeclaration { get; set; }
    public bool RequirePaymentProof { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrgPaymentQrCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public int SlotIndex { get; set; } = 1; // 1..3
    public string DisplayName { get; set; } = string.Empty;
    public string? PaymentPurpose { get; set; }
    public string QrImageDataUri { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PaymentInstructionsSnapshotDto
{
    public OrgBankAccount? BankAccount { get; set; }
    public List<OrgPaymentQrCode> QrCodes { get; set; } = new();
    public DateTimeOffset SnapshottedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrgFeeDashboardSummary
{
    public decimal TotalInvoiced { get; set; }
    public decimal TotalVerifiedCollections { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPendingVerification { get; set; }
    public long PartiallyPaidCount { get; set; }
    public long OverdueCount { get; set; }
    public long AwaitingVerificationCount { get; set; }
    public decimal FlatRateInvoiced { get; set; }
    public decimal TariffBasedInvoiced { get; set; }
    public decimal CashCollected { get; set; }
    public decimal QrCollected { get; set; }
    public decimal BankTransferCollected { get; set; }
}

public static class GatewayConfigurationStatuses
{
    public const string NotConfigured = "NotConfigured";
    public const string ConfigurationPending = "ConfigurationPending";
    public const string TestActive = "TestActive";
    public const string LiveActive = "LiveActive";
    public const string ValidationFailed = "ValidationFailed";
    public const string Disabled = "Disabled";
}

public class OrgPaymentGatewaySettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Provider { get; set; } = "Razorpay";
    public bool HasRazorpayAccount { get; set; }
    public string? KeyId { get; set; }
    public string? EncryptedKeySecret { get; set; }
    public string? EncryptedWebhookSecret { get; set; }
    public string Environment { get; set; } = "Test"; // Test | Live
    public string Status { get; set; } = GatewayConfigurationStatuses.NotConfigured;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastValidatedAt { get; set; }
    public string? ValidationMessage { get; set; }
    public string? ConfiguredBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrgPaymentGatewayPublicStatusDto
{
    public bool HasRazorpayAccount { get; set; }
    public string? KeyId { get; set; }
    public bool HasStoredKeySecret { get; set; }
    public bool HasStoredWebhookSecret { get; set; }
    public string Environment { get; set; } = "Test";
    public string Status { get; set; } = GatewayConfigurationStatuses.NotConfigured;
    public bool IsActive { get; set; }
    public bool CanAcceptOnlinePayments => HasRazorpayAccount && IsActive &&
        (Status == GatewayConfigurationStatuses.LiveActive || Status == GatewayConfigurationStatuses.TestActive);
    public DateTimeOffset? LastValidatedAt { get; set; }
    public string? ValidationMessage { get; set; }
    public string? ConfiguredBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class OrgRazorpayOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Environment { get; set; } = "Test";
    public string PrefillEmail { get; set; } = string.Empty;
}

public static class SettlementStatuses
{
    public const string All = "All";
    public const string Paid = "Paid";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Unpaid = "Unpaid";
    public const string Overdue = "Overdue";
    public const string PendingVerification = "PendingVerification";
    public const string NotInvoiced = "NotInvoiced";
}

public class ChildFeeSettlementReportItem
{
    public Guid ChildId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ChildName => $"{FirstName} {LastName}".Trim();
    public string? PhotoUrl { get; set; }
    public Guid? ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public int InvoiceCount { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public decimal PendingVerificationAmount { get; set; }
    public int PendingVerificationCount { get; set; }
    public string SettlementStatus { get; set; } = SettlementStatuses.Unpaid;
    public string InvoicesJson { get; set; } = "[]";
    public string PaymentsJson { get; set; } = "[]";

    public List<FeeReportInvoiceDto> Invoices { get; set; } = new();
    public List<FeeReportPaymentDto> Payments { get; set; } = new();
}

public class FeeReportInvoiceDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("child_id")] public Guid? ChildId { get; set; }
    [JsonPropertyName("invoice_number")] public string InvoiceNumber { get; set; } = string.Empty;
    [JsonPropertyName("billing_period")] public string? BillingPeriod { get; set; }
    [JsonPropertyName("subtotal")] public decimal Subtotal { get; set; }
    [JsonPropertyName("discount_amount")] public decimal DiscountAmount { get; set; }
    [JsonPropertyName("tax_amount")] public decimal TaxAmount { get; set; }
    [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
    [JsonPropertyName("adjustment_amount")] public decimal AdjustmentAmount { get; set; }
    [JsonPropertyName("effective_payable")] public decimal EffectivePayable { get; set; }
    [JsonPropertyName("amount_paid")] public decimal AmountPaid { get; set; }
    [JsonPropertyName("balance_due")] public decimal BalanceDue { get; set; }
    [JsonPropertyName("pending_verification_amount")] public decimal PendingVerificationAmount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "INR";
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("fee_method")] public string? FeeMethod { get; set; }
    [JsonPropertyName("fee_plan_name")] public string? FeePlanName { get; set; }
    [JsonPropertyName("issue_date")] public DateTimeOffset IssueDate { get; set; }
    [JsonPropertyName("due_date")] public DateTimeOffset DueDate { get; set; }
    [JsonPropertyName("paid_at")] public DateTimeOffset? PaidAt { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("installments")] public List<FeeReportInstallmentDto> Installments { get; set; } = new();
}

public class FeeReportInstallmentDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("installment_number")] public int InstallmentNumber { get; set; }
    [JsonPropertyName("due_date")] public string? DueDate { get; set; }
    [JsonPropertyName("amount_due")] public decimal AmountDue { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}

public class FeeReportPaymentDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("child_id")] public Guid? ChildId { get; set; }
    [JsonPropertyName("invoice_id")] public Guid? InvoiceId { get; set; }
    [JsonPropertyName("invoice_number")] public string? InvoiceNumber { get; set; }
    [JsonPropertyName("payment_reference")] public string? PaymentReference { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "INR";
    [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
    [JsonPropertyName("is_verified")] public bool IsVerified { get; set; }
    [JsonPropertyName("verified_by")] public string? VerifiedBy { get; set; }
    [JsonPropertyName("verified_at")] public DateTimeOffset? VerifiedAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("transaction_id")] public string? TransactionId { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
    [JsonPropertyName("submission_source")] public string? SubmissionSource { get; set; }
    [JsonPropertyName("proof_storage_key")] public string? ProofStorageKey { get; set; }
    [JsonPropertyName("payment_date")] public DateTimeOffset PaymentDate { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
}

public class FeeSettlementReportSummary
{
    public decimal TotalInvoiced { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPendingVerification { get; set; }
    public int TotalChildrenCount { get; set; }
    public int FullySettledCount { get; set; }
    public int PartiallySettledCount { get; set; }
    public int UnpaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int PendingVerificationCount { get; set; }
    public int NotInvoicedCount { get; set; }
    public List<ChildFeeSettlementReportItem> Items { get; set; } = new();
}


