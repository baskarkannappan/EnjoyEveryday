using System;
using System.Collections.Generic;
using System.Linq;
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
public class ManualAndAutomaticBillingSecurityTests
{
    private MockBillingRepository _billingRepo = null!;
    private MockOrganizationRepository _orgRepo = null!;
    private MockAuditService _auditService = null!;
    private TestUserContext _userContext = null!;
    private TestTenantContext _tenantContext = null!;
    private TestResourceAuthorizer _authorizer = null!;
    private BillingService _billingService = null!;

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _billingRepo = new MockBillingRepository();
        _orgRepo = new MockOrganizationRepository();
        _auditService = new MockAuditService();

        _tenantContext = new TestTenantContext { TenantId = _tenantA };
        _userContext = new TestUserContext
        {
            UserId = Guid.NewGuid(),
            Email = "platform.admin@enjoyeveryday.test",
            OrganizationId = null,
            Roles = new List<string> { RoleTemplates.PlatformAdmin },
            Permissions = new List<string> { Permissions.PlatformBillingManage, Permissions.OrganizationBillingView }
        };

        _authorizer = new TestResourceAuthorizer(_userContext, _tenantContext);

        // Seed organizations
        _orgRepo.Organizations.Add(new Organization
        {
            Id = _orgA,
            TenantId = _tenantA,
            Name = "Sunny Daycare Center A",
            IsActive = true
        });

        _orgRepo.Organizations.Add(new Organization
        {
            Id = _orgB,
            TenantId = _tenantB,
            Name = "Nordic Pine Daycare B",
            IsActive = true
        });

