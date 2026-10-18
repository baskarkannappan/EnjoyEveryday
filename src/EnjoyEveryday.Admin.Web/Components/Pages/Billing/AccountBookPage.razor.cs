using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Admin.Web.Components.Pages.Billing;

public partial class AccountBookPage : ComponentBase
{
    [Inject]
    private IAccountBookService AccountBookService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private FinancialOverviewDto? _overview;
    private List<AccountBookTransactionDto> _transactions = new();
    private bool _isLoading = true;

    private string _activeTab = "All"; // "All", "NeedsAttention", "Unreconciled"
    private string _categoryFilter = "All";
    private string _statusFilter = "All";
    private string _searchQuery = "";

    // Investigation drawer state
    private TransactionInvestigationDto? _investigationItem;
    private string _newInvestigationNote = "";

    // Record cash payment modal
    private bool _isRecordCashModalOpen = false;
    private string _cashRef = "";
    private decimal _cashAmount = 8600m;
    private string _cashReceiptNumber = "RCPT-202610-842";
    private string _cashNotes = "Cash paid at central office counter";

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        try
        {
            _overview = await AccountBookService.GetFinancialOverviewAsync();
            await ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading account book: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SetTab(string tab)
    {
        _activeTab = tab;
        await ApplyFilters();
    }

    private async Task ApplyFilters()
    {
        _transactions = await AccountBookService.GetUnifiedLedgerAsync(
            activeTab: _activeTab,
            categoryFilter: _categoryFilter,
            statusFilter: _statusFilter,
            searchQuery: _searchQuery);
    }

    private async Task ClearSearch()
    {
        _searchQuery = "";
        await ApplyFilters();
    }

    private async Task ResetFilters()
    {
        _activeTab = "All";
        _categoryFilter = "All";
        _statusFilter = "All";
        _searchQuery = "";
        await ApplyFilters();
    }

    private async Task OpenInvestigation(string reference)
    {
        _newInvestigationNote = "";
        _investigationItem = await AccountBookService.GetInvestigationDetailsAsync(reference);
    }

    private void CloseInvestigation()
    {
        _investigationItem = null;
    }

    private async Task SubmitInvestigationNote()
    {
        if (_investigationItem == null || string.IsNullOrWhiteSpace(_newInvestigationNote)) return;

        await AccountBookService.AddInvestigationNoteAsync(_investigationItem.Transaction.Reference, _newInvestigationNote);
        _investigationItem = await AccountBookService.GetInvestigationDetailsAsync(_investigationItem.Transaction.Reference);
        _newInvestigationNote = "";
    }

    private async Task VerifyCurrentTransaction()
    {
        if (_investigationItem == null) return;

        await AccountBookService.VerifyAndReconcileTransactionAsync(_investigationItem.Transaction.Reference, "Manually verified by Platform Admin");
        _investigationItem = await AccountBookService.GetInvestigationDetailsAsync(_investigationItem.Transaction.Reference);
        await LoadDataAsync();
    }

    private async Task ReleaseCurrentDeposit()
    {
        if (_investigationItem == null) return;

        await AccountBookService.ReleaseDepositAsync(_investigationItem.Transaction.Reference, _investigationItem.ExpectedAmount, "Kit returned in good condition. Security deposit refunded in full.");
        _investigationItem = await AccountBookService.GetInvestigationDetailsAsync(_investigationItem.Transaction.Reference);
        await LoadDataAsync();
    }

    private async Task ReprocessCurrentRefund()
    {
        if (_investigationItem == null) return;

        await AccountBookService.ProcessRefundAsync(_investigationItem.Transaction.Reference, _investigationItem.ExpectedAmount, "Refund reprocessed to verified destination bank account.");
        _investigationItem = await AccountBookService.GetInvestigationDetailsAsync(_investigationItem.Transaction.Reference);
        await LoadDataAsync();
    }

    private void OpenRecordCashModal()
    {
        _cashRef = "MAT-00821";
        _cashAmount = 8600m;
        _cashReceiptNumber = $"RCPT-{DateTimeOffset.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
        _cashNotes = "Cash received at administrative office";
        _isRecordCashModalOpen = true;
    }

    private async Task SubmitCashPayment()
    {
        await AccountBookService.RecordCashReceiptAsync(new RecordManualCashReceiptCommand
        {
            TransactionReference = _cashRef,
            Amount = _cashAmount,
            ReceiptNumber = _cashReceiptNumber,
            Notes = _cashNotes
        });

        _isRecordCashModalOpen = false;
        await LoadDataAsync();
        await OpenInvestigation(_cashRef);
    }

    private string GetFormattedAmount(AccountBookTransactionDto tx)
    {
        var prefix = tx.GrossAmount >= 0 ? "+" : "-";
        return $"{prefix}₹{Math.Abs(tx.GrossAmount):N0}";
    }

    private string GetAmountClass(AccountBookTransactionDto tx)
    {
        if (tx.Category == "Refund" || tx.GrossAmount < 0) return "neg";
        if (tx.FinancialStatus == "Paid") return "pos";
        if (tx.FinancialStatus == "PaymentPending") return "pending";
        if (tx.FinancialStatus == "Held") return "deposit";
        return "pos";
    }

    private string GetStatusPillClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "paid" => "paid",
            "completed" => "paid",
            "reconciled" => "paid",
            "paymentpending" => "pending",
            "pending" => "pending",
            "held" => "held",
            "failed" => "failed",
            "overdue" => "overdue",
            "refunded" => "refunded",
            _ => "pending"
        };
    }

    private string GetReviewBadgeClass(string badge)
    {
        return badge.ToLowerInvariant() switch
        {
            "needs review" => "needs-review",
            "verified" => "verified",
            "failed refund" => "failed",
            "deposit held" => "held",
            _ => "needs-review"
        };
    }
}
