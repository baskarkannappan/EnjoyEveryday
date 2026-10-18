using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Components;

namespace EnjoyEveryday.Admin.Web.Components.Pages.Operations;

public partial class MarketplaceOperationsPage : ComponentBase
{
    [Inject]
    private IMaterialMarketplaceService MaterialService { get; set; } = default!;

    [Inject]
    private IUserContext UserContext { get; set; } = default!;

    private string _activeQueue = "Requests";
    private bool _isLoading = true;

    private List<MaterialOrder> _orders = new();
    private List<MaterialLoan> _loans = new();

    // Quotation modal
    private MaterialOrder? _quotationModalOrder;
    private decimal _quotePurchaseAmount;
    private decimal _quoteRentalFee;
    private decimal _quoteDeposit;
    private decimal _quoteShippingFee = 150m;
    private DateTime? _quoteValidUntil = DateTime.Today.AddDays(7);
    private string _quoteNotes = "";

    // Manual Payment modal
    private MaterialOrder? _manualPaymentOrder;
    private decimal _manualPaymentAmount;
    private string _manualPaymentMethod = "Cash";
    private string _manualReceiptNumber = "";
    private string _manualPaymentNotes = "";

    // Dispatch modal
    private MaterialOrder? _dispatchOrder;
    private string _dispatchCarrier = "Blue Dart Express";
    private string _dispatchTrackingNumber = "";
    private DateTime? _dispatchEstDelivery = DateTime.Today.AddDays(2);
    private string _dispatchNotes = "";

    // Inspection modal
    private MaterialLoan? _inspectionLoan;
    private string _inspectCondition = "Good";
    private int _inspectApprovedQty = 1;
    private int _inspectCleaningQty = 0;
    private int _inspectDamagedQty = 0;
    private int _inspectMissingQty = 0;
    private string _inspectDepositDisposition = "FullRefund";
    private decimal _inspectDeductionAmount = 0;
    private string _inspectNotes = "";

    // Timeline modal
    private MaterialOrder? _selectedOrder;
    private List<MaterialOrderHistory> _orderTimeline = new();
    private bool _isTimelineOpen = false;

    // Operation notices
    private string? _operationErrorMessage;
    private MaterialOrder? _insufficientStockOrder;

    protected override async Task OnInitializedAsync()
    {
        await LoadQueueDataAsync();
    }

    private async Task LoadQueueDataAsync()
    {
        _isLoading = true;
        try
        {
            _orders = await MaterialService.GetPlatformOrderQueueAsync();
            _loans = await MaterialService.GetPlatformLoanQueueAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading platform queue: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RefreshQueue()
    {
        await LoadQueueDataAsync();
    }

    private void SetQueue(string queue)
    {
        _activeQueue = queue;
    }

    private IEnumerable<MaterialOrder> FilteredOrders
    {
        get
        {
            return _activeQueue switch
            {
                "Requests" => _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.RequestSubmitted || o.WorkflowStatus == MaterialWorkflowStatus.UnderReview),
                "Fulfilment" => _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.Confirmed || o.WorkflowStatus == MaterialWorkflowStatus.PickingPacking),
                "Shipments" => _orders.Where(o => o.WorkflowStatus == MaterialWorkflowStatus.Dispatched || o.WorkflowStatus == MaterialWorkflowStatus.InTransit),
                _ => _orders
            };
        }
    }

    private async Task OpenOrderDetails(MaterialOrder order)
    {
        _selectedOrder = order;
        _orderTimeline = await MaterialService.GetOrderTimelineAsync(order.Id);
        _isTimelineOpen = true;
    }

    private async Task OpenOrderFromLoan(MaterialLoan loan)
    {
        var ord = _orders.FirstOrDefault(o => o.Id == loan.OrderId) ?? await MaterialService.GetOrderDetailsAsync(loan.OrderId);
        if (ord != null)
        {
            await OpenOrderDetails(ord);
        }
    }