        _billingService = new BillingService(
            _billingRepo,
            _orgRepo,
            _authorizer,
            _tenantContext,
            _userContext,
            _auditService,
            NullLogger<BillingService>.Instance);
    }

    [Test]
    public async Task PlatformAdmin_CanCreateDraft_WithServerCalculatedTotals()
    {
        var request = new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Description = "Platform Subscription - October 2026",
            TaxRatePercentage = 10, // 10% tax
            LineItems = new List<CreateInvoiceLineItemRequest>
            {
                new() { Description = "Sprout Plan Base", Quantity = 1, UnitPrice = 100m, Category = "Subscription" },
                new() { Description = "Extra Classroom Addon", Quantity = 2, UnitPrice = 25m, Category = "AdditionalService" }
            }
        };

        var invoice = await _billingService.CreateDraftInvoiceAsync(request);

        Assert.That(invoice, Is.Not.Null);
        Assert.That(invoice.Status, Is.EqualTo(InvoiceStatus.Draft));
        Assert.That(invoice.InvoiceNumber, Does.StartWith("INV-"));
        // Subtotal = 100 + 50 = 150
        Assert.That(invoice.Subtotal, Is.EqualTo(150.00m));
        // Tax = 10% of 150 = 15
        Assert.That(invoice.TaxAmount, Is.EqualTo(15.00m));
        // Total = 165
        Assert.That(invoice.TotalAmount, Is.EqualTo(165.00m));
        Assert.That(invoice.BalanceDue, Is.EqualTo(165.00m));
        Assert.That(invoice.AmountPaid, Is.EqualTo(0m));

        Assert.That(_auditService.Entries.Any(e => e.Action == "Invoice.DraftCreated"), Is.True);
    }

    [Test]
    public async Task PlatformAdmin_CanPublishDraft_InvoiceBecomesUnpaid()
    {
        var request = new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Description = "October Invoice",
            LineItems = new List<CreateInvoiceLineItemRequest>
            {
                new() { Description = "Base Plan", Quantity = 1, UnitPrice = 200m }
            }
        };

        var draft = await _billingService.CreateDraftInvoiceAsync(request);
        Assert.That(draft.Status, Is.EqualTo(InvoiceStatus.Draft));

        var published = await _billingService.PublishInvoiceAsync(draft.Id);

        Assert.That(published.Status, Is.EqualTo(InvoiceStatus.Unpaid));
        Assert.That(published.PublishedAt, Is.Not.Null);
        Assert.That(published.PublishedBy, Is.EqualTo(_userContext.Email));
        Assert.That(_auditService.Entries.Any(e => e.Action == "Invoice.Published"), Is.True);
    }

    [Test]
    public async Task DraftInvoices_AreHiddenFromOrganizationAdmin()
    {
        // Platform Admin creates 1 draft and 1 published invoice for Org A
        var draft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Draft Fee", Quantity = 1, UnitPrice = 100m } }
        });

        var publishedDraft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Published Fee", Quantity = 1, UnitPrice = 200m } }
        });
        await _billingService.PublishInvoiceAsync(publishedDraft.Id);

        // Switch identity to Organization Admin of Org A
        _userContext.Roles = new List<string> { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = new List<string> { Permissions.OrganizationBillingView };
        _userContext.OrganizationId = _orgA;

        var orgInvoices = await _billingService.GetInvoicesForOrganizationAsync(_orgA);

        // Org admin should only see the 1 published invoice, NEVER the draft
        Assert.That(orgInvoices.Count, Is.EqualTo(1));
        Assert.That(orgInvoices[0].Id, Is.EqualTo(publishedDraft.Id));
        Assert.That(orgInvoices.Any(i => i.Id == draft.Id), Is.False);
    }

    [Test]
    public void OrganizationAdmin_FromOrgA_CannotAccessInvoiceFromOrgB()
    {
        // Create published invoice for Org B
        var invoiceB = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantB,
            OrganizationId = _orgB,
            InvoiceNumber = "INV-B-001",
            Status = InvoiceStatus.Unpaid,
            TotalAmount = 300m,
            Subtotal = 300m,
            DueDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        _billingRepo.Invoices[invoiceB.Id] = invoiceB;

        // Switch to Org Admin of Org A
        _userContext.Roles = new List<string> { RoleTemplates.OrganizationAdmin };
        _userContext.Permissions = new List<string> { Permissions.OrganizationBillingView };
        _userContext.OrganizationId = _orgA;
        _tenantContext.TenantId = _tenantA;

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _billingService.GetInvoiceByIdAsync(invoiceB.Id);
        });
    }

    [Test]
    public async Task OverdueStatus_IsDerived_WhenDueDatePassedWithBalanceDue()
    {
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantA,
            OrganizationId = _orgA,
            InvoiceNumber = "INV-PAST-DUE",
            Status = InvoiceStatus.Unpaid,
            Subtotal = 250m,
            TotalAmount = 250m,
            AmountPaid = 0m,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-30),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5) // 5 days past due
        };
        _billingRepo.Invoices[inv.Id] = inv;

        var fetched = await _billingService.GetInvoiceByIdAsync(inv.Id);

        Assert.That(fetched, Is.Not.Null);
        // Overdue must be dynamically derived without requiring a manual daily job
        Assert.That(fetched!.Status, Is.EqualTo(InvoiceStatus.Overdue));
    }

    [Test]
    public async Task FullyPaidInvoice_IsNotTreatedAsOverdue_EvenIfDueDatePassed()
    {
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantA,
            OrganizationId = _orgA,
            InvoiceNumber = "INV-FULLY-PAID",
            Status = InvoiceStatus.Paid,
            Subtotal = 250m,
            TotalAmount = 250m,
            AmountPaid = 250m,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-30),
            DueDate = DateTimeOffset.UtcNow.AddDays(-5)
        };
        _billingRepo.Invoices[inv.Id] = inv;

        var fetched = await _billingService.GetInvoiceByIdAsync(inv.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Status, Is.EqualTo(InvoiceStatus.Paid));
        Assert.That(fetched.BalanceDue, Is.EqualTo(0m));
    }

    [Test]
    public async Task RecordingPayment_PartialPayment_UpdatesBalanceAndStatus()
    {
        var draft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Tuition & Care", Quantity = 1, UnitPrice = 500m } }
        });
        var published = await _billingService.PublishInvoiceAsync(draft.Id);

        var payment = await _billingService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = published.Id,
            Amount = 200m,
            PaymentMethod = "ACH",
            PaymentReference = "ACH-TRX-1002"
        });

        Assert.That(payment, Is.Not.Null);
        Assert.That(payment.Amount, Is.EqualTo(200m));

        var updatedInvoice = await _billingService.GetInvoiceByIdAsync(published.Id);
        Assert.That(updatedInvoice!.AmountPaid, Is.EqualTo(200m));
        Assert.That(updatedInvoice.BalanceDue, Is.EqualTo(300m));
        Assert.That(updatedInvoice.Status, Is.EqualTo(InvoiceStatus.PartiallyPaid));

        Assert.That(_auditService.Entries.Any(e => e.Action == "Payment.Recorded"), Is.True);
    }

    [Test]
    public async Task RecordingPayment_FullPayment_SetsStatusToPaid()
    {
        var draft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Annual Licensing", Quantity = 1, UnitPrice = 400m } }
        });
        var published = await _billingService.PublishInvoiceAsync(draft.Id);

        await _billingService.RecordPaymentAsync(new RecordPaymentRequest
        {
            InvoiceId = published.Id,
            Amount = 400m,
            PaymentMethod = "BankTransfer",
            PaymentReference = "WIRE-9921"
        });

        var updatedInvoice = await _billingService.GetInvoiceByIdAsync(published.Id);
        Assert.That(updatedInvoice!.AmountPaid, Is.EqualTo(400m));
        Assert.That(updatedInvoice.BalanceDue, Is.EqualTo(0m));
        Assert.That(updatedInvoice.Status, Is.EqualTo(InvoiceStatus.Paid));
        Assert.That(updatedInvoice.PaidAt, Is.Not.Null);
    }

    [Test]
    public async Task Overpayment_ThrowsInvalidOperationException()
    {
        var draft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Service Fee", Quantity = 1, UnitPrice = 100m } }
        });
        var published = await _billingService.PublishInvoiceAsync(draft.Id);

        // Attempting to record 150 against a 100 balance due
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _billingService.RecordPaymentAsync(new RecordPaymentRequest
            {
                InvoiceId = published.Id,
                Amount = 150m,
                PaymentReference = "OVERPAY-REF"
            });
        });
    }

    [Test]
    public async Task VoidInvoice_SetsVoidStatusAndReason_CannotBeVoidedTwice()
    {
        var draft = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Issued by Mistake", Quantity = 1, UnitPrice = 150m } }
        });
        var published = await _billingService.PublishInvoiceAsync(draft.Id);

        var voided = await _billingService.VoidInvoiceAsync(published.Id, "Customer duplicate invoice");
        Assert.That(voided.Status, Is.EqualTo(InvoiceStatus.Void));
        Assert.That(voided.VoidReason, Is.EqualTo("Customer duplicate invoice"));
        Assert.That(voided.VoidedAt, Is.Not.Null);

        // Trying to void again throws
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _billingService.VoidInvoiceAsync(published.Id, "Trying again");
        });
    }

    [Test]
    public async Task BillingModeSwitch_FromManualToAutomatic_PreservesHistory_AndGeneratesEligibleInvoice()
    {
        // 1. Issue a manual invoice in manual mode
        var manualInv = await _billingService.CreateDraftInvoiceAsync(new CreateDraftInvoiceRequest
        {
            OrganizationId = _orgA,
            LineItems = new List<CreateInvoiceLineItemRequest> { new() { Description = "Manual Startup Fee", Quantity = 1, UnitPrice = 500m } }
        });
        await _billingService.PublishInvoiceAsync(manualInv.Id);

        // 2. Switch to Automatic mode
        var settings = await _billingService.UpdateBillingModeAsync(new UpdateBillingModeRequest
        {
            OrganizationId = _orgA,
            Mode = BillingMode.Automatic,
            Frequency = BillingFrequency.Monthly,
            EffectiveDate = DateTimeOffset.UtcNow.AddDays(-1),
            Reason = "Contract renewal to automated monthly billing"
        });

        Assert.That(settings.Mode, Is.EqualTo(BillingMode.Automatic));
        Assert.That(_auditService.Entries.Any(e => e.Action == "BillingMode.Changed"), Is.True);

        // 3. Execute automatic billing run
        var runResult = await _billingService.ExecuteAutomaticBillingRunAsync(DateTimeOffset.UtcNow);
        Assert.That(runResult.GeneratedInvoices, Is.GreaterThanOrEqualTo(1));

        // 4. Repeated run in the same period does NOT create duplicate invoices
        var repeatRun = await _billingService.ExecuteAutomaticBillingRunAsync(DateTimeOffset.UtcNow);
        Assert.That(repeatRun.GeneratedInvoices, Is.EqualTo(0));

        // 5. Existing manual invoice and outstanding balances are preserved
        var orgInvoices = await _billingService.GetInvoicesForOrganizationAsync(_orgA);
        Assert.That(orgInvoices.Any(i => i.Id == manualInv.Id), Is.True);
    }

    // --- Mocks and Test Doubles ---

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
            Payments.Add(payment);
            if (payment.InvoiceId.HasValue && Invoices.TryGetValue(payment.InvoiceId.Value, out var inv))
            {
                inv.Payments.Add(payment);
                inv.AmountPaid += payment.Amount;
                if (inv.BalanceDue <= 0)
                {
                    inv.Status = InvoiceStatus.Paid;
                    inv.PaidAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    inv.Status = InvoiceStatus.PartiallyPaid;
                }
            }
            return Task.FromResult(payment);
        }

        public Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default)
        {
            var p = Payments.FirstOrDefault(x => x.Id == paymentId);
            return Task.FromResult(p);
        }

        public Task<Payment?> GetPaymentByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default)
        {
            var p = Payments.FirstOrDefault(x => x.ProviderPaymentId == providerPaymentId && !x.IsReversed);
            return Task.FromResult(p);
        }

        public Task<Payment?> GetPaymentByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default)
        {
            var p = Payments.FirstOrDefault(x => x.ReceiptNumber == receiptNumber && !x.IsReversed);
            return Task.FromResult(p);
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
        {
            return Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));
        }

        public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));
        }

        public Task<IEnumerable<Organization>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Organizations.AsEnumerable());
        }

        public Task<Organization> AddAsync(Organization organization, CancellationToken cancellationToken = default)
        {
            Organizations.Add(organization);
            return Task.FromResult(organization);
        }

        public Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

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
        public Task<IReadOnlyList<Child>> FilterAccessibleChildrenAsync(IEnumerable<Child> children, string permission, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Child>>(children.ToList());
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
