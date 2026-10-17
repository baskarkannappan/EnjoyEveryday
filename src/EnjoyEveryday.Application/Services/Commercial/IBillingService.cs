using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Application.Services;

public class CreateInvoiceLineItemRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Category { get; set; }
}

public class CreateDraftInvoiceRequest
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset IssueDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueDate { get; set; } = DateTimeOffset.UtcNow.AddDays(14);
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? PaymentInstructions { get; set; }
    public decimal TaxRatePercentage { get; set; } = 0;
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class UpdateDraftInvoiceRequest
{
    public Guid InvoiceId { get; set; }
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? PaymentInstructions { get; set; }
    public decimal TaxRatePercentage { get; set; }
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class RecordPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string PaymentReference { get; set; } = string.Empty;
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}

public class RecordCashPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ReceiptDate { get; set; } = DateTimeOffset.UtcNow;
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

public class ReversePaymentRequest
{
    public Guid PaymentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CreateRazorpayOrderRequest
{
    public Guid InvoiceId { get; set; }
}

public class RazorpayOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class VerifyRazorpayPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public class RazorpayVerificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }
}

public class UpdateBillingModeRequest
{
    public Guid OrganizationId { get; set; }
    public BillingMode Mode { get; set; }
    public BillingFrequency Frequency { get; set; } = BillingFrequency.Monthly;
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}

public class AutomaticBillingRunResult
{
    public int ProcessedOrganizations { get; set; }
    public int GeneratedInvoices { get; set; }
    public List<string> GeneratedInvoiceNumbers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public interface IBillingService
{
    Task<Invoice> CreateDraftInvoiceAsync(CreateDraftInvoiceRequest request, CancellationToken ct = default);
    Task<Invoice> UpdateDraftInvoiceAsync(UpdateDraftInvoiceRequest request, CancellationToken ct = default);
    Task<Invoice> PublishInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<Invoice> VoidInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default);
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<List<Invoice>> GetInvoicesForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<List<Invoice>> GetAllInvoicesAsync(InvoiceStatus? status = null, Guid? organizationId = null, CancellationToken ct = default);
    Task<Payment> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<Payment> RecordCashPaymentAsync(RecordCashPaymentRequest request, CancellationToken ct = default);
    Task<Payment> ReversePaymentAsync(ReversePaymentRequest request, CancellationToken ct = default);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<RazorpayOrderResponse> CreateRazorpayOrderAsync(CreateRazorpayOrderRequest request, CancellationToken ct = default);
    Task<RazorpayVerificationResult> VerifyRazorpayPaymentAsync(VerifyRazorpayPaymentRequest request, CancellationToken ct = default);
    Task<bool> ProcessRazorpayWebhookAsync(string requestBody, string signatureHeader, CancellationToken ct = default);
    Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<List<Payment>> GetInvoicePaymentsAsync(Guid invoiceId, CancellationToken ct = default);
    Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<OrganizationBillingSettings> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default);
    Task<OrganizationBillingSettings> UpdateBillingModeAsync(UpdateBillingModeRequest request, CancellationToken ct = default);
    Task<AutomaticBillingRunResult> ExecuteAutomaticBillingRunAsync(DateTimeOffset asOfDate, CancellationToken ct = default);
}
