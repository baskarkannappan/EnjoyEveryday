using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class RazorpayAndCashPaymentLedgerTests
{
    private MockBillingRepository _billingRepo = null!;
    private MockOrganizationRepository _orgRepo = null!;
    private MockAuditService _auditService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private TestResourceAuthorizer _authorizer = null!;
    private TestRazorpayGateway _gateway = null!;
    private BillingService _billingService = null!;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private Invoice _testInvoice = null!;

    private const string TestKeyId = "rzp_test_key123";
    private const string TestKeySecret = "rzp_test_secret456";
    private const string TestWebhookSecret = "whsec_test_secret789";

    [SetUp]
    public void Setup()
    {
        _billingRepo = new MockBillingRepository();
        _orgRepo = new MockOrganizationRepository();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantId };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "platform.admin@enjoyeveryday.test",
            Roles = new List<string> { RoleTemplates.PlatformAdmin },
            Permissions = new List<string>
            {
                Permissions.PlatformBillingManage,
                Permissions.PlatformCashCollect,
                Permissions.OrganizationBillingView
            }
        };

        _authorizer = new TestResourceAuthorizer(_userContext, _tenantContext);
        _gateway = new TestRazorpayGateway(TestKeyId, TestKeySecret, TestWebhookSecret);

        _orgRepo.Organizations.Add(new Organization
        {
            Id = _orgId,
            TenantId = _tenantId,
            Name = "Little Explorers Daycare",
            IsActive = true
        });

        _billingService = new BillingService(
            _billingRepo,
            _orgRepo,
            _authorizer,
            _tenantContext,
            _userContext,
            _auditService,
            NullLogger<BillingService>.Instance,
            _gateway);

        // Seed a published invoice with $500 total amount
        _testInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            InvoiceNumber = "INV-202610-001",
            Subtotal = 500.00m,
            TotalAmount = 500.00m,
            AmountPaid = 0.00m,
            Currency = "INR",
            Status = InvoiceStatus.Unpaid,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14)
        };
        _billingRepo.Invoices[_testInvoice.Id] = _testInvoice;
    }

    [Test]
    public async Task PlatformAdmin_CanRecordCashPayment_AndUpdatesSameLedgerAndInvoice()
    {
        var request = new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 200.00m,
            ReceiptNumber = "RCPT-CASH-20261016-01",
            ReceiptDate = DateTimeOffset.UtcNow,
            Notes = "Cash handed over at center reception"
        };

        var payment = await _billingService.RecordCashPaymentAsync(request);

        Assert.That(payment, Is.Not.Null);
        Assert.That(payment.PaymentMethod, Is.EqualTo(PaymentMethods.Cash));
        Assert.That(payment.Amount, Is.EqualTo(200.00m));
        Assert.That(payment.ReceiptNumber, Is.EqualTo("RCPT-CASH-20261016-01"));
        Assert.That(payment.IsVerified, Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Completed));

        // Invoice balance is updated
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(200.00m));
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(300.00m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.PartiallyPaid));

        // Audit entry is generated
        Assert.That(_auditService.Entries.Any(e => e.Action == "Payment.CashRecorded"), Is.True);
    }

    [Test]
    public void NonAuthorizedUser_CannotRecordCashPayment()
    {
        // Remove platform cash collection permission
        _userContext.Permissions = new List<string> { Permissions.OrganizationBillingView };
        _userContext.Roles = new List<string> { RoleTemplates.OrganizationAdmin };

        var request = new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 100.00m,
            ReceiptNumber = "RCPT-UNAUTH-01",
            ReceiptDate = DateTimeOffset.UtcNow
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _billingService.RecordCashPaymentAsync(request);
        });
    }

    [Test]
    public void CashPayment_Overpayment_IsRejected()
    {
        var request = new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 550.00m, // Invoice balance is only $500
            ReceiptNumber = "RCPT-OVER-01",
            ReceiptDate = DateTimeOffset.UtcNow
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _billingService.RecordCashPaymentAsync(request);
        });

        Assert.That(ex!.Message, Does.Contain("exceeds outstanding balance"));
    }

    [Test]
    public async Task FullCashPayment_MarksInvoiceAsPaid()
    {
        var request = new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 500.00m,
            ReceiptNumber = "RCPT-FULL-01",
            ReceiptDate = DateTimeOffset.UtcNow
        };

        var payment = await _billingService.RecordCashPaymentAsync(request);

        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(500.00m));
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(0.00m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.Paid));
        Assert.That(_testInvoice.PaidAt, Is.Not.Null);
    }

    [Test]
    public async Task Razorpay_CreateOrder_UsesServerCalculatedOutstandingBalanceInPaise()
    {
        // Outstanding is $500.00 -> 50000 paise/cents
        var order = await _billingService.CreateRazorpayOrderAsync(new CreateRazorpayOrderRequest { InvoiceId = _testInvoice.Id });

        Assert.That(order, Is.Not.Null);
        Assert.That(order.AmountInPaise, Is.EqualTo(50000));
        Assert.That(order.KeyId, Is.EqualTo(TestKeyId));
        Assert.That(order.OrderId, Does.StartWith("order_"));
    }

    [Test]
    public async Task Razorpay_VerifyPayment_WithValidSignature_RecordsPaymentInSameLedger()
    {
        var order = await _billingService.CreateRazorpayOrderAsync(new CreateRazorpayOrderRequest { InvoiceId = _testInvoice.Id });
        var paymentId = "pay_test_999";
        var validSignature = ComputeRazorpaySignature(order.OrderId, paymentId, TestKeySecret);

        var verifyReq = new VerifyRazorpayPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            RazorpayOrderId = order.OrderId,
            RazorpayPaymentId = paymentId,
            RazorpaySignature = validSignature
        };

        var result = await _billingService.VerifyRazorpayPaymentAsync(verifyReq);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payment, Is.Not.Null);
        var payment = result.Payment!;
        Assert.That(payment.PaymentMethod, Is.EqualTo(PaymentMethods.Razorpay));
        Assert.That(payment.ProviderPaymentId, Is.EqualTo(paymentId));
        Assert.That(payment.ProviderOrderId, Is.EqualTo(order.OrderId));
        Assert.That(payment.IsVerified, Is.True);
        Assert.That(payment.Status, Is.EqualTo(PaymentStatus.Completed));

        // Invoice balance is updated in the same ledger
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(500.00m));
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(0.00m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.Paid));
    }

    [Test]
    public async Task Razorpay_VerifyPayment_WithTamperedSignature_IsRejected()
    {
        var order = await _billingService.CreateRazorpayOrderAsync(new CreateRazorpayOrderRequest { InvoiceId = _testInvoice.Id });

        var verifyReq = new VerifyRazorpayPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            RazorpayOrderId = order.OrderId,
            RazorpayPaymentId = "pay_test_999",
            RazorpaySignature = "tampered_fake_signature"
        };

        var result = await _billingService.VerifyRazorpayPaymentAsync(verifyReq);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid payment signature"));

        // Ensure invoice remains unpaid
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(0m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.Unpaid));
    }

    [Test]
    public async Task Razorpay_DuplicatePaymentCallback_IsIdempotent()
    {
        var order = await _billingService.CreateRazorpayOrderAsync(new CreateRazorpayOrderRequest { InvoiceId = _testInvoice.Id });
        var paymentId = "pay_test_idempotent_1";
        var validSignature = ComputeRazorpaySignature(order.OrderId, paymentId, TestKeySecret);

        var verifyReq = new VerifyRazorpayPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            RazorpayOrderId = order.OrderId,
            RazorpayPaymentId = paymentId,
            RazorpaySignature = validSignature
        };

        // First verification
        var result1 = await _billingService.VerifyRazorpayPaymentAsync(verifyReq);
        Assert.That(result1.Success, Is.True);
        var payment1 = result1.Payment!;
        Assert.That(payment1.Amount, Is.EqualTo(500.00m));
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(500.00m));

        // Second duplicate verification (e.g. network retry or webhook duplicate)
        var result2 = await _billingService.VerifyRazorpayPaymentAsync(verifyReq);
        Assert.That(result2.Success, Is.True);
        var payment2 = result2.Payment!;

        Assert.That(payment2.Id, Is.EqualTo(payment1.Id));
        // Crucial: invoice amount paid must NOT be double-credited
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(500.00m));
    }

    [Test]
    public async Task PlatformAdmin_CanReversePayment_RestoresInvoiceBalance()
    {
        // 1. Record cash payment of $200
        var cashReq = new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 200.00m,
            ReceiptNumber = "RCPT-REV-01",
            ReceiptDate = DateTimeOffset.UtcNow
        };
        var payment = await _billingService.RecordCashPaymentAsync(cashReq);
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(300.00m));

        // 2. Reverse the payment
        var reversedPayment = await _billingService.ReversePaymentAsync(new ReversePaymentRequest
        {
            PaymentId = payment.Id,
            Reason = "Cash check bounced / returned to sender"
        });

        Assert.That(reversedPayment.IsReversed, Is.True);
        Assert.That(reversedPayment.Status, Is.EqualTo("Reversed"));
        Assert.That(reversedPayment.ReversalReason, Does.Contain("Cash check bounced"));

        // 3. Balance due is restored to full $500
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(0.00m));
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(500.00m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.Unpaid));

        // Audit entry logged
        Assert.That(_auditService.Entries.Any(e => e.Action == "Payment.Reversed"), Is.True);
    }

    [Test]
    public async Task UnifiedPaymentLedger_SupportsBothCashAndRazorpayOnSameInvoice()
    {
        // Daycare pays $150 in cash
        await _billingService.RecordCashPaymentAsync(new RecordCashPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            Amount = 150.00m,
            ReceiptNumber = "RCPT-PART-CASH",
            ReceiptDate = DateTimeOffset.UtcNow
        });

        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(350.00m));

        // Daycare pays remainder ($350) via Razorpay
        var order = await _billingService.CreateRazorpayOrderAsync(new CreateRazorpayOrderRequest { InvoiceId = _testInvoice.Id });
        Assert.That(order.AmountInPaise, Is.EqualTo(35000)); // Exactly remaining balance

        var paymentId = "pay_part_razorpay";
        var signature = ComputeRazorpaySignature(order.OrderId, paymentId, TestKeySecret);

        var verifyResult = await _billingService.VerifyRazorpayPaymentAsync(new VerifyRazorpayPaymentRequest
        {
            InvoiceId = _testInvoice.Id,
            RazorpayOrderId = order.OrderId,
            RazorpayPaymentId = paymentId,
            RazorpaySignature = signature
        });
        Assert.That(verifyResult.Success, Is.True);

        // Invoice is now fully settled across both payment methods
        Assert.That(_testInvoice.AmountPaid, Is.EqualTo(500.00m));
        Assert.That(_testInvoice.BalanceDue, Is.EqualTo(0.00m));
        Assert.That(_testInvoice.Status, Is.EqualTo(InvoiceStatus.Paid));

        var invoicePayments = await _billingService.GetInvoicePaymentsAsync(_testInvoice.Id);
        Assert.That(invoicePayments.Count, Is.EqualTo(2));
        Assert.That(invoicePayments.Any(p => p.PaymentMethod == PaymentMethods.Cash), Is.True);
        Assert.That(invoicePayments.Any(p => p.PaymentMethod == PaymentMethods.Razorpay), Is.True);
    }

    private static string ComputeRazorpaySignature(string orderId, string paymentId, string secret)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // --- Test Doubles ---

    private class TestRazorpayGateway : IRazorpayGateway
    {
        public RazorpaySettings Settings { get; }
        public string KeyId => Settings.KeyId;
        private readonly string _secret;
        private readonly string _webhookSecret;

        public TestRazorpayGateway(string keyId, string secret, string webhookSecret)
        {
            _secret = secret;
            _webhookSecret = webhookSecret;
            Settings = new RazorpaySettings
            {
                KeyId = keyId,
                KeySecret = secret,
                WebhookSecret = webhookSecret
            };
        }

        public Task<RazorpayGatewayOrderResult> CreateOrderAsync(long amountPaise, string currency, string receipt, Dictionary<string, string>? notes = null, CancellationToken ct = default)
        {
            var result = new RazorpayGatewayOrderResult
            {
                OrderId = "order_" + Guid.NewGuid().ToString("N")[..14],
                AmountInPaise = amountPaise,
                Currency = currency,
                Receipt = receipt,
                Status = "created"
            };
            return Task.FromResult(result);
        }

        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
        {
            var expected = ComputeRazorpaySignature(orderId, paymentId, _secret);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expected = Convert.ToHexString(hash).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
        }
    }

    private class MockBillingRepository : IBillingRepository
    {
        public readonly Dictionary<Guid, Invoice> Invoices = new();
        public readonly List<Payment> Payments = new();
        public readonly Dictionary<Guid, OrganizationBillingSettings> Settings = new();

        public Task<List<Invoice>> GetInvoicesForOrganizationAsync(Guid tenantId, Guid organizationId, bool includeDrafts, CancellationToken ct = default)
        {
            var list = Invoices.Values
                .Where(i => i.OrganizationId == organizationId)
                .Where(i => includeDrafts || i.Status != InvoiceStatus.Draft)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<Invoice>> GetAllInvoicesAsync(Guid? tenantId, Guid? organizationId, InvoiceStatus? status, CancellationToken ct = default)
        {
            var query = Invoices.Values.AsEnumerable();
            if (organizationId.HasValue) query = query.Where(i => i.OrganizationId == organizationId.Value);
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);
            return Task.FromResult(query.ToList());
        }

        public Task<Invoice?> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default)
        {
            Invoices.TryGetValue(invoiceId, out var inv);
            return Task.FromResult<Invoice?>(inv);
        }

        public Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default)
        {
            var inv = Invoices.Values.FirstOrDefault(i => i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(inv);
        }

        public Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
        {
            Invoices[invoice.Id] = invoice;
            return Task.FromResult(invoice);
        }

        public Task<bool> UpdateInvoiceAsync(Invoice invoice, CancellationToken ct = default)
        {
            Invoices[invoice.Id] = invoice;
            return Task.FromResult(true);
        }

        public Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            {
                var existing = Payments.FirstOrDefault(p => p.ProviderPaymentId == payment.ProviderPaymentId && !p.IsReversed);
                if (existing != null) return Task.FromResult(existing);
            }

            if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
            Payments.Add(payment);

            if (payment.InvoiceId.HasValue && Invoices.TryGetValue(payment.InvoiceId.Value, out var inv))
            {
                inv.Payments.Add(payment);
                var validSum = Payments.Where(p => p.InvoiceId == inv.Id && p.Status == PaymentStatus.Completed && !p.IsReversed).Sum(p => p.Amount);
                inv.AmountPaid = validSum;
                if (inv.BalanceDue <= 0)
                {
                    inv.Status = InvoiceStatus.Paid;
                    inv.PaidAt = DateTimeOffset.UtcNow;
                }
                else if (inv.AmountPaid > 0)
                {
                    inv.Status = InvoiceStatus.PartiallyPaid;
                }
                else
                {
                    inv.Status = InvoiceStatus.Unpaid;
                }
            }

            return Task.FromResult(payment);
        }

        public Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default)
        {
            return Task.FromResult(Payments.FirstOrDefault(p => p.Id == paymentId));
        }

        public Task<Payment?> GetPaymentByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default)
        {
            return Task.FromResult(Payments.FirstOrDefault(p => p.ProviderPaymentId == providerPaymentId && !p.IsReversed));
        }

        public Task<Payment?> GetPaymentByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default)
        {
            return Task.FromResult(Payments.FirstOrDefault(p => p.ReceiptNumber == receiptNumber && !p.IsReversed));
        }

        public Task<Payment> ReversePaymentAsync(Guid paymentId, string reversedBy, string reason, CancellationToken ct = default)
        {
            var p = Payments.FirstOrDefault(x => x.Id == paymentId)
                ?? throw new KeyNotFoundException($"Payment {paymentId} not found");
            p.IsReversed = true;
            p.Status = "Reversed";
            p.ReversedAt = DateTimeOffset.UtcNow;
            p.ReversedBy = reversedBy;
            p.ReversalReason = reason;

            if (p.InvoiceId.HasValue && Invoices.TryGetValue(p.InvoiceId.Value, out var inv))
            {
                var validPaid = Payments.Where(x => x.InvoiceId == inv.Id && x.Status == PaymentStatus.Completed && !x.IsReversed).Sum(x => x.Amount);
                inv.AmountPaid = validPaid;
                inv.Status = inv.BalanceDue <= 0 ? InvoiceStatus.Paid : (validPaid > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid);
                if (inv.BalanceDue > 0) inv.PaidAt = null;
            }
            return Task.FromResult(p);
        }

        public Task<Invoice> RecalculateInvoiceBalanceAsync(Guid invoiceId, CancellationToken ct = default)
        {
            if (Invoices.TryGetValue(invoiceId, out var inv))
            {
                var validPaid = Payments.Where(x => x.InvoiceId == inv.Id && x.Status == PaymentStatus.Completed && !x.IsReversed).Sum(x => x.Amount);
                inv.AmountPaid = validPaid;
                inv.Status = inv.BalanceDue <= 0 ? InvoiceStatus.Paid : (validPaid > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid);
                if (inv.BalanceDue > 0) inv.PaidAt = null;
                return Task.FromResult(inv);
            }
            throw new KeyNotFoundException($"Invoice {invoiceId} not found");
        }

        public Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        {
            return Task.FromResult(Payments.Where(p => p.InvoiceId == invoiceId).ToList());
        }

        public Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        {
            return Task.FromResult(Payments.Where(p => p.OrganizationId == organizationId).ToList());
        }

        public Task<OrganizationBillingSettings?> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default)
        {
            Settings.TryGetValue(organizationId, out var s);
            return Task.FromResult<OrganizationBillingSettings?>(s);
        }

        public Task<bool> UpsertBillingSettingsAsync(OrganizationBillingSettings settings, CancellationToken ct = default)
        {
            Settings[settings.OrganizationId] = settings;
            return Task.FromResult(true);
        }

        public Task<List<OrganizationBillingSettings>> GetOrganizationsDueForAutomaticBillingAsync(DateTimeOffset asOfDate, CancellationToken ct = default)
        {
            var list = Settings.Values
                .Where(s => s.Mode == BillingMode.Automatic && s.EffectiveDate <= asOfDate && (s.NextBillingDate == null || s.NextBillingDate <= asOfDate))
                .ToList();
            return Task.FromResult(list);
        }
    }

    private class MockOrganizationRepository : IOrganizationRepository
    {
        public readonly List<Organization> Organizations = new();

        public Task<Organization?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));

        public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));

        public Task<IEnumerable<Organization>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(Organizations.AsEnumerable());

        public Task<Organization> AddAsync(Organization organization, CancellationToken cancellationToken = default)
        {
            Organizations.Add(organization);
            return Task.FromResult(organization);
        }

        public Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        {
            Organizations.RemoveAll(o => o.Id == id);
            return Task.CompletedTask;
        }
    }

    private class MockAuditService : IAuditService
    {
        public readonly List<AuditEntry> Entries = new();

        public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private class TestResourceAuthorizer : IResourceAuthorizer
    {
        private readonly IUserContext _user;
        private readonly ITenantContext _tenant;

        public TestResourceAuthorizer(IUserContext user, ITenantContext tenant)
        {
            _user = user;
            _tenant = tenant;
        }

        public Task RequireOrganizationPermissionAsync(string permission, CancellationToken ct = default) => Task.CompletedTask;

        public Task RequireOrganizationAsync(Guid organizationId, string permission, CancellationToken ct = default)
        {
            if (_user.HasPermission(Permissions.PlatformBillingManage)) return Task.CompletedTask;
            if (_user.OrganizationId != organizationId) throw new UnauthorizedAccessException("Access denied.");
            return Task.CompletedTask;
        }

        public Task RequireBranchAsync(Guid branchId, string permission, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CanAccessBranchAsync(Guid branchId, string permission, CancellationToken ct = default) => Task.FromResult(true);
        public Task RequireClassroomAsync(Guid classroomId, string permission, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequireChildAsync(Guid childId, string permission, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CanAccessChildAsync(Guid childId, string permission, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<Child>> FilterAccessibleChildrenAsync(IEnumerable<Child> children, string permission, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Child>>(children.ToList());
        public Task RequireGuardianAsync(Guid childId, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequireCanGrantRoleAsync(string roleName, Guid targetUserId, CancellationToken ct = default) => Task.CompletedTask;

        public Task RequireInvoiceAccessAsync(Invoice invoice, string permission, CancellationToken ct = default)
        {
            if (_user.HasPermission(Permissions.PlatformBillingManage)) return Task.CompletedTask;
            if (invoice.TenantId != _tenant.TenantId || invoice.OrganizationId != _user.OrganizationId)
            {
                throw new UnauthorizedAccessException("Access denied to invoice.");
            }
            if (invoice.Status == InvoiceStatus.Draft)
            {
                throw new UnauthorizedAccessException("Draft invoices are not accessible to daycare organization members.");
            }
            return Task.CompletedTask;
        }
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public string TenantName => "Test Tenant";
        public bool IsResolved => true;
    }

    private class TestUserContext : IUserContext
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsAuthenticated => true;
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public IEnumerable<string> Permissions { get; set; } = new List<string>();
        public Guid? OrganizationId { get; set; }

        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }
    }
}
