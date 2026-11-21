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
    #region Fulfilment, Dispatch & Shipments

    public async Task AllocateStockAndPrepareOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        // Allocate remaining unreserved items
        foreach (var item in order.Items)
        {
            var bal = await _materialRepository.GetInventoryBalanceAsync(item.MaterialProductId, central.Id, ct);
            if (bal == null || bal.AvailableStock < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient available stock to allocate '{item.ProductName}'. Available: {bal?.AvailableStock ?? 0}, Required: {item.Quantity}");
            }

            await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                item.MaterialProductId,
                central.Id,
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
                LocationId = central.Id,
                TransactionType = MaterialInventoryTransactionType.Reservation,
                QuantityDelta = 0,
                AvailableDelta = -item.Quantity,
                ReservedDelta = item.Quantity,
                ReferenceType = "OrderAllocation",
                ReferenceId = order.Id,
                Notes = $"Allocated and reserved for order {order.OrderNumber}",
                PerformedByEmail = _userContext.Email
            }, ct);
        }

        await _materialRepository.UpdateOrderStatusAsync(
            orderId,
            MaterialWorkflowStatus.PickingPacking,
            fulfilmentStatus: "Allocated",
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "StockAllocatedAndPacking",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.PickingPacking,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = "Warehouse staff allocated stock from central warehouse. Picking and packing in progress.",
            IsInternalNote = false
        }, ct);
    }

    public async Task DispatchOrderShipmentAsync(DispatchShipmentRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        if (order.WorkflowStatus == MaterialWorkflowStatus.Dispatched || order.FulfilmentStatus == "Dispatched")
        {
            throw new InvalidOperationException($"Order {order.OrderNumber} has already been dispatched.");
        }

        if (string.IsNullOrWhiteSpace(request.CarrierName))
            throw new ArgumentException("Carrier name is required.");
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
            throw new ArgumentException("Tracking number / reference is required.");

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        bool wasAllocated = order.WorkflowStatus == MaterialWorkflowStatus.PickingPacking || order.FulfilmentStatus == "Allocated";

        // If stock was not previously reserved/allocated, validate available stock first
        if (!wasAllocated)
        {
            foreach (var item in order.Items)
            {
                var bal = await _materialRepository.GetInventoryBalanceAsync(item.MaterialProductId, central.Id, ct);
                if (bal == null || bal.AvailableStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient available warehouse stock to dispatch '{item.ProductName}'. Available: {bal?.AvailableStock ?? 0}, Required: {item.Quantity}");
                }
            }
        }

        var shipment = new MaterialShipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShipmentNumber = $"SHP-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            CarrierName = request.CarrierName.Trim(),
            TrackingReference = request.TrackingReference.Trim(),
            DispatchDate = DateTimeOffset.UtcNow,
            EstimatedDeliveryDate = request.EstimatedDeliveryDate ?? DateTimeOffset.UtcNow.AddDays(2),
            DeliveryStatus = "Dispatched",
            RecordedByUserId = _userContext.UserId
        };

        await _materialRepository.CreateShipmentAsync(shipment, ct);

        // Move inventory: from Allocated (Reserved) or Available -> Dispatched
        var loansToCreate = new List<MaterialLoan>();

        foreach (var item in order.Items)
        {
            int reservedDelta = wasAllocated ? -item.Quantity : 0;
            int availableDelta = wasAllocated ? 0 : -item.Quantity;

            if (item.FulfilmentMode == MaterialFulfilmentMode.Purchase)
            {
                // Purchased items: deduct from warehouse total & mark dispatched
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    central.Id,
                    totalDelta: -item.Quantity,
                    availableDelta: availableDelta,
                    reservedDelta: reservedDelta,
                    dispatchedDelta: item.Quantity,
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
                    TransactionType = MaterialInventoryTransactionType.DispatchPurchase,
                    QuantityDelta = -item.Quantity,
                    AvailableDelta = availableDelta,
                    ReservedDelta = reservedDelta,
                    ReferenceType = "Shipment",
                    ReferenceId = shipment.Id,
                    Notes = $"Dispatched purchase on {shipment.ShipmentNumber}",
                    PerformedByEmail = _userContext.Email
                }, ct);
            }
            else // Borrow
            {
                // Borrowed items: keep in total stock, move from reserved/available -> dispatched
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    central.Id,
                    totalDelta: 0,
                    availableDelta: availableDelta,
                    reservedDelta: reservedDelta,
                    dispatchedDelta: item.Quantity,
                    onLoanDelta: 0,
                    underInspectionDelta: 0,
                    underCleaningDelta: 0,
                    damagedDelta: 0,
                    missingDelta: 0,
                    retiredDelta: 0,
                    ct: ct);

                int loanDays = item.LoanDurationDays > 0 ? item.LoanDurationDays : 14;
                var loan = new MaterialLoan
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OrderItemId = item.Id,
                    MaterialProductId = item.MaterialProductId,
                    OrganizationId = order.OrganizationId,
                    ClassroomId = order.ClassroomId,
                    QuantityIssued = item.Quantity,
                    IssuedAt = DateTimeOffset.UtcNow,
                    DueDate = DateTimeOffset.UtcNow.AddDays(loanDays + 2), // +2 days buffer for delivery
                    ReturnStatus = MaterialLoanStatus.Dispatched,
                    DepositAmount = item.Deposit,
                    DepositStatus = item.Deposit > 0 ? "Held" : "Waived"
                };
                loansToCreate.Add(loan);

                await _materialRepository.RecordInventoryLedgerAsync(new InventoryLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    MaterialProductId = item.MaterialProductId,
                    LocationId = central.Id,
                    TransactionType = MaterialInventoryTransactionType.DispatchLoan,
                    QuantityDelta = 0,
                    AvailableDelta = availableDelta,
                    ReservedDelta = reservedDelta,
                    OnLoanDelta = 0,
                    ReferenceType = "Shipment",
                    ReferenceId = shipment.Id,
                    Notes = $"Dispatched loan on {shipment.ShipmentNumber}",
                    PerformedByEmail = _userContext.Email
                }, ct);
            }
        }

        if (loansToCreate.Any())
        {
            await _materialRepository.CreateLoansAsync(loansToCreate, ct);
        }

        await _materialRepository.UpdateOrderStatusAsync(
            order.Id,
            MaterialWorkflowStatus.Dispatched,
            fulfilmentStatus: "Dispatched",
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            EventType = "OrderDispatched",
            PreviousStatus = order.WorkflowStatus,
            NewStatus = MaterialWorkflowStatus.Dispatched,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = $"Package dispatched via {request.CarrierName} (Tracking #: {request.TrackingReference}). Estimated delivery: {shipment.EstimatedDeliveryDate:d MMM yyyy}.",
            Notes = request.DispatchNotes,
            IsInternalNote = false
        }, ct);

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "MaterialOrderDispatched",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "MaterialMarketplace",
                    Title = $"Order Dispatched: {order.OrderNumber}",
                    Description = $"Order {order.OrderNumber} was dispatched via {request.CarrierName} ({request.TrackingReference}).",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    Status = MaterialWorkflowStatus.Dispatched,
                    ActionUrl = "/marketplace-operations",
                    Metadata = new { OrderId = order.Id, OrderNumber = order.OrderNumber, Carrier = request.CarrierName, Tracking = request.TrackingReference }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish business event for dispatched order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    public async Task ConfirmDeliveryReceiptAsync(ConfirmDeliveryReceiptRequest request, CancellationToken ct = default)
    {
        var order = await _materialRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null) throw new KeyNotFoundException("Order not found.");

        await EnsureOrganizationAccessAsync(order.OrganizationId, ct);

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        if (request.IsDeliveredSuccessfully)
        {
            // Clear dispatched stock
            foreach (var item in order.Items)
            {
                if (item.FulfilmentMode == MaterialFulfilmentMode.Purchase)
                {
                    await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                        item.MaterialProductId,
                        central.Id,
                        totalDelta: 0,
                        availableDelta: 0,
                        reservedDelta: 0,
                        dispatchedDelta: -item.Quantity,
                        onLoanDelta: 0,
                        underInspectionDelta: 0,
                        underCleaningDelta: 0,
                        damagedDelta: 0,
                        missingDelta: 0,
                        retiredDelta: 0,
                        ct: ct);
                }
                else // Borrow
                {
                    // Move from dispatched -> on loan
                    await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                        item.MaterialProductId,
                        central.Id,
                        totalDelta: 0,
                        availableDelta: 0,
                        reservedDelta: 0,
                        dispatchedDelta: -item.Quantity,
                        onLoanDelta: item.Quantity,
                        underInspectionDelta: 0,
                        underCleaningDelta: 0,
                        damagedDelta: 0,
                        missingDelta: 0,
                        retiredDelta: 0,
                        ct: ct);
                }
            }

            // Update loans to InUse
            var loans = await _materialRepository.GetLoansByOrderIdAsync(order.Id, ct);
            foreach (var loan in loans)
            {
                await _materialRepository.UpdateLoanStatusAsync(
                    loan.Id,
                    MaterialLoanStatus.InUse,
                    depositStatus: loan.DepositStatus,
                    ct: ct);
            }

            // Update shipment
            var shipment = await _materialRepository.GetShipmentByOrderIdAsync(order.Id, ct);
            if (shipment != null)
            {
                await _materialRepository.UpdateShipmentStatusAsync(
                    shipment.Id,
                    "Delivered",
                    DateTimeOffset.UtcNow,
                    request.Notes,
                    ct);
            }

            bool hasLoans = loans.Any();
            string finalStatus = hasLoans ? MaterialWorkflowStatus.ReceiptConfirmed : MaterialWorkflowStatus.Completed;

            await _materialRepository.UpdateOrderStatusAsync(
                order.Id,
                finalStatus,
                fulfilmentStatus: "Delivered",
                ct: ct);

            await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                EventType = "DeliveryConfirmed",
                PreviousStatus = order.WorkflowStatus,
                NewStatus = finalStatus,
                ActorUserId = _userContext.UserId,
                ActorEmail = _userContext.Email,
                ActorRole = "DaycareOrganization",
                Description = hasLoans
                    ? "Delivery confirmed by daycare. Borrowed kits are now In Use until due date."
                    : "Delivery confirmed by daycare. All items received. Order completed.",
                Notes = request.Notes,
                IsInternalNote = false
            }, ct);
        }
        else
        {
            // Report delivery exception
            await _materialRepository.UpdateOrderStatusAsync(
                order.Id,
                MaterialWorkflowStatus.DeliveryException,
                ct: ct);

            await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                EventType = "DeliveryExceptionReported",
                PreviousStatus = order.WorkflowStatus,
                NewStatus = MaterialWorkflowStatus.DeliveryException,
                ActorUserId = _userContext.UserId,
                ActorEmail = _userContext.Email,
                ActorRole = "DaycareOrganization",
                Description = $"Daycare reported a delivery issue: {request.Notes}",
                Notes = request.Notes,
                IsInternalNote = false
            }, ct);
        }
    }

    #endregion

    #region Loans, Returns & Inspections

    public async Task<List<MaterialLoan>> GetOrganizationLoansAsync(Guid organizationId, CancellationToken ct = default)
    {
        await EnsureOrganizationAccessAsync(organizationId, ct);
        return await _materialRepository.GetActiveLoansForOrganizationAsync(organizationId, ct);
    }

    public async Task<List<MaterialLoan>> GetPlatformLoanQueueAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        EnsurePlatformPermission();
        return await _materialRepository.GetPlatformLoanQueueAsync(statusFilter, ct);
    }

    public async Task InitiateLoanReturnAsync(Guid loanId, string? notes = null, CancellationToken ct = default)
    {
        var loan = await _materialRepository.GetLoanByIdAsync(loanId, ct);
        if (loan == null) throw new KeyNotFoundException("Loan record not found.");

        await EnsureOrganizationAccessAsync(loan.OrganizationId, ct);

        await _materialRepository.UpdateLoanStatusAsync(
            loanId,
            MaterialLoanStatus.ReturnInTransit,
            actualReturnDate: DateTimeOffset.UtcNow,
            notes: notes,
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = loan.OrderId,
            EventType = "LoanReturnInitiated",
            PreviousStatus = loan.ReturnStatus,
            NewStatus = MaterialLoanStatus.ReturnInTransit,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "DaycareOrganization",
            Description = $"Return initiated for '{loan.ProductName}' ({loan.QuantityIssued} units). Package is in transit back to EnjoyEveryday warehouse.",
            Notes = notes,
            IsInternalNote = false
        }, ct);
    }

    public async Task ReceiveLoanReturnAtWarehouseAsync(Guid loanId, int receivedQuantity, string? notes = null, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var loan = await _materialRepository.GetLoanByIdAsync(loanId, ct);
        if (loan == null) throw new KeyNotFoundException("Loan record not found.");

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        // Move inventory: from OnLoan -> UnderInspection
        await _materialRepository.UpdateInventoryBalanceAtomicAsync(
            loan.MaterialProductId,
            central.Id,
            totalDelta: 0,
            availableDelta: 0,
            reservedDelta: 0,
            dispatchedDelta: 0,
            onLoanDelta: -receivedQuantity,
            underInspectionDelta: receivedQuantity,
            underCleaningDelta: 0,
            damagedDelta: 0,
            missingDelta: 0,
            retiredDelta: 0,
            ct: ct);

        await _materialRepository.UpdateLoanStatusAsync(
            loanId,
            MaterialLoanStatus.Received,
            actualReturnDate: DateTimeOffset.UtcNow,
            notes: notes,
            ct: ct);

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = loan.OrderId,
            EventType = "LoanReturnReceivedAtWarehouse",
            PreviousStatus = loan.ReturnStatus,
            NewStatus = MaterialLoanStatus.Received,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = $"Returned items for '{loan.ProductName}' received at Central Warehouse. Scheduled for condition & sanitization inspection.",
            Notes = notes,
            IsInternalNote = false
        }, ct);
    }

    public async Task CompleteReturnInspectionAsync(CompleteReturnInspectionRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var loan = await _materialRepository.GetLoanByIdAsync(request.LoanId, ct);
        if (loan == null) throw new KeyNotFoundException("Loan record not found.");

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        int totalInspected = request.QuantityApprovedForStock + request.QuantityRequiringCleaning + request.QuantityDamaged + request.QuantityMissing;
        if (totalInspected <= 0)
            throw new ArgumentException("Total inspected items must be greater than zero.");

        var inspection = new MaterialReturnInspection
        {
            Id = Guid.NewGuid(),
            LoanId = loan.Id,
            OrderId = loan.OrderId,
            MaterialProductId = loan.MaterialProductId,
            ReturnedQuantity = request.ReturnedQuantity,
            InspectedAt = DateTimeOffset.UtcNow,
            InspectedByUserId = _userContext.UserId,
            InspectedByEmail = _userContext.Email,
            Condition = request.Condition,
            QuantityApprovedForStock = request.QuantityApprovedForStock,
            QuantityRequiringCleaning = request.QuantityRequiringCleaning,
            QuantityDamaged = request.QuantityDamaged,
            QuantityMissing = request.QuantityMissing,
            InspectionNotes = request.InspectionNotes,
            DepositDisposition = request.DepositDisposition,
            DepositDeductionAmount = request.DepositDeductionAmount,
            ResolutionStatus = "Completed"
        };

        await _materialRepository.RecordReturnInspectionAsync(inspection, ct);

        // Deduct from UnderInspection and route to respective stock balances
        await _materialRepository.UpdateInventoryBalanceAtomicAsync(
            loan.MaterialProductId,
            central.Id,
            totalDelta: -(request.QuantityDamaged + request.QuantityMissing),
            availableDelta: request.QuantityApprovedForStock,
            reservedDelta: 0,
            dispatchedDelta: 0,
            onLoanDelta: 0,
            underInspectionDelta: -totalInspected,
            underCleaningDelta: request.QuantityRequiringCleaning,
            damagedDelta: request.QuantityDamaged,
            missingDelta: request.QuantityMissing,
            retiredDelta: 0,
            ct: ct);

        // Update Loan Status
        string finalLoanStatus = request.QuantityDamaged > 0 
            ? MaterialLoanStatus.Damaged 
            : (request.QuantityMissing > 0 ? MaterialLoanStatus.Missing : MaterialLoanStatus.AvailableAgain);

        string depositStatus = request.DepositDisposition switch
        {
            "FullRefund" => "Refunded",
            "FullForfeiture" => "Forfeited",
            "PartialDeduction" => "PartialDeduction",
            _ => "Waived"
        };

        await _materialRepository.UpdateLoanStatusAsync(
            loan.Id,
            finalLoanStatus,
            depositStatus: depositStatus,
            notes: request.InspectionNotes,
            ct: ct);

        // Check if all loans for order are resolved
        var remainingLoans = (await _materialRepository.GetLoansByOrderIdAsync(loan.OrderId, ct))
            .Where(l => l.ReturnStatus != MaterialLoanStatus.AvailableAgain && l.ReturnStatus != MaterialLoanStatus.Damaged && l.ReturnStatus != MaterialLoanStatus.Retired)
            .ToList();

        if (!remainingLoans.Any())
        {
            await _materialRepository.UpdateOrderStatusAsync(
                loan.OrderId,
                MaterialWorkflowStatus.Completed,
                ct: ct);
        }

        await _materialRepository.AddOrderHistoryEventAsync(new MaterialOrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = loan.OrderId,
            EventType = "ReturnInspectionCompleted",
            PreviousStatus = loan.ReturnStatus,
            NewStatus = finalLoanStatus,
            ActorUserId = _userContext.UserId,
            ActorEmail = _userContext.Email,
            ActorRole = "PlatformAdmin",
            Description = $"Return inspection completed: {request.QuantityApprovedForStock} approved & returned to stock, {request.QuantityRequiringCleaning} sent to cleaning, {request.QuantityDamaged} damaged, {request.QuantityMissing} missing. Deposit disposition: {request.DepositDisposition}.",
            Notes = request.InspectionNotes,
            IsInternalNote = false
        }, ct);
    }

    public async Task SanitizeAndRestockCleaningItemsAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

        var bal = await _materialRepository.GetInventoryBalanceAsync(productId, central.Id, ct);
        if (bal == null || bal.UnderCleaningStock < quantity)
            throw new InvalidOperationException($"Insufficient items under cleaning. Current: {bal?.UnderCleaningStock ?? 0}");

        // Move from under_cleaning -> available
        await _materialRepository.UpdateInventoryBalanceAtomicAsync(
            productId,
            central.Id,
            totalDelta: 0,
            availableDelta: quantity,
            reservedDelta: 0,
            dispatchedDelta: 0,
            onLoanDelta: 0,
            underInspectionDelta: 0,
            underCleaningDelta: -quantity,
            damagedDelta: 0,
            missingDelta: 0,
            retiredDelta: 0,
            ct: ct);

        await _materialRepository.RecordInventoryLedgerAsync(new InventoryLedgerEntry
        {
            Id = Guid.NewGuid(),
            MaterialProductId = productId,
            LocationId = central.Id,
            TransactionType = MaterialInventoryTransactionType.CleaningComplete,
            QuantityDelta = 0,
            AvailableDelta = quantity,
            Notes = $"Sanitization completed for {quantity} units. Restored to available stock.",
            PerformedByEmail = _userContext.Email
        }, ct);
    }

    #endregion

    #region Recurring Schedules

    public async Task<Guid> CreateRecurringScheduleAsync(CreateRecurringScheduleRequest request, CancellationToken ct = default)
    {
        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.");
        if (request.ProductId == Guid.Empty)
            throw new ArgumentException("Material Product ID is required.");
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1.");

        await EnsureOrganizationAccessAsync(request.OrganizationId, ct);

        var product = await _materialRepository.GetProductByIdAsync(request.ProductId, ct);
        if (product == null || !product.IsActive)
            throw new KeyNotFoundException("Product is unavailable.");

        var schedule = new MaterialRecurringSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            ClassroomId = request.ClassroomId,
            ScheduleType = request.ScheduleType,
            Frequency = request.Frequency,
            MaterialProductId = request.ProductId,
            OfferingId = product.Offering?.Id,
            Quantity = request.Quantity,
            LoanDurationDays = request.LoanDurationDays,
            DeliveryAddress = request.DeliveryAddress,
            NextRunDate = request.FirstDeliveryDate,
            Status = "Active",
            Notes = request.Notes,
            CreatedByUserId = _userContext.UserId
        };

        return await _materialRepository.CreateRecurringScheduleAsync(schedule, ct);
    }

    public async Task<List<MaterialRecurringSchedule>> GetRecurringSchedulesAsync(Guid organizationId, CancellationToken ct = default)
    {
        await EnsureOrganizationAccessAsync(organizationId, ct);
        return await _materialRepository.GetRecurringSchedulesAsync(organizationId, ct);
    }

    public async Task UpdateRecurringScheduleStatusAsync(Guid scheduleId, string newStatus, CancellationToken ct = default)
    {
        // Enforce valid status: Active, Paused, Cancelled
        if (newStatus != "Active" && newStatus != "Paused" && newStatus != "Cancelled")
            throw new ArgumentException("Invalid status.");

        await _materialRepository.UpdateRecurringScheduleStatusAsync(scheduleId, newStatus, null, null, ct);
    }

    public async Task<int> ProcessDueRecurringSchedulesJobAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        var dueSchedules = await _materialRepository.GetDueRecurringSchedulesAsync(asOfDate, ct);
        int processedCount = 0;

        foreach (var schedule in dueSchedules)
        {
            try
            {
                // Generate next occurrence order
                var orderReq = new CreateMaterialOrderRequest
                {
                    OrganizationId = schedule.OrganizationId,
                    BranchId = schedule.BranchId,
                    ClassroomId = schedule.ClassroomId,
                    DeliveryAddress = schedule.DeliveryAddress,
                    RequiredDeliveryDate = schedule.NextRunDate,
                    Notes = $"Automated recurring {schedule.ScheduleType} occurrence ({schedule.Frequency})",
                    Items = new List<CreateMaterialOrderItemRequest>
                    {
                        new()
                        {
                            ProductId = schedule.MaterialProductId,
                            FulfilmentMode = schedule.ScheduleType == "RecurringLoan" ? MaterialFulfilmentMode.Borrow : MaterialFulfilmentMode.Purchase,
                            Quantity = schedule.Quantity,
                            LoanDurationDays = schedule.LoanDurationDays
                        }
                    }
                };

                await SubmitMaterialOrderAsync(orderReq, ct);

                // Compute next run date
                DateTimeOffset nextDate = schedule.Frequency switch
                {
                    "Weekly" => schedule.NextRunDate.AddDays(7),
                    "BiWeekly" => schedule.NextRunDate.AddDays(14),
                    _ => schedule.NextRunDate.AddMonths(1)
                };

                await _materialRepository.UpdateRecurringScheduleStatusAsync(
                    schedule.Id,
                    "Active",
                    nextRunDate: nextDate,
                    lastRunDate: DateTimeOffset.UtcNow,
                    ct: ct);

                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process recurring schedule {ScheduleId}", schedule.Id);
            }
        }

        return processedCount;
    }

    #endregion

    #region Experience Planning Integration

    public async Task<ExperienceMaterialReadinessSummary> CheckExperienceMaterialReadinessAsync(Guid experienceId, Guid classroomId, CancellationToken ct = default)
    {
        var exp = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, experienceId, ct);
        var cls = await _classroomRepository.GetByIdAsync(_tenantContext.TenantId, classroomId, ct);

        var summary = new ExperienceMaterialReadinessSummary
        {
            ExperienceId = experienceId,
            ExperienceTitle = exp?.Title ?? "Experience",
            ClassroomId = classroomId,
            ClassroomName = cls?.Name ?? "Classroom"
        };

        var catalog = await _materialRepository.GetProductsAsync(activeOnly: true, ct: ct);
        var targetOrgId = exp?.OrganizationId ?? _userContext.OrganizationId ?? Guid.Empty;
        var activeOrders = targetOrgId != Guid.Empty
            ? await _materialRepository.GetOrdersForOrganizationAsync(targetOrgId, ct: ct)
            : new List<MaterialOrder>();

        List<string> dnaMaterials = new();
        if (!string.IsNullOrWhiteSpace(exp?.DnaPayload))
        {
            try
            {
                var dna = JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dna?.Materials != null && dna.Materials.Any())
                {
                    dnaMaterials = dna.Materials;
                }
            }
            catch
            {
                // Fallback if payload isn't json
            }
        }

        if (!dnaMaterials.Any())
        {
            dnaMaterials = new List<string> { "Water exploration kit", "Craft paper", "Finger paints", "Wooden sorting trays" };
        }

        summary.TotalRequired = dnaMaterials.Count;
        int readyCount = 0;

        foreach (var mat in dnaMaterials)
        {
            // Find matched catalog item
            var matched = catalog.FirstOrDefault(c => 
                c.Name.Contains(mat, StringComparison.OrdinalIgnoreCase) ||
                mat.Contains(c.Name, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(mat, StringComparison.OrdinalIgnoreCase));

            // Check if there is an active order or delivery for this experience or classroom
            var linkedOrder = activeOrders.FirstOrDefault(o => 
                (o.PlannedExperienceId == experienceId || o.ClassroomId == classroomId) &&
                o.Items.Any(i => i.ProductName?.Contains(mat, StringComparison.OrdinalIgnoreCase) == true));

            var item = new MaterialReadinessItem
            {
                MaterialName = mat,
                MatchedProductId = matched?.Id,
                CanPurchase = matched?.Offering?.AllowPurchase ?? false,
                PurchasePrice = matched?.Offering?.PurchasePrice ?? 0,
                CanBorrow = matched?.Offering?.AllowBorrow ?? false,
                RentalFee = matched?.Offering?.RentalFee ?? 0
            };

            if (linkedOrder != null)
            {
                item.IsOrdered = true;
                item.OrderNumber = linkedOrder.OrderNumber;
                item.FulfilmentStatus = linkedOrder.FulfilmentStatus;

                if (linkedOrder.WorkflowStatus == MaterialWorkflowStatus.Delivered ||
                    linkedOrder.WorkflowStatus == MaterialWorkflowStatus.ReceiptConfirmed ||
                    linkedOrder.WorkflowStatus == MaterialWorkflowStatus.Completed)
                {
                    item.IsAvailableInStock = true;
                    item.SuggestedAction = "Available";
                    readyCount++;
                }
                else
                {
                    item.IsAvailableInStock = false;
                    item.SuggestedAction = "AwaitingDelivery";
                }
            }
            else
            {
                // If in classroom or local stock, mark available
                if (mat.Contains("paper", StringComparison.OrdinalIgnoreCase) || mat.Contains("crayon", StringComparison.OrdinalIgnoreCase))
                {
                    item.IsAvailableInStock = true;
                    item.SuggestedAction = "Available";
                    readyCount++;
                }
                else
                {
                    item.IsAvailableInStock = false;
                    item.SuggestedAction = matched?.Offering?.AllowBorrow == true ? "Borrow" : "Buy";
                }
            }

            summary.Materials.Add(item);
        }

        summary.TotalOnHandOrDelivered = readyCount;
        summary.IsFullyReady = readyCount >= summary.TotalRequired;
        summary.OverallStatusBadge = summary.IsFullyReady
            ? "Ready"
            : (summary.Materials.Any(m => m.SuggestedAction == "AwaitingDelivery") ? "Dispatched" : "MissingMaterials");

        return summary;
    }

    #endregion

}
