using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EnjoyEveryday.Admin.Web.Components.Pages.Billing;

public partial class BillingPage : ComponentBase
{
    [Inject]
    private IBillingService BillingService { get; set; } = default!;

    [Inject]
    private IPlatformAdminService PlatformService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private List<PlatformOrganizationDto> _organizations = new();
    private List<Invoice> _allInvoices = new();
    private OrganizationBillingSettings? _currentBillingSettings;
    private Guid? _selectedOrgId;
    private string _searchQuery = string.Empty;
    private string _activeTab = "All";
    private bool _isLoading = true;
    private string? _statusNotification;

    // Create / Edit modal state
    private bool _showInvoiceModal = false;
    private Guid? _editingInvoiceId;
    private Guid _invoiceOrgId;
    private DateTime _invoiceIssueDate = DateTime.Today;
    private DateTime _invoiceDueDate = DateTime.Today.AddDays(14);
    private string _invoiceCurrency = "INR";
    private string? _invoiceDescription;
    private decimal _invoiceTaxRate = 0;
    private string? _invoicePaymentInstructions;
    private List<CreateInvoiceLineItemRequest> _lineItems = new();

    // Record Payment modal state
    private bool _showPaymentModal = false;
    private Invoice? _selectedInvoiceForAction;
    private decimal _paymentAmount;
    private string _paymentMethod = "BankTransfer";
    private string _paymentReference = string.Empty;
    private DateTime _paymentDate = DateTime.Today;
    private string? _paymentNotes;

    // Cash Payment modal state
    private bool _showCashPaymentModal = false;
    private decimal _cashAmount;
    private DateTime _cashReceiptDate = DateTime.Today;
    private string _cashReceiptNumber = string.Empty;
    private string? _cashNotes;
    private bool _cashConfirmed = false;

    // Reversal modal state
    private bool _showReverseModal = false;
    private Payment? _selectedPaymentForReversal;
    private string _reversalReason = string.Empty;
    private List<Payment> _invoicePayments = new();

    // Void modal state
    private bool _showVoidModal = false;
    private string _voidReason = string.Empty;

    // Settings modal state
    private bool _showSettingsModal = false;
    private BillingMode _targetBillingMode = BillingMode.Manual;
    private BillingFrequency _targetFrequency = BillingFrequency.Monthly;
    private DateTime _targetEffectiveDate = DateTime.Today;
    private string? _billingSettingsReason;

    // Preview modal state
    private bool _showPreviewModal = false;

    // Computed metrics
    private decimal TotalInvoicedAmount => _allInvoices.Where(i => i.Status != InvoiceStatus.Void).Sum(i => i.TotalAmount);
    private decimal TotalOutstandingBalance => _allInvoices.Where(i => i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue);
    private decimal TotalAmountPaid => _allInvoices.Where(i => i.Status != InvoiceStatus.Void).Sum(i => i.AmountPaid);
    private int OverdueCount => _allInvoices.Count(i => i.Status == InvoiceStatus.Overdue);

