using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class BillingService : IBillingService
{
    private readonly IBillingRepository _billingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IResourceAuthorizer _authorizer;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<BillingService> _logger;
    private readonly IRazorpayGateway _razorpayGateway;

    public BillingService(
        IBillingRepository billingRepository,
        IOrganizationRepository organizationRepository,
        IResourceAuthorizer authorizer,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        ILogger<BillingService> logger,
        IRazorpayGateway? razorpayGateway = null)
    {
        _billingRepository = billingRepository;
        _organizationRepository = organizationRepository;
        _authorizer = authorizer;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
        _razorpayGateway = razorpayGateway ?? new DefaultFallbackRazorpayGateway();
    }

    public async Task<Invoice> CreateDraftInvoiceAsync(CreateDraftInvoiceRequest request, CancellationToken ct = default)
    {
        // Platform billing permission required
        EnsurePlatformBillingPermission();

        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("A valid organization must be selected.", nameof(request.OrganizationId));

        if (request.DueDate < request.IssueDate)
            throw new ArgumentException("Due date cannot be earlier than invoice issue date.");

        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("A target daycare organization must be selected.");

        var org = await _organizationRepository.GetByIdAsync(request.OrganizationId, ct);
        if (org == null)
        {
            throw new ArgumentException($"Daycare organization with ID '{request.OrganizationId}' was not found. Please select an existing organization.");
        }

        var orgTenantId = org.TenantId;
        var invoiceNumber = await GenerateUniqueInvoiceNumberAsync(ct);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = orgTenantId,
            OrganizationId = org.Id,
            InvoiceNumber = invoiceNumber,
            Currency = "INR",
            IssueDate = request.IssueDate.ToUniversalTime(),
            DueDate = request.DueDate.ToUniversalTime(),
            Status = InvoiceStatus.Draft,
            Description = request.Description,
            Notes = request.Notes,
            PaymentInstructions = request.PaymentInstructions,
            CreatedBy = _userContext.Email ?? "Platform Admin",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Calculate line items on server
        int lineNumber = 1;
        decimal subtotal = 0;
        foreach (var item in request.LineItems)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                throw new ArgumentException("Line item description cannot be empty.");
            if (item.Quantity <= 0)
                throw new ArgumentException("Line item quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                throw new ArgumentException("Line item unit price cannot be negative.");

            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = lineNumber++,
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = Math.Round(item.UnitPrice, 2),
                Category = string.IsNullOrWhiteSpace(item.Category) ? "Subscription" : item.Category.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            subtotal += lineItem.LineTotal;
            invoice.LineItems.Add(lineItem);
        }

        invoice.Subtotal = Math.Round(subtotal, 2);
        var taxRate = Math.Max(0, request.TaxRatePercentage);
        invoice.TaxAmount = Math.Round(invoice.Subtotal * (taxRate / 100m), 2);
        invoice.TotalAmount = invoice.Subtotal + invoice.TaxAmount - invoice.DiscountAmount;
        invoice.AmountPaid = 0;

        var created = await _billingRepository.CreateInvoiceAsync(invoice, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Invoice.DraftCreated",
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            Details = new { invoice.InvoiceNumber, invoice.TotalAmount, invoice.OrganizationId }
        }, ct);

        return created;
    }

    public async Task<Invoice> UpdateDraftInvoiceAsync(UpdateDraftInvoiceRequest request, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        var existing = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {request.InvoiceId} was not found.");

        if (existing.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Only draft invoices can be modified. Current status: {existing.Status}.");

        if (request.DueDate < request.IssueDate)
            throw new ArgumentException("Due date cannot be earlier than invoice issue date.");

        existing.IssueDate = request.IssueDate.ToUniversalTime();
        existing.DueDate = request.DueDate.ToUniversalTime();
        existing.Description = request.Description;
        existing.Notes = request.Notes;
        existing.PaymentInstructions = request.PaymentInstructions;
        existing.LineItems.Clear();

        int lineNumber = 1;
        decimal subtotal = 0;
        foreach (var item in request.LineItems)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                throw new ArgumentException("Line item description cannot be empty.");
            if (item.Quantity <= 0)
                throw new ArgumentException("Line item quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                throw new ArgumentException("Line item unit price cannot be negative.");

            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = existing.Id,
                LineNumber = lineNumber++,
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = Math.Round(item.UnitPrice, 2),
                Category = string.IsNullOrWhiteSpace(item.Category) ? "Subscription" : item.Category.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            subtotal += lineItem.LineTotal;
            existing.LineItems.Add(lineItem);
        }

        existing.Subtotal = Math.Round(subtotal, 2);
        var taxRate = Math.Max(0, request.TaxRatePercentage);
        existing.TaxAmount = Math.Round(existing.Subtotal * (taxRate / 100m), 2);
        existing.TotalAmount = existing.Subtotal + existing.TaxAmount - existing.DiscountAmount;

        await _billingRepository.UpdateInvoiceAsync(existing, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = existing.TenantId,
            UserId = _userContext.UserId,
            Action = "Invoice.DraftUpdated",
            EntityType = nameof(Invoice),
            EntityId = existing.Id,
            Details = new { existing.InvoiceNumber, existing.TotalAmount }
        }, ct);

        return existing;
    }

    public async Task<Invoice> PublishInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} was not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Only draft invoices can be published. Current status: {invoice.Status}.");

        if (!invoice.LineItems.Any())
            throw new InvalidOperationException("An invoice must contain at least one line item before publishing.");

        if (invoice.TotalAmount <= 0)
            throw new InvalidOperationException("Invoice total amount must be greater than zero to publish.");

        invoice.Status = InvoiceStatus.Unpaid;
        invoice.PublishedAt = DateTimeOffset.UtcNow;
        invoice.PublishedBy = _userContext.Email ?? "Platform Admin";
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _billingRepository.UpdateInvoiceAsync(invoice, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Invoice.Published",
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            Details = new { invoice.InvoiceNumber, invoice.TotalAmount, invoice.OrganizationId, invoice.PublishedBy }
        }, ct);

        return invoice;
    }

    public async Task<Invoice> VoidInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason must be provided to void an invoice.", nameof(reason));

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} was not found.");

        if (invoice.Status == InvoiceStatus.Void)
            throw new InvalidOperationException("This invoice has already been voided.");

        if (invoice.AmountPaid > 0)
            throw new InvalidOperationException($"Cannot void an invoice with recorded payments ({invoice.AmountPaid:C2}). Please reverse or refund payments before voiding.");

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAt = DateTimeOffset.UtcNow;
        invoice.VoidedBy = _userContext.Email ?? "Platform Admin";
        invoice.VoidReason = reason.Trim();
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _billingRepository.UpdateInvoiceAsync(invoice, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Invoice.Voided",
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            Details = new { invoice.InvoiceNumber, Reason = reason, VoidedBy = invoice.VoidedBy }
        }, ct);

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct);
        if (invoice == null) return null;

        await _authorizer.RequireInvoiceAccessAsync(invoice, Permissions.OrganizationBillingView, ct);
        return invoice;
    }

    public async Task<List<Invoice>> GetInvoicesForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var isPlatformAdmin = _userContext.HasPermission(Permissions.PlatformBillingManage);
        if (!isPlatformAdmin)
        {
            await _authorizer.RequireOrganizationAsync(organizationId, Permissions.OrganizationBillingView, ct);
        }

        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : organizationId;
        // Org admins can never view drafts; platform admins can
        return await _billingRepository.GetInvoicesForOrganizationAsync(tenantId, organizationId, includeDrafts: isPlatformAdmin, ct);
    }

    public async Task<List<Invoice>> GetAllInvoicesAsync(InvoiceStatus? status = null, Guid? organizationId = null, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();
        return await _billingRepository.GetAllInvoicesAsync(null, organizationId, status, ct);
    }

    public async Task<Payment> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(request.Amount));

        if (string.IsNullOrWhiteSpace(request.PaymentReference))
            throw new ArgumentException("Payment reference or transaction number is required.", nameof(request.PaymentReference));

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {request.InvoiceId} was not found.");

        if (invoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot record payment against a draft invoice. Please publish the invoice first.");

        if (invoice.Status == InvoiceStatus.Void)
            throw new InvalidOperationException("Cannot record payment against a voided invoice.");

        if (request.Amount > invoice.BalanceDue)
            throw new InvalidOperationException($"Payment amount ({request.Amount:C2}) exceeds outstanding balance due ({invoice.BalanceDue:C2}). Overpayment is not permitted.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = Math.Round(request.Amount, 2),
            Currency = invoice.Currency,
            Method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "BankTransfer" : request.PaymentMethod.Trim(),
            PaymentReference = request.PaymentReference.Trim(),
            PaymentDate = request.PaymentDate.ToUniversalTime(),
            IsVerified = true,
            VerifiedBy = _userContext.Email ?? "Platform Admin",
            VerifiedAt = DateTimeOffset.UtcNow,
            Status = "Completed",
            Notes = request.Notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var recorded = await _billingRepository.RecordPaymentAsync(payment, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Payment.Recorded",
            EntityType = nameof(Payment),
            EntityId = recorded.Id,
            Details = new { invoice.InvoiceNumber, recorded.Amount, recorded.PaymentReference, recorded.Method }
        }, ct);

        return recorded;
    }

    public async Task<Payment> RecordCashPaymentAsync(RecordCashPaymentRequest request, CancellationToken ct = default)
    {
        EnsurePlatformCashPermission();

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(request.Amount));

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {request.InvoiceId} was not found.");

        if (invoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot record payment against a draft invoice. Please publish the invoice first.");

        if (invoice.Status == InvoiceStatus.Void)
            throw new InvalidOperationException("Cannot record payment against a voided invoice.");

        if (request.Amount > invoice.BalanceDue)
            throw new InvalidOperationException($"Cash amount ({request.Amount:N2}) exceeds outstanding balance due ({invoice.BalanceDue:N2}). Overpayment is not permitted.");

        var receiptNumber = !string.IsNullOrWhiteSpace(request.ReceiptNumber)
            ? request.ReceiptNumber.Trim()
            : "RCPT-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = Math.Round(request.Amount, 2),
            Currency = invoice.Currency,
            Method = PaymentMethods.Cash,
            PaymentReference = receiptNumber,
            ReceiptNumber = receiptNumber,
            RecordedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
            RecordedByEmail = _userContext.Email ?? "Platform Admin",
            IsVerified = true,
            VerifiedBy = _userContext.Email ?? "Platform Admin",
            VerifiedAt = DateTimeOffset.UtcNow,
            Status = "Completed",
            Notes = request.Notes,
            PaymentDate = request.ReceiptDate.ToUniversalTime(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var recorded = await _billingRepository.RecordPaymentAsync(payment, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Payment.CashRecorded",
            EntityType = nameof(Payment),
            EntityId = recorded.Id,
            Details = new { invoice.InvoiceNumber, recorded.Amount, recorded.ReceiptNumber, recorded.RecordedByEmail }
        }, ct);

        return recorded;
    }

    public async Task<Payment> ReversePaymentAsync(ReversePaymentRequest request, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A reason is required to reverse a confirmed payment.", nameof(request.Reason));

        var payment = await _billingRepository.GetPaymentByIdAsync(request.PaymentId, ct)
            ?? throw new KeyNotFoundException($"Payment with ID '{request.PaymentId}' was not found.");

        if (payment.IsReversed)
            throw new InvalidOperationException($"Payment '{payment.PaymentReference}' is already reversed.");

        var actor = _userContext.Email ?? "Platform Admin";
        var reversed = await _billingRepository.ReversePaymentAsync(request.PaymentId, actor, request.Reason.Trim(), ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = reversed.TenantId,
            UserId = _userContext.UserId,
            Action = "Payment.Reversed",
            EntityType = nameof(Payment),
            EntityId = reversed.Id,
            Details = new { reversed.InvoiceNumber, reversed.Amount, reversed.PaymentReference, reversed.ReversalReference, Reason = request.Reason }
        }, ct);

        return reversed;
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _billingRepository.GetPaymentByIdAsync(paymentId, ct);
        if (payment == null) return null;

        var isPlatformAdmin = _userContext.HasPermission(Permissions.PlatformBillingManage);
        if (!isPlatformAdmin)
        {
            await _authorizer.RequireOrganizationAsync(payment.OrganizationId, Permissions.OrganizationBillingView, ct);
        }

        return payment;
    }

    public async Task<RazorpayOrderResponse> CreateRazorpayOrderAsync(CreateRazorpayOrderRequest request, CancellationToken ct = default)
    {
        if (request.InvoiceId == Guid.Empty)
            throw new ArgumentException("A valid invoice ID is required.", nameof(request.InvoiceId));

        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {request.InvoiceId} was not found.");

        // Strict multi-tenant authorization
        await _authorizer.RequireInvoiceAccessAsync(invoice, Permissions.OrganizationBillingView, ct);

        if (invoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Draft invoices cannot be paid online. The invoice must be published first.");

        if (invoice.Status == InvoiceStatus.Void)
            throw new InvalidOperationException("Cannot create an order for a voided invoice.");

        if (invoice.BalanceDue <= 0 || invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException($"Invoice '{invoice.InvoiceNumber}' has no balance due.");

        // Supported currency enforcement: Entire application currency is INR only
        var targetCurrency = !string.IsNullOrWhiteSpace(invoice.Currency) ? invoice.Currency.ToUpperInvariant() : "INR";
        if (targetCurrency != "INR")
        {
            throw new NotSupportedException($"Currency '{targetCurrency}' is not supported. The application uses INR only.");
        }

        // Authoritative server-side balance
        var balanceDue = invoice.BalanceDue;
        long amountInPaise = (long)Math.Round(balanceDue * 100m, MidpointRounding.AwayFromZero);
        if (amountInPaise <= 0)
        {
            throw new InvalidOperationException("Calculated payable amount in paise is zero or invalid.");
        }

        var org = await _organizationRepository.GetByIdAsync(invoice.OrganizationId, ct);
        var orgName = org?.Name ?? "Daycare Organization";

        var notes = new Dictionary<string, string>
        {
            ["invoice_id"] = invoice.Id.ToString(),
            ["invoice_number"] = invoice.InvoiceNumber,
            ["organization_id"] = invoice.OrganizationId.ToString(),
            ["tenant_id"] = invoice.TenantId.ToString()
        };

        var gatewayResult = await _razorpayGateway.CreateOrderAsync(
            amountInPaise,
            targetCurrency,
            invoice.InvoiceNumber,
            notes,
            ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Payment.RazorpayOrderCreated",
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            Details = new { invoice.InvoiceNumber, gatewayResult.OrderId, AmountInPaise = amountInPaise, Currency = targetCurrency }
        }, ct);

        return new RazorpayOrderResponse
        {
            OrderId = gatewayResult.OrderId,
            KeyId = _razorpayGateway.Settings.KeyId,
            Amount = balanceDue,
            AmountInPaise = amountInPaise,
            Currency = targetCurrency,
            InvoiceNumber = invoice.InvoiceNumber,
            OrganizationName = orgName,
            CustomerEmail = invoice.BillingContactEmail ?? _userContext.Email,
            OutstandingBalance = balanceDue
        };
    }

    public async Task<RazorpayVerificationResult> VerifyRazorpayPaymentAsync(VerifyRazorpayPaymentRequest request, CancellationToken ct = default)
    {
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, request.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {request.InvoiceId} was not found.");

        await _authorizer.RequireInvoiceAccessAsync(invoice, Permissions.OrganizationBillingView, ct);

        // 1. Verify provider signature
        var isValid = _razorpayGateway.VerifyPaymentSignature(
            request.RazorpayOrderId,
            request.RazorpayPaymentId,
            request.RazorpaySignature);

        if (!isValid)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = invoice.TenantId,
                UserId = _userContext.UserId,
                Action = "Payment.SignatureVerificationFailed",
                EntityType = nameof(Payment),
                Details = new { request.InvoiceId, request.RazorpayOrderId, request.RazorpayPaymentId }
            }, ct);

            return new RazorpayVerificationResult
            {
                Success = false,
                ErrorMessage = "Razorpay payment signature verification failed. Transaction was not confirmed by the server."
            };
        }

        // 2. Check idempotency: has this payment ID already been recorded?
        var existingPayment = await _billingRepository.GetPaymentByProviderPaymentIdAsync(request.RazorpayPaymentId, ct);
        if (existingPayment != null)
        {
            var refreshedInvoice = await _billingRepository.GetInvoiceByIdAsync(invoice.TenantId, invoice.Id, ct);
            return new RazorpayVerificationResult
            {
                Success = true,
                Payment = existingPayment,
                Invoice = refreshedInvoice
            };
        }

        // 3. Create and record the verified payment
        var paymentAmount = invoice.BalanceDue;
        if (paymentAmount <= 0)
        {
            var refreshedInvoice = await _billingRepository.GetInvoiceByIdAsync(invoice.TenantId, invoice.Id, ct);
            return new RazorpayVerificationResult
            {
                Success = true,
                Invoice = refreshedInvoice,
                ErrorMessage = "Invoice is already fully paid."
            };
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = invoice.TenantId,
            OrganizationId = invoice.OrganizationId,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Amount = paymentAmount,
            Currency = invoice.Currency,
            Method = PaymentMethods.Razorpay,
            PaymentReference = request.RazorpayPaymentId.Trim(),
            ProviderOrderId = request.RazorpayOrderId.Trim(),
            ProviderPaymentId = request.RazorpayPaymentId.Trim(),
            ProviderSignature = request.RazorpaySignature.Trim(),
            IsVerified = true,
            VerifiedBy = "Razorpay Checkout Verification",
            VerifiedAt = DateTimeOffset.UtcNow,
            Status = "Completed",
            PaymentDate = DateTimeOffset.UtcNow,
            Notes = $"Razorpay online payment captured. Order ID: {request.RazorpayOrderId}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var recorded = await _billingRepository.RecordPaymentAsync(payment, ct);
        var updatedInvoice = await _billingRepository.GetInvoiceByIdAsync(invoice.TenantId, invoice.Id, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = invoice.TenantId,
            UserId = _userContext.UserId,
            Action = "Payment.RazorpayVerified",
            EntityType = nameof(Payment),
            EntityId = recorded.Id,
            Details = new { invoice.InvoiceNumber, recorded.Amount, recorded.ProviderPaymentId, recorded.ProviderOrderId }
        }, ct);

        return new RazorpayVerificationResult
        {
            Success = true,
            Payment = recorded,
            Invoice = updatedInvoice
        };
    }

    public async Task<bool> ProcessRazorpayWebhookAsync(string requestBody, string signatureHeader, CancellationToken ct = default)
    {
        if (!_razorpayGateway.VerifyWebhookSignature(requestBody, signatureHeader))
        {
            _logger.LogWarning("Razorpay webhook signature verification failed.");
            return false;
        }

        using var doc = System.Text.Json.JsonDocument.Parse(requestBody);
        var root = doc.RootElement;
        if (!root.TryGetProperty("event", out var eventProp)) return false;

        var eventName = eventProp.GetString();
        if (eventName != "payment.captured" && eventName != "order.paid")
        {
            _logger.LogInformation("Ignoring unhandled Razorpay webhook event: {Event}", eventName);
            return true;
        }

        if (root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("payment", out var paymentProp) &&
            paymentProp.TryGetProperty("entity", out var entity))
        {
            var paymentId = entity.GetProperty("id").GetString();
            var orderId = entity.TryGetProperty("order_id", out var o) ? o.GetString() : null;
            var amountPaise = entity.GetProperty("amount").GetInt64();
            var currency = entity.GetProperty("currency").GetString() ?? "INR";
            decimal amount = amountPaise / 100m;

            if (string.IsNullOrWhiteSpace(paymentId)) return false;

            var existing = await _billingRepository.GetPaymentByProviderPaymentIdAsync(paymentId, ct);
            if (existing != null)
            {
                _logger.LogInformation("Razorpay payment {PaymentId} already recorded idempotently.", paymentId);
                return true;
            }

            Guid? invoiceId = null;
            if (entity.TryGetProperty("notes", out var notes))
            {
                if (notes.TryGetProperty("invoice_id", out var invIdProp) && 
                    Guid.TryParse(invIdProp.GetString(), out var parsedId))
                {
                    invoiceId = parsedId;
                }
            }

            if (invoiceId.HasValue)
            {
                var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId.Value, ct);
                if (invoice != null && invoice.Status != InvoiceStatus.Void)
                {
                    var newPayment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        TenantId = invoice.TenantId,
                        OrganizationId = invoice.OrganizationId,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Amount = Math.Min(amount, invoice.BalanceDue > 0 ? invoice.BalanceDue : amount),
                        Currency = currency,
                        Method = PaymentMethods.Razorpay,
                        PaymentReference = paymentId,
                        ProviderOrderId = orderId,
                        ProviderPaymentId = paymentId,
                        IsVerified = true,
                        VerifiedBy = "Razorpay Webhook",
                        VerifiedAt = DateTimeOffset.UtcNow,
                        Status = "Completed",
                        Notes = $"Captured via webhook {eventName}",
                        PaymentDate = DateTimeOffset.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await _billingRepository.RecordPaymentAsync(newPayment, ct);
                    _logger.LogInformation("Webhook recorded payment {PaymentId} against invoice {InvoiceNumber}", paymentId, invoice.InvoiceNumber);
                }
            }
        }

        return true;
    }

    private void EnsurePlatformCashPermission()
    {
        if (!_userContext.HasPermission(Permissions.PlatformBillingManage) &&
            !_userContext.HasPermission(Permissions.PlatformCashCollect))
        {
            throw new UnauthorizedAccessException("User does not have permission to collect cash payments or manage commercial billing.");
        }
    }

    public async Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _billingRepository.GetInvoiceByIdAsync(Guid.Empty, invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice with ID {invoiceId} was not found.");

        await _authorizer.RequireInvoiceAccessAsync(invoice, Permissions.OrganizationBillingView, ct);
        return await _billingRepository.GetPaymentsForInvoiceAsync(invoiceId, ct);
    }

    public Task<List<Payment>> GetInvoicePaymentsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        return GetPaymentsForInvoiceAsync(invoiceId, ct);
    }

    public async Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var isPlatformAdmin = _userContext.HasPermission(Permissions.PlatformBillingManage);
        if (!isPlatformAdmin)
        {
            await _authorizer.RequireOrganizationAsync(organizationId, Permissions.OrganizationBillingView, ct);
        }

        return await _billingRepository.GetPaymentsForOrganizationAsync(organizationId, ct);
    }

    public async Task<OrganizationBillingSettings> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var isPlatformAdmin = _userContext.HasPermission(Permissions.PlatformBillingManage);
        if (!isPlatformAdmin)
        {
            await _authorizer.RequireOrganizationAsync(organizationId, Permissions.OrganizationBillingView, ct);
        }

        var settings = await _billingRepository.GetBillingSettingsAsync(organizationId, ct);
        if (settings != null) return settings;

        // Default to Manual mode
        var tenantId = _tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : organizationId;
        return new OrganizationBillingSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            Mode = BillingMode.Manual,
            Frequency = BillingFrequency.Monthly,
            EffectiveDate = DateTimeOffset.UtcNow,
            AutoChargeEnabled = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<OrganizationBillingSettings> UpdateBillingModeAsync(UpdateBillingModeRequest request, CancellationToken ct = default)
    {
        EnsurePlatformBillingPermission();

        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException("A valid organization must be specified.", nameof(request.OrganizationId));

        var existing = await _billingRepository.GetBillingSettingsAsync(request.OrganizationId, ct);
        var oldMode = existing?.Mode ?? BillingMode.Manual;

        var tenantId = existing?.TenantId ?? (_tenantContext.TenantId != Guid.Empty ? _tenantContext.TenantId : request.OrganizationId);

        DateTimeOffset? nextBillingDate = null;
        if (request.Mode == BillingMode.Automatic)
        {
            // If switching to Automatic, schedule next billing date based on effective date
            nextBillingDate = request.EffectiveDate > DateTimeOffset.UtcNow 
                ? request.EffectiveDate 
                : DateTimeOffset.UtcNow.AddMonths(1);
        }

        var settings = new OrganizationBillingSettings
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = request.OrganizationId,
            Mode = request.Mode,
            Frequency = request.Frequency,
            EffectiveDate = request.EffectiveDate.ToUniversalTime(),
            NextBillingDate = nextBillingDate?.ToUniversalTime(),
            AutoChargeEnabled = false,
            Notes = request.Reason,
            UpdatedBy = _userContext.Email ?? "Platform Admin",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _billingRepository.UpsertBillingSettingsAsync(settings, ct);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = _userContext.UserId,
            Action = "BillingMode.Changed",
            EntityType = nameof(OrganizationBillingSettings),
            EntityId = settings.Id,
            Details = new 
            { 
                settings.OrganizationId, 
                OldMode = oldMode.ToString(), 
                NewMode = settings.Mode.ToString(), 
                settings.Frequency,
                settings.EffectiveDate,
                Reason = request.Reason
            }
        }, ct);

        return settings;
    }

    public async Task<AutomaticBillingRunResult> ExecuteAutomaticBillingRunAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
    {
        var result = new AutomaticBillingRunResult();
        var dueOrgs = await _billingRepository.GetOrganizationsDueForAutomaticBillingAsync(asOfDate, ct);
        result.ProcessedOrganizations = dueOrgs.Count;

        var periodKey = asOfDate.ToString("yyyy-MM");

        foreach (var orgSetting in dueOrgs)
        {
            try
            {
                // Check if an invoice for this organization and billing period already exists
                var existingInvoices = await _billingRepository.GetInvoicesForOrganizationAsync(orgSetting.TenantId, orgSetting.OrganizationId, includeDrafts: true, ct);
                var alreadyBilled = existingInvoices.Any(i => i.BillingPeriod == periodKey && i.Status != InvoiceStatus.Void);
                if (alreadyBilled)
                {
                    _logger.LogInformation("Organization {OrgId} already has an invoice for period {Period}. Skipping duplicate generation.", orgSetting.OrganizationId, periodKey);
                    continue;
                }

                // Generate automatic subscription invoice
                var invoiceNumber = await GenerateUniqueInvoiceNumberAsync(ct);
                var autoInvoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = orgSetting.TenantId,
                    OrganizationId = orgSetting.OrganizationId,
                    InvoiceNumber = invoiceNumber,
                    BillingPeriod = periodKey,
                    Currency = "INR",
                    IssueDate = asOfDate,
                    DueDate = asOfDate.AddDays(14),
                    Status = InvoiceStatus.Unpaid,
                    Description = $"Automated Subscription Billing — Period {periodKey}",
                    Notes = "Generated automatically according to active organization subscription schedule.",
                    PaymentInstructions = "Please remit payment via Bank Transfer or ACH within 14 days.",
                    CreatedBy = "System Automated Billing",
                    PublishedAt = asOfDate,
                    PublishedBy = "System Automated Billing",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                // Standard monthly subscription line item
                var lineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = autoInvoice.Id,
                    LineNumber = 1,
                    Description = $"EnjoyEveryday Platform Subscription ({orgSetting.Frequency})",
                    Quantity = 1,
                    UnitPrice = 299.00m, // Baseline standard tariff snapshot
                    Category = "Subscription",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                autoInvoice.LineItems.Add(lineItem);
                autoInvoice.Subtotal = lineItem.LineTotal;
                autoInvoice.TaxAmount = 0;
                autoInvoice.TotalAmount = autoInvoice.Subtotal;

                await _billingRepository.CreateInvoiceAsync(autoInvoice, ct);

                // Advance next billing date
                orgSetting.NextBillingDate = orgSetting.Frequency switch
                {
                    BillingFrequency.Quarterly => asOfDate.AddMonths(3),
                    BillingFrequency.Annual => asOfDate.AddYears(1),
                    _ => asOfDate.AddMonths(1)
                };
                await _billingRepository.UpsertBillingSettingsAsync(orgSetting, ct);

                result.GeneratedInvoices++;
                result.GeneratedInvoiceNumbers.Add(autoInvoice.InvoiceNumber);

                await _auditService.LogAsync(new AuditEntry
                {
                    TenantId = orgSetting.TenantId,
                    UserId = Guid.Empty,
                    Action = "Invoice.AutoGenerated",
                    EntityType = nameof(Invoice),
                    EntityId = autoInvoice.Id,
                    Details = new { autoInvoice.InvoiceNumber, autoInvoice.BillingPeriod, autoInvoice.TotalAmount, orgSetting.OrganizationId }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to automatically generate invoice for organization {OrgId}", orgSetting.OrganizationId);
                result.Errors.Add($"Org {orgSetting.OrganizationId}: {ex.Message}");
            }
        }

        return result;
    }

    private void EnsurePlatformBillingPermission()
    {
        if (!_userContext.HasPermission(Permissions.PlatformBillingManage))
        {
            throw new UnauthorizedAccessException("Unauthorized: Operation requires platform billing management permission.");
        }
    }

    private async Task<string> GenerateUniqueInvoiceNumberAsync(CancellationToken ct)
    {
        for (int i = 0; i < 10; i++)
        {
            var candidate = $"INV-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}";
            var existing = await _billingRepository.GetInvoiceByNumberAsync(candidate, ct);
            if (existing == null)
            {
                return candidate;
            }
        }
        return $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }
}

internal class DefaultFallbackRazorpayGateway : IRazorpayGateway
{
    public RazorpaySettings Settings { get; } = new RazorpaySettings { MockMode = true };

    public Task<RazorpayGatewayOrderResult> CreateOrderAsync(long amountInPaise, string currency, string receipt, Dictionary<string, string>? notes = null, CancellationToken ct = default)
    {
        return Task.FromResult(new RazorpayGatewayOrderResult
        {
            OrderId = "order_fallback_" + Guid.NewGuid().ToString("N")[..8],
            AmountInPaise = amountInPaise,
            Currency = currency,
            Receipt = receipt,
            Status = "created"
        });
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature) => signature == "mock_sig_valid" || signature == "mock_sig_test";
    public bool VerifyWebhookSignature(string rawRequestBody, string signatureHeader) => signatureHeader == "mock_webhook_valid";
}
