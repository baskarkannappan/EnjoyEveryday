using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

public partial class MaterialOrdersPage : ComponentBase, IDisposable
{
    [Inject]
    private IMaterialMarketplaceService MaterialService { get; set; } = default!;

    [Inject]
    private IUserContext UserContext { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private string _activeTab = "Orders";
    private string _selectedStatusFilter = "All";
    private bool _isLoading = true;

    private List<MaterialOrder> _orders = new();
    private List<MaterialLoan> _loans = new();
    private List<MaterialRecurringSchedule> _schedules = new();

    private MaterialOrder? _selectedOrder;
    private List<MaterialOrderHistory> _orderTimeline = new();
    private bool _isDetailsDrawerOpen = false;

    private MaterialOrder? _paymentModalOrder;
    private bool _isProcessingPayment = false;
    private Guid? _processingOrderId;
    private string? _paymentErrorMessage;
    private DotNetObjectReference<MaterialOrdersPage>? _dotNetRef;

    private MaterialOrder? _orderToCancel;
    private string _cancelReason = "";

    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        try
        {
            var orgId = UserContext.OrganizationId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            _orders = await MaterialService.GetOrdersForOrganizationAsync(orgId);
            _loans = await MaterialService.GetOrganizationLoansAsync(orgId);
            _schedules = await MaterialService.GetRecurringSchedulesAsync(orgId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading orders: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private IEnumerable<MaterialOrder> FilteredOrders
    {
        get
        {
            if (_selectedStatusFilter == "Pending Review")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.RequestSubmitted || o.WorkflowStatus == MaterialWorkflowStatus.UnderReview);
            if (_selectedStatusFilter == "Quotation Offered")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.QuotationOffered);
            if (_selectedStatusFilter == "Payment Pending")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.Confirmed && o.PaymentStatus != "Completed");
            if (_selectedStatusFilter == "Dispatched")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.Dispatched || o.WorkflowStatus == MaterialWorkflowStatus.InTransit);
            if (_selectedStatusFilter == "Receipt Confirmed")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.ReceiptConfirmed);
            if (_selectedStatusFilter == "Completed")
                return _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.Completed);

            return _orders;
        }
    }

    private void SetTab(string tab)
    {
        _activeTab = tab;
    }

    private void FilterOrders(string filter)
    {
        _selectedStatusFilter = filter;
    }

    private async Task OpenOrderDetails(MaterialOrder order)
    {
        _selectedOrder = order;
        _orderTimeline = await MaterialService.GetOrderTimelineAsync(order.Id);
        _isDetailsDrawerOpen = true;
    }

    private async Task AcceptQuotation(MaterialOrder order)
    {
        await MaterialService.AcceptOrderQuotationAsync(order.Id);
        await LoadDataAsync();
    }

    private void OpenPaymentModal(MaterialOrder order)
    {
        _paymentModalOrder = order;
        _paymentErrorMessage = null;
        _isProcessingPayment = false;
    }

    private void ClosePaymentModal()
    {
        _paymentModalOrder = null;
        _paymentErrorMessage = null;
        _isProcessingPayment = false;
    }

    private async Task PayOrderDirectly(MaterialOrder order)
    {
        if (_isProcessingPayment) return;
        _paymentModalOrder = order;
        _processingOrderId = order.Id;
        _paymentErrorMessage = null;
        _isProcessingPayment = true;
        StateHasChanged();

        try
        {
            var rzpOrder = await MaterialService.CreateMaterialRazorpayOrderAsync(order.Id);
            var options = new
            {
                key = rzpOrder.KeyId,
                amount = rzpOrder.AmountInPaise,
                currency = rzpOrder.Currency,
                name = "EnjoyEveryday Materials",
                description = $"Payment for Order {order.OrderNumber}",
                orderId = rzpOrder.OrderId,
                invoiceId = order.Id.ToString(),
                invoiceNumber = order.OrderNumber,
                organizationId = order.OrganizationId.ToString(),
                prefillName = order.OrganizationName ?? "Daycare Admin",
                prefillEmail = order.RequestingUserEmail ?? ""
            };

            await JS.InvokeVoidAsync("initiateRazorpayPayment", options, _dotNetRef);
        }
        catch (Exception ex)
        {
            _paymentErrorMessage = $"Could not launch Razorpay: {ex.Message}";
            _isProcessingPayment = false;
            // Leave _paymentModalOrder open so the user sees the error and can click Quick Simulation
            StateHasChanged();
        }
    }

    private async Task PayOrderWithRazorpay()
    {
        if (_paymentModalOrder == null || _isProcessingPayment) return;
        await PayOrderDirectly(_paymentModalOrder);
    }

    [JSInvokable]
    public async Task OnRazorpaySuccess(RazorpayCallbackPayload payload)
    {
        _isProcessingPayment = true;
        StateHasChanged();

        try
        {
            if (_paymentModalOrder == null) return;

            var rzpReq = new VerifyMaterialOrderPaymentRequest
            {
                OrderId = _paymentModalOrder.Id,
                RazorpayOrderId = payload.OrderId,
                RazorpayPaymentId = payload.PaymentId,
                RazorpaySignature = payload.Signature
            };

            var verified = await MaterialService.VerifyMaterialRazorpayPaymentAsync(rzpReq);
            if (!verified)
            {
                _paymentErrorMessage = "Razorpay payment verification failed on server.";
                _isProcessingPayment = false;
                _processingOrderId = null;
                StateHasChanged();
                return;
            }

            _paymentModalOrder = null;
            _processingOrderId = null;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _paymentErrorMessage = $"Error confirming payment: {ex.Message}";
        }
        finally
        {
            _isProcessingPayment = false;
            _processingOrderId = null;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnRazorpayDismiss()
    {
        _isProcessingPayment = false;
        _processingOrderId = null;
        if (string.IsNullOrWhiteSpace(_paymentErrorMessage))
        {
            _paymentModalOrder = null;
        }
        StateHasChanged();
    }

    [JSInvokable]
    public void OnRazorpayError(string message)
    {
        _paymentErrorMessage = message;
        _isProcessingPayment = false;
        _processingOrderId = null;
        StateHasChanged();
    }

    private async Task SimulateRazorpayPayment()
    {
        if (_paymentModalOrder == null || _isProcessingPayment) return;
        await QuickSimulateOrderPayment(_paymentModalOrder);
    }

    private async Task QuickSimulateOrderPayment(MaterialOrder order)
    {
        if (_isProcessingPayment) return;
        _isProcessingPayment = true;
        _processingOrderId = order.Id;
        _paymentErrorMessage = null;
        StateHasChanged();

        try
        {
            var rzpReq = new VerifyMaterialOrderPaymentRequest
            {
                OrderId = order.Id,
                RazorpayOrderId = $"order_sim_{Random.Shared.Next(10000, 99999)}",
                RazorpayPaymentId = $"pay_sim_{Random.Shared.Next(10000, 99999)}",
                RazorpaySignature = "mock_sig_valid"
            };

            var verified = await MaterialService.VerifyMaterialRazorpayPaymentAsync(rzpReq);
            if (!verified)
            {
                _paymentErrorMessage = "Payment signature verification failed. Please check payment gateway settings.";
                return;
            }

            _paymentModalOrder = null;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _paymentErrorMessage = $"Test payment simulation failed: {ex.Message}";
        }
        finally
        {
            _isProcessingPayment = false;
            _processingOrderId = null;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }

    private void PromptCancelOrder(MaterialOrder order)
    {
        _orderToCancel = order;
        _cancelReason = "Requested by daycare admin";
    }

    private async Task ConfirmCancelOrder()
    {
        if (_orderToCancel == null) return;
        await MaterialService.CancelOrderAsync(_orderToCancel.Id, string.IsNullOrWhiteSpace(_cancelReason) ? "Cancelled by user" : _cancelReason);
        _orderToCancel = null;
        _isDetailsDrawerOpen = false;
        await LoadDataAsync();
    }

    public class RazorpayCallbackPayload
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    private async Task ConfirmReceipt(MaterialOrder order, bool success)
    {
        await MaterialService.ConfirmDeliveryReceiptAsync(new ConfirmDeliveryReceiptRequest
        {
            OrderId = order.Id,
            IsDeliveredSuccessfully = success,
            Notes = "Confirmed received in good condition by Daycare Admin."
        });
        await LoadDataAsync();
    }

    private async Task InitiateReturn(MaterialLoan loan)
    {
        await MaterialService.InitiateLoanReturnAsync(loan.Id, "Packed in original box, dispatched back via courier");
        await LoadDataAsync();
    }

    private async Task ToggleScheduleStatus(MaterialRecurringSchedule schedule, string newStatus)
    {
        await MaterialService.UpdateRecurringScheduleStatusAsync(schedule.Id, newStatus);
        await LoadDataAsync();
    }

    private void OpenNewRecurringModal()
    {
        // Route to marketplace to pick product for schedule
        NavigationManager.NavigateTo("/materials-marketplace");
    }

    private string FormatStatus(string status) => status switch
    {
        "RequestSubmitted" => "Request Submitted",
        "UnderReview" => "Under Platform Review",
        "QuotationOffered" => "Quotation Offered",
        "Confirmed" => "Confirmed",
        "PickingPacking" => "Picking & Packing",
        "Dispatched" => "Dispatched / In Transit",
        "Delivered" => "Delivered",
        "ReceiptConfirmed" => "Receipt Confirmed",
        "Completed" => "Completed",
        "Rejected" => "Request Rejected",
        "Cancelled" => "Cancelled",
        _ => status
    };

    private string GetStatusClass(string status) => status.ToLowerInvariant();

    private string GetNextActionHint(MaterialOrder order) => order.WorkflowStatus switch
    {
        "RequestSubmitted" => "Waiting for EnjoyEveryday platform operations to review feasibility and terms.",
        "UnderReview" => "Platform admin is checking warehouse stock and preparing loan/quotation terms.",
        "QuotationOffered" => "Please review and accept quoted price & terms to proceed.",
        "Confirmed" when order.PaymentStatus != "Completed" => "Payment pending. Complete Razorpay checkout to start warehouse picking.",
        "Confirmed" => "Payment verified. Warehouse is scheduling allocation.",
        "PickingPacking" => "Warehouse staff is picking, packing, and preparing your shipment.",
        "Dispatched" => "Package is on its way. Confirm receipt when delivered to your center.",
        "ReceiptConfirmed" => "Delivered! Borrowed kits remain active until due date.",
        "Completed" => "All items received / returned. Order completed.",
        _ => ""
    };
}
