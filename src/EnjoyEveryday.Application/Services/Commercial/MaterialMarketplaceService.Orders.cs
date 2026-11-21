using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class MaterialMarketplaceService
{
    #region Order Submission & Shared Workflow

    public async Task<MaterialOrder> SubmitMaterialOrderAsync(CreateMaterialOrderRequest request, CancellationToken ct = default)
    {
        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.");
        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("Order must contain at least one material item.");
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            throw new ArgumentException("Delivery address is required.");

        // Authorize caller has access to Organization
        await EnsureOrganizationAccessAsync(request.OrganizationId, ct);

        var centralLocation = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (centralLocation == null)
            throw new InvalidOperationException("No active central warehouse location found.");

        var orderId = Guid.NewGuid();
        var orderNumber = $"MAT-{DateTime.UtcNow:yyMM}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

        decimal totalPurchase = 0;
        decimal totalRental = 0;
        decimal totalDeposit = 0;
        bool requiresApproval = false;

        var orderItems = new List<MaterialOrderItem>();

        foreach (var reqItem in request.Items)
        {
            var product = await _materialRepository.GetProductByIdAsync(reqItem.ProductId, ct);
            if (product == null || !product.IsActive)
                throw new KeyNotFoundException($"Product '{reqItem.ProductId}' is unavailable.");

            var offering = product.Offering;
            if (offering == null || !offering.IsActive)
                throw new InvalidOperationException($"Product '{product.Name}' has no active commercial offering.");

            var balance = product.StockBalance;
            int availableStock = balance?.AvailableStock ?? 0;

            if (reqItem.Quantity <= 0)
                throw new ArgumentException($"Invalid quantity for '{product.Name}'.");

            if (reqItem.FulfilmentMode == MaterialFulfilmentMode.Purchase)
            {
                if (!offering.AllowPurchase)
                    throw new InvalidOperationException($"Product '{product.Name}' is not offered for purchase.");

                decimal lineTotal = offering.PurchasePrice * reqItem.Quantity;
                totalPurchase += lineTotal;

                if (availableStock < reqItem.Quantity)
                {
                    if (availableStock <= 0)
                    {
                        throw new InvalidOperationException($"Someone has already ordered '{product.Name}' and it is now out of stock. Please remove it from your order.");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Someone has already ordered '{product.Name}'. Only {availableStock} item(s) remain in stock (you requested {reqItem.Quantity}). Please adjust your order quantity.");
                    }
                }

                orderItems.Add(new MaterialOrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    MaterialProductId = product.Id,
                    OfferingId = offering.Id,
                    FulfilmentMode = MaterialFulfilmentMode.Purchase,
                    Quantity = reqItem.Quantity,
                    UnitPrice = offering.PurchasePrice,
                    RentalFee = 0,
                    Deposit = 0,
                    LineTotal = lineTotal,
                    LoanDurationDays = 0,
                    Status = "Requested"
                });
            }
            else // Borrow
            {
                if (!offering.AllowBorrow)
                    throw new InvalidOperationException($"Product '{product.Name}' is not offered for borrowing.");

                // Borrowing reusables requires review / loan verification
                requiresApproval = true;

                int duration = reqItem.LoanDurationDays > 0 ? reqItem.LoanDurationDays : offering.DefaultLoanDurationDays;
                if (duration > offering.MaxLoanDurationDays)
                    duration = offering.MaxLoanDurationDays;

                decimal rentalTotal = offering.RentalFee * reqItem.Quantity;
                decimal depositTotal = offering.RefundableDeposit * reqItem.Quantity;
                totalRental += rentalTotal;
                totalDeposit += depositTotal;

                orderItems.Add(new MaterialOrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    MaterialProductId = product.Id,
                    OfferingId = offering.Id,
                    FulfilmentMode = MaterialFulfilmentMode.Borrow,
                    Quantity = reqItem.Quantity,
                    UnitPrice = 0,
                    RentalFee = offering.RentalFee,
                    Deposit = offering.RefundableDeposit,
                    LineTotal = rentalTotal + depositTotal,
                    LoanDurationDays = duration,
                    Status = "Requested"
                });
            }
        }

        decimal shippingFee = 150.00m; // Standard flat delivery fee
        decimal totalAmount = totalPurchase + totalRental + totalDeposit + shippingFee;

        string initialStatus = requiresApproval 
            ? MaterialWorkflowStatus.RequestSubmitted 
            : MaterialWorkflowStatus.Confirmed;

        string orderType = requiresApproval 
            ? MaterialOrderType.ApprovalRequired.ToString() 
            : MaterialOrderType.StandardCatalog.ToString();

        var order = new MaterialOrder
        {
            Id = orderId,
            OrderNumber = orderNumber,
            TenantId = _tenantContext.TenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            ClassroomId = request.ClassroomId,
            PlannedExperienceId = request.PlannedExperienceId,
            RequestingUserId = _userContext.UserId,
            RequestingUserEmail = _userContext.Email,
            OrderType = orderType,
            WorkflowStatus = initialStatus,
            PaymentStatus = "Pending",
            FulfilmentStatus = "Unfulfilled",
            TotalPurchaseAmount = totalPurchase,
            TotalRentalFee = totalRental,
            TotalDeposit = totalDeposit,
            ShippingFee = shippingFee,
            TaxAmount = 0,
            TotalAmount = totalAmount,
            Currency = "INR",
            DeliveryAddress = request.DeliveryAddress,
            DeliveryContactName = request.DeliveryContactName,
            DeliveryContactPhone = request.DeliveryContactPhone,
            RequiredDeliveryDate = request.RequiredDeliveryDate?.ToUniversalTime(),
            QuotationNotes = request.Notes
        };

        var history = new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "OrderSubmitted",
            PreviousStatus = null,
            NewStatus = initialStatus,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "DaycareOrganization",
            Description = requiresApproval 
                ? $"Order request {orderNumber} submitted by daycare for review and quotation." 
                : $"Order {orderNumber} confirmed directly from catalog. Awaiting payment.",
            Notes = request.Notes,
            IsInternalNote = false
        };

        await _materialRepository.CreateOrderAsync(order, orderItems, history, ct);

        // If standard in-stock order, allocate stock reservation immediately
        if (!requiresApproval)
        {
            foreach (var item in orderItems.Where(i => i.FulfilmentMode == MaterialFulfilmentMode.Purchase))
            {
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    centralLocation.Id,
                    totalDelta: 0,
                    availableDelta: -item.Quantity,
                    reservedDelta: item.Quantity,
                    dispatchedDelta: 0,
                    onLoanDelta: 0,
                    underInspectionDelta: 0,
                    underCleaningDelta: 0,
                    damagedDelta: 0,
                    missingDelta: 0,
                    retiredDelta: 0,
                    ct: ct);

                await _materialRepository.RecordInventoryLedgerAsync(new InventoryLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    MaterialProductId = item.MaterialProductId,
                    LocationId = centralLocation.Id,
                    TransactionType = MaterialInventoryTransactionType.Reservation,
                    QuantityDelta = 0,
                    AvailableDelta = -item.Quantity,
                    ReservedDelta = item.Quantity,
                    ReferenceType = "Order",
                    ReferenceId = order.Id,
                    Notes = $"Stock reserved for order {orderNumber}",
                    PerformedByEmail = _userContext.Email
                }, ct);
            }
        }

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "MaterialOrderCreated",
                    TenantId = _tenantContext.TenantId,
                    OrganizationId = request.OrganizationId,
                    BranchId = request.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = orderNumber,
                    TransactionCategory = "MaterialMarketplace",
                    Title = $"New Material Order: {orderNumber}",
                    Description = $"Order {orderNumber} ({orderItems.Count} item(s)) submitted by {_userContext.Email} for amount ₹{totalAmount:N2}.",
                    Amount = totalAmount,
                    Currency = "INR",
                    Status = initialStatus,
                    ActionUrl = "/marketplace-operations",
                    Metadata = new { OrderId = orderId, OrderNumber = orderNumber, ItemsCount = orderItems.Count, TotalAmount = totalAmount }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish business event for created order {OrderNumber}", orderNumber);
            }
        }

        return (await _materialRepository.GetOrderByIdAsync(orderId, ct))!;
    }

    public async Task<MaterialOrder?> GetOrderDetailsAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) return null;

        // Security check: must have access to organization or be Platform Admin
        if (!IsPlatformAdmin())
        {
            await EnsureOrganizationAccessAsync(order.OrganizationId, ct);
        }

        return order;
    }

    public async Task<List<MaterialOrder>> GetOrdersForOrganizationAsync(Guid organizationId, string? statusFilter = null, CancellationToken ct = default)
    {
        await EnsureOrganizationAccessAsync(organizationId, ct);
        return await _materialRepository.GetOrdersForOrganizationAsync(organizationId, statusFilter, ct);
    }

    public async Task<List<MaterialOrder>> GetPlatformOrderQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        EnsurePlatformPermission();
        return await _materialRepository.GetPlatformOrderQueueAsync(statusFilter, ct);
    }

    public async Task<List<MaterialOrderHistory>> GetOrderTimelineAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        bool isPlatform = IsPlatformAdmin();
        if (!isPlatform)
        {
            await EnsureOrganizationAccessAsync(order.OrganizationId, ct);
        }

        return await _materialRepository.GetOrderHistoryAsync(orderId, isPlatform, ct);
    }

    public async Task ReviewMaterialOrderAsync(ReviewOrderRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        if (request.Approve)
        {
            await _materialRepository.UpdateOrderStatusAsync(
                request.OrderId,
                MaterialWorkflowStatus.UnderReview,
                rejectionReason: null,
                ct: ct);

            await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                EventType = "OrderApprovedForQuotation",
                PreviousStatus = order.WorkflowStatus,
                NewStatus = MaterialWorkflowStatus.UnderReview,
                ActorUserId = _userContext.UserId,
                ActorEmail = _userContext.Email,
                ActorRole = "PlatformAdmin",
                Description = "Order request approved by Platform Admin. Preparing quotation and terms.",
                Notes = request.ReviewNotes,
                IsInternalNote = false
            }, ct);
        }
        else
        {
            await _materialRepository.UpdateOrderStatusAsync(
                request.OrderId,
                MaterialWorkflowStatus.Rejected,
                rejectionReason: request.RejectionReason ?? "Rejected by Platform Admin",
                ct: ct);

            await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                EventType = "OrderRejected",
                PreviousStatus = order.WorkflowStatus,
                NewStatus = MaterialWorkflowStatus.Rejected,
                ActorUserId = _userContext.UserId,
                ActorEmail = _userContext.Email,
                ActorRole = "PlatformAdmin",
                Description = $"Order rejected: {request.RejectionReason}",
                Notes = request.ReviewNotes,
                IsInternalNote = false
            }, ct);
        }
    }

    public async Task ApproveAndConfirmMaterialOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        await _materialRepository.UpdateOrderStatusAsync(
            orderId,
            MaterialWorkflowStatus.Confirmed,
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "OrderApprovedDirectly",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.Confirmed,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = "Order request directly approved & confirmed by EnjoyEveryday platform operations. Ready for daycare payment.",
            IsInternalNote = false
        }, ct);
    }

    public async Task IssueOrderQuotationAsync(IssueQuotationRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        decimal total = request.TotalPurchaseAmount + request.TotalRentalFee + request.TotalDeposit + request.ShippingFee;

        await _materialRepository.UpdateOrderQuotationAsync(
            request.OrderId,
            request.TotalPurchaseAmount,
            request.TotalRentalFee,
            request.TotalDeposit,
            request.ShippingFee,
            total,
            request.QuotationNotes,
            request.ValidUntil ?? DateTimeOffset.UtcNow.AddDays(7),
            ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            EventType = "QuotationIssued",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.QuotationOffered,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = $"Quotation offered: ₹{total:N2} (Purchases: ₹{request.TotalPurchaseAmount:N2}, Rentals: ₹{request.TotalRentalFee:N2}, Deposit: ₹{request.TotalDeposit:N2}, Shipping: ₹{request.ShippingFee:N2}). Valid until {request.ValidUntil:d MMM yyyy}.",
            Notes = request.QuotationNotes,
            IsInternalNote = false
        }, ct);
    }

    public async Task AcceptOrderQuotationAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        await EnsureOrganizationAccessAsync(order.OrganizationId, ct);

        if (order.WorkflowStatus != MaterialWorkflowStatus.QuotationOffered)
            throw new InvalidOperationException($"Cannot accept quotation for order in '{order.WorkflowStatus}' state.");

        if (order.QuotationValidUntil.HasValue && order.QuotationValidUntil.Value < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("The quotation has expired. Please request an updated quotation.");

        await _materialRepository.UpdateOrderStatusAsync(
            orderId,
            MaterialWorkflowStatus.Confirmed,
            paymentStatus: "Pending",
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "QuotationAccepted",
            PreviousStatus = MaterialWorkflowStatus.QuotationOffered,
            NewStatus = MaterialWorkflowStatus.Confirmed,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "DaycareOrganization",
            Description = "Daycare organization accepted quotation and terms. Proceeding to payment and fulfilment.",
            IsInternalNote = false
        }, ct);
    }

    public async Task CancelOrderAsync(Guid orderId, string reason, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        if (!IsPlatformAdmin())
        {
            await EnsureOrganizationAccessAsync(order.OrganizationId, ct);
        }

        if (order.WorkflowStatus == MaterialWorkflowStatus.Delivered ||
            order.WorkflowStatus == MaterialWorkflowStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel an order that has already been delivered or completed.");
        }

        // If already dispatched, reverse inventory back to warehouse
        if (order.WorkflowStatus == MaterialWorkflowStatus.Dispatched)
        {
            await _materialRepository.ReverseDispatchedInventoryAsync(order.Id, _userContext.Email ?? "PlatformAdmin", ct);
        }

        // Release any reserved stock if order was confirmed/allocated
        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central != null && (order.WorkflowStatus == MaterialWorkflowStatus.Confirmed || order.WorkflowStatus == MaterialWorkflowStatus.StockAllocated || order.WorkflowStatus == MaterialWorkflowStatus.PickingPacking))
        {
            foreach (var item in order.Items)
            {
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    central.Id,
                    totalDelta: 0,
                    availableDelta: item.Quantity,
                    reservedDelta: -item.Quantity,
                    dispatchedDelta: 0,
                    onLoanDelta: 0,
                    underInspectionDelta: 0,
                    underCleaningDelta: 0,
                    damagedDelta: 0,
                    missingDelta: 0,
                    retiredDelta: 0,
                    ct: ct);

                await _materialRepository.RecordInventoryLedgerAsync(new InventoryLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    MaterialProductId = item.MaterialProductId,
                    LocationId = central.Id,
                    TransactionType = MaterialInventoryTransactionType.ReservationRelease,
                    QuantityDelta = 0,
                    AvailableDelta = item.Quantity,
                    ReservedDelta = -item.Quantity,
                    ReferenceType = "OrderCancellation",
                    ReferenceId = order.Id,
                    Notes = $"Reservation released on cancellation of {order.OrderNumber}",
                    PerformedByEmail = _userContext.Email
                }, ct);
            }
        }

        await _materialRepository.UpdateOrderStatusAsync(
            orderId,
            MaterialWorkflowStatus.Cancelled,
            cancellationReason: reason,
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "OrderCancelled",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.Cancelled,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = IsPlatformAdmin() ? "PlatformAdmin" : "DaycareOrganization",
            Description = $"Order cancelled: {reason}",
            Notes = reason,
            IsInternalNote = false
        }, ct);

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "MaterialOrderCancelled",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "MaterialMarketplace",
                    Title = $"Order Cancelled: {order.OrderNumber}",
                    Description = $"Order {order.OrderNumber} was cancelled by {(_userContext.Email ?? "User")}. Reason: {reason}",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    Status = MaterialWorkflowStatus.Cancelled,
                    ActionUrl = "/marketplace-operations",
                    Metadata = new { OrderId = order.Id, OrderNumber = order.OrderNumber, Reason = reason }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish business event for cancelled order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    #endregion

    #region Payments

    public async Task<RazorpayOrderResponse> CreateMaterialRazorpayOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        await EnsureOrganizationAccessAsync(order.OrganizationId, ct);

        if (order.PaymentStatus == "Completed")
            throw new InvalidOperationException("Order is already fully paid.");

        long amountInPaise = (long)Math.Round(order.TotalAmount * 100);

        var rzpOrder = await _razorpayGateway.CreateOrderAsync(
            amountInPaise,
            "INR",
            order.OrderNumber,
            new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["OrderNumber"] = order.OrderNumber,
                ["OrganizationId"] = order.OrganizationId.ToString()
            },
            ct);

        return new RazorpayOrderResponse
        {
            OrderId = rzpOrder.OrderId,
            KeyId = _razorpayGateway.Settings.KeyId,
            Amount = order.TotalAmount,
            AmountInPaise = amountInPaise,
            Currency = "INR",
            InvoiceNumber = order.OrderNumber,
            OrganizationName = order.OrganizationName ?? "Daycare Organization",
            CustomerEmail = order.RequestingUserEmail,
            OutstandingBalance = order.TotalAmount
        };
    }

    public async Task<bool> VerifyMaterialRazorpayPaymentAsync(VerifyMaterialOrderPaymentRequest request, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        await EnsureOrganizationAccessAsync(order.OrganizationId, ct);

        bool isValid = _razorpayGateway.VerifyPaymentSignature(
            request.RazorpayOrderId,
            request.RazorpayPaymentId,
            request.RazorpaySignature);

        if (!isValid)
        {
            _logger.LogWarning("Invalid Razorpay signature for order {OrderNumber}", order.OrderNumber);
            return false;
        }

        // Record payment in the unified Payment ledger
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
            OrganizationId = order.OrganizationId,
            InvoiceId = null,
            InvoiceNumber = order.OrderNumber,
            PaymentReference = order.OrderNumber,
            Amount = order.TotalAmount,
            Currency = "INR",
            Method = PaymentMethods.Razorpay,
            Status = PaymentStatus.Completed,
            IsVerified = true,
            VerifiedBy = "RazorpayGateway",
            VerifiedAt = DateTimeOffset.UtcNow,
            ProviderOrderId = request.RazorpayOrderId,
            ProviderPaymentId = request.RazorpayPaymentId,
            ProviderSignature = request.RazorpaySignature,
            Notes = $"Online payment for Materials Order {order.OrderNumber}"
        };

        await _billingRepository.RecordPaymentAsync(payment, ct);

        await _materialRepository.UpdateOrderStatusAsync(
            order.Id,
            MaterialWorkflowStatus.Confirmed,
            paymentStatus: "Completed",
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            EventType = "PaymentRecorded",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.Confirmed,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "DaycareOrganization",
            Description = $"Online payment of ₹{order.TotalAmount:N2} verified via Razorpay (Txn: {request.RazorpayPaymentId}).",
            IsInternalNote = false
        }, ct);

        return true;
    }

    public async Task RecordManualMaterialPaymentAsync(RecordManualOrderPaymentRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        string receiptNum = !string.IsNullOrWhiteSpace(request.ReceiptNumber)
            ? request.ReceiptNumber.Trim()
            : $"RCP-MAT-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
            OrganizationId = order.OrganizationId,
            InvoiceId = null,
            InvoiceNumber = order.OrderNumber,
            PaymentReference = order.OrderNumber,
            Amount = request.Amount,
            Currency = "INR",
            Method = request.PaymentMethod,
            Status = PaymentStatus.Completed,
            IsVerified = true,
            VerifiedBy = _userContext.Email ?? "PlatformAdmin",
            VerifiedAt = DateTimeOffset.UtcNow,
            ReceiptNumber = receiptNum,
            RecordedByUserId = _userContext.UserId,
            RecordedByEmail = _userContext.Email,
            Notes = request.Notes ?? $"Manual payment recorded for Materials Order {order.OrderNumber}"
        };

        await _billingRepository.RecordPaymentAsync(payment, ct);

        await _materialRepository.UpdateOrderStatusAsync(
            order.Id,
            MaterialWorkflowStatus.Confirmed,
            paymentStatus: "Completed",
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            EventType = "ManualPaymentRecorded",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.Confirmed,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = $"Official manual payment of ₹{request.Amount:N2} ({request.PaymentMethod}) recorded with Receipt #{receiptNum}.",
            Notes = request.Notes,
            IsInternalNote = false
        }, ct);
    }

    #endregion

}
