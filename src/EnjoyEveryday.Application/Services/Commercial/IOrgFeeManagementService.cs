using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class UpsertFeePlanRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FeeCategory { get; set; } = "Tuition";
    public string BillingFrequency { get; set; } = FeeBillingFrequencies.Monthly;
    public string? Currency { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AllowPartialPayment { get; set; } = true;
    public decimal? MinimumPartialPayment { get; set; }
    public bool AllowParentCashDeclaration { get; set; }
}

public class UpsertTariffRuleRequest
{
    public Guid? Id { get; set; }
    public Guid FeePlanId { get; set; }
    public string ScopeType { get; set; } = TariffScopeTypes.OrgDefault;
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? ChildId { get; set; }
    public string? LocationTier { get; set; }
    public string? ChildCategory { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class AssignChildFeeRequest
{
    public Guid ChildId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string FeeMethod { get; set; } = FeeMethods.TariffBased;
    public Guid? FeePlanId { get; set; }
    public Guid? TariffRuleId { get; set; }
    public decimal AgreedAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string FeePeriod { get; set; } = FeeBillingFrequencies.Monthly;
    public string ScheduleType { get; set; } = "FullTime";
    public string PaymentArrangement { get; set; } = PaymentArrangements.PartialAllowed;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
    public bool AutoInvoice { get; set; } = true;
    public bool AllowParentCashDeclaration { get; set; }
    public decimal? MinimumPartialPayment { get; set; }
    public string? Remarks { get; set; }
}

public class AdjustChildFlatRateRequest
{
    public Guid ContractId { get; set; }
    public decimal NewAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;
}

public class CreateInstallmentItemRequest
{
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public bool AllowCustomAmount { get; set; } = true;
    public bool AllowMultipleTransactions { get; set; } = true;
    public string? Notes { get; set; }
}

public class GenerateChildTuitionInvoiceRequest
{
    public Guid ChildId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ContractId { get; set; }
    public string FeeMethod { get; set; } = FeeMethods.TariffBased;
    public string? FeePlanName { get; set; }
    public string? BillingPeriod { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRatePercentage { get; set; }
    public DateTimeOffset IssueDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueDate { get; set; } = DateTimeOffset.UtcNow.AddDays(14);
    public bool SaveAsDraft { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public List<CreateInstallmentItemRequest> Installments { get; set; } = new();
}

public class SubmitParentPaymentRequest
{
    public Guid ChildId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = PaymentMethods.QrCode; // QrCode, BankTransfer, Cash
    public string? TransactionId { get; set; }
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Remarks { get; set; }
    public string? ProofStorageKey { get; set; }
}

public class RecordDaycareCashPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ReceiptDate { get; set; } = DateTimeOffset.UtcNow;
    public string? ReceiptNumber { get; set; }
    public string? Remarks { get; set; }
}

public class ApplyInvoiceAdjustmentRequest
{
    public Guid InvoiceId { get; set; }
    public string AdjustmentType { get; set; } = "Waiver"; // Waiver, Credit, Proration, Discount
    public decimal AdjustmentAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}

public class SaveOrgPaymentGatewayRequest
{
    public bool HasRazorpayAccount { get; set; }
    public string? KeyId { get; set; }
    public string? KeySecret { get; set; } // Optional on update if keeping existing stored secret
    public string? WebhookSecret { get; set; }
    public string Environment { get; set; } = "Test"; // Test | Live
    public bool IsActive { get; set; }
}

public class VerifyParentRazorpayPaymentRequest
{
    public Guid ChildId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public interface IOrgFeeManagementService
{
    // Fee Plans & Tariff Rules
    Task<List<OrgFeePlan>> GetFeePlansAsync(CancellationToken ct = default);
    Task<OrgFeePlan> UpsertFeePlanAsync(UpsertFeePlanRequest request, CancellationToken ct = default);
    Task<bool> ToggleFeePlanActiveAsync(Guid planId, bool isActive, CancellationToken ct = default);

    Task<List<OrgTariffRule>> GetTariffRulesAsync(Guid? feePlanId = null, CancellationToken ct = default);
    Task<OrgTariffRule> UpsertTariffRuleAsync(UpsertTariffRuleRequest request, CancellationToken ct = default);
    Task<bool> ToggleTariffRuleActiveAsync(Guid ruleId, bool isActive, CancellationToken ct = default);

    Task<ResolvedTariffResult?> ResolveChildTariffAsync(
        Guid? branchId,
        Guid? classroomId,
        Guid? childId,
        string? locationTier,
        string? billingFrequency,
        DateTime? asOfDate = null,
        CancellationToken ct = default);

    // Child Fee Assignments & Flat-Rate Adjustments
    Task<FamilyBillingContract> AssignChildFeeAsync(AssignChildFeeRequest request, CancellationToken ct = default);
    Task<FamilyBillingContract> AdjustChildFlatRateAsync(AdjustChildFlatRateRequest request, CancellationToken ct = default);
    Task<List<FamilyBillingContract>> GetChildContractsAsync(Guid childId, CancellationToken ct = default);
    Task<List<ChildFeeAdjustment>> GetChildFeeAdjustmentsAsync(Guid childId, CancellationToken ct = default);

    // Tuition Invoices & Installments
    Task<Invoice> GenerateChildTuitionInvoiceAsync(GenerateChildTuitionInvoiceRequest request, CancellationToken ct = default);
    Task<List<Invoice>> GetOrgTuitionInvoicesAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<List<InvoiceInstallment>> GetInvoiceInstallmentsAsync(Guid invoiceId, CancellationToken ct = default);
    Task<bool> PublishTuitionInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<bool> ApplyInvoiceAdjustmentAsync(ApplyInvoiceAdjustmentRequest request, CancellationToken ct = default);
    Task<bool> CancelTuitionInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default);

    // Bank & QR Settings (up to 3 QRs)
    Task<OrgBankAccount?> GetBankAccountAsync(CancellationToken ct = default);
    Task<OrgBankAccount> UpsertBankAccountAsync(OrgBankAccount account, CancellationToken ct = default);
    Task<List<OrgPaymentQrCode>> GetQrCodesAsync(CancellationToken ct = default);
    Task<OrgPaymentQrCode> UpsertQrCodeAsync(OrgPaymentQrCode qrCode, CancellationToken ct = default);
    Task<bool> DeleteQrCodeAsync(int slotIndex, CancellationToken ct = default);

    // Parent Invoices & Submissions (Parent-scoped)
    Task<List<Invoice>> GetParentChildInvoicesAsync(Guid childId, CancellationToken ct = default);
    Task<List<Payment>> GetParentChildInvoicePaymentsAsync(Guid childId, Guid invoiceId, CancellationToken ct = default);
    Task<List<InvoiceInstallment>> GetParentChildInvoiceInstallmentsAsync(Guid childId, Guid invoiceId, CancellationToken ct = default);
    Task<PaymentInstructionsSnapshotDto> GetInvoicePaymentInstructionsAsync(Guid invoiceId, CancellationToken ct = default);
    Task<Payment> SubmitParentPaymentAsync(SubmitParentPaymentRequest request, CancellationToken ct = default);

    // Staff Manual Verification, Rejection & Direct Cash Recording
    Task<List<Payment>> GetPaymentsQueueAsync(Guid? branchId = null, string? status = null, CancellationToken ct = default);
    Task<(bool Success, string Message)> VerifyPaymentAsync(Guid paymentId, string? remarks, CancellationToken ct = default);
    Task<(bool Success, string Message)> RejectPaymentAsync(Guid paymentId, string reason, CancellationToken ct = default);
    Task<Payment> RecordDirectCashPaymentAsync(RecordDaycareCashPaymentRequest request, CancellationToken ct = default);

    // Dashboard & Scope Helpers
    Task<OrgFeeDashboardSummary> GetDashboardSummaryAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<(bool IsOrgAdmin, bool IsBranchAdmin, List<Guid> AllowedBranchIds)> GetCurrentFinancialScopeAsync(CancellationToken ct = default);

    // Organization-Owned Razorpay Gateway Settings & Parent Online Payments
    Task<OrgPaymentGatewayPublicStatusDto> GetPaymentGatewayStatusAsync(CancellationToken ct = default);
    Task<OrgPaymentGatewayPublicStatusDto> SavePaymentGatewaySettingsAsync(SaveOrgPaymentGatewayRequest request, CancellationToken ct = default);
    Task<OrgPaymentGatewayPublicStatusDto> ValidatePaymentGatewayAsync(CancellationToken ct = default);
    Task<OrgPaymentGatewayPublicStatusDto> DisablePaymentGatewayAsync(CancellationToken ct = default);
    Task<OrgPaymentGatewayPublicStatusDto> GetParentInvoiceGatewayAvailabilityAsync(Guid childId, Guid invoiceId, CancellationToken ct = default);
    Task<OrgRazorpayOrderResponse> CreateParentRazorpayOrderAsync(Guid childId, Guid invoiceId, decimal? requestedAmount = null, CancellationToken ct = default);
    Task<RazorpayVerificationResult> VerifyParentRazorpayPaymentAsync(VerifyParentRazorpayPaymentRequest request, CancellationToken ct = default);
    Task<bool> ProcessOrgRazorpayWebhookAsync(Guid organizationId, string rawBody, string signatureHeader, CancellationToken ct = default);

    // Fee Settlement & Payment Status Reporting
    Task<FeeSettlementReportSummary> GetFeeSettlementReportAsync(
        Guid? branchId = null,
        Guid? classroomId = null,
        Guid? childId = null,
        string? billingPeriod = null,
        string? status = null,
        string? search = null,
        CancellationToken ct = default);
}

