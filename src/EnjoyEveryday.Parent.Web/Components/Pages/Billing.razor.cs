using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.UI.Shared.Components;
using EnjoyEveryday.UI.Shared.Services;
using EnjoyEveryday.Domain.Repositories;
using Microsoft.AspNetCore.Components.Forms;

namespace EnjoyEveryday.Parent.Web.Components.Pages;

public partial class Billing
{
    private bool _isLoading = true;
    private bool _isBusy;
    private string? _statusMessage;
    private bool _isSuccess = true;
    private List<ParentAuthorizedChild> _children = new();
    private Guid _selectedChildId;
    private string _currency = "INR";
    private List<Invoice> _invoices = new();
    private DateTimeOffset? _nextDueDate;

    private Dictionary<Guid, List<InvoiceInstallment>> _invoiceInstallments = new();
    private Invoice? _selectedInvoice;
    private List<InvoiceInstallment> _selectedInstallments = new();
    private List<InstallmentDisplayItem> _installmentItems = new();
    private Guid? _selectedInstallmentId;
    private string _paymentModeSelection = "Installment"; // "Installment" | "Full" | "Custom"

    private List<Payment> _selectedPayments = new();
    private OrgPaymentGatewayPublicStatusDto _gatewayStatus = new();
    private PaymentInstructionsSnapshotDto? _snapshot;
    private decimal _razorpayAmount;
    private SubmitParentPaymentRequest _manualSubmit = new();
    private DotNetObjectReference<Billing>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        _children = (await ParentService.GetAuthorizedChildrenAsync()).ToList();
        var current = SessionState.SelectedChild ?? _children.FirstOrDefault();
        if (current != null)
        {
            _selectedChildId = current.ChildId;
            _currency = string.IsNullOrWhiteSpace(current.Currency) ? "INR" : current.Currency;
        }
        await LoadChildInvoicesAsync();
    }

    private async Task OnChildChangedAsync(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var cid))
        {
            _selectedChildId = cid;
            var match = _children.FirstOrDefault(c => c.ChildId == cid);
            if (match != null)
                _currency = string.IsNullOrWhiteSpace(match.Currency) ? "INR" : match.Currency;
            _selectedInvoice = null;
            await LoadChildInvoicesAsync();
        }
    }

    private async Task LoadChildInvoicesAsync()
    {
        _isLoading = true;
        try
        {
            _invoices = _selectedChildId != Guid.Empty
                ? await FeeService.GetParentChildInvoicesAsync(_selectedChildId)
                : new List<Invoice>();

            _invoiceInstallments.Clear();
            foreach (var inv in _invoices)
            {
                try
                {
                    var inst = await FeeService.GetParentChildInvoiceInstallmentsAsync(_selectedChildId, inv.Id);
                    if (inst.Any()) _invoiceInstallments[inv.Id] = inst;
                }
                catch { }
            }

            _nextDueDate = _invoices
                .Where(i => i.BalanceDue > 0 && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Void)
                .OrderBy(i => i.DueDate)
                .Select(i => (DateTimeOffset?)i.DueDate)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _isSuccess = false;
            _statusMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SelectInvoiceAsync(Invoice inv)
    {
        if (_selectedInvoice?.Id == inv.Id)
        {
            _selectedInvoice = null;
            return;
        }

        _selectedInvoice = inv;
        _selectedPayments = await FeeService.GetParentChildInvoicePaymentsAsync(_selectedChildId, inv.Id);
        _gatewayStatus = await FeeService.GetParentInvoiceGatewayAvailabilityAsync(_selectedChildId, inv.Id);
        _snapshot = await FeeService.GetInvoicePaymentInstructionsAsync(inv.Id);

        try
        {
            _selectedInstallments = await FeeService.GetParentChildInvoiceInstallmentsAsync(_selectedChildId, inv.Id);
        }
        catch
        {
            _selectedInstallments = new();
        }

        _installmentItems = CalculateInstallmentItems(inv.AmountPaid, _selectedInstallments);
        var nextDue = _installmentItems.FirstOrDefault(i => i.IsNextDue);

        decimal initialAmount;
        if (nextDue != null && nextDue.RemainingAmountOnThisInstallment > 0)
        {
            _paymentModeSelection = "Installment";
            _selectedInstallmentId = nextDue.Installment.Id;
            initialAmount = nextDue.RemainingAmountOnThisInstallment;
        }
        else
        {
            _paymentModeSelection = "Full";
            _selectedInstallmentId = null;
            initialAmount = inv.BalanceDue;
        }

        _razorpayAmount = initialAmount;
        _manualSubmit = new SubmitParentPaymentRequest
        {
            ChildId = _selectedChildId,
            InvoiceId = inv.Id,
            Amount = initialAmount,
            Method = PaymentMethods.QrCode,
            Remarks = nextDue != null ? $"Installment #{nextDue.Installment.InstallmentNumber}" : null
        };
    }

    private void SelectPaymentMode(string mode, InstallmentDisplayItem? instItem = null)
    {
        if (_selectedInvoice == null) return;
        _paymentModeSelection = mode;

        if (mode == "Installment" && instItem != null)
        {
            _selectedInstallmentId = instItem.Installment.Id;
            var amt = instItem.RemainingAmountOnThisInstallment > 0 ? instItem.RemainingAmountOnThisInstallment : instItem.Installment.AmountDue;
            _razorpayAmount = amt;
            _manualSubmit.Amount = amt;
            _manualSubmit.Remarks = $"Installment #{instItem.Installment.InstallmentNumber}";
        }
        else if (mode == "Full")
        {
            _selectedInstallmentId = null;
            var amt = _selectedInvoice.BalanceDue;
            _razorpayAmount = amt;
            _manualSubmit.Amount = amt;
            _manualSubmit.Remarks = "Full balance payment";
        }
        else if (mode == "Custom")
        {
            _selectedInstallmentId = null;
            _manualSubmit.Remarks = "Partial payment";
        }
    }

    private static List<InstallmentDisplayItem> CalculateInstallmentItems(decimal totalAmountPaid, List<InvoiceInstallment> installments)
    {
        var list = new List<InstallmentDisplayItem>();
        decimal cumulativePrev = 0m;
        bool nextDueAssigned = false;

        var sorted = installments.OrderBy(i => i.InstallmentNumber).ToList();
        foreach (var inst in sorted)
        {
            var cumCurrent = cumulativePrev + inst.AmountDue;
            decimal paidOnThis = 0m;
            decimal remainingOnThis = inst.AmountDue;

            if (totalAmountPaid >= cumCurrent)
            {
                paidOnThis = inst.AmountDue;
                remainingOnThis = 0m;
            }
            else if (totalAmountPaid > cumulativePrev)
            {
                paidOnThis = totalAmountPaid - cumulativePrev;
                remainingOnThis = inst.AmountDue - paidOnThis;
            }
            else
            {
                paidOnThis = 0m;
                remainingOnThis = inst.AmountDue;
            }

            var item = new InstallmentDisplayItem
            {
                Installment = inst,
                CumulativeRequired = cumCurrent,
                PaidAmountOnThisInstallment = paidOnThis,
                RemainingAmountOnThisInstallment = remainingOnThis
            };

            if (!item.IsPaid && !nextDueAssigned)
            {
                item.IsNextDue = true;
                nextDueAssigned = true;
            }

            list.Add(item);
            cumulativePrev = cumCurrent;
        }

        return list;
    }

    private async Task PayWithOrgRazorpayAsync()
    {
        if (_selectedInvoice == null) return;
        _isBusy = true;
        _statusMessage = null;
        try
        {
            var order = await FeeService.CreateParentRazorpayOrderAsync(_selectedChildId, _selectedInvoice.Id, _razorpayAmount);
            var options = new
            {
                key = order.KeyId,
                amount = order.AmountInPaise,
                currency = order.Currency,
                name = order.OrganizationName,
                description = $"Childcare Invoice #{order.InvoiceNumber}",
                orderId = order.OrderId,
                invoiceNumber = order.InvoiceNumber,
                invoiceId = _selectedInvoice.Id.ToString(),
                childId = _selectedChildId.ToString(),
                prefillEmail = order.PrefillEmail
            };
            await JS.InvokeVoidAsync("initiateRazorpayPayment", options, _dotNetRef);
        }
        catch (Exception ex)
        {
            _isSuccess = false;
            _statusMessage = ex.Message;
            _isBusy = false;
        }
    }

    [JSInvokable]
    public async Task OnRazorpaySuccess(RazorpayCallbackResponse response)
    {
        if (_selectedInvoice == null) return;
        try
        {
            var result = await FeeService.VerifyParentRazorpayPaymentAsync(new VerifyParentRazorpayPaymentRequest
            {
                ChildId = _selectedChildId,
                InvoiceId = _selectedInvoice.Id,
                Amount = _razorpayAmount,
                RazorpayOrderId = response.OrderId,
                RazorpayPaymentId = response.PaymentId,
                RazorpaySignature = response.Signature
            });

            _isSuccess = result.Success;
            _statusMessage = result.Success
                ? $"Payment of {CurrencyService.Format(_razorpayAmount, _currency, forceDecimals: true)} verified via organization Razorpay."
                : (result.ErrorMessage ?? "Payment verification failed.");

            await LoadChildInvoicesAsync();
            var refreshed = _invoices.FirstOrDefault(i => i.Id == _selectedInvoice.Id);
            if (refreshed != null) await SelectInvoiceAsync(refreshed);
        }
        catch (Exception ex)
        {
            _isSuccess = false;
            _statusMessage = ex.Message;
        }
        finally
        {
            _isBusy = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public Task OnRazorpayFailed(object? _)
    {
        _isSuccess = false;
        _statusMessage = "Payment was not completed.";
        _isBusy = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnRazorpayDismiss()
    {
        _isBusy = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnRazorpayError(string msg)
    {
        _isSuccess = false;
        _statusMessage = msg;
        _isBusy = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnProofSelectedAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;
        using var stream = file.OpenReadStream(2 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        _manualSubmit.ProofStorageKey = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private async Task SubmitManualAsync()
    {
        if (_selectedInvoice == null) return;
        _isBusy = true;
        try
        {
            _manualSubmit.ChildId = _selectedChildId;
            _manualSubmit.InvoiceId = _selectedInvoice.Id;
            await FeeService.SubmitParentPaymentAsync(_manualSubmit);
            _isSuccess = true;
            _statusMessage = "Payment Submitted – Waiting for Daycare Verification.";
            await LoadChildInvoicesAsync();
            var refreshed = _invoices.FirstOrDefault(i => i.Id == _selectedInvoice.Id);
            if (refreshed != null) await SelectInvoiceAsync(refreshed);
        }
        catch (Exception ex)
        {
            _isSuccess = false;
            _statusMessage = ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }

    public class InstallmentDisplayItem
    {
        public InvoiceInstallment Installment { get; set; } = null!;
        public decimal CumulativeRequired { get; set; }
        public decimal PaidAmountOnThisInstallment { get; set; }
        public decimal RemainingAmountOnThisInstallment { get; set; }
        public bool IsPaid => RemainingAmountOnThisInstallment <= 0;
        public bool IsNextDue { get; set; }
        public bool IsOverdue => !IsPaid && Installment.DueDate < DateTime.Today;
        public string StatusText => IsPaid ? "Paid" : IsOverdue ? "Overdue" : IsNextDue ? "Current Due" : "Upcoming";
        public string StatusBadgeCss => IsPaid ? "ee-inst-badge-paid" : IsOverdue ? "ee-inst-badge-overdue" : IsNextDue ? "ee-inst-badge-due" : "ee-inst-badge-upcoming";
    }

    public class RazorpayCallbackResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