    private decimal ComputedSubtotal => _lineItems.Sum(li => li.Quantity * li.UnitPrice);
    private decimal ComputedTaxAmount => Math.Round(ComputedSubtotal * (_invoiceTaxRate / 100m), 2);
    private decimal ComputedTotalAmount => ComputedSubtotal + ComputedTaxAmount;
    private bool CanSaveDraft => (_invoiceOrgId != Guid.Empty || _editingInvoiceId.HasValue) && _lineItems.Any() && ComputedTotalAmount > 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        try
        {
            _organizations = await PlatformService.GetOrganizationsOverviewAsync();
            _allInvoices = await BillingService.GetAllInvoicesAsync(organizationId: _selectedOrgId);

            if (_selectedOrgId.HasValue)
            {
                _currentBillingSettings = await BillingService.GetBillingSettingsAsync(_selectedOrgId.Value);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private List<Invoice> FilteredInvoices
    {
        get
        {
            var query = _allInvoices.AsEnumerable();

            if (_selectedOrgId.HasValue && _selectedOrgId.Value != Guid.Empty)
            {
                query = query.Where(i => i.OrganizationId == _selectedOrgId.Value);
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var q = _searchQuery.Trim().ToLowerInvariant();
                query = query.Where(i => i.InvoiceNumber.ToLowerInvariant().Contains(q)
                                      || (i.Description != null && i.Description.ToLowerInvariant().Contains(q))
                                      || GetOrgName(i.OrganizationId).ToLowerInvariant().Contains(q));
            }

            query = _activeTab switch
            {
                "Draft" => query.Where(i => i.Status == InvoiceStatus.Draft),
                "Unpaid" => query.Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue),
                "Paid" => query.Where(i => i.Status == InvoiceStatus.Paid),
                "Void" => query.Where(i => i.Status == InvoiceStatus.Void),
                _ => query
            };

            return query.OrderByDescending(i => i.IssueDate).ToList();
        }
    }

    private string GetOrgName(Guid orgId)
    {
        var org = _organizations.FirstOrDefault(o => o.Id == orgId);
        return org?.Name ?? "Daycare Organization";
    }

    private async Task OnOrganizationChanged(ChangeEventArgs e)
    {
        var val = e.Value?.ToString();
        if (Guid.TryParse(val, out var parsed))
        {
            _selectedOrgId = parsed;
            _currentBillingSettings = await BillingService.GetBillingSettingsAsync(parsed);
        }
        else
        {
            _selectedOrgId = null;
            _currentBillingSettings = null;
        }
    }

    private void OpenCreateInvoiceModal()
    {
        _editingInvoiceId = null;
        _invoiceOrgId = _selectedOrgId ?? (_organizations.FirstOrDefault()?.Id ?? Guid.Empty);
        _invoiceIssueDate = DateTime.Today;
        _invoiceDueDate = DateTime.Today.AddDays(14);
        _invoiceCurrency = "INR";
        _invoiceDescription = "Monthly Subscription Fee";
        _invoiceTaxRate = 0;
        _invoicePaymentInstructions = "Remit payment within 14 days via Bank Transfer or ACH.";
        _lineItems = new List<CreateInvoiceLineItemRequest>
        {
            new CreateInvoiceLineItemRequest
            {
                Description = "EnjoyEveryday Early-Childhood Platform Subscription",
                Quantity = 1,
                UnitPrice = 299.00m,
                Category = "Subscription"
            }
        };
        _showInvoiceModal = true;
    }

    private void OpenEditDraftModal(Invoice inv)
    {
        _editingInvoiceId = inv.Id;
        _invoiceOrgId = inv.OrganizationId;
        _invoiceIssueDate = inv.IssueDate.DateTime;
        _invoiceDueDate = inv.DueDate.DateTime;
        _invoiceCurrency = inv.Currency;
        _invoiceDescription = inv.Description;
        _invoicePaymentInstructions = inv.PaymentInstructions;
        _lineItems = inv.LineItems.Select(li => new CreateInvoiceLineItemRequest
        {
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            Category = li.Category
        }).ToList();

        if (!_lineItems.Any())
        {
            _lineItems.Add(new CreateInvoiceLineItemRequest
            {
                Description = inv.Description ?? "Platform Fee",
                Quantity = 1,
                UnitPrice = inv.Subtotal,
                Category = "Subscription"
            });
        }

        _showInvoiceModal = true;
    }

    private void AddLineItem()
    {
        _lineItems.Add(new CreateInvoiceLineItemRequest
        {
            Description = "Additional Service / Branch Fee",
            Quantity = 1,
            UnitPrice = 50.00m,
            Category = "AdditionalService"
        });
    }

    private void RemoveLineItem(CreateInvoiceLineItemRequest item)
    {
        _lineItems.Remove(item);
    }

    private async Task SaveAsDraft()
    {
        if (!_editingInvoiceId.HasValue && _invoiceOrgId == Guid.Empty)
        {
            _statusNotification = "Please select a target daycare organization.";
            return;
        }

        try
        {
            if (_editingInvoiceId.HasValue)
            {
                await BillingService.UpdateDraftInvoiceAsync(new UpdateDraftInvoiceRequest
                {
                    InvoiceId = _editingInvoiceId.Value,
                    IssueDate = DateTime.SpecifyKind(_invoiceIssueDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(_invoiceDueDate, DateTimeKind.Utc),
                    Description = _invoiceDescription,
                    PaymentInstructions = _invoicePaymentInstructions,
                    TaxRatePercentage = _invoiceTaxRate,
                    LineItems = _lineItems
                });
                _statusNotification = "Draft invoice successfully updated.";
            }
            else
            {
                var inv = await BillingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
                {
                    OrganizationId = _invoiceOrgId,
                    IssueDate = DateTime.SpecifyKind(_invoiceIssueDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(_invoiceDueDate, DateTimeKind.Utc),
                    Currency = _invoiceCurrency,
                    Description = _invoiceDescription,
                    PaymentInstructions = _invoicePaymentInstructions,
                    TaxRatePercentage = _invoiceTaxRate,
                    LineItems = _lineItems
                });
                _statusNotification = $"Draft invoice {inv.InvoiceNumber} created successfully.";
            }

            CloseModals();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _statusNotification = $"Error saving invoice: {ex.Message}";
        }
    }

    private async Task SaveAndPublish()
    {
        if (!_editingInvoiceId.HasValue && _invoiceOrgId == Guid.Empty)
        {
            _statusNotification = "Please select a target daycare organization.";
            return;
        }

        try
        {
            Invoice inv;
            if (_editingInvoiceId.HasValue)
            {
                inv = await BillingService.UpdateDraftInvoiceAsync(new UpdateDraftInvoiceRequest
                {
                    InvoiceId = _editingInvoiceId.Value,
                    IssueDate = DateTime.SpecifyKind(_invoiceIssueDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(_invoiceDueDate, DateTimeKind.Utc),
                    Description = _invoiceDescription,
                    PaymentInstructions = _invoicePaymentInstructions,
                    TaxRatePercentage = _invoiceTaxRate,
                    LineItems = _lineItems
                });
            }
            else
            {
                inv = await BillingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
                {
                    OrganizationId = _invoiceOrgId,
                    IssueDate = DateTime.SpecifyKind(_invoiceIssueDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(_invoiceDueDate, DateTimeKind.Utc),
                    Currency = _invoiceCurrency,
                    Description = _invoiceDescription,
                    PaymentInstructions = _invoicePaymentInstructions,
                    TaxRatePercentage = _invoiceTaxRate,
                    LineItems = _lineItems
                });
            }

            await BillingService.PublishInvoiceAsync(inv.Id);
            _statusNotification = $"Invoice {inv.InvoiceNumber} published and available to the daycare organization.";
            CloseModals();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _statusNotification = $"Error publishing invoice: {ex.Message}";
        }
    }

    private async Task PublishInvoice(Guid invoiceId)
    {
        var inv = await BillingService.PublishInvoiceAsync(invoiceId);
        _statusNotification = $"Invoice {inv.InvoiceNumber} successfully published.";
        await LoadDataAsync();
    }

    private void OpenRecordPaymentModal(Invoice inv)
    {
        _selectedInvoiceForAction = inv;
        _paymentAmount = inv.BalanceDue;
        _paymentMethod = "BankTransfer";
        _paymentReference = $"REF-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";
        _paymentDate = DateTime.Today;
        _paymentNotes = string.Empty;
        _showPaymentModal = true;
    }

    private async Task SubmitPayment()
    {
        if (_selectedInvoiceForAction == null) return;

        var payment = await BillingService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = _selectedInvoiceForAction.Id,
            Amount = _paymentAmount,
            PaymentMethod = _paymentMethod,
            PaymentReference = _paymentReference,
            PaymentDate = DateTime.SpecifyKind(_paymentDate, DateTimeKind.Utc),
            Notes = _paymentNotes
        });

        _statusNotification = $"Payment of {payment.Amount:C2} ({payment.PaymentReference}) recorded successfully.";
        CloseModals();
        await LoadDataAsync();
    }

    private void OpenVoidModal(Invoice inv)
    {
        _selectedInvoiceForAction = inv;
        _voidReason = string.Empty;
        _showVoidModal = true;
    }

    private async Task SubmitVoid()
    {
        if (_selectedInvoiceForAction == null) return;

        await BillingService.VoidInvoiceAsync(_selectedInvoiceForAction.Id, _voidReason);
        _statusNotification = $"Invoice {_selectedInvoiceForAction.InvoiceNumber} has been voided.";
        CloseModals();
        await LoadDataAsync();
    }

    private void OpenBillingSettingsModal()
    {
        if (_currentBillingSettings != null)
        {
            _targetBillingMode = _currentBillingSettings.Mode;
            _targetFrequency = _currentBillingSettings.Frequency;
            _targetEffectiveDate = DateTime.Today;
            _billingSettingsReason = string.Empty;
        }
        _showSettingsModal = true;
    }

    private async Task SubmitBillingSettings()
    {
        if (!_selectedOrgId.HasValue) return;

        _currentBillingSettings = await BillingService.UpdateBillingModeAsync(new UpdateBillingModeRequest
        {
            OrganizationId = _selectedOrgId.Value,
            Mode = _targetBillingMode,
            Frequency = _targetFrequency,
            EffectiveDate = DateTime.SpecifyKind(_targetEffectiveDate, DateTimeKind.Utc),
            Reason = _billingSettingsReason
        });

        _statusNotification = $"Billing mode for {GetOrgName(_selectedOrgId.Value)} set to {_targetBillingMode}.";
        CloseModals();
        await LoadDataAsync();
    }

    private async Task RunScheduledBillingAsync()
    {
        var runResult = await BillingService.ExecuteAutomaticBillingRunAsync(DateTimeOffset.UtcNow);
        _statusNotification = $"Automatic Billing Run Completed: {runResult.GeneratedInvoices} invoice(s) generated for {runResult.ProcessedOrganizations} organization(s).";
        await LoadDataAsync();
    }

    private async Task OpenPreviewModal(Invoice inv)
    {
        _selectedInvoiceForAction = inv;
        _invoicePayments = await BillingService.GetInvoicePaymentsAsync(inv.Id);
        _showPreviewModal = true;
    }

    private void OpenRecordCashModal(Invoice inv)
    {
        _selectedInvoiceForAction = inv;
        _cashAmount = inv.BalanceDue;
        _cashReceiptDate = DateTime.Today;
        _cashReceiptNumber = $"RCPT-CASH-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        _cashNotes = string.Empty;
        _cashConfirmed = false;
        _showCashPaymentModal = true;
    }

    private async Task SubmitCashPayment()
    {
        if (_selectedInvoiceForAction == null || !_cashConfirmed) return;

        try
        {
            var payment = await BillingService.RecordCashPaymentAsync(new RecordCashPaymentRequest
            {
                InvoiceId = _selectedInvoiceForAction.Id,
                Amount = _cashAmount,
                ReceiptNumber = _cashReceiptNumber,
                ReceiptDate = DateTime.SpecifyKind(_cashReceiptDate, DateTimeKind.Utc),
                Notes = _cashNotes
            });

            _statusNotification = $"Cash payment of {payment.Amount:C2} (Receipt #{payment.ReceiptNumber}) recorded successfully.";
            CloseModals();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _statusNotification = $"Error recording cash payment: {ex.Message}";
        }
    }

    private void OpenReverseModal(Payment p)
    {
        _selectedPaymentForReversal = p;
        _reversalReason = string.Empty;
        _showReverseModal = true;
    }

    private async Task SubmitReversal()
    {
        if (_selectedPaymentForReversal == null) return;

        try
        {
            await BillingService.ReversePaymentAsync(new ReversePaymentRequest
            {
                PaymentId = _selectedPaymentForReversal.Id,
                Reason = _reversalReason
            });
            _statusNotification = $"Payment {_selectedPaymentForReversal.PaymentReference} reversed. Balance restored.";
            CloseModals();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _statusNotification = $"Error reversing payment: {ex.Message}";
        }
    }

    private async Task PrintInvoice()
    {
        await JS.InvokeVoidAsync("window.print");
    }

    private void CloseModals()
    {
        _showInvoiceModal = false;
        _showPaymentModal = false;
        _showCashPaymentModal = false;
        _showReverseModal = false;
        _showVoidModal = false;
        _showSettingsModal = false;
        _showPreviewModal = false;
        _selectedInvoiceForAction = null;
        _selectedPaymentForReversal = null;
        _cashConfirmed = false;
    }
}
