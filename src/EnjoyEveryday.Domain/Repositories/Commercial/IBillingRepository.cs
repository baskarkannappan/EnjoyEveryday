using EnjoyEveryday.Domain.Entities;

namespace EnjoyEveryday.Domain.Repositories;

public interface IBillingRepository
{
    Task<List<Invoice>> GetInvoicesForOrganizationAsync(Guid tenantId, Guid organizationId, bool includeDrafts, CancellationToken ct = default);
    Task<List<Invoice>> GetAllInvoicesAsync(Guid? tenantId, Guid? organizationId, InvoiceStatus? status, CancellationToken ct = default);
    Task<Invoice?> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task<bool> UpdateInvoiceAsync(Invoice invoice, CancellationToken ct = default);
    Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<Payment?> GetPaymentByProviderPaymentIdAsync(string providerPaymentId, CancellationToken ct = default);
    Task<Payment?> GetPaymentByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default);
    Task<Payment> ReversePaymentAsync(Guid paymentId, string reversedBy, string reason, CancellationToken ct = default);
    Task<Invoice> RecalculateInvoiceBalanceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<List<Payment>> GetPaymentsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<List<Payment>> GetPaymentsForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<OrganizationBillingSettings?> GetBillingSettingsAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> UpsertBillingSettingsAsync(OrganizationBillingSettings settings, CancellationToken ct = default);
    Task<List<OrganizationBillingSettings>> GetOrganizationsDueForAutomaticBillingAsync(DateTimeOffset asOfDate, CancellationToken ct = default);
}