    private void OpenQuotationModal(MaterialOrder order)
    {
        _quotationModalOrder = order;
        _quotePurchaseAmount = order.TotalPurchaseAmount;
        _quoteRentalFee = order.TotalRentalFee;
        _quoteDeposit = order.TotalDeposit;
        _quoteShippingFee = order.ShippingFee > 0 ? order.ShippingFee : 150m;
        _quoteValidUntil = DateTime.Today.AddDays(7);
        _quoteNotes = order.QuotationNotes ?? "Quotation and loan period terms approved by Platform Warehouse Operations.";
    }

    private async Task SubmitQuotation()
    {
        if (_quotationModalOrder == null) return;

        var req = new IssueQuotationRequest
        {
            OrderId = _quotationModalOrder.Id,
            TotalPurchaseAmount = _quotePurchaseAmount,
            TotalRentalFee = _quoteRentalFee,
            TotalDeposit = _quoteDeposit,
            ShippingFee = _quoteShippingFee,
            QuotationNotes = _quoteNotes,
            ValidUntil = _quoteValidUntil.HasValue ? new DateTimeOffset(_quoteValidUntil.Value) : DateTimeOffset.UtcNow.AddDays(7)
        };

        await MaterialService.IssueOrderQuotationAsync(req);
        _quotationModalOrder = null;
        await LoadQueueDataAsync();
    }

    private async Task RejectOrder(MaterialOrder order)
    {
        await MaterialService.ReviewMaterialOrderAsync(new ReviewOrderRequest
        {
            OrderId = order.Id,
            Approve = false,
            RejectionReason = "Item or rental capacity unavailable for selected dates."
        });
        await LoadQueueDataAsync();
    }

    private async Task ApproveOrderDirectly(MaterialOrder order)
    {
        await MaterialService.ApproveAndConfirmMaterialOrderAsync(order.Id);
        await LoadQueueDataAsync();
    }

    private void OpenManualPaymentModal(MaterialOrder order)
    {
        _manualPaymentOrder = order;
        _manualPaymentAmount = order.TotalAmount;
        _manualPaymentMethod = "Cash";
        _manualReceiptNumber = $"RCP-MAT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        _manualPaymentNotes = "In-person cash receipt verified by Platform Admin";
    }

    private async Task SubmitManualPayment()
    {
        if (_manualPaymentOrder == null) return;

        await MaterialService.RecordManualMaterialPaymentAsync(new RecordManualOrderPaymentRequest
        {
            OrderId = _manualPaymentOrder.Id,
            Amount = _manualPaymentAmount,
            PaymentMethod = _manualPaymentMethod,
            ReceiptNumber = _manualReceiptNumber,
            Notes = _manualPaymentNotes
        });

        _manualPaymentOrder = null;
        await LoadQueueDataAsync();
    }

    private async Task AllocateStock(MaterialOrder order)
    {
        _operationErrorMessage = null;
        _insufficientStockOrder = null;
        try
        {
            await MaterialService.AllocateStockAndPrepareOrderAsync(order.Id);
            await LoadQueueDataAsync();
        }
        catch (InvalidOperationException ex)
        {
            _operationErrorMessage = ex.Message;
            _insufficientStockOrder = order;
        }
        catch (Exception ex)
        {
            _operationErrorMessage = ex.Message;
        }
    }

