using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class BillingService : IBillingService
{
    private readonly IBillingRepository _billingRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IResourceAuthorizer _authorizer;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<BillingService> _logger;
    private readonly IRazorpayGateway _razorpayGateway;
    private readonly IEmailNotificationService? _emailService;
    private readonly IPlatformBusinessEventService? _eventService;

    public BillingService(
        IBillingRepository billingRepository,
        IOrganizationRepository organizationRepository,
        IResourceAuthorizer authorizer,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        ILogger<BillingService> logger,
        IRazorpayGateway? razorpayGateway = null,
        IEmailNotificationService? emailService = null,
        IPlatformBusinessEventService? eventService = null)
    {
        _billingRepository = billingRepository;
        _organizationRepository = organizationRepository;
        _authorizer = authorizer;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _logger = logger;
        _razorpayGateway = razorpayGateway ?? new DefaultFallbackRazorpayGateway();
        _emailService = emailService;
        _eventService = eventService;
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
            Currency = !string.IsNullOrWhiteSpace(request.Currency) && request.Currency != "INR"
                ? request.Currency.ToUpperInvariant()
                : (!string.IsNullOrWhiteSpace(org.Currency) ? org.Currency.ToUpperInvariant() : "INR"),
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

}
