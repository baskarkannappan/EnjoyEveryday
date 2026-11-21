using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class BillingService
{
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

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "SubscriptionInvoicePayment",
                    TenantId = invoice.TenantId,
                    OrganizationId = invoice.OrganizationId,
                    InitiatedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
                    TransactionReference = recorded.PaymentReference,
                    TransactionCategory = "PlatformBilling",
                    Title = $"Payment Recorded: {invoice.InvoiceNumber}",
                    Description = $"Payment of {recorded.Amount:N2} {recorded.Currency} ({recorded.Method}) recorded for invoice {invoice.InvoiceNumber}.",
                    Amount = recorded.Amount,
                    Currency = recorded.Currency,
                    Status = "Completed",
                    ActionUrl = "/billing",
                    Metadata = new
                    {
                        PaymentId = recorded.Id,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Amount = recorded.Amount,
                        Method = recorded.Method,
                        PaymentReference = recorded.PaymentReference
                    }
                }, ct);
            }
            catch { /* fail-safe */ }
        }

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

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "SubscriptionInvoicePayment",
                    TenantId = invoice.TenantId,
                    OrganizationId = invoice.OrganizationId,
                    InitiatedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
                    TransactionReference = recorded.ReceiptNumber,
                    TransactionCategory = "PlatformBilling",
                    Title = $"Cash Payment Recorded: {invoice.InvoiceNumber}",
                    Description = $"Cash payment of {recorded.Amount:N2} {recorded.Currency} (Receipt: {recorded.ReceiptNumber}) recorded for invoice {invoice.InvoiceNumber}.",
                    Amount = recorded.Amount,
                    Currency = recorded.Currency,
                    Status = "Completed",
                    ActionUrl = "/billing",
                    Metadata = new
                    {
                        PaymentId = recorded.Id,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Amount = recorded.Amount,
                        Method = recorded.Method,
                        ReceiptNumber = recorded.ReceiptNumber
                    }
                }, ct);
            }
            catch { /* fail-safe */ }
        }

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

        // Supported currency enforcement: Ensure valid currency code
        var targetCurrency = !string.IsNullOrWhiteSpace(invoice.Currency) ? invoice.Currency.ToUpperInvariant() : "INR";
        var supportedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "INR", "USD", "AUD", "JPY", "EUR", "GBP", "CAD", "SGD" };
        if (!supportedCurrencies.Contains(targetCurrency))
        {
            throw new NotSupportedException($"Currency '{targetCurrency}' is not supported.");
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

        if (_eventService != null)
        {
            try
            {
                await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                {
                    EventType = "SubscriptionInvoicePayment",
                    TenantId = invoice.TenantId,
                    OrganizationId = invoice.OrganizationId,
                    InitiatedByUserId = _userContext.UserId != Guid.Empty ? _userContext.UserId : null,
                    TransactionReference = recorded.PaymentReference,
                    TransactionCategory = "PlatformBilling",
                    Title = $"Online Payment Captured: {invoice.InvoiceNumber}",
                    Description = $"Online Razorpay payment of {recorded.Amount:N2} {recorded.Currency} captured for invoice {invoice.InvoiceNumber} (Order: {recorded.ProviderOrderId}).",
                    Amount = recorded.Amount,
                    Currency = recorded.Currency,
                    Status = "Completed",
                    ActionUrl = "/billing",
                    Metadata = new
                    {
                        PaymentId = recorded.Id,
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Amount = recorded.Amount,
                        Method = recorded.Method,
                        ProviderPaymentId = recorded.ProviderPaymentId,
                        ProviderOrderId = recorded.ProviderOrderId
                    }
                }, ct);
            }
            catch { /* fail-safe */ }
        }

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
        var eventId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        // Idempotency check: has this webhook event ID already been recorded?
        if (!string.IsNullOrWhiteSpace(eventId) && await _billingRepository.HasWebhookEventBeenProcessedAsync(eventId, ct))
        {
            _logger.LogInformation("Razorpay webhook event {EventId} already processed idempotently.", eventId);
            return true;
        }

        if (eventName != "payment.captured" && eventName != "order.paid")
        {
            _logger.LogInformation("Ignoring unhandled Razorpay webhook event: {Event}", eventName);
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                await _billingRepository.RecordWebhookEventAsync(eventId, eventName ?? "unknown", null, null, null, null, "Ignored", requestBody, null, ct);
            }
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
                if (!string.IsNullOrWhiteSpace(eventId))
                {
                    await _billingRepository.RecordWebhookEventAsync(eventId, eventName, existing.InvoiceId, paymentId, amountPaise, currency, "DuplicatePaymentIgnored", requestBody, null, ct);
                }
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
                else if (notes.TryGetProperty("invoiceId", out var invIdProp2) && 
                    Guid.TryParse(invIdProp2.GetString(), out var parsedId2))
                {
                    invoiceId = parsedId2;
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

                    if (_eventService != null)
                    {
                        try
                        {
                            await _eventService.PublishEventAsync(new PublishBusinessEventRequest
                            {
                                EventType = "SubscriptionInvoicePayment",
                                TenantId = invoice.TenantId,
                                OrganizationId = invoice.OrganizationId,
                                TransactionReference = paymentId,
                                TransactionCategory = "PlatformBilling",
                                Title = $"Webhook Payment Captured: {invoice.InvoiceNumber}",
                                Description = $"Razorpay webhook captured payment of {newPayment.Amount:N2} {newPayment.Currency} for invoice {invoice.InvoiceNumber}.",
                                Amount = newPayment.Amount,
                                Currency = newPayment.Currency,
                                Status = "Completed",
                                ActionUrl = "/billing",
                                Metadata = new
                                {
                                    PaymentId = newPayment.Id,
                                    InvoiceId = invoice.Id,
                                    InvoiceNumber = invoice.InvoiceNumber,
                                    Amount = newPayment.Amount,
                                    PaymentReference = paymentId,
                                    WebhookEvent = eventName
                                }
                            }, ct);
                        }
                        catch { /* fail-safe */ }
                    }

                    if (!string.IsNullOrWhiteSpace(eventId))
                    {
                        await _billingRepository.RecordWebhookEventAsync(eventId, eventName, invoice.Id, paymentId, amountPaise, currency, "Processed", requestBody, null, ct);
                    }
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

}
