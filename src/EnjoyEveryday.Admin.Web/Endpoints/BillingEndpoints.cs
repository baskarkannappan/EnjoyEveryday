using System.Text;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnjoyEveryday.Admin.Web.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/billing/invoices/{id:guid}/document", async (
            Guid id,
            IBillingService billingService,
            IResourceAuthorizer authorizer,
            IUserContext userContext,
            ITenantContext tenantContext,
            IAuditService auditService,
            HttpContext context) =>
        {
            var invoice = await billingService.GetInvoiceByIdAsync(id, context.RequestAborted);
            if (invoice == null)
            {
                return Results.NotFound();
            }

            // Platform billing managers can access any invoice
            await authorizer.RequireInvoiceAccessAsync(invoice, Permissions.PlatformBillingManage, context.RequestAborted);

            await auditService.LogAsync(new AuditEntry
            {
                TenantId = invoice.TenantId,
                UserId = userContext.UserId,
                Action = "Invoice.DocumentStreamedByAdmin",
                EntityType = nameof(Invoice),
                EntityId = invoice.Id,
                Details = new { invoice.InvoiceNumber, invoice.TotalAmount }
            }, context.RequestAborted);

            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "private, max-age=1800");

            var html = GenerateInvoiceDocumentHtml(invoice);
            return Results.Content(html, "text/html; charset=utf-8");
        }).RequireAuthorization();

        // STREAM PAYMENT RECEIPT FOR ADMIN
        endpoints.MapGet("/api/billing/payments/{id:guid}/receipt", async (
            Guid id,
            IBillingService billingService,
            IUserContext userContext,
            IAuditService auditService,
            HttpContext context) =>
        {
            var payment = await billingService.GetPaymentByIdAsync(id, context.RequestAborted);
            if (payment == null)
            {
                return Results.NotFound();
            }

            Invoice? invoice = null;
            if (payment.InvoiceId.HasValue)
            {
                invoice = await billingService.GetInvoiceByIdAsync(payment.InvoiceId.Value, context.RequestAborted);
            }

            await auditService.LogAsync(new AuditEntry
            {
                TenantId = payment.TenantId,
                UserId = userContext.UserId,
                Action = "Payment.ReceiptStreamedByAdmin",
                EntityType = nameof(Payment),
                EntityId = payment.Id,
                Details = new { payment.PaymentReference, payment.Amount, payment.Method }
            }, context.RequestAborted);

            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "private, max-age=1800");

            var html = GeneratePaymentReceiptHtml(payment, invoice);
            return Results.Content(html, "text/html; charset=utf-8");
        }).RequireAuthorization();

        return endpoints;
    }

    private static string GeneratePaymentReceiptHtml(Payment payment, Invoice? invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"<title>Payment Receipt {payment.ReceiptNumber ?? payment.PaymentReference} — EnjoyEveryday</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Figtree', Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5ef; color: #26332d; padding: 40px 20px; margin: 0; }");
        sb.AppendLine(".receipt-box { max-width: 680px; margin: auto; padding: 36px; background: #ffffff; border: 1px solid #e4e7df; border-radius: 16px; box-shadow: 0 8px 30px rgba(40, 55, 47, .06); position: relative; }");
        sb.AppendLine(".header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #557c6c; padding-bottom: 18px; margin-bottom: 24px; }");
        sb.AppendLine(".brand { font-size: 22px; font-weight: 800; color: #26332d; }");
        sb.AppendLine(".brand-sub { font-size: 12px; color: #6c7870; margin-top: 3px; }");
        sb.AppendLine(".rcpt-title { font-size: 22px; font-weight: 800; color: #557c6c; text-align: right; }");
        sb.AppendLine(".rcpt-number { font-size: 13.5px; font-weight: 750; font-family: monospace; text-align: right; margin-top: 3px; }");
        sb.AppendLine(".amount-banner { background: #e6eee9; border: 1px solid #c8dacd; border-radius: 12px; padding: 18px 24px; margin-bottom: 24px; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine(".amount-label { font-size: 13px; font-weight: 750; color: #3e6253; text-transform: uppercase; }");
        sb.AppendLine(".amount-val { font-size: 28px; font-weight: 850; color: #26332d; }");
        sb.AppendLine(".details-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 24px; font-size: 13px; }");
        sb.AppendLine(".detail-item { padding: 10px 14px; background: #f8f9f5; border-radius: 8px; border: 1px solid #e4e7df; }");
        sb.AppendLine(".detail-label { font-size: 11px; font-weight: 800; color: #6c7870; text-transform: uppercase; margin-bottom: 4px; }");
        sb.AppendLine(".detail-value { font-weight: 700; color: #26332d; }");
        sb.AppendLine(".reversed-stamp { border: 2px dashed #dc2626; color: #dc2626; padding: 12px; text-align: center; border-radius: 8px; margin-bottom: 20px; font-weight: 800; }");
        sb.AppendLine("@media print { body { background: #ffffff; padding: 0; } .receipt-box { border: none; box-shadow: none; padding: 0; } }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"receipt-box\">");
        
        if (payment.IsReversed)
        {
            sb.AppendLine($"<div class=\"reversed-stamp\">⚠️ PAYMENT REVERSED ON {payment.ReversedAt?.ToString("MMM dd, yyyy")}: {payment.ReversalReason} (Ref: {payment.ReversalReference})</div>");
        }

        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div class=\"brand\">🌿 EnjoyEveryday</div>");
        sb.AppendLine("    <div class=\"brand-sub\">Official Payment Receipt &amp; Voucher</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div class=\"rcpt-title\">PAYMENT RECEIPT</div>");
        sb.AppendLine($"    <div class=\"rcpt-number\">{payment.ReceiptNumber ?? payment.PaymentReference}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        var currencySymbol = "₹";
        sb.AppendLine("<div class=\"amount-banner\">");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div class=\"amount-label\">Amount Received</div>");
        sb.AppendLine($"    <div style=\"font-size: 12px; color: #6c7870;\">Status: {(payment.IsReversed ? "Reversed" : "Reconciled & Confirmed")}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine($"  <div class=\"amount-val\">{currencySymbol}{payment.Amount:N2}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"details-grid\">");
        sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Payment Method</div><div class=\"detail-value\">{(payment.Method == PaymentMethods.Cash ? "💵 Cash (In-Person Collection)" : (payment.Method == PaymentMethods.Razorpay ? "💳 Razorpay Online Payment" : payment.Method))}</div></div>");
        sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Payment Date</div><div class=\"detail-value\">{payment.PaymentDate:MMMM dd, yyyy}</div></div>");
        sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Invoice Number</div><div class=\"detail-value\">{payment.InvoiceNumber ?? invoice?.InvoiceNumber ?? "N/A"}</div></div>");
        sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Transaction / Provider ID</div><div class=\"detail-value\">{payment.ProviderPaymentId ?? payment.PaymentReference}</div></div>");
        if (!string.IsNullOrWhiteSpace(payment.RecordedByEmail))
        {
            sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Received / Recorded By</div><div class=\"detail-value\">{payment.RecordedByEmail}</div></div>");
        }
        if (!string.IsNullOrWhiteSpace(payment.VerifiedBy))
        {
            sb.AppendLine($"  <div class=\"detail-item\"><div class=\"detail-label\">Verification Authority</div><div class=\"detail-value\">{payment.VerifiedBy}</div></div>");
        }
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(payment.Notes))
        {
            sb.AppendLine("<div style=\"background: #f8f9f5; border: 1px solid #e4e7df; border-radius: 8px; padding: 12px; font-size: 12.5px; color: #6c7870; margin-bottom: 20px;\">");
            sb.AppendLine("  <strong>Notes:</strong> " + payment.Notes);
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<div style=\"font-size: 11.5px; color: #6c7870; text-align: center; border-top: 1px solid #e4e7df; padding-top: 16px;\">");
        sb.AppendLine("  Thank you for partnering with EnjoyEveryday. This receipt was generated automatically from the authoritative payment ledger.");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string GenerateInvoiceDocumentHtml(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"<title>Invoice {invoice.InvoiceNumber} — EnjoyEveryday</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Figtree', Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5ef; color: #26332d; padding: 40px 20px; margin: 0; }");
        sb.AppendLine(".invoice-box { max-width: 800px; margin: auto; padding: 36px; background: #ffffff; border: 1px solid #e4e7df; border-radius: 16px; box-shadow: 0 8px 30px rgba(40, 55, 47, .06); }");
        sb.AppendLine(".header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #557c6c; padding-bottom: 20px; margin-bottom: 28px; }");
        sb.AppendLine(".brand { font-size: 24px; font-weight: 800; color: #26332d; }");
        sb.AppendLine(".brand-sub { font-size: 13px; color: #6c7870; margin-top: 4px; }");
        sb.AppendLine(".inv-title { font-size: 26px; font-weight: 800; color: #557c6c; text-align: right; }");
        sb.AppendLine(".inv-number { font-size: 14px; font-weight: 750; font-family: monospace; text-align: right; }");
        sb.AppendLine(".grid-meta { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 28px; font-size: 13.5px; }");
        sb.AppendLine(".table { width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13.5px; }");
        sb.AppendLine(".table th { background: #f8f9f5; border-bottom: 1px solid #e4e7df; text-align: left; padding: 12px; font-size: 11.5px; text-transform: uppercase; color: #6c7870; }");
        sb.AppendLine(".table td { padding: 12px; border-bottom: 1px solid #edf1ec; }");
        sb.AppendLine(".totals { display: flex; justify-content: flex-end; margin-bottom: 24px; }");
        sb.AppendLine(".totals-box { width: 260px; font-size: 14px; display: flex; flex-direction: column; gap: 6px; }");
        sb.AppendLine(".totals-row { display: flex; justify-content: space-between; }");
        sb.AppendLine(".instructions { background: #f8f9f5; border: 1px solid #e4e7df; border-radius: 10px; padding: 16px; font-size: 13px; color: #6c7870; }");
        sb.AppendLine("@media print { body { background: #ffffff; padding: 0; } .invoice-box { border: none; box-shadow: none; padding: 0; } }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"invoice-box\">");
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div class=\"brand\">🌿 EnjoyEveryday</div>");
        sb.AppendLine("    <div class=\"brand-sub\">Daycare &amp; Early-Childhood Platform Operations</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div class=\"inv-title\">INVOICE</div>");
        sb.AppendLine($"    <div class=\"inv-number\">{invoice.InvoiceNumber}</div>");
        sb.AppendLine($"    <div style=\"font-size: 12px; color: #6c7870; text-align: right; margin-top: 3px;\">Status: {invoice.Status}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"grid-meta\">");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <div style=\"font-size: 11px; font-weight: 800; color: #6c7870; text-transform: uppercase;\">Invoice Memo / Details:</div>");
        sb.AppendLine($"    <div style=\"font-size: 15px; font-weight: 700; margin-top: 4px;\">{invoice.Description ?? "Monthly Subscription"}</div>");
        if (!string.IsNullOrWhiteSpace(invoice.BillingPeriod))
            sb.AppendLine($"    <div style=\"color: #6c7870;\">Billing Period: {invoice.BillingPeriod}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div style=\"text-align: right;\">");
        sb.AppendLine($"    <div><strong>Issue Date:</strong> {invoice.IssueDate:MMMM dd, yyyy}</div>");
        sb.AppendLine($"    <div style=\"margin-top: 4px;\"><strong>Due Date:</strong> {invoice.DueDate:MMMM dd, yyyy}</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Description</th><th style=\"text-align: right;\">Qty</th><th style=\"text-align: right;\">Unit Price</th><th style=\"text-align: right;\">Amount</th></tr></thead>");
        sb.AppendLine("<tbody>");
        if (invoice.LineItems.Any())
        {
            foreach (var li in invoice.LineItems)
            {
                sb.AppendLine($"<tr><td>{li.LineNumber}</td><td><strong>{li.Description}</strong></td><td style=\"text-align: right;\">{li.Quantity}</td><td style=\"text-align: right;\">{li.UnitPrice:C2}</td><td style=\"text-align: right; font-weight: 700;\">{li.LineTotal:C2}</td></tr>");
            }
        }
        else
        {
            sb.AppendLine($"<tr><td>1</td><td><strong>{invoice.Description ?? "Platform Fee"}</strong></td><td style=\"text-align: right;\">1</td><td style=\"text-align: right;\">{invoice.Subtotal:C2}</td><td style=\"text-align: right; font-weight: 700;\">{invoice.Subtotal:C2}</td></tr>");
        }
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine("<div class=\"totals-box\">");
        sb.AppendLine($"  <div class=\"totals-row\"><span style=\"color: #6c7870;\">Subtotal:</span><strong>{invoice.Subtotal:C2}</strong></div>");
        if (invoice.TaxAmount > 0)
            sb.AppendLine($"  <div class=\"totals-row\"><span style=\"color: #6c7870;\">Tax:</span><span>{invoice.TaxAmount:C2}</span></div>");
        sb.AppendLine($"  <div class=\"totals-row\" style=\"font-size: 16px; font-weight: 800; border-top: 1px solid #e4e7df; padding-top: 6px;\"><span>Total:</span><span>{invoice.TotalAmount:C2}</span></div>");
        sb.AppendLine($"  <div class=\"totals-row\" style=\"color: #557c6c;\"><span>Paid:</span><strong>{invoice.AmountPaid:C2}</strong></div>");
        sb.AppendLine($"  <div class=\"totals-row\" style=\"font-size: 15px; font-weight: 800; color: {(invoice.BalanceDue > 0 ? "#b55a5a" : "#26332d")}; border-top: 1px solid #e4e7df; padding-top: 6px;\"><span>Balance Due:</span><span>{invoice.BalanceDue:C2}</span></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(invoice.PaymentInstructions))
        {
            sb.AppendLine("<div class=\"instructions\">");
            sb.AppendLine("  <div style=\"font-weight: 750; color: #26332d; margin-bottom: 4px;\">Payment Remittance Instructions:</div>");
            sb.AppendLine($"  <div>{invoice.PaymentInstructions}</div>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}
