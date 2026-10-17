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

public class MaterialMarketplaceService : IMaterialMarketplaceService
{
    private readonly IMaterialMarketplaceRepository _materialRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IRazorpayGateway _razorpayGateway;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<MaterialMarketplaceService> _logger;

    public MaterialMarketplaceService(
        IMaterialMarketplaceRepository materialRepository,
        IOrganizationRepository organizationRepository,
        IExperienceRepository experienceRepository,
        IClassroomRepository classroomRepository,
        IBillingRepository billingRepository,
        IRazorpayGateway razorpayGateway,
        IResourceAuthorizer authorizer,
        IUserContext userContext,
        ITenantContext tenantContext,
        IAuditService auditService,
        ILogger<MaterialMarketplaceService> logger)
    {
        _materialRepository = materialRepository;
        _organizationRepository = organizationRepository;
        _experienceRepository = experienceRepository;
        _classroomRepository = classroomRepository;
        _billingRepository = billingRepository;
        _razorpayGateway = razorpayGateway;
        _authorizer = authorizer;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _logger = logger;
    }

    #region Catalog & Warehouse

    public async Task<List<MaterialProduct>> GetCatalogProductsAsync(string? category = null, string? materialType = null, bool activeOnly = true, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductsAsync(category, materialType, activeOnly, ct);
    }

    public async Task<MaterialProduct?> GetProductDetailsAsync(Guid productId, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductByIdAsync(productId, ct);
    }

    public async Task<Guid> SaveProductAsync(SaveMaterialProductRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product name is required.");
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new ArgumentException("Product SKU is required.");

        var existingBySku = await _materialRepository.GetProductBySkuAsync(request.Sku, ct);
        if (existingBySku != null && (!request.Id.HasValue || existingBySku.Id != request.Id.Value))
        {
            throw new InvalidOperationException($"A product with SKU '{request.Sku}' already exists.");
        }

        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            var existing = await _materialRepository.GetProductByIdAsync(request.Id.Value, ct);
            if (existing == null) throw new KeyNotFoundException($"Product with ID {request.Id.Value} not found.");

            existing.Sku = request.Sku.Trim().ToUpperInvariant();
            existing.Name = request.Name.Trim();
            existing.Description = request.Description.Trim();
            existing.Category = request.Category;
            existing.ImageUrl = request.ImageUrl;
            existing.MaterialType = request.MaterialType;
            existing.SafetyInstructions = request.SafetyInstructions;
            existing.AgeMinMonths = request.AgeMinMonths;
            existing.AgeMaxMonths = request.AgeMaxMonths;
            existing.UsageRestrictions = request.UsageRestrictions;
            existing.CleaningInstructions = request.CleaningInstructions;
            existing.ReturnPolicy = request.ReturnPolicy;
            existing.IsActive = request.IsActive;

            var offering = existing.Offering ?? new MaterialOffering { MaterialProductId = existing.Id };
            offering.AllowPurchase = request.AllowPurchase;
            offering.PurchasePrice = request.PurchasePrice;
            offering.AllowBorrow = request.AllowBorrow;
            offering.RentalFee = request.RentalFee;
            offering.RefundableDeposit = request.RefundableDeposit;
            offering.DefaultLoanDurationDays = request.DefaultLoanDurationDays;
            offering.MaxLoanDurationDays = request.MaxLoanDurationDays;
            offering.IsActive = request.IsActive;

            await _materialRepository.UpdateProductAsync(existing, offering, ct);
            return existing.Id;
        }
        else
        {
            var newProd = new MaterialProduct
            {
                Id = Guid.NewGuid(),
                Sku = request.Sku.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Category = request.Category,
                ImageUrl = request.ImageUrl,
                MaterialType = request.MaterialType,
                SafetyInstructions = request.SafetyInstructions,
                AgeMinMonths = request.AgeMinMonths,
                AgeMaxMonths = request.AgeMaxMonths,
                UsageRestrictions = request.UsageRestrictions,
                CleaningInstructions = request.CleaningInstructions,
                ReturnPolicy = request.ReturnPolicy,
                IsActive = request.IsActive
            };

            var newOffering = new MaterialOffering
            {
                Id = Guid.NewGuid(),
                MaterialProductId = newProd.Id,
                AllowPurchase = request.AllowPurchase,
                PurchasePrice = request.PurchasePrice,
                AllowBorrow = request.AllowBorrow,
                RentalFee = request.RentalFee,
                RefundableDeposit = request.RefundableDeposit,
                DefaultLoanDurationDays = request.DefaultLoanDurationDays,
                MaxLoanDurationDays = request.MaxLoanDurationDays,
                Currency = "INR",
                IsActive = request.IsActive
            };

            var id = await _materialRepository.CreateProductAsync(newProd, newOffering, ct);

            // Initialize central warehouse balance
            var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
            if (central != null)
            {
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    id, central.Id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ct);
            }

