using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IOrgFeeRepository
{
    // Fee Plans & Tariff Rules
    Task<List<OrgFeePlan>> GetFeePlansAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<OrgFeePlan> UpsertFeePlanAsync(OrgFeePlan plan, CancellationToken ct = default);
    Task<bool> SetFeePlanActiveAsync(Guid tenantId, Guid organizationId, Guid planId, bool isActive, CancellationToken ct = default);

    Task<List<OrgTariffRule>> GetTariffRulesAsync(Guid tenantId, Guid organizationId, Guid? feePlanId = null, CancellationToken ct = default);
    Task<OrgTariffRule> UpsertTariffRuleAsync(OrgTariffRule rule, CancellationToken ct = default);
    Task<bool> SetTariffRuleActiveAsync(Guid tenantId, Guid organizationId, Guid ruleId, bool isActive, CancellationToken ct = default);

    Task<ResolvedTariffResult?> ResolveChildTariffAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId,
        Guid? classroomId,
        Guid? childId,
        string? locationTier,
        string? billingFrequency,
        DateTime asOfDate,
        CancellationToken ct = default);

    // Flat-rate / Family Contract Extensions & Adjustments
    Task<FamilyBillingContract> UpsertFamilyContractAsync(FamilyBillingContract contract, CancellationToken ct = default);
    Task<FamilyBillingContract?> GetFamilyContractByIdAsync(Guid tenantId, Guid contractId, CancellationToken ct = default);
    Task<List<FamilyBillingContract>> GetFamilyContractsForChildAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<ChildFeeAdjustment> RecordChildFeeAdjustmentAsync(ChildFeeAdjustment adjustment, CancellationToken ct = default);
    Task<List<ChildFeeAdjustment>> GetChildFeeAdjustmentsAsync(Guid tenantId, Guid organizationId, Guid childId, CancellationToken ct = default);

    // Org Tuition Invoices & Installments
    Task<Invoice> CreateTuitionInvoiceAsync(Invoice invoice, List<InvoiceInstallment>? installments = null, CancellationToken ct = default);
    Task<List<Invoice>> GetOrgTuitionInvoicesAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, CancellationToken ct = default);
    Task<List<Invoice>> GetChildInvoicesAsync(Guid tenantId, Guid childId, CancellationToken ct = default);
    Task<List<InvoiceInstallment>> GetInvoiceInstallmentsAsync(Guid invoiceId, CancellationToken ct = default);
    Task<bool> PublishTuitionInvoiceAsync(Guid invoiceId, string publishedBy, string? paymentInstructions, string? paymentInstructionsSnapshotJson, CancellationToken ct = default);
    Task<bool> ApplyInvoiceAdjustmentAsync(Guid invoiceId, decimal adjustmentDelta, string newStatus, CancellationToken ct = default);
    Task<bool> CancelInvoiceAsync(Guid invoiceId, string cancelledBy, string reason, CancellationToken ct = default);

    // Bank Account & QR Codes (up to 3)
    Task<OrgBankAccount?> GetBankAccountAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<OrgBankAccount> UpsertBankAccountAsync(OrgBankAccount account, CancellationToken ct = default);
    Task<List<OrgPaymentQrCode>> GetQrCodesAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<OrgPaymentQrCode> UpsertQrCodeAsync(OrgPaymentQrCode qrCode, CancellationToken ct = default);
    Task<bool> DeleteQrCodeAsync(Guid tenantId, Guid organizationId, int slotIndex, CancellationToken ct = default);

    // Parent Payment Submission & Staff Verification
    Task<Payment> SubmitPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<List<Payment>> GetOrgPaymentsQueueAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, string? status = null, CancellationToken ct = default);
    Task<(bool Success, string Message, Guid? InvoiceId, decimal NewAmountPaid, decimal NewBalanceDue)> VerifySubmittedPaymentAsync(
        Guid paymentId,
        string verifiedBy,
        string? remarks,
        CancellationToken ct = default);
    Task<(bool Success, string Message, Guid? InvoiceId)> RejectSubmittedPaymentAsync(
        Guid paymentId,
        string rejectedBy,
        string reason,
        CancellationToken ct = default);

    // Financial Dashboard
    Task<OrgFeeDashboardSummary> GetDashboardSummaryAsync(Guid tenantId, Guid organizationId, Guid? branchId = null, CancellationToken ct = default);

    // Organization-Owned Razorpay Gateway Settings & Payments
    Task<OrgPaymentGatewaySettings?> GetPaymentGatewaySettingsAsync(Guid tenantId, Guid organizationId, CancellationToken ct = default);
    Task<OrgPaymentGatewaySettings> UpsertPaymentGatewaySettingsAsync(OrgPaymentGatewaySettings settings, CancellationToken ct = default);
    Task<(bool Success, string Message, Guid? PaymentId)> RecordOrgRazorpayPaymentAsync(Payment payment, CancellationToken ct = default);

    // Fee Settlement & Reporting
    Task<List<ChildFeeSettlementReportItem>> GetFeeSettlementReportAsync(
        Guid tenantId,
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        Guid? childId = null,
        string? billingPeriod = null,
        string? status = null,
        string? search = null,
        CancellationToken ct = default);
}

