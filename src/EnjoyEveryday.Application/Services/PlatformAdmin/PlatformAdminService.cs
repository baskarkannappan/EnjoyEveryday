using System.Collections.Concurrent;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ILogger<PlatformAdminService> _logger;

    // Synchronized stores for commercial, lifecycle, billing, and audit records
    private static readonly ConcurrentDictionary<Guid, SubscriptionPlan> _plans = new();
    private static readonly ConcurrentDictionary<Guid, PlatformOrganizationDto> _organizations = new();
    private static readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();
    private static readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private static readonly List<PlatformAuditLog> _auditLogs = new();
    private static readonly object _lock = new();

    static PlatformAdminService()
    {
        InitializeSeedData();
    }

    public PlatformAdminService(
        IOrganizationRepository organizationRepository,
        IBranchRepository branchRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        ILogger<PlatformAdminService> logger)
    {
        _organizationRepository = organizationRepository;
        _branchRepository = branchRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _logger = logger;
    }

    public Task<PlatformDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var orgs = _organizations.Values.ToList();
        var invoices = _invoices.Values.ToList();
        var payments = _payments.Values.ToList();

        var overdueInvoices = invoices.Where(i => i.Status == FinancialStatus.Overdue).ToList();
        var pendingPayments = payments.Where(p => !p.IsVerified).ToList();

        var summary = new PlatformDashboardSummary
        {
            TotalOrganizations = orgs.Count,
            ActiveOrganizations = orgs.Count(o => o.AccountStatus == AccountStatus.Active),
            PendingApprovals = orgs.Count(o => o.AccountStatus == AccountStatus.PendingApproval || o.AccountStatus == AccountStatus.Onboarding),
            SuspendedOrganizations = orgs.Count(o => o.AccountStatus == AccountStatus.Suspended),
            ActiveSubscriptions = orgs.Count(o => o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial),
            AvailablePlansCount = _plans.Count,
            OutstandingBalance = invoices.Where(i => i.Status != FinancialStatus.Reconciled).Sum(i => i.BalanceDue),
            OverdueInvoicesCount = overdueInvoices.Count,
            OverdueAmount = overdueInvoices.Sum(i => i.BalanceDue),
            PaymentsAwaitingVerificationCount = pendingPayments.Count,
            CollectionsThisMonth = payments.Where(p => p.IsVerified && p.PaymentDate >= DateTimeOffset.UtcNow.AddDays(-30)).Sum(p => p.Amount),
            Currency = "USD"
        };

        return Task.FromResult(summary);
    }

    public Task<List<PlatformOrganizationDto>> GetOrganizationsOverviewAsync(
        string? search = null,
        string? accountStatus = null,
        string? financialStatus = null,
        string? lifecycleStage = null,
        CancellationToken cancellationToken = default)
    {
        var query = _organizations.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(o => o.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                                  || (o.Location != null && o.Location.Contains(s, StringComparison.OrdinalIgnoreCase))
                                  || o.AdminEmail.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(accountStatus) && accountStatus != "All")
        {
            if (Enum.TryParse<AccountStatus>(accountStatus, true, out var status))
            {
                query = query.Where(o => o.AccountStatus == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(financialStatus) && financialStatus != "All")
        {
            if (Enum.TryParse<FinancialStatus>(financialStatus, true, out var status))
            {
                query = query.Where(o => o.FinancialStatus == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(lifecycleStage) && lifecycleStage != "All")
        {
            query = query.Where(o => string.Equals(o.LifecycleStageKey, lifecycleStage, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(query.OrderByDescending(o => o.CreatedAt).ToList());
    }

    public Task<PlatformOrganizationDto?> GetOrganizationDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        return Task.FromResult(org);
    }

    public Task<List<SubscriptionPlan>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_plans.Values.OrderBy(p => p.BasePrice).ToList());
    }

    public Task<List<Invoice>> GetInvoicesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _invoices.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            if (Enum.TryParse<FinancialStatus>(status, true, out var parsed))
            {
                query = query.Where(i => i.Status == parsed);
            }
        }
        return Task.FromResult(query.OrderByDescending(i => i.DueDate).ToList());
    }

    public Task<List<Payment>> GetPaymentsAsync(bool? awaitingVerificationOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _payments.Values.AsEnumerable();
        if (awaitingVerificationOnly == true)
        {
            query = query.Where(p => !p.IsVerified);
        }
        return Task.FromResult(query.OrderByDescending(p => p.PaymentDate).ToList());
    }

    public Task<List<PlatformAuditLog>> GetAuditLogsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_auditLogs.OrderByDescending(a => a.Timestamp).Take(limit).ToList());
        }
    }

    public Task<PlatformHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PlatformHealthStatus());
    }

    public Task<PlatformOrganizationDto> CreateOrganizationWorkflowAsync(
        string name,
        string location,
        Guid planId,
        string adminEmail,
        CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(planId, out var plan);
        plan ??= _plans.Values.First();

        var id = Guid.NewGuid();
        var dto = new PlatformOrganizationDto
        {
            Id = id,
            TenantId = id,
            Name = name,
            Location = location,
            BranchCount = 1,
            AccountStatus = AccountStatus.Onboarding,
            FinancialStatus = FinancialStatus.Unbilled,
            PlanName = plan.Name,
            MonthlyRate = plan.BasePrice,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(30),
            AdminEmail = adminEmail,
            LifecycleStageKey = "Onboarding",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _organizations[id] = dto;

        AddAuditLog("Organization Registered", $"New daycare organization '{name}' added to onboarding pipeline with plan '{plan.Name}'.", "Info", id);

        return Task.FromResult(dto);
    }

    public Task<bool> UpdateAccountStatusAsync(Guid organizationId, AccountStatus newStatus, string reason, CancellationToken cancellationToken = default)
    {
        if (_organizations.TryGetValue(organizationId, out var org))
        {
            var oldStatus = org.AccountStatus;
            org.AccountStatus = newStatus;

            // Sync lifecycle stage key
            org.LifecycleStageKey = newStatus switch
            {
                AccountStatus.Onboarding => "Onboarding",
                AccountStatus.PendingApproval => "Onboarding",
                AccountStatus.Trial => "Active",
                AccountStatus.Active => "Active",
                AccountStatus.Suspended => "Invoiced",
                _ => "Active"
            };

            AddAuditLog("Account Status Changed", $"Organization '{org.Name}' status transitioned from {oldStatus} to {newStatus}. Reason: {reason}", "Info", organizationId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> VerifyPaymentWorkflowAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (_payments.TryGetValue(paymentId, out var payment))
        {
            payment.IsVerified = true;
            payment.VerifiedBy = _userContext.UserId != Guid.Empty ? _userContext.UserId.ToString() : "Platform Admin";
            payment.VerifiedAt = DateTimeOffset.UtcNow;

            if (payment.InvoiceId.HasValue && _invoices.TryGetValue(payment.InvoiceId.Value, out var invoice))
            {
                invoice.AmountPaid += payment.Amount;
                if (invoice.BalanceDue <= 0)
                {
                    invoice.Status = FinancialStatus.Reconciled;
                    invoice.PaidAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    invoice.Status = FinancialStatus.PartiallyPaid;
                }

                // Update organization financial status if no other invoices overdue
                if (_organizations.TryGetValue(payment.OrganizationId, out var org))
                {
                    org.FinancialStatus = invoice.Status;
                }
            }

            AddAuditLog("Payment Verified", $"Payment {payment.PaymentReference} of {payment.Amount:C0} reconciled and verified.", "Info", paymentId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Invoice> IssueInvoiceWorkflowAsync(Guid organizationId, decimal amount, string description, CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        var orgName = org?.Name ?? "Organization";

        var id = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = id,
            TenantId = org?.TenantId ?? organizationId,
            OrganizationId = organizationId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}",
            Subtotal = amount,
            DiscountAmount = 0,
            TotalAmount = amount,
            AmountPaid = 0,
            Currency = "USD",
            Status = FinancialStatus.InvoiceDue,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Description = description,
            BillingContactEmail = org?.AdminEmail ?? "billing@daycare.com",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _invoices[id] = invoice;

        if (org != null)
        {
            org.FinancialStatus = FinancialStatus.InvoiceDue;
            org.LifecycleStageKey = "Invoiced";
        }

        AddAuditLog("Invoice Issued", $"Invoice {invoice.InvoiceNumber} for {amount:C0} issued to '{orgName}'.", "Info", id);

        return Task.FromResult(invoice);
    }

    private static void AddAuditLog(string action, string details, string severity, Guid? entityId = null)
    {
        lock (_lock)
        {
            _auditLogs.Insert(0, new PlatformAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Actor = "Platform Superadmin",
                Action = action,
                EntityType = "Platform",
                EntityId = entityId,
                Details = details,
                Severity = severity
            });
            if (_auditLogs.Count > 100)
            {
                _auditLogs.RemoveAt(_auditLogs.Count - 1);
            }
        }
    }

    private static void InitializeSeedData()
    {
        // Plans
        var starterPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Sprout (Starter)",
            Code = "PLAN-SPROUT",
            Description = "Ideal for single-branch nurseries and boutique preschools.",
            BasePrice = 149.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 1,
            MaxClassrooms = 3,
            MaxChildren = 35,
            IncludesAiFeatures = false
        };

        var growthPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Bloom (Growth)",
            Code = "PLAN-BLOOM",
            Description = "Comprehensive suite with AI assistants and multiple classrooms.",
            BasePrice = 299.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 2,
            MaxClassrooms = 8,
            MaxChildren = 90,
            IncludesAiFeatures = true
        };

        var canopyPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Canopy (Multi-Branch)",
            Code = "PLAN-CANOPY",
            Description = "Full multi-branch daycare network with organization intelligence.",
            BasePrice = 599.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 6,
            MaxClassrooms = 24,
            MaxChildren = 300,
            IncludesAiFeatures = true
        };

        _plans[starterPlan.Id] = starterPlan;
        _plans[growthPlan.Id] = growthPlan;
        _plans[canopyPlan.Id] = canopyPlan;

        // Seed Organizations
        var org1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var org1 = new PlatformOrganizationDto
        {
            Id = org1Id,
            TenantId = org1Id,
            Name = "Little Stars Early Learning Center",
            Location = "Seattle, WA",
            BranchCount = 2,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanName = "Bloom (Growth)",
            MonthlyRate = 299.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(18),
            AdminEmail = "director@littlestars.org",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
        };

        var org2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var org2 = new PlatformOrganizationDto
        {
            Id = org2Id,
            TenantId = org2Id,
            Name = "Nordic Pine Forest Preschool",
            Location = "Portland, OR",
            BranchCount = 1,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanName = "Sprout (Starter)",
            MonthlyRate = 149.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(22),
            AdminEmail = "karin@nordicpine.edu",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3)
        };

        var org3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var org3 = new PlatformOrganizationDto
        {
            Id = org3Id,
            TenantId = org3Id,
            Name = "Cedar Grove Childcare Network",
            Location = "Bellevue, WA",
            BranchCount = 4,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.InvoiceDue,
            PlanName = "Canopy (Multi-Branch)",
            MonthlyRate = 599.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(5),
            AdminEmail = "finance@cedargrove.com",
            LifecycleStageKey = "Invoiced",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-11)
        };

        var org4Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var org4 = new PlatformOrganizationDto
        {
            Id = org4Id,
            TenantId = org4Id,
            Name = "Sunny Hollow Montessori",
            Location = "Redmond, WA",
            BranchCount = 1,
            AccountStatus = AccountStatus.Onboarding,
            FinancialStatus = FinancialStatus.Unbilled,
            PlanName = "Bloom (Growth)",
            MonthlyRate = 299.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(30),
            AdminEmail = "onboarding@sunnyhollow.org",
            LifecycleStageKey = "Onboarding",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var org5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var org5 = new PlatformOrganizationDto
        {
            Id = org5Id,
            TenantId = org5Id,
            Name = "Meadowlark Family Cooperative",
            Location = "Spokane, WA",
            BranchCount = 1,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Overdue,
            PlanName = "Sprout (Starter)",
            MonthlyRate = 149.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(-8),
            AdminEmail = "treasurer@meadowlark.org",
            LifecycleStageKey = "Invoiced",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-2)
        };

        var org6Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var org6 = new PlatformOrganizationDto
        {
            Id = org6Id,
            TenantId = org6Id,
            Name = "Evergreen Discovery Academy",
            Location = "Tacoma, WA",
            BranchCount = 2,
            AccountStatus = AccountStatus.Trial,
            FinancialStatus = FinancialStatus.PaymentAwaitingVerification,
            PlanName = "Bloom (Growth)",
            MonthlyRate = 299.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(12),
            AdminEmail = "contact@evergreendiscovery.com",
            LifecycleStageKey = "PaymentVerification",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14)
        };

        _organizations[org1Id] = org1;
        _organizations[org2Id] = org2;
        _organizations[org3Id] = org3;
        _organizations[org4Id] = org4;
        _organizations[org5Id] = org5;
        _organizations[org6Id] = org6;

        // Invoices
        var inv1Id = Guid.NewGuid();
        _invoices[inv1Id] = new Invoice
        {
            Id = inv1Id,
            TenantId = org3Id,
            OrganizationId = org3Id,
            InvoiceNumber = "INV-202610-8491",
            Subtotal = 599.00m,
            TotalAmount = 599.00m,
            AmountPaid = 0,
            Status = FinancialStatus.InvoiceDue,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(5),
            Description = "Canopy Tier Subscription - October 2026",
            BillingContactEmail = "finance@cedargrove.com"
        };

        var inv2Id = Guid.NewGuid();
        _invoices[inv2Id] = new Invoice
        {
            Id = inv2Id,
            TenantId = org5Id,
            OrganizationId = org5Id,
            InvoiceNumber = "INV-202609-3120",
            Subtotal = 149.00m,
            TotalAmount = 149.00m,
            AmountPaid = 0,
            Status = FinancialStatus.Overdue,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-38),
            DueDate = DateTimeOffset.UtcNow.AddDays(-8),
            Description = "Sprout Tier Subscription - September 2026",
            BillingContactEmail = "treasurer@meadowlark.org"
        };

        // Payments
        var pay1Id = Guid.NewGuid();
        _payments[pay1Id] = new Payment
        {
            Id = pay1Id,
            TenantId = org6Id,
            OrganizationId = org6Id,
            PaymentReference = "PAY-ACH-99410",
            Amount = 299.00m,
            Method = "ACH",
            IsVerified = false,
            PaymentDate = DateTimeOffset.UtcNow.AddDays(-1),
            Status = "Pending Verification",
            TransactionId = "txn_ach_88319041"
        };

        // Audit Logs
        AddAuditLog("System Boot", "EnjoyEveryday Platform Administration services initialized.", "Info");
        AddAuditLog("Organization Created", "Sunny Hollow Montessori initiated registration and onboarding.", "Info", org4Id);
        AddAuditLog("Payment Submitted", "ACH payment PAY-ACH-99410 for $299.00 awaiting administrative reconciliation.", "Warning", pay1Id);
    }
}
