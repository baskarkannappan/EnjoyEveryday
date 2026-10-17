using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class FinancialOverviewDto
{
    public decimal MoneyReceived { get; set; }
    public decimal Outstanding { get; set; }
    public int NeedsInvestigationCount { get; set; }
    public decimal RefundsPending { get; set; }
    public decimal RefundableDepositsHeld { get; set; }
    public int UnreconciledCount { get; set; }
    public string Currency { get; set; } = "INR";
}

public class AccountBookTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string OrganizationName { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string? BranchName { get; set; }
    public string Category { get; set; } = "PlatformFee"; // PlatformFee, Materials, RentalDeposit, Refund, RentalFee, DeliveryCharge, Adjustment
    public string CategoryLabel { get; set; } = "Platform fee";
    public string Description { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentMethod { get; set; } = "Razorpay";
    public string FinancialStatus { get; set; } = "Paid"; // Paid, PaymentPending, Held, Failed, Overdue, Refunded, Disputed
    public string FinancialStatusLabel { get; set; } = "Paid";
    public string ReconciliationStatus { get; set; } = "Reconciled"; // Reconciled, Unreconciled, Discrepancy
    public bool HasException { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionReason { get; set; }
    public string? ProviderReference { get; set; }
    public string? ResponsibleActor { get; set; }
}

public class InvestigationChecklistRule
{
    public string Title { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = true;
    public string Status { get; set; } = "Verified";
    public string? Detail { get; set; }
}

public class TransactionInvestigationDto
{
    public AccountBookTransactionDto Transaction { get; set; } = new();
    public string Subtitle { get; set; } = "Example: a daycare says it paid, but the invoice remains unpaid.";
    public string ReviewBadge { get; set; } = "Needs review"; // Needs review, Verified, Reconciled, Action required
    public List<InvestigationChecklistRule> Checklist { get; set; } = new();
    public decimal ExpectedAmount { get; set; }
    public decimal ActualReceivedAmount { get; set; }
    public decimal DiscrepancyAmount => ExpectedAmount - ActualReceivedAmount;
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? GatewaySignature { get; set; }
    public bool GatewaySignatureVerified { get; set; }
    public string? CashReceiptNumber { get; set; }
    public string? RecordedByEmail { get; set; }
    public List<string> AuditTimeline { get; set; } = new();
    public List<string> InvestigationNotes { get; set; } = new();
    public bool CanVerify { get; set; }
    public bool CanRecordCashReceipt { get; set; }
    public bool CanReleaseDeposit { get; set; }
    public bool CanProcessRefund { get; set; }
}

public class RecordManualCashReceiptCommand
{
    public string TransactionReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public interface IAccountBookService
{
    Task<FinancialOverviewDto> GetFinancialOverviewAsync(CancellationToken cancellationToken = default);
    Task<List<AccountBookTransactionDto>> GetUnifiedLedgerAsync(
        string? activeTab = "All", // "All", "NeedsAttention", "Unreconciled"
        string? categoryFilter = "All",
        string? statusFilter = "All",
        string? searchQuery = null,
        CancellationToken cancellationToken = default);
    Task<TransactionInvestigationDto?> GetInvestigationDetailsAsync(string reference, CancellationToken cancellationToken = default);
    Task<bool> AddInvestigationNoteAsync(string reference, string note, CancellationToken cancellationToken = default);
    Task<bool> VerifyAndReconcileTransactionAsync(string reference, string notes, CancellationToken cancellationToken = default);
    Task<bool> ReleaseDepositAsync(string reference, decimal releaseAmount, string reason, CancellationToken cancellationToken = default);
    Task<bool> ProcessRefundAsync(string reference, decimal refundAmount, string reason, CancellationToken cancellationToken = default);
    Task<bool> RecordCashReceiptAsync(RecordManualCashReceiptCommand command, CancellationToken cancellationToken = default);
}