            return id;
        }
    }

    public async Task<List<InventoryBalance>> GetWarehouseInventoryBalancesAsync(Guid? locationId = null, CancellationToken ct = default)
    {
        return await _materialRepository.GetInventoryBalancesAsync(locationId, ct);
    }

    public async Task<List<InventoryLocation>> GetWarehouseLocationsAsync(CancellationToken ct = default)
    {
        return await _materialRepository.GetLocationsAsync(ct);
    }

    public async Task<List<InventoryLedgerEntry>> GetProductLedgerHistoryAsync(Guid productId, CancellationToken ct = default)
    {
        return await _materialRepository.GetProductLedgerAsync(productId, 50, ct);
    }

    public async Task AdjustInventoryStockAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        EnsurePlatformPermission();

        if (request.QuantityDelta == 0)
            throw new ArgumentException("Quantity delta cannot be zero.");

        var balance = await _materialRepository.GetInventoryBalanceAsync(request.ProductId, request.LocationId, ct);
        if (balance == null)
            throw new KeyNotFoundException("Inventory balance record not found for this product and location.");

        if (balance.AvailableStock + request.QuantityDelta < 0)
            throw new InvalidOperationException($"Cannot reduce stock below zero. Current available: {balance.AvailableStock}");

        // Atomic balance update
        await _materialRepository.UpdateInventoryBalanceAtomicAsync(
            request.ProductId,
            request.LocationId,
            totalDelta: request.QuantityDelta,
            availableDelta: request.QuantityDelta,
            reservedDelta: 0,
            dispatchedDelta: 0,
            onLoanDelta: 0,
            underInspectionDelta: 0,
            underCleaningDelta: 0,
            damagedDelta: 0,
            missingDelta: 0,
            retiredDelta: 0,
            ct: ct);

        // Record Ledger
        var ledger = new InventoryLedgerEntry
        {
            Id = Guid.NewGuid(),
            MaterialProductId = request.ProductId,
            LocationId = request.LocationId,
            TransactionType = request.TransactionType,
            QuantityDelta = request.QuantityDelta,
            AvailableDelta = request.QuantityDelta,
            Notes = request.Notes ?? "Manual inventory adjustment",
            PerformedByUserId = _userContext.UserId,
            PerformedByEmail = _userContext.Email
        };
        await _materialRepository.RecordInventoryLedgerAsync(ledger, ct);
    }

    #endregion

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
                    requiresApproval = true; // Out of stock -> requires quotation / review
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

        if (order.WorkflowStatus == MaterialWorkflowStatus.Dispatched ||
            order.WorkflowStatus == MaterialWorkflowStatus.Delivered ||
            order.WorkflowStatus == MaterialWorkflowStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel an order that has already been dispatched or delivered.");
        }

        // Release any reserved stock
        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central != null && (order.WorkflowStatus == MaterialWorkflowStatus.Confirmed || order.WorkflowStatus == MaterialWorkflowStatus.StockAllocated))
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

        if (string.IsNullOrWhiteSpace(request.CarrierName))
            throw new ArgumentException("Carrier name is required.");
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
            throw new ArgumentException("Tracking number / reference is required.");

        var central = await _materialRepository.GetCentralWarehouseLocationAsync(ct);
        if (central == null) throw new InvalidOperationException("No central warehouse location found.");

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

        // Move inventory: from Reserved -> Dispatched
        var loansToCreate = new List<MaterialLoan>();

        foreach (var item in order.Items)
        {
            if (item.FulfilmentMode == MaterialFulfilmentMode.Purchase)
            {
                // Purchased items: deduct from warehouse total & reserved stock, mark dispatched
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    central.Id,
                    totalDelta: -item.Quantity,
                    availableDelta: 0,
                    reservedDelta: -item.Quantity,
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
                    AvailableDelta = 0,
                    ReservedDelta = -item.Quantity,
                    ReferenceType = "Shipment",
                    ReferenceId = shipment.Id,
                    Notes = $"Dispatched purchase on {shipment.ShipmentNumber}",
                    PerformedByEmail = _userContext.Email
                }, ct);
            }
            else // Borrow
            {
                // Borrowed items: keep in total stock, move from reserved -> dispatched
                await _materialRepository.UpdateInventoryBalanceAtomicAsync(
                    item.MaterialProductId,
                    central.Id,
                    totalDelta: 0,
                    availableDelta: 0,
                    reservedDelta: -item.Quantity,
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
                    AvailableDelta = 0,
                    ReservedDelta = -item.Quantity,
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

    #region Security Helpers

    private bool IsPlatformAdmin()
    {
        return (_userContext.Roles != null && _userContext.Roles.Any(r => 
                    r.Equals(RoleTemplates.PlatformAdmin, StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("Superadmin", StringComparison.OrdinalIgnoreCase))) ||
               _userContext.HasPermission(Permissions.PlatformWarehouseManage) ||
               _userContext.HasPermission(Permissions.PlatformBillingManage);
    }

    private void EnsurePlatformPermission()
    {
        if (!IsPlatformAdmin())
        {
            throw new UnauthorizedAccessException("This action is restricted to Platform Operations administrators.");
        }
    }

    private async Task EnsureOrganizationAccessAsync(Guid organizationId, CancellationToken ct)
    {
        if (IsPlatformAdmin()) return;

        var userOrgId = _userContext.OrganizationId;
        if (userOrgId.HasValue && userOrgId.Value == organizationId) return;

        var org = await _organizationRepository.GetByIdAsync(organizationId, ct);
        if (org == null)
            throw new KeyNotFoundException("Organization not found.");

        if (org.TenantId != _tenantContext.TenantId)
        {
            throw new UnauthorizedAccessException("Cross-tenant access is strictly denied.");
        }

        if (!_userContext.HasPermission(Permissions.MaterialsView))
        {
            throw new UnauthorizedAccessException("You do not have authorization for this organization's materials and orders.");
        }
    }

    #endregion
}