    private async Task QuickReplenishAndAllocate()
    {
        if (_insufficientStockOrder == null) return;
        try
        {
            var locations = await MaterialService.GetWarehouseLocationsAsync();
            var central = locations.FirstOrDefault(l => l.IsCentralWarehouse) ?? locations.FirstOrDefault();
            if (central == null) return;

            var balances = await MaterialService.GetWarehouseInventoryBalancesAsync(central.Id);

            foreach (var item in _insufficientStockOrder.Items)
            {
                var itemBal = balances.FirstOrDefault(b => b.MaterialProductId == item.MaterialProductId);
                int available = itemBal?.AvailableStock ?? 0;
                if (available < item.Quantity)
                {
                    int replenishAmount = Math.Max(20, item.Quantity - available + 10);
                    await MaterialService.AdjustInventoryStockAsync(new AdjustStockRequest
                    {
                        ProductId = item.MaterialProductId,
                        LocationId = central.Id,
                        TransactionType = "Receipt",
                        QuantityDelta = replenishAmount,
                        Notes = $"Operational quick-replenishment for Order {_insufficientStockOrder.OrderNumber}"
                    });
                }
            }

            // Retry allocation
            await MaterialService.AllocateStockAndPrepareOrderAsync(_insufficientStockOrder.Id);
            _insufficientStockOrder = null;
            _operationErrorMessage = null;
            await LoadQueueDataAsync();
        }
        catch (Exception ex)
        {
            _operationErrorMessage = $"Quick replenish failed: {ex.Message}";
        }
    }

    private void OpenDispatchModal(MaterialOrder order)
    {
        _dispatchOrder = order;
        _dispatchCarrier = "Blue Dart Express";
        _dispatchTrackingNumber = $"BLU-{Random.Shared.Next(100000, 999999)}";
        _dispatchEstDelivery = DateTime.Today.AddDays(2);
        _dispatchNotes = "Picked, double-checked, packed in recyclable carton";
    }

    private async Task SubmitDispatch()
    {
        if (_dispatchOrder == null) return;

        await MaterialService.DispatchOrderShipmentAsync(new DispatchShipmentRequest
        {
            OrderId = _dispatchOrder.Id,
            CarrierName = _dispatchCarrier,
            TrackingReference = _dispatchTrackingNumber,
            EstimatedDeliveryDate = _dispatchEstDelivery.HasValue ? new DateTimeOffset(_dispatchEstDelivery.Value) : DateTimeOffset.UtcNow.AddDays(2),
            DispatchNotes = _dispatchNotes
        });

        _dispatchOrder = null;
        await LoadQueueDataAsync();
    }

    private async Task MarkDelivered(MaterialOrder order)
    {
        await MaterialService.ConfirmDeliveryReceiptAsync(new ConfirmDeliveryReceiptRequest
        {
            OrderId = order.Id,
            IsDeliveredSuccessfully = true,
            Notes = "Carrier delivery update recorded by Platform Operations"
        });
        await LoadQueueDataAsync();
    }

    private async Task ReceiveReturnAtWarehouse(MaterialLoan loan)
    {
        await MaterialService.ReceiveLoanReturnAtWarehouseAsync(loan.Id, loan.QuantityIssued, "Package received at central intake dock");
        await LoadQueueDataAsync();
    }

    private void OpenInspectionModal(MaterialLoan loan)
    {
        _inspectionLoan = loan;
        _inspectCondition = "Good";
        _inspectApprovedQty = loan.QuantityIssued;
        _inspectCleaningQty = 0;
        _inspectDamagedQty = 0;
        _inspectMissingQty = 0;
        _inspectDepositDisposition = "FullRefund";
        _inspectDeductionAmount = 0;
        _inspectNotes = "Kit contents verified against inventory list; sanitized before restocking.";
    }

    private async Task SubmitInspection()
    {
        if (_inspectionLoan == null) return;

        await MaterialService.CompleteReturnInspectionAsync(new CompleteReturnInspectionRequest
        {
            LoanId = _inspectionLoan.Id,
            ReturnedQuantity = _inspectionLoan.QuantityIssued,
            Condition = _inspectCondition,
            QuantityApprovedForStock = _inspectApprovedQty,
            QuantityRequiringCleaning = _inspectCleaningQty,
            QuantityDamaged = _inspectDamagedQty,
            QuantityMissing = _inspectMissingQty,
            DepositDisposition = _inspectDepositDisposition,
            DepositDeductionAmount = _inspectDeductionAmount,
            InspectionNotes = _inspectNotes
        });

        _inspectionLoan = null;
        await LoadQueueDataAsync();
    }
}
