using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class ClassroomGiftingService : IClassroomGiftingService
{
    private readonly IClassroomGiftingRepository _giftingRepository;
    private readonly IMaterialMarketplaceRepository _materialRepository;
    private readonly IFacilityProfileService _facilityProfileService;
    private readonly IBranchRepository _branchRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrgFeeRepository _feeRepository;
    private readonly IRazorpayGateway? _razorpayGateway;
    private readonly ICredentialProtector _credentialProtector;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly IPlatformBusinessEventService? _eventService;
    private readonly IEmailNotificationService? _emailService;
    private readonly ILogger<ClassroomGiftingService> _logger;

    public ClassroomGiftingService(
        IClassroomGiftingRepository giftingRepository,
        IMaterialMarketplaceRepository materialRepository,
        IFacilityProfileService facilityProfileService,
        IBranchRepository branchRepository,
        IOrganizationRepository organizationRepository,
        IOrgFeeRepository feeRepository,
        ICredentialProtector credentialProtector,
        IUserContext userContext,
        ITenantContext tenantContext,
        ILogger<ClassroomGiftingService> logger,
        IRazorpayGateway? razorpayGateway = null,
        IPlatformBusinessEventService? eventService = null,
        IEmailNotificationService? emailService = null)
    {
        _giftingRepository = giftingRepository;
        _materialRepository = materialRepository;
        _facilityProfileService = facilityProfileService;
        _branchRepository = branchRepository;
        _organizationRepository = organizationRepository;
        _feeRepository = feeRepository;
        _credentialProtector = credentialProtector;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
        _razorpayGateway = razorpayGateway;
        _eventService = eventService;
        _emailService = emailService;
    }

    public async Task<List<GiftingPeerChildDto>> GetClassroomPeersForGiftingAsync(
        Guid classroomId,
        Guid? excludeChildId = null,
        CancellationToken ct = default)
    {
        return await _giftingRepository.GetClassroomPeersForGiftingAsync(_tenantContext.TenantId, classroomId, excludeChildId, ct);
    }

    public async Task<List<MaterialProduct>> GetGiftingCatalogProductsAsync(
        string? category = null,
        CancellationToken ct = default)
    {
        // Reuses material products from existing marketplace with purchase offering
        var products = await _materialRepository.GetProductsAsync(category, null, activeOnly: true, ct);
        return products.Where(p => p.Offering != null && p.Offering.AllowPurchase && p.Offering.PurchasePrice > 0).ToList();
    }

    public async Task<GiftingDeliveryDestinationOptions> GetDeliveryDestinationsAsync(
        Guid branchId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var options = new GiftingDeliveryDestinationOptions();

        // 1. Branch address
        var branchProfile = await _facilityProfileService.GetExactProfileAsync(organizationId, branchId, ct);
        var branch = await _branchRepository.GetByIdAsync(_tenantContext.TenantId, branchId, ct);

        options.BranchName = branchProfile?.Title ?? branch?.Name ?? "Child's Campus Branch";
        if (branchProfile != null && !string.IsNullOrWhiteSpace(branchProfile.StreetAddress))
        {
            var parts = new[] { branchProfile.StreetAddress, branchProfile.City, branchProfile.State, branchProfile.PostalCode, branchProfile.Country }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            options.BranchFullAddress = string.Join(", ", parts);
            options.BranchContactPhone = branchProfile.Phone ?? string.Empty;
        }
        else
        {
            options.BranchFullAddress = branch?.Location ?? "Branch registered facility address";
            options.BranchContactPhone = string.Empty;
        }

        // 2. Organization address
        var orgProfile = await _facilityProfileService.GetExactProfileAsync(organizationId, null, ct);
        var org = await _organizationRepository.GetByIdAsync(organizationId, ct);

        options.OrganizationName = orgProfile?.Title ?? org?.Name ?? "Organization Registered Head Office";
        if (orgProfile != null && !string.IsNullOrWhiteSpace(orgProfile.StreetAddress))
        {
            var parts = new[] { orgProfile.StreetAddress, orgProfile.City, orgProfile.State, orgProfile.PostalCode, orgProfile.Country }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            options.OrganizationFullAddress = string.Join(", ", parts);
            options.OrganizationContactPhone = orgProfile.Phone ?? string.Empty;
        }
        else
        {
            options.OrganizationFullAddress = org?.Location ?? "Organization registered address";
            options.OrganizationContactPhone = string.Empty;
        }

        return options;
    }

    public async Task<ClassroomGiftingOrder> SubmitOrderAsync(
        SubmitGiftingOrderRequest request,
        CancellationToken ct = default)
    {
        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("At least one product item is required for a gift order.");

        if (string.IsNullOrWhiteSpace(request.ConfirmedDeliveryAddress))
            throw new ArgumentException("A confirmed facility delivery destination address is mandatory.");

        // Load and validate products & price
        var orderItems = new List<ClassroomGiftingOrderItem>();
        decimal calculatedTotal = 0m;
        Guid orderId = Guid.NewGuid();
        string orderNumber = $"GIFT-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}";

        foreach (var reqItem in request.Items)
        {
            if (reqItem.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            var product = await _materialRepository.GetProductByIdAsync(reqItem.MaterialProductId, ct);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException($"Product {reqItem.MaterialProductId} is inactive or not found.");

            var offering = product.Offering;
            if (offering == null || !offering.AllowPurchase || offering.PurchasePrice <= 0)
                throw new InvalidOperationException($"Product '{product.Name}' is not available for purchase.");

            var availStock = product.StockBalance?.AvailableStock ?? 0;
            if (availStock < reqItem.Quantity)
            {
                if (availStock <= 0)
                {
                    throw new InvalidOperationException($"Someone has already ordered '{product.Name}' and it is now out of stock. Please remove it from your gift selection.");
                }
                else
                {
                    throw new InvalidOperationException($"Someone has already ordered '{product.Name}'. Only {availStock} item(s) remain in stock (you requested {reqItem.Quantity}). Please adjust your gift quantity.");
                }
            }

            decimal lineTotal = offering.PurchasePrice * reqItem.Quantity;
            calculatedTotal += lineTotal;

            orderItems.Add(new ClassroomGiftingOrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                MaterialProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                Quantity = reqItem.Quantity,
                UnitPrice = offering.PurchasePrice,
                LineTotal = lineTotal
            });
        }

        var order = new ClassroomGiftingOrder
        {
            Id = orderId,
            OrderNumber = orderNumber,
            TenantId = _tenantContext.TenantId,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            ClassroomId = request.ClassroomId,
            ParentUserId = _userContext.UserId,
            ParentEmail = _userContext.Email,
            ParentName = _userContext.Email ?? "Parent",
            ChildId = request.ChildId,
            RecipientType = request.RecipientType,
            RecipientChildId = request.RecipientChildId,
            RecipientChildName = request.RecipientChildName,
            GreetingMessage = request.GreetingMessage,
            DeliveryDestinationType = request.DeliveryDestinationType,
            ConfirmedDeliveryAddress = request.ConfirmedDeliveryAddress,
            ConfirmedContactName = request.ConfirmedContactName,
            ConfirmedContactPhone = request.ConfirmedContactPhone,
            TotalAmount = calculatedTotal,
            Currency = "INR",
            ApprovalStatus = GiftingApprovalStatuses.PendingApproval,
            PaymentStatus = GiftingPaymentStatuses.AwaitingPayment,
            FulfilmentStatus = GiftingFulfilmentStatuses.NotStarted,
            Items = orderItems
        };

        var result = await _giftingRepository.SubmitOrderAsync(order, orderItems, ct);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        // Notify Admins
        await NotifyOrderSubmittedAsync(order, ct);

        return (await _giftingRepository.GetOrderByIdAsync(orderId, ct))!;
    }

    public async Task<(bool Success, string Message)> ApproveOrRejectOrderAsync(
        Guid orderId,
        string decision,
        string? reason = null,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null)
            return (false, "Gifting order not found.");

        string actorRole = _userContext.Roles.Contains("OrganizationAdmin") || _userContext.Roles.Contains("OrganizationOwner")
            ? "OrganizationAdmin"
            : "BranchAdmin";

        var result = await _giftingRepository.ApproveOrRejectOrderAsync(
            orderId,
            _userContext.UserId,
            _userContext.Email ?? "Admin",
            actorRole,
            decision,
            reason,
            ct);

        if (result.Success)
        {
            // Publish event and notify Parent
            await NotifyOrderDecisionAsync(order, decision, reason, ct);
        }

        return (result.Success, result.Message);
    }

    public async Task<GiftingPaymentMethodsConfig> GetPaymentMethodsConfigAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var config = new GiftingPaymentMethodsConfig();

        // 1. Razorpay
        var rzpSettings = await _feeRepository.GetPaymentGatewaySettingsAsync(_tenantContext.TenantId, organizationId, ct);
        if (rzpSettings != null && rzpSettings.HasRazorpayAccount && rzpSettings.IsActive && !string.IsNullOrWhiteSpace(rzpSettings.KeyId))
        {
            config.HasRazorpay = true;
            config.RazorpayKeyId = rzpSettings.KeyId;
            config.RazorpayEnvironment = rzpSettings.Environment;
        }

        // 2. UPI QR Codes
        var qrCodes = await _feeRepository.GetQrCodesAsync(_tenantContext.TenantId, organizationId, ct);
        var activeQr = qrCodes.FirstOrDefault(q => q.IsActive && !string.IsNullOrWhiteSpace(q.QrImageDataUri));
        if (activeQr != null)
        {
            config.HasActiveQrCode = true;
            config.QrDisplayName = activeQr.DisplayName;
            config.QrImageDataUri = activeQr.QrImageDataUri;
            config.QrInstructions = activeQr.Instructions;
        }

        // 3. Bank Account
        var bank = await _feeRepository.GetBankAccountAsync(_tenantContext.TenantId, organizationId, ct);
        if (bank != null && !string.IsNullOrWhiteSpace(bank.AccountNumber))
        {
            config.HasBankAccount = true;
            config.AccountHolderName = bank.AccountHolderName;
            config.BankName = bank.BankName;
            config.BranchName = bank.BranchName;
            config.AccountNumber = bank.AccountNumber;
            config.IfscCode = bank.IfscCode;
            config.BankInstructions = bank.AdditionalInstructions;
            config.AllowCash = bank.AllowParentCashDeclaration;
        }

        return config;
    }

    public async Task<GiftingRazorpayOrderResult> CreateRazorpayOrderAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null)
            throw new KeyNotFoundException("Gifting order not found.");

        if (order.ApprovalStatus != GiftingApprovalStatuses.Approved)
            throw new InvalidOperationException("Payment can only be processed after admin approval.");

        if (order.PaymentStatus == GiftingPaymentStatuses.Paid)
            throw new InvalidOperationException("This order is already fully paid.");

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(order.TenantId, order.OrganizationId, ct);
        if (settings == null || !settings.HasRazorpayAccount || !settings.IsActive || string.IsNullOrWhiteSpace(settings.KeyId))
            throw new InvalidOperationException("This organization has not configured an active Razorpay gateway.");

        if (_razorpayGateway == null)
            throw new InvalidOperationException("Razorpay gateway client is unavailable.");

        string plainSecret = _credentialProtector.Unprotect(settings.EncryptedKeySecret ?? string.Empty);
        long amountInPaise = (long)Math.Round(order.TotalAmount * 100m, MidpointRounding.AwayFromZero);
        string currency = string.IsNullOrWhiteSpace(order.Currency) ? "INR" : order.Currency.ToUpperInvariant();

        var notes = new Dictionary<string, string>
        {
            ["billing_domain"] = "ClassroomGifting",
            ["order_id"] = order.Id.ToString(),
            ["order_number"] = order.OrderNumber,
            ["organization_id"] = order.OrganizationId.ToString(),
            ["tenant_id"] = order.TenantId.ToString(),
            ["parent_user_id"] = order.ParentUserId.ToString()
        };

        var rzpOrder = await _razorpayGateway.CreateOrderWithCredentialsAsync(
            settings.KeyId,
            plainSecret,
            amountInPaise,
            currency,
            order.OrderNumber,
            notes,
            ct);

        var org = await _organizationRepository.GetByIdAsync(order.OrganizationId, ct);

        return new GiftingRazorpayOrderResult
        {
            OrderId = rzpOrder.OrderId,
            KeyId = settings.KeyId,
            Amount = order.TotalAmount,
            AmountInPaise = amountInPaise,
            Currency = currency,
            GiftingOrderNumber = order.OrderNumber,
            OrganizationName = org?.Name ?? "Daycare Organization",
            Environment = settings.Environment,
            PrefillEmail = order.ParentEmail ?? _userContext.Email ?? string.Empty
        };
    }

    public async Task<(bool Success, string Message)> ConfirmRazorpayPaymentAsync(
        ConfirmGiftingRazorpayRequest request,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return (false, "Gifting order not found.");

        var settings = await _feeRepository.GetPaymentGatewaySettingsAsync(order.TenantId, order.OrganizationId, ct);
        if (settings == null || string.IsNullOrWhiteSpace(settings.KeyId))
            return (false, "Organization Razorpay credentials not found.");

        if (_razorpayGateway == null)
            return (false, "Razorpay gateway service unavailable.");

        string plainSecret = _credentialProtector.Unprotect(settings.EncryptedKeySecret ?? string.Empty);

        bool isValid = _razorpayGateway.VerifyPaymentSignatureWithSecret(
            request.RazorpayOrderId,
            request.RazorpayPaymentId,
            request.RazorpaySignature,
            settings.KeyId,
            plainSecret);

        if (!isValid)
        {
            _logger.LogWarning("Invalid Razorpay payment signature for gifting order {OrderNumber}", order.OrderNumber);
            return (false, "Payment signature validation failed.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
            OrganizationId = order.OrganizationId,
            BranchId = order.BranchId,
            ChildId = order.ChildId,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Method = "Razorpay",
            TransactionId = request.RazorpayPaymentId,
            ProviderOrderId = request.RazorpayOrderId,
            ProviderPaymentId = request.RazorpayPaymentId,
            ProviderSignature = request.RazorpaySignature,
            PaymentReference = request.RazorpayPaymentId,
            ReceiptNumber = $"RCP-GIFT-{DateTime.UtcNow:yyyyMMdd}-{order.OrderNumber}",
            Notes = $"Online Razorpay payment for gifting order {order.OrderNumber}"
        };

        var res = await _giftingRepository.RecordOnlineRazorpayPaymentAsync(payment, order.Id, ct);
        if (res.Success)
        {
            await NotifyPaymentConfirmedAsync(order, "Razorpay", order.TotalAmount, ct);
        }

        return (res.Success, res.Message);
    }

    public async Task<(bool Success, string Message)> SubmitManualPaymentAsync(
        SubmitGiftingManualPaymentRequest request,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return (false, "Gifting order not found.");

        if (order.ApprovalStatus != GiftingApprovalStatuses.Approved)
            return (false, "Payment cannot be submitted for an unapproved order.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
            OrganizationId = order.OrganizationId,
            BranchId = order.BranchId,
            ChildId = order.ChildId,
            Amount = request.Amount > 0 ? request.Amount : order.TotalAmount,
            Currency = order.Currency,
            Method = request.Method,
            TransactionId = request.TransactionId,
            PaymentReference = request.TransactionId ?? $"SUBMIT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}",
            ProofStorageKey = request.ProofStorageKey,
            Notes = request.Remarks,
            RecordedByUserId = _userContext.UserId,
            RecordedByEmail = _userContext.Email
        };

        await _giftingRepository.SubmitManualPaymentDeclarationAsync(payment, order.Id, ct);
        await NotifyPaymentSubmittedForVerificationAsync(order, request.Method, payment.Amount, ct);

        return (true, "Payment submitted successfully. Awaiting daycare staff verification.");
    }

    public async Task<(bool Success, string Message, string ReceiptNumber)> RecordCashPaymentReceivedAsync(
        RecordCashReceivedRequest request,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return (false, "Order not found.", string.Empty);

        if (order.ApprovalStatus != GiftingApprovalStatuses.Approved)
            return (false, "Order must be approved before receiving cash.", string.Empty);

        if (request.AmountReceived <= 0)
            return (false, "Amount received must be greater than zero.", string.Empty);

        string receiptNumber = $"RCP-CASH-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        Guid paymentId = Guid.NewGuid();

        var result = await _giftingRepository.RecordCashPaymentAsync(
            paymentId,
            order.Id,
            request.AmountReceived,
            _userContext.UserId,
            _userContext.Email ?? "staff@enjoyeveryday.com",
            _userContext.Email ?? "Staff",
            receiptNumber,
            request.Notes,
            ct);

        if (result.Success)
        {
            await NotifyCashReceivedAsync(order, request.AmountReceived, receiptNumber, ct);
        }

        return (result.Success, result.Message, result.ReceiptNumber);
    }

    public async Task<(bool Success, string Message)> VerifyManualPaymentAsync(
        Guid paymentId,
        string? remarks = null,
        CancellationToken ct = default)
    {
        string actorRole = _userContext.Roles.Contains("OrganizationAdmin") || _userContext.Roles.Contains("OrganizationOwner")
            ? "OrganizationAdmin"
            : "BranchAdmin";

        var result = await _giftingRepository.VerifyPaymentAsync(
            paymentId,
            _userContext.UserId,
            _userContext.Email ?? "Admin",
            actorRole,
            remarks,
            ct);

        if (result.Success && result.OrderId.HasValue)
        {
            var order = await _giftingRepository.GetOrderByIdAsync(result.OrderId.Value, ct);
            if (order != null)
            {
                await NotifyPaymentConfirmedAsync(order, "Manual Transfer", order.TotalAmount, ct);
            }
        }

        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> RejectManualPaymentAsync(
        Guid paymentId,
        string reason,
        CancellationToken ct = default)
    {
        string actorRole = _userContext.Roles.Contains("OrganizationAdmin") || _userContext.Roles.Contains("OrganizationOwner")
            ? "OrganizationAdmin"
            : "BranchAdmin";

        var result = await _giftingRepository.RejectPaymentAsync(
            paymentId,
            _userContext.UserId,
            _userContext.Email ?? "Admin",
            actorRole,
            reason,
            ct);

        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> UpdateFulfilmentStatusAsync(
        Guid orderId,
        string targetStatus,
        string? notes = null,
        CancellationToken ct = default)
    {
        string actorRole = "Staff";
        if (_userContext.Roles.Contains("OrganizationAdmin") || _userContext.Roles.Contains("OrganizationOwner"))
            actorRole = "OrganizationAdmin";
        else if (_userContext.Roles.Contains("BranchAdmin"))
            actorRole = "BranchAdmin";
        else if (_userContext.Roles.Contains("LeadTeacher") || _userContext.Roles.Contains("DaycareTeacher") || _userContext.Roles.Contains("Teacher"))
            actorRole = "Teacher";

        var result = await _giftingRepository.UpdateFulfilmentStatusAsync(
            orderId,
            targetStatus,
            _userContext.UserId,
            _userContext.Email ?? "Staff",
            actorRole,
            notes,
            ct);

        if (result.Success)
        {
            var order = await _giftingRepository.GetOrderByIdAsync(orderId, ct);
            if (order != null)
            {
                await NotifyFulfilmentMilestoneAsync(order, targetStatus, ct);
            }
        }

        return (result.Success, result.Message);
    }

    public async Task<List<ClassroomGiftingOrder>> GetOrdersForAdminAsync(
        Guid organizationId,
        Guid? branchId = null,
        Guid? classroomId = null,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        return await _giftingRepository.GetOrdersForAdminAsync(
            _tenantContext.TenantId,
            organizationId,
            branchId,
            classroomId,
            statusFilter,
            ct);
    }

    public async Task<List<ClassroomGiftingOrder>> GetOrdersForParentAsync(
        CancellationToken ct = default)
    {
        return await _giftingRepository.GetOrdersForParentAsync(
            _tenantContext.TenantId,
            _userContext.UserId,
            ct);
    }

    public async Task<ClassroomGiftingOrder?> GetOrderDetailsAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _giftingRepository.GetOrderByIdAsync(orderId, ct);
        if (order == null) return null;

        order.Items = await _giftingRepository.GetOrderItemsAsync(orderId, ct);
        order.AuditEvents = await _giftingRepository.GetAuditEventsAsync(orderId, ct);
        order.Payments = await _giftingRepository.GetPaymentsForOrderAsync(orderId, ct);

        return order;
    }

    public async Task<GiftingFinancialReconciliationReport> GetFinancialReconciliationAsync(
        Guid organizationId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        return await _giftingRepository.GetFinancialReconciliationAsync(
            _tenantContext.TenantId,
            organizationId,
            branchId,
            ct);
    }

    #region Notifications & Business Events

    private async Task NotifyOrderSubmittedAsync(ClassroomGiftingOrder order, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "ClassroomGiftingOrderSubmitted",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = order.ParentUserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"New Classroom Gifting Order: {order.OrderNumber}",
                    Description = $"Parent {order.ParentName} submitted order {order.OrderNumber} ({order.RecipientType}) for amount ₹{order.TotalAmount:N2}.",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    Status = order.ApprovalStatus,
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, order.RecipientType, order.TotalAmount }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish business event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task NotifyOrderDecisionAsync(ClassroomGiftingOrder order, string decision, string? reason, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = decision == "Approved" ? "ClassroomGiftingOrderApproved" : "ClassroomGiftingOrderRejected",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"Gifting Order {order.OrderNumber} {decision}",
                    Description = $"Order {order.OrderNumber} was {decision.ToLowerInvariant()} by {_userContext.Email ?? "Admin"}. Reason: {reason ?? "None"}",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    Status = decision,
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, Decision = decision, Reason = reason }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish business event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task NotifyPaymentSubmittedForVerificationAsync(ClassroomGiftingOrder order, string method, decimal amount, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "ClassroomGiftingPaymentSubmitted",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"Payment Verification Required: {order.OrderNumber}",
                    Description = $"Parent {order.ParentName} submitted {method} payment (₹{amount:N2}) for verification.",
                    Amount = amount,
                    Currency = order.Currency,
                    Status = "PaymentVerificationPending",
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, Method = method, Amount = amount }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish payment submitted event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task NotifyPaymentConfirmedAsync(ClassroomGiftingOrder order, string method, decimal amount, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "ClassroomGiftingPaymentConfirmed",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"Payment Confirmed: {order.OrderNumber}",
                    Description = $"Payment of ₹{amount:N2} via {method} confirmed for order {order.OrderNumber}.",
                    Amount = amount,
                    Currency = order.Currency,
                    Status = "Paid",
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, Method = method, Amount = amount }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish payment confirmed event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task NotifyCashReceivedAsync(ClassroomGiftingOrder order, decimal amount, string receiptNumber, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "ClassroomGiftingCashReceived",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"Cash Payment Received: {order.OrderNumber}",
                    Description = $"Cash of ₹{amount:N2} received by {_userContext.Email} for order {order.OrderNumber} (Receipt #{receiptNumber}).",
                    Amount = amount,
                    Currency = order.Currency,
                    Status = "Paid",
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, ReceiptNumber = receiptNumber, Amount = amount }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish cash received event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    private async Task NotifyFulfilmentMilestoneAsync(ClassroomGiftingOrder order, string milestone, CancellationToken ct)
    {
        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = $"ClassroomGiftingFulfilment_{milestone}",
                    TenantId = order.TenantId,
                    OrganizationId = order.OrganizationId,
                    BranchId = order.BranchId,
                    InitiatedByUserId = _userContext.UserId,
                    TransactionReference = order.OrderNumber,
                    TransactionCategory = "ClassroomGifting",
                    Title = $"Gifting Order Milestone: {milestone}",
                    Description = $"Order {order.OrderNumber} updated to {milestone} by {_userContext.Email}.",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    Status = milestone,
                    ActionUrl = "/management/gifting-orders",
                    Metadata = new { OrderId = order.Id, order.OrderNumber, Milestone = milestone }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish milestone event for order {OrderNumber}", order.OrderNumber);
            }
        }
    }

    #endregion
}
